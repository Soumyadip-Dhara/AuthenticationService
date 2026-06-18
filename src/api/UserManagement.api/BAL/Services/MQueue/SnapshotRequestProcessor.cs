using FluentValidation;
using FluentValidation.Results;
using Newtonsoft.Json;
using Npgsql;
using RabbitMQ.Client;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.Models.DTO;
using UserManagement.Models.MQueue;
using UserManagement.RbbitMQ;
using UserMangement.BAL.Interfaces.MQueue;

namespace UserManagement.BAL.Services.MQueue
{
    public class SnapshotRequestProcessor : IMessageProcessor<SnapshotRequestModel>
    {
        private readonly ILogger<SnapshotRequestProcessor> _logger;
        private readonly IValidator<SnapshotRequestModel> _validator;
        private readonly string _connString;
        private readonly IMessageQueueRepository _messageQueueRepository;
        private readonly IMQueueProcessingService _mQueueProcessingService;

        public SnapshotRequestProcessor(
        ILogger<SnapshotRequestProcessor> logger,
        IValidator<SnapshotRequestModel> validator,
        IConfiguration config,
        IMessageQueueRepository messageQueueRepository,
        IMQueueProcessingService mQueueProcessingService)
        {
            _logger = logger;
            _validator = validator;
            _connString = config.GetConnectionString("UserManagementDBConnection");
            _messageQueueRepository = messageQueueRepository;
            _mQueueProcessingService = mQueueProcessingService;
        }

        public async Task<ValidationResult> ValidateMessage(SnapshotRequestModel message)
        {
            return await _validator.ValidateAsync(message);
        }

        public async Task ProcessMessage(SnapshotRequestModel message, IReadOnlyBasicProperties mqBasicProperties)




        {
            _logger.LogInformation($"Processing order: {message}");
            string payloadJson = System.Text.Json.JsonSerializer.Serialize<SnapshotRequestModel>(message);
            //var appId = mqBasicProperties.AppId;
            int appId = int.Parse(mqBasicProperties.AppId);
            var correlationId = mqBasicProperties.CorrelationId;
            var respondTo = message.Respond_to;
            var requestType = message.Request_type;

            try
            {

                if (requestType == 1)
                {


                    await using var connection = new NpgsqlConnection(_connString);
                    await connection.OpenAsync();
                    await using var cmd = new NpgsqlCommand(
            @"WITH base AS (
            SELECT 
            um.id AS user_id,
            um.is_active,
            urhul.role_has_level_id,
            r.title AS role_code,
            sm.scope_id AS scope_id
        FROM ""user"".user_master um
        JOIN ""user"".user_has_application uha 
            ON uha.user_id = um.id
        JOIN ""user"".user_application_has_user_role uaur 
            ON uha.id = uaur.user_has_app_id
        JOIN ""user"".user_role_has_user_level urhul 
            ON uaur.id = urhul.application_has_role_id
        JOIN ""master"".roles r 
            ON r.id = uaur.role_id
        LEFT JOIN ""user"".user_level_has_user_scope ulhus 
            ON urhul.id = ulhus.user_role_has_level_id
        LEFT JOIN ""master"".scope_master sm  
            ON ulhus.user_level_has_scope_id = sm.scope_id
        WHERE uha.app_id = @appId
    ),

-- -- remove duplication at user level
-- distinct_users AS (
--     SELECT DISTINCT user_id, is_active, role_has_level_id, role_code
--     FROM base
-- ),

-- role-wise counts
role_counts AS (
    SELECT 
        role_code,
        COUNT(DISTINCT user_id) AS user_count
    FROM base
    GROUP BY role_code
)

SELECT 
	COUNT(*) FILTER (WHERE is_active = true OR is_active = false ) AS ""totalUsers"",
    -- Active / Inactive
    COUNT(*) FILTER (WHERE is_active = true) AS ""totalActiveUsers"",
    COUNT(*) FILTER (WHERE is_active = false) AS ""totalInactiveUsers"",

    -- Role level based
    COUNT(*) FILTER (WHERE role_has_level_id = 447 ) AS ""ddoUserCount"",
    COUNT(*) FILTER (WHERE role_has_level_id <> 447 ) AS ""wbjitUserCount"",

    -- Scope count (distinct)
    -- (SELECT COUNT(DISTINCT scope_id) FROM base WHERE scope_id IS NOT NULL) 
	(
        SELECT 
    COUNT(*) 
FROM master.scope_master sm
WHERE level_id IN (
    SELECT level_id
    FROM master.application_level
    WHERE app_id = @appId
      AND app_level_id NOT IN (191, 192, 193, 194, 196)
)
AND is_active = true
    ) AS ""totalScopeCount"",

    -- Role-wise JSON
    (
        SELECT jsonb_object_agg(role_code, user_count)
        FROM role_counts
    ) AS ""roleWiseUserCounts"",

    -- Timestamp
    NOW() AS ""transactionTime""

FROM base;",
            connection
        );

                    // appId 
                    cmd.Parameters.Add(
                        new NpgsqlParameter("@appId", NpgsqlTypes.NpgsqlDbType.Integer)
                        {
                            Value = appId
                        }
                    );


                    //await using var reader = await cmd.ExecuteReaderAsync();
                    //if (await reader.ReadAsync())
                    //{

                    //    bool IsDone = reader.GetBoolean(reader.GetOrdinal("is_done"));
                    //    var Msg = reader.GetString(reader.GetOrdinal("response"));
                    //    if (!IsDone)
                    //    {
                    //        throw new Exception($"DB Function Failed: {Msg}");
                    //    }
                    //    await Task.CompletedTask;
                    //}

                    await using var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {

                        var json = reader.GetString(reader.GetOrdinal("roleWiseUserCounts"));

                        var result = new UserCountsDto
                        {
                            totalUsers = reader.GetInt64(reader.GetOrdinal("totalUsers")),
                            totalActiveUsers = reader.GetInt64(reader.GetOrdinal("totalActiveUsers")),
                            totalInactiveUsers = reader.GetInt64(reader.GetOrdinal("totalInactiveUsers")),

                            ddoUserCount = reader.GetInt64(reader.GetOrdinal("ddoUserCount")),
                            wbjitUserCount = reader.GetInt64(reader.GetOrdinal("wbjitUserCount")),

                            totalScopeCount = reader.GetInt64(reader.GetOrdinal("totalScopeCount")),

                            //roleWiseUserCounts = reader.GetString(reader.GetOrdinal("roleWiseUserCounts")), // JSON
                            roleWiseUserCounts = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, long>>(json),

                            transactionTime = reader.GetDateTime(reader.GetOrdinal("transactionTime"))
                        };

                        // Example: log or return
                        _logger.LogInformation($"Total Users: {result.totalUsers}");



                        try
                        {
                            var message_id = Guid.NewGuid();

                            _messageQueueRepository.Add(new MessageQueue
                            {
                                UniqueId = message_id,
                                QueueName = respondTo,
                                MessageBody = JsonConvert.SerializeObject(result),
                                CreatedAt = DateTime.Now
                            });

                            _messageQueueRepository.SaveChangesManaged();

                            await _mQueueProcessingService.ProcessQueueAsync(
                               respondTo, correlationId
                            );
                        }
                        catch (Exception)
                        {
                            throw;
                        }

                    }


                }
                else if (requestType == 2)
                {
                    await using var connection = new NpgsqlConnection(_connString);
                    await connection.OpenAsync();

                    await using var cmd = new NpgsqlCommand(@"
                WITH base AS (
                    SELECT 
                            um.id                         AS ""Id"",
                            um.name                       AS ""Name"",
                            um.email                      AS ""Email"",
                            um.hrms_id                    AS ""HrmsId"",
                            um.is_active                  AS ""IsActive"",
                            um.user_name                  AS ""UserName"",
                            um.expires_on                 AS ""ExpiresOn"",
                            um.designation                AS ""Designation"",
                            um.mobile_number              AS ""MobileNumber"",
                            um.effective_from             AS ""EffectiveFrom"",

                            uha.app_id                    AS ""AppId"",
                            app.title                     AS ""AppName"",

                            r.id                          AS ""RoleId"",
                            r.title                       AS ""RoleName"",

                            urhul.role_has_level_id       AS ""LevelId"",
                            lvl.level_name                AS ""LevelName"",

                            s.scope_id                    AS ""ScopeId"",
                            s.scope_name                  AS ""ScopeName"",
                            s.value                       AS ""ScopeValue"",

                            sw.scope_name                 AS ""ParentScope""

                        FROM ""user"".user_master um

                        JOIN ""user"".user_has_application uha 
                            ON uha.user_id = um.id

                        JOIN ""master"".applications app
                            ON app.id = uha.app_id

                        JOIN ""user"".user_application_has_user_role uaur 
                            ON uaur.user_has_app_id = uha.id

                        JOIN ""user"".user_role_has_user_level urhul 
                            ON urhul.application_has_role_id = uaur.id

                        JOIN ""master"".roles r 
                            ON r.id = uaur.role_id

                        JOIN ""master"".application_level al
                            ON al.app_level_id = urhul.role_has_level_id
                           AND al.app_id = uha.app_id

                        JOIN ""master"".level_master lvl 
                            ON lvl.level_id = al.level_id

                        JOIN ""user"".user_level_has_user_scope ulhus 
                            ON ulhus.user_role_has_level_id = urhul.id
     
                        JOIN ""master"".application_scope asm
                            ON asm.app_scope_id = ulhus.user_level_has_scope_id

                        JOIN ""master"".scope_master s 
                            ON s.scope_id = asm.scope_id

                        LEFT JOIN ""master"".scope_relationships sr
                            ON sr.scope_id = asm.app_scope_id
                           AND sr.own_scope_level_id = urhul.role_has_level_id
     
                        LEFT JOIN ""master"".application_scope parent_asm
                            ON parent_asm.app_scope_id = sr.parent_scope_id

                        LEFT JOIN ""master"".scope_master sw
                        ON sw.scope_id = parent_asm.scope_id

                    WHERE 
                        um.user_name = @userId
                        AND uha.app_id = @appId
                ),

                privileges AS (
                    SELECT DISTINCT
                        ""Id"",
                        ""AppId"",
                        ""AppName"",
                        ""RoleId"",
                        ""RoleName"",
                        ""LevelId"",
                        ""LevelName""
                    FROM base
                )

                SELECT json_build_object(
                    'Id', MAX(b.""Id""),
                    'Name', MAX(b.""Name""),
                    'Email', MAX(b.""Email""),
                    'HrmsId', MAX(b.""HrmsId""),
                    'IsActive', bool_or(b.""IsActive""),
                    'UserName', MAX(b.""UserName""),
                    'ExpiresOn', MAX(b.""ExpiresOn""),
                    'Designation', MAX(b.""Designation""),
                    'MobileNumber', MAX(b.""MobileNumber""),
                    'EffectiveFrom', MAX(b.""EffectiveFrom""),

                    'Privileges',
                    json_agg(
                        json_build_object(
                            'AppId', p.""AppId"",
                            'AppName', p.""AppName"",
                            'RoleId', p.""RoleId"",
                            'RoleName', p.""RoleName"",
                            'LevelId', p.""LevelId"",
                            'LevelName', p.""LevelName"",
                            'IsAdmin', true,

                            'Scopes', (
                                SELECT json_agg(DISTINCT jsonb_build_object(
                                    'Id', b2.""ScopeId"",
                                    'Name', b2.""ScopeName"",
                                    'Value', b2.""ScopeValue"",
                                    'ParentScope', b2.""ParentScope""
                                ))
                                FROM base b2
                                WHERE 
                                    b2.""AppId"" = p.""AppId""
                                    AND b2.""RoleId"" = p.""RoleId""
                                    AND b2.""LevelId"" = p.""LevelId""
                            ),

                            'Permissions', json_build_array(
                                json_build_object(
                                    'Id', p.""RoleId"",
                                    'Name', p.""RoleName""
                                )
                            )
                        )
                    )
                ) AS result
                FROM base b
                JOIN privileges p ON b.""Id"" = p.""Id""
                GROUP BY b.""Id"";
                ", connection);

                    // 🔥 PARAMETERS
                    cmd.Parameters.AddWithValue("@userId", message.UserId);
                    cmd.Parameters.AddWithValue("@appId", appId);

                    var jsonResult = "{}";

                    await using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        jsonResult = reader["result"]?.ToString();
                    }

                    // 🔥 SEND TO QUEUE
                    var message_id = Guid.NewGuid();

                    _messageQueueRepository.Add(new MessageQueue
                    {
                        UniqueId = message_id,
                        QueueName = respondTo,
                        MessageBody = jsonResult,
                        CreatedAt = DateTime.Now
                    });

                    _messageQueueRepository.SaveChangesManaged();

                    await _mQueueProcessingService.ProcessQueueAsync(
                        respondTo, correlationId
                    );
                }










            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
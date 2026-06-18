using Microsoft.EntityFrameworkCore;
using Npgsql;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.Models.DTO;
using System.Data.Common;
using Dapper;

namespace UserManagement.DAL.Repositories
{
    //public class UserActivityLogRepository
    //    : Repository<UserActivityLog, UserManagementDBContext, UserSessionActivityAuditV, CTSDBContext>, IUserActivityLogRepository
    //{
    //    private readonly UserManagementDBContext _userManagementDBContext;
    //    private readonly CTSDBContext _ctsDBContext;

    //    public UserActivityLogRepository(UserManagementDBContext context, CTSDBContext cts_context) : base(context, cts_context)
    //    {
    //        _userManagementDBContext = context;
    //        _ctsDBContext = cts_context;
    //    }
    public class UserActivityLogRepository
    : Repository<UserActivityLog, UserManagementDBContext>, IUserActivityLogRepository
    {
        private readonly UserManagementDBContext _userManagementDBContext;
        private readonly CTSDBContext _ctsDBContext;

        public UserActivityLogRepository(
            UserManagementDBContext userManagementDbContext,
            CTSDBContext ctsDbContext
        ) : base(userManagementDbContext)
        {
            _userManagementDBContext = userManagementDbContext;
            _ctsDBContext = ctsDbContext;
        }



        public async Task<List<DeviceLoginCount>> GetDeviceLoginsAsync()
        {
            var result = new List<DeviceLoginCount>();

            var query = @"
                SELECT device, COUNT(*) AS total_logins
                FROM log.user_activity_log
                WHERE is_login = TRUE
                  AND device IS NOT NULL
                GROUP BY device
                ORDER BY total_logins DESC
            ";

            // Get the EF DbConnection
            await using DbConnection connection = _userManagementDBContext.Database.GetDbConnection();

            // Ensure it's NpgsqlConnection
            if (connection is not NpgsqlConnection npgsqlConnection)
                throw new InvalidOperationException("Expected NpgsqlConnection from DbContext");

            await npgsqlConnection.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, npgsqlConnection);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new DeviceLoginCount
                {
                    Device = reader["device"]?.ToString() ?? string.Empty,
                    TotalLogins = Convert.ToInt32(reader["total_logins"])
                });
            }

            return result;
        }
        //public async Task<List<UserActivityLogDTO>> GetDailyUserActivity(ActivityLogFilterDTO filter)
        //{
        //    var start = filter.StartDateTime ?? DateTime.Today;
        //    var end = filter.EndDateTime ?? DateTime.Today.AddDays(1).AddTicks(-1);

        //    if (_userManagementDBContext.Database.GetDbConnection() is not NpgsqlConnection)
        //        throw new InvalidOperationException("Expected NpgsqlConnection from DbContext");

        //    var whereClausesLogin = new List<string>
        //{
        //            "l.is_login = true",
        //            "l.activity_time BETWEEN @start AND @end"
        //        };

        //    var whereClausesLogout = new List<string>
        //        {
        //            "l.is_login = false",
        //            "l.activity_time BETWEEN @start AND @end"
        //        };

        //    var parameters = new DynamicParameters();
        //    parameters.Add("@start", start);
        //    parameters.Add("@end", end);

        //    if (!string.IsNullOrWhiteSpace(filter.Username))
        //    {
        //        whereClausesLogin.Add("LOWER(u.user_name) = LOWER(@username)");
        //        parameters.Add("@username", filter.Username);
        //    }

        //    if (!string.IsNullOrWhiteSpace(filter.Name))
        //    {
        //        whereClausesLogin.Add("LOWER(u.name) LIKE LOWER(@name)");
        //        parameters.Add("@name", $"%{filter.Name}%");
        //    }

        //    var whereSqlLogin = string.Join(" AND ", whereClausesLogin);
        //    var whereSqlLogout = string.Join(" AND ", whereClausesLogout);

        //    var sql = $@"
        //        WITH login_events AS (
        //            SELECT 
        //                l.user_id,
        //                u.user_name,
        //                u.name,
        //                l.activity_time AS login_time,
        //                ARRAY(
        //                    SELECT am.title
        //                    FROM master.applications am
        //                    WHERE am.id = ANY(l.applications)
        //                ) AS applications
        //            FROM log.user_activity_log l
        //            JOIN ""user"".user_master u 
        //                ON u.id = l.user_id
        //            WHERE {whereSqlLogin}
        //        ),
        //        logout_events AS (
        //            SELECT 
        //                l.user_id,
        //                l.activity_time AS logout_time,
        //                l.is_system_logout
        //            FROM log.user_activity_log l
        //            WHERE {whereSqlLogout}
        //        )
        //        SELECT 
        //            le.user_id,
        //            le.user_name AS UserName,
        //            le.name AS Name,
        //            le.login_time AS LoginTime,
        //            lo.logout_time AS LogoutTime,
        //            le.applications AS Application,
        //            lo.is_system_logout AS SystemLogout
        //        FROM login_events le
        //        LEFT JOIN logout_events lo
        //            ON lo.user_id = le.user_id
        //           AND lo.logout_time > le.login_time
        //        GROUP BY le.user_id, le.user_name, le.name, le.login_time, le.applications, lo.logout_time, lo.is_system_logout
        //        ORDER BY le.login_time DESC;
        //        ";

        //    await using var connection = _userManagementDBContext.Database.GetDbConnection();
        //    if (connection.State != System.Data.ConnectionState.Open)
        //        await connection.OpenAsync();

        //    var result = (await connection.QueryAsync<UserActivityLogDTO>(sql, parameters)).ToList();
        //    return result;
        //}


        public async Task<PaginatedResult<UserActivityLogDTO>> GetDailyUserActivity(ActivityLogFilterDTO filter)
        {
            var start = filter.StartDateTime ?? DateTime.Today;
            var end = filter.EndDateTime ?? DateTime.Today.AddDays(1).AddTicks(-1);

            int pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
            int pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

            var parameters = new DynamicParameters();
            parameters.Add("@start", start);
            parameters.Add("@end", end);
            parameters.Add("@pageSize", pageSize);
            parameters.Add("@offset", (pageNumber - 1) * pageSize);

            var whereClauses = new List<string>
        {
            "l.is_login = true",
            "l.activity_time BETWEEN @start AND @end"
        };

            if (!string.IsNullOrWhiteSpace(filter.Username))
            {
                whereClauses.Add("LOWER(u.user_name) = LOWER(@username)");
                parameters.Add("@username", filter.Username);
            }

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                whereClauses.Add("LOWER(u.name) LIKE LOWER(@name)");
                parameters.Add("@name", $"%{filter.Name}%");
            }

            var whereSql = string.Join(" AND ", whereClauses);

            var sql = $@"
            -- Count total rows
            WITH login_events AS (
                SELECT 
                    l.session_id,
                    l.user_id,
                    u.user_name,
                    u.name,
                    l.activity_time AS login_time,
                    ARRAY(
                        SELECT am.title
                        FROM master.applications am
                        WHERE am.id = ANY(l.applications)
                    ) AS applications
                FROM log.user_activity_log l
                JOIN ""user"".user_master u 
                    ON u.id = l.user_id
                WHERE {whereSql}
            )
            SELECT COUNT(*) FROM login_events;

            -- Paginated result
            WITH login_events AS (
                SELECT 
                    l.session_id,
                    l.user_id,
                    u.user_name,
                    u.name,
                    l.activity_time AS login_time,
                    ARRAY(
                        SELECT am.title
                        FROM master.applications am
                        WHERE am.id = ANY(l.applications)
                    ) AS applications
                FROM log.user_activity_log l
                JOIN ""user"".user_master u 
                    ON u.id = l.user_id
                WHERE {whereSql}
            )
            SELECT 
                le.session_id AS SessionId,
                le.user_id AS UserId,
                le.user_name AS UserName,
                le.name AS Name,
                le.login_time AS LoginTime,
                lo.logout_time AS LogoutTime,
                le.applications AS Application,
                lo.is_system_logout AS SystemLogout
            FROM login_events le
            LEFT JOIN LATERAL (
                SELECT l.activity_time AS logout_time,
                       l.is_system_logout
                FROM log.user_activity_log l
                WHERE l.is_login = false
                  AND l.user_id = le.user_id
                  AND l.activity_time > le.login_time
                ORDER BY l.activity_time ASC
                LIMIT 1
            ) lo ON true
            ORDER BY le.login_time DESC
            LIMIT @pageSize OFFSET @offset;
        ";

            await using var connection = _userManagementDBContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            using var multi = await connection.QueryMultipleAsync(sql, parameters);

            int totalRecords = multi.Read<int>().First();
            var data = multi.Read<UserActivityLogDTO>().ToList();

            return new PaginatedResult<UserActivityLogDTO>
            {
                Data = data,
                TotalRecords = totalRecords
            };
        }

        //public async Task<List<UserActivityLogDTO>> GetDailyUserActivity(ActivityLogFilterDTO filter)
        //{
        //    var start = filter.StartDateTime ?? DateTime.Today;
        //    var end = filter.EndDateTime ?? DateTime.Today.AddDays(1).AddTicks(-1);

        //    if (_userManagementDBContext.Database.GetDbConnection() is not NpgsqlConnection)
        //        throw new InvalidOperationException("Expected NpgsqlConnection from DbContext");

        //    var whereClausesLogin = new List<string>
        //    {
        //        "l.is_login = true",
        //        "l.activity_time BETWEEN @start AND @end"
        //    };

        //    var parameters = new DynamicParameters();
        //    parameters.Add("@start", start);
        //    parameters.Add("@end", end);

        //    if (!string.IsNullOrWhiteSpace(filter.Username))
        //    {
        //        whereClausesLogin.Add("LOWER(u.user_name) = LOWER(@username)");
        //        parameters.Add("@username", filter.Username);
        //    }

        //    if (!string.IsNullOrWhiteSpace(filter.Name))
        //    {
        //        whereClausesLogin.Add("LOWER(u.name) LIKE LOWER(@name)");
        //        parameters.Add("@name", $"%{filter.Name}%");
        //    }

        //    var whereSqlLogin = string.Join(" AND ", whereClausesLogin);

        //    var sql = $@"
        //        WITH login_events AS (
        //            SELECT 
        //                l.user_id,
        //                u.user_name,
        //                u.name,
        //                l.activity_time AS login_time,
        //                ARRAY(
        //                    SELECT am.title
        //                    FROM master.applications am
        //                    WHERE am.id = ANY(l.applications)
        //                ) AS applications
        //            FROM log.user_activity_log l
        //            JOIN ""user"".user_master u 
        //                ON u.id = l.user_id
        //            WHERE {whereSqlLogin}
        //        )
        //        SELECT 
        //            le.user_id,
        //            le.user_name AS UserName,
        //            le.name AS Name,
        //            le.login_time AS LoginTime,
        //            lo.logout_time AS LogoutTime,
        //            le.applications AS Application,
        //            lo.is_system_logout AS SystemLogout
        //        FROM login_events le
        //        LEFT JOIN LATERAL (
        //            SELECT l.activity_time AS logout_time,
        //                   l.is_system_logout
        //            FROM log.user_activity_log l
        //            WHERE l.is_login = false
        //              AND l.user_id = le.user_id
        //              AND l.activity_time > le.login_time
        //            ORDER BY l.activity_time ASC
        //            LIMIT 1
        //        ) lo ON true
        //        ORDER BY le.login_time DESC;
        //        ";

        //    await using var connection = _userManagementDBContext.Database.GetDbConnection();
        //    if (connection.State != System.Data.ConnectionState.Open)
        //        await connection.OpenAsync();

        //    var result = (await connection.QueryAsync<UserActivityLogDTO>(sql, parameters)).ToList();
        //    return result;
        //}


        public async Task<List<AgentLoginCount>> GetAgentLoginsAsync()
        {
            var result = new List<AgentLoginCount>();

            var query = @"
                SELECT agent, COUNT(*) AS total_logins
                FROM log.user_activity_log
                WHERE is_login = TRUE
                  AND agent IS NOT NULL
                GROUP BY agent
                ORDER BY total_logins DESC
            ";

            // Get the EF DbConnection
            await using DbConnection connection = _userManagementDBContext.Database.GetDbConnection();

            // Ensure it's NpgsqlConnection
            if (connection is not NpgsqlConnection npgsqlConnection)
                throw new InvalidOperationException("Expected NpgsqlConnection from DbContext");

            await npgsqlConnection.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, npgsqlConnection);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new AgentLoginCount
                {
                    Agent = reader["agent"]?.ToString() ?? string.Empty,
                    TotalLogins = Convert.ToInt32(reader["total_logins"])
                });
            }

            return result;
        }
        public async Task<List<UserLoginCountDTO>> GetCurrentLoggedInUserCount()
        {
            var result = new List<UserLoginCountDTO>();

            var query = @"
                   WITH time_slots AS (
                 SELECT generate_series(
                     date_trunc('day', now()::timestamp), 
                     date_trunc('day', now()::timestamp) + interval '1 day' - interval '30 min',
                     interval '30 min'
                 ) AS slot_time
             ),
             login_sessions AS (
                 SELECT
                     l.id AS session_id,
                     l.user_id,
                     l.activity_time AS login_time,
                     COALESCE(
                         (SELECT MIN(lo.activity_time)
                          FROM log.user_activity_log lo
                          WHERE lo.user_id = l.user_id
                            AND lo.is_login = false
                            AND lo.activity_time > l.activity_time),
                         now()
                     ) AS logout_time
                 FROM log.user_activity_log l
                 WHERE l.is_login = true
                   AND l.activity_time::date = current_date
             )
             SELECT
                 t.slot_time,
                 COUNT(s.session_id) AS active_sessions
             FROM time_slots t
             LEFT JOIN login_sessions s
                 ON t.slot_time >= s.login_time
                AND t.slot_time < s.logout_time
             WHERE t.slot_time <= now()::timestamp  -- only up to current time
             GROUP BY t.slot_time
             ORDER BY t.slot_time;
    ";

            // Get the EF DbConnection
            await using DbConnection connection = _userManagementDBContext.Database.GetDbConnection();

            if (connection is not NpgsqlConnection npgsqlConnection)
                throw new InvalidOperationException("Expected NpgsqlConnection from DbContext");

            await npgsqlConnection.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, npgsqlConnection);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new UserLoginCountDTO
                {
                    LoginTime = ((DateTime)reader["slot_time"]).ToString("yyyy-MM-ddTHH:mm:ss"),
                    UserLoginCount = Convert.ToInt32(reader["active_sessions"])
                });
            }

            return result;
        }




        public async Task<ActivityPageResponse> GetPagedActivities(ActivityLogRequestDto request)
        {
            var query = _ctsDBContext.UserSessionActivityAuditView.AsQueryable();

            // Filters
            if (request.UserId.HasValue)
                query = query.Where(x => x.UserId == request.UserId.Value.ToString());

            //if (request.SessionId.HasValue)
                query = query.Where(x => x.SessionId.ToString() == request.SessionId.Value.ToString());

            int totalRecords = await query.CountAsync();

            // Pagination + Sorting
            var logs = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => x.Activity) 
                .ToListAsync();


            return new ActivityPageResponse
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = totalRecords,
                Activities = logs
            };
        
    }


}
}
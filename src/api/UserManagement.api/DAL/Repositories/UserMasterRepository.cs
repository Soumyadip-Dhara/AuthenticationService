using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Numerics;
using System.Text.Json;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.Models.DTO;
using static UserManagement.Models.Claims.ClaimModel;

namespace UserManagement.DAL.Repositories
{
    public class UserMasterRepository(
        UserManagementDBContext context,
        IClaimService claim,
        IConfiguration config

    ) : Repository<UserMaster, UserManagementDBContext>(context), IUserMasterRepository
    {
        protected readonly UserManagementDBContext _userManagementDBContext = context;
        private readonly IClaimService _auth = claim;
        private readonly KeyValue storage = new();
        private readonly string _connString = config.GetConnectionString("UserManagementDBConnection");

        private class KeyValue
        {
            private static readonly Dictionary<string, (string?, string?)> _kv = [];

            public static void SetKV(string key, (string, string) value)
            {
                _kv[key] = value;
            }
            public static (string?, string?) GetValue(string key)
            {
                if (_kv.ContainsKey(key))
                {
                    (string?, string?) value = _kv[key];
                    _kv.Remove(key);
                    return value;
                }
                return (null, null);
            }
        }

        public async Task<(bool, string, long)> UserRegistration(UserRegistrationNewDTO user, string password, byte[] passwordHash, byte[] passwordSalt)
        {
            var parameters = new[]
            {
                new NpgsqlParameter("_username", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.UserName },
                new NpgsqlParameter("_hrmsid", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.HrmsId },
                new NpgsqlParameter("_name", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.Name },
                new NpgsqlParameter("_passwordhash", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = passwordHash },
                new NpgsqlParameter("_passwordsalt", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = passwordSalt },
                new NpgsqlParameter("_designation", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.Designation },
                new NpgsqlParameter("_mobilenumber", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.MobileNumber },
                new NpgsqlParameter("_email", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.Email },
                new NpgsqlParameter("_createdby", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = _auth.GetUserId() },
                new NpgsqlParameter("_useraccess", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(user.UserAccess) },
                new NpgsqlParameter("_is_done_out", NpgsqlTypes.NpgsqlDbType.Boolean) { Direction = ParameterDirection.InputOutput, Value = false },
                new NpgsqlParameter("_message_out", NpgsqlTypes.NpgsqlDbType.Text) { Direction = ParameterDirection.InputOutput, Value = "" },
                new NpgsqlParameter("_user_id", NpgsqlTypes.NpgsqlDbType.Bigint) { Direction = ParameterDirection.InputOutput, Value = -1 }
            };

            var commandText = "CALL \"user\".user_registration_by_user_admin( @_username, @_hrmsid, @_name, @_passwordhash, @_passwordsalt, @_designation, @_mobilenumber, @_email, @_createdby, @_useraccess, @_is_done_out, @_message_out, @_user_id )";

            await _userManagementDBContext.Database.ExecuteSqlRawAsync(commandText, parameters);

            bool isSuccess = (bool)parameters[10].Value;
            string message = parameters[11].Value as string;
            long user_id = (long)parameters[12].Value;

            return (isSuccess, message, user_id);
        }
        public async Task<(bool, string, long)> NewUserRegistrationBySuperAdmin(UserRegistrationNewDTO user, string password, byte[] passwordHash, byte[] passwordSalt, bool isSuperAdminCreation)
        {
            var parameters = new[]
            {
                new NpgsqlParameter("_username", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.UserName },
                new NpgsqlParameter("_hrmsid", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.HrmsId },
                new NpgsqlParameter("_name", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.Name },
                new NpgsqlParameter("_passwordhash", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = passwordHash },
                new NpgsqlParameter("_passwordsalt", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = passwordSalt },
                new NpgsqlParameter("_designation", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.Designation },
                new NpgsqlParameter("_mobilenumber", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.MobileNumber },
                new NpgsqlParameter("_email", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = user.UserMaster.Email },
                new NpgsqlParameter("_createdby", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = _auth.GetUserId() },

                new NpgsqlParameter("_um", NpgsqlTypes.NpgsqlDbType.Boolean) { Value = (bool)(user.UMApps.Count() > 0) },
                new NpgsqlParameter("_mm", NpgsqlTypes.NpgsqlDbType.Boolean) { Value = (bool)(user.MMApps.Count() > 0) },
                new NpgsqlParameter("_access", NpgsqlTypes.NpgsqlDbType.Boolean) { Value = (bool)(user.UserAccess.Count() > 0) },

                new NpgsqlParameter("_um_app_list", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(user.UMApps) },
                new NpgsqlParameter("_mm_app_list", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(user.MMApps) },
                new NpgsqlParameter("_useraccess", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(user.UserAccess) },

                new NpgsqlParameter("_is_done_out", NpgsqlTypes.NpgsqlDbType.Boolean) { Direction = ParameterDirection.InputOutput, Value = false },
                new NpgsqlParameter("_message_out", NpgsqlTypes.NpgsqlDbType.Text) { Direction = ParameterDirection.InputOutput, Value = "" },
                new NpgsqlParameter("_user_id", NpgsqlTypes.NpgsqlDbType.Bigint) { Direction = ParameterDirection.InputOutput, Value = -1 }
            };
            var commandText = "";
            if (isSuperAdminCreation)
            {
                commandText = "CALL \"user\".super_admin_registration_by_super_admin(@_username, @_hrmsid, @_name, @_passwordhash, @_passwordsalt, @_designation, @_mobilenumber, @_email, @_createdby, @_um, @_mm, @_access, @_um_app_list, @_mm_app_list, @_useraccess, @_is_done_out, @_message_out, @_user_id)";
            }
            else
            {
                commandText = "CALL \"user\".user_registration_by_super_admin(@_username, @_hrmsid, @_name, @_passwordhash, @_passwordsalt, @_designation, @_mobilenumber, @_email, @_createdby, @_um, @_mm, @_access, @_um_app_list, @_mm_app_list, @_useraccess, @_is_done_out, @_message_out, @_user_id)";

            }
            await _userManagementDBContext.Database.ExecuteSqlRawAsync(commandText, parameters);

            bool isSuccess = (bool)parameters[15].Value;
            string message = parameters[16].Value as string;
            long user_id = (long)parameters[17].Value;

            return (isSuccess, message, user_id);
        }

        public (string?, string?) GetJWTFromAccessToken(string guid)
        {
            (string?, string?) value = KeyValue.GetValue(guid);
            return value;
        }

        public void SetJWTAccessToken(string accessToken, (string, string) data)
        {
            KeyValue.SetKV(accessToken, data);
        }

        public async Task<List<UserAccessDTO>> GetUserPrivilegesAsync(long userId = 14)
        {
            var userAccessList = new List<UserAccessDTO>();

            try
            {

                string query = @"SELECT json_build_object(
                    'data', json_build_object(
                        'application', json_build_object('id', a.id, 'title', a.title),
                        'role', json_build_object('id', r.id, 'title', r.title),
                        'level', json_build_object('id', l.app_level_id, 'title', ml.level_name),
                        'permissions', (
                            SELECT json_agg(
                                DISTINCT jsonb_build_object('id', p.id, 'name', p.name)
                            )
                        ),
                        'scopes', (
                            SELECT json_agg(
                                DISTINCT jsonb_build_object('value', ms.value, 'name', ms.scope_name, 'id', s.app_scope_id)
                            )
                        )
                    )
                ) as result
                FROM master.applications a
                JOIN ""user"".user_has_application uha ON a.id = uha.app_id
                JOIN ""user"".user_application_has_user_role uar ON uha.id = uar.user_has_app_id
                JOIN master.roles r ON r.id = uar.role_id
                JOIN ""user"".user_role_has_user_permission urp ON uar.id = urp.application_has_role_id
                JOIN master.permissions p ON p.id = urp.role_has_permission_id
                JOIN ""user"".user_role_has_user_level url ON uar.id = url.application_has_role_id
                JOIN master.application_level l ON l.app_level_id = url.role_has_level_id
					        JOIN master.level_master ml ON ml.level_id = l.level_id
                JOIN ""user"".user_level_has_user_scope uls ON url.id = uls.user_role_has_level_id
                JOIN master.application_scope s ON s.app_scope_id = uls.user_level_has_scope_id
					        JOIN master.scope_master ms ON ms.scope_id = s.scope_id
                WHERE uha.user_id = @userId
                GROUP BY a.id, r.id, l.app_level_id, ml.level_name;";


                using (var command = _userManagementDBContext.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandType = System.Data.CommandType.Text;

                    var userIdParam = command.CreateParameter();
                    userIdParam.ParameterName = "@userId";
                    userIdParam.Value = userId;
                    userIdParam.DbType = System.Data.DbType.Int32;
                    command.Parameters.Add(userIdParam);

                    if (command.Connection.State != System.Data.ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync();
                    }

                    using (var result = await command.ExecuteReaderAsync())
                    {
                        var id = userId;
                        while (await result.ReadAsync())
                        {
                            var jsonData = result["result"].ToString();
                            var parsedData = JsonSerializer.Deserialize<UserAccessDTO>(jsonData);
                            parsedData.Id = id++;
                            userAccessList.Add(parsedData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return userAccessList;
        }

        public async Task<(bool, string)> ChangePassword(long userId, byte[] passwordHash, byte[] passwordSalt)
        {
            var parameters = new[]
            {
                new NpgsqlParameter("_user_id", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = userId },
                new NpgsqlParameter("_passwordhash", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = passwordHash },
                new NpgsqlParameter("_passwordsalt", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = passwordSalt },
                new NpgsqlParameter("_is_done_out", NpgsqlTypes.NpgsqlDbType.Boolean) { Direction = ParameterDirection.InputOutput, Value = false },
                new NpgsqlParameter("_message_out", NpgsqlTypes.NpgsqlDbType.Text) { Direction = ParameterDirection.InputOutput, Value = "" }
            };

            var commandText = "CALL \"user\".password_change( @_user_id, @_passwordhash, @_passwordsalt, @_is_done_out, @_message_out)";

            await _userManagementDBContext.Database.ExecuteSqlRawAsync(commandText, parameters);

            bool isSuccess = (bool)parameters[3].Value;
            string message = parameters[4].Value as string;

            return (isSuccess, message);
        }

        public async Task<UserDetailsForDisplayWithCountDTO> UserDetailsForDisplay(FilterData payload, string userRoles, long userId)
        {
            try
            {
                var res = new UserDetailsForDisplayWithCountDTO();
                var parameters = new[]
                {
                new NpgsqlParameter("p_first", NpgsqlTypes.NpgsqlDbType.Integer) { Value = payload.First },
                new NpgsqlParameter("p_rows", NpgsqlTypes.NpgsqlDbType.Integer) { Value = payload.Rows },
                new NpgsqlParameter("user_role", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = userRoles },
                new NpgsqlParameter("user_id", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = userId },
                new NpgsqlParameter("filters", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = payload.Filters != null ? JsonSerializer.Serialize(payload.Filters) : DBNull.Value },
                new NpgsqlParameter("scope_value", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = payload.ScopeValue },
                new NpgsqlParameter("global_filter", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = payload.GlobalFilter },
                new NpgsqlParameter("user_details", NpgsqlTypes.NpgsqlDbType.Jsonb) { Direction = ParameterDirection.InputOutput, Value = DBNull.Value },
                new NpgsqlParameter("total_users_count", NpgsqlTypes.NpgsqlDbType.Bigint) { Direction = ParameterDirection.InputOutput, Value = 0 },
                new NpgsqlParameter("active_users_count", NpgsqlTypes.NpgsqlDbType.Bigint) { Direction = ParameterDirection.InputOutput, Value = 0 },
                new NpgsqlParameter("inactive_users_count", NpgsqlTypes.NpgsqlDbType.Bigint) { Direction = ParameterDirection.InputOutput, Value = 0 },
                //new NpgsqlParameter("newly_registered_users", NpgsqlTypes.NpgsqlDbType.Bigint) { Direction = ParameterDirection.InputOutput, Value = 0 }
            };

                var commandText = @"CALL ""user"".get_users(@p_first, @p_rows, @user_role, @user_id, @filters, @scope_value, @global_filter, @user_details, @total_users_count, @active_users_count, @inactive_users_count)";

                await _userManagementDBContext.Database.ExecuteSqlRawAsync(commandText, parameters);

                var userDetailsJson = parameters[7].Value;
                //Console.WriteLine(JsonSerializer.Deserialize < List < UserDetailsForDisplayDTO >> (userDetailsJson.ToString()).GetType());
                var userDetails = string.IsNullOrEmpty(userDetailsJson.ToString())
                    ? new List<UserDetailsForDisplayDTOForDeserialize>()
                    : JsonSerializer.Deserialize<List<UserDetailsForDisplayDTOForDeserialize>>(userDetailsJson.ToString());

                res.ActiveUsers = Convert.ToInt32(parameters[9].Value);
                res.InActiveUsers = Convert.ToInt32(parameters[10].Value);
                res.Count = Convert.ToInt32(parameters[8].Value);
                if (userDetails != null)
                {
                    res.Users = userDetails;
                }

                return res;

            }

            catch (Exception ex)
            {
                return new UserDetailsForDisplayWithCountDTO();
            }
        }
        public async Task<(bool, string)> ModifyUserPrivileges(
            UserPrivilegeUpdateDTO userPrivilegeUpdate
        )
        {
            var parameters = new[]
            {
                new NpgsqlParameter("_user_id", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = userPrivilegeUpdate.userId },
                new NpgsqlParameter("um_apps", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(userPrivilegeUpdate.UMApps) },
                new NpgsqlParameter("mm_apps", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(userPrivilegeUpdate.MMApps) },
                new NpgsqlParameter("user_privileges", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(userPrivilegeUpdate.UserPrivileges) },
                new NpgsqlParameter("response", NpgsqlTypes.NpgsqlDbType.Boolean) { Direction = ParameterDirection.InputOutput, Value = false },
                new NpgsqlParameter("message", NpgsqlTypes.NpgsqlDbType.Text) { Direction = ParameterDirection.InputOutput, Value = "" }
            };

            var commandText = "CALL \"user\".user_privilege_modification(@_user_id, @um_apps, @mm_apps, @user_privileges, @response, @message)";

            await _userManagementDBContext.Database.ExecuteSqlRawAsync(commandText, parameters);

            bool isSuccess = (bool)parameters[4].Value;
            string message = parameters[5].Value as string;

            return (isSuccess, message);
        }
        public async Task<object> GetDataForAdminManagement(string scope, int levelId)
        {
            try
            {
                var parameters = new[]
                {
                    new NpgsqlParameter("scope", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = scope },
                    new NpgsqlParameter("levelId", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = levelId }
                };

                await using var connection = _userManagementDBContext.Database.GetDbConnection();
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = """
                    WITH userData AS (
                    SELECT json_build_object(
                        'user_id', json_build_object(
                            'value', um.id,
                            'display_name', 'User ID',
                            'is_hidden', false
                        ),
                        'user_name', json_build_object(
                            'value', um.user_name,
                            'display_name', 'Username',
                            'is_hidden', false
                        ),
                        'name', json_build_object(
                            'value', um.name,
                            'display_name', 'Full Name',
                            'is_hidden', false
                        ),
                        'role', json_build_object(
                            'value', mr.title,
                            'display_name', 'Role',
                            'is_hidden', false
                        ),
                        'is_active', json_build_object(
                            'value', um.is_active,
                            'display_name', 'Is Active',
                            'is_hidden', false
                        ),
                        'is_blocked', json_build_object(
                            'value', um.is_blocked,
                            'display_name', 'Is Blocked',
                            'is_hidden', false
                        ),
                        'is_admin', json_build_object(
                            'value',
                            (
                                SELECT EXISTS (
                                    SELECT 1
                                    FROM "user".user_has_user_management uhum
                                    WHERE uhum.user_id = um.id
                                      AND uhum.assigned_app_id = uha.app_id
                                )
                            ),
                            'display_name', 'Is Admin',
                            'is_hidden', true
                        ),
                        'scope_id', json_build_object(
                            'value', aps.app_scope_id,
                            'display_name', 'Scope ID',
                            'is_hidden', true
                        ),
                        'scope_value', json_build_object(
                            'value', sm.value,
                            'display_name', 'Scope Value',
                            'is_hidden', true
                        ),
                        'is_parent_or_admin_role', json_build_object(
                            'value', lhar.is_parent_or_admin_role,
                            'display_name', 'Is Parent/Admin Role',
                            'is_hidden', true
                        )
                    ) AS userRow
                    FROM "user".user_master um
                    JOIN "user".user_has_application uha ON uha.user_id = um.id
                    JOIN "user".user_application_has_user_role uaur ON uha.id = uaur.user_has_app_id
                    JOIN master.roles mr ON mr.id = uaur.role_id
                    JOIN "user".user_role_has_user_level urhul  ON uaur.id = urhul.application_has_role_id
                    JOIN master.application_level al ON al.app_level_id = urhul.role_has_level_id
                    JOIN "user".user_level_has_user_scope ulhus ON urhul.id = ulhus.user_role_has_level_id
                    JOIN master.application_scope aps ON ulhus.user_level_has_scope_id = aps.app_scope_id
                    JOIN master.scope_master sm ON aps.scope_id = sm.scope_id
                    JOIN master.level_has_allowed_roles lhar  ON al.app_level_id = lhar.level_id  AND mr.id = lhar.role_id
                    WHERE TRIM(sm.value) ILIKE @scope
                      AND al.app_level_id = @levelId
                ),

                isSingleAdmin AS (
                    SELECT a._is_multi_admin_disallowed
                    FROM master.applications a
                    JOIN master.application_level al
                        ON a.id = al.app_id
                    WHERE al.app_level_id = @levelId
                )

                SELECT json_build_object(
                    'isSingleAdmin',
                        COALESCE(
                            (SELECT _is_multi_admin_disallowed FROM isSingleAdmin),
                            false
                        ),
                    'userData',
                        COALESCE(
                            (SELECT json_agg(userRow) FROM userData),
                            '[]'::json
                        )
                ) AS result;
                """;

                var scopeParam = command.CreateParameter();
                scopeParam.ParameterName = "scope";
                scopeParam.DbType = DbType.String;
                scopeParam.Value = scope;
                command.Parameters.Add(scopeParam);

                var levelIdParam = command.CreateParameter();
                levelIdParam.ParameterName = "levelId";
                levelIdParam.DbType = DbType.Int64;
                levelIdParam.Value = levelId;
                command.Parameters.Add(levelIdParam);

                var result = await command.ExecuteScalarAsync();
                return result;
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
    }
        public async Task<(bool, string)> ManageAdmin(List<long> userIds, long scopeId, int appId, bool isSingleAdmin)
        {
            if (isSingleAdmin && userIds.Count() > 1)
            {
                return (false, "Only Single Admin Allowed in this module.");
            }
            if (userIds?.Count() < 1)
            {
                return (false, "Please Choose Admin for this module.");
            }
            try
            {
                await using var connection = _userManagementDBContext.Database.GetDbConnection();
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = """
                       WITH user_ids AS (
                        SELECT DISTINCT um.id
                        FROM "user".user_master um
                        JOIN "user".user_has_application uha
                            ON um.id = uha.user_id

                        JOIN "user".user_application_has_user_role uaur
                            ON uha.id = uaur.user_has_app_id

                        JOIN master.roles mr
                            ON uaur.role_id = mr.id

                        JOIN "user".user_role_has_user_level urhul
                            ON uaur.id = urhul.application_has_role_id

                        JOIN master.application_level al
                            ON urhul.role_has_level_id = al.app_level_id

                        JOIN "user".user_level_has_user_scope ulhus
                            ON urhul.id = ulhus.user_role_has_level_id

                        JOIN master.application_scope aps
                            ON ulhus.user_level_has_scope_id = aps.app_scope_id

                        JOIN master.level_has_allowed_roles lhar
                            ON al.app_level_id = lhar.level_id
                           AND mr.id = lhar.role_id

                          WHERE aps.app_scope_id = @scopeId


                    ),

                    remove_admin_mapping AS (
                        DELETE FROM "user".user_has_user_management
                        WHERE user_id IN (SELECT id FROM user_ids)
                          AND assigned_app_id = @appId
                        RETURNING *
                    ),

                    remove_um_app AS (
                        DELETE FROM "user".user_has_application
                        WHERE user_id IN (SELECT id FROM user_ids)
                          AND app_id = 1
                        RETURNING *
                    ),

                    update_scope_false AS (
                        UPDATE master.application_scope
                        SET is_admin_created = FALSE
                        WHERE app_scope_id = @scopeId
                        RETURNING *
                    ),

                    um_application_ids AS (
                        INSERT INTO "user".user_has_application
                            (user_id, app_id)
                        SELECT unnest(@userIds), 1::INT
                        ON CONFLICT(user_id, app_id)
                        DO UPDATE
                           SET id = "user".user_has_application.id
                        RETURNING id AS user_has_app_id
                    ),

                    um_user_role AS (
                        INSERT INTO "user".user_application_has_user_role
                            (user_has_app_id, role_id, app_id)
                        SELECT user_has_app_id, 115, 1
                        FROM um_application_ids
                        ON CONFLICT(user_has_app_id, role_id)
                        DO UPDATE
                           SET id = "user".user_application_has_user_role.id
                        RETURNING
                            id AS application_has_role_id,
                            user_has_app_id
                    ),

                    user_permissions AS (
                        INSERT INTO "user".user_role_has_user_permission
                            (application_has_role_id, role_has_permission_id)
                        SELECT application_has_role_id, 16
                        FROM um_user_role
                        ON CONFLICT(application_has_role_id, role_has_permission_id)
                        DO NOTHING
                        RETURNING *
                    ),

                    user_levels AS (
                        INSERT INTO "user".user_role_has_user_level
                            (application_has_role_id,
                             role_has_level_id,
                             user_has_app_id)
                        SELECT
                            application_has_role_id,
                            45,
                            user_has_app_id
                        FROM um_user_role
                        ON CONFLICT(
                            application_has_role_id,
                            role_has_level_id,
                            user_has_app_id
                        )
                        DO UPDATE
                           SET id = "user".user_role_has_user_level.id
                        RETURNING id AS user_role_has_level_id
                    ),

                    user_scope_map AS (
                        INSERT INTO "user".user_level_has_user_scope
                            (
                                user_role_has_level_id,
                                user_level_has_scope_id,
                                level_id
                            )
                        SELECT
                            user_role_has_level_id,
                            1038,
                            45
                        FROM user_levels
                        ON CONFLICT(
                            user_role_has_level_id,
                            user_level_has_scope_id,
                            level_id
                        )
                        DO NOTHING
                        RETURNING *
                    ),

                    user_has_user_management AS (
                        INSERT INTO "user".user_has_user_management
                            (
                                user_id,
                                assigned_app_id,
                                assigned_um_role_id
                            )
                        SELECT
                            unnest(@userIds),
                            @appId::INT,
                            115
                        ON CONFLICT(user_id, assigned_app_id)
                        DO NOTHING
                        RETURNING *
                    ),

                    update_scope_true AS (
                        UPDATE master.application_scope
                        SET is_admin_created = TRUE
                        WHERE app_scope_id = @scopeId
                        RETURNING *
                    )

                    SELECT COUNT(*)
                    FROM um_application_ids;
                    
                
                    """;

                var scopeParam = new NpgsqlParameter("scopeId", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = scopeId };
                var appIdParam = new NpgsqlParameter("appId", NpgsqlTypes.NpgsqlDbType.Integer) { Value = appId };
                var userIdsParam = new NpgsqlParameter("userIds", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Bigint) { Value = userIds.ToArray() };

                command.Parameters.Add(appIdParam);
                command.Parameters.Add(scopeParam);
                command.Parameters.Add(userIdsParam);


                long result = (long)await command.ExecuteScalarAsync();
                if (result != (long)userIds.Count())
                {
                    Console.Error.WriteLine($"Error:: Admin Creation Failed:: userIds: {string.Join(",", userIds)}, scopeId: {scopeId}, appId: {appId}, isSingleAdmin: {isSingleAdmin}");
                    return (false, "Something went wrong while managing admin.");
                }
                return (true, "Admin Assigned Successfully");
            }
            catch (PostgresException pgEx)
            {
                string errorMessage = pgEx.SqlState switch
                {
                    "23505" => "Duplicate entry detected (unique constraint violation).",
                    "23503" => "Related entity not found (foreign key constraint violation).",
                    "23502" => "Missing required field (NOT NULL constraint).",
                    _ => "PostgreSQL error: " + pgEx.MessageText
                };

                return (false, errorMessage);
            }
            catch (DbException dbEx)
            {
                return (false, "Database error: " + dbEx.Message);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }


        public async Task<DashboardSummaryDTO> GetDashboardSummaryAsync()
        {
            try
            {
                await using var connection = _userManagementDBContext.Database.GetDbConnection();
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = """
                SELECT json_build_object(
                    'total_user', (SELECT COUNT(1) FROM "user".user_master),
                    'active_total_user', (SELECT COUNT(1) FROM "user".user_master WHERE is_active),
                    'new_users_added_in_month', (
                        SELECT COUNT(1)
                        FROM "user".user_master
                        WHERE created_at::date > NOW()::date - INTERVAL '30 days'
                    ),
                    'average_engagement_time_in_minute', (
                        SELECT ROUND(AVG(EXTRACT(EPOCH FROM (next_activity_time - login_time)) / 60)::numeric, 2)
                        FROM (
                            SELECT
                                user_id,
                                activity_time AS login_time,
                                LEAD(activity_time) OVER (PARTITION BY user_id ORDER BY activity_time) AS next_activity_time,
                                is_login,
                                LEAD(is_login) OVER (PARTITION BY user_id ORDER BY activity_time) AS next_is_login
                            FROM log.user_activity_log
                        ) session_data
                        WHERE is_login = TRUE AND next_is_login = FALSE
                    ),
                    'total_modules', (SELECT COUNT(1) FROM master.applications),
                    'active_modules', (SELECT COUNT(1) FROM master.applications WHERE is_active),
                    'umder maintainance_modules', (SELECT COUNT(1) FROM master.applications WHERE is_under_maintenance),
                    'total_admins_in_the_system', (SELECT COUNT(DISTINCT(user_id)) FROM "user".user_has_user_management)
                );
                """;

                var jsonResult = await command.ExecuteScalarAsync();

                if (jsonResult == null || jsonResult == DBNull.Value)
                    return new DashboardSummaryDTO();
                return JsonSerializer.Deserialize<DashboardSummaryDTO>(jsonResult.ToString()!)!;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve dashboard summary", ex);
            }
        }
        public async Task<string> GetAdminsForScope(string scopeValue, int levelId, int appId)
        {
            try
            {
                //await using var connection = _userManagementDBContext.Database.GetDbConnection();
                //await connection.OpenAsync();
                await using var connection = new NpgsqlConnection(_connString);
                await connection.OpenAsync();


                await using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT DISTINCT um.user_name
                    FROM "user".user_master AS um
                    JOIN "user".user_has_application AS uha ON um.id = uha.user_id
                    JOIN "user".user_has_user_management AS uhum ON uha.user_id = uhum.user_id
                    JOIN "user".user_application_has_user_role AS uahr ON uha.id = uahr.user_has_app_id
                    JOIN "user".user_role_has_user_level AS urhul ON uha.id = urhul.user_has_app_id
                    JOIN master.application_level AS al ON urhul.role_has_level_id = al.app_level_id
                    JOIN master.level_master AS lm ON al.level_id = lm.level_id
                    JOIN "user".user_level_has_user_scope AS ulhus ON urhul.id = ulhus.user_role_has_level_id
                    JOIN master.application_scope aps ON ulhus.user_level_has_scope_id = aps.app_scope_id
                    JOIN master.scope_master sm On sm.scope_id = aps.scope_id
                    WHERE
                        TRIM(sm.value) = @scopeValue
                        AND aps.level_id = @levelId
                        AND um.is_active = TRUE
                        AND uha.app_id = @appId;
                """;

                // scopeValue
                var scopeParam = command.CreateParameter();
                scopeParam.ParameterName = "scopeValue";
                scopeParam.DbType = DbType.String;
                scopeParam.Value = scopeValue;
                command.Parameters.Add(scopeParam);

                // levelId
                var levelParam = command.CreateParameter();
                levelParam.ParameterName = "levelId";
                levelParam.DbType = DbType.Int64;
                levelParam.Value = levelId;
                command.Parameters.Add(levelParam);

                // appId
                var appParam = command.CreateParameter();
                appParam.ParameterName = "appId";
                appParam.DbType = DbType.Int32;
                appParam.Value = appId;
                command.Parameters.Add(appParam);

                var result = new List<string>();

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(reader.GetString(0));
                }
                if (result.Count > 0)
                {
                    return $"Admins found: {string.Join(", ", result)}";
                }

                return "No admin users found.";
            }
            catch(Exception ex)
            {
                return $"Error: {ex.Message}";
 ;
            }
        }



        public async Task<List<UserProfileQueryModel>> GetUserProfileData(string userName)
        {
            try
            {
                string query = @"
            SELECT lm.level_name AS Level, um.id, um.user_name AS UserName, um.name, um.designation, 
                um.mobile_number AS MobileNumber, um.email, COALESCE(um.signer_id, '') AS SignerID
                FROM ""user"".user_master um
	            JOIN ""user"".user_has_application uha ON uha.user_id = um.id
                JOIN ""user"".user_application_has_user_role uaur ON uha.id = uaur.user_has_app_id
                JOIN ""user"".user_role_has_user_level urhul ON uaur.id = urhul.application_has_role_id
	            JOIN ""master"".application_level apl ON urhul.role_has_level_id = apl.app_level_id 
                JOIN ""master"".level_master lm ON lm.level_id = apl.level_id
            WHERE um.user_name = {0}";

                var a = await _userManagementDBContext.Set<UserProfileQueryModel>()
                                .FromSqlRaw(query, userName)
                                .ToListAsync();
                return a;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        
        public async Task<bool> UpdateSignerIdByUserNameAsync(string userName, string signerId)
        {
            const string query = @"
            UPDATE ""user"".user_master
            SET signer_id = @SignerId,
                updated_at = NOW()
            WHERE user_name = @UserName
              AND is_active = true
              AND is_blocked = false";

            var parameters = new
            {
                SignerId = signerId,
                UserName = userName
            };

            using NpgsqlConnection connection = new(_connString);
            int rowsAffected = await connection.ExecuteAsync(query, parameters);
            return rowsAffected > 0;
        }

    }
}

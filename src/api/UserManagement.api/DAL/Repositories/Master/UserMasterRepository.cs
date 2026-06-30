using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Repositories.Master
{
    public class UserMasterRepository : Repository<UserMaster, UserManagementDBContext>, IUserMasterRepository
    {
        public UserMasterRepository(UserManagementDBContext context) : base(context)
        {
        }

        public async Task<string> GetAdminsForScope(string scopeValue, int levelId, int appId)
        {
            try
            {
                var connection = this.UMDbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT DISTINCT um.user_name
                        FROM ""user"".user_master AS um
                        JOIN ""user"".user_has_application AS uha ON um.id = uha.user_id
                        JOIN ""user"".user_has_user_management AS uhum ON uha.user_id = uhum.user_id
                        JOIN ""user"".user_application_has_user_role AS uahr ON uha.id = uahr.user_has_app_id
                        JOIN ""user"".user_role_has_user_level AS urhul ON uha.id = urhul.user_has_app_id
                        JOIN master.application_level AS al ON urhul.role_has_level_id = al.app_level_id
                        JOIN master.level_master AS lm ON al.level_id = lm.level_id
                        JOIN ""user"".user_level_has_user_scope AS ulhus ON urhul.id = ulhus.user_role_has_level_id
                        JOIN master.application_scope aps ON ulhus.user_level_has_scope_id = aps.app_scope_id
                        JOIN master.scope_master sm On sm.scope_id = aps.scope_id
                        WHERE
                            TRIM(sm.value) = @scopeValue
                            AND aps.level_id = @levelId
                            AND um.is_active = TRUE
                            AND uha.app_id = @appId;";

                    var scopeParam = new NpgsqlParameter("scopeValue", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = scopeValue };
                    var levelParam = new NpgsqlParameter("levelId", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = (long)levelId };
                    var appParam = new NpgsqlParameter("appId", NpgsqlTypes.NpgsqlDbType.Integer) { Value = appId };

                    command.Parameters.Add(scopeParam);
                    command.Parameters.Add(levelParam);
                    command.Parameters.Add(appParam);

                    var result = new List<string>();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(reader.GetString(0));
                        }
                    }

                    if (result.Count > 0)
                    {
                        return $"Admins found: {string.Join(", ", result)}";
                    }

                    return "No admin users found.";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<List<UserAccessDTO>> GetUserPrivilegesAsync(long userId)
        {
            var userAccessList = new List<UserAccessDTO>();
            try
            {
                var connection = this.UMDbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT json_build_object(
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

                    var userIdParam = command.CreateParameter();
                    userIdParam.ParameterName = "userId";
                    userIdParam.Value = userId;
                    userIdParam.DbType = DbType.Int64;
                    command.Parameters.Add(userIdParam);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var id = userId;
                        while (await reader.ReadAsync())
                        {
                            var jsonData = reader.GetValue(0)?.ToString();
                            if (!string.IsNullOrEmpty(jsonData))
                            {
                                var parsedData = System.Text.Json.JsonSerializer.Deserialize<UserAccessDTO>(
                                    jsonData, 
                                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                );
                                if (parsedData != null)
                                {
                                    parsedData.Id = id++;
                                    userAccessList.Add(parsedData);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in GetUserPrivilegesAsync: " + ex.Message, ex);
            }

            return userAccessList;
        }
    }
}

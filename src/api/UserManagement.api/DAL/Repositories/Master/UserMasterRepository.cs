using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;

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
    }
}

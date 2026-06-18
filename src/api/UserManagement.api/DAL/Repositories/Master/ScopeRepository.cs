using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;
using UserManagement.Models.MQueue;

namespace UserManagement.DAL.Repositories.Master
{
    public class ScopeRepository : Repository<ScopeMaster, UserManagementDBContext>, IScopeRepository
    {
        private readonly IConfiguration _config;
        private readonly UserManagementDBContext _context;
        private readonly IClaimService _claimService;

        public ScopeRepository(IConfiguration config, UserManagementDBContext context, IClaimService claimService) : base(context)
        {
            _config = config;
            _context = context;
            _claimService = claimService;
        }

        public async Task<(bool, string, string, string)> InsertScopeValue(List<ScopeDataInsertDTO> scopes)
        {
            Console.WriteLine(scopes);
            var parameters = new[]
            {
                new NpgsqlParameter("scopes", NpgsqlTypes.NpgsqlDbType.Jsonb)
                {
                    Value = scopes
                },
                new NpgsqlParameter("created_by", NpgsqlTypes.NpgsqlDbType.Bigint)
                {
                    Value = _claimService.GetUserId()
                },
                new NpgsqlParameter("is_done", NpgsqlTypes.NpgsqlDbType.Boolean)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = false
                },
                new NpgsqlParameter("response", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = string.Empty
                },
                new NpgsqlParameter("scope_data", NpgsqlTypes.NpgsqlDbType.Jsonb)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = new[] {
                        new { id = "id", name = "name", level_id = "level_id" }
                    }
                },
                new NpgsqlParameter("app_name", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = string.Empty
                },
            };

            var commandText = "CALL master.insert_scopes(@scopes, @created_by, @is_done, @response, @scope_data, @app_name)";

            await _context.Database.ExecuteSqlRawAsync(commandText, parameters);

            bool isDone = (bool)parameters[2].Value;
            string responseMessage = parameters[3].Value as string;
            string scope_data = parameters[4].Value as string;
            string app_name = parameters[5].Value as string;

            return (isDone, responseMessage, scope_data, app_name);
        }
        public bool CreateNewScope(ScopeCreateDTO scope)
        {
            try
            {
                string query = $"CREATE TABLE master.scopes_{scope.Name} PARTITION OF master.scopes FOR VALUES IN ({scope.LevelId});";
                int rowsAffected = _context.Database.ExecuteSqlRaw(query);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                return true;
            }
        }
        
        public List<ScopeStructureDTO> GetScopesByLevelId(int levelId)
        {
            var scopes = new List<ScopeStructureDTO>();
            try
            {
                scopes = (from aps in _context.ApplicationScopes
                          where aps.LevelId == levelId && !aps.IsDeleted
                          join sm in _context.ScopeMasters on aps.ScopeId equals sm.ScopeId
                          //join sr in _context.ScopeRelationships on aps.AppScopeId equals sr.ParentScopeId
                          //where !sr.IsDeleted && sr.IsActive
                          //join apsParent in _context.ApplicationScopes on sr.ParentScopeId equals apsParent.AppScopeId
                          //join sm in _context.ScopeMasters on apsParent.ScopeId equals sm.ScopeId
                          select new ScopeStructureDTO
                          {
                              //Id = (long)sr.ParentScopeId,
                              Id = aps.AppScopeId,
                              Name = sm.ScopeName,
                              Value = sm.Value,
                              LevelId = aps.LevelId
                              //LevelId = sr.ParentScopeLevelId ?? 0
                          }).ToList();



                return scopes;
            }
            catch (Exception ex)
            {
                return scopes;
            }
        }



        public async Task<int> GetAppScopeId(int scopeId)
        {
            try
            {
                var appScope = await _context.ApplicationScopes.FirstOrDefaultAsync(a => a.ScopeId == scopeId);
                return appScope?.AppScopeId ?? 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public async Task<bool> DeleteApplicationScopesByScopeId(int scopeId, long? updatedBy = null)
        {
            try
            {
                var appScopes = await _context.ApplicationScopes.Where(a => a.ScopeId == scopeId).ToListAsync();
                if (appScopes.Any())
                {
                    foreach (var appScope in appScopes)
                    {
                        appScope.IsDeleted = true;
                        appScope.Status = 0;
                        appScope.UpdatedAt = DateTime.Now;
                        if (updatedBy.HasValue)
                        {
                            appScope.UpdatedBy = updatedBy.Value;
                        }
                    }
                    _context.ApplicationScopes.UpdateRange(appScopes);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        //public async Task<List<ScopeFetchDTO>> GetScopesOfAuthenticatedUserByLevelId(List<ScopeDataForSearchDTO> data, string? search)
        //{
        //    var scopes = new List<ScopeFetchDTO>();

        //    try
        //    {
        //        foreach (var item in data)
        //        {
        //            string query = @"SELECT id, name, value 
        //                     FROM master.scopes 
        //                     WHERE level_id = @levelId 
        //                     AND id = @id";

        //            if (!string.IsNullOrEmpty(search))
        //            {
        //                query += " AND (LOWER(name) LIKE LOWER(@search) OR LOWER(value) LIKE LOWER(@search))";
        //            }

        //            var command = _context.Database.GetDbConnection().CreateCommand();
        //            command.CommandText = query;

        //            var levelParam = command.CreateParameter();
        //            levelParam.ParameterName = "@levelId";
        //            levelParam.Value = item.LevelId;
        //            command.Parameters.Add(levelParam);

        //            var idParam = command.CreateParameter();
        //            idParam.ParameterName = "@id";
        //            idParam.Value = item.Id;
        //            command.Parameters.Add(idParam);

        //            if (!string.IsNullOrEmpty(search))
        //            {
        //                var searchParam = command.CreateParameter();
        //                searchParam.ParameterName = "@search";
        //                searchParam.Value = $"%{search}%";
        //                command.Parameters.Add(searchParam);
        //            }

        //            if (command.Connection.State != System.Data.ConnectionState.Open)
        //                command.Connection.Open();

        //            using (var result = command.ExecuteReader())
        //            {
        //                while (result.Read())
        //                {
        //                    scopes.Add(new ScopeFetchDTO
        //                    {
        //                        ScopeId = result.GetInt32(0),
        //                        ScopeName = result.GetString(1),
        //                        ScopeValue = result.GetString(2)
        //                    });
        //                }
        //            }
        //        }

        //        return scopes;
        //    }
        //    catch
        //    {
        //        return scopes;
        //    }
        //}
        public async Task<List<ScopeFetchDTO>> GetScopesOfAuthenticatedUserByLevelId(List<ScopeDataForSearchDTO> data, string? filter)
        {
            var scopes = new List<ScopeFetchDTO>();
            try
            {
                foreach (var item in data)
                {
                    string query = $"SELECT aps.app_scope_id, sm.scope_name, sm.value \r\nFROM master.application_scope aps JOIN master.scope_master sm ON aps.scope_id = sm.scope_id\r\nJOIN master.application_level apl ON aps.level_id = apl.app_level_id\r\nWHERE apl.app_level_id = {item.LevelId} and aps.app_scope_id ={item.Id}";

                    if (!string.IsNullOrEmpty(filter))
                    {
                        // Escape special LIKE characters
                        filter = filter
                            .Replace(@"\", @"\\")  // escape backslash first
                            .Replace("_", @"\_")
                            .Replace("%", @"\%");

                        query += " AND ((sm.scope_name) ILIKE @filter ESCAPE '\\' OR (sm.value) ILIKE @filter ESCAPE '\\')";

                        //query += " AND ((name) ILIKE (@filter) OR (value) ILIKE (@filter))";
                    }

                    // Use DbContext to create and execute the command
                    var command = _context.Database.GetDbConnection().CreateCommand();
                    command.CommandText = query;
                    command.CommandType = System.Data.CommandType.Text;

                    // Ensure the DbContext connection is used and opened properly
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                    {
                        command.Connection.Open();
                    }

                    var levelParam = command.CreateParameter();
                    levelParam.ParameterName = "@levelId";
                    levelParam.Value = item.LevelId;
                    command.Parameters.Add(levelParam);

                    var idParam = command.CreateParameter();
                    idParam.ParameterName = "@id";
                    idParam.Value = item.Id;
                    command.Parameters.Add(idParam);

                    if (!string.IsNullOrEmpty(filter))
                    {
                        var searchParam = command.CreateParameter();
                        searchParam.ParameterName = "@filter";
                        searchParam.Value = $"%{filter}%";
                        command.Parameters.Add(searchParam);
                    }

                    using (var result = command.ExecuteReader())
                    {
                        // Map the result to ScopeStructureDTO
                        while (result.Read())
                        {
                            scopes.Add(new ScopeFetchDTO
                            {
                                ScopeId = result.GetInt32(0),
                                ScopeName = result.GetString(1),
                                ScopeValue = result.GetString(2)
                            });
                        }
                    }
                }

                return scopes;
            }
            catch (Exception ex)
            {
                return scopes;
            }
        }
        public async Task<List<ScopeFetchDTO>> GetScopesByScopeIds(List<long> ids)
        {
            var scopes = new List<ScopeFetchDTO>();
            if (ids == null || !ids.Any()) return scopes;

            try
            {
                // Optimized to avoid N+1 query loops
                string idsList = string.Join(",", ids);
                string query = $@"
                    SELECT sm.scope_id, sm.scope_name, sm.value, ascp.level_id 
                    FROM master.scope_master sm
                    LEFT JOIN master.application_scope ascp ON sm.scope_id = ascp.scope_id
                    WHERE ascp.app_scope_id IN ({idsList});";

                var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = query;
                command.CommandType = System.Data.CommandType.Text;

                if (command.Connection.State != System.Data.ConnectionState.Open)
                {
                    await command.Connection.OpenAsync();
                }

                using (var result = await command.ExecuteReaderAsync())
                {
                    while (await result.ReadAsync())
                    {
                        scopes.Add(new ScopeFetchDTO
                        {
                            ScopeId = result.GetInt32(0),
                            ScopeName = result.GetString(1),
                            ScopeValue = result.IsDBNull(2) ? string.Empty : result.GetString(2),
                            LevelId = result.IsDBNull(3) ? null : result.GetInt32(3)
                        });
                    }
                }

                return scopes;
            }
            catch (Exception ex)
            {
                return scopes;
            }
        }
        public async Task<List<ScopeFetchDTO>> GetScopesByScopeIdsForOtherOffice(List<long> ids)
        {
            var uniqueScopes = new HashSet<ScopeFetchDTO>();
            try
            {
                foreach (var id in ids)
                {
                    string query = "SELECT id, name, value, level_id FROM master.scopes WHERE id = @id AND is_admin_created = FALSE;";

                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = query;
                        command.CommandType = System.Data.CommandType.Text;
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@id";
                        parameter.Value = id;
                        command.Parameters.Add(parameter);

                        if (command.Connection.State != System.Data.ConnectionState.Open)
                        {
                            await command.Connection.OpenAsync();
                        }


                        using (var result = await command.ExecuteReaderAsync())
                        {
                            while (await result.ReadAsync())
                            {

                                uniqueScopes.Add(new ScopeFetchDTO
                                {
                                    ScopeId = result.GetInt32(0),
                                    ScopeName = result.GetString(1),
                                    ScopeValue = result.GetString(2),
                                    LevelId = result.GetInt32(3)
                                });
                            }
                        }
                    }
                }

                return uniqueScopes.ToList();
            }
            catch (Exception ex)
            {
                return uniqueScopes.ToList();
            }
        }
        public bool UpdateScopeTableName(string oldTableName, string newTableName)
        {
            if (string.IsNullOrWhiteSpace(oldTableName) || string.IsNullOrWhiteSpace(newTableName))
                throw new ArgumentException("Partition names cannot be null or empty.");

            try
            {
                string detachQuery = $"ALTER TABLE scopes DETACH PARTITION {oldTableName};";
                _context.Database.ExecuteSqlRaw(detachQuery);

                string renameQuery = $"ALTER TABLE {oldTableName} RENAME TO {newTableName};";
                _context.Database.ExecuteSqlRaw(renameQuery);

                string attachQuery = $"ALTER TABLE scopes ATTACH PARTITION {newTableName};";
                _context.Database.ExecuteSqlRaw(attachQuery);

                return true; 
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to rename partition from '{oldTableName}' to '{newTableName}'.", ex);
            }
        }

        public async Task<(bool, string)> ImportScopesFromCSV(CSVScopeDataDTO scopes, long createdBy)
        {
            var parameters = new[]
            {
                    new NpgsqlParameter("scopes", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = scopes },
                    new NpgsqlParameter("created_by_user", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = createdBy },
                    new NpgsqlParameter("is_done", NpgsqlTypes.NpgsqlDbType.Boolean)
                    {
                        Direction = ParameterDirection.InputOutput,
                        Value = false
                    },
                    new NpgsqlParameter("response", NpgsqlTypes.NpgsqlDbType.Text)
                    {
                        Direction = ParameterDirection.InputOutput,
                        Value = string.Empty
                    }
                };

            var commandText = "CALL master.import_scopes_from_csv(@scopes, @created_by_user, @is_done, @response)";

            await _context.Database.ExecuteSqlRawAsync(commandText, parameters);

            bool isSuccess = (bool)parameters[2].Value;
            string responseMessage = parameters[3].Value as string;

            return (isSuccess, responseMessage);
        }

        public async Task<(bool, string)> DeleteScopePartition(string partitionTableName)
        {
            try
            {
                var parameters = new[] {
                    new NpgsqlParameter("table_name", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = partitionTableName },
                    new NpgsqlParameter("is_done", NpgsqlTypes.NpgsqlDbType.Boolean)
                    {
                        Direction = ParameterDirection.InputOutput,
                        Value = false
                    },
                    new NpgsqlParameter("response", NpgsqlTypes.NpgsqlDbType.Text)
                    {
                        Direction = ParameterDirection.InputOutput,
                        Value = string.Empty
                    }
                };
                var commandText = "CALL master.delete_scope(@table_name, @is_done, @response)";

                await _context.Database.ExecuteSqlRawAsync(commandText, parameters);

                bool isSuccess = (bool)parameters[1].Value;
                string responseMessage = parameters[2].Value as string;

                return (isSuccess, responseMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while dropping partition table: {ex.Message}");
                return (false, "Unable to delete partition scope table");
            }
            }
        public async Task<(bool, string)> UpdatePartitionTable(string oldLevel, string newLevel)
        {
            try
            {
                var parameters = new[] {
                    new NpgsqlParameter("old_name", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = oldLevel },
                    new NpgsqlParameter("new_name", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = newLevel },
                    new NpgsqlParameter("is_done", NpgsqlTypes.NpgsqlDbType.Boolean)
                    {
                        Direction = ParameterDirection.InputOutput,
                        Value = false
                    },
                    new NpgsqlParameter("response", NpgsqlTypes.NpgsqlDbType.Text)
                    {
                        Direction = ParameterDirection.InputOutput,
                        Value = string.Empty
                    }
                };
                var commandText = "CALL master.update_scope_table_name(@old_name, @new_name, @is_done, @response)";

                await _context.Database.ExecuteSqlRawAsync(commandText, parameters);

                bool isSuccess = (bool)parameters[2].Value;
                string responseMessage = parameters[3].Value as string;

                return (isSuccess, responseMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while dropping partition table: {ex.Message}");
                return (false, "Unable to delete partition scope table");
            }
         }
        public async Task<List<string>> GetParentScopeValuesAsync(string scopeValue, long levelId)
        {
            return await _context.ApplicationScopes
                .Where(aps => aps.Scope.Value == scopeValue && aps.LevelId == levelId && !aps.IsDeleted)
                .SelectMany(aps => aps.ScopeRelationships
                    .Where(sr => !sr.IsDeleted && sr.IsActive && sr.ParentAppScope.Scope.Value != scopeValue && sr.OwnScopeLevelId == (int)levelId))
                .Select(sr => sr.ParentAppScope.Scope.Value)
                .ToListAsync();
        }



        public async Task UpsertMasterTreasuryScopeAsync(MasterTreasuryConsumerPayload message)
        {
            var tableName = message.Table?.Trim();
            var code = message.Data.Code?.Trim();
            var treasuryName = message.Data.TreasuryName?.Trim();

            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(code)) 
                return;

            var level = await _context.LevelMasters
                .Where(l => l.LevelName.ToLower().Trim() == tableName.ToLower() && l.IsGlobal == true)
                .FirstOrDefaultAsync();

            if (level != null)
            {
                var existingScope = await _context.ScopeMasters
                    .Where(s => s.Value == code)
                    .FirstOrDefaultAsync();

                int scopeId;
                if (existingScope != null)
                {
                    existingScope.ScopeName = treasuryName;
                    existingScope.LevelId = level.LevelId;
                    existingScope.IsGlobal = true;
                    existingScope.IsActive = message.Data.IsActive ?? true;
                    existingScope.IsDeleted = message.Data.IsDeleted ?? false;
                    existingScope.UpdatedBy = message.Data.UpdatedByUserId;
                    existingScope.UpdatedAt = DateTime.Now;

                    _context.ScopeMasters.Update(existingScope);
                    await _context.SaveChangesAsync();
                    scopeId = existingScope.ScopeId;
                }
                else
                {
                    var newScope = new ScopeMaster
                    {
                        ScopeName = treasuryName,
                        Value = code,
                        IsGlobal = true,
                        CreatedAt = message.Data.CreatedAt ?? DateTime.Now,
                        LevelId = level.LevelId,
                        IsActive = message.Data.IsActive ?? true,
                        IsDeleted = message.Data.IsDeleted ?? false,
                        CreatedBy = message.Data.CreatedByUserId ?? 0,
                        UpdatedBy = message.Data.UpdatedByUserId,
                        UpdatedAt = message.Data.UpdatedAt
                    };

                    await _context.ScopeMasters.AddAsync(newScope);
                    await _context.SaveChangesAsync();
                    scopeId = newScope.ScopeId;
                }

                var appLevels = await _context.ApplicationLevels
                    .Where(al => al.LevelId == level.LevelId && (al.IsDeleted == false || al.IsDeleted == null))
                    .ToListAsync();

                foreach (var appLevel in appLevels)
                {
                    if (appLevel.AppId.HasValue)
                    {
                        var appScope = await _context.ApplicationScopes
                            .Where(aps => aps.AppId == appLevel.AppId.Value && aps.LevelId == appLevel.AppLevelId && aps.ScopeId == scopeId)
                            .FirstOrDefaultAsync();

                        if (appScope == null)
                        {
                            appScope = new ApplicationScope
                            {
                                AppId = appLevel.AppId.Value,
                                LevelId = appLevel.AppLevelId,
                                ScopeId = scopeId,
                                Status = 1
                            };
                            await _context.ApplicationScopes.AddAsync(appScope);
                            await _context.SaveChangesAsync();
                        }

                        var existingRel = await _context.ScopeRelationships
                            .Where(sr => sr.ScopeId == appScope.AppScopeId && sr.ParentScopeId == appScope.AppScopeId)
                            .FirstOrDefaultAsync();

                        if (existingRel == null)
                        {
                            var newRel = new ScopeRelationship
                            {
                                ScopeId = appScope.AppScopeId,
                                ParentScopeId = appScope.AppScopeId,
                                OwnScopeLevelId = appLevel.AppLevelId,
                                ParentScopeLevelId = appLevel.AppLevelId,
                                IsActive = message.Data.IsActive ?? true,
                                IsDeleted = false
                            };
                            await _context.ScopeRelationships.AddAsync(newRel);
                        }
                        else
                        {
                            existingRel.IsActive = message.Data.IsActive ?? true;
                            existingRel.IsDeleted = false;
                            _context.ScopeRelationships.Update(existingRel);
                        }
                    }
                }


                try
                {
                    await _context.SaveChangesAsync();
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    throw;
                }
            }
        }

        public async Task UpsertMasterDdoScopeAsync(MasterDdoConsumerPayload message)
        {
            var tableName = message.Table?.Trim();
            var code = message.Data.DdoCode?.Trim();
            var designation = message.Data.Designation?.Trim();
            var treasuryCode = message.Data.TreasuryCode?.Trim();


            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(code))
                return;

            var level = await _context.LevelMasters
                .Where(l => l.LevelName.ToLower().Trim() == tableName.ToLower() && l.IsGlobal == true)
                .FirstOrDefaultAsync();

            if (level != null)
            {
                var existingScope = await _context.ScopeMasters
                    .Where(s => s.Value.Trim() == code)
                    .FirstOrDefaultAsync();

                int scopeId;
                if (existingScope != null)
                {
                    existingScope.ScopeName = designation;
                    existingScope.LevelId = level.LevelId;
                    existingScope.IsGlobal = true;
                    existingScope.IsActive = message.Data.IsActive ?? true;
                    existingScope.IsDeleted = message.Data.IsDeleted ?? false;
                    existingScope.UpdatedBy = message.Data.UpdatedByUserId;
                    existingScope.UpdatedAt = DateTime.Now;

                    _context.ScopeMasters.Update(existingScope);
                    await _context.SaveChangesAsync();
                    scopeId = existingScope.ScopeId;
                }
                else
                {
                    var newScope = new ScopeMaster
                    {
                        ScopeName = designation ?? string.Empty,
                        Value = code,
                        IsGlobal = true,
                        CreatedAt = message.Data.CreatedAt ?? DateTime.Now,
                        LevelId = level.LevelId,
                        IsActive = message.Data.IsActive ?? true,
                        IsDeleted = message.Data.IsDeleted ?? false,
                        CreatedBy = message.Data.CreatedByUserId ?? 0,
                        UpdatedBy = message.Data.UpdatedByUserId,
                        UpdatedAt = message.Data.UpdatedAt
                    };

                    await _context.ScopeMasters.AddAsync(newScope);
                    await _context.SaveChangesAsync();
                    scopeId = newScope.ScopeId;
                }

                var appLevels = await _context.ApplicationLevels
                    .Where(al => al.LevelId == level.LevelId && (al.IsDeleted == false || al.IsDeleted == null))
                    .ToListAsync();

                foreach (var appLevel in appLevels)
                {
                    try
                    {
                        if (appLevel.AppId.HasValue)
                        {
                            var appScope = await _context.ApplicationScopes
                                .Where(aps => aps.AppId == appLevel.AppId.Value && aps.LevelId == appLevel.AppLevelId && aps.ScopeId == scopeId)
                                .FirstOrDefaultAsync();

                            if (appScope == null)
                            {
                                appScope = new ApplicationScope
                                {
                                    AppId = appLevel.AppId.Value,
                                    LevelId = appLevel.AppLevelId,
                                    ScopeId = scopeId,
                                    Status = 1
                                };
                                await _context.ApplicationScopes.AddAsync(appScope);
                                await _context.SaveChangesAsync();
                            }

                            if (!string.IsNullOrWhiteSpace(treasuryCode))
                            {
                                var treasuryScopeMaster = await _context.ScopeMasters
                                    .FirstOrDefaultAsync(s => s.Value.Trim() == treasuryCode && s.Value.Length == treasuryCode.Length);

                                if (treasuryScopeMaster == null)
                                {
                                    throw new InvalidOperationException(
                                        $"Treasury ScopeMaster not found for TreasuryCode: {treasuryCode}");
                                }

                                var treasuryAppScope = await _context.ApplicationScopes
                                    .FirstOrDefaultAsync(x =>
                                        x.AppId == appLevel.AppId.Value &&
                                        x.ScopeId == treasuryScopeMaster.ScopeId);

                                if (treasuryAppScope == null)
                                {
                                    throw new InvalidOperationException(
                                        $"Treasury ApplicationScope not found. AppId: {appLevel.AppId.Value}, TreasuryCode: {treasuryCode}");
                                }

                                var existingRel = await _context.ScopeRelationships
                                    .FirstOrDefaultAsync(sr =>
                                        sr.ScopeId == appScope.AppScopeId &&
                                        sr.ParentScopeId == treasuryAppScope.AppScopeId);

                                if (existingRel == null)
                                {
                                    var newRel = new ScopeRelationship
                                    {
                                        ScopeId = appScope.AppScopeId,
                                        ParentScopeId = treasuryAppScope.AppScopeId,
                                        OwnScopeLevelId = appScope.LevelId,
                                        ParentScopeLevelId = treasuryAppScope.LevelId,
                                        IsActive = message.Data.IsActive ?? true,
                                        IsDeleted = false
                                    };

                                    await _context.ScopeRelationships.AddAsync(newRel);
                                    await _context.SaveChangesAsync();
                                }
                                else
                                {
                                    existingRel.IsActive = message.Data.IsActive ?? true;
                                    existingRel.IsDeleted = false;
                                    _context.ScopeRelationships.Update(existingRel);
                                    await _context.SaveChangesAsync();
                                }
                            }
                            else
                            {
                                throw new ArgumentException("TreasuryCode is null or empty.");
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error or handle it as needed
                        Console.WriteLine($"Error saving: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        }
                        throw;
                    }
                }
            }
        }

        public async Task<List<LevelGetDTO>> GetAllLevelMastersAsync()
        {
            return await _context.LevelMasters
                .Where(l => l.IsGlobal == true)
                .Select(l => new LevelGetDTO
                {
                    Id = l.LevelId,
                    Title = l.LevelName
                })
                .ToListAsync();
        }

        public async Task<ScopeReturnDTO> GetScopeByLevelIdWithPaginationAsync(int levelId, int offset, int limit, string filter, string search)
        {
            var query = _context.ScopeMasters
                .Where(s => !s.IsDeleted && s.IsActive && s.LevelId == levelId);

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(s => s.ScopeName.ToLower().Contains(lowerSearch) || s.Value.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrEmpty(filter))
            {
                var lowerFilter = filter.ToLower();
                query = query.Where(s => s.ScopeName.ToLower() == lowerFilter || s.Value.ToLower() == lowerFilter);
            }

            var totalCount = await query.CountAsync();

            var scopes = await query
                .OrderBy(s => s.ScopeName)
                .Skip(offset)
                .Take(limit)
                .Select(s => new ScopesDTO
                {
                    Id = s.ScopeId,
                    Name = s.ScopeName,
                    Value = s.Value,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            return new ScopeReturnDTO
            {
                Scopes = scopes,
                TotalCount = totalCount
            };
        }

        public async Task<bool> UpdateApplicationScopeStatus(int appId, int scopeId, short status)
        {
            try
            {
                var appScope = await _context.ApplicationScopes.FirstOrDefaultAsync(a => a.AppId == appId && a.ScopeId == scopeId);
                if (appScope != null)
                {
                    appScope.Status = status;
                    appScope.UpdatedAt = DateTime.Now;
                    appScope.UpdatedBy = _claimService.GetUserId();
                    _context.ApplicationScopes.Update(appScope);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}


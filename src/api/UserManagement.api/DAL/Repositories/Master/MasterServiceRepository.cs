using Microsoft.EntityFrameworkCore;
using Npgsql;
using RabbitMQ.Client;
using System.Data;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Repositories.Master
{
    public class MasterServiceRepository : Repository<Service, UserManagementDBContext>, IMasterServiceRepository
    {
        private readonly UserManagementDBContext _context;

        public MasterServiceRepository(UserManagementDBContext context) : base(context)
        {
            _context = context;
        }
        public async Task<List<MasterServicesDTO>> GetServicesWithPermissionsAsync()
        {
            const string sql = @"
                SELECT
                    s.id                AS Id,
                    s.service_name      AS ServiceName,
                    a.id                AS AppId,
                    CASE 
                        WHEN ahs.id IS NOT NULL THEN true
                        ELSE false
                    END                 AS Enabled
                FROM master.services s
                CROSS JOIN master.applications a
                LEFT JOIN master.application_has_services ahs
                       ON ahs.service_id = s.id
                      AND ahs.app_id = a.id
                ORDER BY s.id, a.id;
            ";

            var rows = new List<ServicePermissionFlatRow>();

            await using var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                rows.Add(new ServicePermissionFlatRow
                {
                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                    ServiceName = reader.GetString(reader.GetOrdinal("ServiceName")),
                    AppId = reader.GetInt64(reader.GetOrdinal("AppId")),
                    Enabled = reader.GetBoolean(reader.GetOrdinal("Enabled"))
                });
            }

            var result = rows
                .GroupBy(r => new { r.Id, r.ServiceName })
                .Select(g => new MasterServicesDTO
                {
                    Id = g.Key.Id,
                    ServiceName = g.Key.ServiceName,
                    Permissions = g.Select(p => new ServicePermissionDTO
                    {
                        AppId = p.AppId,
                        Enabled = p.Enabled
                    }).ToList()
                })
                .ToList();

            return result;
        }

        public async Task<(bool, string)> EnableDisableServiceAsync(long serviceId,long appId,bool enabled,long userId)
        {
            string client_id = "";
            await using var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;

                if (enabled)
                {
                    
                    command.CommandText = @"
                INSERT INTO master.application_has_services
                    (service_id, app_id, client_secret)
                VALUES (@serviceId, @appId, gen_random_uuid())
                ON CONFLICT (service_id, app_id) DO NOTHING
                RETURNING client_secret;
            ";



                }
                else
                {
                    //client_id = await GetClientSecretAsync(serviceId, appId);
                    command.CommandText = @"
                    DELETE FROM master.application_has_services
                    WHERE service_id = @serviceId
                      AND app_id = @appId
                    RETURNING client_secret;";
                }

                var p1 = command.CreateParameter();
                p1.ParameterName = "@serviceId";
                p1.Value = serviceId;

                var p2 = command.CreateParameter();
                p2.ParameterName = "@appId";
                p2.Value = appId;

                command.Parameters.Add(p1);
                command.Parameters.Add(p2);

                var result = await command.ExecuteScalarAsync();
                await transaction.CommitAsync();

                if (result != null && result != DBNull.Value)
                    client_id = ((Guid)result).ToString();
                return (true, client_id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<Guid> GetClientSecretAsync(long serviceId, long appId)
        {
            await using var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
        SELECT client_secret
        FROM master.application_has_services
        WHERE service_id = @serviceId
          AND app_id = @appId
        LIMIT 1;";

            var serviceIdParam = command.CreateParameter();
            serviceIdParam.ParameterName = "@serviceId";
            serviceIdParam.Value = serviceId;

            var appIdParam = command.CreateParameter();
            appIdParam.ParameterName = "@appId";
            appIdParam.Value = appId;

            command.Parameters.Add(serviceIdParam);
            command.Parameters.Add(appIdParam);

            var result = await command.ExecuteScalarAsync();



            return (Guid)result;
        }

    }
}

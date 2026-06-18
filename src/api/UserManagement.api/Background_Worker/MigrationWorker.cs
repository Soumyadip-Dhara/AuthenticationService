using Npgsql;
using NpgsqlTypes;
using RabbitMQ.Client;
using System.Data;
using System.Runtime.CompilerServices;

namespace UserManagement.Background_Worker
{
    public sealed class MigrationWorker : BackgroundService
    {
        private readonly string _connString;
        private readonly ILogger<MigrationWorker> _logger;

        public MigrationWorker(
            IConfiguration config,
            ILogger<MigrationWorker> logger)
        {
            _connString = config.GetConnectionString("UserManagementDBConnection");
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessNextRow(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Migration worker cycle failed");
                }

                await Task.Delay(2000, stoppingToken); // throttle
            }
        }
        private async Task ProcessNextRow(CancellationToken ct)
        {
            long rowId = 0;
            Guid jobId = Guid.Empty;
            string payload = string.Empty;
            bool found;


            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            var cmd = new NpgsqlCommand(@"
                SELECT row_no, job_id, payload
                FROM job.migration_staging
                WHERE status = 'PENDING'
                ORDER BY id
                FOR UPDATE SKIP LOCKED
                LIMIT 1
            ", conn, tx);

            await using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                found = await reader.ReadAsync(ct);

                if (found)
                {
                    rowId = reader.GetInt64(0);
                    jobId = reader.GetGuid(1);
                    payload = reader.GetString(2);
                }
            }

            if (!found)
            {
                await tx.RollbackAsync(ct);
                return;
            }

            await MarkRunning(conn, tx, rowId);

            try
            {
                await ExecuteMigration(conn, tx, payload, rowId, jobId);
                await MarkSuccess(conn, tx, rowId, jobId);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await MarkFailed(conn, tx, rowId, jobId, ex.Message);
                await tx.CommitAsync(ct);
            }
        }
        private async Task ExecuteMigration(NpgsqlConnection conn, NpgsqlTransaction tx, string payloadJson, long rowId, Guid jobId)
        {
            await using var connection = new NpgsqlConnection(_connString);

            await connection.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                @"SELECT * FROM ""user"".migrate_single_user(@payload::jsonb);",
                connection
            );

            cmd.Parameters.AddWithValue(
                "@payload",
                NpgsqlTypes.NpgsqlDbType.Jsonb,
                payloadJson
            );

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                bool IsDone = reader.GetBoolean(reader.GetOrdinal("is_done"));
                var message = reader.GetString(reader.GetOrdinal("msg"));
                if (!IsDone)
                {
                    await MarkFailed(conn, tx, rowId, jobId, message);
                }
                    await MarkSuccess(conn, tx, rowId, jobId);

                //return new MigrateUserResponseDto
                //{
                //    IsDone = reader.GetBoolean(reader.GetOrdinal("is_done")),
                //    Message = reader.GetString(reader.GetOrdinal("msg"))
                //};
            }

            //return new MigrateUserResponseDto
            //{
            //    IsDone = false,
            //    Message = "No response from DB function"
            //};
        }

        private static Task MarkRunning(NpgsqlConnection conn, NpgsqlTransaction tx, long rowId)
        {
            return new NpgsqlCommand(@"
            UPDATE job.migration_staging
            SET status = 'RUNNING'
            WHERE row_no = @row_id",
            conn, tx)
            {
                Parameters = { new("@row_id", rowId) }
            }.ExecuteNonQueryAsync();
        }
        private static async Task MarkSuccess(NpgsqlConnection conn, NpgsqlTransaction tx, long rowId, Guid jobId)
        {
            await new NpgsqlCommand(@"
                UPDATE job.migration_staging
                SET status = 'SUCCESS',
                    processed_at = now()
                WHERE row_no = @id",
                  conn, tx)
            {
                Parameters = { new("@id", rowId) }
            }.ExecuteNonQueryAsync();

            await new NpgsqlCommand(@"
            UPDATE job.migration_jobs
            SET processed_rows = processed_rows + 1,
                success_rows = success_rows + 1
            WHERE job_id = @jobId",
                  conn, tx)
            {
                Parameters = { new("@jobId", jobId) }
            }.ExecuteNonQueryAsync();
        }
        private static async Task MarkFailed(NpgsqlConnection conn, NpgsqlTransaction tx, long rowId, Guid jobId, string error)
        {
            await new NpgsqlCommand(@"
                UPDATE job.migration_staging
                SET status = 'FAILED',
                    error = @err,
                    processed_at = now()
                WHERE row_no = @id",
                  conn, tx)
            {
                Parameters ={
                new("@id", rowId),
                new("@err", error)
                }
            }.ExecuteNonQueryAsync();

            await new NpgsqlCommand(@"
                UPDATE job.migration_jobs
                SET processed_rows = processed_rows + 1,
                    failed_rows = failed_rows + 1
                WHERE job_id = @jobId",
                  conn, tx)
            {
                Parameters = { new("@jobId", jobId) }
            }.ExecuteNonQueryAsync();
        }



    }
}

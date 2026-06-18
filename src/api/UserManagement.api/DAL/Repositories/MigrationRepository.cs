using CsvHelper;
using Dapper;
using Npgsql;
using System.Globalization;
using System.Text.Json;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.Models.DTO;
namespace UserManagement.DAL
{
    public class MigrationRepository(UserManagementDBContext context, IConfiguration config) : Repository<TempHrm, UserManagementDBContext>(context), IMigrationRepository
    {
        private readonly string _connString = config.GetConnectionString("UserManagementDBConnection");

        public async Task<Guid> CreateJobAsync(string entityType)
        {
            var jobId = Guid.NewGuid();

            await using var conn = new NpgsqlConnection(_connString);
            await conn.ExecuteAsync(
                @"INSERT INTO migration_job(job_id, entity_type)
              VALUES (@jobId, @entity)",
                new { jobId, entity = entityType });

            return jobId;
        }

        //public async Task BulkInsertCsvAsync(Guid jobId, IFormFile file)
        //{
        //    await using var conn = new NpgsqlConnection(_connString);
        //    await conn.OpenAsync();

        //    using var reader = new StreamReader(file.OpenReadStream());
        //    using var importer = conn.BeginTextImport(
        //        "COPY migration_staging(job_id, row_no, payload) FROM STDIN");

        //    int rowNo = 0;
        //    while (!reader.EndOfStream)
        //    {
        //        var line = reader.ReadLine();
        //        var json = ConvertCsvLineToJson(line);

        //        importer.WriteLine(
        //            $"{jobId}\t{++rowNo}\t{json}");
        //    }

        //    await conn.ExecuteAsync(
        //        @"UPDATE migration_job
        //      SET total_rows = @cnt
        //      WHERE job_id = @jobId",
        //        new { cnt = rowNo, jobId });
        //}


        public async Task ProcessBatchAsync(Guid jobId, int batchSize)
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.ExecuteAsync(
                "CALL migrate_user_batch(@jobId, @batch)",
                new { jobId, batch = batchSize });
        }

        public async Task RetryFailedAsync(Guid jobId)
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.ExecuteAsync(
                @"UPDATE migration_staging
              SET status = 'PENDING', error = NULL
              WHERE job_id = @jobId
                AND status = 'FAILED'",
                new { jobId });
        }

        //private static string ConvertCsvLineToJson(string csvLine)
        //{
        //    var values = csvLine.Split(',');

        //    var obj = new Dictionary<string, object?>
        //    {
        //        ["id"] = ParseInt(values[0]),
        //        ["hrms_id"] = ParseNullableString(values[1]),
        //        ["name"] = values[2],
        //        ["email"] = ParseNullableString(values[3]),
        //        ["username"] = values[4],
        //        ["is_active"] = values[5],
        //        ["designation"] = values[6],
        //        ["is_an_admin"] = values[7],
        //        ["mobile_number"] = ParseNullableLong(values[8])
        //    };

        //    return JsonSerializer.Serialize(obj);
        //}
        private static string ConvertRowToPayload(UserCsvRowDTO row)
        {
            var payload = new
            {
                user = new
                {
                    id = row.ID,
                    hrms_id = string.IsNullOrWhiteSpace(row.HrmsId) ? null : row.HrmsId,
                    name = row.Name,
                    email = string.IsNullOrWhiteSpace(row.Email) ? null : row.Email,
                    username = row.Username,
                    is_active = row.IsActive,
                    designation = row.Designation,
                    is_an_admin = row.IsAnAdmin,
                    mobile_number = row.MobileNumber,
                    authority_code = row.AuthorityCode,
                    optional = row.Optional
                },
                privileges = new[]
                {
            new
            {
                role = row.Role,
                scopes = new[] { row.AuthorityCode },
                level = row.Level,
                app = row.AppName
            }
        }
            };

            return JsonSerializer.Serialize(payload);
        }


        private static object? ParseNullableString(string v)
            => string.IsNullOrWhiteSpace(v) || v == "NaN" ? null : v;

        private static object? ParseInt(string v)
            => int.TryParse(v, out var i) ? i : null;

        private static object? ParseNullableLong(string v)
            => long.TryParse(v, out var l) ? l : null;


        /*public async Task CopyCsvToStaging(IFormFile file, Guid jobId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("CSV file is empty");

            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            int rowNo = 0;

            try
            {
                using var importer = conn.BeginTextImport(@"
            COPY job.migration_staging (job_id, row_no, payload)
            FROM STDIN (FORMAT text)
        ");

                using var reader = new StreamReader(file.OpenReadStream());

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    rowNo++;

                    var json = ConvertCsvLineToJson(line);

                    // Validate JSON BEFORE sending to DB
                    JsonDocument.Parse(json);

                    importer.WriteLine(
                        $"{jobId}\t{rowNo}\t{json}");
                }

                await using var cmd = new NpgsqlCommand(@"
            UPDATE job.migration_jobs
            SET total_rows = @rows
            WHERE job_id = @jobId", conn, tx);

                cmd.Parameters.AddWithValue("@rows", rowNo);
                cmd.Parameters.AddWithValue("@jobId", jobId);

                await cmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();
            }
            catch (JsonException je)
            {
                await tx.RollbackAsync();
                await MarkJobFailed(conn, jobId, "Invalid JSON payload");

                throw new ApplicationException(
                    $"Invalid JSON detected at row {rowNo}", je);
            }
            catch (PostgresException pg)
            {
                await tx.RollbackAsync();
                await MarkJobFailed(conn, jobId, pg.MessageText);

                throw;
            }
            catch
            {
                await tx.RollbackAsync();
                await MarkJobFailed(conn, jobId, "Unexpected error");
                throw;
            }
        }*/

        public async Task CopyCsvToStaging(IFormFile file, Guid jobId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("CSV file is empty");

            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync();

            int rowNo = 0;

            try
            {
                // ---------- COPY PHASE (NO OTHER SQL ALLOWED) ----------
                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    // CsvHelper hard configuration (no guessing)
                    //csv.Context.RegisterClassMap<UserCsvRowMap>();
                    csv.Context.Configuration.HeaderValidated = null;
                    csv.Context.Configuration.MissingFieldFound = null;

                    using (var importer = conn.BeginTextImport(@"
                COPY job.migration_staging (job_id, row_no, payload)
                FROM STDIN
            "))
                    {
                        foreach (var row in csv.GetRecords<UserCsvRowDTO>())
                        {
                            rowNo++;

                            var payloadJson = ConvertRowToPayload(row);

                            // NOTE: payloadJson MUST be valid JSON without tabs/newlines
                            importer.WriteLine(
                                $"{jobId}\t{rowNo}\t{payloadJson}");
                        }
                    } // COPY ENDS HERE – connection leaves Copy state
                }

                // ---------- POST-COPY SQL ----------
                await conn.ExecuteAsync(@"
            UPDATE job.migration_jobs
            SET total_rows = @rows
            WHERE job_id = @jobId",
                    new { rows = rowNo, jobId });
            }
            catch (Exception ex)
            {
                // COPY is guaranteed to be closed here
                await MarkJobFailed(conn, jobId, ex.Message);
                throw;
            }
        }



        public async Task CreateJob(Guid jobId, string entityType, int totalRows)
        {
            if (totalRows <= 0)
                throw new ArgumentException("Total rows must be greater than zero");

            try
            {
                await using var conn = new NpgsqlConnection(_connString);
                await conn.OpenAsync();

                    await using var cmd = new NpgsqlCommand(@"
                INSERT INTO job.migration_jobs
                (job_id, entity_type, total_rows, status)
                VALUES (@jobId, @entityType, @totalRows, 'PENDING')", conn);

                cmd.Parameters.AddWithValue("@jobId", jobId);
                cmd.Parameters.AddWithValue("@entityType", entityType);
                cmd.Parameters.AddWithValue("@totalRows", totalRows);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (PostgresException pgEx)
            {
                // Constraint violation / duplicate job
                throw new ApplicationException(
                    $"Failed to create migration job {jobId}: {pgEx.MessageText}", pgEx);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Unexpected error creating migration job {jobId}", ex);
            }
        }
        private static async Task MarkJobFailed(NpgsqlConnection conn, Guid jobId, string error)
            {
                await using var cmd = new NpgsqlCommand(@"
                UPDATE job.migration_jobs
                SET status = 'FAILED',
                    completed_at = now()
                WHERE job_id = @jobId", conn);

                cmd.Parameters.AddWithValue("@jobId", jobId);

                await cmd.ExecuteNonQueryAsync();
            }


    }
}
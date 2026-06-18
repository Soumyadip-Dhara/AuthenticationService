using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data.Common;
using UserManagement.DAL;
using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.Models.MQueue;

namespace UserManagement.DAL.Repositories.MQueue
{
    public class RabbitMQLogsRepository : IRabbitMQLogsRepository
    {
        private readonly UserManagementDBContext _dbContext;
        private readonly IMapper _mapper;

        public RabbitMQLogsRepository(UserManagementDBContext context, IMapper mapper)
        {
            _dbContext = context;
            _mapper = mapper;
        }

        // PUBLISHED LOGS
        public async Task<PagedResponse<PublishedLogDTO>> GetPublishedLogsAsync(BaseLogFilterDTO f)
        {
            var result = new List<PublishedLogDTO>();
            var total = 0;
            var offset = (f.Page - 1) * f.Size;

            var query = @"
                SELECT 
                    unique_id,
                    queue_name,
                    exchange_name,
                    message_body::text,
                    created_at,
                    publish_at,
                    COUNT(*) OVER() AS total_count
                FROM message_queue.message_queue_logs
                WHERE
                    (@search::text IS NULL OR queue_name ILIKE '%' || @search::text || '%')
                    AND (@queue::text IS NULL OR queue_name = @queue::text)
                    AND (@fromDate::timestamp IS NULL OR created_at >= @fromDate::timestamp)
                    AND (@toDate::timestamp IS NULL OR created_at <= @toDate::timestamp)
                ORDER BY created_at DESC
                LIMIT @size OFFSET @offset;
            ";

            await using DbConnection connection = _dbContext.Database.GetDbConnection();

            if (connection is not NpgsqlConnection npgsqlConnection)
                throw new InvalidOperationException("Expected NpgsqlConnection");

            await npgsqlConnection.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, npgsqlConnection);

            // PARAMETERS (IMPORTANT)
            cmd.Parameters.AddWithValue("search", (object?)f.Search ?? DBNull.Value);
            cmd.Parameters.AddWithValue("queue", (object?)f.QueueName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fromDate", (object?)f.FromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("toDate", (object?)f.ToDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("size", f.Size);
            cmd.Parameters.AddWithValue("offset", offset);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (total == 0 && reader["total_count"] != DBNull.Value)
                    total = Convert.ToInt32(reader["total_count"]);

                result.Add(new PublishedLogDTO
                {
                    UniqueId = reader.GetGuid(reader.GetOrdinal("unique_id")),
                    QueueName = reader["queue_name"]?.ToString(),
                    ExchangeName = reader["exchange_name"]?.ToString(),
                    MessageBody = reader["message_body"]?.ToString(),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    PublishAt = reader["publish_at"] == DBNull.Value
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("publish_at"))
                });
            }

            return new PagedResponse<PublishedLogDTO>
            {
                Data = result,
                Total = total,
                Page = f.Page,
                Size = f.Size
            };
        }

        // CONSUMED LOGS
        public async Task<PagedResponse<ConsumedLogDTO>> GetConsumedLogsAsync(BaseLogFilterDTO f)
        {
            var result = new List<ConsumedLogDTO>();
            var total = 0;
            var offset = (f.Page - 1) * f.Size;

            var query = @"
                SELECT 
                    message_id,
                    queue_name,
                    exchange_name,
                    raouting_key,
                    message_body,
                    status,
                    consumed_at,
                    error_messages,
                    COUNT(*) OVER() AS total_count
                FROM message_queue.consume_logs
                WHERE
                    (@search::text IS NULL OR queue_name ILIKE '%' || @search::text || '%')
                    AND (@queue::text IS NULL OR queue_name = @queue::text)
                    AND (@fromDate::timestamp IS NULL OR consumed_at >= @fromDate::timestamp)
                    AND (@toDate::timestamp IS NULL OR consumed_at <= @toDate::timestamp)
                ORDER BY consumed_at DESC
                LIMIT @size OFFSET @offset;
            ";

            await using var connection = _dbContext.Database.GetDbConnection();

            if (connection is not NpgsqlConnection npgsqlConnection)
                throw new InvalidOperationException("Expected NpgsqlConnection");

            await npgsqlConnection.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, npgsqlConnection);

            cmd.Parameters.AddWithValue("search", (object?)f.Search ?? DBNull.Value);
            cmd.Parameters.AddWithValue("queue", (object?)f.QueueName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fromDate", (object?)f.FromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("toDate", (object?)f.ToDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("size", f.Size);
            cmd.Parameters.AddWithValue("offset", offset);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (total == 0 && reader["total_count"] != DBNull.Value)
                    total = Convert.ToInt32(reader["total_count"]);

                result.Add(new ConsumedLogDTO
                {
                    MessageId = reader["message_id"]?.ToString(),
                    QueueName = reader["queue_name"]?.ToString(),
                    ExchangeName = reader["exchange_name"]?.ToString(),
                    MessageBody = reader["message_body"]?.ToString(),
                    RoutingKey = reader["raouting_key"]?.ToString(), // keep as per DB typo
                    Status = reader["status"]?.ToString(),
                    ConsumedAt = reader["consumed_at"] == DBNull.Value
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("consumed_at")),
                    ErrorMessages = reader["error_messages"]?.ToString()
                });
            }

            return new PagedResponse<ConsumedLogDTO>
            {
                Data = result,
                Total = total,
                Page = f.Page,
                Size = f.Size
            };
        }
        // FAILED CONSUME LOGS
    
        public async Task<PagedResponse<FailedConsumeLogDTO>> GetFailedConsumeLogsAsync(BaseLogFilterDTO f)
        {
            var result = new List<FailedConsumeLogDTO>();
            var total = 0;
            var offset = (f.Page - 1) * f.Size;

            var query = @"
        SELECT 
            id,
            message_id,
            queue_name,
            exchange_name,
            routing_key,
            message_body,
            failed_type,
            failed_message,
            failed_at,
            action_status,
            COUNT(*) OVER() AS total_count
        FROM message_queue.consume_failed_logs
        WHERE
            (@search::text IS NULL OR queue_name ILIKE '%' || @search::text || '%')
            AND (@queue::text IS NULL OR queue_name = @queue::text)
            AND (@fromDate::timestamp IS NULL OR failed_at >= @fromDate::timestamp)
            AND (@toDate::timestamp IS NULL OR failed_at <= @toDate::timestamp)
        ORDER BY failed_at DESC
        LIMIT @size OFFSET @offset;
    ";

            await using var connection = _dbContext.Database.GetDbConnection();

            if (connection is not NpgsqlConnection npgsqlConnection)
                throw new InvalidOperationException("Expected NpgsqlConnection");

            await npgsqlConnection.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, npgsqlConnection);

            cmd.Parameters.AddWithValue("search", (object?)f.Search ?? DBNull.Value);
            cmd.Parameters.AddWithValue("queue", (object?)f.QueueName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fromDate", (object?)f.FromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("toDate", (object?)f.ToDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("size", f.Size);
            cmd.Parameters.AddWithValue("offset", offset);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (total == 0 && reader["total_count"] != DBNull.Value)
                    total = Convert.ToInt32(reader["total_count"]);

                result.Add(new FailedConsumeLogDTO
                {
                    Id = Convert.ToInt64(reader["id"]),
                    MessageId = reader["message_id"] == DBNull.Value
                        ? Guid.Empty
                        : (Guid)reader["message_id"],
                    QueueName = reader["queue_name"]?.ToString(),
                    ExchangeName = reader["exchange_name"]?.ToString(),
                    RoutingKey = reader["routing_key"]?.ToString(),
                    MessageBody = reader["message_body"]?.ToString(),
                    FailedType = reader["failed_type"]?.ToString(),
                    FailedMessage = reader["failed_message"]?.ToString(),
                    FailedAt = reader.GetDateTime(reader.GetOrdinal("failed_at")),
                    ActionStatus = reader["action_status"]?.ToString()
                });
            }

            return new PagedResponse<FailedConsumeLogDTO>
            {
                Data = result,
                Total = total,
                Page = f.Page,
                Size = f.Size
            };
        }

        // PUBLISHED ACK LOGS

        public async Task<PagedResponse<PublishAckLogDTO>> GetPublishAckLogsAsync(BaseLogFilterDTO f)
        {
            var result = new List<PublishAckLogDTO>();
            var total = 0;
            var offset = (f.Page - 1) * f.Size;

            var query = @"
                SELECT 
                    unique_id,
                    consume_message_id,
                    queue_name,
                    message_body,
                    exchange_name,
                    created_at,
                    publish_at,
                    COUNT(*) OVER() AS total_count
                FROM message_queue.published_acknowledgement_logs
                WHERE
                    (@queue::text IS NULL OR queue_name = @queue::text)
                    AND (@fromDate::timestamp IS NULL OR created_at >= @fromDate::timestamp)
                    AND (@toDate::timestamp IS NULL OR created_at <= @toDate::timestamp)
                ORDER BY created_at DESC
                LIMIT @size OFFSET @offset;
            ";

            await using var connection = _dbContext.Database.GetDbConnection();

            if (connection is not NpgsqlConnection npgsqlConnection)
                throw new InvalidOperationException("Expected NpgsqlConnection");

            await npgsqlConnection.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, npgsqlConnection);

            cmd.Parameters.AddWithValue("queue", (object?)f.QueueName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fromDate", (object?)f.FromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("toDate", (object?)f.ToDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("size", f.Size);
            cmd.Parameters.AddWithValue("offset", offset);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (total == 0 && reader["total_count"] != DBNull.Value)
                    total = Convert.ToInt32(reader["total_count"]);

                result.Add(new PublishAckLogDTO
                {
                    UniqueId = (Guid)reader["unique_id"],
                    ConsumeMessageId = reader["consume_message_id"] == DBNull.Value ? Guid.Empty : (Guid)reader["consume_message_id"],
                    QueueName = reader["queue_name"]?.ToString(),
                    MessageBody = reader["message_body"]?.ToString(),
                    ExchangeName = reader["exchange_name"]?.ToString(),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    PublishAt = reader["publish_at"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("publish_at"))
                });
            }

            return new PagedResponse<PublishAckLogDTO>
            {
                Data = result,
                Total = total,
                Page = f.Page,
                Size = f.Size
            };
        }

        // CONSUME ACK LOGS

        public async Task<PagedResponse<ConsumeAckLogDTO>> GetConsumeAckLogsAsync(BaseLogFilterDTO f)
        {
            var result = new List<ConsumeAckLogDTO>();
            var total = 0;
            var offset = (f.Page - 1) * f.Size;

            var query = @"
                SELECT 
                    unique_id,
                    message_id,
                    published_message_id,
                    queue_name,
                    status,
                    consumed_at,
                    message_body, 
                    error_messages,
                    COUNT(*) OVER() AS total_count
                FROM message_queue.consumed_acknowledgement_logs
                WHERE
                    (@queue::text IS NULL OR queue_name = @queue::text)
                    AND (@fromDate::timestamp IS NULL OR consumed_at >= @fromDate::timestamp)
                    AND (@toDate::timestamp IS NULL OR consumed_at <= @toDate::timestamp)
                ORDER BY consumed_at DESC
                LIMIT @size OFFSET @offset;
            ";

            await using var connection = _dbContext.Database.GetDbConnection();

            if (connection is not NpgsqlConnection npgsqlConnection)
                throw new InvalidOperationException("Expected NpgsqlConnection");

            await npgsqlConnection.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, npgsqlConnection);

            cmd.Parameters.AddWithValue("queue", (object?)f.QueueName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fromDate", (object?)f.FromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("toDate", (object?)f.ToDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("size", f.Size);
            cmd.Parameters.AddWithValue("offset", offset);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (total == 0 && reader["total_count"] != DBNull.Value)
                    total = Convert.ToInt32(reader["total_count"]);

                var rawJson = reader["message_body"]?.ToString();

                string status = "";
                string? errorMessage = null;
                string? failedType = null;

                if (!string.IsNullOrEmpty(rawJson))
                {
                    try
                    {
                        var json = System.Text.Json.JsonDocument.Parse(rawJson);

                        var root = json.RootElement;

                        status = root.GetProperty("Status").GetString() ?? "";

                        // FAILED case
                        if (status == "FAILED")
                        {
                            if (root.TryGetProperty("FailedType", out var ft))
                                failedType = ft.GetString();

                            if (root.TryGetProperty("StatusMsg", out var msg))
                            {
                                if (msg.ValueKind == System.Text.Json.JsonValueKind.Array && msg.GetArrayLength() > 0)
                                {
                                    var first = msg[0];
                                    if (first.TryGetProperty("ErrorMessage", out var err))
                                        errorMessage = err.GetString();
                                }
                                else if (msg.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    errorMessage = msg.GetString();
                                }
                            }
                        }
                    }
                    catch
                    {
                        // ignore malformed JSON
                    }
                }

                result.Add(new ConsumeAckLogDTO
                {
                    UniqueId = (Guid)reader["unique_id"],
                    MessageId = reader["message_id"] == DBNull.Value ? null : (Guid?)reader["message_id"],
                    PublishedMessageId = reader["published_message_id"] == DBNull.Value ? null : (Guid?)reader["published_message_id"],
                    QueueName = reader["queue_name"]?.ToString(),
                    Status = status,
                    ConsumedAt = reader["consumed_at"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("consumed_at")),
                    ErrorMessage = errorMessage,
                    FailedType = failedType,
                    MessageBody = reader["message_body"]?.ToString()
                });
            }

            return new PagedResponse<ConsumeAckLogDTO>
            {
                Data = result,
                Total = total,
                Page = f.Page,
                Size = f.Size
            };
        }

        public async Task<List<AckSummaryDTO>> GetAckSummaryAsync(DateTime from, DateTime to, string queue, string mode)
        {
            var result = new List<AckSummaryDTO>();

            var query = @"
            SELECT * FROM message_queue.message_queues_ack_summary(
                @fromDate::date,
                @toDate::date,
                @queue,
                @mode);";

            await using var connection = _dbContext.Database.GetDbConnection();

            if (connection is not NpgsqlConnection npgsqlConnection)
                throw new InvalidOperationException("Expected NpgsqlConnection");

            await npgsqlConnection.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, npgsqlConnection);

            cmd.Parameters.AddWithValue("fromDate", from.Date);
            cmd.Parameters.AddWithValue("toDate", to.Date);
            cmd.Parameters.AddWithValue("queue", queue ?? "ALL");
            cmd.Parameters.AddWithValue("mode", mode ?? "CONSUMER");

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new AckSummaryDTO
                {
                    Producer = reader["producer"]?.ToString(),
                    Consumer = reader["consumer"]?.ToString(),
                    QueueName = reader["queue_name"]?.ToString(),

                    TotalMessages = Convert.ToInt64(reader["total_messages"]),
                    AckReceived = Convert.ToInt64(reader["ack_rp"]),
                    AckPending = Convert.ToInt64(reader["ack_not_rp"]),

                    AckPercentage = Convert.ToDecimal(reader["ack_rp_percentage"]),
                    AckStatus = reader["ack_status"]?.ToString()
                });
            }

            return result;
        }
    }
}
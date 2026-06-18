using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.RbbitMQ;
using FluentValidation;
using FluentValidation.Results;
using RabbitMQ.Client;
using UserManagement.RbbitMQ;
using UserManagement.Models.MQueue;
using Npgsql;
using StackExchange.Redis;
using System.Text.Json;

namespace UserManagement.BAL.Services.MQueue
{
    public class SlsAgencyProcessor : IMessageProcessor<SlsAgencyModel>
    {
        private readonly ILogger<SlsAgencyProcessor> _logger;
        private readonly IValidator<SlsAgencyModel> _validator;
        private readonly string _connString;

        public SlsAgencyProcessor(
        ILogger<SlsAgencyProcessor> logger,
        IValidator<SlsAgencyModel> validator,
        IConfiguration config)
        {
            _logger = logger;
            _validator = validator;
            _connString = config.GetConnectionString("UserManagementDBConnection");
        }

        public async Task<ValidationResult> ValidateMessage(SlsAgencyModel message)
        {
            return await _validator.ValidateAsync(message);
        }

        public async Task ProcessMessage(SlsAgencyModel message, IReadOnlyBasicProperties mqBasicProperties)
           
        
        
        
        {
            _logger.LogInformation($"Processing order: {message}");
            string payloadJson = JsonSerializer.Serialize<SlsAgencyModel>(message);
            var a = 10;
            //try
            //{
            await using var connection = new NpgsqlConnection(_connString);
            await connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(
    @"SELECT * FROM master.insert_scope_from_payload_wbjit(@payload, @created_by);",
    connection
);

            // Payload (JSONB)
            cmd.Parameters.Add(
                new NpgsqlParameter("@payload", NpgsqlTypes.NpgsqlDbType.Jsonb)
                {
                    Value = payloadJson
                }
            );

            // created_by
            cmd.Parameters.Add(
                new NpgsqlParameter("@created_by", NpgsqlTypes.NpgsqlDbType.Bigint)
                {
                    Value = 14
                }
            );
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                bool IsDone = reader.GetBoolean(reader.GetOrdinal("is_done"));
                var Msg = reader.GetString(reader.GetOrdinal("response"));
                if (!IsDone)
                {
                    throw new Exception($"DB Function Failed: {Msg}");
                }
                await Task.CompletedTask;
            }



            //}
            //catch (Exception ex) {
            //    throw;
            //}
        }
    }
}
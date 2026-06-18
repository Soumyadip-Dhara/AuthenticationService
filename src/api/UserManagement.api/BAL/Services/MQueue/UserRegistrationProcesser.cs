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
    public class UserRegistrationProcesser : IMessageProcessor<UserRegistrationModel>
    {
        private readonly ILogger<UserRegistrationProcesser> _logger;
        private readonly IValidator<UserRegistrationModel> _validator;
        private readonly string _connString;

        public UserRegistrationProcesser(
        ILogger<UserRegistrationProcesser> logger,
        IValidator<UserRegistrationModel> validator,
        IConfiguration config)
        {
            _logger = logger;
            _validator = validator;
            _connString = config.GetConnectionString("UserManagementDBConnection");
        }

        public async Task<ValidationResult> ValidateMessage(UserRegistrationModel message)
        {
            return await _validator.ValidateAsync(message);
        }

        public async Task ProcessMessage(UserRegistrationModel message, IReadOnlyBasicProperties mqBasicProperties)
           
        
        
        
        {
            _logger.LogInformation($"Processing order: {message}");
            string payloadJson = JsonSerializer.Serialize<UserRegistrationModel>(message);
            //try
            //{
                await using var connection = new NpgsqlConnection(_connString);
                await connection.OpenAsync();
                await using var cmd = new NpgsqlCommand(
                    @"SELECT * FROM ""user"".migrate_wbjit_user(@payload::jsonb);",
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
                    var Msg = reader.GetString(reader.GetOrdinal("msg"));
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
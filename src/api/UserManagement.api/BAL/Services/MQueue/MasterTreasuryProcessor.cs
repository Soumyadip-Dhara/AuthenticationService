using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.MQueue;
using UserManagement.RbbitMQ;

namespace UserManagement.BAL.Services.MQueue
{
    public class MasterTreasuryProcessor : IMessageProcessor<MasterTreasuryConsumerPayload>
    {
        private readonly ILogger<MasterTreasuryProcessor> _logger;
        private readonly IValidator<MasterTreasuryConsumerPayload> _validator;
        private readonly IScopeRepository _scopeRepository;

        public MasterTreasuryProcessor(
            ILogger<MasterTreasuryProcessor> logger,
            IValidator<MasterTreasuryConsumerPayload> validator,            
            IScopeRepository scopeRepository
        )
        {
            _logger = logger;
            _validator = validator;
            _scopeRepository = scopeRepository;
        }

        public async Task<ValidationResult> ValidateMessage(MasterTreasuryConsumerPayload message)
        {
            return await _validator.ValidateAsync(message);
        }

        public async Task ProcessMessage(MasterTreasuryConsumerPayload message, IReadOnlyBasicProperties mqBasicProperties)
        {
            _logger.LogInformation($"Processing order: {message}");
            
            if (message.Data == null || string.IsNullOrWhiteSpace(message.Data.Code) || string.IsNullOrWhiteSpace(message.Data.TreasuryName) || string.IsNullOrWhiteSpace(message.Table))
            {
                _logger.LogWarning("Invalid message payload: missing required fields.");
                return;
            }

            await _scopeRepository.UpsertMasterTreasuryScopeAsync(message);

            _logger.LogInformation($"Successfully inserted/updated scope: {message.Data.TreasuryName} ({message.Data.Code}) mapped to level {message.Table}");
        }
    }
}

using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.MQueue;
using UserManagement.RbbitMQ;

namespace UserManagement.BAL.Services.MQueue
{
    public class MasterDdoProcessor : IMessageProcessor<MasterDdoConsumerPayload>
    {
        private readonly ILogger<MasterDdoProcessor> _logger;
        private readonly IValidator<MasterDdoConsumerPayload> _validator;
        private readonly IScopeRepository _scopeRepository;

        public MasterDdoProcessor(
            ILogger<MasterDdoProcessor> logger,
            IValidator<MasterDdoConsumerPayload> validator,
            IScopeRepository scopeRepository)
        {
            _logger = logger;
            _validator = validator;
            _scopeRepository = scopeRepository;
        }

        public async Task<ValidationResult> ValidateMessage(MasterDdoConsumerPayload message)
        {
            return await _validator.ValidateAsync(message);
        }

        public async Task ProcessMessage(MasterDdoConsumerPayload message, IReadOnlyBasicProperties mqBasicProperties)
        {
            _logger.LogInformation($"Processing DDO order: {message}");
            
            if (message.Data == null || string.IsNullOrWhiteSpace(message.Data.DdoCode) || string.IsNullOrWhiteSpace(message.Table))
            {
                _logger.LogWarning("Invalid DDO message payload: missing required fields.");
                return;
            }

            await _scopeRepository.UpsertMasterDdoScopeAsync(message);

            _logger.LogInformation($"Successfully inserted/updated DDO scope: {message.Data.Designation} ({message.Data.DdoCode}) mapped to level {message.Table}");
        }
    }
}

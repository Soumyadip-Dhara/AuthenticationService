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

namespace UserManagement.BAL.Services.MQueue
{
    public class AbcMessageProcesser : IMessageProcessor<abcmodel>
    {
        private readonly ILogger<AbcMessageProcesser> _logger;
        private readonly IValidator<abcmodel> _validator;
        //private readonly IBillDetailsRepository _billDetailsRepository;

        public AbcMessageProcesser(
        ILogger<AbcMessageProcesser> logger,
        IValidator<abcmodel> validator)
        //IBillDetailsRepository billDetailsRepository)
        {
            _logger = logger;
            _validator = validator;
            //_billDetailsRepository = billDetailsRepository;
        }

        public async Task<ValidationResult> ValidateMessage(abcmodel message)
        {
            return await _validator.ValidateAsync(message);
        }

        public async Task ProcessMessage(abcmodel message, IReadOnlyBasicProperties mqBasicProperties)
        {
            _logger.LogInformation($"Processing order: {message}");
            await Task.CompletedTask;
        }
    }
}
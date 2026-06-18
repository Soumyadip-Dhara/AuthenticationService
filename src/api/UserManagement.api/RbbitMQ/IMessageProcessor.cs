using FluentValidation.Results;
using RabbitMQ.Client;

namespace UserManagement.RbbitMQ
{
    public interface IMessageProcessor<T> where T : class
    {
        Task<ValidationResult> ValidateMessage(T message);
        Task ProcessMessage(T message, IReadOnlyBasicProperties mqBasicProperties);
    }
}
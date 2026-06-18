using FluentValidation;

namespace UserManagement.RbbitMQ.Validators
{
    public class MQueueAckValidator : AbstractValidator<AckPayloadModel>
    {
        public MQueueAckValidator()
        {
            RuleFor(x => x.MessageId).NotEmpty().WithMessage("messageId is required.");
            RuleFor(x => x.Status).NotEmpty().WithMessage("Status is required.");
            RuleFor(x => x.Timestamp).NotEmpty().WithMessage("DateTime is required.");
        }
    }
}

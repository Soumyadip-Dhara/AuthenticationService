using FluentValidation;
using UserManagement.Models.MQueue;

namespace UserManagement.BAL.Services.MQueue
{
    public class MasterTreasuryProcessorBase
    {
        private readonly IValidator<MasterTreasuryConsumerPayload> _validator;
    }
}
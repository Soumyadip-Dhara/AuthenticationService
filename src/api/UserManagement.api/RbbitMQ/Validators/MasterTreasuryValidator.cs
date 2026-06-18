using FluentValidation;
using Microsoft.EntityFrameworkCore.Storage;
using UserManagement.Models.MQueue;

namespace UserManagement.RbbitMQ.Validators
{
    public class MasterTreasuryValidator : AbstractValidator<MasterTreasuryConsumerPayload>
    {
        public MasterTreasuryValidator()
        {
        }
    }
}

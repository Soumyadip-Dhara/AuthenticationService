using FluentValidation;
using UserManagement.Models.MQueue;

namespace UserManagement.RbbitMQ.Validators
{
    public class MasterDdoValidator : AbstractValidator<MasterDdoConsumerPayload>
    {
        public MasterDdoValidator()
        {
        }
    }
}

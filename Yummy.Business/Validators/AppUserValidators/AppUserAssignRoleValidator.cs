using FluentValidation;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Business.Validators.AppUserValidators
{
    public class UserAssignRoleValidator : AbstractValidator<AppUserAssignRoleDto>
    {
        public UserAssignRoleValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Kullanıcı kimliği (Id) boş olamaz.");

            RuleFor(x => x.RoleNames)
                .NotNull().WithMessage("Rol listesi null olamaz.");

            RuleForEach(x => x.RoleNames)
                .NotEmpty().WithMessage("Rol adları boş bırakılamaz.");
        }
    }
}
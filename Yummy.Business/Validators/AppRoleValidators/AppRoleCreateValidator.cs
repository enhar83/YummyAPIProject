using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.AppRoleDTOs;

namespace Yummy.Business.Validators.AppRoleValidators
{
    public class AppRoleCreateValidator : AbstractValidator<AppRoleCreateDto>
    {
        public AppRoleCreateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Rol adı boş bırakılamaz.")
                .MinimumLength(3).WithMessage("Rol adı en az 3 karakter olmalıdır.")
                .MaximumLength(50).WithMessage("Rol adı en fazla 50 karakter olabilir.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Rol açıklaması boş bırakılamaz.")
                .MinimumLength(5).WithMessage("Rol açıklaması en az 5 karakter olmalıdır.")
                .MaximumLength(200).WithMessage("Rol açıklaması en fazla 200 karakter olabilir.");
        }
    }
}

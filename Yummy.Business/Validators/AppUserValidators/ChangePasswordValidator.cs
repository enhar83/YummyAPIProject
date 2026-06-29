using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Business.Validators.AppUserValidators
{
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage("Eski şifre boş geçilemez.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Yeni şifre boş geçilemez.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.")
                .NotEqual(x => x.OldPassword).WithMessage("Eski şifre yeni şifre ile aynı olamaz.");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Şifre tekrarı boş geçilemez.")
                .Equal(x => x.NewPassword).WithMessage("Girdiğiniz şifreler birbiriyle uyuşmuyor.");
        }
    }
}

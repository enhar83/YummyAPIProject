using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Business.Validators.AppUserValidators
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-posta adresi boş geçilemez.")
                .EmailAddress().WithMessage("Lütfen geçerli bir e-posta adresi giriniz.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Güvenlik anahtarı (Token) eksik.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Yeni şifre boş geçilemez.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Şifre tekrarı boş geçilemez.")
                .Equal(x => x.NewPassword).WithMessage("Girdiğiniz şifreler birbiriyle uyuşmuyor.");
        }
    }
}

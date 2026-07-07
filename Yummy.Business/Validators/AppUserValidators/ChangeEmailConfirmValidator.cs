using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Business.Validators.AppUserValidators
{
    public class ChangeEmailConfirmValidator : AbstractValidator<ChangeEmailConfirmDto>
    {
        public ChangeEmailConfirmValidator()
        {
            RuleFor(x => x.NewEmail)
                .NotEmpty().WithMessage("E-posta alanı boş bırakılamaz.")
                .EmailAddress().WithMessage("Lütfen geçerli bir e-posta adresi girin.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Doğrulama kodu boş bırakılamaz.");
        }
    }
}

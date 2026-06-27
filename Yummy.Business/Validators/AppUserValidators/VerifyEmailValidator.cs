using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Business.Validators.AppUserValidators
{
    public class VerifyEmailValidator : AbstractValidator<VerifyEmailDto>
    {
        public VerifyEmailValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-posta adresi boş geçilemez.")
                .EmailAddress().WithMessage("Lütfen geçerli bir e-posta adresi giriniz.");

            RuleFor(x => x.ActivationCode)
                .NotEmpty().WithMessage("Aktivasyon kodu boş geçilemez.")
                .Length(6).WithMessage("Aktivasyon kodu tam olarak 6 karakter olmalıdır.");
        }
    }
}

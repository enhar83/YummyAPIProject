using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Business.Validators.AppUserValidators
{
    public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordDto>
    {
        public ForgotPasswordValidator()
        {
            RuleFor(x => x.Email)
               .NotEmpty().WithMessage("E-posta adresi boş geçilemez.")
               .EmailAddress().WithMessage("Lütfen geçerli bir e-posta adresi giriniz.");
        }
    }
}

using FluentValidation;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Business.Validators.AppUserValidators
{
    public class ResendActivationCodeValidator : AbstractValidator<ResendActivationCodeDto>
    {
        public ResendActivationCodeValidator()
        {
            RuleFor(x => x.Email)
               .NotEmpty().WithMessage("E-posta adresi boş geçilemez.")
               .EmailAddress().WithMessage("Lütfen geçerli bir e-posta adresi giriniz.");
        }
    }
}

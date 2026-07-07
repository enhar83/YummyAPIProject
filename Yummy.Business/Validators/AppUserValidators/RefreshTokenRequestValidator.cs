using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Business.Validators.AppUserValidators
{
    public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequestDto>
    {
        public RefreshTokenRequestValidator()
        {
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithMessage("Eski veya süresi dolmuş Access Token alanı boş geçilemez.")
                .NotNull().WithMessage("Access Token mutlaka gönderilmelidir.");

            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Yenileme anahtarı (Refresh Token) alanı boş geçilemez.")
                .NotNull().WithMessage("Refresh Token mutlaka gönderilmelidir.");
        }
    }
}

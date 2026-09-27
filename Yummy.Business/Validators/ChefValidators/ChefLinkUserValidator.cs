using FluentValidation;
using Yummy.Core.DTOs.ChefDTOs;

namespace Yummy.Business.Validators.ChefValidators
{
    public class ChefLinkUserValidator : AbstractValidator<ChefLinkUserDto>
    {
        public ChefLinkUserValidator()
        {
            // boş Guid (00000000-...) ile gelen istek manager'a ulaşmadan reddedilir.
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Bağlanacak kullanıcının kimliği (Id) boş olamaz.");
        }
    }
}

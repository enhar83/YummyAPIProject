using FluentValidation;
using Yummy.Business.Validators.CommonValidators;
using Yummy.Core.DTOs.IngredientRequestDTOs;

namespace Yummy.Business.Validators.IngredientRequestValidators
{
    public class IngredientRequestQueryValidator : AbstractValidator<IngredientRequestQueryDto>
    {
        public IngredientRequestQueryValidator()
        {
            // sayfalama kuralları tüm listelerde ortaktır.
            Include(new PaginationQueryValidator());

            RuleFor(x => x.Status)
                .IsInEnum().When(x => x.Status.HasValue).WithMessage("Geçerli bir talep durumu seçiniz.");
        }
    }
}

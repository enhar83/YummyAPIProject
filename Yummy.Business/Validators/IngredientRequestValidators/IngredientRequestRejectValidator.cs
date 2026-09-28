using FluentValidation;
using Yummy.Core.DTOs.IngredientRequestDTOs;

namespace Yummy.Business.Validators.IngredientRequestValidators
{
    public class IngredientRequestRejectValidator : AbstractValidator<IngredientRequestRejectDto>
    {
        public IngredientRequestRejectValidator()
        {
            // şef talebinin neden reddedildiğini bilmelidir.
            RuleFor(x => x.Note)
                .NotEmpty().WithMessage("Red gerekçesi boş bırakılamaz.")
                .MaximumLength(500).WithMessage("Red gerekçesi en fazla 500 karakter olabilir.");
        }
    }
}

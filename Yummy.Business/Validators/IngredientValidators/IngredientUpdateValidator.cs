using FluentValidation;
using Yummy.Core.DTOs.IngredientDTOs;

namespace Yummy.Business.Validators.IngredientValidators
{
    public class IngredientUpdateValidator : AbstractValidator<IngredientUpdateDto>
    {
        public IngredientUpdateValidator()
        {
            RuleFor(x => x.IngredientId)
                .NotEmpty().WithMessage("Geçerli bir malzeme ID'si giriniz.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Malzeme adı boş bırakılamaz.")
                .MaximumLength(100).WithMessage("Malzeme adı en fazla 100 karakter olabilir.");

            RuleFor(x => x.Unit)
                .IsInEnum().WithMessage("Geçerli bir birim seçiniz.");
        }
    }
}

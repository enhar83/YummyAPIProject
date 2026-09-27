using FluentValidation;
using Yummy.Core.DTOs.IngredientDTOs;

namespace Yummy.Business.Validators.IngredientValidators
{
    public class IngredientCreateValidator : AbstractValidator<IngredientCreateDto>
    {
        public IngredientCreateValidator()
        {
            // uzunluk sınırı veritabanındaki kolon uzunluğu ile aynıdır (IngredientConfiguration).
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Malzeme adı boş bırakılamaz.")
                .MaximumLength(100).WithMessage("Malzeme adı en fazla 100 karakter olabilir.");

            RuleFor(x => x.Unit)
                .IsInEnum().WithMessage("Geçerli bir birim seçiniz.");
        }
    }
}

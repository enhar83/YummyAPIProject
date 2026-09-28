using FluentValidation;
using Yummy.Core.DTOs.IngredientRequestDTOs;

namespace Yummy.Business.Validators.IngredientRequestValidators
{
    public class IngredientRequestCreateValidator : AbstractValidator<IngredientRequestCreateDto>
    {
        public const int MaxItems = 30;

        public IngredientRequestCreateValidator()
        {
            // Cascade.Stop: liste null gelirse sonraki kurallar (Count, Select) çalıştırılmaz.
            RuleFor(x => x.Items).Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Talepte en az bir malzeme bulunmalıdır.")
                .Must(items => items.Count <= MaxItems).WithMessage($"Bir talepte en fazla {MaxItems} malzeme bulunabilir.")
                // aynı malzeme iki satırda istenirse çalışan hangisine ne kadar tedarik edeceğini karıştırır; miktarlar tek satırda toplanmalıdır.
                .Must(items => items.Select(i => i.IngredientId).Distinct().Count() == items.Count).WithMessage("Aynı malzeme talepte birden fazla kez yer alamaz.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.IngredientId)
                    .NotEmpty().WithMessage("Geçerli bir malzeme seçiniz.");

                // veritabanındaki kolon ile aynı hassasiyet: decimal(18,3).
                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("İstenen miktar 0'dan büyük olmalıdır.")
                    .PrecisionScale(18, 3, true).WithMessage("Miktar en fazla 3 ondalık basamak içerebilir.");
            });

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Not en fazla 500 karakter olabilir.");
        }
    }
}

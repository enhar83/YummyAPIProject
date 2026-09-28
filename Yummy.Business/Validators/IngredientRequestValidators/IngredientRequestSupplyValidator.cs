using FluentValidation;
using Yummy.Core.DTOs.IngredientRequestDTOs;

namespace Yummy.Business.Validators.IngredientRequestValidators
{
    // satırların talep ile birebir eşleşmesi manager'da (güncel talep üzerinde) kontrol edilir.
    public class IngredientRequestSupplyValidator : AbstractValidator<IngredientRequestSupplyDto>
    {
        public IngredientRequestSupplyValidator()
        {
            // Cascade.Stop: liste null gelirse sonraki kurallar (Count, Select) çalıştırılmaz.
            RuleFor(x => x.Items).Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Tedarik edilen miktarlar boş bırakılamaz.")
                .Must(items => items.Select(i => i.IngredientRequestItemId).Distinct().Count() == items.Count).WithMessage("Aynı talep satırı birden fazla kez gönderilemez.")
                .Must(items => items.Any(i => i.SuppliedQuantity > 0)).WithMessage("En az bir malzeme için 0'dan büyük miktar girilmelidir. Hiçbir malzeme bulunamadıysa talebi reddediniz.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.IngredientRequestItemId)
                    .NotEmpty().WithMessage("Geçerli bir talep satırı seçiniz.");

                item.RuleFor(i => i.SuppliedQuantity)
                    .GreaterThanOrEqualTo(0).WithMessage("Tedarik edilen miktar negatif olamaz.")
                    .PrecisionScale(18, 3, true).WithMessage("Miktar en fazla 3 ondalık basamak içerebilir.");
            });

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Not en fazla 500 karakter olabilir.");
        }
    }
}

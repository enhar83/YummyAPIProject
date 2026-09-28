using FluentValidation;
using Yummy.Core.DTOs.IngredientDTOs;
using Yummy.Entity.Enums;

namespace Yummy.Business.Validators.IngredientValidators
{
    public class StockAdjustmentValidator : AbstractValidator<StockAdjustmentDto>
    {
        public StockAdjustmentValidator()
        {
            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Geçerli bir stok hareketi türü seçiniz.");

            // giriş ve fire miktarı 0'dan büyük olmalıdır; sayımda ise stoğun 0'a eşitlenmesi (hiç kalmadı) geçerli bir sonuçtur.
            // koşullu kurallar ayrı RuleFor ile yazılır: zincirdeki .When() önceki TÜM kurallara uygulandığı için tek zincirde koşullar birbirini iptal eder.
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalıdır.")
                .When(x => x.Type != StockAdjustmentType.CountCorrection);

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0).WithMessage("Sayılan miktar negatif olamaz.")
                .When(x => x.Type == StockAdjustmentType.CountCorrection);

            // veritabanındaki kolon ile aynı hassasiyet: decimal(18,3).
            RuleFor(x => x.Quantity)
                .PrecisionScale(18, 3, true).WithMessage("Miktar en fazla 3 ondalık basamak içerebilir.");

            // veritabanındaki kolon uzunluğu ile aynı sınır.
            RuleFor(x => x.Note)
                .MaximumLength(250).WithMessage("Not en fazla 250 karakter olabilir.");
        }
    }
}

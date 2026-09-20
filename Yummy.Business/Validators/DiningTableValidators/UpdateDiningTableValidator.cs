using FluentValidation;
using Yummy.Core.DTOs.DiningTableDTOs;

namespace Yummy.Business.Validators.DiningTableValidators
{
    public class UpdateDiningTableValidator : AbstractValidator<DiningTableUpdateDto>
    {
        public UpdateDiningTableValidator()
        {
            RuleFor(x => x.DiningTableId)
                .NotEmpty().WithMessage("Masa ID boş olamaz.");

            RuleFor(x => x.TableNo)
                .NotEmpty().WithMessage("Masa numarası boş olamaz.")
                .MaximumLength(50).WithMessage("Masa numarası 50 karakteri geçemez.");

            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Kapasite 0'dan büyük olmalıdır.");
        }
    }
}

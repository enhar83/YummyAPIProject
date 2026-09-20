using FluentValidation;
using Yummy.Core.DTOs.DiningTableDTOs;

namespace Yummy.Business.Validators.DiningTableValidators
{
    public class CreateDiningTableValidator : AbstractValidator<DiningTableCreateDto>
    {
        public CreateDiningTableValidator()
        {
            RuleFor(x => x.TableNo)
                .NotEmpty().WithMessage("Masa numarası boş olamaz.")
                .MaximumLength(50).WithMessage("Masa numarası 50 karakteri geçemez.");

            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Kapasite 0'dan büyük olmalıdır.");
        }
    }
}

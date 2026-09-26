using FluentValidation;
using Yummy.Core.DTOs.CommonDTOs;

namespace Yummy.Business.Validators.CommonValidators
{
    public class PaginationQueryValidator : AbstractValidator<PaginationQueryDto>
    {
        public PaginationQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası en az 1 olmalıdır.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, PaginationQueryDto.MaxPageSize).WithMessage($"Sayfa boyutu 1 ile {PaginationQueryDto.MaxPageSize} arasında olmalıdır.");
        }
    }
}

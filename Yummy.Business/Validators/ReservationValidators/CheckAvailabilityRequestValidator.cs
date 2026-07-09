using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.ReservationDTOs;

namespace Yummy.Business.Validators.ReservationValidators
{
    public class CheckAvailabilityRequestValidator : AbstractValidator<CheckAvailabilityRequestDto>
    {
        public CheckAvailabilityRequestValidator()
        {
            RuleFor(x => x.ReservationDate)
            .NotEmpty().WithMessage("Tarih seçimi zorunludur.")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Geçmiş bir tarih için uygunluk kontrolü yapılamaz.")
            .LessThanOrEqualTo(DateTime.Today.AddMonths(1)).WithMessage("En fazla 1 ay sonrasına uygunluk kontrolü yapabilirsiniz.");

            RuleFor(x => x.NumberOfGuests)
                .GreaterThan(0).WithMessage("Kişi sayısı en az 1 olmalıdır.")
                .LessThanOrEqualTo(20).WithMessage("20 kişiden kalabalık gruplar için lütfen restoranla telefon üzerinden iletişime geçiniz.");
        }
    }
}

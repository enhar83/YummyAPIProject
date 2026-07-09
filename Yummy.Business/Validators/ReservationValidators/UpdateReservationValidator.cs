using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.ReservationDTOs;

namespace Yummy.Business.Validators.ReservationValidators
{
    public class UpdateReservationValidator : AbstractValidator<ReservationUpdateDto>
    {
        public UpdateReservationValidator()
        {
            RuleFor(x => x.ReservationDate)
                .NotEmpty().WithMessage("Rezervasyon tarihi boş bırakılamaz.")
                .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Geçmiş bir tarihe rezervasyon yapılamaz.")
                .LessThanOrEqualTo(DateTime.Today.AddMonths(1)).WithMessage("En fazla 1 ay (30 gün) sonrasına rezervasyon yapabilirsiniz.");

            RuleFor(x => x.ReservationTime)
                .NotEmpty().WithMessage("Rezervasyon saati zorunludur.")
                .Matches(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$").WithMessage("Saat formatı HH:MM olmalıdır. (Örn: 19:30)")
                .Must(saat =>
                {
                    if (TimeSpan.TryParse(saat, out TimeSpan parsedTime))
                        return parsedTime >= new TimeSpan(9, 0, 0) && parsedTime <= new TimeSpan(21, 0, 0);
                    return false;
                }).WithMessage("Restoranımız 09:00 ile 23:00 saatleri arasında hizmet vermektedir. En son rezervasyon saati 21:00'dır.");

            RuleFor(x => x.NumberOfGuests)
                .GreaterThan(0).WithMessage("Kişi sayısı en az 1 olmalıdır.")
                .LessThanOrEqualTo(20).WithMessage("Tek seferde en fazla 20 kişilik rezervasyon yapılabilir.");
        }
    }
}

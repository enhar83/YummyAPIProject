using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.Extensions;
using Yummy.Core.DTOs.ReservationDTOs;

namespace Yummy.Business.Validators.ReservationValidators
{
    public class CheckAvailabilityRequestValidator : AbstractValidator<CheckAvailabilityRequestDto>
    {
        // tarih kontrolleri sunucunun değil restoranın yerel saatine göre yapılır.
        public CheckAvailabilityRequestValidator(TimeProvider timeProvider)
        {
            RuleFor(x => x.ReservationDate)
            .NotEmpty().WithMessage("Tarih seçimi zorunludur.")
            .Must(date => date.Date >= timeProvider.GetLocalToday()).WithMessage("Geçmiş bir tarih için uygunluk kontrolü yapılamaz.")
            .Must(date => date.Date <= timeProvider.GetLocalToday().AddMonths(1)).WithMessage("En fazla 1 ay sonrasına uygunluk kontrolü yapabilirsiniz.");

            RuleFor(x => x.ReservationTime)
                .NotEmpty().WithMessage("Rezervasyon saati zorunludur.")
                .Matches(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$").WithMessage("Saat formatı HH:MM olmalıdır. (Örn: 19:30)");

            RuleFor(x => x.ReservationEndTime)
                .NotEmpty().WithMessage("Rezervasyon bitiş saati zorunludur.")
                .Matches(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$").WithMessage("Saat formatı HH:MM olmalıdır. (Örn: 21:30)")
                .Must((dto, bitisSaati) =>
                {
                    if (TimeSpan.TryParse(dto.ReservationTime, out TimeSpan baslangic) && TimeSpan.TryParse(bitisSaati, out TimeSpan bitis))
                    {
                        var fark = bitis - baslangic;
                        return bitis <= new TimeSpan(23, 0, 0) && fark.TotalMinutes >= 30 && fark.TotalHours <= 4;
                    }
                    return false;
                }).WithMessage("Bitiş saati başlangıçtan sonra olmalı, en az 30 dakika ve en fazla 4 saat sürmelidir. Ayrıca en geç 23:00'da bitebilir.");

            RuleFor(x => x.NumberOfGuests)
                .GreaterThan(0).WithMessage("Kişi sayısı en az 1 olmalıdır.")
                .LessThanOrEqualTo(20).WithMessage("20 kişiden kalabalık gruplar için lütfen restoranla telefon üzerinden iletişime geçiniz.");
        }
    }
}

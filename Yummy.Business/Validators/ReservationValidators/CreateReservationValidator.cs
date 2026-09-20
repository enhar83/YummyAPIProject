using System.Threading;
﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.ReservationDTOs;

namespace Yummy.Business.Validators.ReservationValidators
{
    public class CreateReservationValidator : AbstractValidator<ReservationCreateDto>
    {
        public CreateReservationValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ad alanı boş bırakılamaz.");

            RuleFor(x => x.Surname)
                .NotEmpty().WithMessage("Soyad alanı boş bırakılamaz.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-posta boş bırakılamaz.")
                .EmailAddress().WithMessage("Lütfen geçerli bir e-posta adresi girin.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Telefon numarası zorunludur.");

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
                }).WithMessage("Restoranımız 09:00 ile 23:00 saatleri arasında hizmet vermektedir. En erken rezervasyon saati 09:00'dır.");

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
                .LessThanOrEqualTo(20).WithMessage("Tek seferde en fazla 20 kişilik rezervasyon yapılabilir.");
        }
    }
}

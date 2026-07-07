using System;
using System.Collections.Generic;
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
                .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Geçmiş bir tarihe rezervasyon yapılamaz.");

            RuleFor(x => x.ReservationTime)
                .NotEmpty().WithMessage("Rezervasyon saati zorunludur.")
                .Matches(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$").WithMessage("Saat formatı HH:MM olmalıdır. (Örn: 19:30)");

            RuleFor(x => x.NumberOfGuests)
                .GreaterThan(0).WithMessage("Kişi sayısı en az 1 olmalıdır.")
                .LessThanOrEqualTo(20).WithMessage("Tek seferde en fazla 20 kişilik rezervasyon yapılabilir.");
        }
    }
}

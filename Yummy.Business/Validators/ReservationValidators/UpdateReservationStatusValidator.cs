using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Entity.Enums;

namespace Yummy.Business.Validators.ReservationValidators
{
    public class UpdateReservationStatusValidator : AbstractValidator<UpdateReservationStatusDto>
    {
        public UpdateReservationStatusValidator()
        {
            RuleFor(x => x.ReservationId)
            .NotEmpty().WithMessage("Geçerli bir rezervasyon ID'si giriniz.");

            RuleFor(x => x.ReservationStatus)
                .IsInEnum().WithMessage("Geçerli bir rezervasyon durumu seçiniz.")
                .Must(status => status != ReservationStatus.Completed)
                .WithMessage("Tamamlandı durumu manuel olarak güncellenemez. Sistem tarafından otomatik atanır.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.ContactDTOs;

namespace Yummy.Business.Validators.ContactValidators
{
    public class ContactCreateValidator : AbstractValidator<ContactCreateDto>
    {
        public ContactCreateValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-posta adresi boş geçilemez.")
                .EmailAddress().WithMessage("Lütfen geçerli bir e-posta adresi formatı giriniz.")
                .MaximumLength(100).WithMessage("E-posta adresi en fazla 100 karakter olabilir.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Telefon numarası boş geçilemez.")
                .MaximumLength(20).WithMessage("Telefon numarası en fazla 20 karakter olabilir.");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Adres bilgisi boş geçilemez.")
                .MaximumLength(1000).WithMessage("Adres bilgisi en fazla 1000 karakter olabilir.");

            RuleFor(x => x.MapLocation)
                .NotEmpty().WithMessage("Harita konumu (URL/Iframe) boş geçilemez.");

            RuleFor(x => x.OpenHours)
                .NotEmpty().WithMessage("Çalışma saatleri boş geçilemez.")
                .Matches(@"^([01][0-9]|2[0-3])[\.:][0-5][0-9]\s*-\s*([01][0-9]|2[0-3])[\.:][0-5][0-9]$")
                .WithMessage("Çalışma saatleri '08.00 - 23.00' veya '08:00 - 23:00' formatında olmalıdır.");
        }
    }
}

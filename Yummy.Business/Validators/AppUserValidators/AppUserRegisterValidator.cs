using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Business.Validators.AppUserValidators
{
    public class AppUserRegisterValidator : AbstractValidator<AppUserRegisterDto>
    {
        public AppUserRegisterValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ad alanı boş geçilemez.")
                .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir.");

            RuleFor(x => x.Surname)
                .NotEmpty().WithMessage("Soyad alanı boş geçilemez.")
                .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir.");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Kullanıcı adı boş geçilemez.")
                .MaximumLength(50).WithMessage("Kullanıcı adı en fazla 50 karakter olabilir.")
                .Matches(@"^[a-zA-Z0-9_\-\.]+$").WithMessage("Kullanıcı adı sadece harf, rakam, alt çizgi, tire ve nokta içerebilir.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-posta adresi boş geçilemez.")
                .EmailAddress().WithMessage("Lütfen geçerli bir e-posta adresi giriniz.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre boş geçilemez.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Şifreler birbiriyle eşleşmiyor.");
        }
    }
}

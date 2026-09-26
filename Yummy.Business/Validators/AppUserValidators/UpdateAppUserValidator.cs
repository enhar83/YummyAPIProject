using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Business.Validators.AppUserValidators
{
    public class UpdateAppUserValidator : AbstractValidator<UpdateAppUserDto>
    {
        public UpdateAppUserValidator()
        {
            // kurallar AppUserRegisterValidator ile aynıdır; kayıtta kabul edilmeyen bir değer profil güncellemede de kabul edilmez.
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ad alanı boş bırakılamaz.")
                .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir.");

            RuleFor(x => x.Surname)
                .NotEmpty().WithMessage("Soyad alanı boş bırakılamaz.")
                .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir.");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Kullanıcı adı boş bırakılamaz.")
                .MaximumLength(50).WithMessage("Kullanıcı adı en fazla 50 karakter olabilir.")
                .Matches(@"^[a-zA-Z0-9_\-\.]+$").WithMessage("Kullanıcı adı sadece harf, rakam, alt çizgi, tire ve nokta içerebilir.");

            RuleFor(x => x.Image)
                .Must(IsSupportedExtension).WithMessage("Sadece .jpg, .jpeg veya .png formatında resim yükleyebilirsiniz.")
                .Must(IsUnderMaxSize).WithMessage("Yüklenen resim 2MB'dan büyük olamaz.")
                .When(x => x.Image != null);
        }

        private bool IsSupportedExtension(IFormFile? file)
        {
            if (file == null) return true;
            var extension = Path.GetExtension(file.FileName).ToLower();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png";
        }

        private bool IsUnderMaxSize(IFormFile? file)
        {
            if (file == null) return true;
            var maxSizeInBytes = 2 * 1024 * 1024;
            return file.Length <= maxSizeInBytes;
        }
    }
}

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
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ad alanı boş bırakılamaz.");

            RuleFor(x => x.Surname)
                .NotEmpty().WithMessage("Soyad alanı boş bırakılamaz.");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Kullanıcı adı boş bırakılamaz.");

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Yummy.Core.DTOs.ChefDTOs;

namespace Yummy.Business.Validators.ChefValidators
{
    public class ChefUpdateValidator : AbstractValidator<ChefUpdateDto>
    {
        public ChefUpdateValidator()
        {
            RuleFor(x => x.ChefId).NotEmpty().WithMessage("Şef ID'si gereklidir.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Şef adı boş geçilemez.")
                .MinimumLength(2).WithMessage("Şef adı en az 2 karakter olmalıdır.");

            RuleFor(x => x.Surname)
                .NotEmpty().WithMessage("Şef soyadı boş geçilemez.")
                .MinimumLength(2).WithMessage("Şef soyadı en az 2 karakter olmalıdır.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Unvan boş geçilemez.")
                .MaximumLength(50).WithMessage("Unvan en fazla 50 karakter olabilir.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Açıklama boş geçilemez.")
                .MinimumLength(10).WithMessage("Açıklama en az 10 karakter olmalıdır.");

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

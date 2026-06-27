using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Yummy.Core.DTOs.FeatureDTOs;

namespace Yummy.Business.Validators.FeatureValidators
{
    public class FeatureUpdateValidator : AbstractValidator<FeatureUpdateDto>
    {
        public FeatureUpdateValidator()
        {
            RuleFor(x => x.FeatureId).NotEmpty().WithMessage("Özellik ID'si gereklidir.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Başlık boş geçilemez.")
                .MaximumLength(150).WithMessage("Başlık en fazla 150 karakter olabilir.");

            RuleFor(x => x.SubTitle)
                .NotEmpty().WithMessage("Alt başlık boş geçilemez.")
                .MaximumLength(250).WithMessage("Alt başlık en fazla 250 karakter olabilir.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Açıklama boş geçilemez.")
                .MaximumLength(1000).WithMessage("Açıklama en fazla 1000 karakter olabilir.");

            RuleFor(x => x.Image)
                .Must(IsSupportedExtension).WithMessage("Sadece .jpg, .jpeg veya .png formatında resim yükleyebilirsiniz.")
                .Must(IsUnderMaxSize).WithMessage("Yüklenen resim 2MB'dan büyük olamaz.")
                .When(x => x.Image != null);

            RuleFor(x => x.VideoUrl)
                .NotEmpty().WithMessage("Video bağlantısı boş geçilemez.")
                .Must(BeAValidUrl).WithMessage("Lütfen geçerli bir web bağlantısı (URL) giriniz. (Örn: https://www.youtube.com/...)");
        }

        private bool BeAValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return true;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                          && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            return result;
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

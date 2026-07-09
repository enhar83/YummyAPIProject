using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.TestimonialDTOs;

namespace Yummy.Business.Validators.TestimonialValidators
{
    public class TestimonialCreateValidator : AbstractValidator<TestimonialCreateDto>
    {
        public TestimonialCreateValidator()
        {
            RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Lütfen yorumunuza bir başlık ekleyin.")
            .MaximumLength(50).WithMessage("Başlık en fazla 50 karakter olabilir.");

            RuleFor(x => x.Comment)
                .NotEmpty().WithMessage("Yorum alanı boş bırakılamaz.")
                .MinimumLength(10).WithMessage("Yorumunuz çok kısa, lütfen biraz daha detaylandırın.")
                .MaximumLength(500).WithMessage("Yorumunuz en fazla 500 karakter olabilir.");

            RuleFor(x => x.Rating)
                .InclusiveBetween((byte)1, (byte)5).WithMessage("Puanınız 1 ile 5 arasında bir değer olmalıdır.");
        }
    }
}

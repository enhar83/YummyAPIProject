using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.ProductDTOs;

namespace Yummy.Business.Validators.ProductValidators
{
    public class ProductUpdateValidator : AbstractValidator<ProductUpdateDto>
    {
        public ProductUpdateValidator() 
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Güncellenecek ürünün sistem kimliği (ID) eksik.");

            RuleFor(x => x.ProductName)
                .NotEmpty().WithMessage("Ürün adı boş geçilemez.")
                .MaximumLength(100).WithMessage("Ürün adı en fazla 100 karakter olabilir.");

            RuleFor(x => x.ProductDescription)
                .NotEmpty().WithMessage("Ürün açıklaması boş geçilemez.")
                .MaximumLength(500).WithMessage("Ürün açıklaması en fazla 500 karakter olabilir.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Ürün fiyatı 0'dan büyük olmalıdır.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Lütfen bir kategori seçiniz.");
        }
    }
}

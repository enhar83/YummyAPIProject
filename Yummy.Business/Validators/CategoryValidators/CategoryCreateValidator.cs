using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.CategoryDTOs;

namespace Yummy.Business.Validators.CategoryValidators
{
    public class CategoryCreateValidator : AbstractValidator<CategoryCreateDto>
    {
        public CategoryCreateValidator()
        {
            RuleFor(x => x.CategoryName)
            .NotEmpty().WithMessage("Kategori adı boş geçilemez.")
            .MinimumLength(3).WithMessage("Kategori adı en az 3 karakter olmalıdır.")
            .MaximumLength(50).WithMessage("Kategori adı en fazla 50 karakter olabilir.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Yummy.Core.DTOs.CategoryDTOs;

namespace Yummy.Business.Validators.CategoryValidators
{
    public class CategoryUpdateValidator : AbstractValidator<CategoryUpdateDto>
    {
        public CategoryUpdateValidator() 
        {
            RuleFor(x => x.CategoryId).NotEmpty();
            RuleFor(x => x.CategoryName)
                .NotEmpty().WithMessage("Kategori adı boş geçilemez.")
                .MinimumLength(3).WithMessage("Kategori adı en az 3 karakter olmalıdır.");
        }
    }
}

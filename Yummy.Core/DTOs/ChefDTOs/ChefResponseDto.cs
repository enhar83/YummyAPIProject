using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.DTOs.ChefDTOs
{
    public record ChefResponseDto
    {
        public Guid ChefId { get; init; }
        public string Name { get; init; } = null!;
        public string Surname { get; init; } = null!;
        public string Title { get; init; } = null!;
        public string Description { get; init; } = null!;
        public string? ImageUrl { get; init; }

        // şef profilinin bağlı olduğu kullanıcı hesabı. hesabı olmayan şeflerde boştur.
        public Guid? AppUserId { get; init; }
        public string? LinkedUserEmail { get; init; }
    }
}
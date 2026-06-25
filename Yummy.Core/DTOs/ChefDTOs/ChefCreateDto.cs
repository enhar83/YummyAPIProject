using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Yummy.Core.DTOs.ChefDTOs
{
    public record ChefCreateDto(string Name,
        string Surname,
        string Title,
        string Description,
        IFormFile? Image);
}

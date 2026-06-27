using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.AppUserDTOs;

namespace Yummy.Core.Services
{
    public interface IAppUserService
    {
        Task RegisterAsync(AppUserRegisterDto dto);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.ReservationDTOs;

namespace Yummy.Core.Services
{
    public interface IReservationService
    {
        Task AddReservationAsync(string userId, ReservationCreateDto dto);
    }
}

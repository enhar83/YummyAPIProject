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
        Task<IEnumerable<PastReservationByUserDto>> SeeMyPastReservationsAsync(string userId);
        Task CancelReservationAsync(string userId, Guid reservationId);
        Task UpdateReservationAsync(string userId, ReservationUpdateDto dto);
        Task<PastReservationByUserDto> GetUserReservationByIdAsync(string userId, Guid reservationId);
        Task<IEnumerable<ReservationListDto>> GetAllReservationsAsync();
        Task<ReservationListDto> GetReservationByIdAsync(Guid reservationId);
        Task UpdateReservationStatusAsync(UpdateReservationDto dto);
        Task<IEnumerable<ReservationListDto>> GetTodaysReservationListAsync();
    }
}

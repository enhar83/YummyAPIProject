using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.ReservationDTOs;

namespace Yummy.Core.Services
{
    public interface IReservationService
    {
        Task AddReservationAsync(string userId, ReservationCreateDto dto, CancellationToken cancellationToken = default);
        Task<IEnumerable<PastReservationByUserDto>> SeeMyPastReservationsAsync(string userId, CancellationToken cancellationToken = default);
        Task CancelReservationAsync(string userId, Guid reservationId, CancellationToken cancellationToken = default);
        Task UpdateReservationAsync(string userId, ReservationUpdateDto dto, CancellationToken cancellationToken = default);
        Task<PastReservationByUserDto> GetUserReservationByIdAsync(string userId, Guid reservationId, CancellationToken cancellationToken = default);
        Task<PagedResultDto<ReservationListDto>> GetAllReservationsAsync(PaginationQueryDto query, CancellationToken cancellationToken = default);
        Task<ReservationListDto> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken = default);
        Task UpdateReservationStatusAsync(UpdateReservationDto dto, CancellationToken cancellationToken = default);
        Task<IEnumerable<ReservationListDto>> GetTodaysReservationListAsync(CancellationToken cancellationToken = default);
        Task<CheckAvailabilityResponseDto> CheckAvailabilityAsync(CheckAvailabilityRequestDto dto, CancellationToken cancellationToken = default);
        Task<IEnumerable<TableStatusForMapDto>> GetTableStatusesForMapAsync(DateTime date, string time, string endTime, CancellationToken cancellationToken = default);

        // arka plan servisi tarafından periyodik olarak çağrılır. bitiş saati geçmiş onaylı rezervasyonları Completed,
        // onaylanmamış olanları Cancelled yapar ve ikincilere bilgilendirme e-postası gönderir. güncellenen rezervasyon sayısını döner.
        Task<int> ProcessPastReservationsAsync(CancellationToken cancellationToken = default);
    }
}

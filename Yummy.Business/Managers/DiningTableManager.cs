using AutoMapper;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Yummy.Core.DTOs.DiningTableDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Business.Managers
{
    public class DiningTableManager : IDiningTableService
    {
        private readonly IGenericRepository<DiningTable> _tableRepository;
        private readonly IGenericRepository<Reservation> _reservationRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public DiningTableManager(IGenericRepository<DiningTable> tableRepository, IGenericRepository<Reservation> reservationRepository, IUnitOfWork uow, IMapper mapper)
        {
            _tableRepository = tableRepository;
            _reservationRepository = reservationRepository;
            _uow = uow;
            _mapper = mapper;
        }

        public async Task AddAsync(DiningTableCreateDto dto, CancellationToken cancellationToken = default)
        {
            var table = _mapper.Map<DiningTable>(dto);
            await _tableRepository.AddAsync(table, cancellationToken);
            await _uow.SaveAsync(cancellationToken);
        }

        public async Task<IEnumerable<DiningTableListDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var tables = await _tableRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<DiningTableListDto>>(tables);
        }

        public async Task<DiningTableListDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var table = await _tableRepository.GetByIdAsync(id, cancellationToken);
            if (table == null)
                throw new LogicException("NotFound", "Masa bulunamadı.");

            return _mapper.Map<DiningTableListDto>(table);
        }

        public async Task UpdateAsync(DiningTableUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var table = await _tableRepository.GetByIdAsync(dto.DiningTableId, cancellationToken);
            if (table == null)
                throw new LogicException("NotFound", "Güncellenecek masa bulunamadı.");

            // bugün veya ileri tarihli aktif (Pending/Approved) rezervasyonu olan masa pasife alınamaz; aksi halde bu rezervasyonlar kullanılamayan bir masada kalır.
            // geçmiş, tamamlanmış veya iptal edilmiş rezervasyonlar engel değildir ve masa pasife alındıktan sonra da kullanıcıların listelerinde görünmeye devam eder.
            if (table.IsActive && !dto.IsActive)
            {
                var hasActiveReservations = await _reservationRepository.AnyAsync(r => r.DiningTableId == table.DiningTableId &&
                    r.ReservationDate >= DateTime.Today &&
                    (r.ReservationStatus == ReservationStatus.Pending || r.ReservationStatus == ReservationStatus.Approved), cancellationToken);

                if (hasActiveReservations)
                    throw new LogicException("TableHasActiveReservations", "Bu masaya ait bekleyen veya onaylanmış rezervasyonlar bulunduğu için masa pasife alınamaz. Önce ilgili rezervasyonları iptal ediniz.");
            }

            _mapper.Map(dto, table);
            _tableRepository.Update(table);
            await _uow.SaveAsync(cancellationToken);
        }
    }
}

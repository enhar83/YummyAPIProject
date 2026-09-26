using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Extensions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Business.Managers
{
    // eşzamanlılık kuralı: bir rezervasyon satırını değiştiren her işlem (oluşturma, güncelleme, iptal, admin durum değişikliği, arka plan servisi)
    // rezervasyonun gününe ait kilidi alır ve kaydı kilidin İÇİNDE yeniden okur. böylece bir işlem, diğerinin yaptığı değişikliği eski bir kopya ile ezemez.
    // kilit sırası her yerde aynıdır: önce kullanıcı kilidi (sadece oluşturmada), sonra gün kilitleri tarih sırasıyla. bu sıra deadlock oluşmasını engeller.
    public class ReservationManager : IReservationService
    {
        private readonly IGenericRepository<Reservation> _reservationRepository;
        private readonly IGenericRepository<DiningTable> _tableRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReservationManager> _logger;
        private readonly TimeProvider _timeProvider; // .net'in saat sınıfıdır. 

        private const int MinHoursBeforeChange = 2; // iptal edememe sınırı
        public const int MaxActiveReservationsPerUser = 3; // kişi başı approved/pending rezervasyon limiti

        public ReservationManager(IGenericRepository<Reservation> reservationRepository, IGenericRepository<DiningTable> tableRepository, IUnitOfWork uow, IMapper mapper, IEmailService emailService, ILogger<ReservationManager> logger, TimeProvider timeProvider)
        {
            _reservationRepository = reservationRepository;
            _tableRepository = tableRepository;
            _uow = uow;
            _mapper = mapper;
            _emailService = emailService;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        public async Task AddReservationAsync(string userId, ReservationCreateDto dto, CancellationToken cancellationToken = default)
        {
            var reservation = _mapper.Map<Reservation>(dto);

            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz veya doğrulanamadı.");

            reservation.AppUserId = parsedUserId;

            var targetDate = dto.ReservationDate.Date;
            var (reqStart, reqEnd) = ParseTimeRange(dto.ReservationTime, dto.ReservationEndTime);

            if (targetDate.Add(reqStart) < _timeProvider.GetLocalDateTime())
                throw new LogicException("PastReservation", "Geçmiş bir tarihe veya saate rezervasyon yapılamaz.");

            DiningTable selectedTable = null!;

            // müsaitlik ve kullanıcı limiti kontrolleri kayıt ile aynı kilitler altında yapılır; aksi halde eşzamanlı istekler aynı masayı boş görüp
            // ikisi de kaydedebilir veya kullanıcı farklı günlere aynı anda istek atarak aktif rezervasyon limitini aşabilir.
            await _uow.ExecuteInLockedTransactionAsync(new[] { GetUserLockKey(parsedUserId), GetDateLockKey(targetDate) }, async () =>
            {
                var today = _timeProvider.GetLocalToday();
                var activeReservations = await _reservationRepository.GetWhereAsync(r => r.AppUserId == parsedUserId && r.ReservationDate >= today &&
                    (r.ReservationStatus == ReservationStatus.Pending || r.ReservationStatus == ReservationStatus.Approved), cancellationToken);
                if (activeReservations.Count() >= MaxActiveReservationsPerUser)
                    throw new LogicException("ReservationLimit", $"Aynı anda en fazla {MaxActiveReservationsPerUser} aktif rezervasyonunuz olabilir. Yeni rezervasyon için mevcut rezervasyonlarınızdan birini iptal edebilirsiniz.");

                var availableTables = await GetAvailableTablesAsync(targetDate, reqStart, reqEnd, dto.NumberOfGuests, null, cancellationToken);

                if (dto.SelectedTableId.HasValue)
                {
                    selectedTable = availableTables.FirstOrDefault(t => t.DiningTableId == dto.SelectedTableId.Value)
                        ?? throw new LogicException("TableNotAvailable", "Seçtiğiniz masa istenilen saat aralığında uygun değil veya kapasitesi yetersiz.");
                }
                else
                {
                    selectedTable = availableTables.FirstOrDefault()
                        ?? throw new LogicException("NoTable", "Seçtiğiniz tarih ve saat aralığında kişi sayınıza uygun boş masamız bulunmamaktadır.");
                }

                reservation.DiningTableId = selectedTable.DiningTableId;

                await _reservationRepository.AddAsync(reservation, cancellationToken);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);

            await TrySendEmailAsync(reservation, "ReservationReceivedTemplate.html", "Yummy Restoran - Rezervasyon Talebiniz Alındı", new Dictionary<string, string>
            {
                ["{{Name}}"] = reservation.Name,
                ["{{Surname}}"] = reservation.Surname,
                ["{{Date}}"] = reservation.ReservationDate.ToString("dd.MM.yyyy"),
                ["{{Time}}"] = reservation.ReservationTime,
                ["{{Guests}}"] = reservation.NumberOfGuests.ToString(),
                ["{{Phone}}"] = reservation.Phone,
                ["{{TableNo}}"] = selectedTable.TableNo,
                ["{{Location}}"] = selectedTable.Location ?? "Belirtilmemiş"
            });
        }

        // kullanıcının kendi rezervasyonunu iptal etmesi. sahiplik kilitten önce, durum ve süre kontrolleri kilit içinde güncel kayıt üzerinde yapılır.
        // iptal edilmiş/tamamlanmış rezervasyon ve saatine 2 saatten az kalan rezervasyon iptal edilemez.
        public async Task CancelReservationAsync(string userId, Guid reservationId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            // kilidin hangi güne alınacağını bilmek için önce okunur; asıl kontroller kilit içerisinde güncel kayıt üzerinde yapılır.
            var snapshot = await _reservationRepository.GetSingleAsync(r => r.ReservationId == reservationId, cancellationToken)
                ?? throw new LogicException("NotFound", "Rezervasyon bulunamadı.");

            if (snapshot.AppUserId != parsedUserId)
                throw new LogicException("Forbidden", "Bu işlem için yetkiniz yok.");

            Reservation reservation = null!;

            await _uow.ExecuteInLockedTransactionAsync(GetDateLockKey(snapshot.ReservationDate), async () =>
            {
                reservation = await ReloadForUpdateAsync(snapshot, cancellationToken);

                if (reservation.ReservationStatus == ReservationStatus.Cancelled)
                    throw new LogicException("AlreadyCancelled", "Bu rezervasyon zaten daha önce iptal edilmiş.");

                if (reservation.ReservationStatus == ReservationStatus.Completed)
                    throw new LogicException("NotAllowed", "Tamamlanmış rezervasyonlar iptal edilemez.");

                if (!TryParseTime(reservation.ReservationTime, out TimeSpan reservationTime))
                    throw new LogicException("InvalidTime", "Rezervasyonun saat bilgisi geçersiz.");

                // saat ve dakika birlikte dikkate alınır (19:59'luk bir rezervasyon 19:00 gibi hesaplanmaz).
                if (reservation.ReservationDate.Date.Add(reservationTime) <= _timeProvider.GetLocalDateTime().AddHours(MinHoursBeforeChange))
                    throw new LogicException("TooLate", $"Rezervasyon saatinize {MinHoursBeforeChange} saatten az kaldığı için iptal işlemi yapılamaz.");

                reservation.ReservationStatus = ReservationStatus.Cancelled;
                _reservationRepository.Update(reservation);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);

            var table = await _tableRepository.GetByIdAsync(reservation.DiningTableId, cancellationToken);
            await TrySendEmailAsync(reservation, "ReservationCancelledTemplate.html", "Yummy Restoran - Rezervasyonunuz İptal Edildi", new Dictionary<string, string>
            {
                ["{{Name}}"] = reservation.Name,
                ["{{Surname}}"] = reservation.Surname,
                ["{{Date}}"] = reservation.ReservationDate.ToString("dd.MM.yyyy"),
                ["{{Time}}"] = reservation.ReservationTime,
                ["{{Guests}}"] = reservation.NumberOfGuests.ToString(),
                ["{{TableNo}}"] = table?.TableNo ?? "",
                ["{{Location}}"] = table?.Location ?? "Belirtilmemiş"
            });
        }

        // giriş gerektirmez. kişi sayısına yeten, aktif ve verilen saat aralığında boş masaları döner. sadece okuma yaptığı için kilit almaz.
        public async Task<CheckAvailabilityResponseDto> CheckAvailabilityAsync(CheckAvailabilityRequestDto dto, CancellationToken cancellationToken = default)
        {
            var targetDate = dto.ReservationDate.Date;
            var (reqStart, reqEnd) = ParseTimeRange(dto.ReservationTime, dto.ReservationEndTime);

            var availableTables = await GetAvailableTablesAsync(targetDate, reqStart, reqEnd, dto.NumberOfGuests, null, cancellationToken);

            return new CheckAvailabilityResponseDto
            {
                ReservationDate = targetDate,
                IsFullyBooked = !availableTables.Any(),
                AvailableTables = availableTables.Select(t => new AvailableTableDto
                {
                    DiningTableId = t.DiningTableId,
                    TableNo = t.TableNo,
                    Capacity = t.Capacity
                }).ToList()
            };
        }

        // admin listesi. sayfalı döner; COUNT ve sayfa sorgusu veritabanında çalışır, tüm tablo belleğe alınmaz.
        public async Task<PagedResultDto<ReservationListDto>> GetAllReservationsAsync(PaginationQueryDto query, CancellationToken cancellationToken = default)
        {
            // en yeni tarihli rezervasyonlar önce gelir; aynı gün ve saatte birden fazla kayıt varsa sayfalar arasında kayma olmaması için id ile sabitlenir.
            var (entities, totalCount) = await _reservationRepository.GetPagedAsync(
                null,
                q => q.OrderByDescending(r => r.ReservationDate).ThenByDescending(r => r.ReservationTime).ThenBy(r => r.ReservationId),
                query.Page,
                query.PageSize,
                cancellationToken,
                x => x.DiningTable);

            return new PagedResultDto<ReservationListDto>
            {
                Items = _mapper.Map<List<ReservationListDto>>(entities),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ReservationListDto> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetWhereAsync(x => x.ReservationId == reservationId, cancellationToken, x => x.DiningTable);
            var reservation = _mapper.Map<IEnumerable<ReservationListDto>>(entities).FirstOrDefault();

            return reservation ?? throw new LogicException("NotFound", "Rezervasyon bulunamadı.");
        }

        public async Task<IEnumerable<ReservationListDto>> GetTodaysReservationListAsync(CancellationToken cancellationToken = default)
        {
            // aralık sorgusu: kayıtta saat kısmı olsa bile bugünün rezervasyonları kaçırılmaz ve ReservationDate üzerindeki index kullanılabilir.
            var today = _timeProvider.GetLocalToday();
            var tomorrow = today.AddDays(1);
            var entities = await _reservationRepository.GetWhereAsync(x => x.ReservationDate >= today && x.ReservationDate < tomorrow, cancellationToken, x => x.DiningTable);
            return _mapper.Map<IEnumerable<ReservationListDto>>(entities.OrderBy(x => x.ReservationTime));
        }

        public async Task<PastReservationByUserDto> GetUserReservationByIdAsync(string userId, Guid reservationId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            var entities = await _reservationRepository.GetWhereAsync(x => x.ReservationId == reservationId && x.AppUserId == parsedUserId, cancellationToken, x => x.DiningTable);
            var reservation = _mapper.Map<IEnumerable<PastReservationByUserDto>>(entities).FirstOrDefault();

            return reservation ?? throw new LogicException("NotFound", "Rezervasyon bulunamadı.");
        }

        // kullanıcının tüm rezervasyonları. masa pasife alınmış olsa bile masa bilgisiyle birlikte listelenir.
        public async Task<IEnumerable<PastReservationByUserDto>> SeeMyPastReservationsAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            var entities = await _reservationRepository.GetWhereAsync(x => x.AppUserId == parsedUserId, cancellationToken, x => x.DiningTable);

            // saat "HH:mm" formatında tutulduğu için metin sıralaması saat sıralamasıyla aynıdır.
            var sortedEntities = entities.OrderByDescending(x => x.ReservationDate).ThenByDescending(x => x.ReservationTime);
            return _mapper.Map<IEnumerable<PastReservationByUserDto>>(sortedEntities);
        }

        // kullanıcının kendi rezervasyonunu güncellemesi:
        // 1) rezervasyon okunur; eski ve yeni günün kilitleri tarih sırasıyla alınır ve kayıt kilit içinde yeniden okunur.
        // 2) iptal/tamamlanmış, geçmiş veya saatine 2 saatten az kalan rezervasyon güncellenemez; hiçbir alan değişmediyse NoChanges döner.
        // 3) tarih/saat/kişi sayısı değiştiyse yeni aralıkta masa aranır (mevcut masa uygunsa korunur).
        // 4) onaylı rezervasyon tekrar Pending olur; kayıt sonrası "güncellendi" e-postası gönderilir.
        public async Task UpdateReservationAsync(string userId, ReservationUpdateDto dto, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(userId, out Guid parsedUserId))
                throw new LogicException("InvalidUserId", "Kullanıcı kimliği geçersiz.");

            var snapshot = await _reservationRepository.GetSingleAsync(x => x.ReservationId == dto.ReservationId && x.AppUserId == parsedUserId, cancellationToken)
                ?? throw new LogicException("InvalidReservationId", "Güncellenmek istenen rezervasyon kimliği bulunamadı.");

            // tarih değişiyorsa hem eski hem yeni günün kilidi alınır: eski gündeki iptal/durum değişiklikleri ile yeni gündeki masa ataması korunur.
            var targetDate = dto.ReservationDate.Date;
            var lockKeys = new[] { snapshot.ReservationDate.Date, targetDate }
                .Distinct()
                .OrderBy(date => date)
                .Select(GetDateLockKey)
                .ToArray();

            Reservation reservation = null!;

            await _uow.ExecuteInLockedTransactionAsync(lockKeys, async () =>
            {
                reservation = await ReloadForUpdateAsync(snapshot, cancellationToken);

                if (reservation.ReservationStatus == ReservationStatus.Completed || reservation.ReservationStatus == ReservationStatus.Cancelled)
                    throw new LogicException("NotAllowed", "Tamamlanmış veya iptal edilmiş rezervasyonlar üzerinde güncelleme yapılamaz.");

                var now = _timeProvider.GetLocalDateTime();
                var currentStart = reservation.ReservationDate.Date;
                if (TryParseTime(reservation.ReservationTime, out TimeSpan parsedTime))
                    currentStart = currentStart.Add(parsedTime);

                if (currentStart < now)
                    throw new LogicException("PastReservation", "Geçmiş rezervasyonlarda herhangi bir değişiklik yapılamaz.");

                if (currentStart <= now.AddHours(MinHoursBeforeChange))
                    throw new LogicException("TooLate", $"Rezervasyonunuza {MinHoursBeforeChange} saatten az bir süre kaldığı için değişiklik yapılamaz.");

                string incomingMessage = dto.Message ?? string.Empty;

                bool isSlotChanged = reservation.ReservationDate.Date != targetDate ||
                    reservation.ReservationTime != dto.ReservationTime ||
                    reservation.ReservationEndTime != dto.ReservationEndTime ||
                    reservation.NumberOfGuests != dto.NumberOfGuests;

                // hiçbir alan değişmediyse kayıt ve e-posta işlemi yapılmaz; aksi halde kullanıcıya gereksiz yere "güncellendi" e-postası gider.
                if (!isSlotChanged && reservation.Message == incomingMessage)
                    throw new LogicException("NoChanges", "Rezervasyonunuzda herhangi bir değişiklik yapılmadı.");

                if (isSlotChanged)
                {
                    var (reqStart, reqEnd) = ParseTimeRange(dto.ReservationTime, dto.ReservationEndTime);

                    if (targetDate.Add(reqStart) < now)
                        throw new LogicException("PastReservation", "Geçmiş bir tarihe veya saate rezervasyon güncellenemez.");

                    var availableTables = await GetAvailableTablesAsync(targetDate, reqStart, reqEnd, dto.NumberOfGuests, reservation.ReservationId, cancellationToken);

                    // rezervasyonun mevcut masası yeni saat aralığında ve kişi sayısında hâlâ uygunsa korunur; değilse en küçük uygun masa atanır.
                    var selectedTable = availableTables.FirstOrDefault(t => t.DiningTableId == reservation.DiningTableId)
                        ?? availableTables.FirstOrDefault()
                        ?? throw new LogicException("NoTable", "Seçtiğiniz yeni tarih ve saat aralığında kişi sayınıza uygun boş masamız bulunmamaktadır.");

                    reservation.DiningTableId = selectedTable.DiningTableId;
                }

                // onaylanmış rezervasyonda yapılan her değişiklik tekrar onaya düşer.
                if (reservation.ReservationStatus == ReservationStatus.Approved)
                    reservation.ReservationStatus = ReservationStatus.Pending;
                _mapper.Map(dto, reservation);

                _reservationRepository.Update(reservation);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);

            var table = await _tableRepository.GetByIdAsync(reservation.DiningTableId, cancellationToken);
            await TrySendEmailAsync(reservation, "ReservationUpdatedTemplate.html", "Yummy Restoran - Rezervasyonunuz Güncellendi ve Onay Bekliyor", new Dictionary<string, string>
            {
                ["{{Name}}"] = reservation.Name,
                ["{{Surname}}"] = reservation.Surname,
                ["{{NewDate}}"] = reservation.ReservationDate.ToString("dd.MM.yyyy"),
                ["{{NewTime}}"] = reservation.ReservationTime,
                ["{{NewGuests}}"] = reservation.NumberOfGuests.ToString(),
                ["{{TableNo}}"] = table?.TableNo ?? "",
                ["{{Location}}"] = table?.Location ?? "Belirtilmemiş"
            });
        }

        // admin'in durum değiştirmesi. kayıt gün kilidi içinde yeniden okunur ve kurallar güncel durum üzerinden uygulanır:
        // aynı duruma geçilemez, tamamlanmış rezervasyon değiştirilemez, iptal edilmiş rezervasyon ancak başlangıç saati geçmemişse,
        // masası aktifse ve o saatte masa başka bir rezervasyona verilmemişse tekrar aktif edilebilir. sonrasında müşteriye durum e-postası gönderilir.
        public async Task UpdateReservationStatusAsync(UpdateReservationDto dto, CancellationToken cancellationToken = default)
        {
            var snapshot = await _reservationRepository.GetSingleAsync(r => r.ReservationId == dto.ReservationId, cancellationToken)
                ?? throw new LogicException("NotFound", "Rezervasyon bulunamadı.");

            var targetDate = snapshot.ReservationDate.Date;
            Reservation reservation = null!;

            await _uow.ExecuteInLockedTransactionAsync(GetDateLockKey(targetDate), async () =>
            {
                reservation = await ReloadForUpdateAsync(snapshot, cancellationToken);

                if (reservation.ReservationStatus == dto.ReservationStatus)
                    throw new LogicException("SameStatus", "Rezervasyon zaten bu durumda.");

                if (reservation.ReservationStatus == ReservationStatus.Completed)
                    throw new LogicException("NotAllowed", "Tamamlanmış rezervasyonların durumu değiştirilemez.");

                // iptal edilmiş bir rezervasyon tekrar aktif edilirken masası bu arada başka bir rezervasyona verilmiş veya pasife alınmış olabilir.
                bool isReactivating = reservation.ReservationStatus == ReservationStatus.Cancelled &&
                    (dto.ReservationStatus == ReservationStatus.Pending || dto.ReservationStatus == ReservationStatus.Approved);

                if (isReactivating)
                {
                    if (!TryParseTime(reservation.ReservationTime, out TimeSpan resStart) || !TryParseTime(reservation.ReservationEndTime, out TimeSpan resEnd))
                        throw new LogicException("InvalidTime", "Rezervasyonun saat bilgisi geçersiz.");

                    if (targetDate.Add(resStart) < _timeProvider.GetLocalDateTime())
                        throw new LogicException("PastReservation", "Başlangıç saati geçmiş bir rezervasyon tekrar aktif edilemez.");

                    var table = await _tableRepository.GetByIdAsync(reservation.DiningTableId, cancellationToken);
                    if (table == null || !table.IsActive)
                        throw new LogicException("TableNotAvailable", "Rezervasyonun masası artık kullanımda değil. Kullanıcının yeni bir rezervasyon oluşturması gerekir.");

                    var busyTableIds = await GetBusyTableIdsAsync(targetDate, resStart, resEnd, reservation.ReservationId, cancellationToken);
                    if (busyTableIds.Contains(reservation.DiningTableId))
                        throw new LogicException("TableNotAvailable", "Rezervasyonun masası bu saat aralığında başka bir rezervasyona ayrılmış. Kullanıcının yeni bir rezervasyon oluşturması gerekir.");
                }

                reservation.ReservationStatus = dto.ReservationStatus;
                _reservationRepository.Update(reservation);
                await _uow.SaveAsync(cancellationToken);
            }, cancellationToken);

            switch (reservation.ReservationStatus)
            {
                case ReservationStatus.Approved:
                    await SendStatusEmailAsync(reservation, "Rezervasyon Onaylandı", "onaylanmıştır. Sizi ağırlamaktan mutluluk duyacağız", "#28a745", cancellationToken);
                    break;
                case ReservationStatus.Cancelled:
                    await SendStatusEmailAsync(reservation, "Rezervasyon İptal Edildi", "operasyonel nedenler nedeniyle iptal edilmiştir", "#dc3545", cancellationToken);
                    break;
                case ReservationStatus.Pending:
                    await SendStatusEmailAsync(reservation, "Rezervasyon Beklemede", "tekrar değerlendirmeye alınmış ve bekleme durumuna çekilmiştir", "#ffc107", cancellationToken);
                    break;
            }
        }

        // arka plan servisi (ReservationStatusWorker) tarafından 10 dakikada bir çağrılır.
        // bitiş saati geçen Approved rezervasyonlar Completed, Pending olanlar Cancelled yapılır; her gün kendi kilidi altında güncel veriyle işlenir.
        // onaylanmadan süresi dolan rezervasyonların sahiplerine iptal e-postası gönderilir.
        public async Task<int> ProcessPastReservationsAsync(CancellationToken cancellationToken = default)
        {
            var now = _timeProvider.GetLocalDateTime();
            var today = now.Date;

            // sadece bugün veya daha önceki günlere ait aktif rezervasyonların günleri bulunur; ileri tarihli rezervasyonların süresi dolmuş olamaz.
            var candidates = await _reservationRepository.GetWhereAsync(r => r.ReservationDate <= today &&
                (r.ReservationStatus == ReservationStatus.Approved || r.ReservationStatus == ReservationStatus.Pending), cancellationToken);

            var candidateDates = candidates.Select(r => r.ReservationDate.Date).Distinct().OrderBy(date => date).ToList();

            var expiredPendingReservations = new List<Reservation>();
            var processedCount = 0;

            // her gün kendi kilidi altında ve güncel veriyle işlenir; aynı anda admin'in veya kullanıcının yaptığı değişiklik ezilmez.
            foreach (var date in candidateDates)
            {
                await _uow.ExecuteInLockedTransactionAsync(GetDateLockKey(date), async () =>
                {
                    var reservations = await _reservationRepository.GetWhereAsync(r => r.ReservationDate == date &&
                        (r.ReservationStatus == ReservationStatus.Approved || r.ReservationStatus == ReservationStatus.Pending), cancellationToken);

                    var changedCount = 0;

                    foreach (var reservation in reservations)
                    {
                        if (!TryParseTime(reservation.ReservationEndTime, out TimeSpan endTime) || date.Add(endTime) > now)
                            continue;

                        if (reservation.ReservationStatus == ReservationStatus.Approved)
                        {
                            reservation.ReservationStatus = ReservationStatus.Completed;
                        }
                        else
                        {
                            // restoran tarafından zamanında onaylanmamış rezervasyonlar iptal edilir ve kullanıcı bilgilendirilir.
                            reservation.ReservationStatus = ReservationStatus.Cancelled;
                            expiredPendingReservations.Add(reservation);
                        }

                        _reservationRepository.Update(reservation);
                        changedCount++;
                    }

                    if (changedCount > 0)
                        await _uow.SaveAsync(cancellationToken);

                    processedCount += changedCount;
                }, cancellationToken);
            }

            foreach (var reservation in expiredPendingReservations)
                await SendStatusEmailAsync(reservation, "Rezervasyon İptal Edildi", "rezervasyon saatine kadar onaylanamadığı için iptal edilmiştir. Anlayışınız için teşekkür ederiz", "#dc3545", cancellationToken);

            return processedCount;
        }

        // giriş gerektirmez. tüm aktif masaları, verilen saat aralığında dolu olup olmadıklarıyla birlikte döner (kapasiteden bağımsız).
        public async Task<IEnumerable<TableStatusForMapDto>> GetTableStatusesForMapAsync(DateTime date, string time, string endTime, CancellationToken cancellationToken = default)
        {
            var targetDate = date.Date;
            var (reqStart, reqEnd) = ParseTimeRange(time, endTime);

            var busyTableIds = await GetBusyTableIdsAsync(targetDate, reqStart, reqEnd, null, cancellationToken);
            var tables = await _tableRepository.GetWhereAsync(t => t.IsActive, cancellationToken);

            return tables.Select(table => new TableStatusForMapDto
            {
                DiningTableId = table.DiningTableId,
                TableNo = table.TableNo,
                Capacity = table.Capacity,
                Location = table.Location,
                IsAvailable = !busyTableIds.Contains(table.DiningTableId)
            }).ToList();
        }

        // kilit alındıktan sonra rezervasyon veritabanından tekrar okunur. kilitten önce okunan kopya, beklerken başka bir işlem tarafından değişmiş olabilir.
        // rezervasyonun günü bu arada değişmişse (başka bir güncelleme ile taşınmışsa) alınan kilit artık doğru günü korumadığı için işlem reddedilir.
        private async Task<Reservation> ReloadForUpdateAsync(Reservation snapshot, CancellationToken cancellationToken)
        {
            var current = await _reservationRepository.GetSingleAsync(r => r.ReservationId == snapshot.ReservationId, cancellationToken)
                ?? throw new LogicException("NotFound", "Rezervasyon bulunamadı.");

            if (current.ReservationDate.Date != snapshot.ReservationDate.Date)
                throw new LogicException("ConcurrencyConflict", "Rezervasyon bu sırada başka bir işlem tarafından değiştirildi. Lütfen güncel bilgilerle tekrar deneyiniz.");

            return current;
        }

        private async Task SendStatusEmailAsync(Reservation reservation, string statusTitle, string statusMessage, string statusColor, CancellationToken cancellationToken)
        {
            var table = await _tableRepository.GetByIdAsync(reservation.DiningTableId, cancellationToken);
            await TrySendEmailAsync(reservation, "ReservationStatusTemplate.html", $"Yummy Restoran - Rezervasyon Bilgilendirmesi ({statusTitle})", new Dictionary<string, string>
            {
                ["{{Name}}"] = reservation.Name,
                ["{{Surname}}"] = reservation.Surname,
                ["{{StatusTitle}}"] = statusTitle,
                ["{{StatusMessage}}"] = statusMessage,
                ["#112233"] = statusColor,
                ["{{Date}}"] = reservation.ReservationDate.ToString("dd.MM.yyyy"),
                ["{{Time}}"] = reservation.ReservationTime,
                ["{{Guests}}"] = reservation.NumberOfGuests.ToString(),
                ["{{TableNo}}"] = table?.TableNo ?? "",
                ["{{Location}}"] = table?.Location ?? "Belirtilmemiş"
            });
        }

        // e-posta, rezervasyon veritabanına kaydedildikten sonra gönderilir. şablon bulunamaz veya SMTP hata verirse işlem geri alınmaz;
        // hata loglanır ve istemciye başarılı cevap dönülür. aksi halde kullanıcı hata görüp tekrar dener ve mükerrer rezervasyon oluşur.
        private async Task TrySendEmailAsync(Reservation reservation, string templateName, string subject, IReadOnlyDictionary<string, string> placeholders)
        {
            try
            {
                // şablonlar derleme çıktısına kopyalanır; çalışma dizini (IIS, Windows servisi vb.) farklı olsa bile uygulamanın kendi klasöründen okunur.
                var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", templateName);
                var mailBody = await File.ReadAllTextAsync(templatePath);

                // e-posta HTML olarak gönderildiği için kullanıcıdan gelen değerler (ad, soyad, telefon vb.) encode edilir;
                // aksi halde kullanıcı restoranın adresinden, istediği bir adrese link/HTML içeren e-posta gönderebilir.
                foreach (var placeholder in placeholders)
                    mailBody = mailBody.Replace(placeholder.Key, WebUtility.HtmlEncode(placeholder.Value));

                await _emailService.SendEmailAsync(reservation.Email, subject, mailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rezervasyon e-postası gönderilemedi. Rezervasyon: {ReservationId}, Şablon: {TemplateName}", reservation.ReservationId, templateName);
            }
        }

        // aynı güne ait rezervasyon yazma işlemleri bu anahtar ile kilitlenir. farklı günlerin istekleri birbirini beklemez.
        private static string GetDateLockKey(DateTime date) => $"reservation:{date:yyyy-MM-dd}";

        // aynı kullanıcının rezervasyon oluşturma istekleri bu anahtar ile sıraya girer (aktif rezervasyon limiti için).
        private static string GetUserLockKey(Guid userId) => $"reservation-user:{userId}";

        // saatler sadece "HH:mm" formatında kabul edilir. TimeSpan.TryParse "19" gibi bir değeri 19 gün olarak yorumladığı için kullanılmaz.
        private static bool TryParseTime(string? value, out TimeSpan time) =>
            TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out time);

        private static (TimeSpan Start, TimeSpan End) ParseTimeRange(string? start, string? end)
        {
            if (!TryParseTime(start, out TimeSpan startTime) || !TryParseTime(end, out TimeSpan endTime) || endTime <= startTime)
                throw new LogicException("InvalidTime", "Geçersiz saat aralığı. Saatler HH:mm formatında olmalı ve bitiş saati başlangıç saatinden sonra olmalıdır.");

            return (startTime, endTime);
        }

        // iki saat aralığı, biri diğeri bitmeden başlıyorsa çakışır. uç uca eklenen aralıklar (19:00-21:00 ve 21:00-22:00) çakışmaz.
        private static bool IsOverlapping(TimeSpan startA, TimeSpan endA, TimeSpan startB, TimeSpan endB) =>
            startA < endB && endA > startB;

        // verilen gün ve saat aralığında aktif (Pending/Approved) bir rezervasyonu bulunan masaların id'lerini döner.
        // excludeReservationId: güncellenen rezervasyonun kendi masasını dolu saymaması için hariç tutulur.
        private async Task<HashSet<Guid>> GetBusyTableIdsAsync(DateTime date, TimeSpan start, TimeSpan end, Guid? excludeReservationId, CancellationToken cancellationToken)
        {
            // ReservationDate saatsiz kaydedildiği için doğrudan eşitlik kullanılır; .Date karşılaştırması index kullanımını engeller.
            var targetDate = date.Date;
            var excludedId = excludeReservationId ?? Guid.Empty;

            var activeReservations = await _reservationRepository.GetWhereAsync(r => r.ReservationDate == targetDate && r.ReservationId != excludedId &&
                           (r.ReservationStatus == ReservationStatus.Approved || r.ReservationStatus == ReservationStatus.Pending), cancellationToken);

            return activeReservations
                .Where(r => TryParseTime(r.ReservationTime, out TimeSpan resStart) &&
                            TryParseTime(r.ReservationEndTime, out TimeSpan resEnd) &&
                            IsOverlapping(start, end, resStart, resEnd))
                .Select(r => r.DiningTableId)
                .ToHashSet();
        }

        // kişi sayısına yeten, aktif ve verilen saat aralığında boş olan masaları kapasiteye göre küçükten büyüğe sıralı döner.
        private async Task<List<DiningTable>> GetAvailableTablesAsync(DateTime date, TimeSpan start, TimeSpan end, int numberOfGuests, Guid? excludeReservationId, CancellationToken cancellationToken)
        {
            var busyTableIds = await GetBusyTableIdsAsync(date, start, end, excludeReservationId, cancellationToken);
            var tables = await _tableRepository.GetWhereAsync(t => t.IsActive && t.Capacity >= numberOfGuests, cancellationToken);

            return tables
                .Where(t => !busyTableIds.Contains(t.DiningTableId))
                .OrderBy(t => t.Capacity)
                .ToList();
        }
    }
}

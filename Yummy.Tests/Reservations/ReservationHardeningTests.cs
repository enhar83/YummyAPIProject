using Microsoft.Extensions.Logging.Abstractions;
using Yummy.Business.Managers;
using Yummy.Business.Validators.CommonValidators;
using Yummy.Business.Validators.ReservationValidators;
using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Core.Exceptions;
using Yummy.Data;
using Yummy.Data.Context;
using Yummy.Data.Repositories;
using Yummy.Entity;
using Yummy.Entity.Enums;
using Yummy.Tests.Infrastructure;

namespace Yummy.Tests.Reservations
{
    // e-posta güvenliği, alan sınırları, eşzamanlı güncellemeler, şablon yolu, saat ayrıştırma, sıralama ve sayfalama.
    public class ReservationHardeningTests : SqliteTestBase
    {
        private async Task<Guid> BookAsync(Guid userId, ReservationCreateDto dto)
        {
            await using var db = CreateDbContext();
            await CreateReservationManager(db).AddReservationAsync(userId.ToString(), dto);
            return db.Reservations.OrderByDescending(r => r.CreatedDate).First().ReservationId;
        }

        private async Task<Guid> InsertReservationAsync(DateTime date, string start, string end, ReservationStatus status, Guid? userId = null)
        {
            var reservation = new Reservation
            {
                Name = "Test", Surname = "Kullanıcı", Email = "a@test.com", Phone = "05551234567", Message = "",
                ReservationDate = date, ReservationTime = start, ReservationEndTime = end,
                NumberOfGuests = 2, ReservationStatus = status, AppUserId = userId ?? UserA, DiningTableId = SmallTableId
            };

            await using var db = CreateDbContext();
            db.Reservations.Add(reservation);
            await db.SaveChangesAsync();
            return reservation.ReservationId;
        }

        private async Task<Reservation> GetAsync(Guid id)
        {
            await using var db = CreateDbContext();
            return db.Reservations.Single(r => r.ReservationId == id);
        }

        // kilidi almadan hemen önce "interleave" işini araya sokan bir manager oluşturur.
        private ReservationManager CreateInterleavedManager(YummyDbContext db, Func<Task> interleave) =>
            new(new GenericRepository<Reservation>(db), new GenericRepository<DiningTable>(db), new InterleavingUnitOfWork(new UnitOfWork(db), interleave),
                Mapper, Email, NullLogger<ReservationManager>.Instance, Clock);

        // ---------- 1. e-posta HTML enjeksiyonu ve alan sınırları ----------

        [Fact]
        public async Task Email_UserInputIsHtmlEncoded()
        {
            var dto = CreateDto(FutureDay);
            dto.Name = "<a href=\"http://kotu-site.example\">Tıklayın</a>";

            await BookAsync(UserA, dto);

            var body = Assert.Single(Email.SentEmails).Body;
            Assert.DoesNotContain("<a href=\"http://kotu-site.example\">", body);
            Assert.Contains("&lt;a href=&quot;http://kotu-site.example&quot;&gt;", body);
        }

        [Theory]
        [InlineData("05551234567", true)]
        [InlineData("0555 123 45 67", true)]
        [InlineData("+90 (555) 123-4567", true)]
        [InlineData("12345", false)]
        [InlineData("<script>alert(1)</script>", false)]
        [InlineData("0555 123 45 67 89 01 23", false)]
        public void CreateValidator_PhoneFormat(string phone, bool isValid)
        {
            var dto = CreateDto(FutureDay);
            dto.Phone = phone;

            var result = new CreateReservationValidator(Clock).Validate(dto);
            Assert.Equal(isValid, !result.Errors.Any(e => e.PropertyName == nameof(ReservationCreateDto.Phone)));
        }

        [Fact]
        public void CreateValidator_RejectsTooLongFields()
        {
            var dto = CreateDto(FutureDay);
            dto.Name = new string('a', 51);
            dto.Surname = new string('a', 51);
            dto.Email = new string('a', 95) + "@x.com";
            dto.Message = new string('a', 501);

            var failed = new CreateReservationValidator(Clock).Validate(dto).Errors.Select(e => e.PropertyName).ToHashSet();
            Assert.Superset(new HashSet<string> { "Name", "Surname", "Email", "Message" }, failed);
        }

        [Fact]
        public void UpdateValidator_RejectsTooLongMessage()
        {
            var result = new UpdateReservationValidator(Clock).Validate(new ReservationUpdateDto
            {
                ReservationId = Guid.NewGuid(), ReservationDate = FutureDay, ReservationTime = "19:00", ReservationEndTime = "21:00",
                NumberOfGuests = 2, Message = new string('a', 501)
            });

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReservationUpdateDto.Message));
        }

        // ---------- 2. eşzamanlı güncellemeler ----------

        [Fact]
        public async Task UserUpdate_WhileAdminCancels_DoesNotResurrectReservation()
        {
            var id = await BookAsync(UserA, CreateDto(FutureDay));
            await using (var db = CreateDbContext())
                await CreateReservationManager(db).UpdateReservationStatusAsync(new UpdateReservationDto { ReservationId = id, ReservationStatus = ReservationStatus.Approved });

            // kullanıcı rezervasyonu okuduktan sonra, kaydetmeden önce admin iptal eder.
            await using (var db = CreateDbContext())
            {
                var manager = CreateInterleavedManager(db, async () =>
                {
                    await using var adminDb = CreateDbContext();
                    await CreateReservationManager(adminDb).UpdateReservationStatusAsync(new UpdateReservationDto { ReservationId = id, ReservationStatus = ReservationStatus.Cancelled });
                });

                var ex = await Assert.ThrowsAsync<LogicException>(() => manager.UpdateReservationAsync(UserA.ToString(), new ReservationUpdateDto
                {
                    ReservationId = id, ReservationDate = FutureDay, ReservationTime = "19:00", ReservationEndTime = "21:00", NumberOfGuests = 2, Message = "Cam kenarı"
                }));
                Assert.Equal("NotAllowed", ex.PropertyName);
            }

            var reservation = await GetAsync(id);
            Assert.Equal(ReservationStatus.Cancelled, reservation.ReservationStatus);
            Assert.Equal("", reservation.Message);
        }

        [Fact]
        public async Task AdminStatusChange_WhileUserMovesReservationToAnotherDay_IsRejected()
        {
            var id = await BookAsync(UserA, CreateDto(FutureDay));

            await using (var db = CreateDbContext())
            {
                // admin rezervasyonu okuduktan sonra kullanıcı onu başka bir güne taşır; admin'in aldığı kilit artık yanlış günü korur.
                var manager = CreateInterleavedManager(db, async () =>
                {
                    await using var userDb = CreateDbContext();
                    await CreateReservationManager(userDb).UpdateReservationAsync(UserA.ToString(), new ReservationUpdateDto
                    {
                        ReservationId = id, ReservationDate = FutureDay.AddDays(1), ReservationTime = "19:00", ReservationEndTime = "21:00", NumberOfGuests = 2
                    });
                });

                var ex = await Assert.ThrowsAsync<LogicException>(() => manager.UpdateReservationStatusAsync(
                    new UpdateReservationDto { ReservationId = id, ReservationStatus = ReservationStatus.Approved }));
                Assert.Equal("ConcurrencyConflict", ex.PropertyName);
            }

            var reservation = await GetAsync(id);
            Assert.Equal(FutureDay.AddDays(1), reservation.ReservationDate);
            Assert.Equal(ReservationStatus.Pending, reservation.ReservationStatus);
        }

        [Fact]
        public async Task UserCancel_WhileAdminApproves_KeepsLatestState()
        {
            var id = await BookAsync(UserA, CreateDto(FutureDay));

            // kullanıcı iptal ederken admin araya girip onaylar; iptal kilit içinde güncel kayıt üzerinde yapılır ve admin'in onayını ezmeden iptal eder.
            await using (var db = CreateDbContext())
            {
                var manager = CreateInterleavedManager(db, async () =>
                {
                    await using var adminDb = CreateDbContext();
                    await CreateReservationManager(adminDb).UpdateReservationStatusAsync(new UpdateReservationDto { ReservationId = id, ReservationStatus = ReservationStatus.Approved });
                });
                await manager.CancelReservationAsync(UserA.ToString(), id);
            }

            var reservation = await GetAsync(id);
            Assert.Equal(ReservationStatus.Cancelled, reservation.ReservationStatus);
            Assert.NotNull(reservation.UpdatedDate);
        }

        [Fact]
        public async Task Worker_WhileAdminCancelsExpiredPending_DoesNotSendSecondEmail()
        {
            var id = await InsertReservationAsync(Today, "09:00", "10:00", ReservationStatus.Pending); // saat 12:00, süresi dolmuş

            await using (var db = CreateDbContext())
            {
                var manager = CreateInterleavedManager(db, async () =>
                {
                    await using var adminDb = CreateDbContext();
                    await CreateReservationManager(adminDb).UpdateReservationStatusAsync(new UpdateReservationDto { ReservationId = id, ReservationStatus = ReservationStatus.Cancelled });
                });
                Assert.Equal(0, await manager.ProcessPastReservationsAsync());
            }

            Assert.Equal(ReservationStatus.Cancelled, (await GetAsync(id)).ReservationStatus);
            Assert.Single(Email.SentEmails); // sadece admin'in iptal e-postası
        }

        // ---------- 3. şablon yolu çalışma dizininden bağımsız ----------

        [Fact]
        public async Task Emails_AreSent_EvenWhenWorkingDirectoryIsDifferent()
        {
            var originalDirectory = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(Path.GetTempPath());
                await BookAsync(UserA, CreateDto(FutureDay));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
            }

            Assert.Single(Email.SentEmails);
        }

        // ---------- 4. katı saat ayrıştırma ----------

        [Theory]
        [InlineData("19", "21")]        // TimeSpan.TryParse bunları gün olarak yorumlar
        [InlineData("21:00", "19:00")]  // bitiş başlangıçtan önce
        [InlineData("19:00", "19:00")]
        [InlineData("7:00", "9:00")]    // tek haneli saat
        [InlineData("abc", "21:00")]
        public async Task MapStatus_InvalidTimeRange_ThrowsInvalidTime(string start, string end)
        {
            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateReservationManager(db).GetTableStatusesForMapAsync(FutureDay, start, end));
            Assert.Equal("InvalidTime", ex.PropertyName);
        }

        // ---------- 6. sıralama ----------

        [Fact]
        public async Task MyReservations_SameDay_AreOrderedByTimeDescending()
        {
            await InsertReservationAsync(FutureDay, "12:00", "13:00", ReservationStatus.Pending);
            await InsertReservationAsync(FutureDay, "19:00", "20:00", ReservationStatus.Pending);
            await InsertReservationAsync(FutureDay.AddDays(1), "10:00", "11:00", ReservationStatus.Pending);

            await using var db = CreateDbContext();
            var times = (await CreateReservationManager(db).SeeMyPastReservationsAsync(UserA.ToString())).Select(r => r.ReservationTime).ToList();
            Assert.Equal(new[] { "10:00", "19:00", "12:00" }, times);
        }

        // ---------- sayfalama ----------

        [Fact]
        public async Task AdminList_IsPaged_NewestFirst()
        {
            for (var i = 0; i < 25; i++)
                await InsertReservationAsync(Today.AddDays(-i), "19:00", "20:00", ReservationStatus.Completed);

            await using var db = CreateDbContext();
            var manager = CreateReservationManager(db);

            var first = await manager.GetAllReservationsAsync(new PaginationQueryDto { Page = 1, PageSize = 10 });
            Assert.Equal(25, first.TotalCount);
            Assert.Equal(3, first.TotalPages);
            Assert.Equal(10, first.Items.Count);
            Assert.Equal(Today, first.Items[0].ReservationDate);
            Assert.Equal("Masa 1", first.Items[0].TableNo); // DiningTable include edilir
            Assert.Equal("a@test.com", first.Items[0].Email); // admin müşterinin e-postasını görür

            var last = await manager.GetAllReservationsAsync(new PaginationQueryDto { Page = 3, PageSize = 10 });
            Assert.Equal(5, last.Items.Count);
            Assert.Equal(Today.AddDays(-24), last.Items[^1].ReservationDate);

            var beyond = await manager.GetAllReservationsAsync(new PaginationQueryDto { Page = 4, PageSize = 10 });
            Assert.Empty(beyond.Items);
            Assert.Equal(25, beyond.TotalCount);
        }

        [Fact]
        public async Task AdminList_PagesDoNotOverlap_WhenManyReservationsShareSameSlot()
        {
            for (var i = 0; i < 6; i++)
                await InsertReservationAsync(FutureDay, "19:00", "20:00", ReservationStatus.Pending);

            await using var db = CreateDbContext();
            var manager = CreateReservationManager(db);
            var ids = new List<Guid>();
            for (var page = 1; page <= 3; page++)
                ids.AddRange((await manager.GetAllReservationsAsync(new PaginationQueryDto { Page = page, PageSize = 2 })).Items.Select(r => r.ReservationId));

            Assert.Equal(6, ids.Distinct().Count());
        }

        [Theory]
        [InlineData(1, 20, true)]
        [InlineData(1, 100, true)]
        [InlineData(0, 20, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, 101, false)]
        public void PaginationValidator(int page, int pageSize, bool isValid)
        {
            var result = new PaginationQueryValidator().Validate(new PaginationQueryDto { Page = page, PageSize = pageSize });
            Assert.Equal(isValid, result.IsValid);
        }
    }
}

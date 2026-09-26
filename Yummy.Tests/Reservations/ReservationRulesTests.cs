using Yummy.Business.Validators.ReservationValidators;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Core.Exceptions;
using Yummy.Entity;
using Yummy.Entity.Enums;
using Yummy.Tests.Infrastructure;

namespace Yummy.Tests.Reservations
{
    // iptal süresi, tarih normalizasyonu ve e-posta hatası davranışları.
    public class ReservationRulesTests : SqliteTestBase
    {
        private async Task<Guid> InsertReservationAsync(DateTime start, ReservationStatus status = ReservationStatus.Approved, DateTime? storedDate = null)
        {
            var reservation = new Reservation
            {
                Name = "Test", Surname = "Kullanıcı", Email = "a@test.com", Phone = "1", Message = "",
                ReservationDate = storedDate ?? start.Date,
                ReservationTime = start.ToString("HH:mm"),
                ReservationEndTime = start.AddMinutes(60).ToString("HH:mm"),
                NumberOfGuests = 2, ReservationStatus = status, AppUserId = UserA, DiningTableId = SmallTableId
            };

            await using var db = CreateDbContext();
            db.Reservations.Add(reservation);
            await db.SaveChangesAsync();
            return reservation.ReservationId;
        }

        // ---------- iptal süresi ----------

        [Fact]
        public async Task Cancel_LessThanTwoHoursBefore_ThrowsTooLate()
        {
            var id = await InsertReservationAsync(Now.AddMinutes(100));

            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateReservationManager(db).CancelReservationAsync(UserA.ToString(), id));
            Assert.Equal("TooLate", ex.PropertyName);
        }

        [Fact]
        public async Task Cancel_MoreThanTwoHoursBefore_Succeeds()
        {
            var id = await InsertReservationAsync(Now.AddMinutes(150));

            await using (var db = CreateDbContext())
                await CreateReservationManager(db).CancelReservationAsync(UserA.ToString(), id);

            await using var check = CreateDbContext();
            Assert.Equal(ReservationStatus.Cancelled, check.Reservations.Single().ReservationStatus);
        }

        [Fact]
        public async Task Cancel_CompletedReservation_ThrowsNotAllowed()
        {
            var id = await InsertReservationAsync(Now.AddDays(-1), ReservationStatus.Completed);

            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateReservationManager(db).CancelReservationAsync(UserA.ToString(), id));
            Assert.Equal("NotAllowed", ex.PropertyName);
        }

        // ---------- tarih normalizasyonu ----------

        [Fact]
        public async Task AddReservation_DateWithTimeComponent_IsStoredAsDateOnly()
        {
            await using (var db = CreateDbContext())
                await CreateReservationManager(db).AddReservationAsync(UserA.ToString(), CreateDto(FutureDay.AddHours(19)));

            await using var check = CreateDbContext();
            Assert.Equal(FutureDay, check.Reservations.Single().ReservationDate);
        }

        [Fact]
        public async Task UpdateReservation_DateWithTimeComponent_IsStoredAsDateOnly()
        {
            Guid id;
            await using (var db = CreateDbContext())
            {
                await CreateReservationManager(db).AddReservationAsync(UserA.ToString(), CreateDto(FutureDay));
                id = db.Reservations.Single().ReservationId;
            }

            await using (var db = CreateDbContext())
                await CreateReservationManager(db).UpdateReservationAsync(UserA.ToString(), new ReservationUpdateDto
                {
                    ReservationId = id, ReservationDate = FutureDay.AddDays(1).AddHours(15), ReservationTime = "19:00", ReservationEndTime = "21:00", NumberOfGuests = 2
                });

            await using var check = CreateDbContext();
            Assert.Equal(FutureDay.AddDays(1), check.Reservations.Single().ReservationDate);
        }

        [Fact]
        public async Task TodaysReservations_IncludesTodayOnly_EvenWithLegacyTimeComponent()
        {
            await InsertReservationAsync(Today.AddHours(20));                                           // normal kayıt
            await InsertReservationAsync(Today.AddHours(21), storedDate: Today.AddHours(15));  // saat kısmı olan eski kayıt
            await InsertReservationAsync(Today.AddDays(1).AddHours(20));                                // yarın

            await using var db = CreateDbContext();
            var todays = await CreateReservationManager(db).GetTodaysReservationListAsync();
            Assert.Equal(2, todays.Count());
        }

        [Fact]
        public void Validator_LastAllowedDayWithTimeComponent_IsValid()
        {
            var dto = CreateDto(Today.AddMonths(1).AddHours(19));
            var result = new CreateReservationValidator(Clock).Validate(dto);
            Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(ReservationCreateDto.ReservationDate));
        }

        [Fact]
        public void Validator_DayAfterLastAllowedDay_IsInvalid()
        {
            var dto = CreateDto(Today.AddMonths(1).AddDays(1));
            var result = new CreateReservationValidator(Clock).Validate(dto);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReservationCreateDto.ReservationDate));
        }

        // ---------- e-posta hatası ----------

        [Fact]
        public async Task AddReservation_WhenEmailFails_ReservationIsSavedAndNoErrorIsReturned()
        {
            Email.ShouldFail = true;

            await using (var db = CreateDbContext())
                await CreateReservationManager(db).AddReservationAsync(UserA.ToString(), CreateDto(FutureDay));

            await using var check = CreateDbContext();
            Assert.Single(check.Reservations);
        }

        [Fact]
        public async Task CancelReservation_WhenEmailFails_CancellationIsSaved()
        {
            var id = await InsertReservationAsync(FutureDay.AddHours(19));
            Email.ShouldFail = true;

            await using (var db = CreateDbContext())
                await CreateReservationManager(db).CancelReservationAsync(UserA.ToString(), id);

            await using var check = CreateDbContext();
            Assert.Equal(ReservationStatus.Cancelled, check.Reservations.Single().ReservationStatus);
        }

        [Fact]
        public async Task AllReservationEmails_AreSentWithTemplates()
        {
            // her işlem ayrı bir HTTP isteğini taklit eder; bu yüzden her biri kendi DbContext'ini kullanır.
            Guid id;
            await using (var db = CreateDbContext())
            {
                await CreateReservationManager(db).AddReservationAsync(UserA.ToString(), CreateDto(FutureDay));
                id = db.Reservations.Single().ReservationId;
            }

            await using (var db = CreateDbContext())
                await CreateReservationManager(db).UpdateReservationAsync(UserA.ToString(), new ReservationUpdateDto
                {
                    ReservationId = id, ReservationDate = FutureDay, ReservationTime = "19:30", ReservationEndTime = "21:00", NumberOfGuests = 2
                });

            await using (var db = CreateDbContext())
                await CreateReservationManager(db).UpdateReservationStatusAsync(new UpdateReservationDto { ReservationId = id, ReservationStatus = ReservationStatus.Approved });

            await using (var db = CreateDbContext())
                await CreateReservationManager(db).CancelReservationAsync(UserA.ToString(), id);

            // şablon bulunamasaydı hata loglanır ve e-posta gönderilmezdi; 4 e-postanın da gitmesi şablonların okunduğunu gösterir.
            Assert.Equal(4, Email.SentEmails.Count);
        }
    }
}

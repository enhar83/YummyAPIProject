using Yummy.Business.Managers;
using Yummy.Business.Time;
using Yummy.Business.Validators.ReservationValidators;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.Extensions;
using Yummy.Entity;
using Yummy.Entity.Enums;
using Yummy.Tests.Infrastructure;

namespace Yummy.Tests.Reservations
{
    // kullanıcı limiti, güncelleme kuralları, süresi dolan rezervasyonlar ve saat dilimi davranışları.
    public class ReservationPolicyTests : SqliteTestBase
    {
        private async Task<Guid> BookAsync(Guid userId, ReservationCreateDto dto)
        {
            await using var db = CreateDbContext();
            await CreateReservationManager(db).AddReservationAsync(userId.ToString(), dto);
            return db.Reservations.OrderByDescending(r => r.CreatedDate).First().ReservationId;
        }

        private async Task<Guid> InsertReservationAsync(DateTime date, string start, string end, ReservationStatus status)
        {
            var reservation = new Reservation
            {
                Name = "Test", Surname = "Kullanıcı", Email = "a@test.com", Phone = "1", Message = "",
                ReservationDate = date, ReservationTime = start, ReservationEndTime = end,
                NumberOfGuests = 2, ReservationStatus = status, AppUserId = UserA, DiningTableId = SmallTableId
            };

            await using var db = CreateDbContext();
            db.Reservations.Add(reservation);
            await db.SaveChangesAsync();
            return reservation.ReservationId;
        }

        private async Task<ReservationStatus> GetStatusAsync(Guid id)
        {
            await using var db = CreateDbContext();
            return db.Reservations.Single(r => r.ReservationId == id).ReservationStatus;
        }

        // ---------- kullanıcı başına aktif rezervasyon limiti ----------

        [Fact]
        public async Task AddReservation_OverActiveLimit_ThrowsReservationLimit()
        {
            for (var day = 1; day <= ReservationManager.MaxActiveReservationsPerUser; day++)
                await BookAsync(UserA, CreateDto(Today.AddDays(day)));

            var ex = await Assert.ThrowsAsync<LogicException>(() => BookAsync(UserA, CreateDto(Today.AddDays(10))));
            Assert.Equal("ReservationLimit", ex.PropertyName);

            // limit kullanıcı bazlıdır; başka bir kullanıcı etkilenmez.
            await BookAsync(UserB, CreateDto(Today.AddDays(10)));
        }

        [Fact]
        public async Task AddReservation_CancelledAndPastReservations_DoNotCountTowardsLimit()
        {
            await InsertReservationAsync(Today.AddDays(-5), "19:00", "21:00", ReservationStatus.Completed);
            await InsertReservationAsync(Today.AddDays(-1), "19:00", "21:00", ReservationStatus.Approved); // worker henüz işlememiş olsa bile geçmişte
            await InsertReservationAsync(Today.AddDays(2), "19:00", "21:00", ReservationStatus.Cancelled);

            for (var day = 1; day <= ReservationManager.MaxActiveReservationsPerUser; day++)
                await BookAsync(UserA, CreateDto(Today.AddDays(day)));

            await using var db = CreateDbContext();
            Assert.Equal(ReservationManager.MaxActiveReservationsPerUser, db.Reservations.Count(r => r.ReservationStatus == ReservationStatus.Pending));
        }

        // ---------- güncelleme ----------

        [Fact]
        public async Task UpdateReservation_WithoutChanges_ThrowsNoChanges_AndSendsNoEmail()
        {
            var id = await BookAsync(UserA, CreateDto(FutureDay));
            Email.SentEmails.Clear();

            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateReservationManager(db).UpdateReservationAsync(UserA.ToString(), new ReservationUpdateDto
            {
                ReservationId = id, ReservationDate = FutureDay, ReservationTime = "19:00", ReservationEndTime = "21:00", NumberOfGuests = 2
            }));

            Assert.Equal("NoChanges", ex.PropertyName);
            Assert.Empty(Email.SentEmails);
        }

        [Fact]
        public async Task UpdateReservation_KeepsCurrentTable_WhenStillAvailable()
        {
            // kullanıcı 2 kişi için 4 kişilik masayı seçmiş; saat değişince en küçük masaya (2 kişilik) taşınmamalı.
            var id = await BookAsync(UserA, CreateDto(FutureDay, guests: 2, tableId: MediumTableId));

            await using (var db = CreateDbContext())
                await CreateReservationManager(db).UpdateReservationAsync(UserA.ToString(), new ReservationUpdateDto
                {
                    ReservationId = id, ReservationDate = FutureDay, ReservationTime = "20:00", ReservationEndTime = "22:00", NumberOfGuests = 2
                });

            await using var check = CreateDbContext();
            Assert.Equal(MediumTableId, check.Reservations.Single().DiningTableId);
        }

        [Fact]
        public async Task UpdateReservation_MovesToAnotherTable_WhenCurrentTableIsBusy()
        {
            var id = await BookAsync(UserA, CreateDto(FutureDay, start: "17:00", end: "18:00"));  // SmallTable
            await BookAsync(UserB, CreateDto(FutureDay, start: "19:00", end: "21:00"));           // SmallTable

            await using (var db = CreateDbContext())
                await CreateReservationManager(db).UpdateReservationAsync(UserA.ToString(), new ReservationUpdateDto
                {
                    ReservationId = id, ReservationDate = FutureDay, ReservationTime = "19:30", ReservationEndTime = "20:30", NumberOfGuests = 2
                });

            await using var check = CreateDbContext();
            Assert.Equal(MediumTableId, check.Reservations.Single(r => r.ReservationId == id).DiningTableId);
        }

        [Fact]
        public void UpdateValidator_EmptyReservationId_IsInvalid()
        {
            var result = new UpdateReservationValidator(Clock).Validate(new ReservationUpdateDto
            {
                ReservationId = Guid.Empty, ReservationDate = FutureDay, ReservationTime = "19:00", ReservationEndTime = "21:00", NumberOfGuests = 2
            });

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReservationUpdateDto.ReservationId));
        }

        // ---------- süresi dolan rezervasyonlar (arka plan servisi) ----------

        [Fact]
        public async Task ProcessPastReservations_CompletesApproved_CancelsPendingWithEmail_LeavesOthers()
        {
            // saat 12:00
            var endedApproved = await InsertReservationAsync(Today, "09:00", "11:00", ReservationStatus.Approved);
            var endedPending = await InsertReservationAsync(Today.AddDays(-1), "19:00", "21:00", ReservationStatus.Pending);
            var ongoing = await InsertReservationAsync(Today, "11:00", "13:00", ReservationStatus.Approved);
            var future = await InsertReservationAsync(Today.AddDays(1), "09:00", "10:00", ReservationStatus.Pending);

            int processed;
            await using (var db = CreateDbContext())
                processed = await CreateReservationManager(db).ProcessPastReservationsAsync();

            Assert.Equal(2, processed);
            Assert.Equal(ReservationStatus.Completed, await GetStatusAsync(endedApproved));
            Assert.Equal(ReservationStatus.Cancelled, await GetStatusAsync(endedPending));
            Assert.Equal(ReservationStatus.Approved, await GetStatusAsync(ongoing));
            Assert.Equal(ReservationStatus.Pending, await GetStatusAsync(future));

            // sadece onaylanmadan süresi dolan rezervasyon için e-posta gönderilir.
            var email = Assert.Single(Email.SentEmails);
            Assert.Contains("İptal", email.Subject);
        }

        [Fact]
        public async Task ProcessPastReservations_UsesRestaurantClock()
        {
            var id = await InsertReservationAsync(Today, "11:00", "13:00", ReservationStatus.Approved);

            await using (var db = CreateDbContext())
                Assert.Equal(0, await CreateReservationManager(db).ProcessPastReservationsAsync());

            Clock.Advance(TimeSpan.FromHours(1)); // 13:00

            await using (var db = CreateDbContext())
                Assert.Equal(1, await CreateReservationManager(db).ProcessPastReservationsAsync());

            Assert.Equal(ReservationStatus.Completed, await GetStatusAsync(id));
        }

        // ---------- saat dilimi ----------

        [Fact]
        public void RestaurantTimeProvider_ReturnsRestaurantLocalTime_RegardlessOfServerTimeZone()
        {
            var istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            var provider = new RestaurantTimeProvider(istanbul);

            var expected = TimeZoneInfo.ConvertTimeFromUtc(provider.GetUtcNow().UtcDateTime, istanbul);
            Assert.True((provider.GetLocalDateTime() - expected).Duration() < TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task AddReservation_PastCheckUsesRestaurantClock()
        {
            // saat 12:00: bugün 11:00 geçmiş, 13:00 gelecek sayılır (sunucunun saati ne olursa olsun).
            var ex = await Assert.ThrowsAsync<LogicException>(() => BookAsync(UserA, CreateDto(Today, start: "11:00", end: "12:00")));
            Assert.Equal("PastReservation", ex.PropertyName);

            await BookAsync(UserA, CreateDto(Today, start: "13:00", end: "14:00"));
        }
    }
}

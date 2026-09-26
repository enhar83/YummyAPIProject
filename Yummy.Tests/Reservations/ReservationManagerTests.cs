using Microsoft.EntityFrameworkCore;
using Yummy.Core.DTOs.ReservationDTOs;
using Yummy.Core.Exceptions;
using Yummy.Data;
using Yummy.Entity;
using Yummy.Entity.Enums;
using Yummy.Tests.Infrastructure;

namespace Yummy.Tests.Reservations
{
    public class ReservationManagerTests : SqliteTestBase
    {
        private async Task<Guid> BookAsync(Guid userId, ReservationCreateDto dto)
        {
            await using var db = CreateDbContext();
            await CreateReservationManager(db).AddReservationAsync(userId.ToString(), dto);
            return db.Reservations.OrderByDescending(r => r.CreatedDate).First().ReservationId;
        }

        [Fact]
        public async Task AddReservation_ChoosesSmallestFittingTable()
        {
            await BookAsync(UserA, CreateDto(FutureDay, guests: 2));

            await using var db = CreateDbContext();
            var reservation = db.Reservations.Single();
            Assert.Equal(SmallTableId, reservation.DiningTableId);
            Assert.Equal(ReservationStatus.Pending, reservation.ReservationStatus);
            Assert.Single(Email.SentEmails);
        }

        [Fact]
        public async Task AddReservation_OverlappingSlot_MovesToNextTable_ThenThrowsNoTable()
        {
            await BookAsync(UserA, CreateDto(FutureDay, guests: 2, start: "19:00", end: "21:00"));
            await BookAsync(UserB, CreateDto(FutureDay, guests: 2, start: "20:00", end: "22:00"));

            await using (var db = CreateDbContext())
                Assert.Equal(MediumTableId, db.Reservations.Single(r => r.ReservationTime == "20:00").DiningTableId);

            var ex = await Assert.ThrowsAsync<LogicException>(() => BookAsync(UserB, CreateDto(FutureDay, guests: 2, start: "19:30", end: "20:30")));
            Assert.Equal("NoTable", ex.PropertyName);
        }

        [Fact]
        public async Task AddReservation_AdjacentSlots_DoNotOverlap()
        {
            await BookAsync(UserA, CreateDto(FutureDay, start: "19:00", end: "21:00"));
            await BookAsync(UserB, CreateDto(FutureDay, start: "21:00", end: "22:00"));

            await using var db = CreateDbContext();
            Assert.All(db.Reservations, r => Assert.Equal(SmallTableId, r.DiningTableId));
        }

        [Fact]
        public async Task AddReservation_SelectedTableBusy_ThrowsTableNotAvailable()
        {
            await BookAsync(UserA, CreateDto(FutureDay, tableId: SmallTableId));

            var ex = await Assert.ThrowsAsync<LogicException>(() => BookAsync(UserB, CreateDto(FutureDay, tableId: SmallTableId)));
            Assert.Equal("TableNotAvailable", ex.PropertyName);
        }

        [Fact]
        public async Task AddReservation_InactiveTable_IsNotOffered()
        {
            await using (var db = CreateDbContext())
            {
                db.DiningTables.Single(t => t.DiningTableId == SmallTableId).IsActive = false;
                await db.SaveChangesAsync();
            }

            await BookAsync(UserA, CreateDto(FutureDay, guests: 2));

            await using var check = CreateDbContext();
            Assert.Equal(MediumTableId, check.Reservations.Single().DiningTableId);
        }

        [Fact]
        public async Task CheckAvailability_ExcludesBusyTables()
        {
            await BookAsync(UserA, CreateDto(FutureDay, guests: 2));

            await using var db = CreateDbContext();
            var result = await CreateReservationManager(db).CheckAvailabilityAsync(new CheckAvailabilityRequestDto
            {
                ReservationDate = FutureDay, ReservationTime = "20:00", ReservationEndTime = "21:00", NumberOfGuests = 2
            });

            Assert.False(result.IsFullyBooked);
            Assert.Equal(MediumTableId, Assert.Single(result.AvailableTables).DiningTableId);
        }

        [Fact]
        public async Task MapStatus_MarksOnlyOverlappingTableAsBusy()
        {
            await BookAsync(UserA, CreateDto(FutureDay, guests: 2));

            await using var db = CreateDbContext();
            var statuses = (await CreateReservationManager(db).GetTableStatusesForMapAsync(FutureDay, "20:00", "21:00")).ToList();

            Assert.False(statuses.Single(s => s.DiningTableId == SmallTableId).IsAvailable);
            Assert.True(statuses.Single(s => s.DiningTableId == MediumTableId).IsAvailable);
        }

        [Fact]
        public async Task UpdateReservation_KeepsSlotFreeForItself()
        {
            var id = await BookAsync(UserA, CreateDto(FutureDay, guests: 2, start: "19:00", end: "21:00"));

            await using (var db = CreateDbContext())
                await CreateReservationManager(db).UpdateReservationAsync(UserA.ToString(), new ReservationUpdateDto
                {
                    ReservationId = id, ReservationDate = FutureDay, ReservationTime = "19:30", ReservationEndTime = "21:30", NumberOfGuests = 2
                });

            await using var check = CreateDbContext();
            var reservation = check.Reservations.Single();
            Assert.Equal("19:30", reservation.ReservationTime);
            Assert.Equal(SmallTableId, reservation.DiningTableId);
        }

        [Fact]
        public async Task CancelReservation_OtherUsersReservation_ThrowsForbidden()
        {
            var id = await BookAsync(UserA, CreateDto(FutureDay));

            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateReservationManager(db).CancelReservationAsync(UserB.ToString(), id));
            Assert.Equal("Forbidden", ex.PropertyName);
        }

        [Fact]
        public async Task AdminReactivatesCancelledReservation_WhenTableTakenMeanwhile_Throws()
        {
            // A'nın 3 kişilik rezervasyonu için tek uygun masa MediumTable. A iptal eder, masa B'ye verilir.
            var reservationA = await BookAsync(UserA, CreateDto(FutureDay, guests: 3));
            await using (var db = CreateDbContext())
                await CreateReservationManager(db).CancelReservationAsync(UserA.ToString(), reservationA);
            await BookAsync(UserB, CreateDto(FutureDay, guests: 3));

            await using (var db = CreateDbContext())
            {
                var ex = await Assert.ThrowsAsync<LogicException>(() => CreateReservationManager(db).UpdateReservationStatusAsync(
                    new UpdateReservationDto { ReservationId = reservationA, ReservationStatus = ReservationStatus.Approved }));
                Assert.Equal("TableNotAvailable", ex.PropertyName);
            }

            await using var check = CreateDbContext();
            Assert.Equal(ReservationStatus.Cancelled, check.Reservations.Single(r => r.ReservationId == reservationA).ReservationStatus);
        }

        [Fact]
        public async Task AdminReactivatesCancelledReservation_WhenTableStillFree_Succeeds()
        {
            var id = await BookAsync(UserA, CreateDto(FutureDay));
            await using (var db = CreateDbContext())
                await CreateReservationManager(db).CancelReservationAsync(UserA.ToString(), id);

            await using (var db = CreateDbContext())
                await CreateReservationManager(db).UpdateReservationStatusAsync(
                    new UpdateReservationDto { ReservationId = id, ReservationStatus = ReservationStatus.Approved });

            await using var check = CreateDbContext();
            Assert.Equal(ReservationStatus.Approved, check.Reservations.Single().ReservationStatus);
        }

        [Fact]
        public async Task AdminReactivatesCancelledReservation_WhenTableInactive_Throws()
        {
            var id = await BookAsync(UserA, CreateDto(FutureDay));
            await using (var db = CreateDbContext())
            {
                await CreateReservationManager(db).CancelReservationAsync(UserA.ToString(), id);
                db.DiningTables.Single(t => t.DiningTableId == SmallTableId).IsActive = false;
                await db.SaveChangesAsync();
            }

            await using var ctx = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateReservationManager(ctx).UpdateReservationStatusAsync(
                new UpdateReservationDto { ReservationId = id, ReservationStatus = ReservationStatus.Pending }));
            Assert.Equal("TableNotAvailable", ex.PropertyName);
        }

        [Fact]
        public async Task AdminChangesCompletedReservation_Throws()
        {
            var id = await BookAsync(UserA, CreateDto(FutureDay));
            await using (var db = CreateDbContext())
            {
                db.Reservations.Single().ReservationStatus = ReservationStatus.Completed;
                await db.SaveChangesAsync();
            }

            await using var ctx = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateReservationManager(ctx).UpdateReservationStatusAsync(
                new UpdateReservationDto { ReservationId = id, ReservationStatus = ReservationStatus.Approved }));
            Assert.Equal("NotAllowed", ex.PropertyName);
        }

        [Fact]
        public async Task AdminSetsSameStatus_Throws()
        {
            var id = await BookAsync(UserA, CreateDto(FutureDay));

            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateReservationManager(db).UpdateReservationStatusAsync(
                new UpdateReservationDto { ReservationId = id, ReservationStatus = ReservationStatus.Pending }));
            Assert.Equal("SameStatus", ex.PropertyName);
        }

        [Fact]
        public async Task LockedTransaction_RollsBackWhenActionThrows()
        {
            await using (var db = CreateDbContext())
            {
                var uow = new UnitOfWork(db);
                await Assert.ThrowsAsync<LogicException>(() => uow.ExecuteInLockedTransactionAsync("test", async () =>
                {
                    db.DiningTables.Add(new DiningTable { TableNo = "Geçici", Capacity = 2 });
                    await uow.SaveAsync();
                    throw new LogicException("Test", "kasıtlı hata");
                }));
            }

            await using var check = CreateDbContext();
            Assert.False(await check.DiningTables.AnyAsync(t => t.TableNo == "Geçici"));
        }
    }
}

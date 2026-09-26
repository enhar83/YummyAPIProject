using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.DiningTableDTOs;
using Yummy.Entity;
using Yummy.Core.Exceptions;
using Yummy.Entity.Enums;
using Yummy.Tests.Infrastructure;

namespace Yummy.Tests.DiningTables
{
    public class DiningTableManagerTests : SqliteTestBase
    {
        private static DiningTableUpdateDto Deactivate(Guid tableId, string tableNo, int capacity) => new()
        {
            DiningTableId = tableId, TableNo = tableNo, Capacity = capacity, IsActive = false
        };

        private async Task<Guid> BookAsync(Guid userId, DateTime date, int guests = 2)
        {
            await using var db = CreateDbContext();
            await CreateReservationManager(db).AddReservationAsync(userId.ToString(), CreateDto(date, guests));
            return db.Reservations.OrderByDescending(r => r.CreatedDate).First().ReservationId;
        }

        [Fact]
        public async Task Deactivate_WithFutureActiveReservation_Throws()
        {
            await BookAsync(UserA, FutureDay);

            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateDiningTableManager(db).UpdateAsync(Deactivate(SmallTableId, "Masa 1", 2)));
            Assert.Equal("TableHasActiveReservations", ex.PropertyName);
        }

        [Fact]
        public async Task Deactivate_WithOnlyCancelledReservation_Succeeds_AndHistoryStaysVisible()
        {
            var id = await BookAsync(UserA, FutureDay);
            await using (var db = CreateDbContext())
                await CreateReservationManager(db).CancelReservationAsync(UserA.ToString(), id);

            await using (var db = CreateDbContext())
                await CreateDiningTableManager(db).UpdateAsync(Deactivate(SmallTableId, "Masa 1", 2));

            await using var check = CreateDbContext();
            var manager = CreateReservationManager(check);

            var mine = Assert.Single(await manager.SeeMyPastReservationsAsync(UserA.ToString()));
            Assert.Equal("Masa 1", mine.TableNo);
            Assert.Single((await manager.GetAllReservationsAsync(new PaginationQueryDto())).Items);
            Assert.Equal("Masa 1", (await manager.GetUserReservationByIdAsync(UserA.ToString(), id)).TableNo);
            Assert.Equal("Masa 1", (await manager.GetReservationByIdAsync(id)).TableNo);
        }

        [Fact]
        public async Task Deactivate_WithPastCompletedReservation_KeepsHistoryVisible()
        {
            await using (var db = CreateDbContext())
            {
                db.Reservations.Add(new Yummy.Entity.Reservation
                {
                    Name = "Eski", Surname = "Kayıt", Email = "a@test.com", Phone = "1", Message = "",
                    ReservationDate = Today.AddDays(-10), ReservationTime = "19:00", ReservationEndTime = "21:00",
                    NumberOfGuests = 2, ReservationStatus = ReservationStatus.Completed, AppUserId = UserA, DiningTableId = SmallTableId
                });
                await db.SaveChangesAsync();
            }

            await using (var db = CreateDbContext())
                await CreateDiningTableManager(db).UpdateAsync(Deactivate(SmallTableId, "Masa 1", 2));

            await using var check = CreateDbContext();
            var tables = await CreateDiningTableManager(check).GetAllAsync();
            Assert.False(tables.Single(t => t.DiningTableId == SmallTableId).IsActive); // admin pasif masayı görmeye devam eder
            Assert.Single(await CreateReservationManager(check).SeeMyPastReservationsAsync(UserA.ToString()));
        }

        [Fact]
        public async Task Create_WithoutIsActiveInRequest_TableIsActive()
        {
            var dto = JsonSerializer.Deserialize<DiningTableCreateDto>("""{ "tableNo": "Masa 9", "capacity": 4 }""", new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

            await using (var db = CreateDbContext())
                await CreateDiningTableManager(db).AddAsync(dto);

            await using var check = CreateDbContext();
            Assert.True(check.DiningTables.Single(t => t.TableNo == "Masa 9").IsActive);
        }

        [Theory]
        [InlineData("Masa 1")]
        [InlineData("  Masa 1  ")]
        public async Task Create_WithExistingTableNo_Throws(string tableNo)
        {
            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() =>
                CreateDiningTableManager(db).AddAsync(new DiningTableCreateDto { TableNo = tableNo, Capacity = 2 }));
            Assert.Equal("TableNo", ex.PropertyName);
        }

        [Fact]
        public async Task Update_ToAnotherTablesNo_Throws()
        {
            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateDiningTableManager(db).UpdateAsync(new DiningTableUpdateDto
            {
                DiningTableId = MediumTableId, TableNo = "Masa 1", Capacity = 4, IsActive = true
            }));
            Assert.Equal("TableNo", ex.PropertyName);
        }

        [Fact]
        public async Task DuplicateTableNo_IsRejectedByDatabaseIndex()
        {
            await using var db = CreateDbContext();
            db.DiningTables.Add(new DiningTable { TableNo = "Masa 1", Capacity = 2 });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task ReduceCapacity_BelowActiveReservation_Throws()
        {
            await BookAsync(UserA, FutureDay, guests: 4); // MediumTable (4 kişilik)

            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateDiningTableManager(db).UpdateAsync(new DiningTableUpdateDto
            {
                DiningTableId = MediumTableId, TableNo = "Masa 2", Capacity = 3, IsActive = true
            }));
            Assert.Equal("CapacityConflict", ex.PropertyName);
        }

        [Fact]
        public async Task ReduceCapacity_WhenReservationsStillFit_Succeeds()
        {
            await BookAsync(UserA, FutureDay, guests: 3); // MediumTable (4 kişilik)

            await using (var db = CreateDbContext())
                await CreateDiningTableManager(db).UpdateAsync(new DiningTableUpdateDto
                {
                    DiningTableId = MediumTableId, TableNo = "Masa 2", Capacity = 3, IsActive = true
                });

            await using var check = CreateDbContext();
            Assert.Equal(3, check.DiningTables.Single(t => t.DiningTableId == MediumTableId).Capacity);
        }

        [Fact]
        public async Task Update_ActiveTableWithReservations_OtherFieldsCanStillChange()
        {
            await BookAsync(UserA, FutureDay);

            await using (var db = CreateDbContext())
                await CreateDiningTableManager(db).UpdateAsync(new DiningTableUpdateDto
                {
                    DiningTableId = SmallTableId, TableNo = "Masa 1", Capacity = 2, IsActive = true, Location = "Bahçe"
                });

            await using var check = CreateDbContext();
            Assert.Equal("Bahçe", (await CreateDiningTableManager(check).GetByIdAsync(SmallTableId)).Location);
        }
    }
}

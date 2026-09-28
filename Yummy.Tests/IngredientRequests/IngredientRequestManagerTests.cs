using Microsoft.EntityFrameworkCore;
using Yummy.Business.Validators.IngredientRequestValidators;
using Yummy.Business.Validators.IngredientValidators;
using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.IngredientDTOs;
using Yummy.Core.DTOs.IngredientRequestDTOs;
using Yummy.Core.Exceptions;
using Yummy.Data;
using Yummy.Entity;
using Yummy.Entity.Enums;
using Yummy.Tests.Infrastructure;

namespace Yummy.Tests.IngredientRequests
{
    // malzeme talebi akışı: şef talep eder → çalışan tedarik eder / reddeder → stok ve şef bildirimi.
    public class IngredientRequestManagerTests : SqliteTestBase
    {
        // UserA → Şef Ayşe, UserB → Şef Mehmet, EmployeeId → çalışan (şef profili yok).
        private static readonly Guid ChefAId = Guid.NewGuid();
        private static readonly Guid ChefBId = Guid.NewGuid();
        private static readonly Guid EmployeeId = Guid.NewGuid();
        private static readonly Guid TomatoId = Guid.NewGuid(); // kg, stok 2
        private static readonly Guid MilkId = Guid.NewGuid();   // L, stok 0

        public IngredientRequestManagerTests()
        {
            using var db = CreateDbContext();
            db.Users.Add(new AppUser { Id = EmployeeId, UserName = "employee", Name = "Ece", Surname = "Çalışan", Email = "employee@test.com", SecurityStamp = "s" });
            db.Chefs.AddRange(
                new Chef { ChefId = ChefAId, Name = "Ayşe", Surname = "Yılmaz", Title = "Baş Şef", Description = "", ImageUrl = "", AppUserId = UserA },
                new Chef { ChefId = ChefBId, Name = "Mehmet", Surname = "Demir", Title = "Şef", Description = "", ImageUrl = "", AppUserId = UserB });
            db.Ingredients.AddRange(
                new Ingredient { IngredientId = TomatoId, Name = "Domates", Unit = IngredientUnit.Kilogram, StockQuantity = 2 },
                new Ingredient { IngredientId = MilkId, Name = "Süt", Unit = IngredientUnit.Liter, StockQuantity = 0 });
            db.SaveChanges();
        }

        private static IngredientRequestCreateDto Request(params (Guid IngredientId, decimal Quantity)[] items) => new()
        {
            Items = items.Select(i => new IngredientRequestItemCreateDto { IngredientId = i.IngredientId, Quantity = i.Quantity }).ToList(),
            Note = "Cuma menüsü için"
        };

        private async Task<Guid> CreateAsync(Guid chefUserId, params (Guid IngredientId, decimal Quantity)[] items)
        {
            await using var db = CreateDbContext();
            return await CreateIngredientRequestManager(db).CreateAsync(chefUserId.ToString(), Request(items));
        }

        private async Task<IngredientRequest> GetAsync(Guid requestId)
        {
            await using var db = CreateDbContext();
            return await db.IngredientRequests.Include(r => r.Items).SingleAsync(r => r.IngredientRequestId == requestId);
        }

        private async Task<decimal> StockAsync(Guid ingredientId)
        {
            await using var db = CreateDbContext();
            return (await db.Ingredients.IgnoreQueryFilters().SingleAsync(i => i.IngredientId == ingredientId)).StockQuantity;
        }

        // talepteki her satır için tedarik miktarı: satır, malzeme kimliği ile eşlenir.
        private async Task SupplyAsync(Guid requestId, Dictionary<Guid, decimal> quantitiesByIngredient, string? note = null)
        {
            var request = await GetAsync(requestId);
            await using var db = CreateDbContext();
            await CreateIngredientRequestManager(db).SupplyAsync(EmployeeId.ToString(), requestId, new IngredientRequestSupplyDto
            {
                Items = request.Items.Select(i => new IngredientRequestSupplyItemDto { IngredientRequestItemId = i.IngredientRequestItemId, SuppliedQuantity = quantitiesByIngredient[i.IngredientId] }).ToList(),
                Note = note
            });
        }

        private async Task RejectAsync(Guid requestId, string note = "Tedarikçide yok")
        {
            await using var db = CreateDbContext();
            await CreateIngredientRequestManager(db).RejectAsync(EmployeeId.ToString(), requestId, new IngredientRequestRejectDto { Note = note });
        }

        private async Task CancelAsync(Guid chefUserId, Guid requestId)
        {
            await using var db = CreateDbContext();
            await CreateIngredientRequestManager(db).CancelAsync(chefUserId.ToString(), requestId);
        }

        // ---------- talep oluşturma ----------

        [Fact]
        public async Task Create_SavesPendingRequest_WithIngredientSnapshot()
        {
            var id = await CreateAsync(UserA, (TomatoId, 5), (MilkId, 1.5m));

            var request = await GetAsync(id);
            Assert.Equal(IngredientRequestStatus.Pending, request.Status);
            Assert.Equal(ChefAId, request.ChefId);
            Assert.Equal("Ayşe Yılmaz", request.ChefName);
            Assert.Equal("Cuma menüsü için", request.ChefNote);

            var milk = request.Items.Single(i => i.IngredientId == MilkId);
            Assert.Equal("Süt", milk.IngredientName);
            Assert.Equal(IngredientUnit.Liter, milk.Unit);
            Assert.Equal(1.5m, milk.RequestedQuantity);
            Assert.Null(milk.SuppliedQuantity);

            Assert.Equal(2, await StockAsync(TomatoId)); // talep stoğu değiştirmez; stok tedarikte artar.
        }

        [Fact]
        public async Task Create_ByUserWithoutChefProfile_Throws()
        {
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateAsync(EmployeeId, (TomatoId, 1)));
            Assert.Equal("ChefProfileNotFound", ex.PropertyName);
        }

        [Fact]
        public async Task Create_WithUnknownOrDeletedIngredient_Throws()
        {
            await using (var db = CreateDbContext())
                await CreateIngredientManager(db).DeleteAsync(MilkId); // stok 0, bekleyen talep yok → silinebilir

            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateAsync(UserA, (TomatoId, 1), (MilkId, 1)));
            Assert.Equal("IngredientNotFound", ex.PropertyName);

            ex = await Assert.ThrowsAsync<LogicException>(() => CreateAsync(UserA, (Guid.NewGuid(), 1)));
            Assert.Equal("IngredientNotFound", ex.PropertyName);
        }

        // ---------- tedarik ----------

        [Fact]
        public async Task Supply_AddsSuppliedQuantitiesToStock_AndRecordsMovements()
        {
            var id = await CreateAsync(UserA, (TomatoId, 5), (MilkId, 2));

            // domates istenenden az geldi, süt hiç bulunamadı.
            await SupplyAsync(id, new() { [TomatoId] = 3, [MilkId] = 0 }, "Süt yarın gelecek");

            var request = await GetAsync(id);
            Assert.Equal(IngredientRequestStatus.Supplied, request.Status);
            Assert.Equal("Süt yarın gelecek", request.ResponseNote);
            Assert.Equal(EmployeeId, request.HandledByUserId);
            Assert.Equal(Clock.GetUtcNow().UtcDateTime, request.HandledDate);
            Assert.Equal(3, request.Items.Single(i => i.IngredientId == TomatoId).SuppliedQuantity);
            Assert.Equal(0, request.Items.Single(i => i.IngredientId == MilkId).SuppliedQuantity);

            Assert.Equal(5, await StockAsync(TomatoId));
            Assert.Equal(0, await StockAsync(MilkId));

            await using var db = CreateDbContext();
            var movement = await db.StockMovements.SingleAsync(); // süt için (0) hareket oluşmaz
            Assert.Equal(StockMovementType.RequestSupply, movement.Type);
            Assert.Equal(TomatoId, movement.IngredientId);
            Assert.Equal(3, movement.QuantityChange);
            Assert.Equal(5, movement.QuantityAfter);
            Assert.Equal(id, movement.IngredientRequestId);
            Assert.Equal(EmployeeId, movement.PerformedByUserId);
        }

        [Fact]
        public async Task Supply_MoreThanRequested_IsAllowed()
        {
            var id = await CreateAsync(UserA, (TomatoId, 4));

            await SupplyAsync(id, new() { [TomatoId] = 5 }); // 5 kg'lık kasa geldi

            Assert.Equal(7, await StockAsync(TomatoId));
        }

        [Fact]
        public async Task Supply_EmailsChef_WithEncodedContent()
        {
            var id = await CreateAsync(UserA, (TomatoId, 2.5m));

            await SupplyAsync(id, new() { [TomatoId] = 2.5m }, "<script>alert(1)</script>");

            var email = Assert.Single(Email.SentEmails);
            Assert.Equal("a@test.com", email.To);
            Assert.Contains("Domates", email.Body);
            Assert.Contains("2,5 kg", email.Body);
            Assert.Contains("Malzemeleriniz Hazır", email.Body);
            Assert.DoesNotContain("<script>", email.Body);
            Assert.Contains("&lt;script&gt;", email.Body);
            Assert.DoesNotContain("{{", email.Body); // tüm yer tutucular doldurulmuş olmalı
        }

        [Fact]
        public async Task Supply_WhenEmailFails_IsStillSaved()
        {
            var id = await CreateAsync(UserA, (TomatoId, 1));
            Email.ShouldFail = true;

            await SupplyAsync(id, new() { [TomatoId] = 1 });

            Assert.Equal(IngredientRequestStatus.Supplied, (await GetAsync(id)).Status);
            Assert.Equal(3, await StockAsync(TomatoId));
        }

        [Fact]
        public async Task Supply_WhenChefAccountUnlinked_SavesWithoutEmail()
        {
            var id = await CreateAsync(UserA, (TomatoId, 1));
            await using (var db = CreateDbContext())
            {
                (await db.Chefs.SingleAsync(c => c.ChefId == ChefAId)).AppUserId = null;
                await db.SaveChangesAsync();
            }

            await SupplyAsync(id, new() { [TomatoId] = 1 });

            Assert.Equal(IngredientRequestStatus.Supplied, (await GetAsync(id)).Status);
            Assert.Empty(Email.SentEmails);
        }

        [Fact]
        public async Task Supply_WithMissingOrForeignItem_Throws_AndNothingChanges()
        {
            var id = await CreateAsync(UserA, (TomatoId, 1), (MilkId, 1));
            var tomatoItem = (await GetAsync(id)).Items.Single(i => i.IngredientId == TomatoId);

            await using (var db = CreateDbContext())
            {
                var manager = CreateIngredientRequestManager(db);

                // süt satırı eksik
                var ex = await Assert.ThrowsAsync<LogicException>(() => manager.SupplyAsync(EmployeeId.ToString(), id, new IngredientRequestSupplyDto
                {
                    Items = new() { new() { IngredientRequestItemId = tomatoItem.IngredientRequestItemId, SuppliedQuantity = 1 } }
                }));
                Assert.Equal("ItemsMismatch", ex.PropertyName);

                // talebe ait olmayan bir satır
                ex = await Assert.ThrowsAsync<LogicException>(() => manager.SupplyAsync(EmployeeId.ToString(), id, new IngredientRequestSupplyDto
                {
                    Items = new()
                    {
                        new() { IngredientRequestItemId = tomatoItem.IngredientRequestItemId, SuppliedQuantity = 1 },
                        new() { IngredientRequestItemId = Guid.NewGuid(), SuppliedQuantity = 1 }
                    }
                }));
                Assert.Equal("ItemsMismatch", ex.PropertyName);
            }

            Assert.Equal(IngredientRequestStatus.Pending, (await GetAsync(id)).Status);
            Assert.Equal(2, await StockAsync(TomatoId));
        }

        [Fact]
        public async Task Supply_Twice_ThrowsNotPending_AndStockIsAddedOnce()
        {
            var id = await CreateAsync(UserA, (TomatoId, 1));
            await SupplyAsync(id, new() { [TomatoId] = 1 });

            var ex = await Assert.ThrowsAsync<LogicException>(() => SupplyAsync(id, new() { [TomatoId] = 1 }));
            Assert.Equal("NotPending", ex.PropertyName);
            Assert.Equal(3, await StockAsync(TomatoId));
        }

        [Fact]
        public async Task Supply_WhileChefCancels_IsRejected_AndStockUnchanged()
        {
            var id = await CreateAsync(UserA, (TomatoId, 1));
            var itemId = (await GetAsync(id)).Items.Single().IngredientRequestItemId;

            // çalışan talebi açtıktan sonra, tedarik kilidi alınmadan hemen önce şef talebi iptal eder.
            await using (var db = CreateDbContext())
            {
                var manager = CreateIngredientRequestManager(db, new InterleavingUnitOfWork(new UnitOfWork(db), () => CancelAsync(UserA, id)));

                var ex = await Assert.ThrowsAsync<LogicException>(() => manager.SupplyAsync(EmployeeId.ToString(), id, new IngredientRequestSupplyDto
                {
                    Items = new() { new() { IngredientRequestItemId = itemId, SuppliedQuantity = 1 } }
                }));
                Assert.Equal("NotPending", ex.PropertyName);
            }

            Assert.Equal(IngredientRequestStatus.Cancelled, (await GetAsync(id)).Status);
            Assert.Equal(2, await StockAsync(TomatoId));
        }

        // ---------- red ve iptal ----------

        [Fact]
        public async Task Reject_SetsRejectedWithReason_EmailsChef_AndStockUnchanged()
        {
            var id = await CreateAsync(UserA, (TomatoId, 1));

            await RejectAsync(id, "Sezonu değil");

            var request = await GetAsync(id);
            Assert.Equal(IngredientRequestStatus.Rejected, request.Status);
            Assert.Equal("Sezonu değil", request.ResponseNote);
            Assert.Equal(EmployeeId, request.HandledByUserId);
            Assert.Equal(2, await StockAsync(TomatoId));

            var email = Assert.Single(Email.SentEmails);
            Assert.Contains("Talebiniz Reddedildi", email.Body);
            Assert.Contains("Sezonu değil", email.Body);
        }

        [Fact]
        public async Task Cancel_ByChef_OnlyWhilePending()
        {
            var id = await CreateAsync(UserA, (TomatoId, 1));

            await CancelAsync(UserA, id);
            Assert.Equal(IngredientRequestStatus.Cancelled, (await GetAsync(id)).Status);

            var ex = await Assert.ThrowsAsync<LogicException>(() => CancelAsync(UserA, id));
            Assert.Equal("NotPending", ex.PropertyName);

            ex = await Assert.ThrowsAsync<LogicException>(() => RejectAsync(id));
            Assert.Equal("NotPending", ex.PropertyName);
            Assert.Empty(Email.SentEmails); // şefin kendi iptali için e-posta gönderilmez
        }

        [Fact]
        public async Task Chef_CannotSeeOrCancelAnotherChefsRequest()
        {
            var id = await CreateAsync(UserA, (TomatoId, 1));

            var ex = await Assert.ThrowsAsync<LogicException>(() => CancelAsync(UserB, id));
            Assert.Equal("NotFound", ex.PropertyName);

            await using var db = CreateDbContext();
            ex = await Assert.ThrowsAsync<LogicException>(() => CreateIngredientRequestManager(db).GetMyRequestByIdAsync(UserB.ToString(), id));
            Assert.Equal("NotFound", ex.PropertyName);
            Assert.Equal(IngredientRequestStatus.Pending, (await GetAsync(id)).Status);
        }

        // ---------- listeleme ----------

        [Fact]
        public async Task Lists_AreScopedAndFilteredByStatus()
        {
            var a1 = await CreateAsync(UserA, (TomatoId, 1));
            var a2 = await CreateAsync(UserA, (MilkId, 1));
            var b1 = await CreateAsync(UserB, (TomatoId, 1));
            await RejectAsync(a1);

            await using var db = CreateDbContext();
            var manager = CreateIngredientRequestManager(db);

            var mine = await manager.GetMyRequestsAsync(UserA.ToString(), new IngredientRequestQueryDto());
            Assert.Equal(new[] { a1, a2 }.OrderBy(x => x), mine.Items.Select(r => r.IngredientRequestId).OrderBy(x => x));

            var minePending = await manager.GetMyRequestsAsync(UserA.ToString(), new IngredientRequestQueryDto { Status = IngredientRequestStatus.Pending });
            Assert.Equal(a2, Assert.Single(minePending.Items).IngredientRequestId);

            var allPending = await manager.GetAllAsync(new IngredientRequestQueryDto { Status = IngredientRequestStatus.Pending });
            Assert.Equal(2, allPending.TotalCount);
            Assert.DoesNotContain(allPending.Items, r => r.IngredientRequestId == a1);

            var rejected = await manager.GetByIdAsync(a1);
            Assert.Equal("Reddedildi", rejected.StatusName);
            Assert.Equal("Ece Çalışan", rejected.HandledBy);
            Assert.Equal("kg", Assert.Single(rejected.Items).UnitName);
            Assert.Contains(allPending.Items, r => r.IngredientRequestId == b1 && r.ChefName == "Mehmet Demir");
        }

        [Fact]
        public async Task StockMovementList_ShowsRequestSupplyType()
        {
            var id = await CreateAsync(UserA, (TomatoId, 1));
            await SupplyAsync(id, new() { [TomatoId] = 1 });

            await using var db = CreateDbContext();
            var movements = await CreateIngredientManager(db).GetStockMovementsAsync(TomatoId, new PaginationQueryDto());
            var movement = Assert.Single(movements.Items);
            Assert.Equal("Talep Tedariki", movement.TypeName);
            Assert.Equal("Ece Çalışan", movement.PerformedBy);
        }

        // ---------- stok kartı ve şef kuralları ----------

        [Fact]
        public async Task IngredientInPendingRequest_CannotBeDeleted_ButCanAfterRequestIsResolved()
        {
            var id = await CreateAsync(UserA, (MilkId, 1)); // süt stoğu 0

            await using (var db = CreateDbContext())
            {
                var ex = await Assert.ThrowsAsync<LogicException>(() => CreateIngredientManager(db).DeleteAsync(MilkId));
                Assert.Equal("IngredientInPendingRequest", ex.PropertyName);
            }

            await RejectAsync(id);

            await using (var db = CreateDbContext())
                await CreateIngredientManager(db).DeleteAsync(MilkId);

            // silinen malzeme geçmiş talepte adıyla görünmeye devam eder.
            await using var check = CreateDbContext();
            var request = await CreateIngredientRequestManager(check).GetByIdAsync(id);
            Assert.Equal("Süt", Assert.Single(request.Items).IngredientName);
        }

        [Fact]
        public async Task IngredientInAnyRequest_UnitCannotChange()
        {
            await CreateAsync(UserA, (MilkId, 1)); // sütün hiç stok hareketi yok, ama talepte "L" olarak geçiyor

            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateIngredientManager(db).UpdateAsync(new IngredientUpdateDto { IngredientId = MilkId, Name = "Süt", Unit = IngredientUnit.Milliliter }));
            Assert.Equal("UnitLocked", ex.PropertyName);
        }

        [Fact]
        public async Task DeletingChef_CancelsOnlyPendingRequests()
        {
            var pending = await CreateAsync(UserA, (TomatoId, 1));
            var supplied = await CreateAsync(UserA, (MilkId, 1));
            await SupplyAsync(supplied, new() { [MilkId] = 1 });

            await using (var db = CreateDbContext())
                await CreateChefManager(db).DeleteAsync(ChefAId);

            var cancelled = await GetAsync(pending);
            Assert.Equal(IngredientRequestStatus.Cancelled, cancelled.Status);
            Assert.Equal("Şef profili silindiği için talep iptal edildi.", cancelled.ResponseNote);
            Assert.Equal(IngredientRequestStatus.Supplied, (await GetAsync(supplied)).Status);

            // silinen şefin geçmiş talepleri çalışan listesinde görünmeye devam eder.
            await using var check = CreateDbContext();
            Assert.Equal(2, (await CreateIngredientRequestManager(check).GetAllAsync(new IngredientRequestQueryDto())).TotalCount);
        }

        // ---------- validator'lar ----------

        [Fact]
        public void CreateValidator_Rules()
        {
            var validator = new IngredientRequestCreateValidator();

            Assert.True(validator.Validate(Request((TomatoId, 1))).IsValid);
            Assert.False(validator.Validate(new IngredientRequestCreateDto()).IsValid); // boş liste
            Assert.False(validator.Validate(new IngredientRequestCreateDto { Items = null! }).IsValid); // null liste hata fırlatmaz
            Assert.False(validator.Validate(Request((TomatoId, 1), (TomatoId, 2))).IsValid); // aynı malzeme iki kez
            Assert.False(validator.Validate(Request((TomatoId, 0))).IsValid);
            Assert.False(validator.Validate(Request((TomatoId, 1.0001m))).IsValid);
            Assert.False(validator.Validate(Request((Guid.Empty, 1))).IsValid);
            Assert.False(validator.Validate(Request(Enumerable.Range(0, 31).Select(_ => (Guid.NewGuid(), 1m)).ToArray())).IsValid);
            Assert.False(validator.Validate(Request((TomatoId, 1)) with { Note = new string('a', 501) }).IsValid);
        }

        [Fact]
        public void SupplyValidator_Rules()
        {
            var validator = new IngredientRequestSupplyValidator();
            IngredientRequestSupplyDto Supply(params decimal[] quantities) =>
                new() { Items = quantities.Select(q => new IngredientRequestSupplyItemDto { IngredientRequestItemId = Guid.NewGuid(), SuppliedQuantity = q }).ToList() };

            Assert.True(validator.Validate(Supply(1, 0)).IsValid);
            Assert.False(validator.Validate(Supply(0, 0)).IsValid); // hiçbiri gelmediyse reddedilmeli
            Assert.False(validator.Validate(Supply(1, -1)).IsValid);
            Assert.False(validator.Validate(new IngredientRequestSupplyDto()).IsValid);
            Assert.False(validator.Validate(new IngredientRequestSupplyDto { Items = null! }).IsValid);

            var itemId = Guid.NewGuid();
            Assert.False(validator.Validate(new IngredientRequestSupplyDto
            {
                Items = new() { new() { IngredientRequestItemId = itemId, SuppliedQuantity = 1 }, new() { IngredientRequestItemId = itemId, SuppliedQuantity = 1 } }
            }).IsValid);
        }

        [Fact]
        public void RejectAndQueryValidators_Rules()
        {
            Assert.False(new IngredientRequestRejectValidator().Validate(new IngredientRequestRejectDto { Note = " " }).IsValid);
            Assert.True(new IngredientRequestRejectValidator().Validate(new IngredientRequestRejectDto { Note = "Stok yok" }).IsValid);

            var queryValidator = new IngredientRequestQueryValidator();
            Assert.True(queryValidator.Validate(new IngredientRequestQueryDto { Status = IngredientRequestStatus.Pending }).IsValid);
            Assert.False(queryValidator.Validate(new IngredientRequestQueryDto { Status = (IngredientRequestStatus)9 }).IsValid);
            Assert.False(queryValidator.Validate(new IngredientRequestQueryDto { PageSize = 1000 }).IsValid); // ortak sayfalama kuralı
        }

        [Fact]
        public void ManualStockAdjustment_CannotUseSystemMovementType()
        {
            // RequestSupply (4) sadece talep tedarikinde sistem tarafından oluşturulur; elle girilemez.
            var result = new StockAdjustmentValidator().Validate(new StockAdjustmentDto { Type = (StockAdjustmentType)(int)StockMovementType.RequestSupply, Quantity = 1 });
            Assert.False(result.IsValid);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Yummy.Business.Managers;
using Yummy.Business.Validators.IngredientValidators;
using Yummy.Core.DTOs.CommonDTOs;
using Yummy.Core.DTOs.IngredientDTOs;
using Yummy.Core.Exceptions;
using Yummy.Data;
using Yummy.Data.Context;
using Yummy.Data.Repositories;
using Yummy.Entity;
using Yummy.Entity.Enums;
using Yummy.Tests.Infrastructure;

namespace Yummy.Tests.Ingredients
{
    // stok kartları, stok hareketleri ve stok kuralları.
    public class IngredientManagerTests : SqliteTestBase
    {
        private async Task<Guid> CreateAsync(string name, IngredientUnit unit = IngredientUnit.Kilogram)
        {
            await using var db = CreateDbContext();
            await CreateIngredientManager(db).AddAsync(new IngredientCreateDto { Name = name, Unit = unit });
            return (await db.Ingredients.SingleAsync(i => i.Name == name.Trim())).IngredientId;
        }

        private async Task AdjustAsync(Guid id, StockAdjustmentType type, decimal quantity, string? note = null)
        {
            await using var db = CreateDbContext();
            await CreateIngredientManager(db).AdjustStockAsync(UserA.ToString(), id, new StockAdjustmentDto { Type = type, Quantity = quantity, Note = note });
        }

        private async Task<Ingredient> GetAsync(Guid id)
        {
            await using var db = CreateDbContext();
            return await db.Ingredients.IgnoreQueryFilters().SingleAsync(i => i.IngredientId == id);
        }

        private async Task<List<StockMovement>> GetMovementsAsync(Guid id)
        {
            await using var db = CreateDbContext();
            return await db.StockMovements.Where(m => m.IngredientId == id).ToListAsync();
        }

        // ---------- stok kartı ----------

        [Fact]
        public async Task Add_CreatesCardWithZeroStock_AndTrimmedName()
        {
            var id = await CreateAsync("  Domates  ");

            var ingredient = await GetAsync(id);
            Assert.Equal("Domates", ingredient.Name);
            Assert.Equal(0, ingredient.StockQuantity);
            Assert.Empty(await GetMovementsAsync(id));
        }

        [Theory]
        [InlineData("incir")]
        [InlineData("  İNCİR ")]
        public async Task Add_DuplicateName_IsRejected_UsingTurkishCaseRules(string name)
        {
            await CreateAsync("İncir");

            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateIngredientManager(db).AddAsync(new IngredientCreateDto { Name = name, Unit = IngredientUnit.Kilogram }));
            Assert.Equal("Name", ex.PropertyName);
        }

        [Fact]
        public async Task Database_RejectsDuplicateActiveName()
        {
            await CreateAsync("Tuz");

            await using var db = CreateDbContext();
            db.Ingredients.Add(new Ingredient { IngredientId = Guid.NewGuid(), Name = "Tuz", Unit = IngredientUnit.Gram });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task Update_ChangesNameAndUnit_WhenNoMovements()
        {
            var id = await CreateAsync("Süt", IngredientUnit.Kilogram);

            await using (var db = CreateDbContext())
                await CreateIngredientManager(db).UpdateAsync(new IngredientUpdateDto { IngredientId = id, Name = "Tam Yağlı Süt", Unit = IngredientUnit.Liter });

            var ingredient = await GetAsync(id);
            Assert.Equal("Tam Yağlı Süt", ingredient.Name);
            Assert.Equal(IngredientUnit.Liter, ingredient.Unit);
        }

        [Fact]
        public async Task Update_UnitChangeAfterMovement_Throws_ButNameCanChange()
        {
            var id = await CreateAsync("Un", IngredientUnit.Kilogram);
            await AdjustAsync(id, StockAdjustmentType.StockIn, 5);

            await using (var db = CreateDbContext())
            {
                var ex = await Assert.ThrowsAsync<LogicException>(() => CreateIngredientManager(db).UpdateAsync(new IngredientUpdateDto { IngredientId = id, Name = "Un", Unit = IngredientUnit.Gram }));
                Assert.Equal("UnitLocked", ex.PropertyName);
            }

            await using (var db = CreateDbContext())
                await CreateIngredientManager(db).UpdateAsync(new IngredientUpdateDto { IngredientId = id, Name = "Buğday Unu", Unit = IngredientUnit.Kilogram });

            var ingredient = await GetAsync(id);
            Assert.Equal("Buğday Unu", ingredient.Name);
            Assert.Equal(IngredientUnit.Kilogram, ingredient.Unit);
        }

        [Fact]
        public async Task Update_ToAnotherIngredientsName_Throws()
        {
            await CreateAsync("Biber");
            var id = await CreateAsync("Patlıcan");

            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateIngredientManager(db).UpdateAsync(new IngredientUpdateDto { IngredientId = id, Name = "biber", Unit = IngredientUnit.Kilogram }));
            Assert.Equal("Name", ex.PropertyName);
        }

        [Fact]
        public async Task Update_WhileStockIsAdjusted_DoesNotOverwriteStock()
        {
            var id = await CreateAsync("Pirinç");
            await AdjustAsync(id, StockAdjustmentType.StockIn, 10);

            // kart güncellemesi kilidi almadan hemen önce başka bir çalışan stok girişi yapar.
            await using (var db = CreateDbContext())
            {
                var manager = new IngredientManager(new GenericRepository<Ingredient>(db), new GenericRepository<StockMovement>(db),
                    new GenericRepository<IngredientRequestItem>(db), new InterleavingUnitOfWork(new UnitOfWork(db), () => AdjustAsync(id, StockAdjustmentType.StockIn, 5)), Mapper);

                await manager.UpdateAsync(new IngredientUpdateDto { IngredientId = id, Name = "Baldo Pirinç", Unit = IngredientUnit.Kilogram });
            }

            var ingredient = await GetAsync(id);
            Assert.Equal("Baldo Pirinç", ingredient.Name);
            Assert.Equal(15, ingredient.StockQuantity);
        }

        [Fact]
        public async Task Delete_WithStock_Throws()
        {
            var id = await CreateAsync("Zeytinyağı", IngredientUnit.Liter);
            await AdjustAsync(id, StockAdjustmentType.StockIn, 2);

            await using var db = CreateDbContext();
            var ex = await Assert.ThrowsAsync<LogicException>(() => CreateIngredientManager(db).DeleteAsync(id));
            Assert.Equal("StockNotEmpty", ex.PropertyName);
        }

        [Fact]
        public async Task Delete_AtZeroStock_SoftDeletes_KeepsHistory_AndNameCanBeReused()
        {
            var id = await CreateAsync("Maydanoz", IngredientUnit.Piece);
            await AdjustAsync(id, StockAdjustmentType.StockIn, 3);
            await AdjustAsync(id, StockAdjustmentType.Waste, 3, "Soldu");

            await using (var db = CreateDbContext())
                await CreateIngredientManager(db).DeleteAsync(id);

            Assert.True((await GetAsync(id)).IsDeleted);
            Assert.Equal(2, (await GetMovementsAsync(id)).Count);

            await using (var db = CreateDbContext())
                Assert.DoesNotContain(await CreateIngredientManager(db).GetAllAsync(), i => i.IngredientId == id);

            var newId = await CreateAsync("Maydanoz", IngredientUnit.Piece);
            Assert.NotEqual(id, newId);
        }

        // ---------- stok hareketleri ----------

        [Fact]
        public async Task StockIn_IncreasesStock_AndRecordsMovementWithPerformer()
        {
            var id = await CreateAsync("Tereyağı");

            await AdjustAsync(id, StockAdjustmentType.StockIn, 2.5m, "  Haftalık alım  ");

            Assert.Equal(2.5m, (await GetAsync(id)).StockQuantity);
            var movement = Assert.Single(await GetMovementsAsync(id));
            Assert.Equal(StockMovementType.StockIn, movement.Type);
            Assert.Equal(2.5m, movement.QuantityChange);
            Assert.Equal(2.5m, movement.QuantityAfter);
            Assert.Equal("Haftalık alım", movement.Note);
            Assert.Equal(UserA, movement.PerformedByUserId);
        }

        [Fact]
        public async Task Waste_MoreThanStock_Throws_AndNothingChanges()
        {
            var id = await CreateAsync("Yumurta", IngredientUnit.Piece);
            await AdjustAsync(id, StockAdjustmentType.StockIn, 10);

            var ex = await Assert.ThrowsAsync<LogicException>(() => AdjustAsync(id, StockAdjustmentType.Waste, 11));
            Assert.Equal("InsufficientStock", ex.PropertyName);

            Assert.Equal(10, (await GetAsync(id)).StockQuantity);
            Assert.Single(await GetMovementsAsync(id));
        }

        [Fact]
        public async Task Waste_DecreasesStock_AsNegativeMovement()
        {
            var id = await CreateAsync("Kıyma");
            await AdjustAsync(id, StockAdjustmentType.StockIn, 4);

            await AdjustAsync(id, StockAdjustmentType.Waste, 1.25m, "Bozuldu");

            Assert.Equal(2.75m, (await GetAsync(id)).StockQuantity);
            var waste = (await GetMovementsAsync(id)).Single(m => m.Type == StockMovementType.Waste);
            Assert.Equal(-1.25m, waste.QuantityChange);
            Assert.Equal(2.75m, waste.QuantityAfter);
        }

        [Theory]
        [InlineData(7.5, -2.5)]
        [InlineData(12, 2)]
        [InlineData(0, -10)]
        public async Task CountCorrection_SetsStockToCountedAmount_AndRecordsDifference(decimal counted, decimal expectedChange)
        {
            var id = await CreateAsync("Patates");
            await AdjustAsync(id, StockAdjustmentType.StockIn, 10);

            await AdjustAsync(id, StockAdjustmentType.CountCorrection, counted);

            Assert.Equal(counted, (await GetAsync(id)).StockQuantity);
            var correction = (await GetMovementsAsync(id)).Single(m => m.Type == StockMovementType.CountCorrection);
            Assert.Equal(expectedChange, correction.QuantityChange);
            Assert.Equal(counted, correction.QuantityAfter);
        }

        [Fact]
        public async Task CountCorrection_SameAsStock_ThrowsNoChanges()
        {
            var id = await CreateAsync("Soğan");
            await AdjustAsync(id, StockAdjustmentType.StockIn, 3);

            var ex = await Assert.ThrowsAsync<LogicException>(() => AdjustAsync(id, StockAdjustmentType.CountCorrection, 3));
            Assert.Equal("NoChanges", ex.PropertyName);
            Assert.Single(await GetMovementsAsync(id));
        }

        [Fact]
        public async Task AdjustStock_UnknownIngredient_ThrowsNotFound()
        {
            var ex = await Assert.ThrowsAsync<LogicException>(() => AdjustAsync(Guid.NewGuid(), StockAdjustmentType.StockIn, 1));
            Assert.Equal("NotFound", ex.PropertyName);
        }

        [Fact]
        public async Task GetStockMovements_NewestFirst_Paged_WithPerformerName()
        {
            var id = await CreateAsync("Limon", IngredientUnit.Piece);
            await AdjustAsync(id, StockAdjustmentType.StockIn, 10);
            await AdjustAsync(id, StockAdjustmentType.Waste, 2);
            await AdjustAsync(id, StockAdjustmentType.CountCorrection, 7);

            await using var db = CreateDbContext();
            var manager = CreateIngredientManager(db);
            var page1 = await manager.GetStockMovementsAsync(id, new PaginationQueryDto { Page = 1, PageSize = 2 });
            var page2 = await manager.GetStockMovementsAsync(id, new PaginationQueryDto { Page = 2, PageSize = 2 });

            Assert.Equal(3, page1.TotalCount);
            Assert.Equal(new[] { StockMovementType.CountCorrection, StockMovementType.Waste }, page1.Items.Select(m => m.Type));
            Assert.Equal(StockMovementType.StockIn, Assert.Single(page2.Items).Type);
            Assert.Equal("Sayım Düzeltmesi", page1.Items[0].TypeName);
            Assert.Equal("User A", page1.Items[0].PerformedBy);
        }

        [Fact]
        public async Task GetAll_IsSortedByName_WithUnitName()
        {
            await CreateAsync("Zencefil", IngredientUnit.Gram);
            await CreateAsync("Ayran", IngredientUnit.Liter);
            await CreateAsync("Çilek", IngredientUnit.Kilogram);

            await using var db = CreateDbContext();
            var list = (await CreateIngredientManager(db).GetAllAsync()).ToList();

            Assert.Equal(new[] { "Ayran", "Çilek", "Zencefil" }, list.Select(i => i.Name)); // Türkçe alfabe sırası: Ç, C'den sonra gelir.
            Assert.Equal("L", list[0].UnitName);
        }

        // ---------- validator'lar ----------

        [Theory]
        [InlineData(StockAdjustmentType.StockIn, 0, false)]
        [InlineData(StockAdjustmentType.Waste, -1, false)]
        [InlineData(StockAdjustmentType.StockIn, 0.001, true)]
        [InlineData(StockAdjustmentType.StockIn, 1.0001, false)] // 4 ondalık basamak
        [InlineData(StockAdjustmentType.CountCorrection, 0, true)]
        [InlineData(StockAdjustmentType.CountCorrection, -1, false)]
        [InlineData((StockAdjustmentType)99, 1, false)]
        public void StockAdjustmentValidator(StockAdjustmentType type, decimal quantity, bool isValid)
        {
            var result = new StockAdjustmentValidator().Validate(new StockAdjustmentDto { Type = type, Quantity = quantity });
            Assert.Equal(isValid, result.IsValid);
        }

        [Fact]
        public void IngredientValidators_RejectInvalidUnitAndLongName()
        {
            Assert.False(new IngredientCreateValidator().Validate(new IngredientCreateDto { Name = "Tuz", Unit = (IngredientUnit)0 }).IsValid);
            Assert.False(new IngredientCreateValidator().Validate(new IngredientCreateDto { Name = new string('a', 101), Unit = IngredientUnit.Gram }).IsValid);
            Assert.False(new IngredientUpdateValidator().Validate(new IngredientUpdateDto { IngredientId = Guid.Empty, Name = "Tuz", Unit = IngredientUnit.Gram }).IsValid);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Yummy.Core.DTOs.IngredientDTOs;
using Yummy.Core.DTOs.IngredientRequestDTOs;
using Yummy.Core.Exceptions;
using Yummy.Data.Context;
using Yummy.Entity;
using Yummy.Entity.Enums;
using Yummy.Tests.Infrastructure;

namespace Yummy.Tests.Ingredients
{
    // stok kilidinin (stok hareketleri ve talep tedariki) gerçek SQL Server üzerinde eşzamanlı isteklerle testi (bkz. SqlServerTestBase).
    public class StockConcurrencyTests : SqlServerTestBase
    {
        private static readonly Guid IngredientId = Guid.NewGuid();

        protected override Task SeedAsync(YummyDbContext db)
        {
            db.Ingredients.Add(new Ingredient { IngredientId = IngredientId, Name = "Domates", Unit = IngredientUnit.Kilogram, StockQuantity = 5 });
            db.Chefs.Add(new Chef { Name = "Ayşe", Surname = "Yılmaz", Title = "Baş Şef", Description = "", ImageUrl = "", AppUserId = UserA });
            return Task.CompletedTask;
        }

        [SkippableFact]
        public async Task ConcurrentSupplyOfSameRequest_AddsStockOnlyOnce()
        {
            Skip.IfNot(IsAvailable, SkipReason);

            Guid requestId;
            await using (var db = CreateDbContext())
                requestId = await CreateIngredientRequestManager(db).CreateAsync(UserA.ToString(), new IngredientRequestCreateDto
                {
                    Items = new() { new() { IngredientId = IngredientId, Quantity = 2 } }
                });

            Guid itemId;
            await using (var db = CreateDbContext())
                itemId = (await db.IngredientRequestItems.SingleAsync()).IngredientRequestItemId;

            // 5 çalışan aynı talebi aynı anda tedarik etmeye çalışır.
            var results = await RunConcurrentlyAsync(Enumerable.Range(0, 5).Select(_ => (Func<Task>)(async () =>
            {
                await using var db = CreateDbContext();
                await CreateIngredientRequestManager(db).SupplyAsync(UserB.ToString(), requestId, new IngredientRequestSupplyDto
                {
                    Items = new() { new() { IngredientRequestItemId = itemId, SuppliedQuantity = 2 } }
                });
            })));

            Assert.Equal(1, results.Count(r => r == null));
            Assert.All(results.Where(r => r != null), ex => Assert.Equal("NotPending", Assert.IsType<LogicException>(ex).PropertyName));

            await using var check = CreateDbContext();
            Assert.Equal(7, (await check.Ingredients.SingleAsync()).StockQuantity);
            Assert.Equal(1, await check.StockMovements.CountAsync());
            Assert.Equal(IngredientRequestStatus.Supplied, (await check.IngredientRequests.SingleAsync()).Status);
        }

        private Func<Task> Adjust(StockAdjustmentType type, decimal quantity) => async () =>
        {
            await using var db = CreateDbContext();
            await CreateIngredientManager(db).AdjustStockAsync(UserA.ToString(), IngredientId, new StockAdjustmentDto { Type = type, Quantity = quantity });
        };

        [SkippableFact]
        public async Task ConcurrentWaste_NeverDropsStockBelowZero()
        {
            Skip.IfNot(IsAvailable, SkipReason);

            // stokta 5 kg var; 10 çalışan aynı anda 1'er kg fire düşmeye çalışır.
            var results = await RunConcurrentlyAsync(Enumerable.Range(0, 10).Select(_ => Adjust(StockAdjustmentType.Waste, 1)));

            Assert.Equal(5, results.Count(r => r == null));
            Assert.All(results.Where(r => r != null), ex => Assert.Equal("InsufficientStock", Assert.IsType<LogicException>(ex).PropertyName));

            await using var check = CreateDbContext();
            Assert.Equal(0, (await check.Ingredients.SingleAsync()).StockQuantity);
            Assert.Equal(5, await check.StockMovements.CountAsync());
        }

        [SkippableFact]
        public async Task ConcurrentStockIns_AreAllApplied_AndMovementsChainCorrectly()
        {
            Skip.IfNot(IsAvailable, SkipReason);

            var results = await RunConcurrentlyAsync(Enumerable.Range(0, 10).Select(_ => Adjust(StockAdjustmentType.StockIn, 0.5m)));
            Assert.All(results, Assert.Null);

            await using var check = CreateDbContext();
            Assert.Equal(10, (await check.Ingredients.SingleAsync()).StockQuantity);

            // her hareketin "sonraki stok" değeri farklıdır ve 5,5'ten 10'a yarımşar artar; hiçbir güncelleme diğerini ezmemiştir.
            var afterValues = await check.StockMovements.Select(m => m.QuantityAfter).OrderBy(q => q).ToListAsync();
            Assert.Equal(Enumerable.Range(1, 10).Select(i => 5 + i * 0.5m), afterValues);
        }
    }
}

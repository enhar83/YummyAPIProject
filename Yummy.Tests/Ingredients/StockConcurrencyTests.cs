using Microsoft.EntityFrameworkCore;
using Yummy.Core.DTOs.IngredientDTOs;
using Yummy.Core.Exceptions;
using Yummy.Data.Context;
using Yummy.Entity;
using Yummy.Entity.Enums;
using Yummy.Tests.Infrastructure;

namespace Yummy.Tests.Ingredients
{
    // stok kilidinin gerçek SQL Server üzerinde eşzamanlı isteklerle testi (bkz. SqlServerTestBase).
    public class StockConcurrencyTests : SqlServerTestBase
    {
        private static readonly Guid IngredientId = Guid.NewGuid();

        protected override Task SeedAsync(YummyDbContext db)
        {
            db.Ingredients.Add(new Ingredient { IngredientId = IngredientId, Name = "Domates", Unit = IngredientUnit.Kilogram, StockQuantity = 5 });
            return Task.CompletedTask;
        }

        private Func<Task> Adjust(StockMovementType type, decimal quantity) => async () =>
        {
            await using var db = CreateDbContext();
            await CreateIngredientManager(db).AdjustStockAsync(UserA.ToString(), IngredientId, new StockAdjustmentDto { Type = type, Quantity = quantity });
        };

        [SkippableFact]
        public async Task ConcurrentWaste_NeverDropsStockBelowZero()
        {
            Skip.IfNot(IsAvailable, SkipReason);

            // stokta 5 kg var; 10 çalışan aynı anda 1'er kg fire düşmeye çalışır.
            var results = await RunConcurrentlyAsync(Enumerable.Range(0, 10).Select(_ => Adjust(StockMovementType.Waste, 1)));

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

            var results = await RunConcurrentlyAsync(Enumerable.Range(0, 10).Select(_ => Adjust(StockMovementType.StockIn, 0.5m)));
            Assert.All(results, Assert.Null);

            await using var check = CreateDbContext();
            Assert.Equal(10, (await check.Ingredients.SingleAsync()).StockQuantity);

            // her hareketin "sonraki stok" değeri farklıdır ve 5,5'ten 10'a yarımşar artar; hiçbir güncelleme diğerini ezmemiştir.
            var afterValues = await check.StockMovements.Select(m => m.QuantityAfter).OrderBy(q => q).ToListAsync();
            Assert.Equal(Enumerable.Range(1, 10).Select(i => 5 + i * 0.5m), afterValues);
        }
    }
}

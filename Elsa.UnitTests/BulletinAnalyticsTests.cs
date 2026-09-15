using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Jobs.BulletinGeneration;
using Xunit;

namespace Elsa.UnitTests
{
    public class BulletinAnalyticsTests
    {
        [Fact]
        public void ProductAllocationPreservesDiscountedRevenueAndQuantity()
        {
            var period = new BulletinPeriod(new DateTime(2026, 9, 15));
            var order = Order(1, new DateTime(2026, 8, 1), "uid:a", 1000m);
            var items = new[]
            {
                new BulletinItem { Id = 1, PurchaseOrderId = 1, ProductId = 1, Name = "A", TaxedPrice = 900, Quantity = 3 },
                new BulletinItem { Id = 2, PurchaseOrderId = 1, ProductId = 2, Name = "B", TaxedPrice = 300, Quantity = 2 }
            };
            var result = ProductPerformanceChapter.Calculate(new[] { order }, items, period);
            Assert.Equal(1000m, result.Sum(r => r.CurrentRevenue));
            Assert.Equal(750m, result.Single(r => r.Name == "A").CurrentRevenue);
            Assert.Equal(3m, result.Single(r => r.Name == "A").CurrentQuantity);
            Assert.Equal(250m, result.Single(r => r.Name == "B").CurrentRevenue);
        }

        [Fact]
        public void AllocationRetainsUnallocatableOrdersAndDecimalRemainder()
        {
            var period = new BulletinPeriod(new DateTime(2026, 9, 15));
            var orders = new[] { Order(1, period.Start, "a", 0.01m), Order(2, period.Start, "b", 99m) };
            var items = Enumerable.Range(1, 3).Select(i => new BulletinItem
            { Id = i, PurchaseOrderId = 1, ProductId = i, Name = i.ToString(), TaxedPrice = 1, Quantity = 1 }).ToArray();
            var result = ProductPerformanceChapter.Calculate(orders, items, period);
            Assert.Equal(99.01m, result.Sum(r => r.CurrentRevenue));
            Assert.Equal(99m, result.Single(r => r.Name.StartsWith("Nepřiřazené")).CurrentRevenue);
        }

        [Fact]
        public void ComparisonUsesCalendarMonthsAndExclusiveEnds()
        {
            var period = new BulletinPeriod(new DateTime(2025, 2, 17));
            Assert.Equal(new DateTime(2024, 11, 1), period.Start);
            Assert.Equal(new DateTime(2023, 11, 1), period.PreviousStart);
            Assert.True(period.Contains(new DateTime(2025, 1, 31, 23, 59, 59)));
            Assert.False(period.Contains(period.End));
            Assert.False(period.ContainsPrevious(period.PreviousEnd));
        }

        [Fact]
        public void LifecycleSeparatesNewReturningLostAndUnknownWithoutDoubleCounting()
        {
            var start = new DateTime(2026, 6, 1);
            var orders = new[]
            {
                Order(1, start, "new", 100), Order(2, start.AddDays(1), "new", 50),
                Order(3, start, "returning", 200), Order(4, start.AddDays(-1), "returning", 20),
                Order(5, start.AddDays(-10), "lost", 300), Order(6, start, null, 40),
                Order(7, start.AddMonths(3), "outside", 10000)
            };
            var first = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase)
            { ["new"] = start, ["returning"] = start.AddYears(-3) };
            var result = CustomerLifecycleChapter.Calculate(orders, first, start, start.AddMonths(3));
            Assert.Equal(1, result.NewCount);
            Assert.Equal(150m, result.NewRevenue);
            Assert.Equal(1, result.ReturningCount);
            Assert.Equal(200m, result.ReturningRevenue);
            Assert.Equal(1, result.LostCount);
            Assert.Equal(300m, result.LostPreviousRevenue);
            Assert.Equal(1, result.UnidentifiedOrders);
            Assert.Equal(390m, result.TotalRevenue);
            Assert.Equal(result.TotalRevenue, result.NewRevenue + result.ReturningRevenue + result.UnidentifiedRevenue);
        }

        [Fact]
        public void DormancyRequiresDistinctDaysAndRespectsExpectedInterval()
        {
            var first = new DateTime(2026, 6, 1);
            var regular = Enumerable.Range(0, 4).Select(i => Order(i, first.AddDays(i * 10), "regular", 100)).ToList();
            regular.ForEach(o => { o.CustomerErpUid = "regular"; o.IsDistributor = 1; });
            var duplicateDays = Enumerable.Range(0, 8).Select(i => Order(10 + i, first.AddDays(i % 3 * 10), "few", 50)).ToList();
            duplicateDays.ForEach(o => { o.CustomerErpUid = "few"; o.IsDistributor = 1; });
            Assert.Empty(DormantWholesaleChapter.Calculate(regular, new DateTime(2026, 7, 10)));
            var result = DormantWholesaleChapter.Calculate(regular.Concat(duplicateDays), new DateTime(2026, 9, 1));
            Assert.Single(result);
            Assert.Equal("regular", result[0].ErpUid);
            Assert.Equal(10m, result[0].MedianDays);
            Assert.Equal(4, result[0].OrderDays);
            Assert.Equal(400m, result[0].RevenueLastYear);
        }

        private static BulletinOrder Order(long id, DateTime date, string key, decimal revenue)
            => new BulletinOrder { Id = id, PurchaseDate = date, CustomerKey = key, NetRevenue = revenue, CustomerName = key };
    }
}

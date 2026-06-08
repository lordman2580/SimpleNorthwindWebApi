using NSubstitute;
using Shouldly;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.ApiLogs;
using SimpleNorthwind.Application.Common;
using SimpleNorthwind.Application.Customers;
using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.Application.Products;
using SimpleNorthwind.Application.Services;

namespace SimpleNorthwind.Application.UnitTests;

/// <summary>
/// 總覽（Dashboard）彙總規則：首頁「最新訂單」清單不顯示已取消訂單（仍以未取消者補滿 RecentTake=6）。
/// 取消訂單仍計入總訂單數（OrderCount），只是不出現在最新清單。
/// </summary>
public sealed class DashboardServiceTests
{
    private readonly IOrderService _orders = Substitute.For<IOrderService>();
    private readonly ICustomerService _customers = Substitute.For<ICustomerService>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IApiLogRepository _apiLogs = Substitute.For<IApiLogRepository>();

    private DashboardService CreateSut()
    {
        _customers.ListAsync(Arg.Any<CancellationToken>())
                  .Returns(Array.Empty<CustomerDto>());
        _products.ListLowStockAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                 .Returns(Array.Empty<ProductDto>());
        _apiLogs.QueryAsync(Arg.Any<ApiLogQuery>(), Arg.Any<CancellationToken>())
                .Returns(new PagedResult<ApiLogDto>(Array.Empty<ApiLogDto>(), 1, 1, 0));
        return new DashboardService(_orders, _customers, _products, _apiLogs);
    }

    private static OrderDto Order(int id, bool canceled = false, bool paidoff = false) =>
        new(id, 1, "Cust", 1, "Emp", DateTime.UtcNow, null, null, null, canceled, paidoff,
            canceled ? "Canceled" : paidoff ? "PaidOff" : "Normal", []);

    // 帶單筆明細（單價=amount、數量1、折扣0 → 折扣後合計=amount），供營收計算驗證。
    private static OrderDto OrderWith(int id, decimal amount, bool canceled = false, bool paidoff = false) =>
        new(id, 1, "Cust", 1, "Emp", DateTime.UtcNow, null, null, null, canceled, paidoff,
            canceled ? "Canceled" : paidoff ? "PaidOff" : "Normal",
            [new OrderDetailDto(1, "P", amount, 1, 0m, 1)]);

    [Fact]
    public async Task GetSummaryAsync_SplitsRevenue_SettledVsExpected_ExcludingCanceled()
    {
        IReadOnlyList<OrderDto> all =
        [
            OrderWith(1, 100m, paidoff: true),   // 已結清 → 實際 +100
            OrderWith(2, 200m),                  // 未結清 → 預計 +200
            OrderWith(3, 999m, canceled: true),  // 已取消 → 兩者皆不計
            OrderWith(4, 50m, paidoff: true),    // 已結清 → 實際 +50
            OrderWith(5, 25m),                   // 未結清 → 預計 +25
        ];
        _orders.ListAsync(Arg.Any<CancellationToken>()).Returns(all);

        var summary = await CreateSut().GetSummaryAsync(lowStockThreshold: 10, default);

        summary.SettledRevenue.ShouldBe(150m);   // 100 + 50（已結清）
        summary.ExpectedRevenue.ShouldBe(225m);  // 200 + 25（未結清），取消的 999 不計
    }

    [Fact]
    public async Task GetSummaryAsync_RecentOrders_ExcludesCanceled()
    {
        IReadOnlyList<OrderDto> all =
        [
            Order(1, canceled: false),
            Order(2, canceled: true),
            Order(3, paidoff: true),
            Order(4, canceled: true),
        ];
        _orders.ListAsync(Arg.Any<CancellationToken>()).Returns(all);

        var summary = await CreateSut().GetSummaryAsync(lowStockThreshold: 10, default);

        summary.RecentOrders.ShouldNotContain(o => o.IsCanceled);
        summary.RecentOrders.Select(o => o.OrderId).ShouldBe([3, 1]); // 依 OrderId desc，已取消(2,4)剔除
        summary.OrderCount.ShouldBe(4); // 取消仍計入總數
    }

    [Fact]
    public async Task GetSummaryAsync_RecentOrders_FillsUpToSix_SkippingCanceled()
    {
        // 10 筆未取消（id 1..10）+ 取消者穿插其間；應回最新 6 筆「未取消」(10..5)
        var all = new List<OrderDto>();
        for (var id = 1; id <= 10; id++)
        {
            all.Add(Order(id, canceled: false));
            all.Add(Order(id + 100, canceled: true)); // 較大 id 的取消單，若未剔除會排在最前
        }
        _orders.ListAsync(Arg.Any<CancellationToken>()).Returns(all);

        var summary = await CreateSut().GetSummaryAsync(lowStockThreshold: 10, default);

        summary.RecentOrders.Count.ShouldBe(6);
        summary.RecentOrders.ShouldNotContain(o => o.IsCanceled);
        summary.RecentOrders.Select(o => o.OrderId).ShouldBe([10, 9, 8, 7, 6, 5]);
    }
}

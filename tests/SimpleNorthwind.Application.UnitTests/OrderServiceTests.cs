using NSubstitute;
using Shouldly;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.Application.Services;
using SimpleNorthwind.Domain.Common;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.UnitTests;

public sealed class OrderServiceTests
{
    // ── shared fixtures ──────────────────────────────────────────────────────
    private readonly IUnitOfWork _unitOfWork        = Substitute.For<IUnitOfWork>();
    private readonly IOrderRepository _orders        = Substitute.For<IOrderRepository>();
    private readonly IOrderDetailRepository _details = Substitute.For<IOrderDetailRepository>();
    private readonly IProductRepository _products    = Substitute.For<IProductRepository>();

    private OrderService CreateSut() =>
        new(_unitOfWork, _orders, _details, _products);

    // ── helpers ──────────────────────────────────────────────────────────────
    private static Order MakeOrder(int id = 1, bool isPaidoff = false, bool isCanceled = false) =>
        new()
        {
            OrderId    = id,
            CustomerId = 10,
            EmployeeId = 1,
            OrderDate  = DateTime.UtcNow,
            IsPaidoff  = isPaidoff,
            IsCanceled = isCanceled
        };

    private static OrderDetail MakeDetail(int orderId, int productId, int qty = 5, int version = 1) =>
        new()
        {
            OrderId        = orderId,
            ProductId      = productId,
            OrderQuantities = qty,
            Discount       = 0m,
            Version        = version
        };

    // ═════════════════════════════════════════════════════════════════════════
    // CreateAsync
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_SufficientStock_DecrementsStockInsertsAndCommits()
    {
        // Arrange
        const int orderId   = 42;
        const int productId1 = 101;
        const int productId2 = 102;

        var request = new CreateOrderRequest(
            CustomerId: 10,
            Details:
            [
                new CreateOrderDetailRequest(productId1, OrderQuantities: 3, Discount: 0m),
                new CreateOrderDetailRequest(productId2, OrderQuantities: 2, Discount: 0.1m)
            ]);

        // Stock checks pass
        _products.TryDecreaseStockAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                 .Returns(true);

        // Insert order returns new id
        _orders.InsertAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
               .Returns(orderId);

        // BuildDtoAsync: GetByIdAsync + ListByOrderAsync after commit
        var savedOrder = MakeOrder(orderId);
        _orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>())
               .Returns(savedOrder);

        IReadOnlyList<OrderDetail> savedDetails =
        [
            MakeDetail(orderId, productId1, 3),
            MakeDetail(orderId, productId2, 2)
        ];
        _details.ListByOrderAsync(orderId, Arg.Any<CancellationToken>())
                .Returns(savedDetails);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(request, actingEmployeeId: 1, default);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        await _products.Received(1)
            .TryDecreaseStockAsync(productId1, 3, Arg.Any<CancellationToken>());
        await _products.Received(1)
            .TryDecreaseStockAsync(productId2, 2, Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_InsufficientStock_FailsAndRolledBack()
    {
        // Arrange
        const int productId1 = 201;
        const int productId2 = 202;

        var request = new CreateOrderRequest(
            CustomerId: 10,
            Details:
            [
                new CreateOrderDetailRequest(productId1, OrderQuantities: 10, Discount: 0m),
                new CreateOrderDetailRequest(productId2, OrderQuantities:  5, Discount: 0m)
            ]);

        // First detail → stock insufficient; second should never be reached
        _products.TryDecreaseStockAsync(productId1, 10, Arg.Any<CancellationToken>())
                 .Returns(false);
        _products.TryDecreaseStockAsync(productId2, Arg.Any<int>(), Arg.Any<CancellationToken>())
                 .Returns(true);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(request, actingEmployeeId: 1, default);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);

        await _unitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CancelAsync
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CancelAsync_PaidOffOrder_ReturnsConflict()
    {
        // Arrange
        const int orderId = 1;
        _orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>())
               .Returns(MakeOrder(orderId, isPaidoff: true));

        var sut = CreateSut();

        // Act
        var result = await sut.CancelAsync(orderId, actingEmployeeId: 1, default);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);

        await _unitOfWork.DidNotReceive().BeginAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        const int orderId = 99;
        _orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>())
               .Returns((Order?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.CancelAsync(orderId, actingEmployeeId: 1, default);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task CancelAsync_OpenOrder_RestoresStockAndCommits()
    {
        // Arrange
        const int orderId   = 5;
        const int productId1 = 301;
        const int productId2 = 302;

        _orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>())
               .Returns(MakeOrder(orderId, isPaidoff: false, isCanceled: false));

        IReadOnlyList<OrderDetail> details =
        [
            MakeDetail(orderId, productId1, qty: 4),
            MakeDetail(orderId, productId2, qty: 7)
        ];
        _details.ListByOrderAsync(orderId, Arg.Any<CancellationToken>())
                .Returns(details);

        var sut = CreateSut();

        // Act
        var result = await sut.CancelAsync(orderId, actingEmployeeId: 1, default);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        await _products.Received(1).RestoreStockAsync(productId1, 4, Arg.Any<CancellationToken>());
        await _products.Received(1).RestoreStockAsync(productId2, 7, Arg.Any<CancellationToken>());
        await _orders.Received(1).SetCanceledAsync(orderId, 1, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ═════════════════════════════════════════════════════════════════════════
    // UpdateAsync
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_NoChanges_ReturnsValidationNotModifiedAndSkipsWrite()
    {
        // Arrange：請求明細與現況完全相同（同產品、同數量、同折扣）
        const int orderId  = 10;
        const int productId = 401;

        _orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>())
               .Returns(MakeOrder(orderId));

        IReadOnlyList<OrderDetail> existing = [MakeDetail(orderId, productId, qty: 5, version: 1)];
        _details.ListByOrderAsync(orderId, Arg.Any<CancellationToken>())
                .Returns(existing);

        // 資料與現況相同（version 帶正確值，但 no-op 比較只看資料）
        var request = new UpdateOrderRequest(
            Details: [new UpdateOrderDetailRequest(productId, OrderQuantities: 5, Discount: 0m, Version: 1)]);

        // Act
        var result = await CreateSut().UpdateAsync(orderId, request, actingEmployeeId: 1, default);

        // Assert：回 Validation（→ 400），且完全不碰 DB
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("order.not_modified");

        await _unitOfWork.DidNotReceive().BeginAsync(Arg.Any<CancellationToken>());
        await _details.DidNotReceive().UpdateWithVersionAsync(Arg.Any<OrderDetail>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ChangedQuantity_UpdatesDetailAndCommits()
    {
        // Arrange：數量 5 → 8（delta +3），version 1 相符
        const int orderId  = 10;
        const int productId = 401;

        _orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>())
               .Returns(MakeOrder(orderId));
        IReadOnlyList<OrderDetail> existing = [MakeDetail(orderId, productId, qty: 5, version: 1)];
        _details.ListByOrderAsync(orderId, Arg.Any<CancellationToken>())
                .Returns(existing);
        _products.TryDecreaseStockAsync(productId, 3, Arg.Any<CancellationToken>())
                 .Returns(true);
        _details.UpdateWithVersionAsync(Arg.Any<OrderDetail>(), Arg.Any<CancellationToken>())
                .Returns(true);

        var request = new UpdateOrderRequest(
            Details: [new UpdateOrderDetailRequest(productId, OrderQuantities: 8, Discount: 0m, Version: 1)]);

        // Act
        var result = await CreateSut().UpdateAsync(orderId, request, actingEmployeeId: 1, default);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _products.Received(1).TryDecreaseStockAsync(productId, 3, Arg.Any<CancellationToken>());
        await _details.Received(1).UpdateWithVersionAsync(Arg.Any<OrderDetail>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ChangedDataWithStaleVersion_ReturnsConflict()
    {
        // Arrange：資料有變（5→8）但 version 不符 → 樂觀並行衝突
        const int orderId  = 10;
        const int productId = 401;

        _orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>())
               .Returns(MakeOrder(orderId));
        IReadOnlyList<OrderDetail> existing = [MakeDetail(orderId, productId, qty: 5, version: 2)];
        _details.ListByOrderAsync(orderId, Arg.Any<CancellationToken>())
                .Returns(existing);
        _products.TryDecreaseStockAsync(productId, 3, Arg.Any<CancellationToken>())
                 .Returns(true);
        _details.UpdateWithVersionAsync(Arg.Any<OrderDetail>(), Arg.Any<CancellationToken>())
                .Returns(false); // 版本不符 → 0 列

        var request = new UpdateOrderRequest(
            Details: [new UpdateOrderDetailRequest(productId, OrderQuantities: 8, Discount: 0m, Version: 1)]);

        // Act
        var result = await CreateSut().UpdateAsync(orderId, request, actingEmployeeId: 1, default);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("order.version_conflict");
        await _unitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        const int orderId = 77;
        _orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>())
               .Returns((Order?)null);

        var request = new UpdateOrderRequest(
            Details: [new UpdateOrderDetailRequest(ProductId: 501, OrderQuantities: 1, Discount: 0m, Version: 1)]);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateAsync(orderId, request, actingEmployeeId: 1, default);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}

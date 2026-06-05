using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.Customers;
using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.Application.Products;
using SimpleNorthwind.WebApi.Web.Http;
using SimpleNorthwind.WebApi.Web.Models;

namespace SimpleNorthwind.WebApi.Web.Controllers;

/// <summary>
/// 訂單後台 UI（F2）：清單／明細／建立／編輯／取消。
/// 清單為 API 全量回傳，篩選／排序／分頁在 controller 記憶體內完成。
/// 所有 loopback 呼叫先以 <see cref="UiControllerBase.RedirectToLoginIfUnauthorizedAsync"/> 處理 401。
/// </summary>
[Route("orders")]
public sealed class OrdersUiController(NorthwindApiClient apiClient) : UiControllerBase
{
    /// <summary>建立訂單明細小計：UnitPrice × Qty × (1 - Discount/100)。</summary>
    private static decimal LineTotal(decimal unitPrice, int quantity, decimal discount)
        => unitPrice * quantity * (1m - (discount / 100m));

    /// <summary>狀態原字串對映頁籤鍵（all 一律通過）。</summary>
    private static bool MatchesTab(string status, string tab) => tab switch
    {
        "normal" => string.Equals(status, "Normal", StringComparison.OrdinalIgnoreCase),
        "paidoff" => string.Equals(status, "PaidOff", StringComparison.OrdinalIgnoreCase),
        "canceled" => string.Equals(status, "Canceled", StringComparison.OrdinalIgnoreCase),
        _ => true,
    };

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? statusTab,
        string? employeeName,
        DateTime? fromDate,
        DateTime? toDate,
        string? sortBy,
        bool desc = true,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var tab = string.IsNullOrWhiteSpace(statusTab) ? "all" : statusTab.ToLowerInvariant();
        var sort = string.IsNullOrWhiteSpace(sortBy) ? "date" : sortBy.ToLowerInvariant();
        if (page < 1) page = 1;
        if (!PagedViewModel<OrderListItemViewModel>.PageSizeOptions.Contains(pageSize)) pageSize = 10;

        var result = await apiClient.ListOrdersAsync(ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Detail ?? "載入訂單失敗。";
            return View("~/Web/Views/Orders/Index.cshtml", new OrderIndexViewModel
            {
                StatusTab = tab,
                EmployeeName = employeeName,
                FromDate = fromDate,
                ToDate = toDate,
                SortBy = sort,
                Desc = desc,
            });
        }

        var all = result.Value!
            .Select(o => new OrderListItemViewModel
            {
                OrderId = o.OrderId,
                CustomerName = o.CustomerName,
                EmployeeName = o.EmployeeName,
                OrderDate = o.OrderDate,
                Status = o.Status,
                ItemCount = o.Details.Count,
                Total = o.Details.Sum(d => LineTotal(d.UnitPrice, d.OrderQuantities, d.Discount)),
            })
            .ToList();

        var employeeNames = all
            .Select(o => o.EmployeeName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.CurrentCulture)
            .ToList();

        IEnumerable<OrderListItemViewModel> filtered = all.Where(o => MatchesTab(o.Status, tab));
        if (!string.IsNullOrWhiteSpace(employeeName))
            filtered = filtered.Where(o => o.EmployeeName.Contains(employeeName, StringComparison.CurrentCultureIgnoreCase));
        if (fromDate is { } from)
            filtered = filtered.Where(o => o.OrderDate >= from.Date);
        if (toDate is { } to)
            filtered = filtered.Where(o => o.OrderDate < to.Date.AddDays(1));

        filtered = sort switch
        {
            "customer" => desc
                ? filtered.OrderByDescending(o => o.CustomerName, StringComparer.CurrentCulture)
                : filtered.OrderBy(o => o.CustomerName, StringComparer.CurrentCulture),
            "total" => desc
                ? filtered.OrderByDescending(o => o.Total)
                : filtered.OrderBy(o => o.Total),
            _ => desc
                ? filtered.OrderByDescending(o => o.OrderDate)
                : filtered.OrderBy(o => o.OrderDate),
        };

        var ordered = filtered.ToList();
        var total = ordered.Count;
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new OrderIndexViewModel
        {
            Page = PagedViewModel<OrderListItemViewModel>.From(items, page, pageSize, total),
            StatusTab = tab,
            EmployeeName = employeeName,
            FromDate = fromDate,
            ToDate = toDate,
            SortBy = sort,
            Desc = desc,
            EmployeeNames = employeeNames,
        };
        return View("~/Web/Views/Orders/Index.cshtml", vm);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var result = await apiClient.GetOrderAsync(id, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.StatusCode == System.Net.HttpStatusCode.NotFound
                ? $"找不到訂單 #{id}。"
                : result.Detail ?? "載入訂單失敗。";
            return RedirectToAction(nameof(Index));
        }

        var order = result.Value!;

        // best-effort 取客戶聯絡資訊；失敗（含 401 以外）一律略過、留 null。
        string? contactName = null;
        string? contactNumber = null;
        var customerResult = await apiClient.GetCustomerAsync(order.CustomerId, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(customerResult.StatusCode) is { } custRedirect) return custRedirect;
        if (customerResult.IsSuccess)
        {
            contactName = customerResult.Value!.ContactName;
            contactNumber = customerResult.Value!.ContactNumber;
        }

        var lines = order.Details
            .Select(d => new OrderLineViewModel
            {
                ProductId = d.ProductId,
                ProductName = d.ProductName,
                UnitPrice = d.UnitPrice,
                Quantity = d.OrderQuantities,
                Discount = d.Discount,
                Version = d.Version,
                LineTotal = LineTotal(d.UnitPrice, d.OrderQuantities, d.Discount),
            })
            .ToList();

        var vm = new OrderDetailViewModel
        {
            OrderId = order.OrderId,
            CustomerName = order.CustomerName,
            CustomerId = order.CustomerId,
            CustomerContactName = contactName,
            CustomerContactNumber = contactNumber,
            EmployeeName = order.EmployeeName,
            OrderDate = order.OrderDate,
            Status = order.Status,
            IsCanceled = order.IsCanceled,
            IsPaidoff = order.IsPaidoff,
            Lines = lines,
            GrandTotal = lines.Sum(l => l.LineTotal),
        };
        return View("~/Web/Views/Orders/Details.cshtml", vm);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var form = await BuildCreateFormAsync(new CreateOrderInputViewModel(), ct);
        return form is IActionResult redirect ? redirect : View("~/Web/Views/Orders/Create.cshtml", (CreateOrderFormViewModel)form!);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderInputViewModel input, CancellationToken ct)
    {
        input.Lines ??= [];
        var lines = input.Lines.Where(l => l.ProductId > 0).ToList();
        if (input.CustomerId <= 0 || lines.Count == 0)
        {
            TempData["Error"] = "請選擇客戶並至少新增一筆有效明細。";
            var reload = await BuildCreateFormAsync(input, ct);
            return reload is IActionResult redirect ? redirect : View("~/Web/Views/Orders/Create.cshtml", (CreateOrderFormViewModel)reload!);
        }

        var request = new CreateOrderRequest(
            input.CustomerId,
            lines.Select(l => new CreateOrderDetailRequest(l.ProductId, l.OrderQuantities, l.Discount)).ToList());

        var result = await apiClient.CreateOrderAsync(request, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } loginRedirect) return loginRedirect;
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.StatusCode == System.Net.HttpStatusCode.BadRequest
                ? result.Detail ?? "輸入資料有誤或庫存不足。"
                : result.Detail ?? "建立訂單失敗。";
            var reload = await BuildCreateFormAsync(input, ct);
            return reload is IActionResult redirect ? redirect : View("~/Web/Views/Orders/Create.cshtml", (CreateOrderFormViewModel)reload!);
        }

        return RedirectToAction(nameof(Details), new { id = result.Value!.OrderId });
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var result = await apiClient.GetOrderAsync(id, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.StatusCode == System.Net.HttpStatusCode.NotFound
                ? $"找不到訂單 #{id}。"
                : result.Detail ?? "載入訂單失敗。";
            return RedirectToAction(nameof(Index));
        }

        var order = result.Value!;
        if (order.IsCanceled)
        {
            TempData["Error"] = "已取消的訂單不可編輯。";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new EditOrderFormViewModel
        {
            OrderId = order.OrderId,
            CustomerName = order.CustomerName,
            Lines = order.Details
                .Select(d => new OrderLineViewModel
                {
                    ProductId = d.ProductId,
                    ProductName = d.ProductName,
                    UnitPrice = d.UnitPrice,
                    Quantity = d.OrderQuantities,
                    Discount = d.Discount,
                    Version = d.Version,
                    LineTotal = LineTotal(d.UnitPrice, d.OrderQuantities, d.Discount),
                })
                .ToList(),
        };
        return View("~/Web/Views/Orders/Edit.cshtml", vm);
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditOrderInputViewModel input, CancellationToken ct)
    {
        input.Lines ??= [];
        var lines = input.Lines.Where(l => l.ProductId > 0).ToList();
        if (lines.Count == 0)
        {
            TempData["Error"] = "訂單至少需保留一筆明細。";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var request = new UpdateOrderRequest(
            lines.Select(l => new UpdateOrderDetailRequest(l.ProductId, l.OrderQuantities, l.Discount, l.Version)).ToList());

        var result = await apiClient.UpdateOrderAsync(id, request, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;
        if (!result.IsSuccess)
        {
            if (result.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                TempData["Error"] = result.Detail ?? "資料已被他人變更，請重新載入。";
                return RedirectToAction(nameof(Edit), new { id });
            }

            TempData["Error"] = result.StatusCode == System.Net.HttpStatusCode.BadRequest
                ? result.Detail ?? "輸入資料有誤或庫存不足。"
                : result.Detail ?? "更新訂單失敗。";
            return RedirectToAction(nameof(Edit), new { id });
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await apiClient.CancelOrderAsync(id, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.StatusCode switch
            {
                System.Net.HttpStatusCode.Conflict => result.Detail ?? "已付清訂單不可取消。",
                System.Net.HttpStatusCode.NotFound => $"找不到訂單 #{id}。",
                _ => result.Detail ?? "取消訂單失敗。",
            };
        }
        else
        {
            TempData["Success"] = $"訂單 #{id} 已取消。";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>載入客戶／產品下拉資料組成建立表單；遇 401 回傳重導 <see cref="IActionResult"/>，否則回 <see cref="CreateOrderFormViewModel"/>。</summary>
    private async Task<object> BuildCreateFormAsync(CreateOrderInputViewModel input, CancellationToken ct)
    {
        var customersResult = await apiClient.ListCustomersAsync(ct);
        if (await RedirectToLoginIfUnauthorizedAsync(customersResult.StatusCode) is { } custRedirect) return custRedirect;

        var productsResult = await apiClient.ListProductsAsync(1, 100, null, "name", false, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(productsResult.StatusCode) is { } prodRedirect) return prodRedirect;

        var customers = customersResult.IsSuccess
            ? customersResult.Value!.Select(c => new OrderCustomerOption(c.CustomerId, c.CompanyName)).ToList()
            : [];
        var products = productsResult.IsSuccess
            ? productsResult.Value!.Items.Select(p => new OrderProductOption(p.ProductId, p.ProductName, p.UnitPrice)).ToList()
            : [];

        if (!customersResult.IsSuccess || !productsResult.IsSuccess)
            TempData["Error"] = (TempData["Error"] as string) ?? "下拉選項載入失敗，部分選項可能不完整。";

        return new CreateOrderFormViewModel
        {
            Input = input,
            Customers = customers,
            Products = products,
        };
    }
}

using System.Net;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.Customers;
using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.WebApi.Web.Http;
using SimpleNorthwind.WebApi.Web.Models;

namespace SimpleNorthwind.WebApi.Web.Controllers;

/// <summary>
/// 客戶（客戶資料 CRUD）UI controller。清單採全量讀取後記憶體排序 / 分頁；
/// 訂單數與客戶概況由 <c>ListOrdersAsync</c> 依 CustomerId 群組計得。Cookie scheme 由 <see cref="UiControllerBase"/> 提供。
/// </summary>
[Route("customers")]
public sealed class CustomersUiController(NorthwindApiClient apiClient) : UiControllerBase
{
    private const string IndexView = "~/Web/Views/Customers/Index.cshtml";
    private const string DetailsView = "~/Web/Views/Customers/Details.cshtml";
    private const string CreateView = "~/Web/Views/Customers/Create.cshtml";
    private const string EditView = "~/Web/Views/Customers/Edit.cshtml";
    private const string DeleteView = "~/Web/Views/Customers/Delete.cshtml";

    /// <summary>客戶清單（記憶體排序 + 分頁）。訂單數取得失敗時以 0 呈現，不阻斷清單。</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(string? sortBy, bool desc = false, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var sort = NormalizeSort(sortBy);
        if (page < 1) page = 1;
        if (Array.IndexOf(PagedViewModel<CustomerViewModel>.PageSizeOptions, pageSize) < 0) pageSize = 10;

        var result = await apiClient.ListCustomersAsync(ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Detail ?? "載入客戶資料失敗。";
            return View(IndexView, new CustomerIndexViewModel { SortBy = sort, Desc = desc });
        }

        // 訂單數：取訂單清單群組計數；取數失敗就視為全 0（不阻斷客戶清單）。
        var orderCounts = await TryGetOrderCountsAsync(ct);

        var rows = result.Value!
            .Select(c => new CustomerViewModel
            {
                CustomerId = c.CustomerId,
                CompanyName = c.CompanyName,
                ContactName = c.ContactName,
                ContactTitle = c.ContactTitle,
                ContactNumber = c.ContactNumber,
                Email = c.Email,
                OrderCount = orderCounts.TryGetValue(c.CustomerId, out var n) ? n : 0
            })
            .ToList();

        var sorted = SortRows(rows, sort, desc);
        var total = sorted.Count;
        var pageItems = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new CustomerIndexViewModel
        {
            Page = PagedViewModel<CustomerViewModel>.From(pageItems, page, pageSize, total),
            SortBy = sort,
            Desc = desc
        };
        return View(IndexView, vm);
    }

    /// <summary>客戶詳情：基本資料 + 該客戶訂單紀錄與概況統計。</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var result = await apiClient.GetCustomerAsync(id, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;
        if (!result.IsSuccess)
        {
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["Error"] = "找不到指定的客戶。";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = result.Detail ?? "載入客戶資料失敗。";
            return RedirectToAction(nameof(Index));
        }

        var customer = result.Value!;

        // 該客戶訂單：取全量後過濾 CustomerId；取數失敗就以空列表呈現（不阻斷詳情）。
        var ordersResult = await apiClient.ListOrdersAsync(ct);
        if (await RedirectToLoginIfUnauthorizedAsync(ordersResult.StatusCode) is { } redirect2) return redirect2;

        var orderRows = (ordersResult.IsSuccess ? ordersResult.Value! : [])
            .Where(o => o.CustomerId == id)
            .Select(o => new CustomerOrderRow
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                Status = o.Status,
                Total = SumOrderTotal(o)
            })
            .OrderByDescending(o => o.OrderDate)
            .ToList();

        var vm = new CustomerDetailViewModel
        {
            CustomerId = customer.CustomerId,
            CompanyName = customer.CompanyName,
            ContactName = customer.ContactName,
            ContactTitle = customer.ContactTitle,
            ContactNumber = customer.ContactNumber,
            Email = customer.Email,
            CreateDate = customer.CreateDate,
            CreateUser = customer.CreateUser,
            IsOutContacted = customer.IsOutContacted,
            OutContactedDate = customer.OutContactedDate,
            Orders = orderRows,
            TotalOrders = orderRows.Count,
            TotalSpent = orderRows.Sum(o => o.Total),
            LastOrderDate = orderRows.Count == 0 ? null : orderRows.Max(o => o.OrderDate)
        };
        return View(DetailsView, vm);
    }

    /// <summary>新增客戶表單。</summary>
    [HttpGet("create")]
    public IActionResult Create()
        => View(CreateView, new CustomerFormViewModel());

    /// <summary>新增客戶送出。</summary>
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(CreateView, form);

        var request = new CreateCustomerRequest(form.CompanyName, form.ContactName, form.ContactNumber, form.ContactTitle, form.Email);
        var result = await apiClient.CreateCustomerAsync(request, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Detail ?? "新增客戶失敗，請檢查輸入內容。");
            return View(CreateView, form);
        }

        TempData["Success"] = "客戶已新增。";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>編輯客戶表單。</summary>
    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var result = await apiClient.GetCustomerAsync(id, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.StatusCode == HttpStatusCode.NotFound ? "找不到指定的客戶。" : result.Detail ?? "載入客戶資料失敗。";
            return RedirectToAction(nameof(Index));
        }

        var c = result.Value!;
        var form = new CustomerFormViewModel
        {
            CustomerId = c.CustomerId,
            CompanyName = c.CompanyName,
            ContactName = c.ContactName,
            ContactTitle = c.ContactTitle,
            ContactNumber = c.ContactNumber,
            Email = c.Email,
            IsOutContacted = c.IsOutContacted,
            OutContactedDate = c.OutContactedDate
        };
        return View(EditView, form);
    }

    /// <summary>編輯客戶送出。</summary>
    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel form, CancellationToken ct)
    {
        form.CustomerId = id;
        if (!ModelState.IsValid)
            return View(EditView, form);

        var request = new UpdateCustomerRequest(
            form.CompanyName, form.ContactName, form.ContactNumber, form.ContactTitle, form.Email,
            form.IsOutContacted, form.OutContactedDate);
        var result = await apiClient.UpdateCustomerAsync(id, request, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;

        if (!result.IsSuccess)
        {
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["Error"] = "客戶已不存在。";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Detail ?? "更新客戶失敗，請檢查輸入內容。");
            return View(EditView, form);
        }

        TempData["Success"] = "客戶已更新。";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>刪除客戶確認頁。</summary>
    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await apiClient.GetCustomerAsync(id, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.StatusCode == HttpStatusCode.NotFound ? "找不到指定的客戶。" : result.Detail ?? "載入客戶資料失敗。";
            return RedirectToAction(nameof(Index));
        }

        var c = result.Value!;
        var vm = new CustomerFormViewModel
        {
            CustomerId = c.CustomerId,
            CompanyName = c.CompanyName,
            ContactName = c.ContactName,
            ContactTitle = c.ContactTitle,
            ContactNumber = c.ContactNumber,
            Email = c.Email,
            IsOutContacted = c.IsOutContacted,
            OutContactedDate = c.OutContactedDate
        };
        return View(DeleteView, vm);
    }

    /// <summary>刪除客戶送出。</summary>
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var result = await apiClient.DeleteCustomerAsync(id, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.StatusCode == HttpStatusCode.NotFound
                ? "客戶已不存在。"
                : result.Detail ?? "刪除客戶失敗。";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "客戶已刪除。";
        return RedirectToAction(nameof(Index));
    }

    // ---- 私有 helper ----

    /// <summary>取訂單清單並依 CustomerId 群組計數；任一失敗回空表（呼叫端以 0 呈現）。</summary>
    private async Task<Dictionary<int, int>> TryGetOrderCountsAsync(CancellationToken ct)
    {
        var ordersResult = await apiClient.ListOrdersAsync(ct);
        if (!ordersResult.IsSuccess)
            return [];

        return ordersResult.Value!
            .GroupBy(o => o.CustomerId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>單筆訂單金額：明細小計加總（小計 = 單價 × 數量 ×（1 − 折扣%/100））。</summary>
    private static decimal SumOrderTotal(OrderDto order)
        => order.Details.Sum(d => d.UnitPrice * d.OrderQuantities * (1m - (d.Discount / 100m)));

    /// <summary>正規化排序欄位（限 company / contact / orders；其餘回 company）。</summary>
    private static string NormalizeSort(string? sortBy)
        => sortBy is "contact" or "orders" ? sortBy : "company";

    /// <summary>依欄位與方向記憶體排序（以 key selector 統一升 / 降序）。</summary>
    private static List<CustomerViewModel> SortRows(List<CustomerViewModel> rows, string sort, bool desc)
    {
        Func<CustomerViewModel, object> keySelector = sort switch
        {
            "contact" => r => r.ContactName ?? string.Empty,
            "orders" => r => r.OrderCount,
            _ => r => r.CompanyName
        };
        return (desc ? rows.OrderByDescending(keySelector) : rows.OrderBy(keySelector)).ToList();
    }
}

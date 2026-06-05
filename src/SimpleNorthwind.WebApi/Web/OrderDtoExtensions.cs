using SimpleNorthwind.Application.Orders;

namespace SimpleNorthwind.WebApi.Web;

/// <summary>
/// 訂單金額的前端衍生計算（單一事實）。折扣為百分比（0..100）；小計 = 單價 × 數量 × (1 − 折扣/100)。
/// <para>
/// 此為呈現層衍生值（<c>OrderDtos</c> 註明「小計由前端」），故置於 <c>WebApi.Web</c> 而非下沉 Application；
/// 取代原先散落於 OrdersUi / CustomersUi / HomeUi 的三份重複計算（見 29-前端共用模組抽取稽核 #7）。
/// </para>
/// </summary>
public static class OrderDtoExtensions
{
    /// <summary>單筆明細折扣後小計。</summary>
    public static decimal LineTotal(this OrderDetailDto detail)
        => detail.UnitPrice * detail.OrderQuantities * (1m - (detail.Discount / 100m));

    /// <summary>整筆訂單折扣後總額 = Σ 各明細小計。</summary>
    public static decimal Total(this OrderDto order)
        => order.Details.Sum(detail => detail.LineTotal());
}

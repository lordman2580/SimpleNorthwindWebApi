using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SimpleNorthwind.WebApi.Web.TagHelpers;

/// <summary>
/// 訂單狀態 → 中文徽章。用法：<c>&lt;status-badge status="@o.Status" /&gt;</c>。
/// <para>
/// 單一事實的狀態對映，取代原先 Orders/Index、Orders/Details、Customers/Details、Home/Index 各自的
/// local function / <c>Func&lt;&gt;</c> / <c>@functions</c>，並統一 fallback（未知狀態 → 原字串 + bg-secondary），
/// 消除 Customers 版「未知 → 正常/綠」與其他版「未知 → 原字串/灰」的語意分歧
/// （見 29-前端共用模組抽取稽核 §0.1 / §3.3）。
/// </para>
/// </summary>
[HtmlTargetElement("status-badge", TagStructure = TagStructure.WithoutEndTag)]
public sealed class StatusBadgeTagHelper : TagHelper
{
    /// <summary>訂單狀態原字串（Normal / PaidOff / Canceled）。</summary>
    public string Status { get; set; } = string.Empty;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var (text, css) = Resolve(Status);
        output.TagName = "span";
        output.Attributes.SetAttribute("class", $"status-badge badge {css}");
        output.Content.SetContent(text);
    }

    /// <summary>狀態 → (中文, badge css)。可單元測試。</summary>
    public static (string Text, string Css) Resolve(string? status) => status switch
    {
        "Normal" => ("正常", "bg-success"),
        "PaidOff" => ("已付清", "bg-primary"),
        "Canceled" => ("已取消", "bg-secondary"),
        _ => (status ?? string.Empty, "bg-secondary"),
    };
}

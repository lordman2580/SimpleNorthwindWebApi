using System.Net;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SimpleNorthwind.WebApi.Web.TagHelpers;

/// <summary>
/// 表格空狀態列：<c>&lt;empty-row colspan="7" message="..." /&gt;</c> 渲染為
/// <c>&lt;tr&gt;&lt;td colspan="7" class="text-center text-muted py-4"&gt;…&lt;/td&gt;&lt;/tr&gt;</c>。
/// 取代各 list / 明細表格重複的空列樣板（僅涵蓋 table 形態，div/p 形態不在此，見
/// 29-前端共用模組抽取稽核 #11 / §3.4）。
/// </summary>
[HtmlTargetElement("empty-row", TagStructure = TagStructure.NormalOrSelfClosing)]
public sealed class EmptyRowTagHelper : TagHelper
{
    /// <summary>橫跨欄數。</summary>
    public int Colspan { get; set; }

    /// <summary>顯示訊息。</summary>
    public string Message { get; set; } = string.Empty;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "tr";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(
            $"<td colspan=\"{Colspan}\" class=\"text-center text-muted py-4\">{WebUtility.HtmlEncode(Message)}</td>");
    }
}

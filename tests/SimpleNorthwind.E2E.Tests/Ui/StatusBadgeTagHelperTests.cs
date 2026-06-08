using Shouldly;
using SimpleNorthwind.WebApi.Web.TagHelpers;

namespace SimpleNorthwind.E2E.Tests.Ui;

/// <summary>
/// 訂單狀態徽章對映（純單元，不需 DB / factory）：
/// 已結清(綠 bg-success)／未結清(黃 bg-warning)／已取消(紅 bg-danger)，未知狀態 fallback 灰。
/// 釘住「英文狀態鍵 → 中文文字 + 顏色」契約，防止再度回歸成全部 fallback 灰底（看似空白）。
/// </summary>
public sealed class StatusBadgeTagHelperTests
{
    [Theory]
    [InlineData("PaidOff", "已結清", "bg-success")]
    [InlineData("Normal", "未結清", "bg-warning")]
    [InlineData("Canceled", "已取消", "bg-danger")]
    public void Resolve_KnownStatus_MapsToLabelAndColor(string status, string text, string css)
    {
        var (resolvedText, resolvedCss) = StatusBadgeTagHelper.Resolve(status);

        resolvedText.ShouldBe(text);
        resolvedCss.ShouldBe(css);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SomethingElse")]
    public void Resolve_UnknownStatus_FallsBackToSecondary(string? status)
    {
        var (text, css) = StatusBadgeTagHelper.Resolve(status);

        text.ShouldBe(status ?? string.Empty);
        css.ShouldBe("bg-secondary");
    }
}

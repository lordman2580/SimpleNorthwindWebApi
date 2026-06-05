// 寫入瀏覽器 IANA 時區到 tz cookie，供 BearerTimeZoneHandler 帶上 X-Time-Zone。
// API 既有 ClientTimeZoneMiddleware 讀此 header → 輸出即為瀏覽器本地時區。見 19-前端架構與整合 §6。
(function () {
    try {
        var tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
        if (tz) {
            document.cookie = "tz=" + encodeURIComponent(tz) + "; path=/; SameSite=Lax; max-age=31536000";
        }
    } catch (e) {
        // 取不到時區 → 不寫 cookie，伺服器退回 App:DefaultTimeZone。
    }
})();

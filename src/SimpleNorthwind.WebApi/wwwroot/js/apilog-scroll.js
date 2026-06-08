// 稽核捲動載入：隨捲動每批 15 筆向下補入較舊紀錄（取代伺服器分頁列），並提供「返回最上層」。
// 與 apilog-live.js 共存：即時推播由上方 prepend；本檔只負責往下載入歷史頁與回頂。
(function () {
    "use strict";

    var rowsEl = document.getElementById("logRows");
    if (!rowsEl) return;

    // 捲動容器為 app shell 的 .content；退回 window（行動版或佈局調整時）。
    var scroller = document.querySelector(".content") || document.scrollingElement || document.documentElement;
    var useWindow = (scroller === document.scrollingElement || scroller === document.documentElement);

    var loaderEl = document.getElementById("logLoader");
    var endEl = document.getElementById("logEnd");
    var statusEl = document.getElementById("logStatus");
    var toTopBtn = document.getElementById("toTopBtn");

    var filterEl = document.getElementById("logFilter");
    var filter = {};
    try { filter = JSON.parse((filterEl && filterEl.textContent) || "{}"); } catch (e) { /* ignore */ }

    var total = parseInt(rowsEl.dataset.total || "0", 10);
    var serverLoaded = parseInt(rowsEl.dataset.loaded || "0", 10);
    var page = parseInt(rowsEl.dataset.page || "1", 10);
    var pageSize = parseInt(rowsEl.dataset.pageSize || "15", 10);
    var loading = false;
    var liveCount = 0;   // SignalR 即時推入的新列數（計入總筆數與已顯示）。

    // 已顯示筆數以 DOM 實際資料列計（對 MAX_ROWS 裁切與即時 prepend 皆正確）。
    function dataRowCount() {
        var n = rowsEl.children.length;
        if (document.getElementById("logEmpty")) n -= 1;   // 排除空狀態 placeholder
        return n;
    }

    function hasMore() { return serverLoaded < total; }

    function updateStatus() {
        var displayTotal = total + liveCount;               // 快照總數 + 即時新增
        if (statusEl) statusEl.textContent = displayTotal === 0
            ? "" : ("已顯示 " + dataRowCount() + " / " + displayTotal + " 筆");
        if (endEl) endEl.hidden = hasMore() || displayTotal === 0;
    }

    function buildQuery(nextPage) {
        var parts = ["page=" + nextPage, "pageSize=" + pageSize];
        if (filter.method) parts.push("method=" + encodeURIComponent(filter.method));
        if (filter.onlyErrors) parts.push("onlyErrors=true");
        if (filter.userId !== null && filter.userId !== undefined) parts.push("userId=" + encodeURIComponent(filter.userId));
        if (filter.from) parts.push("from=" + encodeURIComponent(filter.from));
        if (filter.to) parts.push("to=" + encodeURIComponent(filter.to));
        return parts.join("&");
    }

    function loadMore() {
        if (loading || !hasMore()) return;
        loading = true;
        if (loaderEl) loaderEl.hidden = false;

        var nextPage = page + 1;
        fetch("/apilogs/rows?" + buildQuery(nextPage), {
            headers: { "X-Requested-With": "XMLHttpRequest" },
            credentials: "same-origin"
        })
            .then(function (res) {
                if (res.status === 401) { window.location.href = "/account/login?expired=1"; return null; }
                if (!res.ok) throw new Error("load failed: " + res.status);
                return res.text();
            })
            .then(function (html) {
                if (html === null) return;
                var before = rowsEl.children.length;
                rowsEl.insertAdjacentHTML("beforeend", html);
                var appended = rowsEl.children.length - before;
                serverLoaded += appended;
                page = nextPage;
                rowsEl.dataset.loaded = String(serverLoaded);
                rowsEl.dataset.page = String(page);
                // 防呆：回應為空但理論上仍有資料時，停止避免無限請求。
                if (appended === 0) serverLoaded = total;
                updateStatus();
            })
            .catch(function () { /* 靜默：下次捲動可再試 */ })
            .finally(function () {
                loading = false;
                if (loaderEl) loaderEl.hidden = true;
            });
    }

    function metrics() {
        if (useWindow) {
            var doc = document.scrollingElement || document.documentElement;
            return { top: doc.scrollTop, view: window.innerHeight, height: doc.scrollHeight };
        }
        return { top: scroller.scrollTop, view: scroller.clientHeight, height: scroller.scrollHeight };
    }

    function onScroll() {
        var m = metrics();
        if (hasMore() && m.top + m.view >= m.height - 320) loadMore();
        if (toTopBtn) toTopBtn.hidden = m.top < 400;
    }

    (useWindow ? window : scroller).addEventListener("scroll", onScroll, { passive: true });

    if (toTopBtn) {
        toTopBtn.addEventListener("click", function () {
            if (useWindow) window.scrollTo({ top: 0, behavior: "smooth" });
            else scroller.scrollTo({ top: 0, behavior: "smooth" });
        });
    }

    // SignalR 即時推入新列（apilog-live.js 發出）→ 計入筆數並刷新狀態。
    document.addEventListener("apilog:prepended", function () {
        liveCount++;
        updateStatus();
    });

    // 初始狀態：內容不足一屏時主動補載，直到填滿或載完。
    updateStatus();
    function fillViewport() {
        var m = metrics();
        if (hasMore() && m.height <= m.view + 320 && !loading) {
            loadMore();
            setTimeout(fillViewport, 400);
        }
    }
    fillViewport();
})();

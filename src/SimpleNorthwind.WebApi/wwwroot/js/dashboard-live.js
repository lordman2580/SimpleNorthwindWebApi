// 首頁 dashboard 即時更新：訂閱既有稽核 Hub（/hubs/apilogs），收到 API 事件後（debounce）重抓
// server 端渲染的 dashboard 區塊換新，所有數字（訂單/客戶/未結/營收/最新訂單/庫存/今日稽核）即時刷新。
// 防回授：dashboard 自身重抓會打 /api/dashboard/summary（稽核為 "Dashboard.*"），故忽略此類事件，否則無限迴圈。
(function () {
    "use strict";

    var bodyEl = document.getElementById("dashboardBody");
    if (!bodyEl) return;

    var liveDot = document.getElementById("liveDot");
    var DEBOUNCE_MS = 1000;
    var timer = null;
    var refreshing = false;
    var pending = false;

    function scheduleRefresh() {
        if (timer) clearTimeout(timer);
        timer = setTimeout(refresh, DEBOUNCE_MS);
    }

    function refresh() {
        if (refreshing) { pending = true; return; }   // 重抓中又有新事件 → 抓完再補一次
        refreshing = true;
        fetch("/dashboard/body", {
            headers: { "X-Requested-With": "XMLHttpRequest" },
            credentials: "same-origin"
        })
            .then(function (res) {
                if (res.status === 401) { window.location.href = "/account/login?expired=1"; return null; }
                if (!res.ok) return null;   // 失敗保留現況
                return res.text();
            })
            .then(function (html) {
                if (html === null) return;
                bodyEl.innerHTML = html;
                bodyEl.classList.remove("just-updated");
                void bodyEl.offsetWidth;     // 重啟動畫
                bodyEl.classList.add("just-updated");
            })
            .catch(function () { /* 靜默：下個事件再試 */ })
            .finally(function () {
                refreshing = false;
                if (pending) { pending = false; scheduleRefresh(); }
            });
    }

    if (!window.signalR) return;

    var conn = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/apilogs")
        .withAutomaticReconnect()
        .build();

    conn.on("apilog", function (dto) {
        // 忽略 dashboard 自身的彙總讀取（避免重抓→稽核→再重抓的回授迴圈）。
        if (dto && typeof dto.actions === "string" && dto.actions.indexOf("Dashboard.") === 0) return;
        scheduleRefresh();
    });

    function setLive(on) { if (liveDot) liveDot.hidden = !on; }
    conn.onreconnecting(function () { setLive(false); });
    conn.onreconnected(function () { setLive(true); });
    conn.onclose(function () { setLive(false); });

    conn.start()
        .then(function () { setLive(true); })
        .catch(function () { /* 未連上不阻斷靜態檢視 */ });

    window.addEventListener("beforeunload", function () { conn.stop(); });
})();

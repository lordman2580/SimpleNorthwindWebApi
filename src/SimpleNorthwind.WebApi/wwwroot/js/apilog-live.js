// 稽核即時推播（F3.3）：連 /hubs/apilogs，收到 apilog 事件 → 依當前過濾條件 prepend 一列、
// 維持列表上限、UTC→瀏覽器本地時間格式化（與歷史列一致，見 26-即時稽核推播 §7/§8）。
// 結果檢視彈窗改「事件委派」綁定，讓動態新列的「檢視」鈕也有效（見 29-前端共用模組抽取稽核 #H）。
(function () {
    "use strict";

    var rowsEl = document.getElementById("logRows");
    if (!rowsEl) return;

    var filterEl = document.getElementById("logFilter");
    var filter = {};
    try { filter = JSON.parse((filterEl && filterEl.textContent) || "{}"); } catch (e) { /* ignore */ }
    var MAX_ROWS = 200;

    // ---- 與 server 端 ApiLogsUiController / view 對齊的呈現邏輯 ----
    function methodBadge(m) {
        switch ((m || "").toUpperCase()) {
            case "GET": return "bg-secondary";
            case "POST": return "bg-success";
            case "PUT": return "bg-warning text-dark";
            case "DELETE": return "bg-danger";
            default: return "bg-light text-dark border";
        }
    }
    function statusBadge(s) {
        if (s === null || s === undefined) return null;
        if (s >= 500) return "bg-danger";
        if (s >= 400) return "bg-warning text-dark";
        return "bg-success";
    }
    function parseMethodPath(actionDetail, actions) {
        if (!actionDetail || !actionDetail.trim()) return { method: actions || "", path: "" };
        var t = actionDetail.split(/\s+/).filter(Boolean);
        return { method: t[0] || actions || "", path: t[1] || "" };
    }
    function preview(text) {
        if (!text) return "";
        return text.length <= 120 ? text : text.slice(0, 120) + "…";
    }
    function pad(n) { return n < 10 ? "0" + n : "" + n; }
    function formatLocal(utc) {
        var d = new Date(utc);
        if (isNaN(d.getTime())) return "";
        return d.getFullYear() + "-" + pad(d.getMonth() + 1) + "-" + pad(d.getDate()) + " " +
            pad(d.getHours()) + ":" + pad(d.getMinutes()) + ":" + pad(d.getSeconds());
    }

    // ---- 過濾：與當前查詢條件不符的推播不顯示（§7）----
    function matches(dto, mp) {
        if (filter.method && (mp.method || "").toUpperCase() !== filter.method.toUpperCase()) return false;
        if (filter.onlyErrors && !(dto.responseStatus != null && dto.responseStatus >= 400)) return false;
        if (filter.userId != null && dto.userId !== filter.userId) return false;
        var t = new Date(dto.summaryDate).getTime();
        if (filter.from && t < new Date(filter.from).getTime()) return false;
        if (filter.to && t > new Date(filter.to).getTime()) return false;
        return true;
    }

    // ---- 建一列 <tr>（DOM API 自動 escape，避免來自稽核內容的 XSS）----
    function td(cls) { var e = document.createElement("td"); if (cls) e.className = cls; return e; }
    function badge(cls, text) { var s = document.createElement("span"); s.className = "badge " + cls; s.textContent = text; return s; }

    function buildRow(dto) {
        var mp = parseMethodPath(dto.actionDetail, dto.actions);
        var tr = document.createElement("tr");
        tr.className = "sn-row-new";

        var tdTime = td("text-nowrap small"); tdTime.textContent = formatLocal(dto.summaryDate); tr.appendChild(tdTime);

        var tdMethod = td(); tdMethod.appendChild(badge(methodBadge(mp.method), mp.method)); tr.appendChild(tdMethod);

        var tdPath = td("small"); var code = document.createElement("code"); code.textContent = mp.path; tdPath.appendChild(code); tr.appendChild(tdPath);

        var tdStatus = td();
        var sb = statusBadge(dto.responseStatus);
        if (sb) { tdStatus.appendChild(badge(sb, String(dto.responseStatus))); }
        else { var dash = document.createElement("span"); dash.className = "text-muted"; dash.textContent = "—"; tdStatus.appendChild(dash); }
        tr.appendChild(tdStatus);

        var tdUser = td(); tdUser.textContent = dto.userName || "系統 / 匿名"; tr.appendChild(tdUser);

        var tdIp = td("small text-muted"); tdIp.textContent = dto.clientIp || "—"; tr.appendChild(tdIp);

        var tdDur = td("text-end small"); tdDur.textContent = (dto.durationMs == null) ? "—" : (dto.durationMs + " ms"); tr.appendChild(tdDur);

        var tdResult = td("small");
        if (!dto.responseResult) {
            var d = document.createElement("span"); d.className = "text-muted"; d.textContent = "—"; tdResult.appendChild(d);
        } else {
            var wrap = document.createElement("div"); wrap.className = "d-flex align-items-center gap-2";
            var span = document.createElement("span"); span.className = "text-truncate d-inline-block"; span.style.maxWidth = "260px"; span.textContent = preview(dto.responseResult);
            var btn = document.createElement("button");
            btn.type = "button"; btn.className = "btn btn-sm btn-outline-secondary js-view-result flex-shrink-0";
            btn.setAttribute("data-result", dto.responseResult); btn.textContent = "檢視";
            wrap.appendChild(span); wrap.appendChild(btn); tdResult.appendChild(wrap);
        }
        tr.appendChild(tdResult);
        return tr;
    }

    function prepend(dto) {
        var mp = parseMethodPath(dto.actionDetail, dto.actions);
        if (!matches(dto, mp)) return;
        var empty = document.getElementById("logEmpty");
        if (empty) empty.remove();
        rowsEl.insertBefore(buildRow(dto), rowsEl.firstChild);
        while (rowsEl.children.length > MAX_ROWS) rowsEl.removeChild(rowsEl.lastChild);
        // 通知計數器（apilog-scroll.js）：有新列推入 → 更新「已顯示 X / N 筆」。
        document.dispatchEvent(new CustomEvent("apilog:prepended"));
    }

    // ---- 結果檢視彈窗：事件委派（涵蓋歷史列與動態新列）----
    var modalEl = document.getElementById("resultModal");
    if (modalEl && window.bootstrap) {
        var modal = new bootstrap.Modal(modalEl);
        var bodyEl = document.getElementById("resultModalBody");
        var pretty = function (text) { try { return JSON.stringify(JSON.parse(text), null, 2); } catch (e) { return text; } };
        document.addEventListener("click", function (ev) {
            var btn = ev.target.closest(".js-view-result");
            if (!btn) return;
            bodyEl.textContent = pretty(btn.getAttribute("data-result") || "");
            modal.show();
        });
    }

    // ---- SignalR：訂閱 apilog（Cookie 同源自動帶；連線失敗不影響靜態檢視）----
    if (window.signalR) {
        var conn = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/apilogs")
            .withAutomaticReconnect()
            .build();
        conn.on("apilog", prepend);
        conn.start().catch(function () { /* 未連上不阻斷頁面 */ });
        window.addEventListener("beforeunload", function () { conn.stop(); });
    }
})();

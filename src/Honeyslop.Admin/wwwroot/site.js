document.addEventListener("DOMContentLoaded", () => {
    const refreshSelection = () => {
        const selected = document.querySelectorAll("input[name='ids']:checked").length;
        document.querySelectorAll("[data-requires-selection]").forEach(button => {
            button.disabled = selected === 0;
            button.textContent = selected > 0 ? `Expire ${selected} selected` : "Expire selected";
        });
    };

    document.addEventListener("change", event => {
        if (event.target.matches("[data-select-all]")) {
            document.querySelectorAll("input[name='ids']").forEach(box => box.checked = event.target.checked);
        }
        if (event.target.matches("[data-select-all], input[name='ids']")) {
            refreshSelection();
        }
    });

    const tableSorts = new Map();
    const unitNumber = /^-?[\d,]+(\.\d+)?(\s?[a-zA-Z/%]+)?$/;
    const sortValue = cell => {
        const raw = (cell?.dataset.sort ?? cell?.textContent ?? "").trim();
        return raw === "—" ? "" : raw;
    };
    const applyTableSort = table => {
        const head = table.tHead?.rows[0];
        const body = table.tBodies[0];
        if (!head || !body) {
            return;
        }
        const state = tableSorts.get(table.dataset.sortable);
        Array.from(head.cells).forEach((cell, index) => {
            const sorted = state?.index === index;
            cell.classList.add("sortable");
            cell.classList.toggle("sorted-asc", sorted && state.dir === "asc");
            cell.classList.toggle("sorted-desc", sorted && state.dir === "desc");
            cell.setAttribute("aria-sort", sorted ? (state.dir === "asc" ? "ascending" : "descending") : "none");
        });
        const rows = Array.from(body.rows).filter(row => row.cells.length === head.cells.length);
        rows.forEach((row, index) => {
            if (row.dataset.order === undefined) {
                row.dataset.order = String(index);
            }
        });
        let ordered = [...rows].sort((a, b) => Number(a.dataset.order) - Number(b.dataset.order));
        if (state) {
            const values = new Map(rows.map(row => [row, sortValue(row.cells[state.index])]));
            const numeric = [...values.values()].every(value => value === "" || unitNumber.test(value));
            const key = value => numeric ? Number.parseFloat(value.replace(/,/g, "")) : value.toLowerCase();
            ordered = rows.sort((a, b) => {
                const x = values.get(a);
                const y = values.get(b);
                if (x === "" || y === "") {
                    return x === y ? 0 : x === "" ? 1 : -1;
                }
                const result = numeric ? key(x) - key(y) : key(x).localeCompare(key(y));
                return state.dir === "asc" ? result : -result;
            });
        }
        ordered.forEach(row => body.appendChild(row));
    };
    document.addEventListener("click", event => {
        const header = event.target.closest("table[data-sortable] thead th");
        if (!header) {
            return;
        }
        const table = header.closest("table");
        const key = table.dataset.sortable;
        const state = tableSorts.get(key);
        if (!state || state.index !== header.cellIndex) {
            tableSorts.set(key, { index: header.cellIndex, dir: "asc" });
        } else if (state.dir === "asc") {
            tableSorts.set(key, { index: header.cellIndex, dir: "desc" });
        } else {
            tableSorts.delete(key);
        }
        applyTableSort(table);
    });
    document.querySelectorAll("table[data-sortable]").forEach(applyTableSort);

    const dialog = document.querySelector("[data-confirm-dialog]");
    document.addEventListener("submit", event => {
        const form = event.target;
        const message = form.dataset.confirm;
        if (!message || form.dataset.confirmed === "true") {
            return;
        }
        event.preventDefault();
        const submitter = event.submitter;
        const proceed = () => {
            form.dataset.confirmed = "true";
            form.requestSubmit(submitter);
        };
        if (!dialog || typeof dialog.showModal !== "function") {
            if (window.confirm(message)) {
                proceed();
            }
            return;
        }
        const sender = form.querySelector("input[name='SenderName']");
        dialog.querySelector("[data-confirm-question]").textContent = message;
        dialog.querySelector("[data-confirm-detail]").textContent = form.dataset.confirmDetail || "";
        const name = sender ? sender.value.trim() : "";
        const address = form.dataset.confirmAddress || "";
        dialog.querySelector("[data-confirm-sender]").textContent = !sender
            ? ""
            : address
                ? `From: ${name ? `"${name}" <${address}>` : address}`
                : `From: ${name || "the address alone, without a name"}`;
        dialog.querySelector("[data-confirm-accept]").textContent = form.dataset.confirmButton || "Confirm";
        dialog.returnValue = "";
        dialog.addEventListener("close", () => {
            if (dialog.returnValue === "confirm" && form.isConnected) {
                proceed();
            }
            panels.filter(panel => panel.dirty).forEach(panel => panel.refresh());
        }, { once: true });
        dialog.showModal();
    });

    const panels = Array.from(document.querySelectorAll("[data-live-panel]")).map(element => {
        const panel = {
            element,
            topics: (element.dataset.liveTopics || "").split(" ").filter(Boolean),
            output: Number(element.dataset.liveOutput || 0),
            loading: false,
            dirty: false,
            last: 0
        };
        panel.refresh = async force => {
            if (panel.loading) {
                panel.dirty = true;
                return;
            }
            const typing = element.contains(document.activeElement) && document.activeElement.matches("input, textarea, select");
            const confirming = document.querySelector("dialog[open]") !== null;
            if (!force && (document.hidden || typing || confirming || element.querySelector("input[name='ids']:checked"))) {
                panel.dirty = true;
                return;
            }
            const wait = force ? 0 : 500 - (Date.now() - panel.last);
            if (wait > 0) {
                panel.dirty = true;
                window.setTimeout(panel.refresh, wait);
                return;
            }
            panel.loading = true;
            panel.dirty = false;
            const url = new URL(window.location.href);
            url.searchParams.set("handler", element.dataset.liveHandler || "Panel");
            url.searchParams.delete("Expired");
            try {
                const response = await fetch(url, { credentials: "same-origin", redirect: "manual" });
                if (response.type === "opaqueredirect") {
                    window.location.reload();
                    return;
                }
                if (response.ok) {
                    const html = await response.text();
                    const before = element.querySelector("pre");
                    const follow = !before || before.scrollTop + before.clientHeight >= before.scrollHeight - 4;
                    const top = before ? before.scrollTop : 0;
                    const open = Array.from(element.querySelectorAll("details[id][open]"), details => details.id);
                    const values = Array.from(element.querySelectorAll("input[id]:not([type=hidden])"), input => [input.id, input.value]);
                    element.innerHTML = html;
                    open.forEach(id => element.querySelector(`details[id="${CSS.escape(id)}"]`)?.setAttribute("open", ""));
                    values.forEach(([id, value]) => {
                        const input = element.querySelector(`input[id="${CSS.escape(id)}"]`);
                        if (input) {
                            input.value = value;
                        }
                    });
                    element.querySelectorAll("table[data-sortable]").forEach(applyTableSort);
                    const after = element.querySelector("pre");
                    if (after) {
                        after.scrollTop = follow ? after.scrollHeight : top;
                    }
                    refreshSelection();
                }
            } catch {
            } finally {
                panel.loading = false;
                panel.last = Date.now();
                if (panel.dirty) {
                    window.setTimeout(panel.refresh, 500);
                }
            }
        };
        element.addEventListener("focusout", () => {
            if (panel.dirty) {
                window.setTimeout(panel.refresh, 0);
            }
        });
        element.addEventListener("click", event => {
            if (event.target.closest("[data-reload]")) {
                element.innerHTML = "<p class=\"hint\">Looking it up again…</p>";
                panel.refresh(true);
            }
        });
        if (element.dataset.liveLoad !== undefined) {
            panel.refresh(true);
        }
        const interval = Number(element.dataset.liveInterval || 0);
        if (interval > 0) {
            window.setInterval(panel.refresh, interval);
        }
        return panel;
    });

    document.addEventListener("click", async event => {
        const button = event.target.closest("[data-copy]");
        const source = button && document.getElementById(button.dataset.copy);
        if (!source) {
            return;
        }
        try {
            await navigator.clipboard.writeText(source.textContent);
            button.textContent = "Copied";
        } catch {
            button.textContent = "Select the text and copy it";
        }
        window.setTimeout(() => button.textContent = "Copy", 2000);
    });

    document.querySelectorAll("form[data-auto-filter]").forEach(form => {
        let timer = 0;
        const apply = () => {
            window.clearTimeout(timer);
            const url = new URL(window.location.href);
            const current = new URLSearchParams(url.search);
            url.search = "";
            ["Sort", "Dir"].forEach(name => {
                if (current.get(name)) {
                    url.searchParams.set(name, current.get(name));
                }
            });
            new FormData(form).forEach((value, key) => {
                if (value !== "") {
                    url.searchParams.set(key, value);
                }
            });
            if (panels.length > 0) {
                window.history.replaceState(null, "", url);
                panels.forEach(panel => panel.refresh(true));
            } else {
                window.location.assign(url);
            }
        };
        form.addEventListener("change", event => {
            if (event.target.matches("select")) {
                apply();
            }
        });
        form.addEventListener("input", event => {
            if (event.target.matches("input")) {
                window.clearTimeout(timer);
                timer = window.setTimeout(apply, 400);
            }
        });
        form.addEventListener("submit", event => {
            event.preventDefault();
            apply();
        });
    });

    const entry = document.querySelector("[data-live-entry]");
    const status = document.querySelector("[data-live-status]");
    const setStatus = (online, text) => {
        if (status) {
            status.classList.toggle("online", online);
            status.textContent = text;
        }
    };

    let backoff = 1000;
    let fallback = 0;
    const connect = () => {
        const protocol = window.location.protocol === "https:" ? "wss" : "ws";
        const socket = new WebSocket(`${protocol}://${window.location.host}/live`);
        socket.addEventListener("open", () => {
            backoff = 1000;
            window.clearInterval(fallback);
            setStatus(true, "live");
            panels.forEach(panel => panel.refresh());
        });
        socket.addEventListener("message", event => {
            const message = JSON.parse(event.data);
            panels
                .filter(panel => panel.topics.some(topic => message.topics.includes(topic)))
                .filter(panel => !panel.output || message.outputs.includes(panel.output))
                .forEach(panel => panel.refresh());
            if (entry && message.entries.includes(Number(entry.dataset.liveEntry))) {
                window.location.reload();
            }
        });
        socket.addEventListener("close", () => {
            setStatus(false, "reconnecting…");
            window.clearInterval(fallback);
            fallback = window.setInterval(() => panels.forEach(panel => panel.refresh()), 10000);
            window.setTimeout(connect, backoff);
            backoff = Math.min(backoff * 2, 15000);
        });
    };
    connect();

    document.addEventListener("visibilitychange", () => {
        if (!document.hidden) {
            panels.filter(panel => panel.dirty).forEach(panel => panel.refresh());
        }
    });

    refreshSelection();
});

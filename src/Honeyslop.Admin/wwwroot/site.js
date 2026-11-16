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

    document.addEventListener("submit", event => {
        const message = event.target.dataset.confirm;
        if (message && !window.confirm(message)) {
            event.preventDefault();
        }
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
            if (!force && (document.hidden || element.querySelector("input[name='ids']:checked"))) {
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
            url.searchParams.set("handler", "Panel");
            url.searchParams.delete("Expired");
            try {
                const response = await fetch(url, { credentials: "same-origin", redirect: "manual" });
                if (response.ok) {
                    const html = await response.text();
                    const before = element.querySelector("pre");
                    const follow = !before || before.scrollTop + before.clientHeight >= before.scrollHeight - 4;
                    const top = before ? before.scrollTop : 0;
                    element.innerHTML = html;
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
        const interval = Number(element.dataset.liveInterval || 0);
        if (interval > 0) {
            window.setInterval(panel.refresh, interval);
        }
        return panel;
    });

    document.querySelectorAll("form[data-auto-filter]").forEach(form => {
        let timer = 0;
        const apply = () => {
            window.clearTimeout(timer);
            const url = new URL(window.location.href);
            url.search = "";
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

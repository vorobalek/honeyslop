export const viewports = {
  desktop: { width: 1280, height: 900 },
  phone: { width: 390, height: 844 }
};

export const pages = [
  { name: "entries", path: "/" },
  { name: "entries-sorted", path: "/?Sort=Hits&Dir=Desc" },
  { name: "entries-filtered", path: "/?Host=api&State=Ready" },
  { name: "entries-page-2", path: "/?p=1" },
  { name: "entry-ready", path: "/entries/2" },
  { name: "entry-failed", path: "/entries/3" },
  { name: "entry-expired", path: "/entries/6" },
  { name: "entry-queued", path: "/entries/67" },
  { name: "entry-generating", path: "/entries/69" },
  { name: "requests", path: "/requests" },
  { name: "requests-filtered", path: "/requests?ClientIp=176.9.38.39&Outcome=Hit" },
  { name: "requests-sorted", path: "/requests?Sort=Duration&Dir=Desc" },
  { name: "requests-page-2", path: "/requests?p=1" },
  { name: "request-post", path: "/requests/26" },
  { name: "clients-week", path: "/clients" },
  { name: "clients-all", path: "/clients?Days=0" },
  { name: "clients-sorted", path: "/clients?Days=0&Sort=Requests&Dir=Desc" },
  { name: "client-reported", path: "/clients/176.9.38.39" },
  { name: "client-failed", path: "/clients/185.220.101.1" },
  { name: "client-retrying", path: "/clients/203.0.113.7" },
  { name: "client-queued", path: "/clients/2a01:4f8:c17:1a2b::1" },
  { name: "client-no-contact", path: "/clients/8.8.8.8" },
  { name: "client-fresh", path: "/clients/45.95.147.236" },
  { name: "client-filtered", path: "/clients/176.9.38.39?Target=wp&Sort=Path&Dir=Asc" },
  { name: "reports", path: "/reports" },
  { name: "reports-sorted", path: "/reports?Sort=Status&Dir=Asc" },
  { name: "report-sent", path: "/reports/1" },
  { name: "report-failed", path: "/reports/2" },
  { name: "report-retrying", path: "/reports/3" },
  { name: "report-queued", path: "/reports/4" },
  { name: "generators", path: "/generators" },
  { name: "settings", path: "/settings" }
];

export function stabilize() {
  const replacements = [
    [/updated \d\d:\d\d:\d\d UTC/g, "updated 00:00:00 UTC"],
    [/(?<![\d.])(\d+ h \d+ min|\d+ min \d+ s|\d+ s)\b/g, "0 s"]
  ];
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    let text = node.nodeValue;
    for (const [pattern, value] of replacements) {
      text = text.replace(pattern, value);
    }
    if (text !== node.nodeValue) {
      node.nodeValue = text;
    }
  }
  document.querySelectorAll("table[data-sortable=generators] tbody td:nth-child(12)").forEach(cell => {
    if (cell.textContent.trim() !== "—") {
      cell.textContent = "2000-01-01 00:00:00";
    }
  });
}

export function normalize(html) {
  return html
    .replace(/name="__RequestVerificationToken" type="hidden" value="[^"]*"/g, 'name="__RequestVerificationToken" type="hidden" value=""')
    .replace(/\d\d:\d\d:\d\d UTC/g, "HH:mm:ss UTC")
    .replace(/\d+ h \d+ min|\d+ min \d+ s|\b\d+ s\b/g, "<duration>")
    .replace(/data-sort="-?\d+\.\d+(E[+-]?\d+)?"/g, 'data-sort="<n>"')
    .replace(/\?v=[\w-]+/g, "?v=")
    .replace(/ style=""/g, "")
    .replace(/[ \t]+\n/g, "\n")
    .replace(/\n{2,}/g, "\n");
}

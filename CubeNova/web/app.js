"use strict";
/* ───────────── bridge ───────────── */
const pending = new Map();
let seq = 0;
function call(cmd, args) {
  return new Promise((res, rej) => {
    const id = String(++seq);
    pending.set(id, { res, rej });
    window.chrome.webview.postMessage({ id, cmd, args: args || {} });
  });
}
window.chrome.webview.addEventListener("message", e => {
  const m = e.data;
  if (m.event) return onEvent(m.event, m.data);
  const p = pending.get(m.id);
  if (!p) return;
  pending.delete(m.id);
  m.ok ? p.res(m.data) : p.rej(new Error(m.error));
});

/* ───────────── i18n ───────────── */
const I18N = {
  en: {
    play: "Play", instances: "Instances", store: "Store", client: "Client", capes: "Capes", accounts: "Accounts", settings: "Settings",
    launch: "Play", stop: "Stop", starting: "Starting...", running: "Running",
    noInstance: "No instance yet", noInstanceSub: "Create an instance to start playing.",
    newInstance: "New instance", create: "Create", cancel: "Cancel", close: "Close", save: "Save", del: "Delete", folder: "Folder", manage: "Manage",
    name: "Name", mcVersion: "Minecraft version", loader: "Loader", vanilla: "Vanilla", fabric: "Fabric", showSnaps: "Show snapshots",
    console: "Console", noLogs: "Game output will appear here.",
    search: "Search...", installTo: "Install to", mods: "Mods", resourcepacks: "Resource packs", shaders: "Shaders",
    sortRel: "Relevance", sortDl: "Downloads", install: "Install", installing: "Installing...", installed: "Installed: ",
    loadMore: "Load more", noResults: "No results.", needFabric: "Mods need a Fabric instance. Pick or create one.",
    downloads: "downloads", by: "by",
    overview: "Overview", ram: "Memory (MB)", ramDefault: "0 = use global setting", builtin: "CubeNova Client mods", builtinOff: "Only for Fabric instances on supported versions.",
    openStore: "Open store", noFiles: "Nothing here yet.",
    clientTitle: "CubeNova Client", clientSub: "Built-in HUD mods, loaded automatically into Fabric instances. Press Right Shift in game to open the menu and drag HUD elements.",
    clientNoJar: "The built-in mod jar for this Minecraft version is missing from the builtin folder. Build it from the /mod project (see README).",
    clientNeedFabric: "Select a Fabric instance to configure client mods.",
    m_fps: "FPS", m_fps_d: "Frames per second counter", m_cps: "CPS", m_cps_d: "Clicks per second (left | right)",
    m_keystrokes: "Keystrokes", m_keystrokes_d: "W A S D, space and mouse buttons", m_coords: "Coordinates", m_coords_d: "XYZ position",
    m_direction: "Direction", m_direction_d: "Facing direction", m_clock: "Clock", m_clock_d: "Real time",
    m_sprint: "Toggle sprint", m_sprint_d: "Hold forward to sprint automatically", m_zoom: "Zoom", m_zoom_d: "Hold C to zoom in",
    m_nightvision: "Night Vision", m_nightvision_d: "See clearly in the dark (client-side)",
    m_uncap: "Max Performance", m_uncap_d: "On join: turn VSync off and set FPS limit to unlimited",
    account: "Account", addOffline: "Add offline account", username: "Username", msLogin: "Sign in with Microsoft", add: "Add",
    offline: "Offline", microsoft: "Microsoft", expired: "Expired - sign in again", remove: "Remove", use: "Use",
    noAccount: "No account", language: "Language", memory: "Default memory", jvm: "Extra JVM arguments", hideOnPlay: "Minimize launcher while playing",
    about: "CubeNova Launcher", saved: "Saved", confirmDel: "Delete this? This cannot be undone.",
    exited: "Game exited with code ", enable: "Enabled",
    updates: "Updates", version: "Version", checkNow: "Check for updates", checking: "Checking...",
    upToDate: "You are up to date.", updateAvail: "A new version is available.", updateFail: "Could not check for updates: ",
    updateTitle: "Update available", updateText: "Your version: {0}  →  latest: {1}", download: "Update now", updating: "Updating...", restarting: "Restarting...", releasePage: "Release page", later: "Later",
    fpsBoost: "FPS Boost", fpsBoostD: "On by default: installs performance mods (Sodium, Lithium, FerriteCore, EntityCulling, ImmediatelyFast, ModernFix) for you and uses optimized Java settings. Your video settings are not changed. Needs internet the first time.",
    autoCheck: "Check for updates on startup", unknownVer: "unknown",
    m_freelook: "Freelook", m_freelook_d: "Hold Left Alt to look around without turning your player",
    m_capes: "Custom capes", m_capes_d: "Show your custom cape (only you can see it)",
    capeTitle: "Custom cape", capeSub: "Choose any image (JPG, PNG, WebP) and pick the part you want: it is fitted to a cape automatically. Only you can see it, in game, with the CubeNova Client mod. Restart the game after changing it.",
    elytraTitle: "Custom elytra", elytraSub: "Optional. Without one you get the normal elytra (your cape is not drawn on it).", elytraNone: "Using the normal elytra.", elytraChoose: "Choose image", elytraRemove: "Remove elytra", elytraSaved: "Elytra saved", elytraRemoved: "Elytra removed", cropTitleElytra: "Adjust your elytra",
    cropTitle: "Adjust your cape", cropHint: "Drag the box to move it, drag the corner to resize. The selected area becomes the back of the cape / the outside of the wing.", cropPreview: "Preview", cropBad: "Could not open this image.",
    capeFor: "Account", capeNone: "No cape uploaded.", capeChoose: "Choose image", capeRemove: "Remove cape", capeUploaded: "Cape saved", capeRemoved: "Cape removed", capeLoading: "Loading...",
    noClientVersions: "No CubeNova Client version found. Build the mod first (run build-mod.bat in CubeNova-Client-Mods\\CubeNova-Client-<version>), then restart the launcher.",
  },
  zh: {
    play: "遊玩", instances: "實例", store: "商店", client: "客戶端", capes: "披風", accounts: "帳號", settings: "設定",
    launch: "開始遊戲", stop: "停止", starting: "啟動中...", running: "遊戲執行中",
    noInstance: "還沒有實例", noInstanceSub: "建立一個實例就能開始玩。",
    newInstance: "新增實例", create: "建立", cancel: "取消", close: "關閉", save: "儲存", del: "刪除", folder: "資料夾", manage: "管理",
    name: "名稱", mcVersion: "Minecraft 版本", loader: "載入器", vanilla: "原版", fabric: "Fabric", showSnaps: "顯示快照版",
    console: "主控台", noLogs: "遊戲輸出會顯示在這裡。",
    search: "搜尋...", installTo: "安裝到", mods: "模組", resourcepacks: "資源包", shaders: "光影",
    sortRel: "相關度", sortDl: "下載數", install: "安裝", installing: "安裝中...", installed: "已安裝：",
    loadMore: "載入更多", noResults: "沒有結果。", needFabric: "模組需要 Fabric 實例，請選擇或建立一個。",
    downloads: "次下載", by: "作者",
    overview: "概覽", ram: "記憶體 (MB)", ramDefault: "0 = 使用全域設定", builtin: "CubeNova 內建模組", builtinOff: "僅支援的版本且為 Fabric 實例。",
    openStore: "前往商店", noFiles: "這裡還沒有東西。",
    clientTitle: "CubeNova 客戶端", clientSub: "內建 HUD 模組，會自動載入 Fabric 實例。遊戲中按右 Shift 開啟選單並拖曳 HUD。",
    clientNoJar: "builtin 資料夾中沒有這個 Minecraft 版本的內建模組 jar，請用 /mod 專案建置（見 README）。",
    clientNeedFabric: "請選擇一個 Fabric 實例來設定客戶端模組。",
    m_fps: "FPS", m_fps_d: "每秒幀數", m_cps: "CPS", m_cps_d: "每秒點擊數 (左 | 右)",
    m_keystrokes: "按鍵顯示", m_keystrokes_d: "W A S D、空白鍵與滑鼠按鍵", m_coords: "座標", m_coords_d: "XYZ 位置",
    m_direction: "方向", m_direction_d: "面向的方向", m_clock: "時鐘", m_clock_d: "現實時間",
    m_sprint: "切換衝刺", m_sprint_d: "按住前進自動衝刺", m_zoom: "縮放", m_zoom_d: "按住 C 放大",
    m_nightvision: "夜視", m_nightvision_d: "黑暗中也看得清楚（僅本機）",
    m_uncap: "最大效能", m_uncap_d: "進入遊戲時自動關閉垂直同步，並把 FPS 上限設為無限",
    account: "帳號", addOffline: "新增離線帳號", username: "使用者名稱", msLogin: "使用 Microsoft 登入", add: "新增",
    offline: "離線", microsoft: "Microsoft", expired: "已過期，請重新登入", remove: "移除", use: "使用",
    noAccount: "沒有帳號", language: "語言", memory: "預設記憶體", jvm: "額外 JVM 參數", hideOnPlay: "遊玩時最小化啟動器",
    about: "CubeNova Launcher", saved: "已儲存", confirmDel: "確定要刪除？此動作無法復原。",
    exited: "遊戲結束，代碼 ", enable: "啟用",
    updates: "更新", version: "版本", checkNow: "檢查更新", checking: "檢查中...",
    upToDate: "目前已是最新版本。", updateAvail: "有新版本可以更新。", updateFail: "無法檢查更新：",
    updateTitle: "有新版本", updateText: "你的版本：{0}  →  最新版本：{1}", download: "立即更新", updating: "更新中...", restarting: "即將重新啟動...", releasePage: "Release 頁面", later: "稍後",
    fpsBoost: "FPS 加速", fpsBoostD: "預設開啟：自動幫你安裝效能模組 (Sodium、Lithium、FerriteCore、EntityCulling、ImmediatelyFast、ModernFix) 並使用優化的 Java 參數。不會改你的畫面設定。第一次需要網路。",
    autoCheck: "啟動時自動檢查更新", unknownVer: "未知",
    m_freelook: "自由視角", m_freelook_d: "按住左 Alt 環顧四周，不會轉動你的角色",
    m_capes: "自訂披風", m_capes_d: "顯示你的自訂披風 (只有你自己看得到)",
    capeTitle: "自訂披風", capeSub: "選擇任何圖片 (JPG、PNG、WebP)，再框選你想用的部分，會自動適配成披風。只有你自己在遊戲中看得到 (需要 CubeNova Client 模組)。更換後請重新啟動遊戲。",
    elytraTitle: "自訂鞘翅", elytraSub: "選用。沒有設定時會是普通鞘翅 (不會把披風貼在鞘翅上)。", elytraNone: "使用普通鞘翅。", elytraChoose: "選擇圖片", elytraRemove: "移除鞘翅", elytraSaved: "鞘翅已儲存", elytraRemoved: "鞘翅已移除", cropTitleElytra: "調整你的鞘翅",
    cropTitle: "調整你的披風", cropHint: "拖曳方框移動位置，拖曳右下角調整大小。框選的範圍會成為披風的背面 / 鞘翅的外側。", cropPreview: "預覽", cropBad: "無法開啟這張圖片。",
    capeFor: "帳號", capeNone: "還沒有上傳披風。", capeChoose: "選擇圖片", capeRemove: "移除披風", capeUploaded: "披風已儲存", capeRemoved: "披風已移除", capeLoading: "載入中...",
    noClientVersions: "找不到任何 CubeNova 客戶端版本。請先建置模組（執行 CubeNova-Client-Mods\\CubeNova-Client-<版本>\\build-mod.bat），再重開啟動器。",
  },
};
let lang = "en";
const t = k => (I18N[lang] && I18N[lang][k]) || I18N.en[k] || k;

/* ───────────── helpers ───────────── */
const $ = (s, r = document) => r.querySelector(s);
const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const fmtNum = n => n >= 1e6 ? (n / 1e6).toFixed(1) + "M" : n >= 1e3 ? (n / 1e3).toFixed(1) + "K" : String(n);
const fmtSize = n => n > 1048576 ? (n / 1048576).toFixed(1) + " MB" : Math.max(1, Math.round(n / 1024)) + " KB";

function toast(msg, err) {
  const d = document.createElement("div");
  d.className = "toast" + (err ? " err" : "");
  d.textContent = msg;
  $("#toasts").appendChild(d);
  setTimeout(() => d.remove(), err ? 9000 : 3500);
}
async function safe(fn) {
  try { return await fn(); } catch (e) { toast(e.message || String(e), true); }
}

const ICONS = {
  play: '<svg viewBox="0 0 24 24"><path d="M7 4l13 8-13 8z"/></svg>',
  instances: '<svg viewBox="0 0 24 24"><path d="M12 3l8 4.5v9L12 21l-8-4.5v-9z"/><path d="M12 12l8-4.5M12 12L4 7.5M12 12v9"/></svg>',
  store: '<svg viewBox="0 0 24 24"><path d="M4 8h16l-1 12H5z"/><path d="M8 8V6a4 4 0 018 0v2"/></svg>',
  client: '<svg viewBox="0 0 24 24"><rect x="3" y="5" width="18" height="12" rx="2"/><path d="M8 21h8M12 17v4M7 10h4M7 13h2"/></svg>',
  capes: '<svg viewBox="0 0 24 24"><path d="M8 3h8l2 4-2 14H8L6 7z"/><path d="M8 3c1 2 7 2 8 0"/></svg>',
  accounts: '<svg viewBox="0 0 24 24"><circle cx="12" cy="8" r="4"/><path d="M4 21c0-4 4-6 8-6s8 2 8 6"/></svg>',
  settings: '<svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1L7 17M17 7l2.1-2.1"/></svg>',
};

/* ───────────── state ───────────── */
const S = {
  page: "play", settings: {}, accounts: [], instances: [], builtinVersions: [], totalRamMb: 8192,
  state: {},      // instanceId -> idle|installing|running
  progress: {},   // instanceId -> {text,pct}
  logs: {},       // instanceId -> [lines]
  showConsole: false,
  store: { type: "mod", query: "", sort: "relevance", instanceId: "", offset: 0, hits: [], total: 0, loading: false },
  clientInst: "",
  mcList: null, mcSnaps: false,
};

const curInstance = () => S.instances.find(i => i.id === S.settings.selectedInstance) || S.instances[0] || null;
const curAccount = () => S.accounts.find(a => a.id === S.settings.selectedAccount) || S.accounts[0] || null;
const loaderName = i => i.loader === "fabric" ? t("fabric") : t("vanilla");

/* ───────────── events from C# ───────────── */
function onEvent(ev, d) {
  if (ev === "state") {
    S.state[d.id] = d.state;
    if (d.state === "idle") delete S.progress[d.id];
    if (d.state === "installing") S.logs[d.id] = [];
    if (S.page === "play" || S.page === "instances") render();
  } else if (ev === "progress") {
    S.progress[d.id] = d;
    updateProgress(d.id);
  } else if (ev === "log") {
    const arr = (S.logs[d.id] = S.logs[d.id] || []);
    arr.push(d.line);
    if (arr.length > 800) arr.shift();
    appendLog(d.id, d.line);
  } else if (ev === "updateProgress") {
    const b = $("#upbar"), tx = $("#uptext");
    if (b) b.style.width = Math.round(d.pct * 100) + "%";
    if (tx) tx.textContent = d.text;
  } else if (ev === "winstate") {
    document.body.classList.toggle("max", !!d.maximized);
  } else if (ev === "exit") {
    if (d.code !== 0) {
      toast(t("exited") + d.code, true);
      S.showConsole = true;
      if (S.page === "play") render();
    }
  }
}

/* ───────────── render core ───────────── */
function render() {
  document.documentElement.lang = lang === "zh" ? "zh-Hant" : "en";
  document.querySelectorAll(".nav").forEach(b => {
    const p = b.dataset.p;
    b.innerHTML = ICONS[p] + "<span>" + esc(t(p)) + "</span>" + (p === "settings" && S.update && S.update.available ? '<i class="dot"></i>' : "");
    b.classList.toggle("on", p === S.page);
  });
  $("#title").textContent = t(S.page);
  const a = curAccount();
  $("#accChip").innerHTML = a
    ? `<div class="av">${avatarHtml(a)}</div><div><div style="font-weight:600;font-size:13px">${esc(a.name)}</div><div class="dim" style="font-size:11px">${esc(t(a.type))}</div></div>`
    : `<div class="av">?</div><div class="muted">${esc(t("noAccount"))}</div>`;
  const fn = PAGES[S.page];
  const el = $("#page");
  const sc = el.scrollTop;
  fn(el);
  el.scrollTop = sc;
}
function avatarHtml(a) {
  if (a.type === "microsoft") return `<img src="https://mc-heads.net/avatar/${esc(a.uuid)}/64" onerror="this.parentNode.textContent='${esc(a.name[0] || "?").toUpperCase()}'">`;
  return esc((a.name[0] || "?").toUpperCase());
}
function go(p) { S.page = p; render(); }

function openModal(html) { $("#dlg").innerHTML = html; $("#modal").classList.add("on"); }
function closeModal() { $("#modal").classList.remove("on"); $("#dlg").innerHTML = ""; }
$("#modal").addEventListener("mousedown", e => { if (e.target.id === "modal") closeModal(); });
document.querySelectorAll(".nav").forEach(b => b.addEventListener("click", () => go(b.dataset.p)));
$("#accChip").addEventListener("click", () => go("accounts"));

async function reloadState() {
  const d = await call("init");
  S.settings = d.settings;
  S.accounts = d.accounts;
  S.instances = d.instances;
  S.builtinVersions = d.builtinVersions;
  S.totalRamMb = d.totalRamMb || 8192;
  lang = d.settings.language || "en";
  d.running.forEach(id => S.state[id] = "running");
}

/* ───────────── pages ───────────── */
const PAGES = {};

/* PLAY */
PAGES.play = el => {
  const inst = curInstance();
  if (!inst) {
    el.innerHTML = `<div class="empty"><h2>${esc(t("noInstance"))}</h2><p>${esc(t("noInstanceSub"))}</p><br><button class="btn pri" id="nw">${esc(t("newInstance"))}</button></div>`;
    $("#nw").onclick = newInstanceDialog;
    return;
  }
  const st = S.state[inst.id] || "idle";
  const hasBuiltin = inst.loader === "fabric" && inst.builtinMods && S.builtinVersions.includes(inst.mc);
  el.innerHTML = `
  <div class="hero">
    <div><span class="tag g">${esc(inst.mc)}</span><span class="tag">${esc(loaderName(inst))}</span>${hasBuiltin ? `<span class="tag g">CubeNova Client</span>` : ""}</div>
    <h1>${esc(inst.name)}</h1>
    <div class="row" style="margin-top:14px">
      <button class="btn pri bigplay" id="pb">${esc(st === "idle" ? t("launch") : st === "installing" ? t("stop") : t("stop"))}</button>
      <div class="sp col" style="gap:6px;max-width:520px">
        <div class="muted" id="ptext" style="min-height:20px">${st === "running" ? esc(t("running")) : ""}</div>
        <div class="bar"><i id="pbar"></i></div>
      </div>
    </div>
  </div>
  <div class="row" style="margin:18px 0 10px"><h2 style="margin:0">${esc(t("console"))}</h2><div class="sp"></div>
    <button class="btn sm" id="tc">${S.showConsole ? "−" : "+"}</button></div>
  ${S.showConsole ? `<div class="console" id="con"></div>` : ""}`;
  $("#pb").onclick = () => safe(async () => {
    if (st === "idle") await call("launch", { id: inst.id });
    else await call("stop", { id: inst.id });
  });
  $("#tc").onclick = () => { S.showConsole = !S.showConsole; render(); };
  if (S.showConsole) {
    const con = $("#con");
    const lines = S.logs[inst.id] || [];
    if (!lines.length) con.innerHTML = `<span class="dim">${esc(t("noLogs"))}</span>`;
    else con.innerHTML = lines.map(logHtml).join("");
    con.scrollTop = con.scrollHeight;
  }
  updateProgress(inst.id);
};
function logHtml(l) {
  const c = /\b(ERROR|FATAL|Exception)\b/.test(l) ? "e" : /\bWARN\b/.test(l) ? "w" : "";
  return `<div class="${c}">${esc(l)}</div>`;
}
function appendLog(id, line) {
  const inst = curInstance();
  const con = $("#con");
  if (S.page !== "play" || !inst || inst.id !== id || !con) return;
  if (con.querySelector(".dim")) con.innerHTML = "";
  const stick = con.scrollTop + con.clientHeight >= con.scrollHeight - 30;
  con.insertAdjacentHTML("beforeend", logHtml(line));
  if (stick) con.scrollTop = con.scrollHeight;
}
function updateProgress(id) {
  const inst = curInstance();
  if (S.page !== "play" || !inst || inst.id !== id) return;
  const p = S.progress[id];
  const bar = $("#pbar"), tx = $("#ptext");
  if (!bar) return;
  if (p) { bar.style.width = Math.round(p.pct * 100) + "%"; tx.textContent = p.text; }
  else { bar.style.width = "0"; }
}

/* INSTANCES */
PAGES.instances = el => {
  const cards = S.instances.map(i => {
    const st = S.state[i.id] || "idle";
    return `<div class="card inst ${i.id === S.settings.selectedInstance ? "sel" : ""}" data-id="${i.id}">
      <div class="row"><div class="cube">${esc((i.name[0] || "?").toUpperCase())}</div>
        <div style="min-width:0"><div class="nm" style="font-weight:700;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${esc(i.name)}</div>
        <div><span class="tag g">${esc(i.mc)}</span><span class="tag">${esc(loaderName(i))}</span></div></div></div>
      <div class="row" style="margin-top:16px">
        <button class="btn pri sm" data-a="play">${esc(st === "idle" ? t("launch") : t("stop"))}</button>
        <button class="btn sm" data-a="manage">${esc(t("manage"))}</button>
        <button class="btn sm" data-a="folder">${esc(t("folder"))}</button>
      </div></div>`;
  }).join("");
  el.innerHTML = `<div class="grid">${cards}<div class="card add" id="addi">+ ${esc(t("newInstance"))}</div></div>`;
  $("#addi").onclick = newInstanceDialog;
  el.querySelectorAll(".inst").forEach(c => {
    const id = c.dataset.id;
    c.onclick = e => {
      const a = e.target.dataset.a;
      if (!a) { call("selectInstance", { id }); S.settings.selectedInstance = id; render(); return; }
      safe(async () => {
        if (a === "play") {
          S.settings.selectedInstance = id; call("selectInstance", { id });
          go("play");
          if ((S.state[id] || "idle") === "idle") await call("launch", { id }); else await call("stop", { id });
        } else if (a === "manage") manageDialog(id);
        else if (a === "folder") await call("openFolder", { id });
      });
    };
  });
};

async function getMcList() {
  if (!S.mcList || S.mcSnapsLoaded !== S.mcSnaps) {
    S.mcList = await call("mcVersions", { snapshots: S.mcSnaps });
    S.mcSnapsLoaded = S.mcSnaps;
  }
  return S.mcList;
}
function newInstanceDialog() {
  const vers = (S.builtinVersions || []).slice().sort((a, b) => b.localeCompare(a, undefined, { numeric: true }));
  if (!vers.length) {
    openModal(`<h3>${esc(t("newInstance"))}</h3><p style="color:var(--yel)">${esc(t("noClientVersions"))}</p>
      <div class="row" style="margin-top:16px"><div class="sp"></div><button class="btn" id="ni-cancel">${esc(t("close"))}</button></div>`);
    $("#ni-cancel").onclick = closeModal;
    return;
  }
  openModal(`<h3>${esc(t("newInstance"))}</h3><div class="col">
    <div><label class="lbl">${esc(t("name"))}</label><input class="input" id="ni-name" placeholder="CubeNova ${esc(vers[0])}"></div>
    <div><label class="lbl">${esc(t("mcVersion"))}</label>
      <select class="select" id="ni-mc">${vers.map(v => `<option value="${esc(v)}">${esc(v)}</option>`).join("")}</select></div>
    <div class="row"><span class="tag g">Fabric</span><span class="tag g">CubeNova Client</span></div>
    <div class="row"><div class="sp"></div><button class="btn" id="ni-cancel">${esc(t("cancel"))}</button><button class="btn pri" id="ni-ok">${esc(t("create"))}</button></div>
  </div>`);
  $("#ni-cancel").onclick = closeModal;
  $("#ni-ok").onclick = () => safe(async () => {
    $("#ni-ok").disabled = true;
    try {
      const inst = await call("createInstance", { name: $("#ni-name").value, mc: $("#ni-mc").value, loader: "fabric" });
      await reloadState();
      closeModal();
      render();
      toast(inst.name);
    } finally { const b = $("#ni-ok"); if (b) b.disabled = false; }
  });
}

async function manageDialog(id, tab = "overview") {
  const inst = S.instances.find(i => i.id === id);
  if (!inst) return;
  const tabs = ["overview", "mods", "resourcepacks", "shaderpacks"];
  const tabName = k => k === "shaderpacks" ? t("shaders") : t(k);
  let body = "";
  if (tab === "overview") {
    const canBuiltin = inst.loader === "fabric" && S.builtinVersions.includes(inst.mc);
    body = `<div class="col">
      <div><label class="lbl">${esc(t("name"))}</label><input class="input" id="mg-name" value="${esc(inst.name)}"></div>
      <div><label class="lbl">${esc(t("ram"))} — <span class="dim">${esc(t("ramDefault"))}</span></label>
        <input class="input" id="mg-ram" type="number" min="0" step="256" value="${inst.ramMb}"></div>
      <label class="row"><span class="sw"><input type="checkbox" id="mg-b" ${inst.builtinMods ? "checked" : ""} ${canBuiltin ? "" : "disabled"}><span></span></span>
        <div><div>${esc(t("builtin"))}</div>${canBuiltin ? "" : `<div class="dim" style="font-size:12px">${esc(t("builtinOff"))}</div>`}</div></label>
      <div class="row"><button class="btn dng" id="mg-del">${esc(t("del"))}</button><div class="sp"></div>
        <button class="btn pri" id="mg-save">${esc(t("save"))}</button></div></div>`;
  } else {
    let files = [];
    await safe(async () => { files = await call("listFiles", { id, kind: tab }); });
    body = `<div class="row" style="margin-bottom:12px"><button class="btn sm" id="mg-open">${esc(t("folder"))}</button>
      <button class="btn sm" id="mg-store">${esc(t("openStore"))}</button></div>` +
      (files.length ? files.map(f => `<div class="item" style="padding:9px 12px">
        <label class="sw"><input type="checkbox" data-f="${esc(f.name)}" ${f.enabled ? "checked" : ""}><span></span></label>
        <div class="sp" style="min-width:0"><div style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap;${f.enabled ? "" : "opacity:.5"}">${esc(f.name.replace(/\.disabled$/, ""))}</div></div>
        <span class="dim">${fmtSize(f.size)}</span>
        <button class="btn sm dng" data-d="${esc(f.name)}">✕</button></div>`).join("")
        : `<div class="empty">${esc(t("noFiles"))}</div>`);
  }
  openModal(`<h3>${esc(inst.name)}</h3>
    <div class="tabs">${tabs.map(k => `<div class="tab ${k === tab ? "on" : ""}" data-t="${k}">${esc(tabName(k))}</div>`).join("")}</div>${body}
    <div class="row" style="margin-top:16px"><div class="sp"></div><button class="btn" id="mg-close">${esc(t("close"))}</button></div>`);
  $("#mg-close").onclick = closeModal;
  document.querySelectorAll(".dlg .tab").forEach(x => x.onclick = () => manageDialog(id, x.dataset.t));
  if (tab === "overview") {
    $("#mg-save").onclick = () => safe(async () => {
      await call("updateInstance", { id, name: $("#mg-name").value, ramMb: parseInt($("#mg-ram").value) || 0, builtin: $("#mg-b").checked });
      await reloadState(); render(); toast(t("saved"));
    });
    $("#mg-del").onclick = () => { if (confirm(t("confirmDel"))) safe(async () => { await call("deleteInstance", { id }); await reloadState(); closeModal(); render(); }); };
  } else {
    $("#mg-open").onclick = () => call("openFolder", { id, sub: tab }).catch(e => toast(e.message, true));
    $("#mg-store").onclick = () => { S.store.instanceId = id; S.store.type = tab === "mods" ? "mod" : tab === "resourcepacks" ? "resourcepack" : "shader"; S.store.hits = []; S.store.offset = 0; closeModal(); go("store"); };
    document.querySelectorAll(".dlg [data-f]").forEach(c => c.onchange = () => safe(async () => { await call("toggleFile", { id, kind: tab, name: c.dataset.f }); manageDialog(id, tab); }));
    document.querySelectorAll(".dlg [data-d]").forEach(c => c.onclick = () => safe(async () => { await call("deleteFile", { id, kind: tab, name: c.dataset.d }); manageDialog(id, tab); }));
  }
}

/* STORE */
PAGES.store = el => {
  const s = S.store;
  if (!s.instanceId || !S.instances.find(i => i.id === s.instanceId)) s.instanceId = (curInstance() || {}).id || "";
  const inst = S.instances.find(i => i.id === s.instanceId);
  const types = [["mod", t("mods")], ["resourcepack", t("resourcepacks")], ["shader", t("shaders")]];
  el.innerHTML = `
    <div class="row wrap" style="margin-bottom:14px">
      <div style="width:240px"><select class="select" id="st-inst">${S.instances.map(i => `<option value="${i.id}" ${i.id === s.instanceId ? "selected" : ""}>${esc(i.name)} · ${esc(i.mc)} ${esc(loaderName(i))}</option>`).join("")}</select></div>
      <div class="sp" style="min-width:200px"><input class="input" id="st-q" placeholder="${esc(t("search"))}" value="${esc(s.query)}"></div>
      <div style="width:150px"><select class="select" id="st-sort"><option value="relevance" ${s.sort === "relevance" ? "selected" : ""}>${esc(t("sortRel"))}</option><option value="downloads" ${s.sort === "downloads" ? "selected" : ""}>${esc(t("sortDl"))}</option></select></div>
    </div>
    <div class="tabs">${types.map(([k, n]) => `<div class="tab ${k === s.type ? "on" : ""}" data-k="${k}">${esc(n)}</div>`).join("")}</div>
    ${!inst ? `<div class="empty">${esc(t("noInstance"))}</div>` : (s.type === "mod" && inst.loader !== "fabric") ? `<div class="empty">${esc(t("needFabric"))}</div>` : `<div id="st-list"></div>`}`;
  if (!inst) return;
  $("#st-inst").onchange = e => { s.instanceId = e.target.value; s.hits = []; s.offset = 0; render(); };
  let timer;
  $("#st-q").oninput = e => { s.query = e.target.value; clearTimeout(timer); timer = setTimeout(() => runSearch(true), 350); };
  $("#st-sort").onchange = e => { s.sort = e.target.value; runSearch(true); };
  el.querySelectorAll(".tab").forEach(x => x.onclick = () => { s.type = x.dataset.k; s.hits = []; s.offset = 0; render(); });
  if (s.type === "mod" && inst.loader !== "fabric") return;
  if (!s.hits.length) runSearch(true); else drawHits();
};
async function runSearch(reset) {
  const s = S.store;
  const inst = S.instances.find(i => i.id === s.instanceId);
  if (!inst || s.loading) return;
  if (reset) { s.offset = 0; s.hits = []; }
  s.loading = true;
  try {
    const r = await call("search", { type: s.type, query: s.query, mc: inst.mc, loader: inst.loader, offset: s.offset, sort: s.sort });
    s.total = r.total;
    s.hits = s.hits.concat(r.hits);
    s.offset = s.hits.length;
  } catch (e) { toast(e.message, true); }
  s.loading = false;
  if (S.page === "store") drawHits();
}
function drawHits() {
  const s = S.store, list = $("#st-list");
  if (!list) return;
  if (!s.hits.length) { list.innerHTML = `<div class="empty">${esc(s.loading ? "..." : t("noResults"))}</div>`; return; }
  list.innerHTML = s.hits.map(h => `<div class="item">
      ${h.icon ? `<img class="ic" loading="lazy" src="${esc(h.icon)}">` : `<div class="ph"></div>`}
      <div class="sp" style="min-width:0"><div class="nm">${esc(h.title)} <span class="dim" style="font-weight:400;font-size:12px">${esc(t("by"))} ${esc(h.author)} · ${fmtNum(h.downloads)} ${esc(t("downloads"))}</span></div>
      <div class="ds">${esc(h.description)}</div></div>
      <button class="btn pri sm" data-id="${esc(h.id)}">${esc(t("install"))}</button></div>`).join("") +
    (s.hits.length < s.total ? `<div style="text-align:center;margin-top:14px"><button class="btn" id="st-more">${esc(t("loadMore"))}</button></div>` : "");
  list.querySelectorAll("[data-id]").forEach(b => b.onclick = async () => {
    b.disabled = true; b.textContent = t("installing");
    try {
      const name = await call("installProject", { projectId: b.dataset.id, type: s.type, instanceId: s.instanceId });
      toast(t("installed") + name);
      b.textContent = "✓";
    } catch (e) { toast(e.message, true); b.disabled = false; b.textContent = t("install"); }
  });
  const more = $("#st-more");
  if (more) more.onclick = () => runSearch(false);
}

/* CLIENT (built-in mods) */
const MODULES = ["fps", "cps", "keystrokes", "coords", "direction", "clock", "nightvision", "sprint", "zoom", "freelook", "capes", "uncap"];
PAGES.client = async el => {
  const fabrics = S.instances.filter(i => i.loader === "fabric");
  if (!fabrics.length) {
    el.innerHTML = `<div class="card"><h2>${esc(t("clientTitle"))}</h2><p class="muted">${esc(t("clientSub"))}</p></div><div class="empty">${esc(t("clientNeedFabric"))}<br><br><button class="btn pri" id="nw">${esc(t("newInstance"))}</button></div>`;
    $("#nw").onclick = newInstanceDialog;
    return;
  }
  if (!fabrics.find(i => i.id === S.clientInst)) S.clientInst = (fabrics.find(i => i.id === S.settings.selectedInstance) || fabrics[0]).id;
  const inst = fabrics.find(i => i.id === S.clientInst);
  let info = { available: false, config: {} };
  try { info = await call("builtinInfo", { id: inst.id }); } catch (e) { toast(e.message, true); }
  const mods = (info.config && info.config.modules) || {};
  el.innerHTML = `<div class="card" style="margin-bottom:16px"><h2>${esc(t("clientTitle"))}</h2><p class="muted">${esc(t("clientSub"))}</p>
      <div class="row" style="margin-top:14px"><div style="width:280px"><select class="select" id="cl-inst">${fabrics.map(i => `<option value="${i.id}" ${i.id === inst.id ? "selected" : ""}>${esc(i.name)} · ${esc(i.mc)}</option>`).join("")}</select></div>
      <label class="row"><span class="sw"><input type="checkbox" id="cl-on" ${inst.builtinMods ? "checked" : ""} ${info.available ? "" : "disabled"}><span></span></span><span>${esc(t("builtin"))}</span></label></div>
      ${info.available ? "" : `<p style="color:var(--yel);margin-top:12px">${esc(t("clientNoJar"))}</p>`}</div>
    <div class="col">${MODULES.map(k => `<div class="mod"><div class="sp"><b>${esc(t("m_" + k))}</b><span class="muted">${esc(t("m_" + k + "_d"))}</span></div>
      <label class="sw"><input type="checkbox" data-m="${k}" ${(mods[k] ? mods[k].enabled !== false : defaultOn(k)) ? "checked" : ""}><span></span></label></div>`).join("")}</div>`;
  $("#cl-inst").onchange = e => { S.clientInst = e.target.value; render(); };
  $("#cl-on").onchange = e => safe(async () => { await call("updateInstance", { id: inst.id, builtin: e.target.checked }); await reloadState(); });
  el.querySelectorAll("[data-m]").forEach(c => c.onchange = () => safe(async () => {
    await call("saveBuiltinConfig", { id: inst.id, config: { modules: { [c.dataset.m]: { enabled: c.checked } } } });
  }));
};
const defaultOn = k => ["fps", "cps", "keystrokes", "coords", "zoom", "freelook", "capes", "uncap"].includes(k);

/* CAPES (local only) */
const CAPE_U = 8; // pixels per cape texture unit (64x32 canvas * 8 = 512x256)

/* Lay the picked area out on the Minecraft cape texture: outside face at (1,1) 10x16, inside face at (12,1), edges around. */
function buildCape(img, sx, sy, sw, sh) {
  const u = CAPE_U, c = document.createElement("canvas");
  c.width = 64 * u; c.height = 32 * u;
  const x = c.getContext("2d");
  x.imageSmoothingEnabled = true; x.imageSmoothingQuality = "high";
  x.drawImage(img, sx, sy, sw, sh, u, u, 10 * u, 16 * u);                       // outside (back of the player)
  x.save(); x.translate(22 * u, u); x.scale(-1, 1);
  x.drawImage(img, sx, sy, sw, sh, 0, 0, 10 * u, 16 * u); x.restore();          // inside, mirrored
  x.fillStyle = "rgba(0,0,0,.35)"; x.fillRect(12 * u, u, 10 * u, 16 * u);
  const ey = Math.max(1, sh / 16), ex = Math.max(1, sw / 10);
  x.drawImage(img, sx, sy, sw, ey, u, 0, 10 * u, u);                            // top
  x.drawImage(img, sx, sy + sh - ey, sw, ey, 11 * u, 0, 10 * u, u);             // bottom
  x.drawImage(img, sx, sy, ex, sh, 0, u, u, 16 * u);                            // left edge
  x.drawImage(img, sx + sw - ex, sy, ex, sh, 11 * u, u, u, 16 * u);             // right edge
  return c;
}

/* Elytra texture: wing box 10x20x2 at (22,0): outside face at (24,2), inner face at (36,2). Both wings use the same area. */
function buildElytra(img, sx, sy, sw, sh) {
  const u = CAPE_U, c = document.createElement("canvas");
  c.width = 64 * u; c.height = 32 * u;
  const x = c.getContext("2d");
  x.imageSmoothingEnabled = true; x.imageSmoothingQuality = "high";
  x.drawImage(img, sx, sy, sw, sh, 24 * u, 2 * u, 10 * u, 20 * u);              // outside
  x.save(); x.translate(46 * u, 2 * u); x.scale(-1, 1);
  x.drawImage(img, sx, sy, sw, sh, 0, 0, 10 * u, 20 * u); x.restore();          // inside, mirrored
  const ey = Math.max(1, sh / 20), ex = Math.max(1, sw / 10);
  x.drawImage(img, sx, sy, sw, ey, 24 * u, 0, 10 * u, 2 * u);                   // top
  x.drawImage(img, sx, sy + sh - ey, sw, ey, 34 * u, 0, 10 * u, 2 * u);         // bottom
  x.drawImage(img, sx, sy, ex, sh, 22 * u, 2 * u, 2 * u, 20 * u);               // left edge
  x.drawImage(img, sx + sw - ex, sy, ex, sh, 34 * u, 2 * u, 2 * u, 20 * u);     // right edge
  return c;
}

function cropDialog(img, kind) {
  const isEly = kind === "elytra";
  const MAXW = 520, MAXH = 340;
  const k = Math.min(MAXW / img.naturalWidth, MAXH / img.naturalHeight, 1);
  const dw = Math.round(img.naturalWidth * k), dh = Math.round(img.naturalHeight * k);
  const AR = isEly ? 10 / 20 : 10 / 16; // width / height of the cape back / wing outside
  const PW = 120, PH = Math.round(PW / AR);
  let cw = Math.min(dw, dh * AR); let ch = cw / AR;
  const box = { x: (dw - cw) / 2, y: (dh - ch) / 2, w: cw, h: ch };
  openModal(`<h3>${esc(t(isEly ? "cropTitleElytra" : "cropTitle"))}</h3><p class="muted" style="margin-bottom:12px">${esc(t("cropHint"))}</p>
    <div class="row" style="align-items:flex-start;gap:18px">
      <div id="cr-wrap" style="position:relative;width:${dw}px;height:${dh}px;overflow:hidden;border-radius:8px;background:#000;touch-action:none;flex:none">
        <img src="${img.src}" style="width:${dw}px;height:${dh}px;display:block;pointer-events:none">
        <div id="cr-box" style="position:absolute;border:2px solid var(--ac);box-shadow:0 0 0 9999px rgba(0,0,0,.55);cursor:move">
          <div id="cr-h" style="position:absolute;right:-7px;bottom:-7px;width:14px;height:14px;background:var(--ac);border-radius:3px;cursor:nwse-resize"></div></div></div>
      <div><div class="muted" style="font-size:12px;margin-bottom:6px">${esc(t("cropPreview"))}</div>
        <canvas id="cr-prev" width="${PW}" height="${PH}" style="background:var(--bg3);border-radius:8px;image-rendering:auto"></canvas></div></div>
    <div class="row" style="margin-top:16px;justify-content:flex-end"><button class="btn" id="cr-cancel">${esc(t("cancel"))}</button><button class="btn pri" id="cr-ok">${esc(t("save"))}</button></div>`);
  const el = $("#cr-box"), prev = $("#cr-prev");
  const sel = () => ({ sx: box.x / k, sy: box.y / k, sw: box.w / k, sh: box.h / k });
  const draw = () => {
    el.style.left = box.x + "px"; el.style.top = box.y + "px"; el.style.width = box.w + "px"; el.style.height = box.h + "px";
    const s = sel(), px = prev.getContext("2d");
    px.clearRect(0, 0, PW, PH);
    px.drawImage(img, s.sx, s.sy, s.sw, s.sh, 0, 0, PW, PH);
  };
  draw();
  let mode = null, sx0 = 0, sy0 = 0, b0 = null;
  const down = (e, m) => { e.preventDefault(); e.stopPropagation(); mode = m; sx0 = e.clientX; sy0 = e.clientY; b0 = { ...box }; e.target.setPointerCapture(e.pointerId); };
  el.onpointerdown = e => { if (e.target.id !== "cr-h") down(e, "move"); };
  $("#cr-h").onpointerdown = e => down(e, "size");
  const move = e => {
    if (!mode) return;
    const dx = e.clientX - sx0, dy = e.clientY - sy0;
    if (mode === "move") {
      box.x = Math.max(0, Math.min(dw - box.w, b0.x + dx));
      box.y = Math.max(0, Math.min(dh - box.h, b0.y + dy));
    } else {
      let w = b0.w + Math.max(dx, dy * AR);
      w = Math.max(30, Math.min(w, dw - b0.x, (dh - b0.y) * AR));
      box.w = w; box.h = w / AR;
    }
    draw();
  };
  el.onpointermove = move; $("#cr-h").onpointermove = move;
  el.onpointerup = () => { mode = null; }; $("#cr-h").onpointerup = () => { mode = null; };
  $("#cr-cancel").onclick = closeModal;
  $("#cr-ok").onclick = () => safe(async () => {
    const s = sel();
    const png = (isEly ? buildElytra : buildCape)(img, s.sx, s.sy, s.sw, s.sh).toDataURL("image/png");
    await call("capeSave", { data: png, kind });
    closeModal(); toast(t(isEly ? "elytraSaved" : "capeUploaded")); render();
  });
}

function pickCapeImage(kind) {
  const inp = document.createElement("input");
  inp.type = "file"; inp.accept = "image/*";
  inp.onchange = () => {
    const f = inp.files && inp.files[0];
    if (!f) return;
    const rd = new FileReader();
    rd.onload = () => {
      const img = new Image();
      img.onload = () => {
        // already a ready-made cape texture (64x32 or a multiple)? use it as is
        const w = img.naturalWidth, h = img.naturalHeight;
        if (f.type === "image/png" && w >= 64 && w <= 1024 && w % 64 === 0 && h * 2 === w) {
          safe(async () => { await call("capeSave", { data: rd.result, kind }); toast(t(kind === "elytra" ? "elytraSaved" : "capeUploaded")); render(); });
        } else cropDialog(img, kind);
      };
      img.onerror = () => toast(t("cropBad"), true);
      img.src = rd.result;
    };
    rd.readAsDataURL(f);
  };
  inp.click();
}

PAGES.capes = async el => {
  let info = { has: false, preview: "", hasElytra: false, elytraPreview: "" };
  try { info = await call("capeInfo"); } catch (e) { toast(e.message, true); }
  if (S.page !== "capes") return;
  const img = src => `<img src="${src}" style="width:256px;image-rendering:pixelated;border-radius:8px;background:var(--bg3)">`;
  el.innerHTML = `<div class="col" style="max-width:640px">
    <div class="card"><h2>${esc(t("capeTitle"))}</h2><p class="muted">${esc(t("capeSub"))}</p></div>
    <div class="card col">
      ${info.has ? img(info.preview) : `<div class="muted">${esc(t("capeNone"))}</div>`}
      <div class="row"><button class="btn pri" id="cp-up">${esc(t("capeChoose"))}</button>
      ${info.has ? `<button class="btn dng" id="cp-rm">${esc(t("capeRemove"))}</button>` : ""}</div></div>
    <div class="card"><h2>${esc(t("elytraTitle"))}</h2><p class="muted">${esc(t("elytraSub"))}</p></div>
    <div class="card col">
      ${info.hasElytra ? img(info.elytraPreview) : `<div class="muted">${esc(t("elytraNone"))}</div>`}
      <div class="row"><button class="btn pri" id="el-up">${esc(t("elytraChoose"))}</button>
      ${info.hasElytra ? `<button class="btn dng" id="el-rm">${esc(t("elytraRemove"))}</button>` : ""}</div></div></div>`;
  $("#cp-up").onclick = () => pickCapeImage("cape");
  $("#el-up").onclick = () => pickCapeImage("elytra");
  const rm = $("#cp-rm");
  if (rm) rm.onclick = () => safe(async () => { await call("capeRemove", { kind: "cape" }); toast(t("capeRemoved")); render(); });
  const er = $("#el-rm");
  if (er) er.onclick = () => safe(async () => { await call("capeRemove", { kind: "elytra" }); toast(t("elytraRemoved")); render(); });
};

/* ACCOUNTS */
PAGES.accounts = el => {
  el.innerHTML = `<div class="grid" style="grid-template-columns:1fr 1fr;align-items:start">
    <div>${S.accounts.length ? S.accounts.map(a => `<div class="item"><div class="av" style="width:44px;height:44px;border-radius:12px">${avatarHtml(a)}</div>
      <div class="sp"><div class="nm">${esc(a.name)} ${a.id === S.settings.selectedAccount ? '<span class="tag g">✓</span>' : ""}</div>
      <div class="muted" style="font-size:12px">${esc(t(a.type))}${a.expired ? ` · <span style="color:var(--red)">${esc(t("expired"))}</span>` : ""}</div></div>
      ${a.expired ? `<button class="btn sm pri" data-ms="1">${esc(t("msLogin"))}</button>` : a.id !== S.settings.selectedAccount ? `<button class="btn sm" data-use="${a.id}">${esc(t("use"))}</button>` : ""}
      <button class="btn sm dng" data-rm="${a.id}">${esc(t("remove"))}</button></div>`).join("") : `<div class="empty">${esc(t("noAccount"))}</div>`}</div>
    <div class="col">
      <div class="card col"><h2 style="margin:0">Microsoft</h2><button class="btn pri" id="ac-ms">${esc(t("msLogin"))}</button></div>
      <div class="card col"><h2 style="margin:0">${esc(t("addOffline"))}</h2>
        <input class="input" id="ac-name" placeholder="${esc(t("username"))}" maxlength="16">
        <button class="btn" id="ac-add">${esc(t("add"))}</button></div></div></div>`;
  const refresh = async () => { await reloadState(); render(); };
  $("#ac-ms").onclick = () => safe(async () => { await call("msLogin"); await refresh(); });
  el.querySelectorAll("[data-ms]").forEach(b => b.onclick = () => safe(async () => { await call("msLogin"); await refresh(); }));
  $("#ac-add").onclick = () => safe(async () => { await call("addOffline", { name: $("#ac-name").value }); await refresh(); });
  $("#ac-name").onkeydown = e => { if (e.key === "Enter") $("#ac-add").click(); };
  el.querySelectorAll("[data-use]").forEach(b => b.onclick = () => safe(async () => { await call("selectAccount", { id: b.dataset.use }); await refresh(); }));
  el.querySelectorAll("[data-rm]").forEach(b => b.onclick = () => safe(async () => { await call("removeAccount", { id: b.dataset.rm }); await refresh(); }));
};

/* SETTINGS */
PAGES.settings = el => {
  const s = S.settings;
  const maxRam = Math.max(2048, Math.floor(S.totalRamMb / 512) * 512);
  el.innerHTML = `<div class="col" style="max-width:640px">
    <div class="card col"><h2 style="margin:0">${esc(t("language"))}</h2>
      <div class="tabs" style="margin:0"><div class="tab ${lang === "en" ? "on" : ""}" data-l="en">English</div><div class="tab ${lang === "zh" ? "on" : ""}" data-l="zh">繁體中文</div></div></div>
    <div class="card col"><h2 style="margin:0">${esc(t("memory"))}: <span id="rv">${s.ramMb}</span> MB</h2>
      <input type="range" id="ram" min="1024" max="${maxRam}" step="256" value="${Math.min(s.ramMb, maxRam)}"></div>
    <div class="card col"><h2 style="margin:0">${esc(t("jvm"))}</h2><input class="input" id="jvm" value="${esc(s.jvmArgs)}" placeholder="-XX:+UseG1GC"></div>
    <div class="card"><label class="row"><span class="sw"><input type="checkbox" id="hide" ${s.hideLauncherOnPlay ? "checked" : ""}><span></span></span><span>${esc(t("hideOnPlay"))}</span></label></div>
    <div class="card col"><label class="row"><span class="sw"><input type="checkbox" id="fpsb" ${s.fpsBoost !== false ? "checked" : ""}><span></span></span><span><b>${esc(t("fpsBoost"))}</b></span></label><span class="muted" style="font-size:12px">${esc(t("fpsBoostD"))}</span></div>
    <div class="card"><label class="row"><span class="sw"><input type="checkbox" id="autoupd" ${s.autoCheckUpdates !== false ? "checked" : ""}><span></span></span><span>${esc(t("autoCheck"))}</span></label></div>
    <div class="card col"><h2 style="margin:0">${esc(t("updates"))}</h2>
      <div class="muted">${esc(t("version"))}: <b>${esc(S.update && S.update.current ? S.update.current : (S.version || t("unknownVer")))}</b></div>
      <div class="row"><button class="btn" id="updchk">${esc(t("checkNow"))}</button><span id="updmsg" class="muted">${updMsg()}</span></div>
      ${S.update && S.update.available ? `<div><button class="btn pri" id="upddl">${esc(t("download"))}</button></div>` : ""}</div>
    <div class="dim">${esc(t("about"))}</div></div>`;
  el.querySelectorAll("[data-l]").forEach(b => b.onclick = () => safe(async () => {
    await call("saveSettings", { language: b.dataset.l });
    lang = b.dataset.l; s.language = lang; render();
  }));
  $("#ram").oninput = e => { $("#rv").textContent = e.target.value; };
  $("#ram").onchange = e => safe(async () => { s.ramMb = parseInt(e.target.value); await call("saveSettings", { ramMb: s.ramMb }); });
  $("#jvm").onchange = e => safe(async () => { s.jvmArgs = e.target.value; await call("saveSettings", { jvmArgs: s.jvmArgs }); });
  $("#fpsb").onchange = e => safe(async () => { s.fpsBoost = e.target.checked; await call("saveSettings", { fpsBoost: e.target.checked }); });
  $("#autoupd").onchange = e => safe(async () => { s.autoCheckUpdates = e.target.checked; await call("saveSettings", { autoCheckUpdates: e.target.checked }); });
  $("#updchk").onclick = async () => {
    $("#updmsg").textContent = t("checking");
    await runUpdateCheck();
    if (S.page === "settings") render();
  };
  const dl = $("#upddl"); if (dl) dl.onclick = startUpdate;
  $("#hide").onchange = e => safe(async () => { s.hideLauncherOnPlay = e.target.checked; await call("saveSettings", { hideLauncherOnPlay: e.target.checked }); });
};

/* ───────────── custom title bar ───────────── */
(function wireTitleBar() {
  const tb = $("#titlebar");
  tb.addEventListener("mousedown", e => {
    if (e.button !== 0 || e.target.closest(".nodrag")) return;
    call("winDrag").catch(() => {});
  });
  tb.addEventListener("dblclick", e => {
    if (e.target.closest(".nodrag")) return;
    call("winMax").catch(() => {});
  });
  $("#wmin").onclick = () => call("winMin").catch(() => {});
  $("#wmax").onclick = () => call("winMax").catch(() => {});
  $("#wclose").onclick = () => call("winClose").catch(() => {});
  document.querySelectorAll(".rz").forEach(el => el.addEventListener("mousedown", e => {
    if (e.button !== 0) return;
    e.preventDefault();
    call("winResize", { edge: el.dataset.e }).catch(() => {});
  }));
})();

/* ───────────── boot ───────────── */
function updMsg() {
  const u = S.update;
  if (!u) return "";
  if (u.error) return esc(t("updateFail") + u.error);
  return esc(u.available ? t("updateAvail") : t("upToDate"));
}

async function runUpdateCheck() {
  try { S.update = await call("checkUpdate"); } catch (e) { S.update = { error: e.message, available: false }; }
  return S.update;
}

function showUpdateDialog() {
  const u = S.update;
  openModal(`<h3>${esc(t("updateTitle"))}</h3>
    <p class="muted" style="margin-bottom:16px">${esc(t("updateText").replace("{0}", u.current || t("unknownVer")).replace("{1}", u.latest))}</p>
    <div class="row"><button class="btn sm" id="up-page">${esc(t("releasePage"))}</button><div class="sp"></div><button class="btn" id="up-later">${esc(t("later"))}</button><button class="btn pri" id="up-dl">${esc(t("download"))}</button></div>`);
  $("#up-later").onclick = closeModal;
  $("#up-page").onclick = () => call("openUrl", { url: u.url });
  $("#up-dl").onclick = startUpdate;
}

async function startUpdate() {
  openModal(`<h3>${esc(t("updating"))}</h3><p class="muted" id="uptext" style="margin-bottom:14px">...</p><div class="bar"><i id="upbar"></i></div>`);
  try {
    await call("applyUpdate");
    const tx = $("#uptext"); if (tx) tx.textContent = t("restarting");
  } catch (e) {
    closeModal();
    toast(e.message, true);
  }
}

(async function () {
  await safe(reloadState);
  render();
  // 啟動時自動檢查一次更新 (可在設定關閉)
  if (S.settings.autoCheckUpdates !== false) {
    const u = await runUpdateCheck();
    if (u.available) { render(); showUpdateDialog(); }
  }
})();

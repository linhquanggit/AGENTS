/* office3d.js — Văn phòng 3D của Musude (Three.js r147, chạy local, chỉ đọc).
   Dữ liệu: /api/plans, /api/state, /api/usage, /api/trash (cùng origin).
   Nhân viên = mission (plan). Thông tin trên thanh trạng thái của Musude
   (muse-cli, poll, 5 giờ, Tuần, Auto, Chat Muse, PING, thùng rác, task)
   được dựng thành đồ trang trí trong phòng. Không có nút ghi/xoá nào ở đây. */
(function () {
  'use strict';
  var EMBED = false; try { EMBED = window.parent !== window; } catch (e) {}
  // Chỉ có một cổng vào 3D: bấm tên Musude trên header. Mở thẳng trang này thì chuyển về app chính ở chế độ 3D.
  if (!EMBED) { try { localStorage.setItem('mb_3d', '1'); } catch (e) {} location.replace('/'); return; }
  document.body.classList.toggle('embed', EMBED);
  var $ = function (id) { return document.getElementById(id); };
  function esc(s) { return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;'); }
  function api(path) { return fetch(path).then(function (r) { return r.json().then(function (j) { if (!r.ok || j.error) throw new Error(j.error || r.status); return j; }); }); }
  function rnd(a, b) { return a + Math.random() * (b - a); }

  /* ---------- dữ liệu ---------- */
  var PLANS = [], STATE = { config: {}, runtime: {} }, USAGE = null, TRASH = 0;
  var SHOW_ALL = false, SELECTED = null, sig = '', ready = false;

  function counts(p) { var n = p.tasks.length, d = p.tasks.filter(function (t) { return t.status === 'done'; }).length; return { n: n, d: d }; }
  function planCat(p) {
    var c = counts(p);
    if (c.n && c.d === c.n) return 'done';
    if (p.tasks.some(function (t) { return t.status === 'blocked'; }) || (p.term && p.term.need)) return 'blocked';
    if ((p.muse_wait || []).length) return 'waiting';
    if ((p.term && p.term.alive) || p.tasks.some(function (t) { return t.status === 'doing'; })) return 'doing';
    return 'idle';
  }
  var CAT_TEXT = { done: 'Đã xong', blocked: 'Cần xem', waiting: 'Chờ Muse', doing: 'Đang làm', idle: 'Chưa chạy' };
  var CAT_COLOR = { done: '#3ecf8e', blocked: '#f36c64', waiting: '#ad92ff', doing: '#f2a650', idle: '#8d919c', sleep: '#7a6bd0', game: '#3b82f6', tea: '#e0679b', wc: '#2f8fb8', watched: '#e0763a' };
  function fmtLeft(iso) {
    if (!iso) return ''; var ms = new Date(iso) - Date.now(); if (isNaN(ms)) return ''; if (ms <= 0) return '0p';
    var m = Math.floor(ms / 60000), h = Math.floor(m / 60), d = Math.floor(h / 24);
    if (d >= 1) return d + 'n' + (h % 24) + 'g';
    if (h >= 1) return h + 'g' + String(m % 60).padStart(2, '0');
    return m + 'p';
  }
  function groupKey(p) { return (p.id.split('-')[0] || 'khac').toLowerCase(); }

  /* ---------- renderer ---------- */
  var wrap = $('stage');
  var renderer;
  try { renderer = new THREE.WebGLRenderer({ antialias: true }); }
  catch (e) { wrap.innerHTML = '<p class="nogl">Trình duyệt này không bật được WebGL nên không vẽ được phòng 3D.</p>'; return; }
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.shadowMap.enabled = true; renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  wrap.appendChild(renderer.domElement);
  var scene = new THREE.Scene(); scene.background = new THREE.Color('#c9d1e3');
  var camera = new THREE.PerspectiveCamera(38, 1, 0.5, 500);
  var controls = new THREE.OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true; controls.dampingFactor = 0.08; controls.screenSpacePanning = false;
  controls.maxPolarAngle = 1.38; controls.minPolarAngle = 0.3; controls.minAzimuthAngle = -1.0; controls.maxAzimuthAngle = 1.0;
  controls.minDistance = 8; controls.maxDistance = 120;
  scene.add(new THREE.HemisphereLight(0xffffff, 0x8a93a8, 0.72));
  var sun = new THREE.DirectionalLight(0xfff3e0, 0.6); sun.castShadow = true; sun.shadow.mapSize.set(3072, 3072); sun.shadow.bias = -0.0004; scene.add(sun); scene.add(sun.target);
  var world = new THREE.Group(); scene.add(world);

  /* ---------- tiện ích dựng hình ---------- */
  var matCache = {};
  function M(c, o) { if (!o && matCache[c]) return matCache[c]; var m = new THREE.MeshLambertMaterial(Object.assign({ color: c }, o || {})); if (!o) matCache[c] = m; return m; }
  function bx(par, w, h, d, c, x, y, z, noShadow) {
    var m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), (c && c.isMaterial) ? c : M(c));
    m.position.set(x, y, z); if (!noShadow) { m.castShadow = true; m.receiveShadow = true; } par.add(m); return m;
  }
  function cyl(par, rt, rb, h, c, x, y, z, seg) { var m = new THREE.Mesh(new THREE.CylinderGeometry(rt, rb, h, seg || 16), M(c)); m.position.set(x, y, z); m.castShadow = true; m.receiveShadow = true; par.add(m); return m; }
  function mkTex(w, h, fn) {
    var c = document.createElement('canvas'); c.width = w; c.height = h; var x = c.getContext('2d'); fn(x, w, h);
    var t = new THREE.CanvasTexture(c); t.anisotropy = 4; t.userData = { ctx: x, w: w, h: h }; return t;
  }
  function redraw(t, fn) { fn(t.userData.ctx, t.userData.w, t.userData.h); t.needsUpdate = true; }
  function rr(x, px, py, w, h, r) { x.beginPath(); x.moveTo(px + r, py); x.arcTo(px + w, py, px + w, py + h, r); x.arcTo(px + w, py + h, px, py + h, r); x.arcTo(px, py + h, px, py, r); x.arcTo(px, py, px + w, py, r); x.closePath(); }
  /* ---------- neon ---------- */
  var glowTexCache = null;
  function glowTex() {
    if (glowTexCache) return glowTexCache; var c = document.createElement('canvas'); c.width = c.height = 64; var x = c.getContext('2d'), g = x.createRadialGradient(32, 32, 0, 32, 32, 32);
    g.addColorStop(0, 'rgba(255,255,255,1)'); g.addColorStop(0.25, 'rgba(255,255,255,.45)'); g.addColorStop(1, 'rgba(255,255,255,0)'); x.fillStyle = g; x.fillRect(0, 0, 64, 64);
    glowTexCache = new THREE.CanvasTexture(c); return glowTexCache;
  }
  function glow(par, x, y, z, size, color, op) {
    var s = new THREE.Sprite(new THREE.SpriteMaterial({ map: glowTex(), color: color, transparent: true, opacity: op || 0.55, blending: THREE.AdditiveBlending, depthWrite: false }));
    s.position.set(x, y, z); s.scale.set(size, size, 1); par.add(s); return s;
  }
  function neon(par, w, h, d, c, x, y, z, gs) {
    var m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), new THREE.MeshBasicMaterial({ color: c })); m.position.set(x, y, z); par.add(m);
    if (gs) { var n = Math.max(1, Math.round(Math.max(w, d) / gs)); for (var i = 0; i < n; i++) { var f = n === 1 ? 0 : i / (n - 1) - 0.5; glow(par, x + (w >= d ? f * w : 0), y, z + (w >= d ? 0 : f * d), gs * 1.8, c, 0.4); } }
    return m;
  }
  function plight(par, c, i, dist, x, y, z) { var l = new THREE.PointLight(c, i, dist, 1.6); l.position.set(x, y, z); par.add(l); return l; }
  function neonText(par, text, color, w, h, x, y, z, ry) {
    var t = mkTex(512, Math.round(512 * h / w), function (cx, cw, ch) {
      cx.clearRect(0, 0, cw, ch); cx.textAlign = 'center'; cx.textBaseline = 'middle'; cx.font = '800 ' + Math.round(ch * 0.62) + 'px ' + FONT;
      cx.shadowColor = color; cx.shadowBlur = 24; cx.fillStyle = color; for (var i = 0; i < 3; i++) cx.fillText(text, cw / 2, ch / 2 + 2); cx.shadowBlur = 0; cx.fillStyle = '#ffffff'; cx.globalAlpha = 0.85; cx.fillText(text, cw / 2, ch / 2 + 2);
    });
    var m = new THREE.Mesh(new THREE.PlaneGeometry(w, h), new THREE.MeshBasicMaterial({ map: t, transparent: true, depthWrite: false })); m.position.set(x, y, z); m.rotation.y = ry || 0; par.add(m);
    glow(par, x, y, z + (ry ? 0 : 0.05), Math.max(w, h) * 0.9, color, 0.18); return m;
  }
  var FONT = '-apple-system, "SF Pro Display", "Segoe UI", Roboto, sans-serif';
  function fit(x, text, maxW, size, weight) { do { x.font = (weight || 700) + ' ' + size + 'px ' + FONT; size -= 2; } while (x.measureText(text).width > maxW && size > 10); }
  function panel(par, w, h, px, py, pz, ry, draw, res) {
    var g = new THREE.Group(); g.position.set(px, py, pz); g.rotation.y = ry || 0; par.add(g);
    bx(g, w + 0.28, h + 0.28, 0.16, '#222838', 0, 0, 0);
    var t = mkTex(res || 512, Math.round((res || 512) * h / w), draw);
    var p = new THREE.Mesh(new THREE.PlaneGeometry(w, h), new THREE.MeshBasicMaterial({ map: t })); p.position.z = 0.09; g.add(p);
    return { group: g, tex: t, draw: draw, redraw: function () { redraw(t, draw); } };
  }
  function labelSprite(text, color, scale, fg) {
    var t = mkTex(512, 128, function (x, w, h) {
      x.clearRect(0, 0, w, h); rr(x, 6, 14, w - 12, h - 28, 36); x.fillStyle = color; x.fill();
      x.fillStyle = fg || '#fff'; x.textAlign = 'center'; x.textBaseline = 'middle'; fit(x, text, w - 80, 56, 800); x.fillText(text, w / 2, h / 2 + 2);
    });
    var s = new THREE.Sprite(new THREE.SpriteMaterial({ map: t, transparent: true, depthWrite: false }));
    s.scale.set(4 * scale, scale, 1); return s;
  }

  /* ---------- khuôn mặt + nhân vật kiểu blocky ---------- */
  var faceCache = {};
  function faceTex(kind, skin) {
    var k = kind + skin; if (faceCache[k]) return faceCache[k];
    var t = mkTex(128, 128, function (x, w, h) {
      x.fillStyle = skin; x.fillRect(0, 0, w, h);
      x.fillStyle = '#e98a8a'; x.globalAlpha = 0.35; x.beginPath(); x.ellipse(28, 82, 12, 7, 0, 0, 7); x.ellipse(100, 82, 12, 7, 0, 0, 7); x.fill(); x.globalAlpha = 1;
      x.fillStyle = '#23262e'; x.strokeStyle = '#23262e'; x.lineWidth = 6; x.lineCap = 'round';
      if (kind === 'happy') { x.beginPath(); x.arc(40, 66, 11, Math.PI * 1.1, Math.PI * 1.9); x.stroke(); x.beginPath(); x.arc(88, 66, 11, Math.PI * 1.1, Math.PI * 1.9); x.stroke(); }
      else if (kind === 'sleep') { x.beginPath(); x.moveTo(28, 66); x.lineTo(52, 66); x.moveTo(76, 66); x.lineTo(100, 66); x.stroke(); }
      else { x.beginPath(); x.ellipse(40, 64, 8, 12, 0, 0, 7); x.ellipse(88, 64, 8, 12, 0, 0, 7); x.fill(); x.fillStyle = '#fff'; x.beginPath(); x.arc(43, 59, 3, 0, 7); x.arc(91, 59, 3, 0, 7); x.fill(); }
      x.strokeStyle = '#6b3b3b'; x.lineWidth = 5;
      x.beginPath();
      if (kind === 'worry') { x.arc(64, 106, 12, Math.PI * 1.15, Math.PI * 1.85); }
      else if (kind === 'think') { x.moveTo(52, 98); x.lineTo(76, 98); }
      else if (kind === 'sleep') { x.arc(64, 96, 6, 0, 7); }
      else { x.arc(64, 90, 17, Math.PI * 0.15, Math.PI * 0.85); }
      x.stroke();
      if (kind === 'worry') { x.strokeStyle = '#23262e'; x.lineWidth = 4; x.beginPath(); x.moveTo(26, 40); x.lineTo(52, 46); x.moveTo(102, 40); x.lineTo(76, 46); x.stroke(); }
    });
    faceCache[k] = t; return t;
  }
  function makeChar(o) {
    var skin = o.skin || '#f3cfa8', g = new THREE.Group(), root = new THREE.Group(); g.add(root);
    function limb(px, py, w, h, c, hand) {
      var p = new THREE.Group(); p.position.set(px, py, 0); root.add(p);
      bx(p, w, h, w * 1.05, c, 0, -h / 2, 0); if (hand) bx(p, w * 1.05, 0.14, w * 1.1, skin, 0, -h - 0.02, 0); return p;
    }
    var legL = limb(-0.17, 0, 0.3, 0.52, o.pants || '#2f3647'), legR = limb(0.17, 0, 0.3, 0.52, o.pants || '#2f3647');
    bx(legL, 0.32, 0.12, 0.38, o.shoes || '#e9e9ee', 0, -0.58, 0.04); bx(legR, 0.32, 0.12, 0.38, o.shoes || '#e9e9ee', 0, -0.58, 0.04);
    var torso = bx(root, 0.66, 0.68, 0.38, o.shirt, 0, 0.34, 0);
    if (o.tie) bx(root, 0.1, 0.46, 0.04, o.tie, 0, 0.4, 0.21);
    if (o.collar) bx(root, 0.42, 0.1, 0.4, o.collar, 0, 0.66, 0);
    var armL = limb(-0.45, 0.62, 0.22, 0.56, o.sleeve || o.shirt, true), armR = limb(0.45, 0.62, 0.22, 0.56, o.sleeve || o.shirt, true);
    var head = new THREE.Group(); head.position.set(0, 0.7, 0); root.add(head);
    var faceMat = new THREE.MeshLambertMaterial({ map: faceTex('smile', skin) }), sk = M(skin), hairM = M(o.hair || '#3a2a22');
    var hm = new THREE.Mesh(new THREE.BoxGeometry(0.84, 0.8, 0.8), [sk, sk, hairM, sk, faceMat, hairM]); hm.position.y = 0.42; hm.castShadow = true; head.add(hm);
    bx(head, 0.9, 0.22, 0.86, o.hair || '#3a2a22', 0, 0.78, -0.01);
    bx(head, 0.9, 0.5, 0.2, o.hair || '#3a2a22', 0, 0.55, -0.4);
    bx(head, 0.08, 0.3, 0.7, o.hair || '#3a2a22', -0.44, 0.58, 0); bx(head, 0.08, 0.3, 0.7, o.hair || '#3a2a22', 0.44, 0.58, 0);
    if (o.crown) {
      var cr = new THREE.Group(); cr.position.set(0, 0.9, 0); head.add(cr);
      bx(cr, 0.7, 0.14, 0.7, '#ffd34d', 0, 0, 0);
      [-0.27, 0, 0.27].forEach(function (x) { bx(cr, 0.14, 0.24, 0.14, '#ffd34d', x, 0.18, 0.26); bx(cr, 0.14, 0.24, 0.14, '#ffd34d', x, 0.18, -0.26); });
    }
    if (o.glasses) { bx(head, 0.8, 0.05, 0.04, '#222', 0, 0.44, 0.41); }
    var c = { group: g, root: root, head: head, armL: armL, armR: armR, legL: legL, legR: legR, faceMat: faceMat, skin: skin, face: 'smile',
      seated: !!o.seated, baseY: o.seated ? 0.62 : 0.62, cur: { aLx: 0, aRx: 0, aLz: 0, aRz: 0, hy: 0, hx: 0, jump: 0 },
      status: 'idle', ev: null, nextEv: rnd(3, 12), phase: Math.random() * 6, walk: null };
    root.position.y = c.baseY;
    g.traverse(function (m) { if (m.isMesh) m.userData.charRef = c; });
    return c;
  }
  function setFace(c, kind) { if (c.face === kind) return; c.face = kind; c.faceMat.map = faceTex(kind, c.skin); c.faceMat.needsUpdate = true; }

  var HAIRS = ['#2b2020', '#4a3224', '#1f2a44', '#6b3d23', '#111', '#8a5a2b', '#3a2a5a'];
  var SKINS = ['#f3cfa8', '#e6b88f', '#d9a577', '#f7dcc0', '#c58f64'];
  var chars = [], anim = [], boards = {}, walls = [], pickMeshes = [], bubbleOf = {};

  /* ---------- bong bóng trạng thái trên đầu ---------- */
  function bubbleTex(cat) {
    return mkTex(128, 128, function (x, w, h) {
      x.clearRect(0, 0, w, h); x.fillStyle = CAT_COLOR[cat]; x.strokeStyle = 'rgba(0,0,0,.25)'; x.lineWidth = 3;
      rr(x, 10, 8, 108, 86, 26); x.fill(); x.stroke(); x.beginPath(); x.moveTo(52, 92); x.lineTo(64, 116); x.lineTo(76, 92); x.fill();
      x.fillStyle = '#fff'; x.strokeStyle = '#fff'; x.lineWidth = 11; x.lineCap = 'round'; x.lineJoin = 'round'; x.textAlign = 'center'; x.textBaseline = 'middle';
      if (cat === 'done') { x.beginPath(); x.moveTo(36, 52); x.lineTo(55, 70); x.lineTo(92, 32); x.stroke(); }
      else if (cat === 'blocked') { x.font = '800 70px ' + FONT; x.fillText('!', 64, 54); }
      else if (cat === 'waiting') { [38, 64, 90].forEach(function (cx) { x.beginPath(); x.arc(cx, 52, 8, 0, 7); x.fill(); }); }
      else if (cat === 'doing') { x.font = '800 44px ' + FONT; x.fillText('</>', 64, 52); }
      else if (cat === 'game') { x.font = '800 40px ' + FONT; x.fillText('GG', 64, 52); }
      else if (cat === 'tea') { x.beginPath(); x.moveTo(42, 30); x.lineTo(86, 30); x.lineTo(78, 72); x.lineTo(50, 72); x.closePath(); x.fill(); x.beginPath(); x.moveTo(70, 30); x.lineTo(80, 14); x.stroke(); }
      else if (cat === 'wc') { x.font = '800 40px ' + FONT; x.fillText('WC', 64, 52); }
      else if (cat === 'watched') { x.font = '800 48px ' + FONT; x.fillText('!?', 64, 52); }
      else { x.font = '800 44px ' + FONT; x.fillText('zZ', 64, 52); }
    });
  }
  var bubbleCache = {};
  function bubbleSprite(cat) {
    if (!bubbleCache[cat]) bubbleCache[cat] = bubbleTex(cat);
    var s = new THREE.Sprite(new THREE.SpriteMaterial({ map: bubbleCache[cat], transparent: true, depthWrite: false, depthTest: false }));
    s.scale.set(0.95, 0.95, 1); s.renderOrder = 10; return s;
  }

  /* ---------- đồ nội thất ---------- */
  function makeDesk(par, x, z, plan, team) {
    var g = new THREE.Group(); g.position.set(x, 0, z); par.add(g);
    bx(g, 3.1, 0.1, 1.25, '#f1ece3', 0, 0.95, 0);
    [[-1.45, -0.5], [1.45, -0.5], [-1.45, 0.5], [1.45, 0.5]].forEach(function (p) { bx(g, 0.1, 0.9, 0.1, '#59606f', p[0], 0.45, p[1]); });
    bx(g, 2.9, 0.5, 0.06, '#59606f', 0, 0.62, -0.55);
    // bảng tên đứng trên bàn, quay ra camera
    var plate = new THREE.Mesh(new THREE.PlaneGeometry(1.3, 0.4), new THREE.MeshBasicMaterial({ map: mkTex(512, 158, function () {}), transparent: true }));
    plate.position.set(-1.0, 1.2, 0.42); g.add(plate); bx(g, 1.34, 0.04, 0.2, '#222838', -1.0, 1.0, 0.42, true);
    // hai màn hình máy tính hiển thị code, quay mặt ra camera
    var scr = [];
    [-0.05, 0.98].forEach(function (mx, i) {
      var m = new THREE.Group(); m.position.set(mx, 1.0, -0.3); m.rotation.y = (i ? -0.22 : 0.22); g.add(m);
      bx(m, 0.14, 0.3, 0.14, '#3a4256', 0, 0.18, -0.05); bx(m, 0.5, 0.04, 0.3, '#3a4256', 0, 0.02, -0.05);
      bx(m, 1.0, 0.64, 0.06, '#1c2130', 0, 0.62, 0);
      var t = mkTex(256, 160, function () {});
      var p = new THREE.Mesh(new THREE.PlaneGeometry(0.92, 0.56), new THREE.MeshBasicMaterial({ map: t })); p.position.set(0, 0.62, 0.035); m.add(p); scr.push(t);
    });
    bx(g, 0.9, 0.03, 0.3, '#2b3040', 0.45, 0.99, 0.22, true); bx(g, 0.16, 0.03, 0.22, '#2b3040', 1.1, 0.99, 0.22, true);
    bx(g, 0.26, 0.22, 0.26, '#cbd2e0', 1.3, 1.1, -0.1); bx(g, 0.2, 0.14, 0.2, team, 1.3, 1.29, -0.1);
    // ghế: nhân viên quay lưng về phía camera
    bx(g, 0.9, 0.1, 0.9, '#3b4256', 0.45, 0.5, 0.95); bx(g, 0.9, 0.85, 0.12, '#3b4256', 0.45, 0.98, 1.43); cyl(g, 0.06, 0.06, 0.45, '#222', 0.45, 0.25, 0.95); cyl(g, 0.4, 0.4, 0.06, '#222', 0.45, 0.04, 0.95, 6);
    return { group: g, plate: plate, screens: scr };
  }
  function seeded(n) { var v = Math.sin(n * 127.1 + 311.7) * 43758.5453; return v - Math.floor(v); }
  var CODE_COL = ['#c792ea', '#82aaff', '#c3e88d', '#f78c6c', '#89ddff', '#d6dbe8'];
  function drawCode(x, w, h, cat, id, off, t) {
    x.fillStyle = '#0d111c'; x.fillRect(0, 0, w, h); x.fillStyle = '#1a2133'; x.fillRect(0, 0, w, 20); x.fillStyle = '#7b8cff'; x.fillRect(8, 6, 40, 8);
    x.fillStyle = '#8d97ad'; x.font = '700 11px ui-monospace, Menlo, monospace'; x.textAlign = 'left'; x.fillText(String(id).slice(0, 22), 56, 15);
    var dim = cat === 'idle' ? 0.25 : cat === 'done' ? 0.55 : 1;
    x.globalAlpha = dim;
    for (var i = 0; i < 10; i++) {
      var ln = Math.floor(off) + i, y = 30 + i * 12, ind = Math.floor(seeded(ln) * 3) * 14, px = 26 + ind, segs = 2 + Math.floor(seeded(ln + 7) * 3);
      x.fillStyle = '#4a5370'; x.fillRect(8, y, 10, 6);
      for (var s = 0; s < segs; s++) { var sw = 14 + Math.floor(seeded(ln * 3 + s) * 46); x.fillStyle = CODE_COL[Math.floor(seeded(ln * 5 + s + 2) * CODE_COL.length)]; x.fillRect(px, y, sw, 6); px += sw + 6; }
    }
    x.globalAlpha = 1;
    if (cat === 'doing' && Math.floor(t * 2) % 2) { x.fillStyle = '#fff'; x.fillRect(26 + 60, 30 + 9 * 12, 3, 8); }
    if (cat === 'blocked') { x.fillStyle = 'rgba(243,108,100,.92)'; x.fillRect(0, h - 52, w, 52); x.fillStyle = '#fff'; x.font = '800 24px ' + FONT; x.fillText('ERROR', 14, h - 20); x.font = '700 12px ui-monospace,Menlo,monospace'; x.fillText('cần xem ngay', 120, h - 20); }
    else if (cat === 'waiting') { x.fillStyle = 'rgba(13,17,28,.78)'; x.fillRect(0, 20, w, h); x.strokeStyle = '#ad92ff'; x.lineWidth = 6; x.lineCap = 'round'; x.beginPath(); x.arc(w / 2, h / 2 - 8, 22, t * 4, t * 4 + 4.2); x.stroke(); x.fillStyle = '#ad92ff'; x.font = '700 14px ' + FONT; x.textAlign = 'center'; x.fillText('chờ Muse…', w / 2, h / 2 + 44); x.textAlign = 'left'; }
    else if (cat === 'done') { x.fillStyle = 'rgba(31,107,74,.92)'; x.fillRect(0, h - 40, w, 40); x.fillStyle = '#fff'; x.font = '800 20px ' + FONT; x.fillText('✓ PASSED', 14, h - 14); }
    else if (cat === 'idle') { x.fillStyle = '#5b6275'; x.font = '800 26px ' + FONT; x.textAlign = 'center'; x.fillText('zZ', w / 2, h / 2 + 8); x.textAlign = 'left'; }
  }
  function drawPlate(x, w, h, plan, cat, teamColor) {
    var c = counts(plan), pct = c.n ? c.d / c.n : 0;
    x.clearRect(0, 0, w, h); rr(x, 4, 4, w - 8, h - 8, 22); x.fillStyle = '#171b27'; x.fill();
    x.fillStyle = CAT_COLOR[cat]; rr(x, 4, 4, 18, h - 8, 9); x.fill();
    x.fillStyle = '#fff'; x.textAlign = 'left'; x.textBaseline = 'alphabetic'; fit(x, plan.id, w - 190, 52, 800); x.fillText(plan.id, 38, 66);
    x.fillStyle = CAT_COLOR[cat]; x.font = '700 30px ' + FONT; x.textAlign = 'right'; x.fillText(CAT_TEXT[cat], w - 24, 58);
    x.fillStyle = '#2d3447'; rr(x, 38, 92, w - 76, 26, 13); x.fill();
    if (pct > 0) { x.fillStyle = cat === 'blocked' ? '#f36c64' : '#3ecf8e'; rr(x, 38, 92, Math.max(26, (w - 76) * pct), 26, 13); x.fill(); }
    x.fillStyle = '#cfd3df'; x.font = '700 22px ' + FONT; x.textAlign = 'left'; x.fillText(c.d + '/' + c.n + ' task', 44, 138 - 4);
  }
  function plant(par, x, z, s) {
    var g = new THREE.Group(); g.position.set(x, 0, z); g.scale.setScalar(s || 1); par.add(g);
    cyl(g, 0.38, 0.3, 0.55, '#c6784f', 0, 0.28, 0); cyl(g, 0.34, 0.34, 0.06, '#4a3426', 0, 0.55, 0);
    [[0, 1.15, 0, 0.5], [0.25, 0.95, 0.1, 0.36], [-0.25, 0.92, -0.08, 0.38], [0.05, 1.5, 0.05, 0.32]].forEach(function (p) {
      var m = new THREE.Mesh(new THREE.IcosahedronGeometry(p[3], 0), M('#3fa66a')); m.position.set(p[0], p[1], p[2]); m.castShadow = true; g.add(m);
    });
  }
  function boardClock(par, x, y, z, ry) {
    var g = new THREE.Group(); g.position.set(x, y, z); g.rotation.y = ry; par.add(g);
    var face = new THREE.Mesh(new THREE.CylinderGeometry(1.05, 1.05, 0.16, 40), M('#f4f1ea')); face.rotation.x = Math.PI / 2; g.add(face);
    var rim = new THREE.Mesh(new THREE.TorusGeometry(1.05, 0.08, 8, 40), M('#2c3240')); g.add(rim);
    for (var i = 0; i < 12; i++) { var a = i / 12 * Math.PI * 2; bx(g, 0.06, i % 3 ? 0.12 : 0.2, 0.04, '#2c3240', Math.sin(a) * 0.86, Math.cos(a) * 0.86, 0.1, true).rotation.z = -a; }
    var hh = bx(g, 0.07, 0.5, 0.04, '#2c3240', 0, 0.25, 0.12, true), mh = bx(g, 0.05, 0.78, 0.04, '#2c3240', 0, 0.39, 0.15, true), sh = bx(g, 0.02, 0.85, 0.02, '#e0483e', 0, 0.42, 0.18, true);
    [hh, mh, sh].forEach(function (m) { m.geometry.translate(0, 0, 0); });
    anim.push(function () {
      var d = new Date(), s = d.getSeconds() + d.getMilliseconds() / 1000, m = d.getMinutes() + s / 60, h = (d.getHours() % 12) + m / 60;
      hh.rotation.z = -h / 12 * Math.PI * 2; mh.rotation.z = -m / 60 * Math.PI * 2; sh.rotation.z = -s / 60 * Math.PI * 2;
      [[hh, 0.25, h / 12], [mh, 0.39, m / 60], [sh, 0.42, s / 60]].forEach(function (a) { var ang = a[2] * Math.PI * 2; a[0].position.set(Math.sin(ang) * a[1], Math.cos(ang) * a[1], a[0].position.z); });
    });
  }

  /* ---------- các bảng trang trí lấy từ thanh trạng thái ---------- */
  function dark(x, w, h) { x.fillStyle = '#10141e'; x.fillRect(0, 0, w, h); var g = x.createLinearGradient(0, 0, 0, h); g.addColorStop(0, 'rgba(255,255,255,.07)'); g.addColorStop(1, 'rgba(255,255,255,0)'); x.fillStyle = g; x.fillRect(0, 0, w, h); }
  function title(x, t, w) { x.fillStyle = '#8d97ad'; x.font = '700 26px ' + FONT; x.textAlign = 'left'; x.textBaseline = 'alphabetic'; x.fillText(t, 28, 50); }
  function dot(x, cx, cy, r, c) { x.fillStyle = c; x.shadowColor = c; x.shadowBlur = 18; x.beginPath(); x.arc(cx, cy, r, 0, 7); x.fill(); x.shadowBlur = 0; }
  function drawMcli(x, w, h) {
    dark(x, w, h); title(x, 'MUSE-CLI', w); var r = STATE.runtime || {}, ok = !!r.status, bad = !ok && !!r.status_error;
    var c = ok ? '#3ecf8e' : bad ? '#f36c64' : '#8d919c'; dot(x, 60, 130, 20, c);
    x.fillStyle = '#fff'; x.font = '800 54px ' + FONT; x.fillText(ok ? 'ONLINE' : bad ? 'LỖI' : 'CHƯA RÕ', 100, 150);
    x.fillStyle = '#8d97ad'; x.font = '600 24px ' + FONT; var th = (STATE.config || {}).thread_id; x.fillText('thread ' + (th ? th.slice(0, 13) + '…' : 'chưa có'), 28, h - 28);
  }
  function drawPoll(x, w, h) {
    dark(x, w, h); title(x, 'POLL', w); var r = STATE.runtime || {}, c = r.poll_ok === true ? '#3ecf8e' : r.poll_ok === false ? '#f36c64' : '#8d919c';
    dot(x, w - 52, 38, 12, c); x.fillStyle = '#fff'; x.font = '800 78px ui-monospace, Menlo, monospace'; x.textAlign = 'center';
    x.fillText(r.poll_at ? new Date(r.poll_at * 1000).toLocaleTimeString('vi-VN', { hour12: false }) : '--:--:--', w / 2, 150);
    x.textAlign = 'left'; x.fillStyle = '#8d97ad'; x.font = '600 24px ' + FONT; x.fillText(r.poll_ok === false ? (r.poll_error || 'lỗi') : 'lần poll gần nhất', 28, h - 28);
  }
  function drawGauge(label, key) {
    return function (x, w, h) {
      dark(x, w, h); title(x, label, w); var o = USAGE && USAGE.ok ? USAGE[key] : null, pct = o && o.utilization != null ? Math.round(o.utilization) : null;
      var c = pct == null ? '#8d919c' : pct >= 85 ? '#f36c64' : pct >= 60 ? '#f2a650' : '#3ecf8e';
      x.fillStyle = '#fff'; x.font = '800 84px ' + FONT; x.fillText(pct == null ? '—' : pct + '%', 28, 142);
      var left = o ? fmtLeft(o.resets_at) : ''; if (left) { x.fillStyle = '#8d97ad'; x.font = '700 30px ' + FONT; x.textAlign = 'right'; x.fillText('↺ ' + left, w - 28, 142); x.textAlign = 'left'; }
      x.fillStyle = '#272e40'; rr(x, 28, h - 70, w - 56, 30, 15); x.fill();
      if (pct != null) { x.fillStyle = c; rr(x, 28, h - 70, Math.max(30, (w - 56) * pct / 100), 30, 15); x.fill(); }
    };
  }
  function drawAuto(x, w, h) {
    dark(x, w, h); title(x, 'AUTO', w); var on = !!(STATE.config || {}).auto_mode;
    x.fillStyle = on ? '#1f6b4a' : '#2a3042'; rr(x, 28, 76, 150, 76, 38); x.fill(); x.fillStyle = '#fff'; x.beginPath(); x.arc(on ? 140 : 66, 114, 30, 0, 7); x.fill();
    dot(x, w - 70, 114, 20, on ? '#3ecf8e' : '#5b6275'); x.fillStyle = '#fff'; x.font = '800 40px ' + FONT; x.textAlign = 'right'; x.fillText(on ? 'BẬT' : 'TẮT', w - 105, 128); x.textAlign = 'left';
    x.fillStyle = '#8d97ad'; x.font = '600 22px ' + FONT; x.fillText('DONE → pull → review', 28, h - 24);
  }
  function wrapText(x, text, mx, y, maxW, lh, maxLines) {
    var words = String(text).split(/\s+/), line = '', n = 0;
    for (var i = 0; i < words.length; i++) {
      var t = line ? line + ' ' + words[i] : words[i];
      if (x.measureText(t).width > maxW && line) { x.fillText(line, mx, y); y += lh; line = words[i]; n++; if (n >= maxLines - 1) { x.fillText(line + (i < words.length - 1 ? '…' : ''), mx, y); return; } }
      else line = t;
    }
    x.fillText(line, mx, y);
  }
  function drawChat(x, w, h) {
    dark(x, w, h); title(x, 'CHAT MUSE', w); var ms = (STATE.runtime && STATE.runtime.messages) || [];
    var last = ms.filter(function (m) { return !String(m.role || '').toLowerCase().includes('user'); }).slice(-1)[0];
    rr(x, 28, 70, w - 56, h - 100, 24); x.fillStyle = '#243049'; x.fill(); x.beginPath(); x.moveTo(60, h - 32); x.lineTo(46, h - 8); x.lineTo(86, h - 32); x.fill();
    x.fillStyle = '#ad92ff'; x.font = '700 24px ' + FONT; x.fillText('Muse' + (last && last.seq != null ? ' · #' + last.seq : ''), 50, 108);
    x.fillStyle = '#e9eaee'; x.font = '500 26px ' + FONT; wrapText(x, last ? last.text : 'Chưa có tin nhắn.', 50, 146, w - 100, 34, 4);
  }
  function drawTasks(x, w, h) {
    x.fillStyle = '#c9b48a'; x.fillRect(0, 0, w, h); x.strokeStyle = '#8f7a52'; x.lineWidth = 12; x.strokeRect(6, 6, w - 12, h - 12);
    x.fillStyle = '#2b2418'; x.font = '800 44px ' + FONT; x.textAlign = 'center'; x.fillText('VIỆC ĐANG MỞ', w / 2, 66);
    var open = PLANS.filter(function (p) { return planCat(p) !== 'done'; }), cols = ['#ffe27a', '#ff9eb5', '#9fe3ff', '#b6f0a3', '#ffc48a'];
    open.slice(0, 12).forEach(function (p, i) {
      var cx = 40 + (i % 4) * 118, cy = 96 + Math.floor(i / 4) * 120; x.save(); x.translate(cx + 50, cy + 50); x.rotate((i % 3 - 1) * 0.05);
      x.fillStyle = cols[i % cols.length]; x.shadowColor = 'rgba(0,0,0,.25)'; x.shadowBlur = 8; x.fillRect(-50, -50, 100, 100); x.shadowBlur = 0;
      x.fillStyle = '#2b2418'; x.font = '700 18px ' + FONT; wrapText(x, p.id, -42, -22, 84, 22, 3); x.restore();
    });
    if (!open.length) { x.fillStyle = '#2b2418'; x.font = '600 30px ' + FONT; x.fillText('Không còn việc mở', w / 2, h / 2); }
    x.textAlign = 'left';
  }
  function drawSettings(x, w, h) {
    dark(x, w, h); title(x, 'CÀI ĐẶT', w); [0.7, 0.35, 0.55].forEach(function (v, i) {
      var y = 96 + i * 52; x.fillStyle = '#2a3042'; rr(x, 28, y, w - 56, 12, 6); x.fill(); x.fillStyle = '#7b8cff'; rr(x, 28, y, (w - 56) * v, 12, 6); x.fill(); x.fillStyle = '#fff'; x.beginPath(); x.arc(28 + (w - 56) * v, y + 6, 15, 0, 7); x.fill();
    });
  }
  function drawBin(x, w, h) { x.clearRect(0, 0, w, h); rr(x, 10, 10, w - 20, h - 20, 30); x.fillStyle = '#10141e'; x.fill(); x.fillStyle = TRASH ? '#f36c64' : '#8d919c'; x.font = '800 70px ' + FONT; x.textAlign = 'center'; x.fillText(String(TRASH), w / 2, 90); x.fillStyle = '#8d97ad'; x.font = '700 22px ' + FONT; x.fillText('THÙNG RÁC', w / 2, 120); x.textAlign = 'left'; }
  function drawLogo(x, w, h) {
    x.clearRect(0, 0, w, h); x.fillStyle = '#f6f5f2'; x.font = '800 150px ' + FONT; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText('MUSUDE', w / 2, h / 2 - 6);
    x.fillStyle = '#6b4fc7'; x.beginPath(); x.arc(w / 2 - 330, h / 2 - 6, 24, 0, 7); x.fill(); x.fillStyle = '#2f5bd3'; x.beginPath(); x.arc(w / 2 + 330, h / 2 - 6, 24, 0, 7); x.fill();
  }
  function drawSky(x, w, h) {
    var g = x.createLinearGradient(0, 0, 0, h); g.addColorStop(0, '#6aa7ff'); g.addColorStop(1, '#cfe6ff'); x.fillStyle = g; x.fillRect(0, 0, w, h);
    x.fillStyle = 'rgba(255,255,255,.8)'; [[90, 70, 70], [260, 110, 50], [380, 60, 60]].forEach(function (c) { x.beginPath(); x.arc(c[0], c[1], c[2] * 0.6, 0, 7); x.arc(c[0] + 34, c[1] + 6, c[2] * 0.5, 0, 7); x.arc(c[0] - 30, c[1] + 8, c[2] * 0.45, 0, 7); x.fill(); });
    x.fillStyle = '#7d8fb3'; for (var i = 0; i < 9; i++) { var bw = 36 + (i * 17) % 30, bh = 70 + (i * 53) % 110; x.fillRect(i * 58 + 6, h - bh, bw, bh); }
    x.fillStyle = '#5a6c91'; for (var j = 0; j < 7; j++) { var bw2 = 40 + (j * 23) % 28, bh2 = 50 + (j * 41) % 80; x.fillRect(j * 74 + 24, h - bh2, bw2, bh2); }
  }

  /* ---------- dựng phòng ---------- */
  function clearWorld() { while (world.children.length) { var c = world.children.pop(); c.traverse(function (o) { if (o.geometry) o.geometry.dispose(); }); } chars = []; anim = []; boards = {}; walls = []; pickMeshes = []; bubbleOf = {}; COLL = []; }
  function pickPlans() {
    var act = PLANS.filter(function (p) { return planCat(p) !== 'done'; }), done = PLANS.filter(function (p) { return planCat(p) === 'done'; });
    var shown = SHOW_ALL ? done : done.slice(0, Math.max(0, 14 - act.length));
    return { list: act.concat(shown), hidden: done.length - shown.length };
  }
  function structSig() { var s = pickPlans(); return s.list.map(function (p) { return p.id; }).join('|') + '#' + s.hidden + '#' + SHOW_ALL; }

  var ROOM = { W: 56, D: 40 }, BACKV = 20;
  function build() {
    clearWorld();
    var sel = pickPlans(), groups = {}, order = [];
    sel.list.forEach(function (p) { var k = groupKey(p); (groups[k] = groups[k] || []).push(p); });
    var other = []; Object.keys(groups).forEach(function (k) { if (groups[k].length < 2) { other = other.concat(groups[k]); delete groups[k]; } });
    Object.keys(groups).sort().forEach(function (k) { order.push({ key: k.toUpperCase(), plans: groups[k] }); });
    if (other.length) order.push({ key: 'KHÁC', plans: other });

    // bố cục các khu
    var COLS = 4, PX = 3.45, PZ = 3.6, GAP = 2.2, W = 56, rowW = W - 4;
    order.forEach(function (z) { z.cols = Math.min(COLS, z.plans.length); z.rows = Math.ceil(z.plans.length / COLS); z.w = z.cols * PX + 1.2; z.d = z.rows * PZ + 1.6; });
    var rowsZ = [], cur = { x: 0, h: 0, items: [] };
    order.forEach(function (z) { if (cur.x + z.w > rowW && cur.items.length) { rowsZ.push(cur); cur = { x: 0, h: 0, items: [] }; } z.rx = cur.x; cur.x += z.w + GAP; cur.h = Math.max(cur.h, z.d); cur.items.push(z); });
    if (cur.items.length) rowsZ.push(cur);
    var deskDepth = rowsZ.reduce(function (a, r) { return a + r.h + GAP; }, 0);
    var BACK = (BACKV = 20), D = Math.max(40, BACK + deskDepth + 12);
    ROOM = { W: W, D: D };
    var hw = W / 2, hd = D / 2, WH = 7.2;

    // sàn + tường
    var floorTex = mkTex(512, 512, function (x, w, h) {
      x.fillStyle = '#7d89a6'; x.fillRect(0, 0, w, h);
      for (var r = 0; r < 4; r++) for (var q = 0; q < 4; q++) { var tone = 0.96 + seeded(r * 7 + q * 3 + 1) * 0.06; x.fillStyle = 'rgb(' + Math.round(168 * tone) + ',' + Math.round(176 * tone) + ',' + Math.round(198 * tone) + ')'; x.fillRect(q * 128 + 2, r * 128 + 2, 124, 124); }
    });
    floorTex.wrapS = floorTex.wrapT = THREE.RepeatWrapping; floorTex.repeat.set(W / 8, D / 8);
    var floor = new THREE.Mesh(new THREE.PlaneGeometry(W, D), new THREE.MeshLambertMaterial({ map: floorTex })); floor.rotation.x = -Math.PI / 2; floor.receiveShadow = true; world.add(floor);
    bx(world, W + 1.2, 0.4, D + 1.2, '#7a84a3', 0, -0.22, 0, true);
    function wall(w, x, z, ry, name) {
      var m = new THREE.Mesh(new THREE.BoxGeometry(w, WH, 0.35), new THREE.MeshLambertMaterial({ color: '#eae6f2', transparent: true, opacity: 1 }));
      m.position.set(x, WH / 2, z); m.rotation.y = ry; m.receiveShadow = true; world.add(m);
      bx(m, w, 0.5, 0.42, '#6a5acd', 0, -WH / 2 + 0.25, 0, true);
      walls.push({ mesh: m, n: new THREE.Vector3(Math.sin(ry), 0, Math.cos(ry)), name: name }); return m;
    }
    wall(W, 0, -hd - 0.17, 0, 'back'); wall(W, 0, hd + 0.17, Math.PI, 'front'); wall(D, -hw - 0.17, 0, Math.PI / 2, 'left'); wall(D, hw + 0.17, 0, -Math.PI / 2, 'right');
    // đèn neon chạy quanh phòng
    neon(world, W, 0.1, 0.1, '#19e3ff', 0, 0.5, -hd + 0.22, 7); neon(world, 0.1, 0.1, D, '#19e3ff', -hw + 0.22, 0.5, 0, 7); neon(world, 0.1, 0.1, D, '#19e3ff', hw - 0.22, 0.5, 0, 7);
    neon(world, W, 0.1, 0.1, '#ff3df0', 0, WH - 0.45, -hd + 0.22, 7); neon(world, 0.1, 0.1, D, '#b46bff', -hw + 0.22, WH - 0.45, 0, 7); neon(world, 0.1, 0.1, D, '#b46bff', hw - 0.22, WH - 0.45, 0, 7);
    plight(world, '#19e3ff', 0.55, 40, hw * 0.55, 5.5, -hd * 0.3); plight(world, '#ff3df0', 0.5, 40, -hw * 0.2, 5.5, hd * 0.4);

    // logo + bảng thông tin trên tường sau
    var logo = logoMesh = new THREE.Mesh(new THREE.PlaneGeometry(9, 1.5), new THREE.MeshBasicMaterial({ map: mkTex(1024, 170, drawLogo), transparent: true })); logo.position.set(-hw + 30.9, 6.0, -hd + 0.03); world.add(logo);
    var by = 3.9, bz = -hd + 0.12, bx0 = 8.2;   /* dãy màn hình trạng thái treo ở tường sau phòng PM · Claude, bảng chat Muse ở phòng nghỉ */
    boards.mcli = panel(world, 3.2, 1.68, bx0, by, bz, 0, drawMcli, 640); boards.poll = panel(world, 3.2, 1.68, bx0 + 3.5, by, bz, 0, drawPoll, 640);
    boards.u5 = panel(world, 3.2, 1.68, bx0 + 7.0, by, bz, 0, drawGauge('5 GIỜ', 'five_hour'), 640); boards.u7 = panel(world, 3.2, 1.68, bx0 + 10.5, by, bz, 0, drawGauge('TUẦN', 'seven_day'), 640);
    boards.auto = panel(world, 3.2, 1.68, bx0 + 14.0, by, bz, 0, drawAuto, 640);
    boards.chat = panel(world, 4.6, 2.21, -5.65, 3.5, bz, 0, drawChat, 800);

    // tường phải: cửa sổ, kệ sách, máy pha cà phê, tủ server (PING), thùng rác
    var rx = hw - 0.12;
    var win = new THREE.Mesh(new THREE.PlaneGeometry(6.4, 3.6), new THREE.MeshBasicMaterial({ map: mkTex(512, 288, drawSky) })); win.position.set(rx, 3.7, -hd + 18); win.rotation.y = -Math.PI / 2; world.add(win);
    var fz = -hd + 18; bx(world, 0.2, 3.9, 0.3, '#f4f4f8', rx - 0.05, 3.7, fz - 3.3); bx(world, 0.2, 3.9, 0.3, '#f4f4f8', rx - 0.05, 3.7, fz + 3.3); bx(world, 0.2, 0.3, 6.9, '#f4f4f8', rx - 0.05, 5.6, fz); bx(world, 0.2, 0.3, 6.9, '#f4f4f8', rx - 0.05, 1.8, fz); bx(world, 0.12, 3.6, 0.12, '#f4f4f8', rx - 0.05, 3.7, fz);
    var shelfZ = -hd + 9; bx(world, 1.1, 4.6, 5, '#7a5a3a', hw - 0.7, 2.3, shelfZ);
    for (var s = 0; s < 4; s++) { bx(world, 1.0, 0.1, 4.8, '#5d4329', hw - 0.7, 0.9 + s * 1.05, shelfZ); for (var b = 0; b < 9; b++) bx(world, 0.6, 0.7 + (b % 3) * 0.1, 0.34, ['#e0483e', '#3b82f6', '#f2c14e', '#3fa66a', '#a259ff'][(b + s) % 5], hw - 0.75, 1.3 + s * 1.05, shelfZ - 2.1 + b * 0.5); }
    var cm = new THREE.Group(); cm.position.set(hw - 1.1, 0, hd - 12); world.add(cm);
    bx(cm, 1.5, 1.5, 1.4, '#6a5e52', 0, 0.75, 0); bx(cm, 1.2, 1.1, 1.2, '#d8d2c8', 0, 2.05, 0); bx(cm, 0.4, 0.4, 0.4, '#f2f2f2', -0.2, 1.6, 0.3); bx(cm, 0.14, 0.14, 0.04, '#3ecf8e', 0.4, 2.3, 0.62, true);
    var rack = new THREE.Group(); rack.position.set(hw - 1.1, 0, -hd + 22.5); world.add(rack);
    bx(rack, 1.4, 3.4, 1.6, '#262c3c', 0, 1.7, 0); var leds = [];
    for (var r = 0; r < 6; r++) { bx(rack, 0.06, 0.4, 1.3, '#363e55', -0.72, 0.5 + r * 0.5, 0, true); for (var l = 0; l < 4; l++) { var led = bx(rack, 0.05, 0.08, 0.08, '#3ecf8e', -0.76, 0.42 + r * 0.5, -0.45 + l * 0.28, true); led.material = new THREE.MeshBasicMaterial({ color: '#3ecf8e' }); led.userData.off = Math.random() * 6; leds.push(led); } }
    var beacon = new THREE.Mesh(new THREE.CylinderGeometry(0.28, 0.28, 0.35, 12), new THREE.MeshBasicMaterial({ color: '#7b8cff' })); beacon.position.set(0, 3.7, 0); rack.add(beacon);
    var beam = bx(rack, 1.4, 0.05, 0.12, '#b9c3ff', 0, 3.7, 0, true); beam.material = new THREE.MeshBasicMaterial({ color: '#b9c3ff' });
    var pingLab = labelSprite('PING', '#3a4260', 0.8); pingLab.position.set(0, 4.4, 0); rack.add(pingLab);
    anim.push(function (t) {
      var ok = (STATE.runtime || {}).poll_ok !== false; leds.forEach(function (l, i) { var on = Math.sin(t * (ok ? 5 : 1.5) + l.userData.off + i) > -0.2; l.material.color.set(on ? (ok ? '#3ecf8e' : '#f36c64') : '#1d2230'); });
      beam.rotation.y = t * (ok ? 3 : 0.4); beacon.material.color.set(ok ? '#7b8cff' : '#f36c64');
    });
    var bin = new THREE.Group(); bin.position.set(8.3, 0, hd - 1.5); world.add(bin);
    cyl(bin, 0.62, 0.5, 1.3, '#5b6275', 0, 0.65, 0, 14); cyl(bin, 0.68, 0.68, 0.1, '#475063', 0, 1.35, 0, 14);
    boards.bin = { group: bin, plane: null };
    var binPlane = new THREE.Mesh(new THREE.PlaneGeometry(1.5, 1.1), new THREE.MeshBasicMaterial({ map: mkTex(256, 190, drawBin), transparent: true })); binPlane.position.set(0, 2.4, 0.2); binPlane.rotation.y = 0; bin.add(binPlane); boards.bin.tex = binPlane.material.map; boards.bin.draw = drawBin; boards.bin.redraw = function () { redraw(boards.bin.tex, drawBin); };

    // tường trái: bảng ghim việc, bảng cài đặt, đồng hồ
    var lx = -hw + 0.12, lz0 = -hd + 17;
    boards.tasks = panel(world, 4.6, 3.4, lx, 3.7, lz0, Math.PI / 2, drawTasks, 640); boards.settings = panel(world, 3.4, 2.2, lx, 3.7, lz0 + 6, Math.PI / 2, drawSettings, 512);
    boardClock(world, lx + 0.12, 4.3, lz0 + 11, Math.PI / 2); plant(world, -hw + 1.4, lz0 + 14, 1.3); plant(world, -hw + 1.4, hd - 2, 1.4);

    // phòng riêng của Bố Linh (CEO): nhiều tiện ích hơn PM
    buildFloorplan(hw, hd); var cx0 = -hw + 0.6 + 8.75, cz0 = -hd + 0.6 + 6.25; buildSuite(hw, hd);

    // bàn PM (Claude) + bảng trắng
    var pmx = hw - 12, pmz = -hd + 7.2, pm = new THREE.Group(); pm.position.set(pmx, 0, pmz); world.add(pm);
    bx(pm, 3.6, 0.12, 1.5, '#f1ece3', 0, 0.98, 0); [[-1.6, -0.6], [1.6, -0.6], [-1.6, 0.6], [1.6, 0.6]].forEach(function (p) { bx(pm, 0.12, 0.92, 0.12, '#59606f', p[0], 0.46, p[1]); });
    var m1 = bx(pm, 0.9, 0.6, 0.05, '#aeb5c4', -0.7, 1.4, 0.1); m1.rotation.x = -0.1; var m2 = bx(pm, 0.9, 0.6, 0.05, '#aeb5c4', 0.7, 1.4, 0.1); m2.rotation.x = -0.1; bx(pm, 0.9, 0.03, 0.5, '#8b92a1', 0, 1.06, 0.3, true);
    var pmPlate = labelSprite('PM · Claude', '#c76a1a', 0.9); pmPlate.position.set(0, 3.6, 0.2); pm.add(pmPlate);
    var wbg = new THREE.Group(); wbg.position.set(hw - 0.2, 3.4, -hd + 3.6); wbg.rotation.y = -Math.PI / 2; world.add(wbg); var wbd = bx(wbg, 4.4, 2.6, 0.12, '#ffffff', 0, 0, 0.06); bx(wbg, 4.6, 2.8, 0.1, '#7d8497', 0, 0, 0, true);
    var wbTex = mkTex(512, 300, function (x, w, h) { x.fillStyle = '#fff'; x.fillRect(0, 0, w, h); x.strokeStyle = '#3b82f6'; x.lineWidth = 5; x.beginPath(); x.moveTo(30, 60); x.lineTo(200, 60); x.moveTo(30, 100); x.lineTo(160, 100); x.stroke(); x.strokeStyle = '#e0483e'; x.beginPath(); x.moveTo(250, 200); x.bezierCurveTo(300, 120, 380, 260, 470, 150); x.stroke(); x.fillStyle = '#3b82f6'; x.font = '700 30px ' + FONT; x.fillText('Plan → Muse → Done', 30, 160); });
    var wbp = new THREE.Mesh(new THREE.PlaneGeometry(4.2, 2.4), new THREE.MeshBasicMaterial({ map: wbTex })); wbp.position.set(0, 0, 0.13); wbg.add(wbp);
    var pmc = makeChar({ shirt: '#e07a2b', sleeve: '#e07a2b', pants: '#2f3647', hair: '#2a1e18', skin: '#f3cfa8', collar: '#fff1e4' });
    pmc.group.position.set(pmx, 0, pmz + 1.7); pmc.status = 'doing'; pmc.kind = 'pm'; pmc.name = 'PM · Claude'; pmc.pmHome = new THREE.Vector3(pmx, 0, pmz + 1.7); world.add(pmc.group); chars.push(pmc);
    pmc.walk = null; PMC = pmc; buildPMLounge(hw, hd, pmx, pmz);


    // bàn họp tròn (PM hay ghé) + khu sofa
    var mt = new THREE.Group(); mt.position.set(hw - 7.0, 0, -hd + 10.1); world.add(mt);
    cyl(mt, 1.7, 1.7, 0.12, '#f1ece3', 0, 1.0, 0, 28); cyl(mt, 0.2, 0.3, 0.95, '#59606f', 0, 0.5, 0, 10);
    for (var q = 0; q < 5; q++) { var qa = q / 5 * Math.PI * 2; bx(mt, 0.7, 0.1, 0.7, '#3b4256', Math.sin(qa) * 2.4, 0.5, Math.cos(qa) * 2.4); bx(mt, 0.7, 0.7, 0.1, '#3b4256', Math.sin(qa) * 2.75, 0.9, Math.cos(qa) * 2.75).rotation.y = qa; }
    var rug = new THREE.Mesh(new THREE.BoxGeometry(8.4, 0.04, 4.6), M('#5b4a8a')); rug.position.set(-hw + 6.8, 0.03, hd - 4.9); rug.receiveShadow = true; world.add(rug);
    var sofa = new THREE.Group(); sofa.position.set(-hw + 6.8, 0, hd - 3.2); sofa.rotation.y = Math.PI; world.add(sofa);
    bx(sofa, 5.2, 0.7, 1.6, '#e0763a', 0, 0.45, 0); bx(sofa, 5.2, 1.2, 0.4, '#d0642a', 0, 1.0, -0.7); bx(sofa, 0.5, 1.0, 1.6, '#d0642a', -2.5, 0.75, 0); bx(sofa, 0.5, 1.0, 1.6, '#d0642a', 2.5, 0.75, 0);
    bx(world, 1.4, 0.5, 1.0, '#7a5a3a', -hw + 10.0, 0.3, hd - 5.3); bx(world, 0.5, 0.18, 0.4, '#ffffff', -hw + 9.8, 0.62, hd - 5.3, true);

    // khu làm việc
    var z0 = -hd + BACK, zrow = z0;
    rowsZ.forEach(function (row) {
      var rowW2 = row.items.reduce(function (a, z) { return a + z.w; }, 0) + (row.items.length - 1) * GAP, xs = -rowW2 / 2;
      row.items.forEach(function (z, zi) {
        var zxStart = xs; xs += z.w + GAP;
        var team = ['#4f8cff', '#ff7a59', '#3ecf8e', '#b07cff', '#f2c14e', '#2ec4c6', '#ff6fa5', '#8fa3b8'][(order.indexOf(z)) % 8], zx = zxStart + z.w / 2, zz = zrow + z.d / 2;
        var carpet = new THREE.Mesh(new THREE.BoxGeometry(z.w, 0.05, z.d), new THREE.MeshLambertMaterial({ color: team })); carpet.material.color.multiplyScalar(0.55).lerp(new THREE.Color('#d7c3a2'), 0.45); carpet.position.set(zx, 0.03, zz); carpet.receiveShadow = true; world.add(carpet);
        bx(world, z.w, 0.06, 0.12, team, zx, 0.07, zz + z.d / 2 - 0.05, true); bx(world, z.w, 0.06, 0.12, team, zx, 0.07, zz - z.d / 2 + 0.05, true);
        var tl = labelSprite(z.key + ' · ' + z.plans.length, team, 0.95); tl.position.set(zx, 2.9, zz - z.d / 2 + 0.2); world.add(tl);
        z.plans.forEach(function (p, i) {
          var cc = i % COLS, rr2 = Math.floor(i / COLS), dx = zx - z.w / 2 + 0.6 + PX / 2 + cc * PX, dz = zz - z.d / 2 + 1.0 + 1.8 + rr2 * PZ;
          var desk = makeDesk(world, dx, dz, p, team);
          var cat = planCat(p), ch = makeChar({ shirt: team, sleeve: team, pants: ['#2f3647', '#3a3550', '#2c4036', '#4a3a30'][i % 4], hair: HAIRS[(i * 3 + p.id.length) % HAIRS.length], skin: SKINS[(i + p.id.length) % SKINS.length], seated: true });
          ch.group.position.set(dx + 0.45, 0, dz + 0.95); ch.group.rotation.y = Math.PI; ch.root.position.y = 0.64; ch.kind = 'plan'; ch.plan = p; ch.name = p.id; ch.plate = desk.plate; ch.screens = desk.screens; ch.team = team; ch.cat = null; ch.faceY = Math.PI; ch.scrOff = Math.random() * 40; ch.deskPos = { sx: dx + 0.45, sz: dz + 0.95, az: dz + 2.1 };
          world.add(ch.group); chars.push(ch);
          bx(ch.head, 0.92, 0.16, 0.9, team, 0, 0.92, 0, true); bx(ch.head, 0.92, 0.06, 0.4, team, 0, 0.86, 0.6, true); bx(ch.root, 0.7, 0.14, 0.42, '#ffffff', 0, 0.12, 0, true);
          var tag = labelSprite(z.key, team, 0.55); tag.visible = false; world.add(tag); ch.tag = tag;
          var bub = bubbleSprite('idle'); bub.position.set(dx + 0.45, 3.15, dz + 0.95); world.add(bub); ch.bubble = bub;
          applyPlan(ch, p, true);
        });
      });
      zrow += row.h + GAP;
    });
    if (sel.hidden > 0) {
      var cab = new THREE.Group(); cab.position.set(4.7, 0, -hd + 3.6); cab.rotation.y = -Math.PI / 2; world.add(cab);
      for (var c = 0; c < 3; c++) { bx(cab, 1.3, 2.2, 1.1, '#8b92a1', 0, 1.1, c * 1.4 - 1.4); for (var dr = 0; dr < 3; dr++) bx(cab, 1.0, 0.5, 0.05, '#aeb5c4', 0, 0.45 + dr * 0.7, c * 1.4 - 1.4 + 0.58, true); }
      var al = labelSprite('+' + sel.hidden + ' đã xong (lưu trữ)', '#3a4260', 0.9); al.position.set(0, 3.2, 0); cab.add(al);
    }
    buildLeisure(hw, hd, cx0, cz0);
    buildExtras(hw, hd);
    placePM(); placeCEO();
    // đèn + camera
    sun.position.set(hw * 0.7, 28, hd * 0.9); sun.target.position.set(0, 0, 0); var sc = sun.shadow.camera; sc.left = -hw - 4; sc.right = hw + 4; sc.top = hd + 4; sc.bottom = -hd - 4; sc.near = 1; sc.far = 90; sc.updateProjectionMatrix();
    chars.forEach(function (c) { c.group.traverse(function (m) { if (m.isMesh) pickMeshes.push(m); }); });
    Object.keys(boards).forEach(function (k) { if (boards[k].redraw) boards[k].redraw(); });
    resetCamera();
  }
  function applyPlan(ch, p, force) {
    var cat = planCat(p); ch.plan = p;
    if (ch.cat !== cat || force) {
      ch.cat = cat; ch.status = cat; setFace(ch, cat === 'done' ? 'happy' : cat === 'blocked' ? 'worry' : cat === 'waiting' ? 'think' : cat === 'idle' ? 'sleep' : 'smile');
      var spr = bubbleSprite(cat); ch.bubble.material = spr.material;
    }
    redraw(ch.plate.material.map, function (x, w, h) { drawPlate(x, w, h, p, cat, ch.team); });
    ch.plate.material.map.needsUpdate = true;
  }
  var scrClock = 0;
  function updateScreens(t) {
    scrClock += 1;
    chars.forEach(function (c) {
      if (!c.screens || !c.plan) return; var cat = c.cat || 'idle', animated = (cat === 'doing' || cat === 'waiting');
      if (!animated && c.scrDrawn === cat + c.plan.id) return;
      if (animated && (scrClock + (c.phase | 0)) % 8) return;
      c.scrDrawn = cat + c.plan.id;
      c.screens.forEach(function (tx, i) { redraw(tx, function (x, w, h) { drawCode(x, w, h, cat, c.plan.id, c.scrOff + (cat === 'doing' ? t * 2.2 : 0) + i * 5, t); }); });
    });
  }
  var logoMesh = null, VIEW = 'near', viewInit = false;
  
  function applyView(mode) {
    VIEW = 'far';
    var W = ROOM.W, D = ROOM.D, r = Math.max(W * 0.82, D * 0.95) * 1.14;   /* kéo sát để phòng lấp đầy khung, không lộ nền xung quanh */
    camera.fov = 38; camera.updateProjectionMatrix();
    controls.enablePan = false; controls.enableZoom = true; controls.rotateSpeed = 0.5;
    controls.minDistance = 1; controls.maxDistance = 1000; controls.minPolarAngle = 0; controls.maxPolarAngle = Math.PI; controls.minAzimuthAngle = -Infinity; controls.maxAzimuthAngle = Infinity;
    controls.target.set(0, 1.2, D * 0.1); camera.position.set(r * 0.2, r * 0.74, D * 0.5 + r * 0.6); controls.update();
    var d0 = camera.position.distanceTo(controls.target), pa = controls.getPolarAngle(), az = controls.getAzimuthalAngle();
    /* chỉ cho nghiêng nhẹ quanh góc tổng thể, zoom gần được nhưng không lùi ra xa hơn */
    controls.minDistance = d0 * 0.5; controls.maxDistance = d0;
    controls.minPolarAngle = Math.max(0.2, pa - 0.15); controls.maxPolarAngle = pa + 0.12;
    controls.minAzimuthAngle = az - 0.22; controls.maxAzimuthAngle = az + 0.22;
    controls.update(); updateCeiling();
  }
  var ceil = null;
  function updateCeiling() {
    if (!ceil) { ceil = new THREE.Mesh(new THREE.PlaneGeometry(1, 1), new THREE.MeshBasicMaterial({ color: '#aeb4c6' })); ceil.rotation.x = Math.PI / 2; scene.add(ceil); }
    ceil.scale.set(ROOM.W + 1, ROOM.D + 1, 1); ceil.position.set(0, 7.25, 0); ceil.visible = false;
  }
  function resetCamera() { if (!viewInit) { viewInit = true; applyView(VIEW); } else updateCeiling(); }
  function resetView() { applyView(VIEW); }

  /* ---------- hoạt ảnh nhân vật ---------- */
  var EVENTS = { doing: ['look', 'nod', 'stretch', 'sip', 'turn'], waiting: ['look', 'stretch', 'sip', 'turn'], done: ['cheer', 'wave', 'stretch', 'turn'], idle: ['look', 'stretch', 'turn'], blocked: ['look', 'turn'], ceo: ['sip', 'look', 'nod'] };
  function animateChar(c, t, dt) {
    var s = c.status, cur = c.cur, k = Math.min(1, dt * 9), tg = { aLx: c.seated ? -0.95 : 0.05, aRx: c.seated ? -0.95 : 0.05, aLz: 0.06, aRz: -0.06, hy: Math.sin(t * 0.7 + c.phase) * 0.12, hx: 0, jump: 0 };
    if (c.seated) { c.legL.rotation.x = c.legR.rotation.x = -Math.PI / 2; }
    if (s === 'doing') { tg.aLx = -1.05 + Math.sin(t * 17 + c.phase) * 0.14; tg.aRx = -1.05 + Math.sin(t * 17 + c.phase + 3) * 0.14; tg.hx = 0.12 + Math.sin(t * 2 + c.phase) * 0.03; tg.hy *= 0.3; }
    else if (s === 'waiting') { tg.aRx = -2.3; tg.aRz = -0.2; tg.hx = 0.05; tg.hy = Math.sin(t * 0.9 + c.phase) * 0.35; }
    else if (s === 'blocked') { tg.aRx = Math.PI * 0.93; tg.aRz = -0.25 + Math.sin(t * 7) * 0.3; tg.hx = 0.08; }
    else if (s === 'done') { tg.aLx = -0.7; tg.aRx = -0.7; tg.hx = -0.05; }
    else if (s === 'idle') { tg.hx = 0.18; tg.aLx = -0.5; tg.aRx = -0.5; tg.hy *= 0.4; }
    if (c.act) {
      var a = c.act;
      if (a === 'sleep' || a === 'nap') { tg.hx = 0.55 + Math.sin(t * 1.3) * 0.04; tg.hy = 0; tg.aLx = tg.aRx = -0.5; tg.aLz = 0.12; tg.aRz = -0.12; }
      else if (a === 'game') { tg.aLx = -1.2 + Math.sin(t * 14 + c.phase) * 0.15; tg.aRx = -1.2 + Math.sin(t * 11 + c.phase) * 0.15; tg.hx = -0.1; tg.hy = Math.sin(t * 3 + c.phase) * 0.12; tg.jump = Math.max(0, Math.sin(t * 1.7 + c.phase * 3) - 0.85) * 1.2; }
      else if (a === 'tea') { tg.aRx = -1.9 + Math.sin(t * 1.2 + c.phase) * 0.2; tg.aRz = -0.1; tg.hx = -0.15; tg.hy = Math.sin(t * 0.6 + c.phase) * 0.25; }
      else if (a === 'recline') { tg.hx = -0.35; tg.hy = 0; tg.aLx = tg.aRx = -0.4; tg.aLz = 0.15; tg.aRz = -0.15; }
      else if (a === 'smoke') { tg.aRx = -2.05 + Math.sin(t * 0.5) * 0.05; tg.aRz = -0.1; tg.aLx = -0.5; tg.hx = -0.1 - 0.12 * Math.max(0, Math.sin(t * 1.2)); tg.hy = 0; }
      else if (a === 'box') { var ph = Math.sin(t * 9); tg.aLx = -0.6 - 1.0 * Math.max(0, ph); tg.aRx = -0.6 - 1.0 * Math.max(0, -ph); tg.aLz = 0.05; tg.aRz = -0.05; tg.hx = 0.05; tg.hy = 0; tg.jump = Math.abs(Math.cos(t * 9)) * 0.05; }
      else if (a === 'hurt') { tg.hx = 0.35 + Math.sin(t * 26) * 0.08; tg.hy = Math.sin(t * 22) * 0.5; tg.aLx = tg.aRx = -0.3; tg.aLz = 0.5; tg.aRz = -0.5; }
      else if (a === 'tv') { tg.hx = -0.05; tg.hy = 0; tg.aLx = tg.aRx = -0.45; tg.jump = Math.max(0, Math.sin(t * 0.9 + c.phase) - 0.96) * 4; }
      else if (a === 'run') { var rs = Math.sin(t * 11); tg.aLx = rs * 0.9; tg.aRx = -rs * 0.9; tg.hx = 0.05; tg.hy = 0; tg.jump = Math.abs(Math.cos(t * 11)) * 0.07; }
      else if (a === 'golf') { var gp = (t * 0.55 + c.phase) % 1, ga = gp < 0.6 ? -0.5 - (gp / 0.6) * 1.1 : -1.6 + ((gp - 0.6) / 0.4) * 1.5; tg.aLx = tg.aRx = ga; tg.aLz = 0.1; tg.aRz = -0.1; tg.hx = 0.25; tg.hy = 0; }
      else if (a === 'massage') { tg.hx = -0.3; tg.hy = 0; tg.aLx = tg.aRx = -0.3; tg.aLz = 0.2; tg.aRz = -0.2; }
      else if (a === 'karaoke') { tg.aRx = -2.05; tg.aRz = -0.1; tg.aLx = -0.9 + Math.sin(t * 3) * 0.7; tg.aLz = 0.4; tg.hy = Math.sin(t * 2) * 0.4; tg.hx = -0.08 + Math.sin(t * 6) * 0.05; tg.jump = Math.abs(Math.sin(t * 3)) * 0.06; }
      else if (a === 'aquarium') { tg.hy = Math.sin(t * 0.8 + c.phase) * 0.55; tg.hx = 0.05; tg.aLz = 0.15; tg.aRz = -0.15; }
      else if (a === 'read') { tg.aLx = tg.aRx = -1.15; tg.hx = 0.35; tg.hy = Math.sin(t * 0.5) * 0.08; }
      else if (a === 'punch') { var pp = Math.sin(t * 8); tg.aLx = -0.5 - 1.3 * Math.max(0, pp); tg.aRx = -0.5 - 1.3 * Math.max(0, -pp); tg.hx = 0.12; tg.hy = 0; tg.jump = Math.abs(Math.cos(t * 8)) * 0.07; }
      else if (a === 'wc') { tg.aLx = tg.aRx = -0.9; tg.hx = 0.35; tg.hy = 0; }
      else if (a === 'watched') { tg.aLx = -1.05 + Math.sin(t * 30 + c.phase) * 0.22; tg.aRx = -1.05 + Math.sin(t * 27 + c.phase + 2) * 0.22; tg.hx = 0.22; tg.hy = 0.1; }
      else if (a === 'inspect') { tg.aLx = 0.05; tg.aRx = -1.5; tg.aRz = 0.3; tg.hy = Math.sin(t * 0.9) * 0.45; tg.hx = 0.12; }
    }
    c.nextEv -= dt;
    if (!c.ev && c.nextEv <= 0 && !c.act) { var list = EVENTS[c.kind === 'ceo' ? 'ceo' : s] || ['look']; c.ev = { type: list[Math.floor(Math.random() * list.length)], t0: t, dur: rnd(1.4, 2.4) }; }
    if (c.ev) {
      var e = c.ev, u = (t - e.t0) / e.dur;
      if (u >= 1) { c.ev = null; c.nextEv = rnd(6, 20); }
      else {
        var sn = Math.sin(u * Math.PI);
        if (e.type === 'wave') { tg.aRx = Math.PI * 0.92; tg.aRz = -0.3 + Math.sin(t * 11) * 0.35; tg.hy = 0.3; }
        else if (e.type === 'stretch') { tg.aLx = tg.aRx = Math.PI * 0.96; tg.aLz = 0.45; tg.aRz = -0.45; tg.hx = -0.28 * sn; }
        else if (e.type === 'cheer') { tg.aLx = tg.aRx = Math.PI * 0.9; tg.aLz = 0.3; tg.aRz = -0.3; tg.jump = Math.abs(Math.sin(u * Math.PI * 3)) * 0.42; }
        else if (e.type === 'look') { tg.hy = Math.sin(u * Math.PI * 2) * 0.8; }
        else if (e.type === 'nod') { tg.hx = 0.1 + Math.sin(t * 9) * 0.16; }
        else if (e.type === 'sip') { tg.aRx = -2.1; tg.aRz = -0.1; tg.hx = -0.1 * sn; }
        else if (e.type === 'turn') { c.turn = Math.min(1, sn * 2.2); tg.aRx = Math.PI * 0.92; tg.aRz = -0.3 + Math.sin(t * 11) * 0.35; tg.hy = 0; tg.hx = 0; }
      }
    }
    if (c.faceY !== undefined) { var wantY = c.faceY + ((c.ev && c.ev.type === 'turn') ? -Math.PI * Math.min(1, Math.sin(((t - c.ev.t0) / c.ev.dur) * Math.PI) * 2.2) : 0); c.group.rotation.y += (wantY - c.group.rotation.y) * Math.min(1, dt * 7); }
    ['aLx', 'aRx', 'aLz', 'aRz', 'hy', 'hx', 'jump'].forEach(function (key) { cur[key] += (tg[key] - cur[key]) * k; });
    c.armL.rotation.x = cur.aLx; c.armR.rotation.x = cur.aRx; c.armL.rotation.z = cur.aLz; c.armR.rotation.z = cur.aRz; c.head.rotation.y = cur.hy; c.head.rotation.x = cur.hx;
    if (!c.walk || !c.walk.target) c.root.position.y = (c.seated ? c.root.position.y : 0.62) + 0;
    if (c.kind === 'ceo' && c.act === 'run') { var rl = Math.sin(t * 11); c.legL.rotation.x = rl * 0.95; c.legR.rotation.x = -rl * 0.95; }
    else if (c.kind === 'ceo' && c.act === 'massage') { c.legL.rotation.x = c.legR.rotation.x = -1.25; }
    else if (c.kind === 'ceo' && c.act === 'punch') { var pl = Math.sin(t * 4); c.legL.rotation.x = pl * 0.2; c.legR.rotation.x = -pl * 0.2; }
    c.root.rotation.z = (c.act === 'hurt') ? Math.sin(t * 26) * 0.14 : (c.act === 'massage' ? Math.sin(t * 26) * 0.03 : c.root.rotation.z * 0.8);
    if (c.kind === 'pm') { if (c.act === 'recline') c.legL.rotation.x = c.legR.rotation.x = -1.25; else if (c.act === 'box') { var sw2 = Math.sin(t * 4.5); c.legL.rotation.x = sw2 * 0.18; c.legR.rotation.x = -sw2 * 0.18; } }
    var baseY = c.seated ? (c.kind === 'ceo' ? 0.66 : 0.64) : 0.62; c.root.position.y = baseY + (c.yoff || 0) + cur.jump;
    if (c.plate === undefined && c.bubble) { }
  }
  function walkPM(c, t, dt) {
    var w = c.walk; if (!w) return;
    if (!w.target) { w.wait -= dt; if (w.wait <= 0) { var H = c.pmHome, spots = [new THREE.Vector3(H.x + rnd(-4, 5), 0, H.z + rnd(2, 6)), new THREE.Vector3(H.x - 7, 0, H.z + 1), new THREE.Vector3(ROOM.W / 2 - 3.5, 0, -ROOM.D / 2 + 13)]; w.target = spots[Math.floor(Math.random() * spots.length)]; w.home = false; } }
    else {
      var p = c.group.position, dx = w.target.x - p.x, dz = w.target.z - p.z, dist = Math.hypot(dx, dz);
      if (dist < 0.15) { w.target = w.home === false && Math.random() < 0.6 ? c.pmHome.clone() : null; if (!w.target) { w.wait = rnd(4, 9); } else w.home = true; c.group.rotation.y += (0 - c.group.rotation.y) * 0; }
      else { var sp = 1.7 * dt; p.x += dx / dist * Math.min(sp, dist); p.z += dz / dist * Math.min(sp, dist); var ang = Math.atan2(dx, dz), d = ang - c.group.rotation.y; d = Math.atan2(Math.sin(d), Math.cos(d)); c.group.rotation.y += d * Math.min(1, dt * 8);
        var sw = Math.sin(t * 9); c.legL.rotation.x = sw * 0.6; c.legR.rotation.x = -sw * 0.6; c.cur.aLx = -sw * 0.6; c.cur.aRx = sw * 0.6; c.root.position.y = 0.62 + Math.abs(Math.cos(t * 9)) * 0.05; return true; }
    }
    if (!w.target) { c.group.rotation.y += (0 - c.group.rotation.y) * Math.min(1, dt * 4); c.legL.rotation.x *= 0.8; c.legR.rotation.x *= 0.8; }
  }


  /* ---------- nghỉ ngơi, WC, BỐ đi kiểm tra ---------- */
  var LV = null, DIR = { nextL: 6, nextWC: 25, nextTour: 35 };
  function V3(x, z) { return new THREE.Vector3(x, 0, z); }
  function showCat(c, cat) {
    var f = { done: 'happy', blocked: 'worry', waiting: 'think', idle: 'sleep', doing: 'smile', sleep: 'sleep', game: 'happy', tea: 'happy', wc: 'smile', watched: 'worry' }[cat] || 'smile';
    setFace(c, f); if (!c.bubble) return;
    if (!bubbleCache[cat]) bubbleCache[cat] = bubbleTex(cat);
    c.bubble.material.map = bubbleCache[cat]; c.bubble.material.needsUpdate = true;
  }
  function ensureCup(c) {
    if (!c.cup) { var g = new THREE.Group(); g.position.set(0, -0.62, 0.2); g.rotation.x = 1.9; cyl(g, 0.15, 0.11, 0.32, '#f4a6c0', 0, 0, 0, 10); cyl(g, 0.16, 0.16, 0.05, '#ffffff', 0, 0.18, 0, 10); bx(g, 0.04, 0.3, 0.04, '#7a4a2a', 0.04, 0.3, 0, true); c.armR.add(g); c.cup = g; }
    return c.cup;
  }
  function goPath(c, pts, done, sp) { if (window.__pathLog) window.__pathLog.push({ who: c.kind + ':' + (c.name || ''), p0: { x: c.group.position.x, z: c.group.position.z }, pts: pts.map(function (q) { return { x: q.x, z: q.z }; }) }); c.mv = { pts: pts, i: 0, done: done, sp: sp || 2.6 }; c.ev = null; c.seated = false; c.yoff = 0; }
  function stepMove(c, t, dt) {
    var m = c.mv; if (!m) return false;
    var p = c.group.position, tg = m.pts[m.i], dx = tg.x - p.x, dz = tg.z - p.z, d = Math.hypot(dx, dz);
    if (d < 0.1) {
      m.i++;
      if (m.i >= m.pts.length) { c.mv = null; c.legL.rotation.x = c.legR.rotation.x = 0; if (m.done) m.done(); return false; }
      return true;
    }
    var s = Math.min(m.sp * dt, d); p.x += dx / d * s; p.z += dz / d * s;
    var df = Math.atan2(dx, dz) - c.group.rotation.y; df = Math.atan2(Math.sin(df), Math.cos(df)); c.group.rotation.y += df * Math.min(1, dt * 10);
    var sw = Math.sin(t * 10 + c.phase); c.legL.rotation.x = sw * 0.6; c.legR.rotation.x = -sw * 0.6; c.cur.aLx = -sw * 0.6; c.cur.aRx = sw * 0.6;
    c.root.position.y = 0.62 + Math.abs(Math.cos(t * 10 + c.phase)) * 0.05; return true;
  }
  function sideCx(c) { return c.deskPos.sx < 0 ? LV.CL : LV.CR; }
  function pathHome(c) {
    var p = c.group.position, d = c.deskPos, cx = sideCx(c), pts, sd = c.slotDoor;
    if (sd && p.z > FP.ZF + 0.05) {
      pts = []; if (p.z > sd.rowZ + 0.05) pts.push(V3(p.x, sd.rowZ));
      pts.push(V3(sd.x, Math.min(p.z, sd.rowZ)), V3(sd.x, LV.FZ), V3(cx, LV.FZ), V3(cx, d.az), V3(d.sx, d.az));
    }
    else if (p.z >= LV.FZ - 0.3) pts = [V3(p.x, LV.FZ), V3(cx, LV.FZ), V3(cx, d.az), V3(d.sx, d.az)];
    else if (Math.abs(p.x - cx) < 0.7) pts = [V3(cx, d.az), V3(d.sx, d.az)];
    else pts = [V3(d.sx, d.az)];
    pts.push(V3(d.sx, d.sz)); return pts;
  }
  function freeSlot(k) { var a = LV.slots[k]; for (var i = 0; i < a.length; i++) if (!a[i].busy) return a[i]; return null; }
  function endLeisure(c) {
    if (c.returning) return;
    if (c.slot) { c.slot.busy = null; c.slot = null; }
    c.act = null; if (c.cup) c.cup.visible = false;
    if (c.away === 'nap') { c.away = null; c.slotDoor = null; c.cool = rnd(25, 60); c.forced = false; showCat(c, c.cat || 'idle'); return; }
    if (c.away === 'wc') c.wcStage = 'out';
    c.returning = true; showCat(c, c.cat || 'idle');
    goPath(c, pathHome(c), function () { var d = c.deskPos; c.group.position.set(d.sx, 0, d.sz); c.seated = true; c.faceY = Math.PI; c.away = null; c.slotDoor = null; c.returning = false; c.forced = false; c.cool = rnd(25, 60); });
  }
  function startLeisure(c, kind) {
    var opts = []; if (freeSlot('sofa')) opts.push('sleep'); if (freeSlot('game')) opts.push('game'); if (freeSlot('tea')) opts.push('tea'); opts.push('nap');
    var k = kind || opts[Math.floor(Math.random() * opts.length)]; c.ev = null;
    if (k === 'nap') { c.away = 'nap'; c.act = 'nap'; c.dwell = rnd(18, 40); showCat(c, 'sleep'); return; }
    var slot = freeSlot(k === 'sleep' ? 'sofa' : k); if (!slot) return startLeisure(c, 'nap');
    slot.busy = c; c.slot = slot; c.away = 'leisure'; c.returning = false; c.slotDoor = { x: slot.door, rowZ: slot.rowZ };
    var d = c.deskPos, cx = sideCx(c), pts = [V3(d.sx, d.az), V3(cx, d.az), V3(cx, LV.FZ), V3(slot.door, LV.FZ), V3(slot.door, slot.rowZ), V3(slot.x, slot.rowZ)];
    if (k === 'sleep') pts.push(V3(slot.x, slot.appr));
    pts.push(V3(slot.x, slot.z));
    goPath(c, pts, function () {
      c.group.position.set(slot.x, 0, slot.z); c.act = k; c.dwell = k === 'sleep' ? rnd(25, 50) : k === 'game' ? rnd(20, 40) : rnd(12, 22);
      if (k === 'sleep') { c.seated = true; c.yoff = 0.25; c.faceY = Math.PI; } else c.faceY = 0;
      if (k === 'tea') ensureCup(c).visible = true;
      showCat(c, k);
    });
  }
  function startWC(c) {
    var wc = LV.wc, d = c.deskPos, cx = sideCx(c); wc.visitor = c; c.away = 'wc'; c.wcStage = 'in'; c.ev = null; c.returning = false; c.slotDoor = { x: wc.x, rowZ: wc.seatZ };
    goPath(c, [V3(d.sx, d.az), V3(cx, d.az), V3(cx, LV.FZ), V3(wc.x, LV.FZ), V3(wc.x, wc.doorZ), V3(wc.x, wc.seatZ)], function () {
      c.group.position.set(wc.x, 0, wc.seatZ); c.seated = true; c.faceY = Math.PI; c.act = 'wc'; c.dwell = rnd(7, 11); showCat(c, 'wc');
    });
  }
  function startTour(ceo, forceN) {
    var L = LV, cand = chars.filter(function (c) { return c.kind === 'plan' && !c.away && !c.mv && !c.act; });
    if (!cand.length) return false;
    cand.sort(function (a, b) { return ((b.cat === 'done' || b.cat === 'idle') ? 0 : 1) - ((a.cat === 'done' || a.cat === 'idle') ? 0 : 1) + (Math.random() - 0.5) * 1.5; });
    var targets = cand.slice(0, forceN || (1 + Math.floor(Math.random() * 3))), prev = null, i = 0;
    ceo.away = 'tour'; ceo.ev = null; ceo.act = null;
    var out = [V3(L.cx0 + 2.2, L.cz0 - 1.55), V3(L.cx0 + 2.2, L.cz0 + 3.0), V3(L.cx0 + 1.7, L.cz0 + 5.4), V3(L.cx0 + 1.7, L.BZ)];
    function next() {
      if (i >= targets.length) {
        var pts = []; if (prev) { var cp = sideCx(prev); pts.push(V3(cp, prev.deskPos.az), V3(cp, L.BZ)); }
        pts.push(V3(L.cx0 + 1.7, L.BZ), V3(L.cx0 + 1.7, L.cz0 + 5.4), V3(L.cx0 + 2.2, L.cz0 + 3.0), V3(L.cx0 + 2.2, L.cz0 - 1.55), V3(L.cx0 - 0.4, L.cz0 - 1.55));
        goPath(ceo, pts, function () { ceo.group.position.set(L.cx0 - 0.4, 0, L.cz0 - 1.55); ceo.seated = true; ceo.faceY = 0; ceo.away = null; ceo.act = null; });
        return;
      }
      var e = targets[i++]; if (e.away || e.act) return next();
      var d = e.deskPos, ce = sideCx(e), pts2 = [];
      if (prev) { var cq = sideCx(prev); pts2.push(V3(cq, prev.deskPos.az)); if (cq !== ce) pts2.push(V3(cq, L.BZ), V3(ce, L.BZ)); pts2.push(V3(ce, d.az), V3(d.sx, d.az)); }
      else pts2 = out.concat([V3(ce, L.BZ), V3(ce, d.az), V3(d.sx, d.az)]);
      goPath(ceo, pts2, function () {
        ceo.group.position.set(d.sx, 0, d.az); ceo.faceY = Math.PI; ceo.act = 'inspect'; ceo.dwell = rnd(4, 7);
        e.act = 'watched'; showCat(e, 'watched');
        ceo.onDwell = function () { if (e.act === 'watched') { e.act = null; showCat(e, e.cat || 'idle'); } ceo.act = null; prev = e; next(); };
      });
    }
    next(); return true;
  }
  function rpick(a) { return a[Math.floor(Math.random() * a.length)]; }
  function director(dt) {
    if (!LV) return;
    var wc = LV.wc, ceo = null;
    chars.forEach(function (c) {
      if (c.kind === 'ceo') ceo = c;
      if (c.kind !== 'plan') return;
      if (c.cool > 0) c.cool -= dt;
      if ((c.away === 'leisure' || c.away === 'nap') && !c.forced && c.cat !== 'done' && !c.returning) endLeisure(c);
      if (c.act && c.dwell !== undefined && (c.away === 'leisure' || c.away === 'nap' || c.away === 'wc')) { c.dwell -= dt; if (c.dwell <= 0) { c.dwell = 1e9; endLeisure(c); } }
    });
    var v = wc.visitor; if (v && v.wcStage === 'out' && v.group.position.z < wc.doorZ - 0.7) { v.wcStage = null; wc.visitor = null; }
    DIR.nextL -= dt;
    if (DIR.nextL <= 0) {
      DIR.nextL = rnd(3, 7);
      var cl = chars.filter(function (c) { return c.kind === 'plan' && !c.away && !c.mv && !c.act && c.cat === 'done' && !(c.cool > 0); });
      if (cl.length && Math.random() < 0.7) startLeisure(rpick(cl));
    }
    DIR.nextWC -= dt;
    if (DIR.nextWC <= 0) {
      DIR.nextWC = rnd(30, 60);
      var wl = chars.filter(function (c) { return c.kind === 'plan' && !c.away && !c.mv && !c.act; });
      if (!wc.visitor && wl.length) startWC(rpick(wl));
    }
  }
  function forceTest(kind, n) {
    var pl = chars.filter(function (c) { return c.kind === 'plan' && !c.away; }), ceo = chars.filter(function (c) { return c.kind === 'ceo'; })[0];
    if (kind === 'leisure') { pl.slice(0, n || 4).forEach(function (c, i) { c.forced = true; startLeisure(c, ['sleep', 'game', 'tea', 'sleep', 'game', 'tea', 'nap'][i % 7]); }); }
    else if (kind === 'wc') { if (pl.length && !LV.wc.visitor) startWC(pl[0]); }
    else if (kind === 'tour') { startTour(ceo, n || 2); }
  }
  /* ---------- PM · Claude: hành vi theo hoạt động thật (chỉ đọc /api/activity) ----------
     review (claude_verdict, claude_bug, review) → đi tới đúng bàn nhân viên của plan đó
     chạy MCP/Unity (claude_unity, claude_test, claude_file, claude_plan) → ngồi bàn máy tính của PM
     rảnh → góc nghỉ: ngả lưng, uống trà, boxing, hút điếu cày */
  var PMS = { cur: null, queue: [], workUntil: 0, lastTs: undefined, tag: null, tagText: '', loc: null }, PMC = null, PML = null;
  var REVIEW_KINDS = { claude_verdict: 1, review: 1, claude_bug: 1 }, WORK_KINDS = { claude_unity: 1, claude_test: 1, claude_file: 1, claude_plan: 1 };
  var ACT_TEXT = { recline: 'Ngả lưng', tea: 'Uống trà', smoke: 'Hút điếu cày', box: 'Tập boxing' }, ACT_FACE = { recline: 'sleep', tea: 'happy', smoke: 'smile', box: 'happy', inspect: 'smile', hurt: 'worry' };
  function eventPlan(e) { return e.pplan || String(e.task || '').split('/')[0]; }
  function onEvents(list) {
    if (PMS.lastTs === undefined) PMS.lastTs = Date.now() - 60000; var mx = PMS.lastTs;
    (list || []).slice().reverse().forEach(function (e) {
      var ts = Date.parse(e.at); if (!ts || ts <= PMS.lastTs) return; if (ts > mx) mx = ts;
      if (REVIEW_KINDS[e.kind]) { var id = eventPlan(e); if (id && PMS.queue.indexOf(id) < 0 && PMS.queue.length < 3) PMS.queue.push(id); }
      else if (WORK_KINDS[e.kind]) PMS.workUntil = Math.max(PMS.workUntil, ts + 120000);
      if (STUCK_KINDS[e.kind]) ceoQueuePunish(eventPlan(e));
    });
    PMS.lastTs = mx;
  }
  function pmTag(text, color) {
    if (!PMC) return; if (PMS.tagText === text && PMS.tag && PMS.tag.parent) return;
    if (PMS.tag) { world.remove(PMS.tag); if (PMS.tag.material.map) PMS.tag.material.map.dispose(); }
    PMS.tag = labelSprite(text, color, 0.75); world.add(PMS.tag); PMS.tagText = text;
  }
  function ensurePipe(c) {
    if (!c.pipe) { var g = new THREE.Group(); g.position.set(0, -0.62, 0.2); g.rotation.x = 1.9; cyl(g, 0.045, 0.05, 0.95, '#c9b36b', 0, 0.3, 0, 8); cyl(g, 0.11, 0.08, 0.16, '#5a4630', 0, 0.1, 0, 8); c.armR.add(g); c.pipe = g; }
    return c.pipe;
  }
  function pmAct(act) {
    var c = PMC; c.act = act || null; c.ev = null;
    if (c.cup) c.cup.visible = act === 'tea'; if (act === 'tea') ensureCup(c).visible = true;
    if (c.pipe) c.pipe.visible = act === 'smoke'; if (act === 'smoke') ensurePipe(c).visible = true;
    setFace(c, ACT_FACE[act] || 'smile');
  }
  function pmAt(k, x, z, rot, seated, act, e) {
    var c = PMC; c.mv = null; c.group.position.set(x, 0, z); c.faceY = rot; c.group.rotation.y = rot; c.seated = seated; c.yoff = 0;
    c.legL.rotation.x = c.legR.rotation.x = 0; pmAct(act); PMS.loc = { k: k, x: x, z: z, e: e || null };
  }
  function pmSpot(act) { return act === 'box' ? { x: PML.boxX, z: PML.z0, rot: Math.PI / 2, seated: false } : { x: PML.rx, z: PML.z0, rot: 0, seated: true }; }
  function pmRoute(to, arg) {
    var P = PML, L = LV, loc = PMS.loc || { k: 'desk' }, pts = [], hubZ, d, ce, sp;
    if (loc.k === 'desk') pts.push(V3(P.hubX, P.deskZ));
    else if (loc.k === 'lounge') pts.push(V3(loc.x, P.zl), V3(P.hubX, P.zl));
    else if (loc.e && loc.e.deskPos) { d = loc.e.deskPos; ce = sideCx(loc.e); pts.push(V3(d.sx, d.az), V3(ce, d.az), V3(ce, L.BZ), V3(P.hubX, L.BZ)); }
    else pts.push(V3(P.hubX, P.zl));
    if (to === 'desk') pts.push(V3(P.hubX, P.deskZ), V3(P.deskX, P.deskZ));
    else if (to === 'lounge') { sp = pmSpot(arg); pts.push(V3(P.hubX, P.zl), V3(sp.x, P.zl), V3(sp.x, sp.z)); }
    else { d = arg.deskPos; ce = sideCx(arg); pts.push(V3(P.hubX, L.BZ), V3(ce, L.BZ), V3(ce, d.az), V3(d.sx, d.az)); }
    return pts;
  }
  function pmChoose() {
    var now = Date.now(), c = PMC, P = PML;
    while (PMS.queue.length) {
      var id = PMS.queue.shift(), e = chars.filter(function (x) { return x.kind === 'plan' && x.plan && x.plan.id === id; })[0];
      if (!e || e.away || e.act || e.mv) continue;
      pmTag('Review · ' + id, '#e0763a'); PMS.cur = null;
      goPath(c, pmRoute('emp', e), function () {
        var d = e.deskPos; pmAt('emp', d.sx, d.az, Math.PI, false, 'inspect', e);
        if (!e.away && !e.act) { e.act = 'watched'; showCat(e, 'watched'); }
        PMS.cur = { kind: 'review', until: Date.now() + rnd(11, 16) * 1000, e: e };
      });
      return;
    }
    if (now < PMS.workUntil) {
      pmTag('Chạy MCP / Unity', '#3b82f6');
      if (PMS.loc && PMS.loc.k === 'desk') { pmAct(null); PMS.cur = { kind: 'desk' }; return; }
      PMS.cur = null; goPath(c, pmRoute('desk'), function () { pmAt('desk', P.deskX, P.deskZ, Math.PI, true, null); PMS.cur = { kind: 'desk' }; });
      return;
    }
    var acts = ['recline', 'tea', 'box', 'smoke', 'recline'].filter(function (a) { return a !== c.act; }), act = acts[Math.floor(Math.random() * acts.length)], sp = pmSpot(act);
    pmTag(ACT_TEXT[act], '#3ecf8e');
    function arrive() { pmAt('lounge', sp.x, sp.z, sp.rot, sp.seated, act); PMS.cur = { kind: 'rest', act: act, until: Date.now() + rnd(14, 26) * 1000 }; }
    if (PMS.loc && PMS.loc.k === 'lounge' && Math.abs(PMS.loc.x - sp.x) < 0.1) { c.seated = sp.seated; pmAct(act); PMS.cur = { kind: 'rest', act: act, until: now + rnd(14, 26) * 1000 }; return; }
    PMS.cur = null; goPath(c, pmRoute('lounge', act), arrive);
  }
  function pmBrain() {
    if (!PMC || !PML || !LV) return; var c = PMC, now = Date.now();
    if (PMS.tag && PMS.tag.parent) PMS.tag.position.set(c.group.position.x, c.seated ? 3.3 : 3.9, c.group.position.z);
    if (c.mv || PMS.hold) return; var cur = PMS.cur;
    if (!cur) return pmChoose();
    if (cur.kind === 'review') {
      if (now < cur.until) return; var e = cur.e; if (e && e.act === 'watched') { e.act = null; showCat(e, e.cat || 'idle'); }
      pmAct(null); PMS.cur = null; return pmChoose();
    }
    if (PMS.queue.length) return pmChoose();
    if (cur.kind === 'desk') { if (now >= PMS.workUntil) pmChoose(); return; }
    if (cur.kind === 'rest' && (now < PMS.workUntil || now >= cur.until)) pmChoose();
  }
  function placePM() {
    var cur = PMS.cur; PMS.tag = null; PMS.tagText = '';
    if (cur && cur.kind === 'desk') { pmAt('desk', PML.deskX, PML.deskZ, Math.PI, true, null); pmTag('Chạy MCP / Unity', '#3b82f6'); return; }
    var act = (cur && cur.kind === 'rest' && cur.act) || 'recline', sp = pmSpot(act);
    pmAt('lounge', sp.x, sp.z, sp.rot, sp.seated, act); PMS.cur = { kind: 'rest', act: act, until: Date.now() + rnd(8, 16) * 1000 }; pmTag(ACT_TEXT[act], '#3ecf8e');
  }
  function buildPMLounge(hw, hd, pmx, pmz) {
    var z0 = -hd + 6, rx = -7.0, boxX = -3.0, bagX = -1.9;
    PML = { z0: z0, zl: z0 + 1.9, rx: rx, boxX: boxX, hubX: 2.1, deskX: pmx, deskZ: pmz + 1.1 };
    var ch = new THREE.Group(); ch.position.set(pmx, 0, pmz + 1.1); world.add(ch);
    bx(ch, 1.0, 0.14, 1.0, '#3b4256', 0, 0.55, 0); bx(ch, 1.0, 1.1, 0.14, '#3b4256', 0, 1.1, 0.52); cyl(ch, 0.08, 0.08, 0.5, '#222', 0, 0.28, 0); cyl(ch, 0.5, 0.5, 0.06, '#222', 0, 0.04, 0, 6);
    var rug = new THREE.Mesh(new THREE.BoxGeometry(8.0, 0.04, 4.6), M('#4a5d6e')); rug.position.set(-5.65, 0.03, z0 + 0.5); rug.receiveShadow = true; world.add(rug);
    var rc = new THREE.Group(); rc.position.set(rx, 0, z0); world.add(rc);
    bx(rc, 1.4, 0.3, 1.5, '#7a4a3a', 0, 0.45, 0); var rb = bx(rc, 1.4, 1.5, 0.3, '#6a3f31', 0, 1.15, -0.8); rb.rotation.x = -0.35;
    bx(rc, 0.25, 0.55, 1.4, '#5d3427', -0.82, 0.65, 0); bx(rc, 0.25, 0.55, 1.4, '#5d3427', 0.82, 0.65, 0);
    var tbl = new THREE.Group(); tbl.position.set(rx + 1.9, 0, z0 + 0.1); world.add(tbl);
    cyl(tbl, 0.38, 0.38, 0.06, '#7a5a3a', 0, 0.78, 0, 14); cyl(tbl, 0.06, 0.06, 0.75, '#5d4329', 0, 0.4, 0, 8); cyl(tbl, 0.14, 0.16, 0.2, '#e8e3d8', 0, 0.91, 0, 10); cyl(tbl, 0.07, 0.06, 0.1, '#f4a6c0', 0.22, 0.84, 0.1, 8);
    var bagP = new THREE.Group(); bagP.position.set(bagX, 3.0, z0); world.add(bagP);
    cyl(bagP, 0.03, 0.03, 0.5, '#888', 0, -0.25, 0, 6); cyl(bagP, 0.36, 0.32, 1.3, '#c0392b', 0, -1.15, 0, 14); cyl(bagP, 0.37, 0.37, 0.08, '#222', 0, -0.5, 0, 14);
    cyl(world, 0.07, 0.07, 3.0, '#555', bagX, 1.5, z0 - 0.9, 8); bx(world, 0.08, 0.08, 0.95, '#555', bagX, 3.0, z0 - 0.42); bx(world, 1.2, 0.12, 1.2, '#333', bagX, 0.06, z0 - 0.9);
    var ll = labelSprite('GÓC NGHỈ', '#3a4260', 0.8); ll.position.set(-5.65, 3.1, z0 - 1.6); world.add(ll);
    anim.push(function (t, dt) {
      var on = PMC && PMC.act === 'box' && !PMC.mv && PMS.cur && PMS.cur.kind === 'rest';
      bagP.rotation.z += ((on ? 0.22 * Math.abs(Math.sin(t * 9)) : 0) - bagP.rotation.z) * Math.min(1, dt * 12);
    });
    var puffs = []; for (var i = 0; i < 6; i++) { var pf = new THREE.Mesh(new THREE.SphereGeometry(0.12, 8, 6), new THREE.MeshBasicMaterial({ color: '#e9ecf2', transparent: true, opacity: 0, depthWrite: false })); pf.visible = false; world.add(pf); puffs.push(pf); }
    anim.push(function (t) {
      var on = PMC && PMC.act === 'smoke' && !PMC.mv && PMS.cur && PMS.cur.kind === 'rest';
      puffs.forEach(function (pf, i) {
        if (!on) { pf.visible = false; return; } var u = (t * 0.45 + i / 6) % 1; pf.visible = true;
        pf.position.set(PMC.group.position.x + 0.1 + Math.sin(u * 6 + i) * 0.15, 1.7 + u * 1.3, PMC.group.position.z + 0.45 + u * 0.3);
        var sc = 0.6 + u * 1.6; pf.scale.set(sc, sc, sc); pf.material.opacity = 0.55 * (1 - u);
      });
    });
  }
  /* ---------- Bố Linh (CEO): phòng riêng + hành vi ----------
     Rảnh → làm đủ thứ trong phòng (ký duyệt, TV, bar, chạy bộ, golf, massage, karaoke, ngắm cá, đọc sách) và đi kiểm tra bàn nhân viên.
     Task treo/pending (needs_human, muse_timeout, plan_blocked, muse_blocked, hoặc plan "Cần xem" kéo dài) → ra đấm Claude. */
  var SU = null, CEOC = null;
  var CEOS = { cur: null, loc: null, punishQ: [], lastPunish: Date.now(), tag: null, tagText: '', nextTour: Date.now() + 90000, nextScan: 0, tour: null, last: null };
  var STUCK_KINDS = { needs_human: 1, muse_timeout: 1, plan_blocked: 1, muse_blocked: 1 };
  var CEO_TEXT = { work: 'Ký duyệt', tv: 'Xem TV', bar: 'Nhâm nhi', run: 'Chạy bộ', golf: 'Đánh golf', massage: 'Massage', karaoke: 'Karaoke', aquarium: 'Ngắm cá', read: 'Đọc sách' };
  var CEO_ACT = { work: null, tv: 'tv', bar: 'tea', run: 'run', golf: 'golf', massage: 'massage', karaoke: 'karaoke', aquarium: 'aquarium', read: 'read' };
  var CEO_FACE = { tv: 'happy', tea: 'happy', run: 'smile', golf: 'smile', massage: 'sleep', karaoke: 'happy', aquarium: 'smile', read: 'smile', punch: 'worry', inspect: 'smile' };
  function ensureClub(c) {
    if (!c.club) { var g = new THREE.Group(); g.position.set(0, -0.62, 0.1); bx(g, 0.04, 1.25, 0.04, '#c8c8d0', 0, -0.45, 0.05, true); bx(g, 0.22, 0.07, 0.12, '#6b6b78', 0.08, -1.1, 0.05, true); c.armR.add(g); c.club = g; }
    return c.club;
  }
  function ensureMic(c) {
    if (!c.mic) { var g = new THREE.Group(); g.position.set(0, -0.62, 0.2); g.rotation.x = 1.9; cyl(g, 0.03, 0.03, 0.35, '#222', 0, 0.15, 0, 8); cyl(g, 0.09, 0.09, 0.12, '#bbbbbb', 0, 0.38, 0, 10); c.armR.add(g); c.mic = g; }
    return c.mic;
  }
  function ensureBook(c) {
    if (!c.book) { c.book = bx(c.root, 0.5, 0.34, 0.06, '#c0392b', 0, 0.45, 0.36, true); }
    return c.book;
  }
  function ceoAct(act) {
    var c = CEOC; c.act = act || null; c.ev = null;
    if (c.cup) c.cup.visible = act === 'tea'; if (act === 'tea') ensureCup(c).visible = true;
    if (c.club) c.club.visible = act === 'golf'; if (act === 'golf') ensureClub(c).visible = true;
    if (c.mic) c.mic.visible = act === 'karaoke'; if (act === 'karaoke') ensureMic(c).visible = true;
    if (c.book) c.book.visible = act === 'read'; if (act === 'read') ensureBook(c).visible = true;
    setFace(c, CEO_FACE[act] || 'smile');
  }
  function ceoAt(k, x, z, rot, seated, act, extra) {
    var c = CEOC; c.mv = null; c.group.position.set(x, 0, z); c.faceY = rot; c.group.rotation.y = rot; c.seated = seated; c.yoff = 0; c.legL.rotation.x = c.legR.rotation.x = 0;
    ceoAct(act); CEOS.loc = Object.assign({ k: k, x: x, z: z }, extra || {});
  }
  function ceoTag(text, color) {
    if (!CEOC) return; if (CEOS.tagText === text && CEOS.tag && CEOS.tag.parent) return;
    if (CEOS.tag) { world.remove(CEOS.tag); if (CEOS.tag.material.map) CEOS.tag.material.map.dispose(); }
    CEOS.tag = labelSprite(text, color || '#b8860b', 0.95); world.add(CEOS.tag); CEOS.tagText = text;
  }
  function ceoG() { return V3(SU.outX, PML.zl); }
  function ceoRoute(to, arg) {
    var loc = CEOS.loc || { k: 'suite', spot: 'work' }, pts = [], level;
    if (loc.k === 'suite') { var sp = SU.spots[loc.spot || 'work']; pts = sp.via.slice(0, -1).reverse(); pts.push(SU.hub); level = 'hub'; }
    else { var op = loc.outPts || []; pts = op.slice(0, -1).reverse(); pts.push(ceoG()); level = 'gate'; }
    if (to === 'spot') { if (level === 'gate') pts.push(SU.doorOut, SU.doorIn, SU.hub); pts = pts.concat(SU.spots[arg].via); }
    else { if (level === 'hub') pts.push(SU.doorIn, SU.doorOut, ceoG()); pts = pts.concat(arg.outPts); }
    return pts;
  }
  function ceoGoSpot(name) {
    var sp = SU.spots[name], dw = name === 'work' ? rnd(20, 38) : rnd(14, 26);
    ceoTag('Bố Linh · ' + CEO_TEXT[name], '#b8860b'); CEOS.last = name;
    function setRest() { CEOS.cur = { kind: 'rest', act: name, until: Date.now() + dw * 1000 }; }
    if (CEOS.loc && CEOS.loc.k === 'suite' && CEOS.loc.spot === name) { ceoAct(CEO_ACT[name]); setRest(); return; }
    CEOS.cur = null;
    goPath(CEOC, ceoRoute('spot', name), function () { ceoAt('suite', sp.x, sp.z, sp.rot, sp.seated, CEO_ACT[name], { spot: name }); setRest(); });
  }
  function ceoQueuePunish(id, force) {
    if (!force && Date.now() - CEOS.lastPunish < 90000) return; if (CEOS.punishQ.length >= 1) return; CEOS.punishQ.push(id || '?');
  }
  function ceoPunishStart(id) {
    var c = CEOC, loc = PMS.loc, T, d, ce;
    if (!PMC || PMS.hold || PMC.mv || !loc) return false;
    if (loc.k === 'lounge') T = { outPts: [V3(loc.x, PML.zl), V3(loc.x, PML.z0 + 1.3)], rot: Math.PI };
    else if (loc.k === 'desk') T = { outPts: [V3(PML.hubX, PML.zl), V3(PML.hubX, PML.deskZ), V3(PML.deskX - 1.5, PML.deskZ)], rot: Math.PI / 2 };
    else if (loc.e && loc.e.deskPos) { d = loc.e.deskPos; ce = sideCx(loc.e); T = { outPts: [V3(PML.hubX, PML.zl), V3(PML.hubX, LV.BZ), V3(ce, LV.BZ), V3(ce, d.az), V3(d.sx + 1.4, d.az)], rot: -Math.PI / 2 }; }
    else return false;
    PMS.hold = true; var cur = PMS.cur; if (cur && cur.kind === 'review' && cur.e && cur.e.act === 'watched') { cur.e.act = null; showCat(cur.e, cur.e.cat || 'idle'); }
    ceoTag('Bố Linh · Task treo! ' + String(id).slice(0, 18), '#c0392b'); CEOS.cur = null; CEOS.lastPunish = Date.now();
    var end = T.outPts[T.outPts.length - 1];
    goPath(c, ceoRoute('out', T), function () {
      ceoAt('out', end.x, end.z, T.rot, false, 'punch', { outPts: T.outPts });
      pmAct('hurt'); pmTag('Dạ con xin lỗi bố Linh!', '#c0392b');
      CEOS.cur = { kind: 'punish', until: Date.now() + 7000 };
    });
    return true;
  }
  function ceoTour() {
    CEOS.nextTour = Date.now() + rnd(140, 240) * 1000;
    var cand = chars.filter(function (x) { return x.kind === 'plan' && x.deskPos && !x.away && !x.mv && !x.act; });
    if (!cand.length) return false;
    cand.sort(function (a, b) { return ((b.cat === 'doing' || b.cat === 'blocked') ? 1 : 0) - ((a.cat === 'doing' || a.cat === 'blocked') ? 1 : 0) + (Math.random() - 0.5); });
    CEOS.tour = { list: cand.slice(0, 1 + Math.floor(Math.random() * 2)), i: 0 }; ceoTourNext(); return true;
  }
  function ceoTourNext() {
    var t = CEOS.tour; if (!t || t.i >= t.list.length) { CEOS.tour = null; return ceoGoSpot('work'); }
    var e = t.list[t.i++]; if (e.away || e.act) return ceoTourNext();
    var d = e.deskPos, ce = sideCx(e), T = { outPts: [V3(PML.hubX, PML.zl), V3(PML.hubX, LV.BZ), V3(ce, LV.BZ), V3(ce, d.az), V3(d.sx, d.az)] };
    ceoTag('Bố Linh · Đi kiểm tra', '#b8860b'); CEOS.cur = null;
    goPath(CEOC, ceoRoute('out', T), function () {
      ceoAt('out', d.sx, d.az, Math.PI, false, 'inspect', { outPts: T.outPts });
      if (!e.away && !e.act) { e.act = 'watched'; showCat(e, 'watched'); }
      CEOS.cur = { kind: 'inspect', until: Date.now() + rnd(4, 7) * 1000, e: e };
    });
  }
  function ceoChoose() {
    var now = Date.now();
    if (CEOS.punishQ.length && ceoPunishStart(CEOS.punishQ[0])) { CEOS.punishQ.shift(); return; }
    if (now > CEOS.nextTour && ceoTour()) return;
    var list = ['work', 'work', 'tv', 'bar', 'run', 'golf', 'massage', 'karaoke', 'aquarium', 'read'].filter(function (a) { return a !== CEOS.last; });
    ceoGoSpot(rpick(list));
  }
  function ceoBrain() {
    if (!CEOC || !SU || !PML || !LV) return; var c = CEOC, now = Date.now();
    if (CEOS.tag && CEOS.tag.parent) CEOS.tag.position.set(c.group.position.x, c.seated ? 3.9 : 4.5, c.group.position.z);
    if (now > CEOS.nextScan) {
      CEOS.nextScan = now + 20000;
      if (!CEOS.punishQ.length && now - CEOS.lastPunish > 240000) { var bp = PLANS.filter(function (p) { return planCat(p) === 'blocked'; })[0]; if (bp) ceoQueuePunish(bp.id); }
    }
    if (c.mv) return; var cur = CEOS.cur;
    if (!cur) return ceoChoose();
    if (cur.kind === 'punish') {
      if (now < cur.until) return; PMS.hold = false; pmAct(null); PMS.cur = null; CEOS.lastPunish = now; ceoAct(null); return ceoChoose();
    }
    if (cur.kind === 'inspect') {
      if (now < cur.until) return; var e = cur.e; if (e && e.act === 'watched') { e.act = null; showCat(e, e.cat || 'idle'); }
      ceoAct(null); CEOS.cur = null; return ceoTourNext();
    }
    if (cur.kind === 'rest') {
      if (CEOS.punishQ.length && PMC && !PMC.mv && !PMS.hold) return ceoChoose();
      if (now >= cur.until) ceoChoose();
    }
  }
  function placeCEO() {
    var cur = CEOS.cur; CEOS.tag = null; CEOS.tagText = ''; CEOS.tour = null; PMS.hold = false;
    var name = (cur && cur.kind === 'rest' && cur.act) || 'work', sp = SU.spots[name];
    ceoAt('suite', sp.x, sp.z, sp.rot, sp.seated, CEO_ACT[name], { spot: name });
    CEOS.cur = { kind: 'rest', act: name, until: Date.now() + rnd(8, 18) * 1000 }; CEOS.last = name; ceoTag('Bố Linh · ' + CEO_TEXT[name], '#b8860b');
  }
  function ceoTp(name) {
    var sp = SU.spots[name]; ceoAt('suite', sp.x, sp.z, sp.rot, sp.seated, CEO_ACT[name], { spot: name });
    CEOS.cur = { kind: 'rest', act: name, until: Date.now() + 90000 }; CEOS.last = name; ceoTag('Bố Linh · ' + CEO_TEXT[name], '#b8860b');
  }
  function buildSuite(hw, hd) {
    var sx0 = -hw + 0.6, sz0 = -hd + 0.6, SW = 17.5, SD = 12.5, doorZ = sz0 + 7.75;
    function X(u) { return sx0 + u; } function Z(v) { return sz0 + v; }
    SU = { sx0: sx0, sz0: sz0, SW: SW, SD: SD, hub: V3(X(9.0), Z(7.2)), doorIn: V3(X(SW - 0.8), doorZ), doorOut: V3(X(SW + 1.3), doorZ), outX: X(SW + 1.3), spots: {} };
    var glass = new THREE.MeshLambertMaterial({ color: '#9fd5ff', transparent: true, opacity: 0.2 });
    // sàn thảm + tường kính + neon
    var fl = bx(world, SW, 0.14, SD, '#35285a', X(SW / 2), 0.07, Z(SD / 2), true); fl.receiveShadow = true;
    bx(world, SW, 3.4, 0.08, glass, X(SW / 2), 1.8, Z(SD), true);
    var n1 = doorZ - 1.2 - sz0; bx(world, 0.08, 3.4, n1, glass, X(SW), 1.8, sz0 + n1 / 2, true);
    var n2 = sz0 + SD - (doorZ + 1.2); bx(world, 0.08, 3.4, n2, glass, X(SW), 1.8, doorZ + 1.2 + n2 / 2, true);
    COLL.push({ x1: X(0), x2: X(SW), z1: Z(SD) - 0.05, z2: Z(SD) + 0.05 }, { x1: X(SW) - 0.05, x2: X(SW) + 0.05, z1: sz0, z2: doorZ - 1.2 }, { x1: X(SW) - 0.05, x2: X(SW) + 0.05, z1: doorZ + 1.2, z2: sz0 + SD });
    [[X(0), Z(SD)], [X(SW), Z(SD)], [X(SW), doorZ - 1.2], [X(SW), doorZ + 1.2], [X(SW), sz0]].forEach(function (p) { bx(world, 0.2, 3.5, 0.2, '#f1f1f6', p[0], 1.75, p[1]); });
    neon(world, SW, 0.12, 0.12, '#ff3df0', X(SW / 2), 3.5, Z(SD), 5); neon(world, 0.12, 0.12, SD, '#ff3df0', X(SW), 3.5, sz0 + SD / 2, 5);
    neon(world, SW, 0.06, 0.06, '#19e3ff', X(SW / 2), 0.2, Z(SD) - 0.15, 5);
    signBoard('BỐ LINH', '#ff4fd8', 6.2, 1.3, X(6.8), 5.3, -hd + 0.06, 0);
    plight(world, '#ff4fd8', 1.0, 22, X(4), 4.5, Z(5)); plight(world, '#19e3ff', 0.9, 22, X(14), 4.5, Z(6)); plight(world, '#ffb347', 0.6, 12, X(8), 3, Z(10));
    var hr = new THREE.Mesh(new THREE.TorusGeometry(2.6, 0.04, 6, 40), new THREE.MeshBasicMaterial({ color: '#19e3ff' })); hr.rotation.x = Math.PI / 2; hr.position.set(X(9), 0.17, Z(7.2)); world.add(hr);
    // bàn làm việc + ghế (ký duyệt)
    var cd = new THREE.Group(); cd.position.set(X(4.4), 0, Z(3.6)); world.add(cd);
    bx(cd, 3.6, 0.14, 1.7, '#3a2a22', 0, 1.0, 0); bx(cd, 0.2, 0.95, 1.5, '#2d211b', -1.6, 0.5, 0); bx(cd, 0.2, 0.95, 1.5, '#2d211b', 1.6, 0.5, 0); bx(cd, 3.2, 0.9, 0.12, '#2d211b', 0, 0.55, 0.7);
    bx(cd, 0.9, 0.04, 0.6, '#9aa3b5', 0.6, 1.1, 0.1); var cl = bx(cd, 0.9, 0.56, 0.04, '#b7bfcf', 0.6, 1.4, -0.15); cl.rotation.x = -0.1;
    bx(cd, 1.2, 0.16, 0.5, '#ffd34d', -0.8, 1.15, 0.5); bx(cd, 1.0, 0.1, 0.34, '#2a2a30', -0.8, 1.26, 0.5, true);
    var chair = new THREE.Group(); chair.position.set(X(4.4), 0, Z(2.5)); world.add(chair);
    bx(chair, 1.1, 0.14, 1.1, '#7a1f2b', 0, 0.55, 0); bx(chair, 1.1, 1.3, 0.16, '#7a1f2b', 0, 1.2, -0.55); cyl(chair, 0.08, 0.08, 0.5, '#222', 0, 0.28, 0); cyl(chair, 0.5, 0.5, 0.06, '#222', 0, 0.04, 0, 6);
    SU.spots.work = { x: X(4.4), z: Z(2.4), rot: 0, seated: true, via: [V3(X(7.6), Z(3.6)), V3(X(7.6), Z(2.4)), V3(X(4.4), Z(2.4))] };
    // tủ sách
    for (var bay = 0; bay < 3; bay++) {
      var bxm = X(11.2 + bay * 2.2); bx(world, 2.1, 3.8, 0.7, '#5d4329', bxm, 1.9, -hd + 0.45);
      for (var sh = 0; sh < 4; sh++) { bx(world, 1.9, 0.08, 0.62, '#7a5a3a', bxm, 0.5 + sh * 0.9, -hd + 0.5); for (var bk = 0; bk < 7; bk++) bx(world, 0.2, 0.6 + (bk % 3) * 0.1, 0.4, ['#e0483e', '#3b82f6', '#f2c14e', '#3fa66a', '#a259ff', '#ff7a59', '#2ec4c6'][(bk + sh + bay) % 7], bxm - 0.8 + bk * 0.27, 0.9 + sh * 0.9, -hd + 0.55, true); }
    }
    SU.spots.read = { x: X(13.4), z: Z(2.4), rot: Math.PI, seated: false, via: [V3(X(13.4), Z(5.0)), V3(X(13.4), Z(2.4))] };
    // hồ cá
    var aq = new THREE.Group(); aq.position.set(X(8.5), 0, Z(1.1)); world.add(aq);
    bx(aq, 2.2, 1.0, 1.0, '#2d3347', 0, 0.5, 0); bx(aq, 2.2, 1.3, 0.9, new THREE.MeshLambertMaterial({ color: '#6fd6ff', transparent: true, opacity: 0.35 }), 0, 1.65, 0, true); neon(aq, 2.2, 0.08, 0.9, '#19e3ff', 0, 2.35, 0, 1.6);
    var fish = [['#ff7a59', 0], ['#ffd34d', 2], ['#ff6fa5', 4]].map(function (f) { var m = bx(aq, 0.22, 0.12, 0.08, f[0], 0, 1.7, 0, true); m.userData.o = f[1]; return m; });
    anim.push(function (t) { fish.forEach(function (m, i) { var o = m.userData.o; m.position.x = Math.sin(t * 0.6 + o) * 0.85; m.position.y = 1.35 + i * 0.28 + Math.sin(t * 1.3 + o) * 0.08; m.position.z = Math.sin(t * 0.4 + o) * 0.25; m.rotation.y = Math.cos(t * 0.6 + o) > 0 ? 0 : Math.PI; }); });
    SU.spots.aquarium = { x: X(8.5), z: Z(2.7), rot: Math.PI, seated: false, via: [V3(X(8.5), Z(5.0)), V3(X(8.5), Z(2.7))] };
    // ghế massage
    var mg = new THREE.Group(); mg.position.set(X(16.3), 0, Z(3.4)); world.add(mg);
    bx(mg, 1.4, 0.3, 1.7, '#2b2f66', 0, 0.45, 0); bx(mg, 0.5, 1.7, 1.5, '#3a3f8a', 0.55, 1.15, 0); bx(mg, 1.2, 0.15, 0.22, '#555a70', -0.05, 0.9, 0.85); bx(mg, 1.2, 0.15, 0.22, '#555a70', -0.05, 0.9, -0.85); bx(mg, 0.6, 0.25, 1.2, '#2b2f66', -0.95, 0.28, 0);
    neon(mg, 0.06, 1.4, 0.06, '#b46bff', 0.82, 1.2, 0.78, 0); neon(mg, 0.06, 1.4, 0.06, '#b46bff', 0.82, 1.2, -0.78, 0);
    SU.spots.massage = { x: X(16.2), z: Z(3.4), rot: -Math.PI / 2, seated: true, via: [V3(X(14.0), Z(5.0)), V3(X(14.0), Z(3.4)), V3(X(16.2), Z(3.4))] };
    // TV + sofa
    var rg = new THREE.Mesh(new THREE.BoxGeometry(5.2, 0.04, 4.8), M('#4b2a78')); rg.position.set(X(2.6), 0.16, Z(8.6)); rg.receiveShadow = true; world.add(rg);
    bx(world, 0.12, 1.9, 3.4, '#111', X(0.3), 2.3, Z(8.6)); bx(world, 0.6, 0.7, 3.6, '#2d211b', X(0.4), 0.35, Z(8.6));
    var tv = new THREE.Mesh(new THREE.PlaneGeometry(3.1, 1.6), new THREE.MeshBasicMaterial({ color: '#0a0d1a' })); tv.position.set(X(0.38), 2.3, Z(8.6)); tv.rotation.y = Math.PI / 2; world.add(tv);
    var sf = new THREE.Group(); sf.position.set(X(3.5), 0, Z(8.6)); world.add(sf);
    bx(sf, 1.4, 0.45, 3.0, '#7b3fa0', 0, 0.4, 0); bx(sf, 0.35, 1.05, 3.0, '#6a2f8f', 0.78, 0.95, 0); bx(sf, 1.4, 0.7, 0.35, '#6a2f8f', 0, 0.65, 1.6); bx(sf, 1.4, 0.7, 0.35, '#6a2f8f', 0, 0.65, -1.6);
    anim.push(function (t) { tv.material.color.set(CEOC && CEOC.act === 'tv' && !CEOC.mv ? ['#2a6bff', '#ff4fd8', '#19e3ff', '#ffd34d'][Math.floor(t * 1.5) % 4] : '#0a0d1a'); });
    SU.spots.tv = { x: X(3.4), z: Z(8.6), rot: -Math.PI / 2, seated: true, via: [V3(X(2.0), Z(6.0)), V3(X(2.0), Z(8.6)), V3(X(3.4), Z(8.6))] };
    // quầy bar
    var br = new THREE.Group(); br.position.set(X(8.0), 0, Z(11.7)); world.add(br);
    bx(br, 4.6, 1.1, 0.7, '#2d211b', 0, 0.55, 0); bx(br, 4.8, 0.1, 0.9, '#7a5a3a', 0, 1.15, 0); neon(br, 4.6, 0.06, 0.06, '#ffb347', 0, 0.85, -0.38, 1.6);
    ['#e0483e', '#3fa66a', '#ffd34d', '#a259ff', '#3b82f6'].forEach(function (c, i) { cyl(br, 0.1, 0.12, 0.5, c, -1.7 + i * 0.7, 1.45, 0, 8); cyl(br, 0.04, 0.05, 0.2, c, -1.7 + i * 0.7, 1.8, 0, 6); });
    [6.0, 8.3].forEach(function (u) { cyl(world, 0.3, 0.3, 0.1, '#ff3df0', X(u), 0.8, Z(10.95), 12); cyl(world, 0.05, 0.05, 0.75, '#555', X(u), 0.4, Z(10.95), 8); });
    neonText(world, 'BAR', '#ffb347', 2.2, 0.9, X(8.0), 2.8, Z(11.7), 0);
    SU.spots.bar = { x: X(7.15), z: Z(10.75), rot: 0, seated: false, via: [V3(X(7.15), Z(9.2)), V3(X(7.15), Z(10.75))] };
    // máy chạy bộ
    var tm = new THREE.Group(); tm.position.set(X(12.8), 0, Z(9.9)); world.add(tm);
    bx(tm, 1.2, 0.25, 2.8, '#222', 0, 0.18, 0);
    var beltTex = mkTex(64, 256, function (x, w, h) { x.fillStyle = '#2b2f3a'; x.fillRect(0, 0, w, h); x.fillStyle = '#4a5064'; for (var i = 0; i < 8; i++) x.fillRect(0, i * 32, w, 6); });
    beltTex.wrapS = beltTex.wrapT = THREE.RepeatWrapping; beltTex.repeat.set(1, 2);
    var belt = new THREE.Mesh(new THREE.BoxGeometry(1.0, 0.06, 2.5), new THREE.MeshLambertMaterial({ map: beltTex })); belt.position.set(0, 0.33, 0); tm.add(belt);
    bx(tm, 0.1, 1.2, 0.1, '#555', -0.5, 0.8, 1.2); bx(tm, 0.1, 1.2, 0.1, '#555', 0.5, 0.8, 1.2); bx(tm, 1.1, 0.1, 0.1, '#555', 0, 1.35, 1.2); var tms = bx(tm, 0.7, 0.4, 0.06, '#111', 0, 1.6, 1.25, true);
    neon(tm, 1.2, 0.05, 0.05, '#3ecf8e', 0, 0.35, 1.45, 0);
    anim.push(function (t, dt) { var on = CEOC && CEOC.act === 'run' && !CEOC.mv; if (on) beltTex.offset.y -= dt * 1.6; tms.material.color.set(on ? '#3ecf8e' : '#111'); });
    SU.spots.run = { x: X(12.8), z: Z(9.7), rot: 0, seated: false, via: [V3(X(12.8), Z(7.8)), V3(X(12.8), Z(9.7))] };
    // karaoke
    var kg = new THREE.Group(); kg.position.set(X(15.8), 0, Z(10.2)); world.add(kg);
    [-1.3, 1.3].forEach(function (o) { bx(kg, 0.8, 1.6, 0.7, '#1a1d2e', o, 0.8, 1.0); cyl(kg, 0.28, 0.28, 0.06, '#444a66', o, 0.55, 1.37, 14).rotation.x = Math.PI / 2; cyl(kg, 0.16, 0.16, 0.06, '#444a66', o, 1.2, 1.37, 12).rotation.x = Math.PI / 2; });
    var kr = new THREE.Mesh(new THREE.TorusGeometry(1.0, 0.04, 6, 36), new THREE.MeshBasicMaterial({ color: '#ff3df0' })); kr.rotation.x = Math.PI / 2; kr.position.set(0, 0.12, 0); kg.add(kr);
    var ball = new THREE.Mesh(new THREE.SphereGeometry(0.35, 10, 8), new THREE.MeshBasicMaterial({ color: '#cfd6ff' })); ball.position.set(0, 4.3, 0); kg.add(ball); bx(kg, 0.03, 0.8, 0.03, '#888', 0, 4.85, 0, true);
    var knk = neonText(world, 'KARAOKE', '#19e3ff', 3.6, 0.9, X(15.8), 3.1, Z(11.95), 0);
    var noteTex = mkTex(64, 64, function (x, w, h) { x.clearRect(0, 0, w, h); x.fillStyle = '#ffffff'; x.font = '800 54px ' + FONT; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillText('♪', 32, 34); });
    var notes = [0, 1, 2].map(function (i) { var sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: noteTex, color: ['#ff4fd8', '#19e3ff', '#ffd34d'][i], transparent: true, depthWrite: false })); sp.visible = false; world.add(sp); return sp; });
    anim.push(function (t) {
      var on = CEOC && CEOC.act === 'karaoke' && !CEOC.mv; ball.rotation.y = t * 1.5; kr.material.color.set(on ? ['#ff3df0', '#19e3ff', '#ffd34d'][Math.floor(t * 3) % 3] : '#ff3df0');
      notes.forEach(function (n, i) { if (!on) { n.visible = false; return; } var u = (t * 0.5 + i / 3) % 1; n.visible = true; n.position.set(CEOC.group.position.x + 0.5 + Math.sin(u * 7 + i) * 0.35, 2.0 + u * 1.6, CEOC.group.position.z + 0.2); n.scale.set(0.55, 0.55, 1); n.material.opacity = 1 - u; });
    });
    SU.spots.karaoke = { x: X(15.8), z: Z(10.2), rot: 0, seated: false, via: [V3(X(15.8), Z(8.4)), V3(X(15.8), Z(10.2))] };
    // golf
    bx(world, 5.6, 0.05, 1.0, '#2f8f4e', X(13.2), 0.08, Z(5.5), true); cyl(world, 0.13, 0.13, 0.03, '#111', X(15.6), 0.11, Z(5.5), 12); cyl(world, 0.02, 0.02, 1.1, '#eee', X(15.6), 0.65, Z(5.5), 6); bx(world, 0.4, 0.25, 0.02, '#e0483e', X(15.8), 1.05, Z(5.5), true);
    var gb = new THREE.Mesh(new THREE.SphereGeometry(0.07, 8, 6), new THREE.MeshBasicMaterial({ color: '#ffffff' })); gb.position.set(X(11.9), 0.14, Z(5.5)); world.add(gb);
    neon(world, 5.8, 0.04, 0.04, '#3ecf8e', X(13.2), 0.1, Z(6.05), 0);
    SU.spots.golf = { x: X(11.0), z: Z(5.5), rot: Math.PI / 2, seated: false, via: [V3(X(11.0), Z(5.5))] };
    // cây cảnh + thảm trang trí
    plant(world, X(1.1), Z(1.2), 1.2); plant(world, X(1.0), Z(11.6), 1.3); plant(world, X(17.0), Z(1.2), 1.1);
    // Bố Linh
    var ceo = makeChar({ shirt: '#1d2a4f', sleeve: '#1d2a4f', collar: '#f4f4f8', tie: '#c92a3a', pants: '#1d2433', shoes: '#111', hair: '#1c1c22', crown: true, seated: true, glasses: true });
    ceo.group.position.set(X(4.4), 0, Z(2.4)); ceo.root.position.y = 0.66; ceo.status = 'doing'; ceo.kind = 'ceo'; ceo.name = 'Bố Linh'; world.add(ceo.group); chars.push(ceo); CEOC = ceo;
  }
  /* ---------- mặt bằng tầng văn phòng: vách kính, hành lang, các phòng ----------
     Dãy sau: Bố Linh | Phòng nghỉ (PM) | hành lang | Phòng PM · Claude. Giữa: Dev floor. Dãy trước: Game room | Pantry | WC | Lobby.
     Mọi vách ghi vào COLL (đoạn thẳng trục x/z) để bài kiểm tra tự động soi đường đi. */
  var COLL = [], FP = null, GLASS_C = {};
  function glassMat(c) { var k = c || '#9fd5ff'; if (!GLASS_C[k]) GLASS_C[k] = new THREE.MeshLambertMaterial({ color: k, transparent: true, opacity: 0.2, depthWrite: false }); return GLASS_C[k]; }
  function postAt(x, z, h) { bx(world, 0.2, h + 0.1, 0.2, '#f1f1f6', x, (h + 0.1) / 2, z, true); }
  function wallPiece(horiz, fixed, a, b, o) {
    var h = o.h || 3.4, th = 0.1, len = b - a, mid = (a + b) / 2; if (len < 0.05) return;
    if (horiz) bx(world, len, h, th, o.mat, mid, h / 2, fixed, true); else bx(world, th, h, len, o.mat, fixed, h / 2, mid, true);
    COLL.push(horiz ? { x1: a, x2: b, z1: fixed - th / 2, z2: fixed + th / 2 } : { x1: fixed - th / 2, x2: fixed + th / 2, z1: a, z2: b });
    if (o.neon) neon(world, horiz ? len : 0.07, 0.07, horiz ? 0.07 : len, o.neon, horiz ? mid : fixed, h + 0.04, horiz ? fixed : mid, 0);
  }
  function partition(horiz, fixed, a, b, gaps, o) {
    o = o || {}; o.mat = o.mat || glassMat(o.tint); var h = o.h || 3.4, cur = a, g = (gaps || []).slice().sort(function (p, q) { return p[0] - q[0]; });
    postAt(horiz ? a : fixed, horiz ? fixed : a, h); postAt(horiz ? b : fixed, horiz ? fixed : b, h);
    g.forEach(function (gp) {
      wallPiece(horiz, fixed, cur, gp[0], o); cur = gp[1];
      var gl = gp[1] - gp[0], gm = (gp[0] + gp[1]) / 2;
      postAt(horiz ? gp[0] : fixed, horiz ? fixed : gp[0], h); postAt(horiz ? gp[1] : fixed, horiz ? fixed : gp[1], h);
      if (horiz) bx(world, gl, 0.5, 0.16, '#262b40', gm, 3.25, fixed, true); else bx(world, 0.16, 0.5, gl, '#262b40', fixed, 3.25, gm, true);
      neon(world, horiz ? gl : 0.06, 0.05, horiz ? 0.06 : gl, o.neon || '#19e3ff', horiz ? gm : fixed, 2.98, horiz ? fixed : gm, 0);
    });
    wallPiece(horiz, fixed, cur, b, o);
  }
  function signBoard(text, color, w, h, x, y, z, ry) {
    var g = new THREE.Group(); g.position.set(x, y, z); g.rotation.y = ry || 0; world.add(g);
    bx(g, w + 0.7, h + 0.3, 0.12, '#14172a', 0, 0, -0.05, true); neon(g, w + 0.7, 0.05, 0.05, color, 0, (h + 0.3) / 2, 0.02, 0); neon(g, w + 0.7, 0.05, 0.05, color, 0, -(h + 0.3) / 2, 0.02, 0);
    return neonText(g, text, color, w, h, 0, 0, 0.04, 0);
  }
  function patch(x1, z1, x2, z2, c, y) {
    var m = new THREE.Mesh(new THREE.BoxGeometry(x2 - x1, 0.03, z2 - z1), (c && c.isMaterial) ? c : M(c)); m.position.set((x1 + x2) / 2, y || 0.02, (z1 + z2) / 2); m.receiveShadow = true; world.add(m); return m;
  }
  function buildFloorplan(hw, hd) {
    var Zb = -hd + 13.1, ZF = hd - 7.6, DR = [-hd + 7.0, -hd + 9.2];
    FP = { hw: hw, hd: hd, Zb: Zb, ZF: ZF, hubX: 2.1, rowZ: ZF + 1.0, door: { game: -17.0, pantry: 2.2, wc: 11.4, lobby: 16.4 } };
    // sàn từng phòng
    patch(-9.9, -hd, -1.4, Zb, '#d9d0c0'); patch(5.6, -hd, hw, Zb, '#c3d0e4');
    patch(-hw, ZF, -5.2, hd, '#3d4273'); patch(-5.2, ZF, 9.4, hd, '#f1e6d0'); patch(9.4, ZF, 13.4, hd, '#cfe3f0'); patch(13.4, ZF, hw, hd, '#e8e2d4');
    patch(13.4, ZF, hw, ZF + 0.25, '#b9a98a', 0.025);
    // vách dãy sau (kính; cửa thông Bố ⇄ phòng nghỉ nằm ở tường kính của phòng Bố)
    partition(true, Zb, -9.9, -1.4, [], { neon: '#4f8cff' });
    partition(false, -1.4, -hd, Zb, [DR], { neon: '#4f8cff' });
    partition(false, 5.6, -hd, Zb, [DR], { neon: '#ff7a59' });
    partition(true, Zb, 5.6, hw, [], { neon: '#ff7a59' });
    // vách dãy trước
    partition(true, ZF, -hw, -5.2, [[-19, -15]], { tint: '#d6bfff', neon: '#b46bff' });
    partition(false, -5.2, ZF, hd, [], { tint: '#d6bfff', neon: '#b46bff' });
    partition(true, ZF, -5.2, 9.4, [[0.2, 4.2]], { tint: '#ffe0b8', neon: '#ffb347' });
    var wm = M('#e9eef5');
    partition(false, 9.4, ZF, hd, [], { mat: wm, h: 3.0 }); partition(true, ZF, 9.4, 13.4, [[10.7, 12.1]], { mat: wm, h: 3.0 }); partition(false, 13.4, ZF, hd, [], { mat: wm, h: 3.0 });
    partition(true, ZF, 13.4, hw, [[14.6, 18.2]], { tint: '#bfe9ff', neon: '#19e3ff' });
    // biển neon từng phòng
    var sg = function (t, c, w, x, y, z) { return signBoard(t, c, w, 1.0, x, y, z, 0); };
    sg('PHÒNG NGHỈ', '#4f8cff', 5.4, -5.65, 5.7, -hd + 0.06); sg('PM · CLAUDE', '#ff7a59', 6.0, 15.0, 5.7, -hd + 0.06);
    signBoard('DEV FLOOR', '#3ecf8e', 6.4, 1.2, -hw + 0.1, 6.2, -hd + 21, Math.PI / 2);
    sg('GAME ROOM', '#b46bff', 6.0, -17.0, 4.05, ZF + 0.1); sg('PANTRY', '#ffb347', 4.0, 2.2, 4.05, ZF + 0.1); sg('WC', '#19b5e8', 2.4, 11.4, 3.75, ZF + 0.1); sg('LOBBY', '#19e3ff', 3.6, 16.4, 4.05, ZF + 0.1);
  }
  function buildExtras(hw, hd) {
    var ZF = FP.ZF;
    // Game room: bàn bóng bàn, ghế lười
    var pt = new THREE.Group(); pt.position.set(-9.2, 0, hd - 4.8); world.add(pt);
    bx(pt, 3.4, 0.12, 1.9, '#1f6fb5', 0, 0.95, 0); [[-1.5, -0.8], [1.5, -0.8], [-1.5, 0.8], [1.5, 0.8]].forEach(function (p) { bx(pt, 0.1, 0.9, 0.1, '#333', p[0], 0.45, p[1]); });
    bx(pt, 0.04, 0.3, 1.9, '#ffffff', 0, 1.15, 0, true); neon(pt, 3.5, 0.04, 0.04, '#19e3ff', 0, 0.9, 1.0, 0);
    var pl = labelSprite('PING PONG', '#1f6fb5', 0.7); pl.position.set(0, 2.6, 0); pt.add(pl);
    [['#ff7a59', -7.0, hd - 1.6], ['#4f8cff', -8.5, hd - 1.0]].forEach(function (b) { var m = new THREE.Mesh(new THREE.SphereGeometry(0.75, 12, 10), M(b[0])); m.scale.set(1, 0.6, 1); m.position.set(b[1], 0.45, b[2]); m.castShadow = true; world.add(m); });
    // Pantry: máy bán nước, tủ lạnh, bàn ghế
    var vm = new THREE.Group(); vm.position.set(-3.8, 0, hd - 0.8); vm.rotation.y = Math.PI; world.add(vm);
    bx(vm, 1.4, 2.6, 1.1, '#d63a5a', 0, 1.3, 0); bx(vm, 1.0, 1.6, 0.05, '#2b3350', 0, 1.5, 0.56, true); neon(vm, 1.2, 0.06, 0.06, '#ffd34d', 0, 2.45, 0.58, 0);
    var fr = new THREE.Group(); fr.position.set(6.6, 0, hd - 0.9); world.add(fr);
    bx(fr, 1.5, 3.2, 1.2, '#d8dde8', 0, 1.6, 0); bx(fr, 1.46, 0.05, 0.06, '#8d95a8', 0, 1.9, 0.62, true); bx(fr, 0.07, 0.7, 0.07, '#8d95a8', 0.55, 2.5, 0.64, true); bx(fr, 0.07, 0.9, 0.07, '#8d95a8', 0.55, 1.2, 0.64, true); neon(fr, 1.4, 0.05, 0.05, '#19e3ff', 0, 3.25, 0.5, 0);
    var tl = new THREE.Group(); tl.position.set(-1.2, 0, hd - 4.2); world.add(tl);
    cyl(tl, 0.9, 0.9, 0.08, '#f1ece3', 0, 1.0, 0, 20); cyl(tl, 0.12, 0.2, 0.95, '#59606f', 0, 0.5, 0, 10);
    [[-1.4, 0], [1.4, 0], [0, -1.4]].forEach(function (o) { cyl(tl, 0.3, 0.3, 0.08, '#e0679b', o[0], 0.62, o[1], 12); cyl(tl, 0.05, 0.05, 0.6, '#555', o[0], 0.3, o[1], 6); });
    cyl(tl, 0.14, 0.12, 0.2, '#f4a6c0', 0.3, 1.14, 0.2, 10);
    buildLobby(hw, hd);
    // neon + ánh sáng theo phòng
    neon(world, 7.8, 0.08, 0.08, '#4f8cff', -5.65, 2.3, -hd + 0.22, 4); plight(world, '#4f8cff', 0.7, 16, -5.65, 3.5, -hd + 6);
    neon(world, 4.6, 0.08, 0.08, '#ff3d81', -12.8, 3.55, hd - 0.4, 3); plight(world, '#ff3d81', 0.8, 14, -12.8, 3.5, hd - 2);
    plight(world, '#ffb347', 0.5, 14, 2.2, 3.2, hd - 3); plight(world, '#19e3ff', 0.6, 14, 20.5, 3.4, hd - 3);
    plight(world, '#3ecf8e', 0.6, 14, hw - 3, 3, -hd + 22);
  }
  function buildLobby(hw, hd) {
    var ZF = FP.ZF;
    // quầy lễ tân MUSUDE
    var rc = new THREE.Group(); rc.position.set(21.7, 0, hd - 3.4); world.add(rc);
    bx(rc, 5.8, 1.1, 1.2, '#2b3150', 0, 0.55, 0); bx(rc, 6.0, 0.1, 1.4, '#e8e3d8', 0, 1.15, 0);
    neonText(rc, 'MUSUDE', '#ff4fd8', 3.6, 0.8, 0, 0.62, 0.62, 0); neon(rc, 5.8, 0.06, 0.06, '#ff4fd8', 0, 0.12, 0.62, 0);
    var mo = bx(rc, 0.9, 0.56, 0.05, '#aeb5c4', -1.3, 1.6, -0.25); mo.rotation.x = -0.1; bx(rc, 0.9, 0.03, 0.5, '#8b92a1', -1.3, 1.22, -0.1, true);
    cyl(rc, 0.14, 0.14, 0.06, '#ffd34d', 1.2, 1.25, 0, 10); bx(rc, 0.5, 0.04, 0.35, '#3ecf8e', 0.2, 1.22, -0.1, true);
    var cs = new THREE.Group(); cs.position.set(21.7, 0, hd - 5.2); world.add(cs);
    bx(cs, 0.9, 0.12, 0.9, '#3b4256', 0, 0.55, 0); bx(cs, 0.9, 0.9, 0.12, '#3b4256', 0, 1.0, -0.45); cyl(cs, 0.07, 0.07, 0.5, '#222', 0, 0.28, 0); cyl(cs, 0.45, 0.45, 0.05, '#222', 0, 0.04, 0, 6);
    // sofa chờ + bàn nước
    var sf = new THREE.Group(); sf.position.set(25.2, 0, ZF + 1.4); world.add(sf);
    bx(sf, 3.8, 0.5, 1.2, '#3b82f6', 0, 0.4, 0); bx(sf, 3.8, 0.9, 0.3, '#2f6fd0', 0, 0.95, -0.5); bx(sf, 0.3, 0.85, 1.2, '#2f6fd0', -1.85, 0.65, 0); bx(sf, 0.3, 0.85, 1.2, '#2f6fd0', 1.85, 0.65, 0);
    bx(sf, 1.4, 0.45, 0.8, '#7a5a3a', 0, 0.25, 1.6); bx(sf, 0.5, 0.18, 0.4, '#ffffff', 0, 0.55, 1.6, true);
    plant(world, 14.5, hd - 1.2, 1.3);
    // cửa chính ENTRANCE trên tường phải
    var ez = hd - 2.3, fm = '#2a2f45';
    bx(world, 0.3, 3.1, 0.2, fm, hw - 0.15, 1.55, ez - 1.6, true); bx(world, 0.3, 3.1, 0.2, fm, hw - 0.15, 1.55, ez + 1.6, true); bx(world, 0.3, 0.3, 3.4, fm, hw - 0.15, 3.0, ez, true);
    bx(world, 0.1, 2.8, 1.5, glassMat('#9fe3ff'), hw - 0.15, 1.4, ez - 0.76, true); bx(world, 0.1, 2.8, 1.5, glassMat('#9fe3ff'), hw - 0.15, 1.4, ez + 0.76, true);
    bx(world, 0.08, 0.9, 0.07, '#d9dee8', hw - 0.25, 1.3, ez - 0.12, true); bx(world, 0.08, 0.9, 0.07, '#d9dee8', hw - 0.25, 1.3, ez + 0.12, true);
    patch(hw - 2.8, ez - 1.5, hw - 0.3, ez + 1.5, '#2f3647', 0.04);
    signBoard('ENTRANCE', '#3ecf8e', 4.2, 0.95, hw - 2.6, 4.3, ez, 0);
    plight(world, '#3ecf8e', 0.5, 10, hw - 2, 3.2, ez);
  }
  function buildLeisure(hw, hd, cx0, cz0) {
    var pl = chars.filter(function (c) { return c.kind === 'plan'; }), xs = pl.map(function (c) { return c.deskPos.sx; });
    var mn = xs.length ? Math.min.apply(null, xs) : -8, mx = xs.length ? Math.max.apply(null, xs) : 8, sx0 = -hw + 6.8, ZF = FP.ZF, rowZ = FP.rowZ, DD = FP.door;
    LV = { CL: Math.max(-hw + 1.8, mn - 2.7), CR: Math.min(hw - 2.6, mx + 1.9), FZ: hd - 8.9, BZ: -hd + BACKV - 1.2, cx0: cx0, cz0: cz0, slots: { sofa: [], game: [], tea: [] }, wc: null };
    DIR.nextL = 6; DIR.nextWC = 25; DIR.nextTour = 35;
    // hành lang sáng dọc các lối đi chính
    var BZ = LV.BZ, FZ = LV.FZ, strips = [[-1.4, -hd, 5.6, BZ + 1.0], [-hw + 0.4, BZ - 1.0, hw - 0.4, BZ + 1.0], [-hw + 0.4, FZ - 1.0, hw - 0.4, FZ + 1.0], [LV.CL - 1.0, BZ, LV.CL + 1.0, FZ], [LV.CR - 1.0, BZ, LV.CR + 1.0, FZ],
      [DD.game - 2.2, FZ, DD.game + 2.2, rowZ + 0.9], [DD.pantry - 2.2, FZ, DD.pantry + 2.2, rowZ + 0.9], [DD.wc - 1.0, FZ, DD.wc + 1.0, ZF + 0.9], [DD.lobby - 2.0, FZ, DD.lobby + 2.0, rowZ + 0.9]];
    strips.forEach(function (r) { patch(Math.max(r[0] - 0.18, -hw), Math.max(r[1] - 0.18, -hd), Math.min(r[2] + 0.18, hw), r[3] + 0.18, '#8fa4cc', 0.025); });
    strips.forEach(function (r) { patch(r[0], Math.max(r[1], -hd), r[2], r[3], '#d8e3ff', 0.03); });
    [-1.5, 0, 1.5].forEach(function (o) { LV.slots.sofa.push({ x: sx0 + o, z: hd - 3.35, appr: hd - 5.6, door: DD.game, rowZ: rowZ, busy: null }); });
    // máy game
    [-14.0, -11.6].forEach(function (ax, i) {
      var g = new THREE.Group(); g.position.set(ax, 0, hd - 0.95); world.add(g);
      bx(g, 1.5, 2.7, 1.2, '#2b2f66', 0, 1.35, 0); bx(g, 1.6, 0.5, 1.4, '#ff3d81', 0, 2.75, -0.05); bx(g, 1.3, 0.12, 0.5, '#1a1d3d', 0, 1.05, -0.8);
      cyl(g, 0.05, 0.05, 0.3, '#dddddd', -0.25, 1.25, -0.8, 8); bx(g, 0.14, 0.1, 0.14, '#ff5252', 0.2, 1.15, -0.8, true); bx(g, 0.14, 0.1, 0.14, '#ffd34d', 0.45, 1.15, -0.8, true);
      var scr = new THREE.Mesh(new THREE.PlaneGeometry(1.1, 0.85), new THREE.MeshBasicMaterial({ color: '#0b1030' })); scr.position.set(0, 1.75, -0.62); scr.rotation.y = Math.PI; g.add(scr);
      var slot = { x: ax, z: hd - 3.0, door: DD.game, rowZ: rowZ, busy: null }; LV.slots.game.push(slot);
      anim.push(function (t) { scr.material.color.set(slot.busy && slot.busy.act === 'game' ? ['#3ecf8e', '#ffd34d', '#4f8cff', '#ff6fa5'][Math.floor(t * 6) % 4] : '#0b1030'); });
    });
    var al = labelSprite('ARCADE', '#6a2c91', 0.9); al.position.set(-12.8, 4.3, hd - 0.95); world.add(al);
    // quầy trà sữa (Pantry)
    var tb = new THREE.Group(); tb.position.set(2.4, 0, hd - 1.0); world.add(tb);
    bx(tb, 4.8, 1.1, 1.2, '#f6c8d6', 0, 0.55, 0); bx(tb, 5.0, 0.12, 1.4, '#7a5a3a', 0, 1.16, 0);
    [['#e8c9a0', -1.6], ['#f4a6c0', -0.6], ['#b8e0c0', 0.4], ['#f8e8a0', 1.4]].forEach(function (u) { cyl(tb, 0.2, 0.16, 0.5, u[0], u[1], 1.47, -0.1, 12); cyl(tb, 0.21, 0.21, 0.05, '#ffffff', u[1], 1.74, -0.1, 12); });
    var ts = labelSprite('TRÀ SỮA', '#d6538b', 1.0); ts.position.set(0, 3.0, 0); tb.add(ts);
    [1.2, 3.6].forEach(function (x) { LV.slots.tea.push({ x: x, z: hd - 2.7, door: DD.pantry, rowZ: rowZ, busy: null }); });
    // WC: phòng kín, cửa trượt ở vách phía bắc
    var wx = 11.4, wz = hd - 3.8, wcg = new THREE.Group(); wcg.position.set(wx, 0, wz); world.add(wcg);
    var wc = { x: wx, doorZ: ZF - 0.3, seatZ: wz + 2.65, visitor: null }; LV.wc = wc;
    var door = bx(wcg, 1.4, 2.5, 0.1, '#6ea8d8', 0, 1.25, -3.9);
    bx(wcg, 0.8, 0.5, 0.9, '#ffffff', 0, 0.3, 2.9); bx(wcg, 0.8, 0.9, 0.3, '#ffffff', 0, 0.9, 3.5);
    [0.2, 1.4].forEach(function (z) { bx(wcg, 0.5, 0.15, 0.8, '#ffffff', -1.7, 0.95, z); bx(wcg, 0.3, 0.9, 0.3, '#d0d8e0', -1.8, 0.45, z); });
    bx(wcg, 0.06, 0.9, 2.2, '#bfe6ff', -1.94, 1.9, 0.8);
    var lamp = new THREE.Mesh(new THREE.SphereGeometry(0.2, 12, 8), new THREE.MeshBasicMaterial({ color: '#3ecf8e' })); lamp.position.set(0, 3.3, -3.8); wcg.add(lamp);
    var open = 0;
    anim.push(function (t, dt) {
      var v = wc.visitor, near = v && v.act !== 'wc' && Math.hypot(v.group.position.x - wx, v.group.position.z - wc.doorZ) < 2.4 && v.group.position.z > wc.doorZ - 2.4;
      open += ((near ? 1 : 0) - open) * Math.min(1, dt * 6); door.position.x = open * 1.45; lamp.material.color.set(v ? '#f36c64' : '#3ecf8e');
    });
  }

  /* ---------- tương tác ---------- */
  var ray = new THREE.Raycaster(), mouse = new THREE.Vector2(), hover = null, down = null;
  var tip = $('tip'), card = $('card');
  function pick(e) {
    var r = renderer.domElement.getBoundingClientRect(); mouse.set(((e.clientX - r.left) / r.width) * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1);
    ray.setFromCamera(mouse, camera); var hit = ray.intersectObjects(pickMeshes, false)[0]; return hit ? hit.object.userData.charRef : null;
  }
  function hitLogo(e) {
    if (!logoMesh) return false; var r = renderer.domElement.getBoundingClientRect(); mouse.set(((e.clientX - r.left) / r.width) * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1);
    ray.setFromCamera(mouse, camera); return ray.intersectObject(logoMesh, false).length > 0;
  }
  renderer.domElement.addEventListener('pointermove', function (e) {
    if (e.buttons) { tip.style.display = 'none'; return; }
    var c = pick(e); hover = c; var onLogo = !c && hitLogo(e); renderer.domElement.style.cursor = (c || onLogo) ? 'pointer' : 'grab';
    if (onLogo) { tip.style.display = 'block'; tip.style.left = (e.clientX + 14) + 'px'; tip.style.top = (e.clientY + 12) + 'px'; tip.textContent = 'Quay lại 2D'; return; }
    if (c) { tip.style.display = 'block'; tip.style.left = (e.clientX + 14) + 'px'; tip.style.top = (e.clientY + 12) + 'px'; tip.textContent = c.name + (c.cat ? ' · ' + CAT_TEXT[c.cat] : ''); } else tip.style.display = 'none';
  });
  renderer.domElement.addEventListener('pointerdown', function (e) { down = { x: e.clientX, y: e.clientY }; });
  renderer.domElement.addEventListener('pointerup', function (e) {
    if (!down || Math.hypot(e.clientX - down.x, e.clientY - down.y) > 5) return; var c = pick(e); if (c) showCard(c); else if (hitLogo(e)) { try { window.parent.postMessage({ o3d: 'exit' }, '*'); } catch (x) {} } else hideCard();
  });
  function hideCard() { card.style.display = 'none'; SELECTED = null; }
  function showCard(c) {
    SELECTED = c.kind === 'plan' ? c.plan.id : c.kind; card.style.display = 'block';
    if (c.kind === 'ceo') { card.innerHTML = '<button class="x" id="cx">×</button><h2>Bố Linh · CEO</h2><p class="muted">Người giao việc. Phòng riêng ở góc trái phía sau: bar, karaoke, golf, máy chạy bộ, ghế massage, hồ cá, TV. Task treo thì ra đấm Claude.</p>'; }
    else if (c.kind === 'pm') { card.innerHTML = '<button class="x" id="cx">×</button><h2>PM · Claude</h2><p class="muted">Điều phối Muse, review và cập nhật trạng thái mission. Hay đi lại giữa bàn, bảng trắng và máy cà phê.</p>'; }
    else {
      var p = c.plan, n = counts(p), cat = planCat(p);
      card.innerHTML = '<button class="x" id="cx">×</button><h2>' + esc(p.title || p.id) + '</h2><div class="mono">' + esc(p.id) + '</div><div class="chip" style="color:' + CAT_COLOR[cat] + '">● ' + CAT_TEXT[cat] + '</div><div class="bar"><i style="width:' + (n.n ? 100 * n.d / n.n : 0) + '%"></i></div><div class="muted">' + n.d + '/' + n.n + ' task xong</div>' +
        p.tasks.map(function (t) { var tc = t.status === 'done' ? '#3ecf8e' : t.status === 'doing' ? '#f2a650' : t.status === 'blocked' ? '#f36c64' : '#8d919c'; return '<div class="trow"><span class="d" style="background:' + tc + '"></span><b>' + esc(t.id) + '</b> ' + esc(t.title || '') + '</div>'; }).join('');
    }
    $('cx').onclick = hideCard;
  }

  /* ---------- nạp dữ liệu ---------- */
  function refreshAll(force) {
    var s = structSig();
    if (force || s !== sig) { sig = s; build(); }
    else { chars.forEach(function (c) { if (c.kind === 'plan') { var np = PLANS.filter(function (p) { return p.id === c.plan.id; })[0]; if (np) applyPlan(c, np, false); } }); Object.keys(boards).forEach(function (k) { if (boards[k].redraw) boards[k].redraw(); }); }
    if (SELECTED && SELECTED !== 'ceo' && SELECTED !== 'pm') { var cc = chars.filter(function (c) { return c.kind === 'plan' && c.plan.id === SELECTED; })[0]; if (cc) showCard(cc); }
  }
  var tick = 0;
  function load() {
    api('/api/activity?limit=30').then(function (r) { onEvents(r.events); }).catch(function () { });
    Promise.all([api('/api/plans'), api('/api/state')]).then(function (r) {
      PLANS = r[0].plans || []; STATE = r[1]; ready = true; refreshAll(false);
    }).catch(function () { });
    if (tick++ % 6 === 0) {
      api('/api/usage').then(function (u) { USAGE = u; if (boards.u5) { boards.u5.redraw(); boards.u7.redraw(); } }).catch(function () { });
      api('/api/trash').then(function (j) { TRASH = (j.items || []).length; if (boards.bin) boards.bin.redraw(); }).catch(function () { });
    }
  }

  /* ---------- vòng lặp ---------- */
  var clock = new THREE.Clock(), wp = new THREE.Vector3();
  function resize() { var w = wrap.clientWidth || 800, h = wrap.clientHeight || 600; renderer.setSize(w, h, false); renderer.domElement.style.width = '100%'; renderer.domElement.style.height = '100%'; camera.aspect = w / h; camera.updateProjectionMatrix(); }
  window.addEventListener('resize', resize); resize();
  function step(dt, t) {
    controls.update();
    var tx = Math.max(-ROOM.W / 2, Math.min(ROOM.W / 2, controls.target.x)), tz = Math.max(-ROOM.D / 2, Math.min(ROOM.D / 2, controls.target.z)); controls.target.x = tx; controls.target.z = tz;
    walls.forEach(function (w) { wp.copy(camera.position).sub(w.mesh.position); var outside = wp.dot(w.n) < 0; w.mesh.material.opacity += ((outside ? 0.07 : 1) - w.mesh.material.opacity) * Math.min(1, dt * 8); w.mesh.visible = w.mesh.material.opacity > 0.03; });
    anim.forEach(function (f) { f(t, dt); }); updateScreens(t);
    director(dt); pmBrain(); ceoBrain();
    chars.forEach(function (c) { var moving = c.mv ? stepMove(c, t, dt) : (c.walk ? walkPM(c, t, dt) : false); if (!moving) animateChar(c, t, dt); else { c.armL.rotation.x = c.cur.aLx; c.armR.rotation.x = c.cur.aRx; c.armL.rotation.z = 0.05; c.armR.rotation.z = -0.05; c.head.rotation.y = 0; c.head.rotation.x = 0; }
      if (c.bubble) { c.bubble.position.y = 3.2 + Math.sin(t * 2 + c.phase) * 0.06; if (c.deskPos) { if (c.tag) { c.tag.visible = !!c.away; c.tag.position.set(c.group.position.x, 4.1, c.group.position.z); } c.bubble.position.x = c.group.position.x; c.bubble.position.z = c.group.position.z; } } });
  }
  function frame() { requestAnimationFrame(frame); if (document.hidden) return; step(Math.min(clock.getDelta(), 0.1), clock.elapsedTime); renderer.render(scene, camera); }
  frame(); load(); setInterval(load, 5000);
  window.__office3d = { ceo: { state: CEOS, tp: function (act) { ceoTp(act); }, punish: function (id) { ceoQueuePunish(id || 'test', true); }, choose: function () { ceoChoose(); } }, pm: { events: onEvents, state: PMS, choose: function () { return pmChoose(); }, tp: function (act) { var sp = pmSpot(act); pmAt('lounge', sp.x, sp.z, sp.rot, sp.seated, act); PMS.cur = { kind: 'rest', act: act, until: Date.now() + 90000 }; pmTag(ACT_TEXT[act], '#3ecf8e'); } }, force: forceTest, resetCamera: resetView, controls: controls, camera: camera, count: function () { return chars.length; }, step: step, coll: function () { return COLL; }, fp: function () { return FP; }, lv: function () { return LV; }, chars: function () { return chars; }, room: function () { return ROOM; } };
})();

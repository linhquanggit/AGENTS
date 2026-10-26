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
  var CAT_COLOR = { done: '#3ecf8e', blocked: '#f36c64', waiting: '#ad92ff', doing: '#f2a650', idle: '#8d919c' };
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
  var scene = new THREE.Scene(); scene.background = new THREE.Color('#0d1018');
  var camera = new THREE.PerspectiveCamera(38, 1, 0.5, 500);
  var controls = new THREE.OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true; controls.dampingFactor = 0.08; controls.screenSpacePanning = false;
  controls.maxPolarAngle = 1.38; controls.minPolarAngle = 0.3; controls.minAzimuthAngle = -1.0; controls.maxAzimuthAngle = 1.0;
  controls.minDistance = 8; controls.maxDistance = 120;
  scene.add(new THREE.HemisphereLight(0xffffff, 0x8a93a8, 0.85));
  var sun = new THREE.DirectionalLight(0xfff3e0, 0.7); sun.castShadow = true; sun.shadow.mapSize.set(2048, 2048); sun.shadow.bias = -0.0004; scene.add(sun); scene.add(sun.target);
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
  function clearWorld() { while (world.children.length) { var c = world.children.pop(); c.traverse(function (o) { if (o.geometry) o.geometry.dispose(); }); } chars = []; anim = []; boards = {}; walls = []; pickMeshes = []; bubbleOf = {}; }
  function pickPlans() {
    var act = PLANS.filter(function (p) { return planCat(p) !== 'done'; }), done = PLANS.filter(function (p) { return planCat(p) === 'done'; });
    var shown = SHOW_ALL ? done : done.slice(0, Math.max(0, 14 - act.length));
    return { list: act.concat(shown), hidden: done.length - shown.length };
  }
  function structSig() { var s = pickPlans(); return s.list.map(function (p) { return p.id; }).join('|') + '#' + s.hidden + '#' + SHOW_ALL; }

  var ROOM = { W: 40, D: 40 };
  function build() {
    clearWorld();
    var sel = pickPlans(), groups = {}, order = [];
    sel.list.forEach(function (p) { var k = groupKey(p); (groups[k] = groups[k] || []).push(p); });
    var other = []; Object.keys(groups).forEach(function (k) { if (groups[k].length < 2) { other = other.concat(groups[k]); delete groups[k]; } });
    Object.keys(groups).sort().forEach(function (k) { order.push({ key: k.toUpperCase(), plans: groups[k] }); });
    if (other.length) order.push({ key: 'KHÁC', plans: other });

    // bố cục các khu
    var COLS = 4, PX = 3.45, PZ = 3.6, GAP = 2.2, W = 40, rowW = W - 4;
    order.forEach(function (z) { z.cols = Math.min(COLS, z.plans.length); z.rows = Math.ceil(z.plans.length / COLS); z.w = z.cols * PX + 1.2; z.d = z.rows * PZ + 1.6; });
    var rowsZ = [], cur = { x: 0, h: 0, items: [] };
    order.forEach(function (z) { if (cur.x + z.w > rowW && cur.items.length) { rowsZ.push(cur); cur = { x: 0, h: 0, items: [] }; } z.rx = cur.x; cur.x += z.w + GAP; cur.h = Math.max(cur.h, z.d); cur.items.push(z); });
    if (cur.items.length) rowsZ.push(cur);
    var deskDepth = rowsZ.reduce(function (a, r) { return a + r.h + GAP; }, 0);
    var BACK = 16, D = Math.max(34, BACK + deskDepth + 8);
    ROOM = { W: W, D: D };
    var hw = W / 2, hd = D / 2, WH = 7.2;

    // sàn + tường
    var floorTex = mkTex(512, 512, function (x, w, h) {
      for (var r = 0; r < 16; r++) { var off = (r * 97) % 256, y = r * 32; for (var seg = 0; seg < 3; seg++) { var sx = ((off + seg * 256) % 512) - 256 + 0, tone = 0.92 + seeded(r * 5 + seg) * 0.12; x.fillStyle = 'rgb(' + Math.round(196 * tone) + ',' + Math.round(158 * tone) + ',' + Math.round(116 * tone) + ')'; x.fillRect(sx, y, 255, 31); x.fillRect(sx + 512, y, 255, 31); } }
      x.fillStyle = 'rgba(60,35,15,.25)'; for (var l = 0; l < 16; l++) x.fillRect(0, l * 32 + 31, w, 1);
    });
    floorTex.wrapS = floorTex.wrapT = THREE.RepeatWrapping; floorTex.repeat.set(W / 8, D / 8);
    var floor = new THREE.Mesh(new THREE.PlaneGeometry(W, D), new THREE.MeshLambertMaterial({ map: floorTex })); floor.rotation.x = -Math.PI / 2; floor.receiveShadow = true; world.add(floor);
    bx(world, W + 1.2, 0.4, D + 1.2, '#1a1f2c', 0, -0.22, 0, true);
    function wall(w, x, z, ry, name) {
      var m = new THREE.Mesh(new THREE.BoxGeometry(w, WH, 0.35), new THREE.MeshLambertMaterial({ color: '#eae6f2', transparent: true, opacity: 1 }));
      m.position.set(x, WH / 2, z); m.rotation.y = ry; m.receiveShadow = true; world.add(m);
      bx(m, w, 0.5, 0.42, '#6a5acd', 0, -WH / 2 + 0.25, 0, true);
      walls.push({ mesh: m, n: new THREE.Vector3(Math.sin(ry), 0, Math.cos(ry)), name: name }); return m;
    }
    wall(W, 0, -hd - 0.17, 0, 'back'); wall(W, 0, hd + 0.17, Math.PI, 'front'); wall(D, -hw - 0.17, 0, Math.PI / 2, 'left'); wall(D, hw + 0.17, 0, -Math.PI / 2, 'right');

    // logo + bảng thông tin trên tường sau
    var logo = new THREE.Mesh(new THREE.PlaneGeometry(9, 1.5), new THREE.MeshBasicMaterial({ map: mkTex(1024, 170, drawLogo), transparent: true })); logo.position.set(-hw + 18.5, 6.0, -hd + 0.03); world.add(logo);
    var by = 3.55, bz = -hd + 0.12, bx0 = -hw + 12.2;
    boards.mcli = panel(world, 4.0, 2.1, bx0, by, bz, 0, drawMcli, 640); boards.poll = panel(world, 4.0, 2.1, bx0 + 4.7, by, bz, 0, drawPoll, 640);
    boards.u5 = panel(world, 4.0, 2.1, bx0 + 9.4, by, bz, 0, drawGauge('5 GIỜ', 'five_hour'), 640); boards.u7 = panel(world, 4.0, 2.1, bx0 + 14.1, by, bz, 0, drawGauge('TUẦN', 'seven_day'), 640);
    boards.auto = panel(world, 4.0, 2.1, bx0 + 18.8, by, bz, 0, drawAuto, 640);
    boards.chat = panel(world, 5.2, 2.5, hw - 4.2, 3.7, bz, 0, drawChat, 800);

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
    var bin = new THREE.Group(); bin.position.set(hw - 1.3, 0, hd - 3); world.add(bin);
    cyl(bin, 0.62, 0.5, 1.3, '#5b6275', 0, 0.65, 0, 14); cyl(bin, 0.68, 0.68, 0.1, '#475063', 0, 1.35, 0, 14);
    boards.bin = { group: bin, plane: null };
    var binPlane = new THREE.Mesh(new THREE.PlaneGeometry(1.5, 1.1), new THREE.MeshBasicMaterial({ map: mkTex(256, 190, drawBin), transparent: true })); binPlane.position.set(-0.5, 2.3, 0.2); binPlane.rotation.y = -Math.PI / 2 + 0.6; bin.add(binPlane); boards.bin.tex = binPlane.material.map; boards.bin.draw = drawBin; boards.bin.redraw = function () { redraw(boards.bin.tex, drawBin); };

    // tường trái: bảng ghim việc, bảng cài đặt, đồng hồ
    var lx = -hw + 0.12, lz0 = -hd + 17;
    boards.tasks = panel(world, 4.6, 3.4, lx, 3.7, lz0, Math.PI / 2, drawTasks, 640); boards.settings = panel(world, 3.4, 2.2, lx, 3.7, lz0 + 6, Math.PI / 2, drawSettings, 512);
    boardClock(world, lx + 0.12, 4.3, lz0 + 11, Math.PI / 2); plant(world, -hw + 1.4, lz0 + 14, 1.3); plant(world, hw - 1.6, hd - 2, 1.2); plant(world, -hw + 1.4, hd - 2, 1.4);

    // phòng kính của BỐ (CEO)
    var cx0 = -hw + 5.2, cz0 = -hd + 4.4; var glass = new THREE.MeshLambertMaterial({ color: '#9fd5ff', transparent: true, opacity: 0.2 });
    bx(world, 9.6, 0.2, 9.6, '#5b4a8a', cx0 - 0.0, 0.1, cz0, true).receiveShadow = true;
    bx(world, 9.6, 3.4, 0.08, glass, cx0, 1.8, cz0 + 4.8, true); bx(world, 0.08, 3.4, 9.6, glass, cx0 + 4.8, 1.8, cz0, true);
    [[cx0 - 4.8, cz0 + 4.8], [cx0 + 4.8, cz0 + 4.8], [cx0 + 4.8, cz0 - 4.8]].forEach(function (p) { bx(world, 0.2, 3.5, 0.2, '#f1f1f6', p[0], 1.75, p[1]); });
    bx(world, 9.8, 0.2, 0.2, '#f1f1f6', cx0, 3.5, cz0 + 4.8); bx(world, 0.2, 0.2, 9.8, '#f1f1f6', cx0 + 4.8, 3.5, cz0);
    var cd = new THREE.Group(); cd.position.set(cx0 - 0.4, 0, cz0 - 0.4); world.add(cd);
    bx(cd, 3.6, 0.14, 1.7, '#3a2a22', 0, 1.0, 0); bx(cd, 0.2, 0.95, 1.5, '#2d211b', -1.6, 0.5, 0); bx(cd, 0.2, 0.95, 1.5, '#2d211b', 1.6, 0.5, 0); bx(cd, 3.2, 0.9, 0.12, '#2d211b', 0, 0.55, 0.7);
    bx(cd, 0.9, 0.04, 0.6, '#9aa3b5', 0.6, 1.1, 0.1); var cl = bx(cd, 0.9, 0.56, 0.04, '#b7bfcf', 0.6, 1.4, -0.15); cl.rotation.x = -0.1;
    bx(cd, 1.2, 0.16, 0.5, '#ffd34d', -0.8, 1.15, 0.5); bx(cd, 1.0, 0.1, 0.34, '#2a2a30', -0.8, 1.26, 0.5, true);
    var chair = new THREE.Group(); chair.position.set(cx0 - 0.4, 0, cz0 - 1.5); world.add(chair); bx(chair, 1.1, 0.14, 1.1, '#7a1f2b', 0, 0.55, 0); bx(chair, 1.1, 1.3, 0.16, '#7a1f2b', 0, 1.2, -0.55); cyl(chair, 0.08, 0.08, 0.5, '#222', 0, 0.28, 0); cyl(chair, 0.5, 0.5, 0.06, '#222', 0, 0.04, 0, 6);
    var ceo = makeChar({ shirt: '#1d2a4f', sleeve: '#1d2a4f', collar: '#f4f4f8', tie: '#c92a3a', pants: '#1d2433', shoes: '#111', hair: '#1c1c22', crown: true, seated: true, glasses: true });
    ceo.group.position.set(cx0 - 0.4, 0.0, cz0 - 1.55); ceo.group.rotation.y = 0; ceo.root.position.y = 0.66; ceo.status = 'doing'; ceo.kind = 'ceo'; ceo.name = 'BỐ · CEO'; world.add(ceo.group); chars.push(ceo);
    var ceoLab = labelSprite('BỐ · CEO', '#b8860b', 1.0); ceoLab.position.set(cx0 - 0.4, 4.9, cz0 - 1.5); world.add(ceoLab);
    plant(world, cx0 + 3.6, cz0 - 3.6, 1.0);

    // bàn PM (Claude) + bảng trắng
    var pmx = hw - 12, pmz = -hd + 7.2, pm = new THREE.Group(); pm.position.set(pmx, 0, pmz); world.add(pm);
    bx(pm, 3.6, 0.12, 1.5, '#f1ece3', 0, 0.98, 0); [[-1.6, -0.6], [1.6, -0.6], [-1.6, 0.6], [1.6, 0.6]].forEach(function (p) { bx(pm, 0.12, 0.92, 0.12, '#59606f', p[0], 0.46, p[1]); });
    var m1 = bx(pm, 0.9, 0.6, 0.05, '#aeb5c4', -0.7, 1.4, 0.1); m1.rotation.x = -0.1; var m2 = bx(pm, 0.9, 0.6, 0.05, '#aeb5c4', 0.7, 1.4, 0.1); m2.rotation.x = -0.1; bx(pm, 0.9, 0.03, 0.5, '#8b92a1', 0, 1.06, 0.3, true);
    var pmPlate = labelSprite('PM · Claude', '#c76a1a', 0.9); pmPlate.position.set(0, 3.6, 0.2); pm.add(pmPlate);
    var wbd = bx(world, 4.4, 2.6, 0.12, '#ffffff', pmx + 5.4, 3.4, -hd + 0.28); bx(world, 4.6, 2.8, 0.1, '#7d8497', pmx + 5.4, 3.4, -hd + 0.22, true);
    var wbTex = mkTex(512, 300, function (x, w, h) { x.fillStyle = '#fff'; x.fillRect(0, 0, w, h); x.strokeStyle = '#3b82f6'; x.lineWidth = 5; x.beginPath(); x.moveTo(30, 60); x.lineTo(200, 60); x.moveTo(30, 100); x.lineTo(160, 100); x.stroke(); x.strokeStyle = '#e0483e'; x.beginPath(); x.moveTo(250, 200); x.bezierCurveTo(300, 120, 380, 260, 470, 150); x.stroke(); x.fillStyle = '#3b82f6'; x.font = '700 30px ' + FONT; x.fillText('Plan → Muse → Done', 30, 160); });
    var wbp = new THREE.Mesh(new THREE.PlaneGeometry(4.2, 2.4), new THREE.MeshBasicMaterial({ map: wbTex })); wbp.position.set(pmx + 5.4, 3.4, -hd + 0.36); world.add(wbp);
    var pmc = makeChar({ shirt: '#e07a2b', sleeve: '#e07a2b', pants: '#2f3647', hair: '#2a1e18', skin: '#f3cfa8', collar: '#fff1e4' });
    pmc.group.position.set(pmx, 0, pmz + 1.7); pmc.status = 'doing'; pmc.kind = 'pm'; pmc.name = 'PM · Claude'; pmc.pmHome = new THREE.Vector3(pmx, 0, pmz + 1.7); world.add(pmc.group); chars.push(pmc);
    pmc.walk = { target: null, wait: rnd(2, 5) };


    // bàn họp tròn (PM hay ghé) + khu sofa
    var mt = new THREE.Group(); mt.position.set(hw - 9.5, 0, -hd + 11); world.add(mt);
    cyl(mt, 1.7, 1.7, 0.12, '#f1ece3', 0, 1.0, 0, 28); cyl(mt, 0.2, 0.3, 0.95, '#59606f', 0, 0.5, 0, 10);
    for (var q = 0; q < 5; q++) { var qa = q / 5 * Math.PI * 2; bx(mt, 0.7, 0.1, 0.7, '#3b4256', Math.sin(qa) * 2.4, 0.5, Math.cos(qa) * 2.4); bx(mt, 0.7, 0.7, 0.1, '#3b4256', Math.sin(qa) * 2.75, 0.9, Math.cos(qa) * 2.75).rotation.y = qa; }
    var rug = new THREE.Mesh(new THREE.BoxGeometry(8.4, 0.04, 5.2), M('#5b4a8a')); rug.position.set(-hw + 6.8, 0.03, hd - 5); rug.receiveShadow = true; world.add(rug);
    var sofa = new THREE.Group(); sofa.position.set(-hw + 6.8, 0, hd - 7); world.add(sofa);
    bx(sofa, 5.2, 0.7, 1.6, '#e0763a', 0, 0.45, 0); bx(sofa, 5.2, 1.2, 0.4, '#d0642a', 0, 1.0, -0.7); bx(sofa, 0.5, 1.0, 1.6, '#d0642a', -2.5, 0.75, 0); bx(sofa, 0.5, 1.0, 1.6, '#d0642a', 2.5, 0.75, 0);
    bx(world, 2.4, 0.5, 1.2, '#7a5a3a', -hw + 6.8, 0.3, hd - 4.4); bx(world, 0.6, 0.18, 0.5, '#ffffff', -hw + 6.4, 0.62, hd - 4.4, true);

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
          ch.group.position.set(dx + 0.45, 0, dz + 0.95); ch.group.rotation.y = Math.PI; ch.root.position.y = 0.64; ch.kind = 'plan'; ch.plan = p; ch.name = p.id; ch.plate = desk.plate; ch.screens = desk.screens; ch.team = team; ch.cat = null; ch.faceY = Math.PI; ch.scrOff = Math.random() * 40;
          world.add(ch.group); chars.push(ch);
          var bub = bubbleSprite('idle'); bub.position.set(dx + 0.45, 3.15, dz + 0.95); world.add(bub); ch.bubble = bub;
          applyPlan(ch, p, true);
        });
      });
      zrow += row.h + GAP;
    });
    if (sel.hidden > 0) {
      var cab = new THREE.Group(); cab.position.set(hw - 2.0, 0, hd - 8.5); cab.rotation.y = -Math.PI / 2; world.add(cab);
      for (var c = 0; c < 3; c++) { bx(cab, 1.3, 2.2, 1.1, '#8b92a1', 0, 1.1, c * 1.4 - 1.4); for (var dr = 0; dr < 3; dr++) bx(cab, 1.0, 0.5, 0.05, '#aeb5c4', 0, 0.45 + dr * 0.7, c * 1.4 - 1.4 + 0.58, true); }
      var al = labelSprite('+' + sel.hidden + ' đã xong (lưu trữ)', '#3a4260', 0.9); al.position.set(0, 3.2, 0); cab.add(al);
    }
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
  function resetCamera() {
    var W = ROOM.W, D = ROOM.D, r = Math.max(W * 0.82, D * 0.95);
    controls.target.set(0, 1.2, D * 0.06); camera.position.set(r * 0.2, r * 0.74, D * 0.5 + r * 0.6); controls.update();
  }

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
    c.nextEv -= dt;
    if (!c.ev && c.nextEv <= 0) { var list = EVENTS[c.kind === 'ceo' ? 'ceo' : s] || ['look']; c.ev = { type: list[Math.floor(Math.random() * list.length)], t0: t, dur: rnd(1.4, 2.4) }; }
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
    var baseY = c.seated ? (c.kind === 'ceo' ? 0.66 : 0.64) : 0.62; c.root.position.y = baseY + cur.jump;
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

  /* ---------- tương tác ---------- */
  var ray = new THREE.Raycaster(), mouse = new THREE.Vector2(), hover = null, down = null;
  var tip = $('tip'), card = $('card');
  function pick(e) {
    var r = renderer.domElement.getBoundingClientRect(); mouse.set(((e.clientX - r.left) / r.width) * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1);
    ray.setFromCamera(mouse, camera); var hit = ray.intersectObjects(pickMeshes, false)[0]; return hit ? hit.object.userData.charRef : null;
  }
  renderer.domElement.addEventListener('pointermove', function (e) {
    if (e.buttons) { tip.style.display = 'none'; return; }
    var c = pick(e); hover = c; renderer.domElement.style.cursor = c ? 'pointer' : 'grab';
    if (c) { tip.style.display = 'block'; tip.style.left = (e.clientX + 14) + 'px'; tip.style.top = (e.clientY + 12) + 'px'; tip.textContent = c.name + (c.cat ? ' · ' + CAT_TEXT[c.cat] : ''); } else tip.style.display = 'none';
  });
  renderer.domElement.addEventListener('pointerdown', function (e) { down = { x: e.clientX, y: e.clientY }; });
  renderer.domElement.addEventListener('pointerup', function (e) {
    if (!down || Math.hypot(e.clientX - down.x, e.clientY - down.y) > 5) return; var c = pick(e); if (c) showCard(c); else hideCard();
  });
  function hideCard() { card.style.display = 'none'; SELECTED = null; }
  function showCard(c) {
    SELECTED = c.kind === 'plan' ? c.plan.id : c.kind; card.style.display = 'block';
    if (c.kind === 'ceo') { card.innerHTML = '<button class="x" id="cx">×</button><h2>BỐ · CEO</h2><p class="muted">Người giao việc. Phòng kính ở góc trái phía sau.</p>'; }
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
  function frame() {
    requestAnimationFrame(frame); if (document.hidden) return;
    var dt = Math.min(clock.getDelta(), 0.1), t = clock.elapsedTime;
    controls.update();
    var tx = Math.max(-ROOM.W / 2, Math.min(ROOM.W / 2, controls.target.x)), tz = Math.max(-ROOM.D / 2, Math.min(ROOM.D / 2, controls.target.z)); controls.target.x = tx; controls.target.z = tz;
    walls.forEach(function (w) { wp.copy(camera.position).sub(w.mesh.position); var outside = wp.dot(w.n) < 0; w.mesh.material.opacity += ((outside ? 0.07 : 1) - w.mesh.material.opacity) * Math.min(1, dt * 8); w.mesh.visible = w.mesh.material.opacity > 0.03; });
    anim.forEach(function (f) { f(t, dt); }); updateScreens(t);
    chars.forEach(function (c) { var moving = c.walk ? walkPM(c, t, dt) : false; if (!moving) animateChar(c, t, dt); else { c.armL.rotation.x = c.cur.aLx; c.armR.rotation.x = c.cur.aRx; c.armL.rotation.z = 0.05; c.armR.rotation.z = -0.05; c.head.rotation.y = 0; c.head.rotation.x = 0; }
      if (c.bubble) c.bubble.position.y = 3.2 + Math.sin(t * 2 + c.phase) * 0.06; });
    renderer.render(scene, camera);
  }
  frame(); load(); setInterval(load, 5000);
  window.__office3d = { resetCamera: resetCamera, controls: controls, camera: camera, count: function () { return chars.length; } };
})();

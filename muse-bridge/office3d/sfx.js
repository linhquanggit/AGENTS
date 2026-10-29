/* sfx.js — Engine âm thanh tổng hợp cho văn phòng 3D của Musude.
   Chạy trong trình duyệt, KHÔNG nạp file mp3/wav, KHÔNG dùng CDN.
   Mọi âm đều tổng hợp bằng oscillator + noise + envelope (gain)
   qua một AudioContext dùng chung, qua một DynamicsCompressor ở cuối.
   Phong cách ES5 như office3d.js: chỉ var + function, không dùng cú pháp
   mới hay module. Nạp bằng <script src="/office3d/sfx.js">
   (T3 sẽ thêm thẻ script này vào office3d/index.html).
   Không tạo AudioContext trước khi có cử chỉ người dùng: chỉ unlock()
   mới tạo, và unlock() cũng tự gắn vào pointerdown/keydown đầu tiên. */
(function () {
  'use strict';
  var G = (typeof window !== 'undefined') ? window : ((typeof global !== 'undefined') ? global : {});
  var DOC = (typeof document !== 'undefined') ? document : null;
  var KEY = 'mb_sfx';
  var MAX_VOICES = 16;
  var COOLDOWN_MS = 40;
  /* 23 âm: 15 âm gốc + laugh, crowd, whistle, drum, ring, clap, fight, music */

  var NAMES = ['type', 'typeburst', 'start', 'ping', 'done', 'error',
    'think', 'sigh', 'door', 'flush', 'step', 'punch', 'ouch', 'cheer', 'coin',
    'laugh', 'crowd', 'whistle', 'drum', 'ring', 'clap', 'fight', 'music'];

  /* Giọng nói cho talk(): mỗi voice một âm sắc / cao độ cơ bản khác nhau.
     Voice 5 là pulse (xung hẹp): tạo bằng PeriodicWave khi có, nếu không
     dùng square làm dự phòng. */
  var VOICES = [
    { type: 'square', base: 130 },
    { type: 'sine', base: 330 },
    { type: 'triangle', base: 230 },
    { type: 'sawtooth', base: 190 },
    { type: 'sine', base: 150 },
    { type: 'pulse', base: 410 }
  ];
  var VOWELS = 'aeiouyAEIOUY' +
    'àáảãạăắằẳẵặâấầẩẫậèéẻẽẹêếềểễệìíỉĩị' +
    'òóỏõọôốồổỗộơớờởỡợùúủũụưứừửữựýỳỷỹỵ' +
    'ÀÁẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÈÉẺẼẸÊẾỀỂỄỆÌÍỈĨỊ' +
    'ÒÓỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÙÚỦŨỤƯỨỪỬỮỰÝỲỶỸỴ';

  var volume = 0.5;
  var muted = false;
  var ctx = null;
  var master = null;
  var comp = null;
  var noiseBuf = null;
  var active = 0;
  var lastPlay = {};
  var curRate = 1;

  function clamp(v, lo, hi) {
    if (isNaN(v)) return lo;
    if (v < lo) return lo;
    if (v > hi) return hi;
    return v;
  }
  function numOpt(v, fallback) {
    if (typeof v !== 'number' || isNaN(v)) return fallback;
    return v;
  }

  function load() {
    try {
      if (!G.localStorage) return;
      var raw = G.localStorage.getItem(KEY);
      if (!raw) return;
      var j = JSON.parse(raw);
      if (j && typeof j.v === 'number' && !isNaN(j.v)) volume = clamp(j.v, 0, 1);
      if (j && typeof j.m === 'boolean') muted = j.m;
    } catch (e) {}
  }
  function save() {
    try {
      if (!G.localStorage) return;
      G.localStorage.setItem(KEY, JSON.stringify({ v: volume, m: muted }));
    } catch (e) {}
  }
  load();

  function applyMaster() {
    if (master && master.gain) {
      try { master.gain.value = muted ? 0 : volume; } catch (e) {}
    }
  }

  function unlock() {
    var Ctor;
    if (ctx) {
      try {
        if (ctx.resume) {
          var p = ctx.resume();
          if (p && p.catch) p.catch(function () {});
        }
      } catch (e) {}
      return;
    }
    Ctor = G.AudioContext || G.webkitAudioContext;
    if (!Ctor) return;
    try { ctx = new Ctor(); } catch (e) { ctx = null; return; }
    try {
      master = ctx.createGain();
      master.gain.value = muted ? 0 : volume;
      try {
        comp = ctx.createDynamicsCompressor();
        if (comp.threshold) comp.threshold.value = -18;
        if (comp.knee) comp.knee.value = 20;
        if (comp.ratio) comp.ratio.value = 6;
        if (comp.attack) comp.attack.value = 0.003;
        if (comp.release) comp.release.value = 0.2;
        master.connect(comp);
        comp.connect(ctx.destination);
      } catch (e2) {
        comp = null;
        master.connect(ctx.destination);
      }
    } catch (e3) { master = null; ctx = null; return; }
    if (DOC && DOC.removeEventListener) {
      try {
        DOC.removeEventListener('pointerdown', unlock, false);
        DOC.removeEventListener('keydown', unlock, false);
      } catch (e4) {}
    }
  }
  if (DOC && DOC.addEventListener) {
    try {
      DOC.addEventListener('pointerdown', unlock, false);
      DOC.addEventListener('keydown', unlock, false);
    } catch (e) {}
  }

  function getNoise() {
    var i, d, len;
    if (noiseBuf) return noiseBuf;
    len = (ctx && ctx.sampleRate) ? ctx.sampleRate : 44100;
    noiseBuf = ctx.createBuffer(1, len, len);
    d = noiseBuf.getChannelData(0);
    for (i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
    return noiseBuf;
  }

  /* Đầu ra cho một âm: qua StereoPanner nếu có và pan khác 0, nếu không
     đi thẳng vào master (master -> compressor -> destination). */
  function outFor(pan) {
    var p;
    if (!master) return null;
    if (pan && ctx.createStereoPanner) {
      try {
        p = ctx.createStereoPanner();
        p.pan.value = pan;
        p.connect(master);
        return p;
      } catch (e) {}
    }
    return master;
  }

  /* Envelope attack/release: bắt đầu gần 0, lên đỉnh rồi về gần 0,
     nên không có click đột ngột ở đầu/cuối. */
  function envGain(out, t0, dur, peak, attack) {
    var g = ctx.createGain();
    var a = attack || 0.005;
    var d = dur / curRate;
    if (a > d * 0.5) a = d * 0.5;
    if (a < 0.001) a = 0.001;
    g.connect(out);
    g.gain.setValueAtTime(0.0001, t0);
    g.gain.linearRampToValueAtTime(peak, t0 + a);
    g.gain.linearRampToValueAtTime(0.0001, t0 + d);
    return g;
  }

  /* Một nốt oscillator. Tần số nhân với curRate, độ dài chia cho curRate
     (rate 0.5..2: chậm/trầm hơn hoặc nhanh/cao hơn). Trả về không gì. */
  function tone(out, type, f0, f1, t0, dur, peak, attack) {
    var osc, g, d;
    if (!ctx || !out) return;
    try {
      osc = ctx.createOscillator();
      if (type === 'pulse') {
        try {
          var real = new Float32Array([0, 1, 0.5, 0.25]);
          var imag = new Float32Array([0, 0, 0, 0]);
          osc.setPeriodicWave(ctx.createPeriodicWave(real, imag));
        } catch (e) { osc.type = 'square'; }
      } else {
        osc.type = type;
      }
      d = dur / curRate;
      osc.frequency.setValueAtTime(f0 * curRate, t0);
      if (f1 && f1 !== f0) {
        try { osc.frequency.linearRampToValueAtTime(f1 * curRate, t0 + d); } catch (e2) {}
      }
      g = envGain(out, t0, dur, peak, attack);
      osc.connect(g);
      osc.start(t0);
      try { osc.stop(t0 + d + 0.05); } catch (e3) { try { osc.stop(); } catch (e4) {} }
    } catch (e5) {}
  }

  /* Một đoạn noise qua bộ lọc. f0/f1 là tần số cắt (bandpass/lowpass). */
  function noise(out, filterType, f0, f1, q, t0, dur, peak, attack) {
    var src, flt, g, d;
    if (!ctx || !out) return;
    try {
      src = ctx.createBufferSource();
      src.buffer = getNoise();
      src.loop = true;
      flt = ctx.createBiquadFilter();
      flt.type = filterType;
      if (flt.frequency) flt.frequency.setValueAtTime(f0, t0);
      if (f1 && f1 !== f0 && flt.frequency) {
        try { flt.frequency.linearRampToValueAtTime(f1, t0 + dur / curRate); } catch (e) {}
      }
      if (flt.Q) flt.Q.value = q || 1;
      d = dur / curRate;
      g = envGain(out, t0, dur, peak, attack);
      src.connect(flt);
      flt.connect(g);
      src.start(t0);
      try { src.stop(t0 + d + 0.05); } catch (e2) { try { src.stop(); } catch (e3) {} }
    } catch (e4) {}
  }

  function clack(out, t, peak) {
    noise(out, 'bandpass', 1800, 1800, 1, t, 0.04, peak * 0.35);
    tone(out, 'triangle', 900, 500, t, 0.025, peak * 0.15);
  }

  /* Mỗi âm nhận (out, t, peak) và trả về độ dài gốc (giây, trước rate).
     Đỉnh nội bộ giữ <= 0.4, master (mặc định 0.5) và compressor giữ
     đỉnh tổng dưới 0.9. */
  var SOUNDS = {
    type: function (out, t, peak) {
      clack(out, t, peak);
      return 0.05;
    },
    typeburst: function (out, t, peak) {
      var i;
      for (i = 0; i < 5; i++) clack(out, t + i * 0.07, peak * 0.9);
      return 0.42;
    },
    start: function (out, t, peak) {
      noise(out, 'bandpass', 400, 2400, 1, t, 0.35, peak * 0.25);
      tone(out, 'sine', 880, 880, t + 0.25, 0.28, peak * 0.3);
      tone(out, 'sine', 1320, 1320, t + 0.25, 0.2, peak * 0.12);
      return 0.55;
    },
    ping: function (out, t, peak) {
      tone(out, 'sine', 660, 660, t, 0.12, peak * 0.3);
      tone(out, 'sine', 990, 990, t + 0.13, 0.18, peak * 0.3);
      return 0.32;
    },
    done: function (out, t, peak) {
      var notes = [523, 659, 784];
      var i;
      for (i = 0; i < notes.length; i++) {
        tone(out, 'triangle', notes[i], notes[i], t + i * 0.18, 0.3, peak * 0.3);
      }
      return 0.72;
    },
    error: function (out, t, peak) {
      tone(out, 'sawtooth', 110, 90, t, 0.18, peak * 0.35);
      tone(out, 'sawtooth', 110, 90, t + 0.26, 0.18, peak * 0.35);
      return 0.5;
    },
    think: function (out, t, peak) {
      tone(out, 'sine', 300, 170, t, 0.5, peak * 0.25);
      return 0.5;
    },
    sigh: function (out, t, peak) {
      noise(out, 'lowpass', 900, 250, 0.8, t, 0.9, peak * 0.22, 0.2);
      return 0.9;
    },
    door: function (out, t, peak) {
      noise(out, 'bandpass', 600, 1700, 1, t, 0.3, peak * 0.22);
      tone(out, 'triangle', 2000, 2000, t + 0.33, 0.03, peak * 0.2);
      return 0.4;
    },
    flush: function (out, t, peak) {
      noise(out, 'lowpass', 600, 400, 0.8, t, 1.4, peak * 0.18, 0.25);
      return 1.4;
    },
    step: function (out, t, peak) {
      noise(out, 'lowpass', 500, 500, 0.8, t, 0.06, peak * 0.25);
      tone(out, 'sine', 95, 60, t, 0.06, peak * 0.2);
      return 0.07;
    },
    punch: function (out, t, peak) {
      tone(out, 'sine', 130, 45, t, 0.18, peak * 0.4);
      noise(out, 'lowpass', 800, 300, 0.8, t, 0.08, peak * 0.3);
      return 0.25;
    },
    ouch: function (out, t, peak) {
      tone(out, 'sine', 600, 200, t, 0.3, peak * 0.35);
      return 0.3;
    },
    cheer: function (out, t, peak) {
      var notes = [523, 659, 784, 1047];
      var i;
      for (i = 0; i < notes.length; i++) {
        tone(out, 'sine', notes[i], notes[i], t + i * 0.09, 0.12, peak * 0.28);
      }
      noise(out, 'bandpass', 2000, 2000, 0.8, t + 0.4, 0.35, peak * 0.15, 0.02);
      return 0.9;
    },
    coin: function (out, t, peak) {
      tone(out, 'sine', 1568, 1568, t, 0.12, peak * 0.28);
      tone(out, 'sine', 2093, 2093, t + 0.07, 0.18, peak * 0.28);
      return 0.26;
    },
    laugh: function (out, t, peak) {
      var i, f = [750, 560, 720, 540, 800, 580];
      for (i = 0; i < f.length; i++) tone(out, 'triangle', f[i], f[i] * 0.85, t + i * 0.13, 0.09, peak * 0.28);
      return 0.85;
    },
    crowd: function (out, t, peak) {
      noise(out, 'lowpass', 500, 350, 0.8, t, 1.8, peak * 0.14, 0.4);
      noise(out, 'bandpass', 900, 700, 0.8, t + 0.3, 1.2, peak * 0.08, 0.3);
      return 1.8;
    },
    whistle: function (out, t, peak) {
      tone(out, 'sine', 2100, 2800, t, 0.35, peak * 0.22);
      tone(out, 'sine', 2800, 1900, t + 0.35, 0.3, peak * 0.22);
      return 0.65;
    },
    drum: function (out, t, peak) {
      tone(out, 'sine', 140, 50, t, 0.16, peak * 0.4);
      tone(out, 'sine', 140, 50, t + 0.25, 0.16, peak * 0.4);
      noise(out, 'lowpass', 600, 300, 0.8, t, 0.05, peak * 0.2);
      return 0.45;
    },
    ring: function (out, t, peak) {
      var i;
      for (i = 0; i < 3; i++) { tone(out, 'sine', 1250, 1250, t + i * 0.35, 0.12, peak * 0.25); tone(out, 'sine', 1560, 1560, t + i * 0.35 + 0.15, 0.12, peak * 0.18); }
      return 1.1;
    },
    clap: function (out, t, peak) {
      var i;
      for (i = 0; i < 4; i++) noise(out, 'bandpass', 1800, 1800, 1, t + i * 0.12, 0.04, peak * 0.3);
      return 0.5;
    },
    fight: function (out, t, peak) {
      var i;
      for (i = 0; i < 4; i++) { tone(out, 'sine', 130, 45, t + i * 0.18, 0.14, peak * 0.38); tone(out, 'sine', 600, 250, t + i * 0.18 + 0.09, 0.12, peak * 0.25); }
      noise(out, 'lowpass', 800, 300, 0.8, t, 0.7, peak * 0.12);
      return 0.8;
    },
    music: function (out, t, peak) {
      var notes = [523, 659, 784, 1047, 784, 659, 587, 523], i;
      for (i = 0; i < notes.length; i++) {
        tone(out, 'triangle', notes[i], notes[i], t + i * 0.22, 0.2, peak * 0.26);
        if (i % 2 === 0) tone(out, 'sine', notes[i] / 2, notes[i] / 2, t + i * 0.22, 0.4, peak * 0.2);
      }
      return 1.9;
    }
  };

  function play(name, opts) {
    var o, gainOpt, pan, rate, out, dur, nowMs, ms;
    if (muted || !ctx || !master) return;
    if (!Object.prototype.hasOwnProperty.call(SOUNDS, name)) return;
    nowMs = (typeof Date !== 'undefined' && Date.now) ? Date.now() : 0;
    if (lastPlay[name] && nowMs - lastPlay[name] < COOLDOWN_MS) return;
    if (active >= MAX_VOICES) return;
    o = opts || {};
    gainOpt = clamp(numOpt(o.gain, 1), 0, 1);
    pan = clamp(numOpt(o.pan, 0), -1, 1);
    rate = clamp(numOpt(o.rate, 1), 0.5, 2);
    out = outFor(pan);
    if (!out) return;
    curRate = rate;
    dur = 0.5;
    try { dur = SOUNDS[name](out, ctx.currentTime + 0.01, gainOpt) || 0.5; } catch (e) {}
    curRate = 1;
    lastPlay[name] = nowMs;
    active++;
    ms = Math.round((dur / rate) * 1000) + 80;
    setTimeout(function () { if (active > 0) active--; }, ms);
  }

  /* talk(text, voice): một blip cho mỗi chữ cái, tối đa 14 blip/câu,
     mỗi blip 45 ms. Dấu cách thành ngắt nhẹ 60 ms (không tính blip).
     Nguyên âm cao hơn phụ âm (x1.35), cao độ ngẫu nhiên ±8%. */
  function talk(text, voice) {
    var s, vi, v, t, off, count, i, ch, isVowel, freq, total;
    if (muted || !ctx || !master) return;
    if (text == null) return;
    s = String(text);
    if (!s) return;
    if (active >= MAX_VOICES) return;
    vi = parseInt(voice, 10);
    if (isNaN(vi)) vi = 0;
    if (vi < 0) vi = 0;
    if (vi > 5) vi = 5;
    v = VOICES[vi];
    curRate = 1;
    t = ctx.currentTime + 0.01;
    off = 0;
    count = 0;
    for (i = 0; i < s.length; i++) {
      ch = s.charAt(i);
      if (ch === ' ') { off += 0.06; continue; }
      if (count >= 14) break;
      isVowel = VOWELS.indexOf(ch) !== -1;
      freq = v.base * (isVowel ? 1.35 : 1.0) * (1 + (Math.random() * 0.16 - 0.08));
      tone(master, v.type, freq, freq * 0.92, t + off, 0.045, 0.22);
      count++;
      off += 0.06;
    }
    active++;
    total = Math.round((off + 0.1) * 1000) + 80;
    setTimeout(function () { if (active > 0) active--; }, total);
  }

  function list() { return NAMES.slice(); }

  var Sfx = {
    unlock: unlock,
    setVolume: function (v) {
      var nv = numOpt(v, volume);
      volume = clamp(nv, 0, 1);
      applyMaster();
      save();
    },
    getVolume: function () { return volume; },
    setMuted: function (b) {
      muted = !!b;
      applyMaster();
      save();
    },
    isMuted: function () { return muted; },
    play: play,
    talk: talk,
    list: list
  };
  G.Sfx = Sfx;
})();

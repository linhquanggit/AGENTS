/* sfx-ui.js — Bảng âm thanh cho văn phòng 3D (T2).
   ES5, tự dựng DOM, không sửa office3d.js. Dùng API window.Sfx
   có sẵn trong sfx.js (được nạp trước file này).
   Nút loa cố định góc phải-dưới; bấm để mở/đóng panel gồm:
   công tắc Bật tiếng, thanh trượt âm lượng 0-100, lưới nút
   nghe thử (mỗi nút 1 âm từ Sfx.list()) và nút Nói thử
   kèm ô chọn voice 0..5. Âm lượng + tắt tiếng do Sfx lưu
   sẵn vào localStorage nên tải lại trang vẫn nhớ. */
(function () {
  'use strict';
  var G = (typeof window !== 'undefined') ? window : {};
  var DOC = (typeof document !== 'undefined') ? document : null;
  if (!DOC || !G.Sfx || G.__mbSfxUi) return;
  G.__mbSfxUi = true;
  var Sfx = G.Sfx;

  var SVG_ON = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polygon points="11 5 6 9 2 9 2 15 6 15 11 19 11 5" fill="currentColor" stroke="none"></polygon><path d="M15.5 8.5a5 5 0 0 1 0 7"></path><path d="M18.5 5.5a9 9 0 0 1 0 13"></path></svg>';
  var SVG_OFF = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polygon points="11 5 6 9 2 9 2 15 6 15 11 19 11 5" fill="currentColor" stroke="none"></polygon><line x1="16" y1="9" x2="22" y2="15"></line><line x1="22" y1="9" x2="16" y2="15"></line></svg>';
  var TALK_TEXT = 'Khó thế… làm thế nào nhỉ?';
  var VOICE_KEY = 'mb_sfx_voice';

  function el(tag, cls, text) {
    var n = DOC.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.appendChild(DOC.createTextNode(text));
    return n;
  }
  function clampVoice(v) {
    if (isNaN(v)) return 0;
    if (v < 0) return 0;
    if (v > 5) return 5;
    return v;
  }
  function loadVoice() {
    try {
      if (G.localStorage) return clampVoice(parseInt(G.localStorage.getItem(VOICE_KEY), 10));
    } catch (e) {}
    return 0;
  }
  function saveVoice(v) {
    try {
      if (G.localStorage) G.localStorage.setItem(VOICE_KEY, String(v));
    } catch (e) {}
  }

  function init() {
    var btn, panel, muteBox, slider, volLabel, voiceSel, talkBtn, grid;
    var names, i, b, opt, row, label;

    btn = el('button', 'sfx-btn');
    btn.type = 'button';
    btn.title = 'Âm thanh';
    btn.setAttribute('aria-label', 'Âm thanh');
    btn.setAttribute('aria-expanded', 'false');

    panel = el('div', 'sfx-panel');
    panel.appendChild(el('div', 'sfx-title', 'Âm thanh'));

    row = el('div', 'sfx-row');
    muteBox = DOC.createElement('input');
    muteBox.type = 'checkbox';
    muteBox.className = 'sfx-mute';
    label = el('label', 'sfx-mute-label');
    label.appendChild(muteBox);
    label.appendChild(DOC.createTextNode('Bật tiếng'));
    row.appendChild(label);
    panel.appendChild(row);

    row = el('div', 'sfx-row');
    row.appendChild(el('span', 'sfx-label', 'Âm lượng'));
    slider = DOC.createElement('input');
    slider.type = 'range';
    slider.min = '0';
    slider.max = '100';
    slider.step = '1';
    slider.className = 'sfx-volume';
    slider.setAttribute('aria-label', 'Âm lượng');
    row.appendChild(slider);
    volLabel = el('span', 'sfx-vol', '');
    row.appendChild(volLabel);
    panel.appendChild(row);

    row = el('div', 'sfx-row');
    row.appendChild(el('span', 'sfx-label', 'Giọng'));
    voiceSel = DOC.createElement('select');
    voiceSel.className = 'sfx-voice';
    voiceSel.setAttribute('aria-label', 'Giọng nói thử');
    for (i = 0; i <= 5; i++) {
      opt = DOC.createElement('option');
      opt.value = String(i);
      opt.appendChild(DOC.createTextNode(String(i)));
      voiceSel.appendChild(opt);
    }
    voiceSel.value = String(loadVoice());
    row.appendChild(voiceSel);
    talkBtn = el('button', 'sfx-talk', 'Nói thử');
    talkBtn.type = 'button';
    row.appendChild(talkBtn);
    panel.appendChild(row);

    grid = el('div', 'sfx-grid');
    names = Sfx.list();
    for (i = 0; i < names.length; i++) {
      (function (name) {
        var sb = el('button', 'sfx-sound', name);
        sb.type = 'button';
        sb.title = 'Nghe thử: ' + name;
        sb.onclick = function () {
          Sfx.unlock();
          Sfx.play(name);
        };
        grid.appendChild(sb);
      })(names[i]);
    }
    panel.appendChild(grid);

    function refresh() {
      var muted = Sfx.isMuted();
      btn.innerHTML = muted ? SVG_OFF : SVG_ON;
      btn.title = muted ? 'Âm thanh (đang tắt tiếng)' : 'Âm thanh';
      muteBox.checked = !muted;
      slider.value = String(Math.round(Sfx.getVolume() * 100));
      volLabel.firstChild.nodeValue = slider.value + '%';
    }
    refresh();

    btn.onclick = function () {
      var open = panel.style.display === 'block';
      panel.style.display = open ? 'none' : 'block';
      btn.setAttribute('aria-expanded', open ? 'false' : 'true');
      if (!open) refresh();
    };
    muteBox.onchange = function () {
      Sfx.setMuted(!muteBox.checked);
      refresh();
    };
    slider.oninput = function () {
      Sfx.setVolume(parseInt(slider.value, 10) / 100);
      volLabel.firstChild.nodeValue = slider.value + '%';
    };
    slider.onchange = slider.oninput;
    voiceSel.onchange = function () {
      saveVoice(clampVoice(parseInt(voiceSel.value, 10)));
    };
    talkBtn.onclick = function () {
      Sfx.unlock();
      Sfx.talk(TALK_TEXT, clampVoice(parseInt(voiceSel.value, 10)));
    };

    DOC.body.appendChild(panel);
    DOC.body.appendChild(btn);
  }

  if (DOC.readyState === 'loading') {
    DOC.addEventListener('DOMContentLoaded', init, false);
  } else {
    init();
  }
})();

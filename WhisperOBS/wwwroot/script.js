const captionEl = document.getElementById('caption');
const statusEl  = document.getElementById('status');
let hideTimer   = null;
const SHOW_DURATION = 4000;

function showCaption(text) {
    captionEl.textContent = text;
    captionEl.classList.add('visible');

    clearTimeout(hideTimer);
    hideTimer = setTimeout(() => {
        captionEl.classList.remove('visible');
    }, SHOW_DURATION);
}

function connect() {
    const ws = new WebSocket(`ws://${location.host}/ws`);

    ws.onopen  = () => { statusEl.className = 'connected'; };
    ws.onclose = ws.onerror = () => {
        statusEl.className = 'error';
        setTimeout(connect, 2000);
    };
    ws.onmessage = (evt) => {
        try {
            const msg = JSON.parse(evt.data);
            if (msg.type === 'caption') showCaption(msg.text);
        } catch {}
    };
}
connect();
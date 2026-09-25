(() => {
    const themeKey = 'autotracker-theme';
    const savedTheme = localStorage.getItem(themeKey) || 'dark';
    document.body.classList.toggle('light-mode', savedTheme === 'light');

    document.getElementById('themeToggle')?.addEventListener('click', () => {
        const useLight = !document.body.classList.contains('light-mode');
        document.body.classList.toggle('light-mode', useLight);
        localStorage.setItem(themeKey, useLight ? 'light' : 'dark');
    });

    const navToggle = document.getElementById('navToggle');
    const sidebar = document.getElementById('sidebar');
    const backdrop = document.getElementById('navBackdrop');
    const setNavOpen = (open) => {
        document.body.classList.toggle('nav-open', open);
        navToggle?.setAttribute('aria-expanded', String(open));
        if (backdrop) backdrop.hidden = !open;
    };
    navToggle?.addEventListener('click', () => setNavOpen(!document.body.classList.contains('nav-open')));
    backdrop?.addEventListener('click', () => setNavOpen(false));
    sidebar?.querySelectorAll('a').forEach(link => link.addEventListener('click', () => setNavOpen(false)));
    document.addEventListener('keydown', event => { if (event.key === 'Escape') setNavOpen(false); });

    if ('serviceWorker' in navigator && location.protocol === 'https:') {
        window.addEventListener('load', () => navigator.serviceWorker.register('/service-worker.js').catch(() => {}));
    }
})();

const autoTrackerScanners = new Map();

function setTagScanMessage(messageId, type, text) {
    const host = document.getElementById(messageId);
    if (!host) return;
    host.replaceChildren();
    const notice = document.createElement('div');
    notice.className = `alert ${type}`;
    notice.textContent = text;
    host.appendChild(notice);
}

function completeTagScan(videoId, inputId, messageId, rawValue) {
    const value = String(rawValue || '').trim().toUpperCase();
    if (!value) return;
    const input = document.getElementById(inputId);
    if (input) {
        input.value = value;
        input.dispatchEvent(new Event('change', { bubbles: true }));
        input.focus();
    }
    setTagScanMessage(messageId, 'success', `Tag scanned: ${value}`);
    stopTagScanner(videoId);
}

async function startTagScanner(videoId, inputId, messageId) {
    stopTagScanner(videoId);
    const video = document.getElementById(videoId);
    if (!video || !navigator.mediaDevices?.getUserMedia) {
        setTagScanMessage(messageId, 'warning', 'Camera scanning is unavailable. Type the printed tag code instead.');
        return;
    }

    try {
        if ('BarcodeDetector' in window) {
            const detector = new BarcodeDetector({ formats: ['qr_code', 'code_128', 'code_39', 'ean_13', 'ean_8', 'upc_a', 'upc_e'] });
            const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: 'environment' } }, audio: false });
            video.srcObject = stream;
            video.hidden = false;
            await video.play();
            const state = { stream, timer: 0 };
            autoTrackerScanners.set(videoId, state);
            setTagScanMessage(messageId, 'info', 'Point the rear camera at the printed QR or barcode.');
            state.timer = window.setInterval(async () => {
                if (!video.srcObject) return;
                try {
                    const codes = await detector.detect(video);
                    if (codes[0]?.rawValue) completeTagScan(videoId, inputId, messageId, codes[0].rawValue);
                } catch { }
            }, 350);
            return;
        }

        if (window.ZXingBrowser?.BrowserMultiFormatReader) {
            const reader = new ZXingBrowser.BrowserMultiFormatReader();
            const controls = await reader.decodeFromConstraints(
                { video: { facingMode: { ideal: 'environment' } }, audio: false },
                video,
                (result) => { if (result?.getText()) completeTagScan(videoId, inputId, messageId, result.getText()); }
            );
            video.hidden = false;
            autoTrackerScanners.set(videoId, { reader, controls });
            setTagScanMessage(messageId, 'info', 'Point the rear camera at the printed QR or barcode.');
            return;
        }

        setTagScanMessage(messageId, 'warning', 'This browser cannot decode barcodes. Type the printed tag code instead.');
    } catch (error) {
        stopTagScanner(videoId);
        const denied = error?.name === 'NotAllowedError';
        setTagScanMessage(messageId, 'danger', denied ? 'Camera permission was denied. Allow camera access or type the tag code.' : 'Camera could not start. Use the manual tag field instead.');
    }
}

function stopTagScanner(videoId) {
    const video = document.getElementById(videoId);
    const state = autoTrackerScanners.get(videoId);
    if (state?.timer) window.clearInterval(state.timer);
    state?.controls?.stop?.();
    state?.reader?.reset?.();
    state?.stream?.getTracks?.().forEach(track => track.stop());
    video?.srcObject?.getTracks?.().forEach(track => track.stop());
    if (video) { video.pause(); video.srcObject = null; video.hidden = true; }
    autoTrackerScanners.delete(videoId);
}

window.startTagScanner = startTagScanner;
window.stopTagScanner = stopTagScanner;
window.addEventListener('pagehide', () => [...autoTrackerScanners.keys()].forEach(stopTagScanner));

import { StrictMode } from 'react'
import './i18n'
import { createRoot } from 'react-dom/client'
import 'remixicon/fonts/remixicon.css'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)

if ('serviceWorker' in navigator) {
  window.addEventListener('load', async () => {
    try {
      const registration = await navigator.serviceWorker.register('/sw.js');
      if (registration.waiting && navigator.serviceWorker.controller) window.dispatchEvent(new Event('hcbe:sw-update'));
      registration.addEventListener('updatefound', () => {
        const worker = registration.installing;
        worker?.addEventListener('statechange', () => {
          if (worker.state === 'installed' && navigator.serviceWorker.controller) window.dispatchEvent(new Event('hcbe:sw-update'));
        });
      });
      window.setInterval(() => void registration.update(), 60 * 60 * 1000);
    } catch (error) { console.warn('Service worker registration failed', error); }
  });
}

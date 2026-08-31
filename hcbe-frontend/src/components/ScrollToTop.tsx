import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

/**
 * Resets window scroll on every client-side navigation.
 * Preserves in-page hash anchors when present.
 */
const ScrollToTop = () => {
  const { pathname, hash, key } = useLocation();

  useEffect(() => {
    if ('scrollRestoration' in window.history) {
      window.history.scrollRestoration = 'manual';
    }
  }, []);

  useEffect(() => {
    document.body.style.overflow = '';

    if (hash) {
      const id = decodeURIComponent(hash.replace('#', ''));
      const target = document.getElementById(id);
      if (target) {
        // Wait one frame so the destination page can paint.
        requestAnimationFrame(() => {
          target.scrollIntoView({ behavior: 'auto', block: 'start' });
        });
        return;
      }
    }

    window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
  }, [pathname, hash, key]);

  return null;
};

export default ScrollToTop;

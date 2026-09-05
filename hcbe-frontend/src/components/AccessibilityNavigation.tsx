import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

export default function AccessibilityNavigation() {
  const location = useLocation();
  const { i18n } = useTranslation();
  const french = !i18n.language.startsWith('en');

  useEffect(() => {
    const frame = requestAnimationFrame(() => {
      document.querySelectorAll('main[data-skip-target]').forEach((main) => { main.removeAttribute('id'); main.removeAttribute('tabindex'); main.removeAttribute('data-skip-target'); });
      const main = document.querySelector('main');
      if (main) { main.setAttribute('id', 'main-content'); main.setAttribute('tabindex', '-1'); main.setAttribute('data-skip-target', 'true'); }
    });
    return () => cancelAnimationFrame(frame);
  }, [location.pathname]);

  return <a href="#main-content" className="fixed left-4 top-3 z-[200] -translate-y-24 rounded-lg bg-gold px-5 py-3 text-sm font-bold text-green-deep shadow-xl transition-transform focus:translate-y-0">{french ? 'Aller au contenu principal' : 'Skip to main content'}</a>;
}

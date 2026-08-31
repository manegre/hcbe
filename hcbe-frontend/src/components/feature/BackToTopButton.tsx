import { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const ADMIN_PREFIX = '/admin';

const BackToTopButton = () => {
  const { pathname } = useLocation();
  const { t } = useTranslation();
  const [isVisible, setIsVisible] = useState(false);

  const isAdminRoute = pathname.startsWith(ADMIN_PREFIX);

  useEffect(() => {
    if (isAdminRoute) {
      setIsVisible(false);
      return;
    }

    const onScroll = () => {
      setIsVisible(window.scrollY > 400);
    };

    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, [isAdminRoute, pathname]);

  if (isAdminRoute || !isVisible) {
    return null;
  }

  return (
    <button
      type="button"
      onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
      className="fixed bottom-5 right-4 z-40 inline-flex h-12 w-12 items-center justify-center rounded-full border border-transparent bg-green text-white transition hover:bg-green-deep focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-green sm:bottom-8 sm:right-8"
      aria-label={t('public.backToTop')}
    >
      <i className="ri-arrow-up-line text-xl" aria-hidden="true"></i>
    </button>
  );
};

export default BackToTopButton;

import { useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import PublicLanguageSwitcher from './PublicLanguageSwitcher';
import { HcbeLogoMark } from '../brand/HcbeLogo';
import ThemeToggle from './ThemeToggle';
import { useAuth } from '../../contexts/AuthContext';
import { siteContentApi } from '../../lib/api/site-content';
import type { NavigationItemDto } from '../../lib/api/types';

const Navbar = () => {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [openDropdown, setOpenDropdown] = useState<string | null>(null);
  const location = useLocation();
  const { t, i18n } = useTranslation();
  const { user } = useAuth();
  const [cmsNavigation, setCmsNavigation] = useState<NavigationItemDto[]>([]);
  const closeButtonRef = useRef<HTMLButtonElement>(null);

  const defaultNavLinks = useMemo(
    () => [
      { path: '/', labelKey: 'public.nav.home' },
      {
        path: '/services',
        labelKey: 'public.nav.services',
        dropdown: [
          { path: '/services/documents-officiels', labelKey: 'public.nav.documents' },
          { path: '/services/comites', labelKey: 'public.nav.committees' },
          { path: '/services/bourses', labelKey: 'public.nav.grants' },
        ],
      },
      {
        path: '/actualites',
        labelKey: 'public.nav.news',
        dropdown: [
          { path: '/actualites/evenements', labelKey: 'public.nav.events' },
          { path: '/actualites/annonces', labelKey: 'public.nav.announcements' },
          { path: '/actualites/souvenirs', labelKey: 'public.nav.memories' },
        ],
      },
      {
        path: '/engagement',
        labelKey: 'public.nav.engagement',
        dropdown: [
          { path: '/engagement/annuaire', labelKey: 'public.nav.associations' },
          { path: '/engagement/projets', labelKey: 'public.nav.projects' },
          { path: '/engagement/consultations', labelKey: 'public.nav.consultations' },
        ],
      },
      { path: '/espace-membre', labelKey: 'public.nav.members' },
      { path: '/contact', labelKey: 'public.nav.contact' },
    ],
    [],
  );

  useEffect(() => {
    const loadNavigation = () => siteContentApi.getNavigation().then((response) => {
      if (response.success && response.data) setCmsNavigation(response.data);
    }).catch(() => undefined);
    void loadNavigation();
    window.addEventListener('hcbe:content-published', loadNavigation);
    return () => window.removeEventListener('hcbe:content-published', loadNavigation);
  }, []);

  const navLinks = useMemo(() => {
    if (cmsNavigation.length === 0) return defaultNavLinks;
    const english = i18n.language.startsWith('en');
    return [...cmsNavigation]
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .map((item) => {
        const fallback = defaultNavLinks.find((link) => link.path === item.url);
        return { ...(fallback || { path: item.url, labelKey: '' }), path: item.url, label: english && item.labelEn ? item.labelEn : item.label };
      });
  }, [cmsNavigation, defaultNavLinks, i18n.language]);

  useEffect(() => {
    document.body.style.overflow = isMobileMenuOpen ? 'hidden' : '';
    return () => {
      document.body.style.overflow = '';
    };
  }, [isMobileMenuOpen]);

  useEffect(() => {
    setIsMobileMenuOpen(false);
    setOpenDropdown(null);
  }, [location.pathname]);

  useEffect(() => {
    if (!isMobileMenuOpen) return;

    closeButtonRef.current?.focus();

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsMobileMenuOpen(false);
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isMobileMenuOpen]);

  const handleDropdownToggle = (path: string) => {
    setOpenDropdown(openDropdown === path ? null : path);
  };

  const isActiveLink = (linkPath: string, hasDropdown = false) => {
    if (hasDropdown) {
      return location.pathname.startsWith(linkPath);
    }
    return location.pathname === linkPath;
  };

  const mainLinks = navLinks.filter((link) => link.path !== '/espace-membre');
  const hasMemberSession = Boolean(user?.memberId);
  const memberCtaLabel = hasMemberSession ? t('public.nav.memberSpace') : t('public.nav.memberAccess');

  const mobileMenu = isMobileMenuOpen
    ? createPortal(
        <div
          role="dialog"
          aria-modal="true"
          aria-label={t('public.nav.openMenu')}
          className="fixed inset-0 z-[100] lg:hidden"
        >
          <button
            type="button"
            aria-hidden="true"
            tabIndex={-1}
            className="absolute inset-0 bg-green-deep/45 backdrop-blur-[2px]"
            onClick={() => setIsMobileMenuOpen(false)}
          />

          <div className="absolute inset-y-0 right-0 flex h-[100dvh] w-full max-w-[32rem] flex-col overflow-hidden bg-background shadow-[-24px_0_70px_rgba(0,35,18,.24)]">
            <div className="flex h-16 shrink-0 items-center justify-between border-b border-line bg-surface px-margin-mobile sm:px-6">
              <Link to="/" onClick={() => setIsMobileMenuOpen(false)} className="flex min-w-0 items-center">
                <HcbeLogoMark size="sm" />
              </Link>
              <div className="flex shrink-0 items-center gap-1">
                <ThemeToggle />
                <button
                  type="button"
                  ref={closeButtonRef}
                  onClick={() => setIsMobileMenuOpen(false)}
                  aria-label={t('public.nav.closeMenu')}
                  className="flex h-11 w-11 items-center justify-center rounded-full text-ink transition-colors hover:bg-green/8 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-green"
                >
                  <i className="ri-close-line text-2xl" aria-hidden="true"></i>
                </button>
              </div>
            </div>

            <nav id="mobile-navigation" className="min-h-0 flex-1 overflow-y-auto overscroll-contain px-margin-mobile py-4 sm:px-6 sm:py-6">
              <div className="overflow-hidden rounded-2xl border border-line/80 bg-surface shadow-[0_16px_40px_rgba(0,59,27,.06)]">
                {mainLinks.map((link, index) => {
                  const label = link.label || t(link.labelKey);
                  const active = isActiveLink(link.path, Boolean(link.dropdown));
                  const expanded = openDropdown === link.path;

                  return (
                    <div key={link.path} className={index > 0 ? 'border-t border-line/70' : ''}>
                      {link.dropdown ? (
                        <div className="flex min-h-[54px] items-stretch">
                          <Link
                            to={link.path}
                            onClick={() => setIsMobileMenuOpen(false)}
                            className={`relative flex flex-1 items-center px-4 font-display text-[1.05rem] leading-tight transition-colors sm:px-5 ${
                              active ? 'bg-green/7 text-green' : 'text-ink hover:bg-green/5 hover:text-green'
                            }`}
                          >
                            {active && <span className="absolute inset-y-3 left-0 w-[3px] rounded-r-full bg-gold" aria-hidden="true" />}
                            {label}
                          </Link>
                          <button
                            type="button"
                            onClick={() => handleDropdownToggle(link.path)}
                            aria-label={
                              expanded
                                ? t('public.nav.closeSubmenu', { label })
                                : t('public.nav.openSubmenu', { label })
                            }
                            aria-expanded={expanded}
                            aria-controls={`mobile-submenu-${link.path.replace(/\W/g, '')}`}
                            className={`flex w-14 shrink-0 items-center justify-center border-l border-line/60 transition-colors ${
                              expanded ? 'bg-green text-white' : 'text-green hover:bg-green/8'
                            }`}
                          >
                            <i
                              className={`ri-arrow-${expanded ? 'up' : 'down'}-s-line text-xl transition-transform`}
                              aria-hidden="true"
                            ></i>
                          </button>
                        </div>
                      ) : (
                        <Link
                          to={link.path}
                          onClick={() => setIsMobileMenuOpen(false)}
                          className={`relative flex min-h-[54px] items-center px-4 font-display text-[1.05rem] leading-tight transition-colors sm:px-5 ${
                            active ? 'bg-green/7 text-green' : 'text-ink hover:bg-green/5 hover:text-green'
                          }`}
                        >
                          {active && <span className="absolute inset-y-3 left-0 w-[3px] rounded-r-full bg-gold" aria-hidden="true" />}
                          {label}
                        </Link>
                      )}

                      {link.dropdown && expanded && (
                        <div id={`mobile-submenu-${link.path.replace(/\W/g, '')}`} className="border-t border-line/60 bg-surface-container/55 px-3 py-2 sm:px-4">
                          {link.dropdown.map((subLink) => (
                            <Link
                              key={subLink.path}
                              to={subLink.path}
                              onClick={() => setIsMobileMenuOpen(false)}
                              className={`flex min-h-[44px] items-center rounded-xl px-3 text-sm font-medium transition-colors ${
                                location.pathname === subLink.path
                                  ? 'bg-green text-white'
                                  : 'text-ink-variant hover:bg-green/7 hover:text-green'
                              }`}
                            >
                              <span className="mr-3 h-1.5 w-1.5 rounded-full bg-gold" aria-hidden="true" />
                              {t(subLink.labelKey)}
                            </Link>
                          ))}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </nav>

            <div className="shrink-0 border-t border-line bg-surface px-margin-mobile pb-[max(1rem,env(safe-area-inset-bottom))] pt-4 sm:px-6">
              <div className="flex items-center justify-between gap-4">
                <span className="text-[10px] font-bold uppercase tracking-[0.16em] text-ink-variant">
                  {t('public.nav.language', { defaultValue: 'Langue' })}
                </span>
                <PublicLanguageSwitcher />
              </div>
              <Link
                to="/espace-membre"
                onClick={() => setIsMobileMenuOpen(false)}
                className={`mt-4 flex min-h-[48px] w-full items-center justify-center gap-2 rounded-full px-6 py-3 text-[11px] font-bold uppercase tracking-[0.1em] text-white transition-all duration-200 hover:-translate-y-0.5 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 ${hasMemberSession ? 'bg-green shadow-[0_10px_24px_rgba(0,59,27,.2)] hover:bg-green-deep focus-visible:outline-green' : 'bg-red-link shadow-[0_10px_24px_rgba(174,45,31,.2)] hover:bg-red-deep focus-visible:outline-red-link'}`}
              >
                <i className={hasMemberSession ? 'ri-user-smile-line text-base' : 'ri-user-add-line text-base'} aria-hidden="true" />
                {memberCtaLabel}
                <i className="ri-arrow-right-line text-base" aria-hidden="true" />
              </Link>
            </div>
          </div>
        </div>,
        document.body,
      )
    : null;

  return (
    <>
    <header className="sticky top-0 z-50 border-b border-line/50 bg-surface/90 shadow-[0_8px_30px_rgba(0,59,27,.055)] backdrop-blur-xl">
      <div className="mx-auto flex h-[76px] w-full max-w-[1440px] items-center justify-between px-margin-mobile md:px-margin-desktop">
        <Link to="/" className="flex shrink-0 items-center gap-3">
          <HcbeLogoMark size="md" />
        </Link>

        <div className="hidden flex-1 justify-center lg:flex">
          <nav className="flex items-center lg:space-x-7">
            {mainLinks.map((link) => {
              const label = link.label || t(link.labelKey);
              const active = isActiveLink(link.path, Boolean(link.dropdown));
              const linkClasses = `relative flex min-h-[44px] items-center gap-1 text-[12px] font-bold uppercase tracking-[0.08em] transition-colors duration-200 after:absolute after:inset-x-0 after:bottom-0 after:h-0.5 after:origin-left after:bg-gold after:transition-transform ${
                active
                  ? 'text-green after:scale-x-100'
                  : 'text-ink-variant after:scale-x-0 hover:text-green hover:after:scale-x-100'
              }`;

              return (
                <div key={link.path} className="group relative">
                  {link.dropdown ? (
                    <>
                      <Link to={link.path} className={linkClasses} onMouseEnter={() => setOpenDropdown(link.path)}>
                        {label}
                        <i className="ri-arrow-down-s-line" aria-hidden="true"></i>
                      </Link>

                      <div
                        className="invisible absolute left-1/2 top-[calc(100%+8px)] z-50 min-w-[270px] -translate-x-1/2 translate-y-2 rounded-xl border border-line/60 bg-surface p-2 opacity-0 shadow-[0_20px_50px_rgba(0,59,27,.16)] transition-all duration-200 group-hover:visible group-hover:translate-y-0 group-hover:opacity-100"
                        onMouseLeave={() => setOpenDropdown(null)}
                      >
                        {link.dropdown.map((subLink) => (
                          <Link
                            key={subLink.path}
                            to={subLink.path}
                            className={`flex min-h-[44px] items-center rounded-lg px-4 py-3 text-sm font-medium transition-colors duration-200 ${
                              location.pathname === subLink.path
                                ? 'bg-green/8 text-green'
                                : 'text-ink-variant hover:bg-green/5 hover:text-green'
                            }`}
                          >
                            {t(subLink.labelKey)}
                          </Link>
                        ))}
                      </div>
                    </>
                  ) : (
                    <Link to={link.path} className={linkClasses}>
                      {label}
                    </Link>
                  )}
                </div>
              );
            })}
          </nav>
        </div>

        <div className="hidden shrink-0 items-center lg:flex">
          <ThemeToggle className="mr-3" />
          <div className="border-l border-line pl-4">
            <PublicLanguageSwitcher />
          </div>
          <Link
            to="/espace-membre"
            className="ml-4 inline-flex min-h-[44px] items-center justify-center gap-2 whitespace-nowrap rounded-full bg-green px-6 py-3 text-[11px] font-bold uppercase tracking-[0.1em] text-white shadow-[0_8px_20px_rgba(0,59,27,.16)] transition-all duration-200 hover:-translate-y-0.5 hover:bg-green-deep focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-green"
          >
            <i className={hasMemberSession ? 'ri-user-smile-line text-base' : 'ri-user-add-line text-base'} aria-hidden="true" />
            {memberCtaLabel}
          </Link>
        </div>

        <button
          type="button"
          onClick={() => setIsMobileMenuOpen(true)}
          aria-label={t('public.nav.openMenu')}
          aria-expanded={isMobileMenuOpen}
          aria-controls="mobile-navigation"
          className="flex h-11 w-11 items-center justify-center rounded-full text-ink transition-colors hover:bg-green/8 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-green lg:hidden"
        >
          <i className="ri-menu-line text-2xl" aria-hidden="true"></i>
        </button>
      </div>
    </header>
    {mobileMenu}
    </>
  );
};

export default Navbar;

import { useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { LanguageSwitcher } from './LanguageSwitcher';
import { HcbeLogoMark } from '../brand/HcbeLogo';
import ThemeToggle from './ThemeToggle';
import { useAuth } from '../../contexts/AuthContext';
import { siteContentApi } from '../../lib/api/site-content';
import type { NavigationItemDto } from '../../lib/api/types';

const mobileNavIcons: Record<string, string> = {
  '/': 'ri-home-5-line',
  '/services': 'ri-service-line',
  '/actualites': 'ri-newspaper-line',
  '/engagement': 'ri-hand-heart-line',
  '/contact': 'ri-mail-send-line',
  '/contribuer': 'ri-hand-coin-line',
};

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
          { path: '/contribuer', labelKey: 'public.nav.contribute' },
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
  const openMobileMenu = () => {
    const activeParent = mainLinks.find((link) => link.dropdown && location.pathname.startsWith(link.path));
    setOpenDropdown(activeParent?.path || null);
    setIsMobileMenuOpen(true);
  };

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

          <div className="mobile-nav-panel public-grid-pattern absolute inset-0 flex h-[100dvh] w-full flex-col overflow-hidden bg-green-deep text-white">
            <div className="pointer-events-none absolute -right-24 top-28 h-72 w-72 rounded-full border-[52px] border-gold/[.055]" aria-hidden="true" />
            <div className="pointer-events-none absolute -bottom-8 -left-3 font-display text-[8rem] font-black leading-none tracking-[-.08em] text-white/[.025] sm:text-[12rem]" aria-hidden="true">HCBE</div>

            <div className="relative flex min-h-[74px] shrink-0 items-center justify-between border-b border-white/10 px-margin-mobile sm:px-7">
              <Link to="/" onClick={() => setIsMobileMenuOpen(false)} className="flex min-w-0 items-center">
                <HcbeLogoMark size="sm" tone="dark" />
              </Link>
              <div className="flex shrink-0 items-center gap-2">
                <ThemeToggle variant="onDark" />
                <button
                  type="button"
                  ref={closeButtonRef}
                  onClick={() => setIsMobileMenuOpen(false)}
                  aria-label={t('public.nav.closeMenu')}
                  className="flex h-11 w-11 items-center justify-center rounded-full border border-white/20 bg-white/[.06] text-white transition-all hover:rotate-3 hover:border-gold/60 hover:bg-white/[.1] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gold"
                >
                  <i className="ri-close-line text-2xl" aria-hidden="true"></i>
                </button>
              </div>
            </div>

            <nav id="mobile-navigation" className="relative min-h-0 flex-1 overflow-y-auto overscroll-contain px-margin-mobile pb-5 pt-6 sm:px-7 sm:pt-8">
              <div className="mb-5 flex items-end justify-between border-b border-white/10 pb-4">
                <div>
                  <p className="text-[9px] font-bold uppercase tracking-[.22em] text-gold">HCBE Canada</p>
                  <p className="mt-1 font-display text-2xl font-bold leading-none text-white">{t('public.nav.explore', { defaultValue: 'Explorer le site' })}</p>
                </div>
                <span className="pb-0.5 text-[9px] font-bold uppercase tracking-[.16em] text-white/35">Menu</span>
              </div>

              <div>
                {mainLinks.map((link, index) => {
                  const label = link.label || t(link.labelKey);
                  const active = isActiveLink(link.path, Boolean(link.dropdown));
                  const expanded = openDropdown === link.path;

                  return (
                    <div key={link.path} className="mobile-nav-entry border-b border-white/10" style={{ animationDelay: `${80 + index * 55}ms` }}>
                      {link.dropdown ? (
                        <div className="flex min-h-[66px] items-stretch">
                          <Link
                            to={link.path}
                            onClick={() => setIsMobileMenuOpen(false)}
                            className={`group relative flex flex-1 items-center gap-4 py-3 font-display text-[1.35rem] font-bold leading-tight transition-colors sm:text-[1.5rem] ${
                              active ? 'text-gold' : 'text-white hover:text-gold'
                            }`}
                          >
                            <span className={`w-6 font-sans text-[9px] font-bold tracking-[.16em] ${active ? 'text-gold' : 'text-white/30'}`}>{String(index + 1).padStart(2, '0')}</span>
                            <i className={`${mobileNavIcons[link.path] || 'ri-arrow-right-up-line'} text-base ${active ? 'text-gold' : 'text-white/35'}`} aria-hidden="true" />
                            <span>{label}</span>
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
                            className={`my-3 flex h-10 w-10 shrink-0 items-center justify-center rounded-full border transition-all ${
                              expanded ? 'rotate-45 border-gold bg-gold text-green-deep' : 'border-white/15 bg-white/[.04] text-white/65 hover:border-gold/60 hover:text-gold'
                            }`}
                          >
                            <i className="ri-add-line text-lg" aria-hidden="true"></i>
                          </button>
                        </div>
                      ) : (
                        <Link
                          to={link.path}
                          onClick={() => setIsMobileMenuOpen(false)}
                          className={`group relative flex min-h-[66px] items-center gap-4 py-3 font-display text-[1.35rem] font-bold leading-tight transition-colors sm:text-[1.5rem] ${
                            active ? 'text-gold' : 'text-white hover:text-gold'
                          }`}
                        >
                          <span className={`w-6 font-sans text-[9px] font-bold tracking-[.16em] ${active ? 'text-gold' : 'text-white/30'}`}>{String(index + 1).padStart(2, '0')}</span>
                          <i className={`${mobileNavIcons[link.path] || 'ri-arrow-right-up-line'} text-base ${active ? 'text-gold' : 'text-white/35'}`} aria-hidden="true" />
                          <span>{label}</span>
                          <i className="ri-arrow-right-up-line ml-auto text-base text-white/25 transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5 group-hover:text-gold" aria-hidden="true" />
                        </Link>
                      )}

                      {link.dropdown && expanded && (
                        <div id={`mobile-submenu-${link.path.replace(/\W/g, '')}`} className="mb-4 ml-10 grid gap-1 border-l border-gold/60 pl-5">
                          {link.dropdown.map((subLink) => (
                            <Link
                              key={subLink.path}
                              to={subLink.path}
                              onClick={() => setIsMobileMenuOpen(false)}
                              className={`flex min-h-[38px] items-center gap-3 text-[11px] font-bold uppercase tracking-[.1em] transition-colors ${
                                location.pathname === subLink.path
                                  ? 'text-gold'
                                  : 'text-white/55 hover:text-white'
                              }`}
                            >
                              <span className={`h-1.5 w-1.5 rotate-45 ${location.pathname === subLink.path ? 'bg-gold' : 'bg-white/25'}`} aria-hidden="true" />
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

            <div className="relative shrink-0 border-t border-white/10 bg-black/10 px-margin-mobile pb-[max(1rem,env(safe-area-inset-bottom))] pt-4 backdrop-blur-sm sm:px-7">
              <div className="flex items-center justify-between gap-4">
                <span className="text-[9px] font-bold uppercase tracking-[0.18em] text-white/45">
                  {t('public.nav.language', { defaultValue: 'Langue' })}
                </span>
                <LanguageSwitcher variant="onDark" compact />
              </div>
              <Link
                to="/espace-membre"
                onClick={() => setIsMobileMenuOpen(false)}
                className={`mt-3 flex min-h-[50px] w-full items-center justify-center gap-3 rounded-xl px-6 py-3 text-[10px] font-bold uppercase tracking-[0.12em] transition-all duration-200 hover:-translate-y-0.5 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 ${hasMemberSession ? 'bg-gold text-green-deep shadow-[0_12px_28px_rgba(252,209,22,.12)] hover:bg-[#ffe04d] focus-visible:outline-gold' : 'bg-red-link text-white shadow-[0_12px_28px_rgba(174,45,31,.22)] hover:bg-red-deep focus-visible:outline-red-link'}`}
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
            <LanguageSwitcher compact />
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
          onClick={openMobileMenu}
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

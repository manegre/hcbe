import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import PublicLanguageSwitcher from './PublicLanguageSwitcher';
import { HcbeLogoMark } from '../brand/HcbeLogo';
import ThemeToggle from './ThemeToggle';
import { siteContentApi } from '../../lib/api/site-content';
import type { NavigationItemDto } from '../../lib/api/types';

const Navbar = () => {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [openDropdown, setOpenDropdown] = useState<string | null>(null);
  const location = useLocation();
  const { t, i18n } = useTranslation();
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
    siteContentApi.getNavigation().then((response) => {
      if (response.success && response.data) setCmsNavigation(response.data);
    }).catch(() => undefined);
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

  return (
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
            {t('public.home.hero.cta.member')}
          </Link>
        </div>

        <button
          type="button"
          onClick={() => setIsMobileMenuOpen(true)}
          aria-label={t('public.nav.openMenu')}
          className="flex h-11 w-11 items-center justify-center text-ink lg:hidden"
        >
          <i className="ri-menu-line text-2xl" aria-hidden="true"></i>
        </button>
      </div>

      {isMobileMenuOpen && (
        <div
          role="dialog"
          aria-modal="true"
          aria-label={t('public.nav.openMenu')}
          className="fixed inset-0 z-50 flex flex-col bg-background lg:hidden"
        >
          <div className="flex h-16 w-full items-center justify-between border-b border-line bg-surface px-margin-mobile md:px-margin-desktop">
            <HcbeLogoMark size="sm" />
            <div className="flex items-center gap-1">
              <ThemeToggle />
              <button
                type="button"
                ref={closeButtonRef}
                onClick={() => setIsMobileMenuOpen(false)}
                aria-label={t('public.nav.closeMenu')}
                className="flex h-11 w-11 items-center justify-center"
              >
                <i className="ri-close-line text-2xl text-ink" aria-hidden="true"></i>
              </button>
            </div>
          </div>

          <nav className="flex-grow overflow-y-auto px-margin-mobile py-6">
            {mainLinks.map((link) => {
              const label = link.label || t(link.labelKey);
              const active = isActiveLink(link.path, Boolean(link.dropdown));

              return (
                <div key={link.path}>
                  {link.dropdown ? (
                    <div className="flex items-stretch border-t border-line">
                      <Link
                        to={link.path}
                        onClick={() => setIsMobileMenuOpen(false)}
                        className={`flex min-h-[56px] flex-1 items-center font-display text-headline-md ${
                          active ? 'text-green' : 'text-ink'
                        }`}
                      >
                        {label}
                      </Link>
                      <button
                        type="button"
                        onClick={() => handleDropdownToggle(link.path)}
                        aria-label={
                          openDropdown === link.path
                            ? t('public.nav.closeSubmenu', { label })
                            : t('public.nav.openSubmenu', { label })
                        }
                        aria-expanded={openDropdown === link.path}
                        className="flex w-11 items-center justify-center text-ink"
                      >
                        <i
                          className={`ri-arrow-${openDropdown === link.path ? 'up' : 'down'}-s-line text-xl`}
                          aria-hidden="true"
                        ></i>
                      </button>
                    </div>
                  ) : (
                    <Link
                      to={link.path}
                      onClick={() => setIsMobileMenuOpen(false)}
                      className={`flex min-h-[56px] items-center border-t border-line font-display text-headline-md ${
                        active ? 'text-green' : 'text-ink'
                      }`}
                    >
                      {label}
                    </Link>
                  )}

                  {link.dropdown && openDropdown === link.path && (
                    <div className="pl-4">
                      {link.dropdown.map((subLink) => (
                        <Link
                          key={subLink.path}
                          to={subLink.path}
                          onClick={() => setIsMobileMenuOpen(false)}
                          className={`flex min-h-[56px] items-center border-t border-line text-body-lg ${
                            location.pathname === subLink.path ? 'text-green' : 'text-ink-variant'
                          }`}
                        >
                          {t(subLink.labelKey)}
                        </Link>
                      ))}
                    </div>
                  )}
                </div>
              );
            })}
          </nav>

          <div className="border-t border-line bg-surface p-margin-mobile">
            <PublicLanguageSwitcher />
            <Link
              to="/espace-membre"
              className="mt-4 flex whitespace-nowrap min-h-[44px] w-full items-center justify-center gap-2 rounded-control bg-red-link px-6 py-3 text-label-md uppercase text-white transition-colors duration-200 hover:bg-red-deep focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-link"
            >
              {t('public.home.hero.cta.member')}
            </Link>
          </div>
        </div>
      )}
    </header>
  );
};

export default Navbar;

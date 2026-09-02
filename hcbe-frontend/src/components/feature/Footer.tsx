import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { HcbeLogo } from '../brand/HcbeLogo';
import { InstitutionalFlags } from '../brand/InstitutionalFlags';
import NewsletterSignup from './NewsletterSignup';
import { SOCIAL_LINKS } from '../../lib/social-links';
import { siteContentApi } from '../../lib/api/site-content';
import type { FooterLinkDto } from '../../lib/api/types';
import { openCookieSettings } from '../../lib/cookie-consent';

const linkClasses =
  'group flex min-h-[34px] items-center gap-2 text-sm text-white/70 transition-colors hover:text-white';

const Footer = () => {
  const { t, i18n } = useTranslation();
  const [cmsLinks, setCmsLinks] = useState<FooterLinkDto[]>([]);
  const currentYear = new Date().getFullYear();

  useEffect(() => {
    const loadFooter = () => siteContentApi.getFooter().then((response) => {
      if (response.success && response.data) setCmsLinks(response.data);
    }).catch(() => undefined);
    void loadFooter();
    window.addEventListener('hcbe:content-published', loadFooter);
    return () => window.removeEventListener('hcbe:content-published', loadFooter);
  }, []);

  const footerGroups = useMemo(() => {
    const english = i18n.language.startsWith('en');
    const links = cmsLinks.length > 0 ? cmsLinks : [
      { id: 'home', category: t('public.footer.navigation'), label: t('public.nav.home'), url: '/', displayOrder: 0 },
      { id: 'services', category: t('public.footer.navigation'), label: t('public.footer.services'), url: '/services', displayOrder: 1 },
      { id: 'news', category: t('public.footer.navigation'), label: t('public.footer.news'), url: '/actualites', displayOrder: 2 },
      { id: 'engagement', category: t('public.footer.navigation'), label: t('public.footer.engagement'), url: '/engagement', displayOrder: 3 },
      { id: 'email', category: t('public.footer.contacts'), label: 'contact@hcbecanada.org', url: 'mailto:contact@hcbecanada.org', displayOrder: 0 },
      { id: 'country', category: t('public.footer.contacts'), label: t('public.footer.country'), url: '/contact', displayOrder: 1 },
    ] as FooterLinkDto[];
    return Object.entries(links.reduce<Record<string, FooterLinkDto[]>>((groups, link) => {
      const category = english && link.categoryEn ? link.categoryEn : link.category;
      (groups[category] ||= []).push(link);
      return groups;
    }, {}));
  }, [cmsLinks, i18n.language, t]);

  return (
    <>
      <NewsletterSignup />
      <footer className="relative overflow-hidden bg-green-deep text-white">
        <div className="flex h-1" aria-hidden="true">
          <span className="w-1/3 bg-red" />
          <span className="w-1/3 bg-gold" />
          <span className="w-1/3 bg-green" />
        </div>

        <div
          className="pointer-events-none absolute -right-28 top-10 h-72 w-72 rounded-full border-[46px] border-white/[0.025]"
          aria-hidden="true"
        />
        <div
          className="pointer-events-none absolute bottom-2 left-1/2 hidden font-serif text-9xl font-black leading-none tracking-tighter text-white/5 lg:block"
          aria-hidden="true"
        >
          HCBE
        </div>

        <div className="container-page relative py-11 lg:py-14">
          <div className="site-footer-main-grid">
            <div>
              <HcbeLogo size="md" showWordmark subtitle={t('public.footer.tagline')} tone="dark" />
              <p className="mt-4 max-w-md text-sm leading-6 text-white/65">
                {t('public.footer.description')}
              </p>
              <InstitutionalFlags variant="signature" className="mt-6" />
            </div>

            <div className="site-footer-link-grid border-t border-white/10 pt-8 lg:border-l lg:border-t-0 lg:pl-10 lg:pt-0">
              {footerGroups.map(([category, links]) => (
                <div key={category}>
                  <h3 className="text-[10px] font-bold uppercase tracking-[0.2em] text-gold">{category}</h3>
                  <ul className="mt-3 flex flex-col">
                    {[...links].sort((a, b) => a.displayOrder - b.displayOrder).map((link) => {
                      const label = i18n.language.startsWith('en') && link.labelEn ? link.labelEn : link.label;
                      const content = <><span className="h-px w-3 bg-gold/70 transition-all group-hover:w-5" />{label}</>;
                      return <li key={link.id}>{link.url.startsWith('/') ? <Link to={link.url} className={linkClasses}>{content}</Link> : <a href={link.url} className={linkClasses}>{content}</a>}</li>;
                    })}
                  </ul>
                </div>
              ))}

              <div>
                <h3 className="text-[10px] font-bold uppercase tracking-[0.2em] text-gold">
                  {t('public.footer.follow')}
                </h3>
                <div className="mt-4 flex flex-wrap gap-2">
                  {SOCIAL_LINKS.map((network) => (
                    <a
                      key={network.id}
                      href={network.href}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex h-10 w-10 items-center justify-center rounded-full border border-white/15 bg-white/[0.06] text-white/80 transition-all hover:-translate-y-0.5 hover:border-gold hover:bg-gold hover:text-green-deep focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gold"
                      aria-label={network.label}
                    >
                      <i className={`${network.iconClass} text-lg`} aria-hidden="true" />
                    </a>
                  ))}
                </div>
                <p className="mt-4 max-w-[13rem] text-sm leading-6 text-white/55">
                  {t('public.footer.followHint')}
                </p>
              </div>
            </div>
          </div>
        </div>

        <div className="relative border-t border-white/10 bg-black/10 py-4 text-xs text-white/50">
          <div className="container-page flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <span>{t('public.footer.copyright', { year: currentYear })}</span>
            <div className="flex items-center gap-4">
              <Link to="/contact" className="transition-colors hover:text-gold">
                {t('public.footer.contacts')}
              </Link>
              <Link to="/confidentialite" className="transition-colors hover:text-gold">
                {t('public.newsletter.privacyLink')}
              </Link>
              <button type="button" onClick={openCookieSettings} className="transition-colors hover:text-gold">
                {t('public.cookies.manage')}
              </button>
            </div>
          </div>
        </div>
      </footer>
    </>
  );
};

export default Footer;

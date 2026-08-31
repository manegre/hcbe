import Navbar from '../../components/feature/Navbar';
import Footer from '../../components/feature/Footer';
import { PageHeader } from '../../components/ui';

const sections = [
  { id: 'newsletter', titleKey: 'public.privacy.newsletterTitle', bodyKey: 'public.privacy.newsletterBody' },
  { id: 'analytics', titleKey: 'public.privacy.analyticsTitle', bodyKey: 'public.privacy.analyticsBody' },
  { id: 'retention', titleKey: 'public.privacy.retentionTitle', bodyKey: 'public.privacy.retentionBody' },
  { id: 'rights', titleKey: 'public.privacy.rightsTitle', bodyKey: 'public.privacy.rightsBody' },
] as const;

const PrivacyPage = () => {
  const { t } = useTranslation();

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <PageHeader variant="interior" title={t('public.privacy.title')} description={t('public.privacy.subtitle')} />

      <main className="container-page py-16 md:py-24">
        <div className="mx-auto max-w-[65ch]">
          <nav aria-label={t('public.privacy.toc')} className="border border-line bg-surface p-6">
            <p className="text-label-md uppercase text-ink-variant">{t('public.privacy.toc')}</p>
            <ol className="mt-4 space-y-3">
              {sections.map((section, index) => (
                <li key={section.id}>
                  <a
                    href={`#${section.id}`}
                    className="flex items-baseline gap-3 text-body-md text-gold-ink hover:text-green"
                  >
                    <span className="text-label-md text-ink-variant">{String(index + 1).padStart(2, '0')}</span>
                    {t(section.titleKey)}
                  </a>
                </li>
              ))}
            </ol>
          </nav>

          {sections.map((section) => (
            <div key={section.id} id={section.id} className="mt-8 scroll-mt-24 border-t border-line pt-8">
              <h2 className="font-display text-headline-md text-green">{t(section.titleKey)}</h2>
              <p className="mt-3 text-body-md leading-7 text-ink-variant">{t(section.bodyKey)}</p>
            </div>
          ))}

          <p className="mt-8 border-t border-line pt-8 text-body-md text-ink-variant">
            {t('public.privacy.contact')}
          </p>
        </div>
      </main>

      <Footer />
    </div>
  );
};

export default PrivacyPage;

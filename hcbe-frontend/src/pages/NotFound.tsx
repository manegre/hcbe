import Navbar from '../components/feature/Navbar';
import Footer from '../components/feature/Footer';
import { Button } from '../components/ui';

const destinations = [
  { path: '/services', labelKey: 'public.nav.services' },
  { path: '/actualites', labelKey: 'public.nav.news' },
  { path: '/engagement/annuaire', labelKey: 'public.nav.associations' },
  { path: '/contact', labelKey: 'public.nav.contact' },
] as const;

export default function NotFound() {
  const { t } = useTranslation();

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <main className="container-page py-24 text-left">
        <p className="text-label-md uppercase text-red-link">{t('public.notFound.eyebrow')}</p>
        <h1 className="mt-3 font-display text-headline-xl-m text-green md:text-headline-xl">{t('public.notFound.title')}</h1>
        <p className="mt-4 max-w-2xl text-body-lg text-ink-variant">{t('public.notFound.subtitle')}</p>

        <div className="mt-8">
          <Button variant="primary" to="/">
            {t('public.notFound.cta')}
          </Button>
        </div>

        <ul className="mt-12 max-w-md divide-y divide-line border-t border-line">
          {destinations.map((destination) => (
            <li key={destination.path}>
              <Link
                to={destination.path}
                className="flex items-center justify-between gap-4 py-4 text-body-md text-ink transition-colors hover:text-green"
              >
                {t(destination.labelKey)}
                <i className="ri-arrow-right-line text-green" aria-hidden="true"></i>
              </Link>
            </li>
          ))}
        </ul>
      </main>

      <Footer />
    </div>
  );
}

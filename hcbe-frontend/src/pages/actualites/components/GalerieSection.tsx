import { Link } from 'react-router-dom';
import { eventsApi } from '../../../lib/api/events';
import type { Event } from '../../../lib/api/types';
import { getEventLifecycle } from '../../../lib/events/lifecycle';
import { resolveMediaUrl } from '../../../lib/api/media-url';
import { localized } from '../../../lib/i18n/localized';
import { EmptyState } from '../../../components/ui';

const GalerieSection = () => {
  const { t, i18n } = useTranslation();
  const [events, setEvents] = useState<Event[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const load = async () => {
      try {
        setIsLoading(true);
        const response = await eventsApi.getEvents();
        if (response.success && response.data) {
          const withMemories = response.data
            .filter((event) => getEventLifecycle(event) === 'past')
            .filter((event) => (event.media?.length ?? 0) > 0)
            .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
          setEvents(withMemories);
        } else {
          setError(t('public.news.souvenirs.empty.error'));
        }
      } catch (err) {
        console.error('Error loading souvenirs:', err);
        setError(t('public.news.souvenirs.empty.error'));
      } finally {
        setIsLoading(false);
      }
    };

    void load();
  }, [t]);

  const locale = i18n.language.startsWith('fr') ? 'fr-CA' : 'en-CA';

  const formatDate = (dateString: string) =>
    new Date(dateString).toLocaleDateString(locale, {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });

  const coverFor = (event: Event) => {
    const firstImage = event.media?.find((m) => m.mediaType === 'image');
    if (firstImage) return resolveMediaUrl(firstImage.url);
    if (event.imageUrl) return resolveMediaUrl(event.imageUrl);
    return null;
  };

  return (
    <section className="bg-white py-16 md:py-20">
      <div className="container-page">
        <div className="mb-10 grid gap-6 border-b border-line pb-8 lg:grid-cols-[0.85fr_1.15fr] lg:items-end">
          <div>
            <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-green/10 bg-background px-4 py-2 text-[10px] font-bold uppercase tracking-[0.14em] text-green">
              <i className="ri-camera-line" aria-hidden="true"></i>
              {t('public.news.souvenirs.archivesBadge')}
            </div>
            <h2 className="font-display text-headline-lg text-green">
              {t('public.news.page.cards.memories.title')}
            </h2>
          </div>
          <p className="max-w-2xl text-body-md leading-7 text-ink-variant">{t('public.news.souvenirs.archivesIntro')}</p>
        </div>

        {isLoading && (
          <div className="grid grid-cols-1 gap-gutter md:grid-cols-2">
            {[1, 2].map((item) => (
              <div key={item} className="space-y-4">
                <div className="aspect-[4/3] w-full animate-pulse bg-surface-container" />
                <div className="h-6 w-2/3 animate-pulse bg-surface-container" />
              </div>
            ))}
          </div>
        )}

        {!isLoading && error && (
          <EmptyState tone="error" icon="ri-error-warning-line" title={error} />
        )}

        {!isLoading && !error && events.length === 0 && (
          <EmptyState
            icon="ri-image-line"
            title={t('public.news.souvenirs.empty.title')}
            description={t('public.news.souvenirs.empty.description')}
          />
        )}

        {!isLoading && events.length > 0 && (
          <div className="grid grid-cols-1 gap-gutter md:grid-cols-2">
            {events.map((event) => {
              const cover = coverFor(event);
              const photoCount = event.media?.filter((m) => m.mediaType === 'image').length ?? 0;
              const videoCount = event.media?.filter((m) => m.mediaType === 'video').length ?? 0;

              return (
                <Link key={event.id} to={`/actualites/evenements/${event.id}`} className="group block overflow-hidden rounded-[20px] border border-green/10 bg-background transition-all hover:-translate-y-1 hover:shadow-[0_22px_50px_rgba(0,59,27,.12)]">
                  <div className="relative aspect-[4/3] overflow-hidden bg-surface-container">
                    {cover ? (
                      <img
                        src={cover}
                        alt=""
                        className="h-full w-full object-cover transition duration-500 group-hover:scale-105"
                      />
                    ) : (
                      <div className="flex h-full items-center justify-center text-ink-variant">
                        <i className="ri-play-circle-line text-5xl" aria-hidden="true" />
                      </div>
                    )}
                    <span className="absolute left-4 top-4 inline-flex items-center gap-2 rounded-full bg-green-deep/90 px-3 py-2 text-[10px] font-bold uppercase tracking-[0.12em] text-white backdrop-blur">
                      <i className="ri-gallery-line text-gold" aria-hidden="true" />
                      {photoCount + videoCount}
                    </span>
                  </div>
                  <div className="p-6">
                    <h3 className="font-display text-headline-md text-green">
                      {localized(event.title, event.titleEn, i18n.language)}
                    </h3>
                    <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-ink-variant">
                      <span className="inline-flex items-center gap-2">
                        <i className="ri-calendar-line" aria-hidden="true" />
                        {formatDate(event.date)}
                      </span>
                      {photoCount > 0 && <span>{t('public.news.souvenirs.photoCount', { count: photoCount })}</span>}
                      {videoCount > 0 && <span>{t('public.news.souvenirs.videoCount', { count: videoCount })}</span>}
                    </div>
                    <span className="mt-5 inline-flex items-center gap-2 text-[10px] font-bold uppercase tracking-[0.12em] text-red-link">
                      {t('public.common.discover')}
                      <i className="ri-arrow-right-line transition-transform group-hover:translate-x-1" aria-hidden="true" />
                    </span>
                  </div>
                </Link>
              );
            })}
          </div>
        )}
      </div>
    </section>
  );
};

export default GalerieSection;

import { newsApi } from '../../../lib/api/news';
import type { NewsArticle } from '../../../lib/api/types';
import { localized, localizedOptional } from '../../../lib/i18n/localized';
import { ArrowLink, EmptyState, SectionHeading } from '../../../components/ui';

const MAX_ANNOUNCEMENTS = 6;

const sortByRecency = (a: NewsArticle, b: NewsArticle) => {
  if (a.isPinned !== b.isPinned) {
    return a.isPinned ? -1 : 1;
  }
  const dateA = new Date(a.publishedDate || a.createdAt).getTime();
  const dateB = new Date(b.publishedDate || b.createdAt).getTime();
  return dateB - dateA;
};

const RecentAnnouncementsSection = () => {
  const { t, i18n } = useTranslation();
  const [announcements, setAnnouncements] = useState<NewsArticle[]>([]);
  const [isReady, setIsReady] = useState(false);
  const [hasError, setHasError] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const loadAnnouncements = async () => {
      try {
        const response = await newsApi.getPublishedNews();
        if (cancelled) return;

        if (response.success && response.data) {
          setAnnouncements([...response.data].sort(sortByRecency).slice(0, MAX_ANNOUNCEMENTS));
        }
      } catch (error) {
        console.error('Error loading home announcements:', error);
        if (!cancelled) {
          setHasError(true);
        }
      } finally {
        if (!cancelled) {
          setIsReady(true);
        }
      }
    };

    loadAnnouncements();
    return () => {
      cancelled = true;
    };
  }, []);

  const locale = i18n.language.startsWith('fr') ? 'fr-CA' : 'en-CA';

  const formatDate = (dateString: string) =>
    new Intl.DateTimeFormat(locale, {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    }).format(new Date(dateString));

  return (
    <section className="bg-background py-24">
      <div className="container-page">
        <SectionHeading
          title={t('public.home.announcements.title')}
          description={t('public.home.announcements.subtitle')}
          action={
            <ArrowLink to="/actualites/annonces" tone="red">
              {t('public.home.announcements.viewAll')}
            </ArrowLink>
          }
        />

        {!isReady ? (
          <div className="border-b border-line">
            {[1, 2, 3].map((item) => (
              <div key={item} className="grid grid-cols-1 gap-6 border-t border-line py-8 md:grid-cols-[120px_1fr]">
                <div className="h-4 w-24 animate-pulse bg-surface-container" />
                <div className="space-y-3">
                  <div className="h-6 w-2/3 animate-pulse bg-surface-container" />
                  <div className="h-4 w-full animate-pulse bg-surface-container" />
                  <div className="h-4 w-32 animate-pulse bg-surface-container" />
                </div>
              </div>
            ))}
          </div>
        ) : hasError ? (
          <EmptyState
            tone="error"
            icon="ri-error-warning-line"
            title={t('public.home.announcements.error.title')}
            description={t('public.home.announcements.error.description')}
          />
        ) : announcements.length === 0 ? (
          <EmptyState
            icon="ri-megaphone-line"
            title={t('public.home.announcements.empty.title')}
            description={t('public.home.announcements.empty.description')}
          />
        ) : (
          <div className="border-b border-line">
            {announcements.map((item) => {
              const publishedAt = item.publishedDate || item.createdAt;
              const excerpt = localizedOptional(item.excerpt, item.excerptEn, i18n.language);
              const content = localized(item.content, item.contentEn, i18n.language);
              const preview = excerpt || content;

              return (
                <article
                  key={item.id}
                  className="grid grid-cols-1 gap-6 border-t border-line py-8 md:grid-cols-[120px_1fr]"
                >
                  <p className="text-label-md uppercase text-red-link">{formatDate(publishedAt)}</p>
                  <div>
                    <h3 className="font-display text-headline-md text-ink">
                      {localized(item.title, item.titleEn, i18n.language)}
                    </h3>
                    {preview && (
                      <p className="mt-2 line-clamp-3 text-body-md text-ink-variant">{preview}</p>
                    )}
                    <ArrowLink to={`/actualites/annonces/${item.id}`} tone="red" className="mt-4">
                      {t('public.home.announcements.readMore')}
                    </ArrowLink>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </div>
    </section>
  );
};

export default RecentAnnouncementsSection;

import { newsApi } from '../../../lib/api/news';
import type { NewsArticle } from '../../../lib/api/types';
import { localized, localizedOptional } from '../../../lib/i18n/localized';
import { ArrowLink, EmptyState, Tag } from '../../../components/ui';

interface AnnoncesExemplesProps {
  selectedCategory: string;
}

const formatDate = (dateString: string, locale: string) =>
  new Intl.DateTimeFormat(locale === 'en' ? 'en-CA' : 'fr-CA', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(new Date(dateString));

const AnnoncesExemples = ({ selectedCategory }: AnnoncesExemplesProps) => {
  const { t, i18n } = useTranslation();
  const [articles, setArticles] = useState<NewsArticle[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    const loadNews = async () => {
      try {
        const response = await newsApi.getPublishedNews();
        if (response.success && response.data) {
          setArticles(response.data);
        } else {
          setError(true);
        }
      } catch (err) {
        console.error('Error loading news:', err);
        setError(true);
      } finally {
        setLoading(false);
      }
    };

    loadNews();
  }, []);

  const filteredNews =
    selectedCategory === 'all'
      ? articles
      : articles.filter((item) => item.category === selectedCategory);

  const sortedNews = [...filteredNews].sort((a, b) => {
    if (a.isPinned !== b.isPinned) {
      return a.isPinned ? -1 : 1;
    }
    const dateA = new Date(a.publishedDate || a.createdAt).getTime();
    const dateB = new Date(b.publishedDate || b.createdAt).getTime();
    return dateB - dateA;
  });

  if (loading) {
    return (
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {[1, 2, 3].map((item) => (
          <div key={item} className="space-y-3 rounded-xl bg-background p-5">
            <div className="h-4 w-24 animate-pulse bg-surface-container" />
            <div className="h-6 w-2/3 animate-pulse bg-surface-container" />
            <div className="h-4 w-full animate-pulse bg-surface-container" />
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <EmptyState tone="error" icon="ri-error-warning-line" title={t('public.news.annonces.errorLoad')} />
    );
  }

  if (sortedNews.length === 0) {
    return (
      <EmptyState
        icon="ri-newspaper-line"
        title={t('public.news.annonces.emptyCategory')}
        description={t('public.news.annonces.emptyCategoryHint')}
      />
    );
  }

  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      {sortedNews.slice(0, 3).map((item) => {
        const publishedAt = item.publishedDate || item.createdAt;
        const excerpt = localizedOptional(item.excerpt, item.excerptEn, i18n.language);
        const content = localized(item.content, item.contentEn, i18n.language);
        const preview = excerpt || content;

        return (
          <article key={item.id} className="group h-full rounded-xl border border-green/10 bg-white p-5 transition-all hover:-translate-y-0.5 hover:shadow-[0_12px_30px_rgba(0,59,27,.07)]">
            <div className="flex items-start gap-4">
              <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-gold text-green-deep">
                <i className="ri-megaphone-line text-lg" aria-hidden="true" />
              </span>
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-3">
                  {item.isPinned && <Tag>{t('public.news.annonces.pinned')}</Tag>}
                  <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-red-link">{formatDate(publishedAt, i18n.language)}</p>
                </div>
                <h3 className="mt-2 font-display text-xl font-semibold leading-tight text-green">
                  {localized(item.title, item.titleEn, i18n.language)}
                </h3>
                {preview && <p className="mt-3 line-clamp-2 text-sm leading-6 text-ink-variant">{preview}</p>}
                <ArrowLink to={`/actualites/annonces/${item.id}`} tone="red" className="mt-4">
                  {t('public.news.annonces.readMore')}
                </ArrowLink>
              </div>
            </div>
          </article>
        );
      })}
    </div>
  );
};

export default AnnoncesExemples;

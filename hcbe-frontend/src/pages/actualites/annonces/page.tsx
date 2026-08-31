import Navbar from '../../../components/feature/Navbar';
import Footer from '../../../components/feature/Footer';
import { newsApi } from '../../../lib/api/news';
import type { NewsArticle } from '../../../lib/api/types';
import { resolveMediaUrl } from '../../../lib/api/media-url';
import { localized, localizedOptional } from '../../../lib/i18n/localized';
import { getNewsCategoryLabelKey } from '../../../lib/news/category-styles';
import { ArrowLink, Button, EmptyState, PageHeader, Tag } from '../../../components/ui';

const categoryKeys = [
  { id: 'all', labelKey: 'public.news.categories.all.label', fullLabelKey: 'public.news.categories.all.fullLabel' },
  {
    id: 'Communiqué Officiel',
    labelKey: 'public.news.categories.official.label',
    fullLabelKey: 'public.news.categories.official.fullLabel',
  },
  {
    id: 'Éducation',
    labelKey: 'public.news.categories.education.label',
    fullLabelKey: 'public.news.categories.education.fullLabel',
  },
  {
    id: 'Événement',
    labelKey: 'public.news.categories.event.label',
    fullLabelKey: 'public.news.categories.event.fullLabel',
  },
  {
    id: 'Service',
    labelKey: 'public.news.categories.service.label',
    fullLabelKey: 'public.news.categories.service.fullLabel',
  },
  {
    id: 'Solidarité',
    labelKey: 'public.news.categories.solidarity.label',
    fullLabelKey: 'public.news.categories.solidarity.fullLabel',
  },
  {
    id: 'Formation',
    labelKey: 'public.news.categories.training.label',
    fullLabelKey: 'public.news.categories.training.fullLabel',
  },
  {
    id: 'Annonce',
    labelKey: 'public.news.categories.announcement.label',
    fullLabelKey: 'public.news.categories.announcement.fullLabel',
  },
  {
    id: 'Partenariat',
    labelKey: 'public.news.categories.partnership.label',
    fullLabelKey: 'public.news.categories.partnership.fullLabel',
  },
] as const;

const formatDate = (dateString: string, locale: string) =>
  new Intl.DateTimeFormat(locale === 'en' ? 'en-CA' : 'fr-CA', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(new Date(dateString));

const AnnoncesPage = () => {
  const { t, i18n } = useTranslation();
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
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
    selectedCategory === 'all' ? articles : articles.filter((item) => item.category === selectedCategory);

  const sortedNews = [...filteredNews].sort((a, b) => {
    if (a.isPinned !== b.isPinned) {
      return a.isPinned ? -1 : 1;
    }
    const dateA = new Date(a.publishedDate || a.createdAt).getTime();
    const dateB = new Date(b.publishedDate || b.createdAt).getTime();
    return dateB - dateA;
  });

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <PageHeader
        variant="hero"
        title={t('public.news.annonces.title')}
        description={t('public.news.annonces.subtitle')}
        aside={
          <div className="border border-white/25 p-6">
            <p className="text-label-md uppercase text-gold">{t('public.news.annonces.remember.label')}</p>
            <p className="mt-4 font-display text-headline-md text-white">
              {t('public.news.annonces.remember.title')}
            </p>
            <p className="mt-3 text-body-md text-green-dim">{t('public.news.annonces.remember.description')}</p>
          </div>
        }
      />

      <main className="bg-surface-container py-12 md:py-16">
        <div className="container-page">
          <div className="rounded-[18px] border border-green/10 bg-white p-5 shadow-[0_14px_35px_rgba(0,59,27,.06)]">
            <p className="mb-3 text-[10px] font-bold uppercase tracking-[0.14em] text-green">{t('public.news.annonces.filter.label')}</p>
            <div className="flex flex-wrap gap-2">
              {categoryKeys.map((category) => (
                <button
                  key={category.id}
                  type="button"
                  title={t(category.fullLabelKey)}
                  onClick={() => setSelectedCategory(category.id)}
                  className={`min-h-[40px] rounded-full border px-4 py-2 text-[10px] font-bold uppercase tracking-[0.1em] transition-colors ${
                    selectedCategory === category.id
                      ? 'border-green-deep bg-green-deep text-white'
                      : 'border-green/10 bg-background text-ink-variant hover:border-green hover:text-green'
                  }`}
                >
                  {t(category.labelKey)}
                </button>
              ))}
            </div>
          </div>

          <div className="mt-8">
            {loading ? (
              <div className="grid gap-5 md:grid-cols-2">
                {[1, 2, 3, 4].map((item) => (
                  <div
                    key={item}
                    className="overflow-hidden rounded-[18px] border border-green/10 bg-white"
                  >
                    <div className="h-48 w-full animate-pulse bg-surface-container" />
                    <div className="space-y-3 p-6">
                      <div className="h-4 w-1/3 animate-pulse bg-surface-container" />
                      <div className="h-6 w-2/3 animate-pulse bg-surface-container" />
                      <div className="h-4 w-full animate-pulse bg-surface-container" />
                    </div>
                  </div>
                ))}
              </div>
            ) : error ? (
              <EmptyState tone="error" icon="ri-error-warning-line" title={t('public.news.annonces.errorLoad')} />
            ) : sortedNews.length === 0 ? (
              <EmptyState
                icon="ri-newspaper-line"
                title={t('public.news.annonces.emptyCategory')}
                description={t('public.news.annonces.emptyCategoryHint')}
              />
            ) : (
              <div className="grid gap-5 md:grid-cols-2">
                {sortedNews.map((item) => {
                  const publishedAt = item.publishedDate || item.createdAt;
                  const excerpt = localizedOptional(item.excerpt, item.excerptEn, i18n.language);
                  const content = localized(item.content, item.contentEn, i18n.language);
                  const preview = excerpt || content;
                  const categoryLabelKey = getNewsCategoryLabelKey(item.category);
                  const thumbnail = resolveMediaUrl(item.imageUrl);

                  return (
                    <article
                      key={item.id}
                      className="group flex min-h-full flex-col overflow-hidden rounded-[18px] border border-green/10 bg-white transition-all hover:-translate-y-0.5 hover:shadow-[0_20px_48px_rgba(0,59,27,.11)]"
                    >
                      <div className="flex flex-1 flex-col p-6">
                        <div className="flex flex-wrap items-center gap-3">
                          {item.isPinned && <Tag>{t('public.news.annonces.pinned')}</Tag>}
                          <p className="text-label-md uppercase text-ink-variant">
                            {formatDate(publishedAt, i18n.language)}
                            {categoryLabelKey ? ` · ${t(categoryLabelKey)}` : ''}
                          </p>
                        </div>
                        <h3 className="mt-3 font-display text-headline-md leading-tight text-green">
                          {localized(item.title, item.titleEn, i18n.language)}
                        </h3>
                        {preview && <p className="mt-3 line-clamp-3 text-body-md text-ink-variant">{preview}</p>}
                        <ArrowLink to={`/actualites/annonces/${item.id}`} tone="red" className="mt-auto pt-6">
                          {t('public.news.annonces.readMore')}
                        </ArrowLink>
                      </div>
                      <div className="relative order-first flex h-48 w-full items-center justify-center overflow-hidden bg-green-deep text-gold">
                        <i className="ri-newspaper-line text-4xl" aria-hidden="true" />
                        {thumbnail && (
                          <img
                            src={thumbnail}
                            alt=""
                            className="absolute inset-0 h-full w-full object-cover transition-transform duration-500 group-hover:scale-[1.02]"
                            onError={(event) => {
                              event.currentTarget.style.display = 'none';
                            }}
                          />
                        )}
                      </div>
                    </article>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </main>

      <section className="bg-white py-12 md:py-16">
        <div className="container-page">
          <div className="public-grid-pattern flex flex-col gap-8 overflow-hidden rounded-[20px] bg-green-deep p-8 shadow-[0_20px_48px_rgba(0,59,27,.14)] md:flex-row md:items-center md:justify-between md:p-10">
          <div className="md:max-w-2xl">
            <h2 className="font-display text-headline-lg text-white">{t('public.news.annonces.cta.title')}</h2>
            <p className="mt-4 text-body-md text-green-dim">{t('public.news.annonces.cta.description')}</p>
          </div>
          <Button to="/contact" variant="primary">
            {t('public.common.writeToUs')}
          </Button>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
};

export default AnnoncesPage;

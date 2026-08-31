import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import Navbar from '../../../../components/feature/Navbar';
import Footer from '../../../../components/feature/Footer';
import ImageCarousel from '../../../../components/media/ImageCarousel';
import { newsApi } from '../../../../lib/api/news';
import type { NewsArticle } from '../../../../lib/api/types';
import { formatFileSize, resolveMediaUrl } from '../../../../lib/api/media-url';
import { isImageFile } from '../../../../lib/media/is-image-file';
import { getNewsCategoryLabelKey } from '../../../../lib/news/category-styles';
import { newsImageObjectPositionClass } from '../../../../lib/news/image-position';
import { localized } from '../../../../lib/i18n/localized';
import { ArrowLink, Button, EmptyState, Tag } from '../../../../components/ui';

const formatDate = (dateString: string, locale: string) =>
  new Intl.DateTimeFormat(locale.startsWith('en') ? 'en-CA' : 'fr-CA', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(new Date(dateString));

const AnnonceDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const [article, setArticle] = useState<NewsArticle | null>(null);
  const [loading, setLoading] = useState(true);
  const [coverBroken, setCoverBroken] = useState(false);

  useEffect(() => {
    if (!id) return;

    let cancelled = false;

    const loadArticle = async () => {
      setLoading(true);
      setCoverBroken(false);
      try {
        const response = await newsApi.getNewsById(id);
        if (cancelled) return;

        if (response.success && response.data) {
          setArticle(response.data);
        } else {
          setArticle(null);
          setTimeout(() => navigate('/actualites/annonces'), 2000);
        }
      } catch (error) {
        console.error('Error loading announcement:', error);
        if (!cancelled) {
          setArticle(null);
          setTimeout(() => navigate('/actualites/annonces'), 2000);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    loadArticle();
    return () => {
      cancelled = true;
    };
  }, [id, navigate]);

  const { imageAttachments, fileAttachments } = useMemo(() => {
    const attachments = article?.attachments ?? [];
    return {
      imageAttachments: attachments.filter((attachment) =>
        isImageFile(attachment.contentType, attachment.fileName),
      ),
      fileAttachments: attachments.filter(
        (attachment) => !isImageFile(attachment.contentType, attachment.fileName),
      ),
    };
  }, [article]);

  if (loading) {
    return (
      <div className="min-h-screen bg-background">
        <Navbar />
        <div className="container-page py-16">
          <div className="space-y-4">
            <div className="h-4 w-40 animate-pulse bg-surface-container" />
            <div className="h-10 w-2/3 animate-pulse bg-surface-container" />
            <div className="h-64 w-full animate-pulse bg-surface-container" />
          </div>
        </div>
        <Footer />
      </div>
    );
  }

  if (!article) {
    return (
      <div className="min-h-screen bg-background">
        <Navbar />
        <div className="container-page py-16">
          <EmptyState
            icon="ri-newspaper-line"
            title={t('public.news.annonces.notFound')}
            action={
              <ArrowLink to="/actualites/annonces" tone="red">
                {t('public.news.annonces.backToList')}
              </ArrowLink>
            }
          />
        </div>
        <Footer />
      </div>
    );
  }

  const categoryLabel = article.category
    ? getNewsCategoryLabelKey(article.category)
      ? t(getNewsCategoryLabelKey(article.category)!)
      : article.category
    : undefined;
  const publishedAt = article.publishedDate || article.createdAt;
  const showCover = Boolean(article.imageUrl) && !coverBroken;
  const coverObjectPosition = newsImageObjectPositionClass(article.imagePosition);

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <header className="public-grid-pattern relative overflow-hidden bg-green-deep py-12 md:py-16">
        <div className="pointer-events-none absolute -right-20 -top-24 h-72 w-72 rounded-full border-[48px] border-white/5" aria-hidden="true" />
        <div className="container-page">
          <Link
            to="/actualites/annonces"
            className="inline-flex min-h-[44px] items-center gap-2 text-[10px] font-bold uppercase tracking-[0.14em] text-white/70 transition-colors hover:text-gold"
          >
            <i className="ri-arrow-left-line" aria-hidden="true"></i>
            {t('public.news.annonces.backToList')}
          </Link>

          <div className="mt-6 flex flex-wrap items-center gap-4">
            <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[10px] font-bold uppercase tracking-[0.12em] text-white/65">
              {categoryLabel && <span>{categoryLabel}</span>}
              <time dateTime={publishedAt}>{formatDate(publishedAt, i18n.language)}</time>
              {article.author && <span>{article.author}</span>}
            </div>
            {article.isPinned && (
              <Tag>
                <i className="ri-pushpin-line mr-1" aria-hidden="true"></i>
                {t('public.news.annonces.pinned')}
              </Tag>
            )}
          </div>

          <h1 className="mt-5 max-w-4xl font-display text-[36px] font-bold leading-[1.06] tracking-[-0.03em] text-white md:text-[56px]">
            {localized(article.title, article.titleEn, i18n.language)}
          </h1>
        </div>
      </header>

      <main className="bg-surface-container py-12 md:py-16">
        <div className="container-page">
          {showCover && (
            <img
              src={resolveMediaUrl(article.imageUrl)}
              alt=""
              className={`h-[360px] w-full rounded-[20px] object-cover shadow-[0_22px_55px_rgba(0,59,27,.14)] md:h-[460px] ${coverObjectPosition}`}
              onError={() => setCoverBroken(true)}
            />
          )}

          <article className={`max-w-4xl rounded-[20px] border border-green/10 bg-white p-6 shadow-[0_18px_48px_rgba(0,59,27,.07)] md:p-10 ${showCover ? 'mt-8' : ''}`}>
            <div className="max-w-[65ch] whitespace-pre-line text-[17px] leading-8 text-ink-variant">
              {localized(article.content, article.contentEn, i18n.language)}
            </div>

            {(imageAttachments.length > 0 || fileAttachments.length > 0) && (
              <section className="mt-16">
                <h2 className="border-b border-line pb-4 font-display text-headline-md text-green">
                  {t('public.news.annonces.attachments')}
                </h2>

                {imageAttachments.length > 0 && (
                  <div className="mt-8">
                    <h3 className="text-label-md uppercase text-ink-variant">
                      {t('public.news.annonces.photos')}
                    </h3>
                    <div className="mt-4">
                      <ImageCarousel
                        images={imageAttachments.map((attachment) => ({
                          id: attachment.id,
                          url: attachment.url,
                          alt: attachment.fileName,
                        }))}
                      />
                    </div>
                  </div>
                )}

                {fileAttachments.length > 0 && (
                  <ul className="mt-8 divide-y divide-line overflow-hidden rounded-xl border border-line bg-background px-5">
                    {fileAttachments.map((attachment) => (
                      <li key={attachment.id} className="flex flex-wrap items-center justify-between gap-4 py-4">
                        <div>
                          <p className="text-body-md text-ink">{attachment.fileName}</p>
                          <p className="mt-1 text-label-md uppercase text-ink-variant">
                            {(attachment.contentType || '').split('/').pop() || 'FICHIER'} ·{' '}
                            {formatFileSize(attachment.sizeBytes)}
                          </p>
                        </div>
                        <a
                          href={resolveMediaUrl(attachment.url)}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="inline-flex min-h-[44px] items-center gap-2 text-label-md uppercase text-red-link transition-colors hover:text-green"
                        >
                          {t('public.news.annonces.download')}
                          <i className="ri-arrow-right-line" aria-hidden="true"></i>
                        </a>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            )}

            <div className="mt-12 flex flex-wrap items-center gap-6 border-t border-line pt-8">
              <Button to="/contact" variant="secondary">
                {t('public.news.annonces.askQuestion')}
              </Button>
              <ArrowLink to="/actualites/evenements">
                {t('public.news.annonces.viewEvents')}
              </ArrowLink>
            </div>
          </article>
        </div>
      </main>

      <Footer />
    </div>
  );
};

export default AnnonceDetailPage;

import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AdminDetailLayout } from '../../../../components/admin/AdminDetailLayout';
import { Button, EmptyState, Tag } from '../../../../components/ui';
import { newsApi } from '../../../../lib/api/news';
import type { NewsArticle } from '../../../../lib/api/types';
import { formatFileSize, resolveMediaUrl } from '../../../../lib/api/media-url';
import { newsImageObjectPositionClass } from '../../../../lib/news/image-position';

const NewsViewPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [article, setArticle] = useState<NewsArticle | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadArticle = async () => {
      if (!id) return;
      try {
        setLoading(true);
        const response = await newsApi.getNewsByIdForAdmin(id);
        if (response.success && response.data) {
          setArticle(response.data);
        } else {
          setError(t('admin.news.errorLoad'));
        }
      } catch (err) {
        console.error('Error loading news:', err);
        setError(t('admin.news.errorLoad'));
      } finally {
        setLoading(false);
      }
    };
    loadArticle();
  }, [id, t]);

  const handleDelete = async () => {
    if (!id || !article) return;
    if (!window.confirm(t('admin.common.confirmDelete', { name: article.title }))) return;
    try {
      const response = await newsApi.deleteNews(id);
      if (response.success) navigate('/admin/news');
    } catch (err) {
      console.error('Error deleting news:', err);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (error || !article) {
    return (
      <EmptyState
        tone="error"
        title={error || t('admin.news.errorLoad')}
        action={
          <Button to="/admin/news" variant="secondary">
            {t('admin.common.backToList')}
          </Button>
        }
      />
    );
  }

  const showCover = Boolean(article.imageUrl);
  const coverObjectPosition = newsImageObjectPositionClass(article.imagePosition);

  return (
    <AdminDetailLayout
      title={article.title}
      backPath="/admin/news"
      status={{
        status: article.status === 'published' ? 'published' : 'draft',
        label: article.status === 'published' ? t('admin.news.statusPublished') : t('admin.news.statusDraft'),
      }}
      subtitle={[article.author, article.publishedDate ? new Date(article.publishedDate).toLocaleString() : null]
        .filter(Boolean)
        .join(' · ')}
      secondaryActions={
        (article.isPinned || article.category) && (
          <div className="flex flex-wrap items-center gap-2">
            {article.isPinned && <Tag>{t('admin.news.pinned')}</Tag>}
            {article.category && <Tag>{article.category}</Tag>}
          </div>
        )
      }
      actions={
        <>
          <Button to={`/admin/news/${article.id}/edit`} variant="secondary">
            <i className="ri-edit-line" aria-hidden="true" />
            {t('admin.common.edit')}
          </Button>
          <Button variant="destructive" onClick={handleDelete}>
            <i className="ri-delete-bin-line" aria-hidden="true" />
            {t('admin.common.delete')}
          </Button>
        </>
      }
      main={
        <>
          {article.excerpt && (
            <div className="flex items-start gap-3 rounded-xl border border-gold/25 bg-gold/[0.055] p-4 text-ink-variant">
              <i className="ri-double-quotes-l mt-0.5 shrink-0 text-xl text-gold-ink" aria-hidden="true" />
              <p className="text-[15px] italic leading-6">{article.excerpt}</p>
            </div>
          )}
          <p className="max-w-[78ch] whitespace-pre-wrap text-[16px] leading-7 text-ink">{article.content}</p>

          {(article.attachments?.length ?? 0) > 0 && (
            <div>
              <h2 className="flex items-center gap-2 font-display text-headline-sm text-green-deep">
                <i className="ri-attachment-2 text-base text-gold-ink" aria-hidden="true" />
                {t('admin.news.attachments')}
              </h2>
              <ul className="mt-4 grid gap-2 sm:grid-cols-2">
                {article.attachments?.map((attachment) => (
                  <li key={attachment.id}>
                    <a
                      href={resolveMediaUrl(attachment.url)}
                      target="_blank"
                      rel="noreferrer"
                      className="flex min-h-[48px] items-center gap-2 rounded-xl border border-line/55 bg-surface-container/45 px-4 text-sm text-red-link transition-colors hover:border-gold/60 hover:text-green"
                    >
                      <i className="ri-attachment-2" aria-hidden="true"></i>
                      {attachment.fileName}
                      <span className="text-body-md text-ink-variant">({formatFileSize(attachment.sizeBytes)})</span>
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </>
      }
      aside={
        showCover ? (
          <img
            src={resolveMediaUrl(article.imageUrl)}
            alt={article.title}
            className={`h-72 w-full border border-line object-cover ${coverObjectPosition}`}
          />
        ) : undefined
      }
    />
  );
};

export default NewsViewPage;

import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { newsApi } from '../../../../../lib/api/news';
import type { CreateNewsRequest, NewsAttachment } from '../../../../../lib/api/types';
import { NewsForm } from '../../NewsForm';

const NewsEditPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [coverFile, setCoverFile] = useState<File | null>(null);
  const [pendingAttachments, setPendingAttachments] = useState<File[]>([]);
  const [existingAttachments, setExistingAttachments] = useState<NewsAttachment[]>([]);
  const [formData, setFormData] = useState<CreateNewsRequest>({
    title: '',
    titleEn: '',
    content: '',
    contentEn: '',
    excerpt: '',
    excerptEn: '',
    imageUrl: '',
    imagePosition: 'center',
    author: '',
    category: '',
    publishedDate: '',
    isPinned: false,
    status: 'published',
  });

  useEffect(() => {
    const loadArticle = async () => {
      if (!id) return;
      try {
        setLoading(true);
        const response = await newsApi.getNewsByIdForAdmin(id);
        if (response.success && response.data) {
          const article = response.data;
          setFormData({
            title: article.title,
            titleEn: article.titleEn || '',
            content: article.content,
            contentEn: article.contentEn || '',
            excerpt: article.excerpt || '',
            excerptEn: article.excerptEn || '',
            imageUrl: article.imageUrl || '',
            imagePosition: article.imagePosition || 'center',
            author: article.author || '',
            category: article.category || '',
            publishedDate: article.publishedDate || '',
            isPinned: article.isPinned,
            status: article.status,
          });
          setExistingAttachments(article.attachments ?? []);
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

  const handleDeleteAttachment = async (attachmentId: string) => {
    if (!id) return;
    const response = await newsApi.deleteAttachment(id, attachmentId);
    if (response.success) {
      setExistingAttachments((prev) => prev.filter((item) => item.id !== attachmentId));
    } else {
      setError(response.message || t('admin.news.errorUpload'));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;
    setSubmitting(true);
    setError(null);
    try {
      let imageUrl = formData.imageUrl || undefined;

      if (coverFile) {
        const coverResponse = await newsApi.uploadCover(id, coverFile);
        if (!coverResponse.success || !coverResponse.data) {
          setError(coverResponse.message || t('admin.news.errorUpload'));
          return;
        }
        imageUrl = coverResponse.data.url;
      }

      const response = await newsApi.updateNews(id, {
        ...formData,
        imageUrl,
      });

      if (!response.success) {
        setError(response.message || t('admin.news.errorUpdate'));
        return;
      }

      for (const file of pendingAttachments) {
        const attachmentResponse = await newsApi.uploadAttachment(id, file);
        if (!attachmentResponse.success) {
          setError(attachmentResponse.message || t('admin.news.errorUpload'));
          return;
        }
      }

      navigate(`/admin/news/${id}`);
    } catch (err) {
      console.error('Error updating news:', err);
      setError(t('admin.news.errorUpdate'));
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  return (
    <div className="min-w-0">
      {error && <div className="mb-6 border border-error bg-surface px-4 py-3 text-error">{error}</div>}
      <NewsForm
        formData={formData}
        onChange={setFormData}
        onSubmit={handleSubmit}
        submitting={submitting}
        submitLabel={t('admin.common.save')}
        onCancel={() => navigate(`/admin/news/${id}`)}
        coverFile={coverFile}
        onCoverFileChange={setCoverFile}
        pendingAttachments={pendingAttachments}
        onPendingAttachmentsChange={setPendingAttachments}
        existingAttachments={existingAttachments}
        onDeleteAttachment={handleDeleteAttachment}
      />
    </div>
  );
};

export default NewsEditPage;

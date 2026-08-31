import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { newsApi } from '../../../../lib/api/news';
import type { CreateNewsRequest } from '../../../../lib/api/types';
import { NewsForm } from '../NewsForm';

const NewsCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [coverFile, setCoverFile] = useState<File | null>(null);
  const [pendingAttachments, setPendingAttachments] = useState<File[]>([]);
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
    publishedDate: new Date().toISOString(),
    isPinned: false,
    status: 'published',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      let imageUrl = formData.imageUrl || undefined;

      if (coverFile) {
        const mediaResponse = await newsApi.uploadMedia(coverFile);
        if (!mediaResponse.success || !mediaResponse.data) {
          setError(mediaResponse.message || t('admin.news.errorUpload'));
          return;
        }
        imageUrl = mediaResponse.data.url;
      }

      const response = await newsApi.createNews({
        ...formData,
        imageUrl,
      });

      if (!response.success || !response.data) {
        setError(response.message || t('admin.news.errorCreate'));
        return;
      }

      const newsId = response.data.id;
      for (const file of pendingAttachments) {
        const attachmentResponse = await newsApi.uploadAttachment(newsId, file);
        if (!attachmentResponse.success) {
          setError(attachmentResponse.message || t('admin.news.errorUpload'));
          navigate(`/admin/news/${newsId}/edit`);
          return;
        }
      }

      navigate(`/admin/news/${newsId}`);
    } catch (err) {
      console.error('Error creating news:', err);
      setError(t('admin.news.errorCreate'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="min-w-0">
      {error && <div className="mb-6 border border-error bg-surface px-4 py-3 text-error">{error}</div>}
      <NewsForm
        formData={formData}
        onChange={setFormData}
        onSubmit={handleSubmit}
        submitting={submitting}
        submitLabel={t('admin.common.create')}
        onCancel={() => navigate('/admin/news')}
        coverFile={coverFile}
        onCoverFileChange={setCoverFile}
        pendingAttachments={pendingAttachments}
        onPendingAttachmentsChange={setPendingAttachments}
      />
    </div>
  );
};

export default NewsCreatePage;

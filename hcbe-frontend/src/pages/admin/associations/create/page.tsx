import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { associationsApi } from '../../../../lib/api/associations';
import { AssociationForm, type AssociationFormValues } from '../AssociationForm';

export const CreateAssociationPage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [formData, setFormData] = useState<AssociationFormValues>({
    name: '',
    nameEn: '',
    description: '',
    descriptionEn: '',
    province: '',
    city: '',
    contact: '',
    phone: '',
    president: '',
    memberCount: '',
    foundedYear: undefined,
    imageUrl: '',
    website: '',
    domains: [],
    domainsEn: [],
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.name.trim() || !formData.province || !formData.city.trim()) {
      setError(t('admin.associations.errorRequired'));
      return;
    }

    if (formData.domains.length === 0) {
      setError(t('admin.associations.errorDomains'));
      return;
    }

    try {
      setIsSubmitting(true);
      setError('');

      let imageUrl = formData.imageUrl || undefined;
      if (imageFile) {
        const mediaResponse = await associationsApi.uploadMedia(imageFile);
        if (!mediaResponse.success || !mediaResponse.data) {
          setError(mediaResponse.message || t('admin.associations.errorUpload'));
          return;
        }
        imageUrl = mediaResponse.data.url;
      }

      const response = await associationsApi.createAssociation({
        ...formData,
        imageUrl,
      });
      if (response.success) {
        navigate('/admin/associations');
      } else {
        setError(response.errors?.join(', ') || response.message || t('admin.associations.errorCreate'));
      }
    } catch (err) {
      console.error('Error creating association:', err);
      setError(t('admin.associations.errorCreate'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-w-0">
      {error && (
        <div className="mb-6 border border-error bg-surface px-4 py-3 text-error">{error}</div>
      )}

      <AssociationForm
        formData={formData}
        onChange={setFormData}
        onSubmit={handleSubmit}
        submitting={isSubmitting}
        submitLabel={t('admin.associations.create')}
        submittingLabel={t('admin.associations.creating')}
        onCancel={() => navigate('/admin/associations')}
        imageFile={imageFile}
        onImageFileChange={setImageFile}
      />
    </div>
  );
};

import React, { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { associationsApi } from '../../../../lib/api/associations';
import type { Association } from '../../../../lib/api/types';
import { AssociationForm, type AssociationFormValues } from '../AssociationForm';

export const EditAssociationPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [association, setAssociation] = useState<Association | null>(null);
  const [isLoading, setIsLoading] = useState(true);
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
    isActive: true,
  });

  useEffect(() => {
    if (!id) return;

    const loadAssociation = async () => {
      try {
        setIsLoading(true);
        setError('');
        const response = await associationsApi.getAssociationForAdmin(id);
        if (response.success && response.data) {
          const assoc = response.data;
          setAssociation(assoc);
          setFormData({
            name: assoc.name,
            nameEn: assoc.nameEn || '',
            description: assoc.description || '',
            descriptionEn: assoc.descriptionEn || '',
            province: assoc.province,
            city: assoc.city,
            contact: assoc.contact || '',
            phone: assoc.phone || '',
            president: assoc.president || '',
            memberCount: assoc.memberCount || '',
            foundedYear: assoc.foundedYear,
            imageUrl: assoc.imageUrl || '',
            website: assoc.website || '',
            domains: [...assoc.domains],
            domainsEn: [...(assoc.domainsEn || [])],
            isActive: assoc.isActive,
          });
        } else {
          setError(t('admin.associations.errorNotFound'));
        }
      } catch (err) {
        console.error('Error loading association:', err);
        setError(t('admin.associations.errorLoad'));
      } finally {
        setIsLoading(false);
      }
    };

    loadAssociation();
  }, [id, t]);

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

    if (!id) return;

    try {
      setIsSubmitting(true);
      setError('');

      let imageUrl = formData.imageUrl || undefined;
      if (imageFile) {
        const imageResponse = await associationsApi.uploadImage(id, imageFile);
        if (!imageResponse.success || !imageResponse.data) {
          setError(imageResponse.message || t('admin.associations.errorUpload'));
          return;
        }
        imageUrl = imageResponse.data.url;
      }

      const response = await associationsApi.updateAssociation(id, {
        ...formData,
        imageUrl,
      });
      if (response.success) {
        navigate(`/admin/associations/${id}`);
      } else {
        setError(response.errors?.join(', ') || response.message || t('admin.associations.errorUpdate'));
      }
    } catch (err) {
      console.error('Error updating association:', err);
      setError(t('admin.associations.errorUpdate'));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  if (error && !association) {
    return (
      <div className="min-w-0">
        <div className="border border-error bg-surface px-6 py-12 text-center">
          <p className="text-body-md text-error">{error}</p>
          <div className="mt-4">
            <Link to="/admin/associations" className="text-body-md text-green hover:underline">
              {t('admin.common.backToList')}
            </Link>
          </div>
        </div>
      </div>
    );
  }

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
        submitLabel={t('admin.common.save')}
        submittingLabel={t('admin.associations.saving')}
        onCancel={() => navigate(`/admin/associations/${id}`)}
        showActiveToggle
        imageFile={imageFile}
        onImageFileChange={setImageFile}
      />
    </div>
  );
};

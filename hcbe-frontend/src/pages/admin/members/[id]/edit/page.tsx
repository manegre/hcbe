import React, { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AdminFormLayout } from '../../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../../../components/ui';
import { membersApi } from '../../../../../lib/api/members';
import type { UpdateMemberRequest } from '../../../../../lib/api/types';

const provinces = [
  'Alberta',
  'Colombie-Britannique',
  'Manitoba',
  'Nouveau-Brunswick',
  'Terre-Neuve-et-Labrador',
  'Nouvelle-Écosse',
  'Ontario',
  'Île-du-Prince-Édouard',
  'Québec',
  'Saskatchewan',
];

const MemberEditPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<UpdateMemberRequest>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    city: '',
    province: '',
    profession: '',
    expertise: '',
    interests: '',
    availability: '',
    zone: '',
  });

  const backPath = `/admin/members/${id}`;

  useEffect(() => {
    const loadMember = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const response = await membersApi.getMemberById(id);
        if (response.success && response.data) {
          const member = response.data;
          setFormData({
            firstName: member.firstName,
            lastName: member.lastName,
            email: member.email,
            phone: member.phone || '',
            city: member.city || '',
            province: member.province || '',
            profession: member.profession || '',
            expertise: member.expertise || '',
            interests: member.interests || '',
            availability: member.availability || '',
            zone: member.zone || '',
          });
        } else {
          setError(t('admin.members.errorLoad'));
        }
      } catch (err) {
        console.error('Error loading member:', err);
        setError(t('admin.members.errorLoad'));
      } finally {
        setLoading(false);
      }
    };

    loadMember();
  }, [id, t]);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;

    setSubmitting(true);
    setError(null);

    try {
      const response = await membersApi.updateMember(id, formData);
      if (response.success) {
        navigate(`/admin/members/${id}`);
      } else {
        setError(response.message || t('admin.members.errorUpdate'));
      }
    } catch (err) {
      console.error('Error updating member:', err);
      setError(t('admin.members.errorUpdate'));
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
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title={t('admin.members.editTitle')}
        backPath={backPath}
        backLabel={t('admin.common.back')}
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? t('admin.common.loading') : t('admin.common.save')}
          </Button>
        }
        main={
          <div>
            {error && (
              <p className="mb-6 border border-error bg-surface px-4 py-3 text-error">{error}</p>
            )}
            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
              <Field label={t('admin.members.firstName')} htmlFor="firstName" required>
                <input
                  type="text"
                  id="firstName"
                  name="firstName"
                  value={formData.firstName}
                  onChange={handleChange}
                  required
                  className={inputClasses}
                />
              </Field>
              <Field label={t('admin.members.lastName')} htmlFor="lastName" required>
                <input
                  type="text"
                  id="lastName"
                  name="lastName"
                  value={formData.lastName}
                  onChange={handleChange}
                  required
                  className={inputClasses}
                />
              </Field>
              <Field label={t('admin.common.email')} htmlFor="email" required>
                <input
                  type="email"
                  id="email"
                  name="email"
                  value={formData.email}
                  onChange={handleChange}
                  required
                  className={inputClasses}
                />
              </Field>
              <Field label={t('admin.members.phone')} htmlFor="phone">
                <input
                  type="tel"
                  id="phone"
                  name="phone"
                  value={formData.phone}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <Field label={t('admin.members.city')} htmlFor="city">
                <input
                  type="text"
                  id="city"
                  name="city"
                  value={formData.city}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <Field label={t('admin.members.province')} htmlFor="province">
                <select
                  id="province"
                  name="province"
                  value={formData.province}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="">{t('admin.members.selectProvince')}</option>
                  {provinces.map((prov) => (
                    <option key={prov} value={prov}>
                      {prov}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label={t('admin.members.profession')} htmlFor="profession">
                <input
                  type="text"
                  id="profession"
                  name="profession"
                  value={formData.profession}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <Field label={t('admin.members.expertise')} htmlFor="expertise">
                <input
                  type="text"
                  id="expertise"
                  name="expertise"
                  value={formData.expertise}
                  onChange={handleChange}
                  className={inputClasses}
                />
              </Field>
              <Field label={t('admin.common.zone')} htmlFor="zone">
                <select
                  id="zone"
                  name="zone"
                  value={formData.zone}
                  onChange={handleChange}
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="">{t('admin.members.selectZone')}</option>
                  <option value="Zone 1">Zone 1</option>
                  <option value="Zone 2">Zone 2</option>
                </select>
              </Field>
              <div className="md:col-span-2">
                <Field label={t('admin.members.interests')} htmlFor="interests">
                  <textarea
                    id="interests"
                    name="interests"
                    value={formData.interests}
                    onChange={handleChange}
                    rows={3}
                    className={inputClasses}
                  />
                </Field>
              </div>
            </div>
          </div>
        }
      />
    </form>
  );
};

export default MemberEditPage;

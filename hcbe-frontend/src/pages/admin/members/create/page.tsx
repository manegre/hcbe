import React, { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AdminFormLayout } from '../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../../components/ui';
import { membersApi } from '../../../../lib/api/members';
import type { CreateMemberRequest } from '../../../../lib/api/types';

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

const MemberCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<CreateMemberRequest>({
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

  const backPath = '/admin/members';

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      const response = await membersApi.createMember(formData);
      if (response.success && response.data) {
        navigate(`/admin/members/${response.data.id}`);
      } else {
        setError(response.message || t('admin.members.errorCreate'));
      }
    } catch (err) {
      console.error('Error creating member:', err);
      setError(t('admin.members.errorCreate'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title={t('admin.members.createTitle')}
        backPath={backPath}
        backLabel={t('admin.common.backToList')}
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? t('admin.common.loading') : t('admin.common.create')}
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

export default MemberCreatePage;

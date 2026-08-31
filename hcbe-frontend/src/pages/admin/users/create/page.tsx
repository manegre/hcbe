import React, { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AdminFormLayout } from '../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../../components/ui';
import { usersApi } from '../../../../lib/api/users';
import type { CreateAdminUserRequest } from '../../../../lib/api/types';

const AdminUserCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<CreateAdminUserRequest>({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
  });

  const backPath = '/admin/users';

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      const response = await usersApi.createAdminUser(formData);
      if (response.success) {
        navigate(backPath);
      } else {
        setError(response.message || t('admin.users.errorCreate'));
      }
    } catch (err) {
      console.error('Error creating admin user:', err);
      setError(t('admin.users.errorCreate'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title={t('admin.users.createTitle')}
        backPath={backPath}
        backLabel={t('admin.common.backToList')}
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? t('admin.common.loading') : t('admin.users.create')}
          </Button>
        }
        main={
          <div>
            {error && (
              <p className="mb-6 border border-error bg-surface px-4 py-3 text-error">{error}</p>
            )}
            <p className="mb-6 text-body-md text-ink-variant">{t('admin.users.createHint')}</p>
            <div className="space-y-6">
              <Field label={t('admin.common.email')} htmlFor="email" required>
                <input
                  type="email"
                  id="email"
                  name="email"
                  value={formData.email}
                  onChange={handleChange}
                  required
                  autoComplete="off"
                  className={inputClasses}
                />
              </Field>
              <Field label={t('admin.common.password')} htmlFor="password" required hint={t('admin.users.passwordHint')}>
                <input
                  type="password"
                  id="password"
                  name="password"
                  value={formData.password}
                  onChange={handleChange}
                  required
                  minLength={6}
                  autoComplete="new-password"
                  className={inputClasses}
                />
              </Field>
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <Field label={t('admin.users.firstName')} htmlFor="firstName">
                  <input
                    type="text"
                    id="firstName"
                    name="firstName"
                    value={formData.firstName}
                    onChange={handleChange}
                    className={inputClasses}
                  />
                </Field>
                <Field label={t('admin.users.lastName')} htmlFor="lastName">
                  <input
                    type="text"
                    id="lastName"
                    name="lastName"
                    value={formData.lastName}
                    onChange={handleChange}
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

export default AdminUserCreatePage;

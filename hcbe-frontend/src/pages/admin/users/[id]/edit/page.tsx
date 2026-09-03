import React, { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AdminFormLayout } from '../../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../../../components/ui';
import { usersApi } from '../../../../../lib/api/users';
import type { UpdateAdminUserRequest } from '../../../../../lib/api/types';
import { AdminRoleFields } from '../../../../../components/admin/AdminRoleFields';

const AdminUserEditPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [email, setEmail] = useState('');
  const [formData, setFormData] = useState<UpdateAdminUserRequest>({
    firstName: '',
    lastName: '',
    password: '',
    adminRole: 'community-manager',
    permissions: [],
  });

  const backPath = '/admin/users';

  useEffect(() => {
    const loadUser = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const response = await usersApi.getAdminUser(id);
        if (response.success && response.data) {
          setEmail(response.data.email);
          setFormData({
            firstName: response.data.firstName || '',
            lastName: response.data.lastName || '',
            password: '',
            adminRole: response.data.adminRole,
            permissions: response.data.permissions,
          });
        } else {
          setError(response.message || t('admin.users.errorLoad'));
        }
      } catch (err) {
        console.error('Error loading admin user:', err);
        setError(err instanceof Error ? err.message : t('admin.users.errorLoad'));
      } finally {
        setLoading(false);
      }
    };

    loadUser();
  }, [id, t]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;

    setSubmitting(true);
    setError(null);

    const payload: UpdateAdminUserRequest = {
      firstName: formData.firstName,
      lastName: formData.lastName,
      adminRole: formData.adminRole,
      permissions: formData.permissions,
    };
    if (formData.password?.trim()) {
      payload.password = formData.password;
    }

    try {
      const response = await usersApi.updateAdminUser(id, payload);
      if (response.success) {
        navigate(backPath);
      } else {
        setError(response.message || t('admin.users.errorUpdate'));
      }
    } catch (err) {
      console.error('Error updating admin user:', err);
      setError(t('admin.users.errorUpdate'));
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
        title={t('admin.users.editTitle')}
        backPath={backPath}
        backLabel={t('admin.common.backToList')}
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
            <p className="mb-6 text-body-md text-ink-variant">{email}</p>
            <div className="space-y-6">
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
              <Field label={t('admin.users.newPassword')} htmlFor="password">
                <input
                  type="password"
                  id="password"
                  name="password"
                  value={formData.password}
                  onChange={handleChange}
                  minLength={6}
                  autoComplete="new-password"
                  placeholder={t('admin.users.newPasswordHint')}
                  className={inputClasses}
                />
              </Field>
              <AdminRoleFields
                role={formData.adminRole ?? 'community-manager'}
                permissions={formData.permissions ?? []}
                onChange={(adminRole, permissions) => setFormData((previous) => ({ ...previous, adminRole, permissions }))}
              />
            </div>
          </div>
        }
      />
    </form>
  );
};

export default AdminUserEditPage;

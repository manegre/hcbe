import React, { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AdminFormLayout } from '../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../../components/ui';
import { usersApi } from '../../../../lib/api/users';
import type { CreateAdminUserRequest } from '../../../../lib/api/types';
import { AdminRoleFields } from '../../../../components/admin/AdminRoleFields';

const AdminUserCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const formRef = useRef<HTMLFormElement>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [generatingPassword, setGeneratingPassword] = useState(true);
  const [showPassword, setShowPassword] = useState(true);
  const [copied, setCopied] = useState(false);
  const [formData, setFormData] = useState<CreateAdminUserRequest>({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    adminRole: 'community-manager',
    permissions: [],
  });

  const backPath = '/admin/users';

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = event.target;
    setFormData((previous) => ({ ...previous, [name]: value }));
    if (name === 'password') setCopied(false);
  };

  const regeneratePassword = async () => {
    setGeneratingPassword(true);
    try {
      const response = await usersApi.generateTemporaryPassword();
      if (response.success && response.data) {
        setFormData((previous) => ({ ...previous, password: response.data! }));
        setShowPassword(true);
        setCopied(false);
      } else {
        setError(response.message || t('admin.users.passwordGenerationError'));
      }
    } catch {
      setError(t('admin.users.passwordGenerationError'));
    } finally {
      setGeneratingPassword(false);
    }
  };

  useEffect(() => { void regeneratePassword(); }, []);

  const copyPassword = async () => {
    await navigator.clipboard.writeText(formData.password);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1800);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      const response = await usersApi.createAdminUser(formData);
      if (response.success) {
        navigate(backPath, { replace: true, state: { invitedEmail: formData.email } });
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
          <Button type="submit" variant="primary" disabled={submitting || generatingPassword || !formData.password}>
            <i className={submitting ? 'ri-loader-4-line animate-spin' : 'ri-mail-send-line'} aria-hidden="true" />
            {submitting ? t('admin.common.loading') : t('admin.users.sendInvitation')}
          </Button>
        }
        main={
          <div className="space-y-7">
            {error && (
              <div role="alert" className="flex items-start gap-3 rounded-xl border border-error/25 bg-error/5 px-4 py-3 text-sm text-error">
                <i className="ri-error-warning-line mt-0.5 text-lg" aria-hidden="true" /><span>{error}</span>
              </div>
            )}

            <div className="relative overflow-hidden rounded-2xl bg-green-deep p-6 text-white sm:p-7">
              <div className="pointer-events-none absolute -right-12 -top-14 h-40 w-40 rounded-full border-[28px] border-gold/[.08]" aria-hidden="true" />
              <div className="relative flex items-start gap-4">
                <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gold text-xl text-green-deep"><i className="ri-user-received-2-line" /></span>
                <div><p className="text-[9px] font-bold uppercase tracking-[.18em] text-gold">{t('admin.users.invitationEyebrow')}</p><h2 className="mt-1 font-display text-2xl font-bold text-white">{t('admin.users.invitationTitle')}</h2><p className="mt-2 max-w-2xl text-sm leading-6 text-white/65">{t('admin.users.invitationDescription')}</p></div>
              </div>
            </div>

            <section className="rounded-2xl border border-line bg-surface p-5 sm:p-7">
              <div className="mb-6 flex items-center gap-3 border-b border-line pb-5">
                <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-green/10 text-green"><i className="ri-id-card-line" /></span>
                <div><p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">01</p><h3 className="font-display text-xl font-bold text-green-deep">{t('admin.users.identitySection')}</h3></div>
              </div>
              <div className="grid gap-5 md:grid-cols-2">
                <Field label={t('admin.users.firstName')} htmlFor="firstName" required>
                  <input type="text" id="firstName" name="firstName" value={formData.firstName} onChange={handleChange} required autoComplete="given-name" className={inputClasses} />
                </Field>
                <Field label={t('admin.users.lastName')} htmlFor="lastName" required>
                  <input type="text" id="lastName" name="lastName" value={formData.lastName} onChange={handleChange} required autoComplete="family-name" className={inputClasses} />
                </Field>
                <div className="md:col-span-2">
                  <Field label={t('admin.common.email')} htmlFor="email" required hint={t('admin.users.emailHint')}>
                    <input type="email" id="email" name="email" value={formData.email} onChange={handleChange} required autoComplete="off" className={inputClasses} />
                  </Field>
                </div>
              </div>
            </section>

            <section className="rounded-2xl border border-gold/35 bg-gold/[.055] p-5 sm:p-7">
              <div className="mb-6 flex flex-col gap-4 border-b border-gold/25 pb-5 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex items-center gap-3"><span className="flex h-9 w-9 items-center justify-center rounded-xl bg-gold text-green-deep"><i className="ri-key-2-line" /></span><div><p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">02</p><h3 className="font-display text-xl font-bold text-green-deep">{t('admin.users.securitySection')}</h3></div></div>
                <span className="inline-flex w-fit items-center gap-2 rounded-full border border-green/15 bg-surface px-3 py-1.5 text-[9px] font-bold uppercase tracking-[.11em] text-green"><i className="ri-refresh-line" />{t('admin.users.forcedReset')}</span>
              </div>

              <Field label={t('admin.users.temporaryPassword')} htmlFor="password" required hint={t('admin.users.passwordHint')}>
                <div className="relative">
                  <input type={showPassword ? 'text' : 'password'} id="password" name="password" value={formData.password} onChange={handleChange} required minLength={12} autoComplete="new-password" className={`${inputClasses} pr-12 font-mono tracking-wide`} />
                  <button type="button" onClick={() => setShowPassword((visible) => !visible)} aria-label={t(showPassword ? 'admin.login.hidePassword' : 'admin.login.showPassword')} className="absolute right-2 top-1/2 flex h-9 w-9 -translate-y-1/2 items-center justify-center rounded-lg text-ink-variant hover:bg-green/8 hover:text-green"><i className={showPassword ? 'ri-eye-off-line' : 'ri-eye-line'} /></button>
                </div>
              </Field>

              <div className="mt-4 grid grid-cols-2 gap-3 sm:flex">
                <button type="button" disabled={generatingPassword} onClick={() => void regeneratePassword()} className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl border border-green/20 bg-surface px-4 text-[10px] font-bold uppercase tracking-[.1em] text-green transition-colors hover:border-green hover:bg-green hover:text-white disabled:cursor-wait disabled:opacity-50"><i className={generatingPassword ? 'ri-loader-4-line animate-spin' : 'ri-sparkling-2-line'} />{t('admin.users.generatePassword')}</button>
                <button type="button" disabled={!formData.password} onClick={() => void copyPassword()} className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl border border-green/20 bg-surface px-4 text-[10px] font-bold uppercase tracking-[.1em] text-green transition-colors hover:border-green hover:bg-green hover:text-white disabled:opacity-50"><i className={copied ? 'ri-check-line' : 'ri-file-copy-line'} />{t(copied ? 'admin.users.copied' : 'admin.users.copyPassword')}</button>
              </div>
            </section>

            <AdminRoleFields
              role={formData.adminRole ?? 'community-manager'}
              permissions={formData.permissions ?? []}
              onChange={(adminRole, permissions) => setFormData((previous) => ({ ...previous, adminRole, permissions }))}
            />

            <div className="grid gap-3 sm:grid-cols-3">
              {[
                ['ri-mail-check-line', t('admin.users.benefitEmail')],
                ['ri-shield-keyhole-line', t('admin.users.benefitReset')],
                ['ri-community-line', t('admin.users.benefitMember')],
              ].map(([icon, label]) => <div key={label} className="flex items-center gap-3 rounded-xl border border-line bg-surface-container/60 p-4"><i className={`${icon} text-lg text-green`} /><span className="text-xs font-medium leading-5 text-ink-variant">{label}</span></div>)}
            </div>
          </div>
        }
      />
    </form>
  );
};

export default AdminUserCreatePage;

import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { HcbeLogoMark } from '../../../components/brand/HcbeLogo';
import { LanguageSwitcher } from '../../../components/admin/LanguageSwitcher';
import ThemeToggle from '../../../components/feature/ThemeToggle';
import { Button, Field } from '../../../components/ui';
import { useAuth } from '../../../contexts/AuthContext';

const passwordInputClasses =
  'min-h-[54px] w-full rounded-xl border border-green/20 bg-surface-container px-4 pr-12 text-base text-ink transition-all placeholder:text-ink-variant/45 hover:border-green/40 focus:border-green focus:bg-surface focus:outline-none focus:ring-4 focus:ring-green/10';

const RequiredPasswordChangePage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { user, completeRequiredPasswordChange, logout } = useAuth();
  const [password, setPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (user && !user.mustChangePassword) {
      navigate(user.isAdmin ? '/admin/dashboard' : '/espace-membre', { replace: true });
    }
  }, [navigate, user]);

  const checks = useMemo(() => [
    { label: t('admin.passwordChange.ruleLength'), valid: password.length >= 12 },
    { label: t('admin.passwordChange.ruleCase'), valid: /[a-z]/.test(password) && /[A-Z]/.test(password) },
    { label: t('admin.passwordChange.ruleNumber'), valid: /\d/.test(password) },
    { label: t('admin.passwordChange.ruleSymbol'), valid: /[^A-Za-z0-9]/.test(password) },
  ], [password, t]);
  const passwordIsStrong = checks.every((check) => check.valid);
  const passwordsMatch = confirmation.length > 0 && password === confirmation;

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!passwordIsStrong || !passwordsMatch) return;
    setSubmitting(true);
    setError('');
    const result = await completeRequiredPasswordChange(password);
    if (result.success) navigate('/admin/dashboard', { replace: true });
    else setError(result.message || t('admin.passwordChange.error'));
    setSubmitting(false);
  };

  return (
    <main className="min-h-screen bg-canvas lg:grid lg:grid-cols-[minmax(360px,.78fr)_minmax(520px,1.22fr)]">
      <aside className="public-grid-pattern relative flex min-h-[360px] flex-col overflow-hidden bg-green-deep px-6 py-8 text-white sm:px-10 lg:min-h-screen lg:px-14 lg:py-12">
        <div className="pointer-events-none absolute -bottom-36 -left-32 h-[430px] w-[430px] rounded-full border-[70px] border-gold/[.055]" aria-hidden="true" />
        <div className="pointer-events-none absolute -right-20 top-32 h-64 w-64 rounded-full border-[44px] border-white/[.035]" aria-hidden="true" />
        <div className="relative flex items-center justify-between gap-4">
          <HcbeLogoMark size="md" tone="dark" />
          <div className="flex items-center gap-2"><ThemeToggle variant="onDark" /><LanguageSwitcher variant="onDark" /></div>
        </div>

        <div className="relative my-auto max-w-xl py-12">
          <p className="text-[10px] font-bold uppercase tracking-[.22em] text-gold">{t('admin.passwordChange.eyebrow')}</p>
          <h1 className="mt-5 font-display text-4xl font-bold leading-[1.04] text-white sm:text-5xl">{t('admin.passwordChange.title')}</h1>
          <p className="mt-5 border-l-2 border-gold pl-5 text-sm leading-7 text-white/65">{t('admin.passwordChange.description')}</p>

          <div className="mt-9 grid gap-px overflow-hidden rounded-2xl border border-white/10 bg-white/10 sm:grid-cols-3 lg:grid-cols-1 xl:grid-cols-3">
            {[
              ['01', 'ri-key-2-line', t('admin.passwordChange.stepTemporary')],
              ['02', 'ri-shield-check-line', t('admin.passwordChange.stepSecure')],
              ['03', 'ri-community-line', t('admin.passwordChange.stepAccess')],
            ].map(([number, icon, label]) => (
              <div key={number} className="bg-green-deep/85 p-4">
                <div className="flex items-center justify-between"><i className={`${icon} text-lg text-gold`} /><span className="text-[9px] font-bold text-white/30">{number}</span></div>
                <p className="mt-4 text-[10px] font-bold uppercase tracking-[.12em] text-white/75">{label}</p>
              </div>
            ))}
          </div>
        </div>

        <button type="button" onClick={logout} className="relative inline-flex min-h-11 w-fit items-center gap-2 text-[10px] font-bold uppercase tracking-[.12em] text-white/50 hover:text-gold">
          <i className="ri-logout-box-r-line" />{t('admin.passwordChange.signOut')}
        </button>
      </aside>

      <section className="relative flex items-center justify-center px-5 py-12 sm:px-10 lg:py-16">
        <div className="pointer-events-none absolute right-0 top-0 h-56 w-56 rounded-bl-full bg-gold/[.055]" aria-hidden="true" />
        <form onSubmit={handleSubmit} className="relative w-full max-w-[590px] overflow-hidden rounded-[26px] border border-green/12 bg-surface shadow-[0_28px_90px_rgba(0,59,27,.12)]">
          <header className="border-b border-line bg-green/[.045] px-6 py-6 sm:px-9">
            <div className="flex items-start gap-4">
              <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-green text-xl text-gold shadow-[0_10px_24px_rgba(0,59,27,.18)]"><i className="ri-lock-password-line" /></span>
              <div><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">{t('admin.passwordChange.required')}</p><h2 className="mt-1 font-display text-2xl font-bold text-green-deep">{t('admin.passwordChange.formTitle')}</h2><p className="mt-1 text-sm text-ink-variant">{user?.email}</p></div>
            </div>
          </header>

          <div className="space-y-6 px-6 py-7 sm:px-9 sm:py-9">
            {error && <div role="alert" className="flex gap-3 rounded-xl border border-error/25 bg-error/5 p-4 text-sm text-error"><i className="ri-error-warning-line mt-0.5" /><span>{error}</span></div>}
            <Field label={t('admin.passwordChange.newPassword')} htmlFor="required-new-password" required>
              <div className="relative">
                <input id="required-new-password" type={showPassword ? 'text' : 'password'} autoComplete="new-password" minLength={12} required className={passwordInputClasses} value={password} onChange={(event) => setPassword(event.target.value)} />
                <button type="button" onClick={() => setShowPassword((value) => !value)} aria-label={t(showPassword ? 'admin.login.hidePassword' : 'admin.login.showPassword')} className="absolute right-2 top-1/2 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-lg text-lg text-ink-variant hover:bg-green/8 hover:text-green"><i className={showPassword ? 'ri-eye-off-line' : 'ri-eye-line'} /></button>
              </div>
            </Field>

            <div className="grid gap-2 rounded-2xl border border-line bg-canvas/65 p-4 sm:grid-cols-2">
              {checks.map((check) => <p key={check.label} className={`flex items-center gap-2 text-xs ${check.valid ? 'text-green' : 'text-ink-variant'}`}><i className={check.valid ? 'ri-checkbox-circle-fill' : 'ri-checkbox-blank-circle-line'} /><span>{check.label}</span></p>)}
            </div>

            <Field label={t('admin.passwordChange.confirmPassword')} htmlFor="required-confirm-password" required>
              <input id="required-confirm-password" type="password" autoComplete="new-password" minLength={12} required className={passwordInputClasses} value={confirmation} onChange={(event) => setConfirmation(event.target.value)} />
            </Field>
            {confirmation && !passwordsMatch && <p className="-mt-3 text-xs font-medium text-error">{t('admin.passwordChange.mismatch')}</p>}

            <div className="border-t border-line pt-6">
              <Button type="submit" variant="primary" className="w-full py-3.5" disabled={submitting || !passwordIsStrong || !passwordsMatch}>
                {submitting ? <i className="ri-loader-4-line animate-spin" /> : <i className="ri-shield-check-line" />}
                {t(submitting ? 'admin.passwordChange.saving' : 'admin.passwordChange.submit')}
              </Button>
              <p className="mt-4 text-center text-xs leading-5 text-ink-variant">{t('admin.passwordChange.memberAccess')}</p>
            </div>
          </div>
        </form>
        <Link to="/" className="absolute bottom-5 right-6 text-[9px] font-bold uppercase tracking-[.12em] text-ink-variant hover:text-green">HCBE.CA <i className="ri-arrow-right-up-line" /></Link>
      </section>
    </main>
  );
};

export default RequiredPasswordChangePage;

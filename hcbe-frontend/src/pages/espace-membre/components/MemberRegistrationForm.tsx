import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../../../components/ui';
import { useAuth } from '../../../contexts/AuthContext';
import { membershipApplicationsApi } from '../../../lib/api/membership-applications';
import { memberProfessionalDomains, memberProvinces } from '../memberProfileOptions';

const emptyRegistration = {
  prenom: '', nom: '', email: '', telephone: '', ville: '', province: '', profession: '',
  domaineProfessionnel: '', motivationAdhesion: '', password: '', confirmPassword: '',
};

const MemberRegistrationForm = ({ onSwitchToLogin }: { onSwitchToLogin?: () => void }) => {
  const { t } = useTranslation();
  const { login } = useAuth();
  const [data, setData] = useState(emptyRegistration);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitStatus, setSubmitStatus] = useState<'idle' | 'success' | 'error'>('idle');
  const [submitMessage, setSubmitMessage] = useState('');
  const [accountExists, setAccountExists] = useState(false);

  const update = (field: keyof typeof data, value: string) => setData((current) => ({ ...current, [field]: value }));

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setAccountExists(false);
    if (data.motivationAdhesion.length > 500) {
      setSubmitStatus('error');
      setSubmitMessage(t('public.member.form.error.motivationLength'));
      return;
    }
    if (data.password.length < 8) {
      setSubmitStatus('error');
      setSubmitMessage(t('public.member.form.error.passwordLength'));
      return;
    }
    if (data.password !== data.confirmPassword) {
      setSubmitStatus('error');
      setSubmitMessage(t('public.member.form.error.passwordMismatch'));
      return;
    }
    setIsSubmitting(true);
    setSubmitStatus('idle');
    setSubmitMessage('');
    try {
      const response = await membershipApplicationsApi.submit({
        firstName: data.prenom.trim(), lastName: data.nom.trim(), email: data.email.trim(),
        phone: data.telephone.trim(), city: data.ville.trim(), province: data.province,
        profession: data.profession.trim(), expertise: data.domaineProfessionnel,
        motivation: data.motivationAdhesion.trim(), password: data.password,
      });
      if (!response.success) {
        setSubmitStatus('error');
        setSubmitMessage(response.message || t('public.member.form.error.message'));
        return;
      }
      const loginResult = await login(data.email.trim(), data.password);
      setSubmitStatus(loginResult.success ? 'success' : 'error');
      setSubmitMessage(loginResult.success ? t('public.member.form.success.message') : (loginResult.message || t('public.member.form.error.message')));
      if (loginResult.success) setData(emptyRegistration);
    } catch (error) {
      const apiMessage = error instanceof Error ? error.message : '';
      const normalized = apiMessage.toLowerCase();
      const isExistingAccount = normalized.includes('already exists') || normalized.includes('déjà');
      setAccountExists(isExistingAccount);
      setSubmitStatus('error');
      setSubmitMessage(isExistingAccount ? t('public.member.form.error.accountExists') : (apiMessage || t('public.member.form.error.message')));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mt-7 border-t border-line pt-7">
      <div className="flex items-center gap-4">
        <span className="h-px flex-1 bg-line" aria-hidden="true" />
        <span className="text-[10px] font-bold uppercase tracking-[0.16em] text-ink-variant/70">{t('public.member.gateway.orForm')}</span>
        <span className="h-px flex-1 bg-line" aria-hidden="true" />
      </div>

      <form id="inscription-membre-form" data-readdy-form onSubmit={submit} className="mt-7 space-y-7">
        <FormSection number="01" title={t('public.member.form.sections.contact')}>
          <div className="grid items-start gap-x-5 gap-y-5 sm:grid-cols-2">
            <Field label={t('public.member.form.fields.firstName')} htmlFor="prenom" required><input id="prenom" autoComplete="given-name" value={data.prenom} onChange={(e) => update('prenom', e.target.value)} required className={inputClasses} /></Field>
            <Field label={t('public.member.form.fields.lastName')} htmlFor="nom" required><input id="nom" autoComplete="family-name" value={data.nom} onChange={(e) => update('nom', e.target.value)} required className={inputClasses} /></Field>
            <Field label={t('public.member.form.fields.email')} htmlFor="member-email" required><input type="email" id="member-email" autoComplete="email" value={data.email} onChange={(e) => update('email', e.target.value)} required className={inputClasses} /></Field>
            <Field label={t('public.member.form.fields.phone')} htmlFor="telephone" required><input type="tel" id="telephone" autoComplete="tel" value={data.telephone} onChange={(e) => update('telephone', e.target.value)} required className={inputClasses} /></Field>
            <Field label={t('public.member.form.fields.city')} htmlFor="ville" required><input id="ville" autoComplete="address-level2" value={data.ville} onChange={(e) => update('ville', e.target.value)} required className={inputClasses} /></Field>
            <Field label={t('public.member.form.fields.province')} htmlFor="province" required>
              <select id="province" autoComplete="address-level1" value={data.province} onChange={(e) => update('province', e.target.value)} required className={`${inputClasses} cursor-pointer`}>
                <option value="">{t('public.member.form.select')}</option>{memberProvinces.map((province) => <option key={province} value={province}>{province}</option>)}
              </select>
            </Field>
          </div>
        </FormSection>

        <FormSection number="02" title={t('public.member.form.sections.account')}>
          <div className="grid items-start gap-x-5 gap-y-5 sm:grid-cols-2">
            <Field label={t('public.member.form.fields.password')} htmlFor="member-password" required hint={t('public.member.form.passwordHint')}><input id="member-password" type="password" minLength={8} autoComplete="new-password" value={data.password} onChange={(e) => update('password', e.target.value)} required className={inputClasses} /></Field>
            <Field label={t('public.member.form.fields.confirmPassword')} htmlFor="member-confirm-password" required><input id="member-confirm-password" type="password" minLength={8} autoComplete="new-password" value={data.confirmPassword} onChange={(e) => update('confirmPassword', e.target.value)} required className={inputClasses} /></Field>
            <div className="sm:col-span-2">
              <Field label={t('public.member.form.fields.motivation')} htmlFor="motivationAdhesion" required hint={t('public.member.form.charCount', { count: data.motivationAdhesion.length })}>
                <textarea id="motivationAdhesion" rows={4} maxLength={500} value={data.motivationAdhesion} onChange={(e) => update('motivationAdhesion', e.target.value)} required className={`${inputClasses} resize-none`} placeholder={t('public.member.form.motivation.placeholder')} />
              </Field>
            </div>
          </div>
        </FormSection>

        <FormSection number="03" title={t('public.member.form.sections.professionalOptional')} description={t('public.member.form.professionalOptionalHint')}>
          <div className="grid items-start gap-x-5 gap-y-5 sm:grid-cols-2">
            <Field label={t('public.member.form.fields.profession')} htmlFor="profession" hint={t('public.member.form.optional')}><input id="profession" value={data.profession} onChange={(e) => update('profession', e.target.value)} className={inputClasses} /></Field>
            <Field label={t('public.member.form.fields.domain')} htmlFor="domaineProfessionnel" hint={t('public.member.form.optional')}>
              <select id="domaineProfessionnel" value={data.domaineProfessionnel} onChange={(e) => update('domaineProfessionnel', e.target.value)} className={`${inputClasses} cursor-pointer`}>
                <option value="">{t('public.member.form.select')}</option>{memberProfessionalDomains.map((domain) => <option key={domain} value={domain}>{domain}</option>)}
              </select>
            </Field>
          </div>
        </FormSection>

        {submitStatus !== 'idle' && (
          <div className={`rounded-r-xl border-l-4 p-4 ${submitStatus === 'success' ? 'border-green bg-green/5 text-green' : 'border-error bg-error/5 text-error'}`} role="status">
            <div className="flex items-start gap-3">
            <i className={submitStatus === 'success' ? 'ri-checkbox-circle-fill' : 'ri-error-warning-fill'} aria-hidden="true" />
              <p className="text-sm leading-6">{submitMessage || t(submitStatus === 'success' ? 'public.member.form.success.message' : 'public.member.form.error.message')}</p>
            </div>
            {accountExists && onSwitchToLogin && <button type="button" onClick={onSwitchToLogin} className="mt-3 inline-flex items-center gap-2 text-[10px] font-bold uppercase tracking-[0.12em] text-green hover:text-red-link"><i className="ri-login-circle-line" aria-hidden="true" />{t('public.member.form.error.signInInstead')}</button>}
          </div>
        )}

        <div className="border-t border-line pt-6">
          <Button type="submit" variant="primary" disabled={isSubmitting} className="w-full sm:w-auto sm:min-w-64">
            <i className={isSubmitting ? 'ri-loader-4-line animate-spin' : 'ri-user-add-line'} aria-hidden="true" />
            {t(isSubmitting ? 'public.member.form.submit.loading' : 'public.member.form.submit.label')}
          </Button>
          <p className="mt-4 max-w-2xl text-xs leading-5 text-ink-variant">
            {t('public.member.form.consent')}{' '}
            <Link to="/confidentialite#account" className="font-semibold text-green underline decoration-gold underline-offset-4 hover:text-red-link">
              {t('public.member.form.privacyNotice')}
            </Link>
          </p>
        </div>
      </form>
    </div>
  );
};

const FormSection = ({ number, title, description, children }: { number: string; title: string; description?: string; children: ReactNode }) => (
  <fieldset>
    <legend className="mb-5 flex w-full items-center gap-3 border-b border-line pb-3">
      <span className="font-display text-lg font-bold text-red-link">{number}</span><span className="text-label-md uppercase text-ink-variant">{title}</span>
    </legend>
    {description && <p className="mb-5 text-sm leading-6 text-ink-variant">{description}</p>}
    {children}
  </fieldset>
);

export default MemberRegistrationForm;

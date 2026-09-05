import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../../../components/ui';
import { useAuth } from '../../../contexts/AuthContext';
import { memberAccountApi } from '../../../lib/api/member-account';
import type { MemberDto, UpdateMemberPreferenceRequest } from '../../../lib/api/types';
import { authApi } from '../../../lib/api/auth';
import MemberCommunityWorkspace from './MemberCommunityWorkspace';
import { GoogleSignInButton } from '../../../components/auth/GoogleSignInButton';
import { memberProfessionalDomains, memberProvinces } from '../memberProfileOptions';
import { useNavigate } from 'react-router-dom';
import { AccountSecurityPanel } from '../../../components/security/AccountSecurityPanel';

const isMemberProfileComplete = (member: MemberDto) => [
  member.firstName,
  member.lastName,
  member.phone,
  member.city,
  member.province,
  member.interests,
].every((value) => Boolean(value?.trim()));

interface MemberLoginFormProps {
  mode?: 'login' | 'signup';
  embedded?: boolean;
}

const MemberLoginForm = ({ mode = 'login', embedded = false }: MemberLoginFormProps) => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const { user, login, googleMemberLogin, verifyMfa, resendMfaCode, logout } = useAuth();
  const [loginData, setLoginData] = useState({ email: '', password: '' });
  const [member, setMember] = useState<MemberDto | null>(null);
  const [memberLoading, setMemberLoading] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [resetPassword, setResetPassword] = useState('');
  const [mfaChallenge, setMfaChallenge] = useState('');
  const [mfaCode, setMfaCode] = useState('');
  const [mfaMethod, setMfaMethod] = useState<'Authenticator' | 'Email'>('Authenticator');
  const [mfaDestination, setMfaDestination] = useState('');
  const [profileData, setProfileData] = useState({
    firstName: '', lastName: '', phone: '', city: '', province: '', profession: '',
    expertise: '', interests: '', availability: '',
  });
  const [onboardingStep, setOnboardingStep] = useState(0);
  const [onboardingPreferences, setOnboardingPreferences] = useState<UpdateMemberPreferenceRequest>({
    preferredLanguage: i18n.language.startsWith('en') ? 'en' : 'fr',
    timeZone: 'America/Toronto', emailEvents: false, emailOpportunities: false,
    emailMentorship: false, emailServiceUpdates: false, emailNewsletter: false,
    pushNotifications: false, digestFrequency: 'Off',
  });
  const resetToken = new URLSearchParams(window.location.search).get('resetToken');
  const requestedReturnTo = new URLSearchParams(window.location.search).get('returnTo');
  const safeReturnTo = requestedReturnTo?.startsWith('/') && !requestedReturnTo.startsWith('//')
    ? requestedReturnTo
    : null;

  useEffect(() => {
    if (!user?.memberId) {
      setMember(null);
      setMemberLoading(false);
      return;
    }

    setMemberLoading(true);
    setStatus(null);
    memberAccountApi.getMe()
      .then((response) => {
        if (response.success && response.data) {
          setMember(response.data);
          setProfileData({
            firstName: response.data.firstName,
            lastName: response.data.lastName,
            phone: response.data.phone || '',
            city: response.data.city || '',
            province: response.data.province || '',
            profession: response.data.profession || '',
            expertise: response.data.expertise || '',
            interests: response.data.interests || '',
            availability: response.data.availability || '',
          });
        } else {
          setStatus(response.message || t('public.member.login.error'));
        }
      })
      .catch(() => setStatus(t('public.member.login.error')))
      .finally(() => setMemberLoading(false));
  }, [user?.memberId]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setStatus(null);
    const result = await login(loginData.email.trim(), loginData.password);
    const storedUser = JSON.parse(localStorage.getItem('hcbe_user') || 'null');
    if (result.mfaRequired && result.challengeToken) {
      setMfaChallenge(result.challengeToken);
      setMfaMethod(result.mfaMethod ?? 'Authenticator');
      setMfaDestination(result.mfaDestination ?? '');
    } else if (!result.success) {
      setStatus(result.message || t('public.member.login.error'));
    } else if (storedUser?.mustChangePassword) {
      navigate('/admin/change-password');
    } else if (!storedUser?.memberId) {
      logout();
      setStatus(t('public.member.login.notMember'));
    } else if (safeReturnTo) {
      navigate(safeReturnTo);
    }
    setSubmitting(false);
  };

  const handleGoogleCredential = useCallback(async (credential: string) => {
    setSubmitting(true);
    setStatus(null);
    const result = await googleMemberLogin(credential);
    if (result.mfaRequired && result.challengeToken) {
      setMfaChallenge(result.challengeToken);
      setMfaMethod(result.mfaMethod ?? 'Authenticator');
      setMfaDestination(result.mfaDestination ?? '');
    } else if (!result.success) {
      const normalized = (result.message ?? '').toLowerCase();
      setStatus(
        normalized.includes('could not be activated') || normalized.includes('403')
          ? t('public.member.login.googleAccountBlocked')
          : normalized.includes('not configured') || normalized.includes('503')
            ? t('public.member.login.googleUnavailable')
            : t('public.member.login.googleError'),
      );
    } else {
      const authenticatedUser = JSON.parse(localStorage.getItem('hcbe_user') || 'null');
      if (authenticatedUser?.mustChangePassword) navigate('/admin/change-password');
      else if (safeReturnTo) navigate(safeReturnTo);
    }
    setSubmitting(false);
  }, [googleMemberLogin, navigate, safeReturnTo, t]);

  const handleMfaSubmit = async (event: React.FormEvent) => {
    event.preventDefault(); setSubmitting(true); setStatus(null);
    const result = await verifyMfa(mfaChallenge, mfaCode);
    if (!result.success) setStatus(t('public.member.login.mfaInvalid'));
    else if (safeReturnTo) navigate(safeReturnTo);
    setSubmitting(false);
  };

  const handleMfaResend = async () => {
    setSubmitting(true); setStatus(null);
    const result = await resendMfaCode(mfaChallenge);
    setStatus(result.success ? t('public.member.login.mfaResent') : result.message || t('public.member.login.error'));
    setSubmitting(false);
  };

  const handleGoogleUnavailable = useCallback(() => {
    setStatus(t('public.member.login.googleUnavailable'));
  }, [t]);

  const handleForgotPassword = async () => {
    if (!loginData.email.trim()) {
      setStatus(t('public.member.login.enterEmail'));
      return;
    }
    setSubmitting(true);
    const response = await authApi.requestPasswordReset(loginData.email.trim());
    setStatus(response.message || t('public.member.login.resetSent'));
    setSubmitting(false);
  };

  const handleResetPassword = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!resetToken || resetPassword.length < 8) return;
    setSubmitting(true);
    const response = await authApi.confirmPasswordReset(resetToken, resetPassword);
    setStatus(response.success ? t('public.member.login.resetSuccess') : response.message || t('public.member.login.error'));
    if (response.success) window.history.replaceState({}, '', '/espace-membre');
    setSubmitting(false);
  };

  const handleProfileSave = async (event: React.FormEvent) => {
    event.preventDefault();
    const completingOnboarding = member ? !isMemberProfileComplete(member) : false;
    setSubmitting(true);
    setStatus(null);
    const response = await memberAccountApi.updateMe(profileData);
    if (response.success && response.data) {
      setMember(response.data);
      setStatus(t(completingOnboarding ? 'public.member.onboarding.complete' : 'public.member.login.profileSaved'));
    } else {
      setStatus(response.message || t('public.member.login.error'));
    }
    setSubmitting(false);
  };

  const finishOnboarding = async (event: React.FormEvent) => {
    event.preventDefault(); setSubmitting(true); setStatus(null);
    try {
      const profileResponse = await memberAccountApi.updateMe(profileData);
      if (!profileResponse.success || !profileResponse.data) throw new Error(profileResponse.message || t('public.member.login.error'));
      const preferenceResponse = await memberAccountApi.updatePreferences(onboardingPreferences);
      if (!preferenceResponse.success) throw new Error(preferenceResponse.message || t('public.member.login.error'));
      setMember(profileResponse.data);
      setStatus(t('public.member.onboarding.complete'));
    } catch (reason) { setStatus(reason instanceof Error ? reason.message : t('public.member.login.error')); }
    finally { setSubmitting(false); }
  };

  const identityFields = (
      <div className="grid gap-5 md:grid-cols-2">
      <Field label={t('public.member.form.fields.firstName')} htmlFor="member-profile-first-name" required>
        <input id="member-profile-first-name" className={inputClasses} required autoComplete="given-name" value={profileData.firstName} onChange={(e) => setProfileData({ ...profileData, firstName: e.target.value })} />
      </Field>
      <Field label={t('public.member.form.fields.lastName')} htmlFor="member-profile-last-name" required>
        <input id="member-profile-last-name" className={inputClasses} required autoComplete="family-name" value={profileData.lastName} onChange={(e) => setProfileData({ ...profileData, lastName: e.target.value })} />
      </Field>
      <Field label={t('public.member.form.fields.phone')} htmlFor="member-profile-phone" required>
        <input id="member-profile-phone" type="tel" className={inputClasses} required autoComplete="tel" value={profileData.phone} onChange={(e) => setProfileData({ ...profileData, phone: e.target.value })} />
      </Field>
      <Field label={t('public.member.form.fields.city')} htmlFor="member-profile-city" required>
        <input id="member-profile-city" className={inputClasses} required autoComplete="address-level2" value={profileData.city} onChange={(e) => setProfileData({ ...profileData, city: e.target.value })} />
      </Field>
      <Field label={t('public.member.form.fields.province')} htmlFor="member-profile-province" required>
        <select id="member-profile-province" className={`${inputClasses} cursor-pointer`} required value={profileData.province} onChange={(e) => setProfileData({ ...profileData, province: e.target.value })}>
          <option value="">{t('public.member.form.select')}</option>
          {memberProvinces.map((province) => <option key={province} value={province}>{province}</option>)}
        </select>
      </Field>
      <div className="md:col-span-2">
        <Field label={t('public.member.form.fields.motivation')} htmlFor="member-profile-interests" required hint={t('public.member.form.charCount', { count: profileData.interests.length })}>
          <textarea id="member-profile-interests" rows={4} maxLength={500} required className={`${inputClasses} resize-none`} value={profileData.interests} onChange={(e) => setProfileData({ ...profileData, interests: e.target.value })} />
        </Field>
      </div>
      </div>
  );

  const professionalFields = (
      <fieldset className="rounded-2xl border border-line bg-canvas/45 p-5 sm:p-6">
        <legend className="px-2 text-label-md uppercase text-green">
          {t('public.member.form.sections.professionalOptional')}
        </legend>
        <p className="mb-5 text-sm leading-6 text-ink-variant">{t('public.member.form.professionalOptionalHint')}</p>
        <div className="grid gap-5 md:grid-cols-2">
          <Field label={t('public.member.form.fields.profession')} htmlFor="member-profile-profession" hint={t('public.member.form.optional')}>
            <input id="member-profile-profession" className={inputClasses} value={profileData.profession} onChange={(e) => setProfileData({ ...profileData, profession: e.target.value })} />
          </Field>
          <Field label={t('public.member.form.fields.domain')} htmlFor="member-profile-expertise" hint={t('public.member.form.optional')}>
            <select id="member-profile-expertise" className={`${inputClasses} cursor-pointer`} value={profileData.expertise} onChange={(e) => setProfileData({ ...profileData, expertise: e.target.value })}>
              <option value="">{t('public.member.form.select')}</option>
              {memberProfessionalDomains.map((domain) => <option key={domain} value={domain}>{domain}</option>)}
            </select>
          </Field>
          <Field label={t('public.member.login.availability')} htmlFor="member-onboarding-availability" hint={t('public.member.form.optional')}>
            <input id="member-onboarding-availability" className={inputClasses} value={profileData.availability} onChange={(e) => setProfileData({ ...profileData, availability: e.target.value })} />
          </Field>
        </div>
      </fieldset>
  );

  const profileFields = (
    <div className="space-y-8">
      {identityFields}
      {professionalFields}
    </div>
  );

  if (resetToken) {
    return (
      <div className="border border-line bg-surface p-6">
        <p className="text-label-md uppercase text-red-link">{t('public.member.login.resetBadge')}</p>
        <h2 className="mt-3 font-display text-headline-md text-green">{t('public.member.login.resetTitle')}</h2>
        <form className="mt-5 space-y-4" onSubmit={handleResetPassword}>
          <Field label={t('public.member.login.newPassword')} htmlFor="reset-password">
            <input id="reset-password" type="password" minLength={8} required className={inputClasses} value={resetPassword} onChange={(e) => setResetPassword(e.target.value)} />
          </Field>
          {status && <p className="border border-line p-3 text-sm text-ink-variant">{status}</p>}
          <Button type="submit" variant="secondary" className="w-full" disabled={submitting}>
            {t('public.member.login.resetSubmit')}
          </Button>
        </form>
      </div>
    );
  }

  if (user?.memberId) {
    if (memberLoading) {
      return (
        <div className="rounded-2xl border border-line bg-surface p-10 text-center shadow-[0_18px_55px_rgba(0,59,27,.08)]">
          <span className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-green/10 text-xl text-green">
            <i className="ri-loader-4-line animate-spin" aria-hidden="true" />
          </span>
          <p className="mt-4 text-sm text-ink-variant">{t('public.member.login.loading')}</p>
          {status && <p className="mt-3 text-sm text-error">{status}</p>}
        </div>
      );
    }

    if (!member) {
      return (
        <div className="rounded-2xl border border-error/25 bg-surface p-8 text-center">
          <span className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-error/10 text-xl text-error">
            <i className="ri-user-unfollow-line" aria-hidden="true" />
          </span>
          <p className="mt-4 text-sm text-ink-variant">{status || t('public.member.login.error')}</p>
          <Button type="button" variant="secondary" className="mt-5" onClick={logout}>{t('public.member.onboarding.signOut')}</Button>
        </div>
      );
    }

    if (!isMemberProfileComplete(member)) {
      return (
        <section className="overflow-hidden rounded-[24px] border border-line bg-surface shadow-[0_24px_80px_rgba(0,59,27,.12)]">
          <div className="relative overflow-hidden bg-green-deep px-6 py-8 text-white sm:px-10 sm:py-10">
            <div className="absolute -right-14 -top-16 h-48 w-48 rounded-full border-[34px] border-gold/[0.09]" aria-hidden="true" />
            <div className="relative max-w-3xl">
              <p className="text-[10px] font-bold uppercase tracking-[0.24em] text-gold">{t('public.member.onboarding.eyebrow')}</p>
              <h2 className="mt-3 font-display text-3xl font-bold sm:text-4xl">{t('public.member.onboarding.title')}</h2>
              <p className="mt-3 max-w-2xl text-sm leading-6 text-green-dim">{t('public.member.onboarding.intro')}</p>
            </div>
          </div>

          <div className="grid border-b border-line sm:grid-cols-3">
            {[
              [i18n.language.startsWith('en') ? 'Contact details' : 'Coordonnées', i18n.language.startsWith('en') ? 'Your Canadian base' : 'Votre ancrage'],
              [i18n.language.startsWith('en') ? 'Profile' : 'Profil', i18n.language.startsWith('en') ? 'Skills and availability' : 'Compétences et disponibilité'],
              [i18n.language.startsWith('en') ? 'Preferences' : 'Préférences', i18n.language.startsWith('en') ? 'Useful updates only' : 'Seulement l’utile'],
            ].map(([title, hint], index) => <button key={title} type="button" onClick={() => index < onboardingStep && setOnboardingStep(index)} disabled={index > onboardingStep} className={`flex items-center gap-3 border-b border-line px-5 py-4 text-left last:border-b-0 sm:border-b-0 sm:border-r sm:last:border-r-0 ${index === onboardingStep ? 'bg-gold/[.08]' : ''}`}><span className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs font-bold ${index < onboardingStep ? 'bg-green text-white' : index === onboardingStep ? 'bg-gold text-green-deep' : 'bg-line text-ink-variant'}`}>{index < onboardingStep ? <i className="ri-check-line" /> : index + 1}</span><span><strong className="block text-[10px] uppercase tracking-[.13em] text-green-deep">{title}</strong><small className="mt-1 hidden text-[10px] text-ink-variant sm:block">{hint}</small></span></button>)}
          </div>

          <form className="px-6 py-8 sm:px-10 sm:py-10" onSubmit={onboardingStep < 2 ? (event) => { event.preventDefault(); setOnboardingStep((value) => value + 1); } : finishOnboarding}>
            {onboardingStep === 0 && identityFields}
            {onboardingStep === 1 && <div><p className="mb-6 max-w-2xl text-sm leading-6 text-ink-variant">{i18n.language.startsWith('en') ? 'These optional details help us prioritize opportunities, events and associations that fit you.' : 'Ces renseignements facultatifs nous aident à prioriser les occasions, événements et associations qui vous correspondent.'}</p>{professionalFields}</div>}
            {onboardingStep === 2 && <div className="space-y-6"><div><p className="text-[10px] font-bold uppercase tracking-[.16em] text-red-link">{i18n.language.startsWith('en') ? 'Your communication choices' : 'Vos choix de communication'}</p><h3 className="mt-2 font-display text-2xl font-bold text-green-deep">{i18n.language.startsWith('en') ? 'Stay informed, on your terms.' : 'Restez informé, à vos conditions.'}</h3><p className="mt-2 text-sm leading-6 text-ink-variant">{i18n.language.startsWith('en') ? 'Nothing promotional is enabled automatically. Choose only the operational updates you want.' : 'Aucune communication promotionnelle n’est activée automatiquement. Choisissez seulement les suivis opérationnels souhaités.'}</p></div><div className="grid gap-5 sm:grid-cols-2"><Field label={i18n.language.startsWith('en') ? 'Preferred language' : 'Langue préférée'} htmlFor="onboarding-language"><select id="onboarding-language" className={inputClasses} value={onboardingPreferences.preferredLanguage} onChange={(event) => setOnboardingPreferences({ ...onboardingPreferences, preferredLanguage: event.target.value as 'fr' | 'en' })}><option value="fr">Français</option><option value="en">English</option></select></Field><Field label={i18n.language.startsWith('en') ? 'Time zone' : 'Fuseau horaire'} htmlFor="onboarding-timezone"><select id="onboarding-timezone" className={inputClasses} value={onboardingPreferences.timeZone} onChange={(event) => setOnboardingPreferences({ ...onboardingPreferences, timeZone: event.target.value })}><option value="America/Toronto">Eastern — Toronto / Montréal</option><option value="America/Winnipeg">Central — Winnipeg</option><option value="America/Edmonton">Mountain — Edmonton</option><option value="America/Vancouver">Pacific — Vancouver</option><option value="America/Halifax">Atlantic — Halifax</option></select></Field></div><div className="grid gap-3 sm:grid-cols-2">{[
              ['emailEvents', 'ri-calendar-event-line', i18n.language.startsWith('en') ? 'Events and registrations' : 'Événements et inscriptions'],
              ['emailOpportunities', 'ri-briefcase-4-line', i18n.language.startsWith('en') ? 'Opportunities and volunteering' : 'Occasions et bénévolat'],
              ['emailMentorship', 'ri-user-heart-line', i18n.language.startsWith('en') ? 'Mentorship' : 'Mentorat'],
              ['emailServiceUpdates', 'ri-customer-service-2-line', i18n.language.startsWith('en') ? 'Service request updates' : 'Suivi des demandes'],
              ['emailNewsletter', 'ri-mail-star-line', i18n.language.startsWith('en') ? 'Community newsletter' : 'Infolettre communautaire'],
            ].map(([key, icon, label]) => <label key={key} className={`flex cursor-pointer items-center gap-3 rounded-2xl border p-4 ${onboardingPreferences[key as keyof UpdateMemberPreferenceRequest] ? 'border-green/25 bg-green/[.045]' : 'border-line'}`}><span className="flex h-10 w-10 items-center justify-center rounded-xl bg-green/8 text-lg text-green"><i className={icon} /></span><span className="flex-1 text-sm font-semibold text-green-deep">{label}</span><input type="checkbox" className="h-5 w-5 accent-green" checked={Boolean(onboardingPreferences[key as keyof UpdateMemberPreferenceRequest])} onChange={(event) => setOnboardingPreferences({ ...onboardingPreferences, [key]: event.target.checked })} /></label>)}</div><label className="flex items-start gap-3 rounded-2xl border border-line bg-canvas/40 p-4"><input type="checkbox" className="mt-0.5 h-5 w-5 accent-green" checked={onboardingPreferences.digestFrequency === 'Weekly'} onChange={(event) => setOnboardingPreferences({ ...onboardingPreferences, digestFrequency: event.target.checked ? 'Weekly' : 'Off' })} /><span><strong className="block text-sm text-green-deep">{i18n.language.startsWith('en') ? 'Weekly community digest' : 'Résumé communautaire hebdomadaire'}</strong><small className="mt-1 block leading-5 text-ink-variant">{i18n.language.startsWith('en') ? 'One concise email with upcoming events and opportunities.' : 'Un seul courriel concis avec les événements et occasions à venir.'}</small></span></label></div>}
            {status && <p className="mt-6 border-l-2 border-error bg-error/5 px-4 py-3 text-sm text-error">{status}</p>}
            <div className="mt-8 flex flex-col gap-3 border-t border-line pt-6 sm:flex-row sm:items-center sm:justify-between">
              <button type="button" onClick={() => onboardingStep > 0 ? setOnboardingStep((value) => value - 1) : logout()} className="text-left text-[11px] font-bold uppercase tracking-[0.14em] text-ink-variant transition-colors hover:text-red-link">{onboardingStep > 0 ? (i18n.language.startsWith('en') ? 'Back' : 'Retour') : t('public.member.onboarding.signOut')}</button>
              <Button type="submit" variant="primary" disabled={submitting} className="sm:min-w-64">
                {submitting ? <i className="ri-loader-4-line animate-spin" aria-hidden="true" /> : <i className="ri-arrow-right-line" aria-hidden="true" />}
                {submitting ? t('public.member.onboarding.submitting') : onboardingStep < 2 ? (i18n.language.startsWith('en') ? 'Continue' : 'Continuer') : t('public.member.onboarding.submit')}
              </Button>
            </div>
          </form>
        </section>
      );
    }

    const accountPanel = (
      <div className="space-y-6">
      <form className="overflow-hidden rounded-[24px] border border-line bg-surface shadow-[0_14px_40px_rgba(0,59,27,.06)]" onSubmit={handleProfileSave}>
        <div className="flex flex-col gap-4 border-b border-line bg-green/[0.045] px-6 py-5 sm:flex-row sm:items-center sm:justify-between sm:px-7">
          <div className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-green text-lg text-white"><i className="ri-mail-check-line" aria-hidden="true" /></span>
            <div>
              <p className="text-[9px] font-bold uppercase tracking-[0.14em] text-green">{t('public.member.login.accountBadge')}</p>
              <p className="mt-1 text-sm text-ink-variant">{member.email}</p>
            </div>
          </div>
          <span className="inline-flex w-fit items-center gap-2 rounded-full border border-green/20 bg-green/8 px-3 py-1.5 text-[9px] font-bold uppercase tracking-[0.12em] text-green">
            <span className="h-2 w-2 rounded-full bg-green" />{t('public.member.login.accountActive')}
          </span>
        </div>
        <div className="space-y-6 p-6 sm:p-7">
          {profileFields}
          {status && <p className="rounded-xl border border-green/15 bg-green/[0.055] px-4 py-3 text-sm text-green">{status}</p>}
          <div className="flex justify-end border-t border-line pt-5">
            <Button type="submit" variant="primary" disabled={submitting} className="w-full sm:w-auto sm:min-w-64">
              {submitting ? <i className="ri-loader-4-line animate-spin" aria-hidden="true" /> : <i className="ri-save-line" aria-hidden="true" />}
              {t('public.member.login.saveProfile')}
            </Button>
          </div>
        </div>
      </form>
      <AccountSecurityPanel />
      </div>
    );

    return <MemberCommunityWorkspace member={member} accountPanel={accountPanel} onLogout={logout} />;
  }

  const signupMode = mode === 'signup';

  return (
    <div className={embedded ? '' : 'border border-line bg-surface p-6'}>
      <div className="mb-6">
        <p className="text-label-md uppercase text-red-link">{t(signupMode ? 'public.member.gateway.signupEyebrow' : 'public.member.login.badge')}</p>
        <h2 className="mt-3 font-display text-[30px] font-bold leading-tight text-green">{t(signupMode ? 'public.member.gateway.signupTitle' : 'public.member.login.title')}</h2>
        <p className="mt-2 max-w-xl text-body-md leading-7 text-ink-variant">{t(signupMode ? 'public.member.gateway.signupIntro' : 'public.member.login.subtitle')}</p>
      </div>

      {!mfaChallenge && <GoogleSignInButton
        disabled={submitting}
        onCredential={handleGoogleCredential}
        onUnavailable={handleGoogleUnavailable}
      />}

      {!mfaChallenge && import.meta.env.VITE_GOOGLE_CLIENT_ID && (
        <p className="mt-3 text-center text-xs leading-5 text-ink-variant">
          {t(signupMode ? 'public.member.gateway.googleSignupHint' : 'public.member.login.googleSignupHint')}
        </p>
      )}

      {!mfaChallenge && !signupMode && import.meta.env.VITE_GOOGLE_CLIENT_ID && (
        <div className="my-5 flex items-center gap-3 text-[10px] font-bold uppercase tracking-[0.14em] text-ink-variant">
          <span className="h-px flex-1 bg-line" aria-hidden="true" />
          <span>{t('public.member.login.orEmail')}</span>
          <span className="h-px flex-1 bg-line" aria-hidden="true" />
        </div>
      )}

      {mfaChallenge && <form className="space-y-5" onSubmit={handleMfaSubmit}>
        <div className="rounded-2xl border border-green/15 bg-green/[.04] p-5"><i className={`${mfaMethod === 'Email' ? 'ri-mail-check-line' : 'ri-shield-keyhole-line'} text-2xl text-green`} /><h3 className="mt-3 font-display text-xl font-bold text-green-deep">{t('public.member.login.mfaTitle')}</h3><p className="mt-2 text-sm leading-6 text-ink-variant">{mfaMethod === 'Email' ? t('public.member.login.mfaEmailHint', { destination: mfaDestination }) : t('public.member.login.mfaHint')}</p></div>
        <Field label={t('public.member.login.mfaCode')} htmlFor="member-mfa-code" required><input id="member-mfa-code" value={mfaCode} onChange={e => setMfaCode(e.target.value)} inputMode="numeric" autoComplete="one-time-code" className={inputClasses} autoFocus required placeholder="000 000" /></Field>
        {status && <p role="status" className="rounded-xl border border-green/20 bg-green/5 p-3 text-sm font-semibold text-green">{status}</p>}
        <Button type="submit" variant="secondary" className="w-full" disabled={submitting || mfaCode.trim().length < 6}><i className="ri-shield-check-line" />{t('public.member.login.mfaVerify')}</Button>
        {mfaMethod === 'Email' && <button type="button" disabled={submitting} className="min-h-11 w-full text-sm font-semibold text-green disabled:opacity-50" onClick={handleMfaResend}><i className="ri-refresh-line mr-2" />{t('public.member.login.mfaResend')}</button>}
        <button type="button" className="min-h-11 w-full text-sm font-semibold text-green" onClick={() => { setMfaChallenge(''); setMfaCode(''); setStatus(null); }}>{t('public.member.login.mfaBack')}</button>
      </form>}

      {!mfaChallenge && !signupMode && <form className="space-y-5" onSubmit={handleSubmit}>
        <Field label={t('public.member.login.email')} htmlFor="login-email">
          <input
            type="email"
            id="login-email"
            value={loginData.email}
            onChange={(e) => setLoginData({ ...loginData, email: e.target.value })}
            className={inputClasses}
            placeholder="votre.email@exemple.com"
          />
        </Field>

        <Field label={t('public.member.login.password')} htmlFor="login-password">
          <input
            type="password"
            id="login-password"
            value={loginData.password}
            onChange={(e) => setLoginData({ ...loginData, password: e.target.value })}
            className={inputClasses}
            placeholder="••••••••"
          />
        </Field>

        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <label className="flex cursor-pointer items-center gap-2">
            <input type="checkbox" className="h-4 w-4 cursor-pointer rounded-control-sm border border-outline accent-green" />
            <span className="text-body-md text-ink-variant">{t('public.member.login.remember')}</span>
          </label>
          <button
            type="button"
            onClick={handleForgotPassword}
            className="text-left text-label-md uppercase text-red-link hover:text-green sm:text-right"
          >
            {t('public.member.login.forgot')}
          </button>
        </div>

        {status && <p className="border border-error/30 bg-error/5 p-3 text-sm text-error">{status}</p>}

        <Button type="submit" variant="secondary" className="w-full" disabled={submitting}>
          <i className="ri-login-circle-line" aria-hidden="true"></i>
          {submitting ? t('public.member.login.submitting') : t('public.member.login.submit')}
        </Button>
      </form>}

      {signupMode && status && <p className="mt-5 border border-error/30 bg-error/5 p-3 text-sm text-error">{status}</p>}
    </div>
  );
};

export default MemberLoginForm;

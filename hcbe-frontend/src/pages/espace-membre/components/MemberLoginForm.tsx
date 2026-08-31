import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../../../components/ui';
import { useAuth } from '../../../contexts/AuthContext';
import { memberAccountApi } from '../../../lib/api/member-account';
import type { MemberDto } from '../../../lib/api/types';
import { authApi } from '../../../lib/api/auth';
import MemberCommunityWorkspace from './MemberCommunityWorkspace';

const MemberLoginForm = () => {
  const { t } = useTranslation();
  const { user, login, logout } = useAuth();
  const [loginData, setLoginData] = useState({ email: '', password: '' });
  const [member, setMember] = useState<MemberDto | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [resetPassword, setResetPassword] = useState('');
  const [editingProfile, setEditingProfile] = useState(false);
  const [profileData, setProfileData] = useState({
    firstName: '', lastName: '', phone: '', city: '', province: '', profession: '',
    expertise: '', interests: '', availability: '',
  });
  const resetToken = new URLSearchParams(window.location.search).get('resetToken');

  useEffect(() => {
    if (!user?.memberId) {
      setMember(null);
      return;
    }

    memberAccountApi.getMe().then((response) => {
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
      }
    });
  }, [user?.memberId]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setStatus(null);
    const result = await login(loginData.email.trim(), loginData.password);
    const storedUser = JSON.parse(localStorage.getItem('hcbe_user') || 'null');
    if (!result.success) {
      setStatus(result.message || t('public.member.login.error'));
    } else if (!storedUser?.memberId) {
      logout();
      setStatus(t('public.member.login.notMember'));
    }
    setSubmitting(false);
  };

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
    setSubmitting(true);
    setStatus(null);
    const response = await memberAccountApi.updateMe(profileData);
    if (response.success && response.data) {
      setMember(response.data);
      setEditingProfile(false);
      setStatus(t('public.member.login.profileSaved'));
    } else {
      setStatus(response.message || t('public.member.login.error'));
    }
    setSubmitting(false);
  };

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
    return (
      <div className="space-y-7">
      <div className="border border-line bg-surface p-6">
        <p className="text-label-md uppercase text-red-link">{t('public.member.login.accountBadge')}</p>
        <h2 className="mt-3 font-display text-headline-md text-green">
          {member ? `${member.firstName} ${member.lastName}` : t('public.member.login.loading')}
        </h2>
        {member && !editingProfile && (
          <div className="mt-4 space-y-2 text-body-md text-ink-variant">
            <p>{member.email}</p>
            <p>{[member.city, member.province].filter(Boolean).join(', ')}</p>
            <p>{member.profession}</p>
          </div>
        )}
        {member && editingProfile && (
          <form className="mt-5 space-y-4" onSubmit={handleProfileSave}>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label={t('public.member.form.firstName')} htmlFor="member-profile-first-name"><input id="member-profile-first-name" className={inputClasses} required value={profileData.firstName} onChange={(e) => setProfileData({ ...profileData, firstName: e.target.value })} /></Field>
              <Field label={t('public.member.form.lastName')} htmlFor="member-profile-last-name"><input id="member-profile-last-name" className={inputClasses} required value={profileData.lastName} onChange={(e) => setProfileData({ ...profileData, lastName: e.target.value })} /></Field>
              <Field label={t('public.member.form.phone')} htmlFor="member-profile-phone"><input id="member-profile-phone" className={inputClasses} value={profileData.phone} onChange={(e) => setProfileData({ ...profileData, phone: e.target.value })} /></Field>
              <Field label={t('public.member.form.city')} htmlFor="member-profile-city"><input id="member-profile-city" className={inputClasses} value={profileData.city} onChange={(e) => setProfileData({ ...profileData, city: e.target.value })} /></Field>
              <Field label={t('public.member.form.province')} htmlFor="member-profile-province"><input id="member-profile-province" className={inputClasses} value={profileData.province} onChange={(e) => setProfileData({ ...profileData, province: e.target.value })} /></Field>
              <Field label={t('public.member.form.profession')} htmlFor="member-profile-profession"><input id="member-profile-profession" className={inputClasses} value={profileData.profession} onChange={(e) => setProfileData({ ...profileData, profession: e.target.value })} /></Field>
            </div>
            <Field label={t('public.member.form.expertise')} htmlFor="member-profile-expertise"><textarea id="member-profile-expertise" rows={2} className={inputClasses} value={profileData.expertise} onChange={(e) => setProfileData({ ...profileData, expertise: e.target.value })} /></Field>
            <Field label={t('public.member.login.interests')} htmlFor="member-profile-interests"><textarea id="member-profile-interests" rows={2} className={inputClasses} value={profileData.interests} onChange={(e) => setProfileData({ ...profileData, interests: e.target.value })} /></Field>
            <Field label={t('public.member.login.availability')} htmlFor="member-profile-availability"><input id="member-profile-availability" className={inputClasses} value={profileData.availability} onChange={(e) => setProfileData({ ...profileData, availability: e.target.value })} /></Field>
            <div className="flex gap-3"><Button type="submit" variant="primary" disabled={submitting}>{t('public.member.login.saveProfile')}</Button><Button type="button" variant="tertiary" onClick={() => setEditingProfile(false)}>{t('admin.common.cancel')}</Button></div>
          </form>
        )}
        {status && <p className="mt-4 border border-line p-3 text-sm text-ink-variant">{status}</p>}
        {!editingProfile && <div className="mt-6 flex flex-col gap-3 sm:flex-row"><Button type="button" variant="primary" className="flex-1" onClick={() => setEditingProfile(true)}>{t('public.member.login.editProfile')}</Button><Button type="button" variant="secondary" className="flex-1" onClick={logout}>{t('public.member.login.logout')}</Button></div>}
      </div>
      <MemberCommunityWorkspace />
      </div>
    );
  }

  return (
    <div className="border border-line bg-surface p-6">
      <div className="mb-6">
        <p className="text-label-md uppercase text-red-link">{t('public.member.login.badge')}</p>
        <h2 className="mt-3 font-display text-headline-md text-green">{t('public.member.login.title')}</h2>
        <p className="mt-2 text-body-md text-ink-variant">{t('public.member.login.subtitle')}</p>
      </div>

      <form className="space-y-5" onSubmit={handleSubmit}>
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
      </form>
    </div>
  );
};

export default MemberLoginForm;

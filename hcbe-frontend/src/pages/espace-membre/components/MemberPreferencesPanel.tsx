import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../../../components/ui';
import { memberAccountApi } from '../../../lib/api/member-account';
import type { MemberOnboarding, UpdateMemberPreferenceRequest } from '../../../lib/api/types';
import { disablePushNotifications, enablePushNotifications, sendTestPushNotification, setAppNotificationsEnabled, supportsPushNotifications } from '../../../lib/pwa/notifications';
import MemberPrivacyPanel from './MemberPrivacyPanel';

const defaults: UpdateMemberPreferenceRequest = {
  preferredLanguage: 'fr', timeZone: 'America/Toronto', emailEvents: false,
  emailOpportunities: false, emailMentorship: false, emailServiceUpdates: false,
  emailNewsletter: false, pushNotifications: false,
  digestFrequency: 'Off',
};

export default function MemberPreferencesPanel() {
  const { i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const [onboarding, setOnboarding] = useState<MemberOnboarding | null>(null);
  const [form, setForm] = useState(defaults);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const load = async () => {
    const response = await memberAccountApi.getOnboarding();
    if (response.data) {
      setOnboarding(response.data);
      const { hasCompletedPreferences, updatedAt: _updated, lastDigestSentAtUtc: _lastDigest, ...preferences } = response.data.preferences;
      setForm(hasCompletedPreferences ? preferences : { ...defaults, preferredLanguage: preferences.preferredLanguage, timeZone: preferences.timeZone });
    }
  };
  useEffect(() => { void load(); }, []);

  const persist = async (preferences: UpdateMemberPreferenceRequest) => {
    setBusy(true); setNotice(null);
    try {
      const response = await memberAccountApi.updatePreferences(preferences);
      setNotice(response.success ? (fr ? 'Vos préférences sont enregistrées.' : 'Your preferences have been saved.') : response.message || 'Error');
      if (response.success) {
        setAppNotificationsEnabled(preferences.pushNotifications);
        await load();
      }
    } catch (error) {
      setNotice(error instanceof Error ? error.message : (fr ? 'Impossible d’enregistrer vos préférences.' : 'Unable to save your preferences.'));
    } finally { setBusy(false); }
  };

  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    await persist(form);
  };

  const changeOption = async (key: keyof UpdateMemberPreferenceRequest, checked: boolean) => {
    if (key !== 'pushNotifications') { setForm({ ...form, [key]: checked }); return; }
    setBusy(true); setNotice(null);
    try {
      if (checked) await enablePushNotifications(); else await disablePushNotifications();
      setForm((current) => ({ ...current, pushNotifications: checked }));
      setNotice(checked ? (fr ? 'Cet appareil est prêt à recevoir les notifications.' : 'This device is ready to receive notifications.') : (fr ? 'Les notifications sont désactivées sur cet appareil.' : 'Notifications are disabled on this device.'));
    } catch {
      setForm((current) => ({ ...current, pushNotifications: false }));
      setNotice(fr ? 'Les notifications ne sont pas disponibles ou leur autorisation a été refusée.' : 'Notifications are unavailable or permission was denied.');
    } finally { setBusy(false); }
  };

  const testPush = async () => {
    setBusy(true); setNotice(null);
    try { await sendTestPushNotification(fr ? 'fr' : 'en'); setNotice(fr ? 'Notification de test envoyée.' : 'Test notification sent.'); }
    catch { setNotice(fr ? 'Impossible d’envoyer la notification de test.' : 'Unable to send the test notification.'); }
    finally { setBusy(false); }
  };

  const withdrawOptional = async () => {
    const withdrawn = {
      ...form,
      emailEvents: false,
      emailOpportunities: false,
      emailMentorship: false,
      emailServiceUpdates: false,
      emailNewsletter: false,
      pushNotifications: false,
      digestFrequency: 'Off' as const,
    };
    setForm(withdrawn);
    await persist(withdrawn);
  };

  const options: Array<[keyof UpdateMemberPreferenceRequest, string, string]> = [
    ['emailEvents', fr ? 'Événements et inscriptions' : 'Events and registrations', 'ri-calendar-event-line'],
    ['emailOpportunities', fr ? 'Occasions et bénévolat' : 'Opportunities and volunteering', 'ri-briefcase-4-line'],
    ['emailMentorship', fr ? 'Mentorat et jumelages' : 'Mentorship and matches', 'ri-user-heart-line'],
    ['emailServiceUpdates', fr ? 'Suivi de mes demandes' : 'My service request updates', 'ri-customer-service-2-line'],
    ['emailNewsletter', fr ? 'Infolettre communautaire' : 'Community newsletter', 'ri-mail-star-line'],
    ['pushNotifications', fr ? 'Notifications de l’application' : 'App notifications', 'ri-notification-3-line'],
  ];

  return (
    <div className="space-y-7">
      <section className="relative overflow-hidden rounded-[26px] bg-green-deep p-6 text-white sm:p-8">
        <div className="absolute -right-16 -top-20 h-52 w-52 rounded-full border-[34px] border-gold/[.09]" />
        <div className="relative grid gap-6 lg:grid-cols-[1fr_280px] lg:items-end">
          <div><p className="text-[9px] font-bold uppercase tracking-[.18em] text-gold">{fr ? 'Bienvenue dans la communauté' : 'Welcome to the community'}</p><h2 className="mt-3 font-display text-3xl font-bold text-white sm:text-4xl">{fr ? 'Configurez votre expérience.' : 'Set up your experience.'}</h2><p className="mt-3 max-w-xl text-sm leading-6 text-green-dim">{fr ? 'Quelques choix simples nous permettent de vous envoyer uniquement ce qui vous est utile.' : 'A few simple choices help us send only what is useful to you.'}</p></div>
          <div className="rounded-2xl border border-white/10 bg-white/[.06] p-4"><div className="flex items-end justify-between"><span className="text-[9px] font-bold uppercase tracking-[.14em] text-green-dim">Onboarding</span><strong className="font-display text-3xl text-gold">{onboarding?.completionPercent ?? 0}%</strong></div><div className="mt-3 h-2 rounded-full bg-white/10"><div className="h-full rounded-full bg-gold" style={{ width: `${onboarding?.completionPercent ?? 0}%` }} /></div></div>
        </div>
      </section>

      {onboarding && <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">{onboarding.steps.map((step) => <Link key={step.key} to={step.actionUrl} className={`flex items-center gap-3 rounded-2xl border p-4 ${step.completed ? 'border-green/20 bg-green/[.045]' : 'border-line bg-surface'}`}><span className={`flex h-9 w-9 items-center justify-center rounded-xl ${step.completed ? 'bg-green text-white' : 'bg-gold/15 text-green'}`}><i className={step.completed ? 'ri-check-line' : 'ri-arrow-right-line'} /></span><span className="text-xs font-semibold leading-5 text-green-deep">{step.title}</span></Link>)}</section>}

      <form onSubmit={save} className="overflow-hidden rounded-[26px] border border-line bg-surface shadow-[0_16px_45px_rgba(0,59,27,.06)]">
        <header className="border-b border-line bg-green/[.035] p-6 sm:px-8"><p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{fr ? 'Préférences' : 'Preferences'}</p><h3 className="mt-2 font-display text-2xl font-bold text-green-deep">{fr ? 'Ce que vous souhaitez recevoir' : 'What you want to receive'}</h3></header>
        <div className="space-y-7 p-6 sm:p-8">
          <div className="grid gap-5 sm:grid-cols-2">
            <Field label={fr ? 'Langue préférée' : 'Preferred language'} htmlFor="preference-language"><select id="preference-language" className={inputClasses} value={form.preferredLanguage} onChange={(event) => setForm({ ...form, preferredLanguage: event.target.value as 'fr' | 'en' })}><option value="fr">Français</option><option value="en">English</option></select></Field>
            <Field label={fr ? 'Fuseau horaire' : 'Time zone'} htmlFor="preference-timezone"><select id="preference-timezone" className={inputClasses} value={form.timeZone} onChange={(event) => setForm({ ...form, timeZone: event.target.value })}><option value="America/Toronto">Eastern — Toronto / Montréal</option><option value="America/Winnipeg">Central — Winnipeg</option><option value="America/Edmonton">Mountain — Edmonton</option><option value="America/Vancouver">Pacific — Vancouver</option><option value="America/Halifax">Atlantic — Halifax</option></select></Field>
          </div>
          <div className="rounded-2xl border border-line bg-canvas/45 p-5">
            <div className="grid gap-4 sm:grid-cols-[1fr_240px] sm:items-center">
              <div><p className="text-sm font-semibold text-green-deep">{fr ? 'Résumé communautaire' : 'Community digest'}</p><p className="mt-1 text-xs leading-5 text-ink-variant">{fr ? 'Recevez un seul courriel hebdomadaire avec les prochains événements et les nouvelles occasions. Désactivé par défaut.' : 'Receive one weekly email with upcoming events and new opportunities. Off by default.'}</p></div>
              <select className={inputClasses} value={form.digestFrequency} onChange={(event) => setForm({ ...form, digestFrequency: event.target.value as 'Off' | 'Weekly' })}><option value="Off">{fr ? 'Désactivé' : 'Off'}</option><option value="Weekly">{fr ? 'Chaque semaine' : 'Weekly'}</option></select>
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">{options.map(([key, label, icon]) => <label key={key} className={`flex cursor-pointer items-center gap-3 rounded-2xl border p-4 ${form[key] ? 'border-green/25 bg-green/[.045]' : 'border-line'}`}><span className="flex h-10 w-10 items-center justify-center rounded-xl bg-green/8 text-lg text-green"><i className={icon} /></span><span className="flex-1 text-sm font-semibold text-green-deep">{label}</span><input type="checkbox" className="h-5 w-5 accent-green" checked={Boolean(form[key])} disabled={busy || (key === 'pushNotifications' && !supportsPushNotifications())} onChange={(event) => void changeOption(key, event.target.checked)} /></label>)}</div>
          {notice && <p className="rounded-xl border border-green/15 bg-green/5 px-4 py-3 text-sm text-green">{notice}</p>}
          <div className="flex flex-wrap justify-end gap-3">{form.pushNotifications && <Button type="button" variant="tertiary" disabled={busy} onClick={testPush}><i className="ri-notification-badge-line" />{fr ? 'Tester sur cet appareil' : 'Test on this device'}</Button>}<Button type="submit" variant="secondary" disabled={busy}><i className={busy ? 'ri-loader-4-line animate-spin' : 'ri-save-line'} />{fr ? 'Enregistrer mes préférences' : 'Save my preferences'}</Button></div>
        </div>
      </form>

      <MemberPrivacyPanel fr={fr} withdrawing={busy} onWithdrawOptional={withdrawOptional} />
    </div>
  );
}

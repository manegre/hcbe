import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../../../components/ui';
import { memberAccountApi } from '../../../lib/api/member-account';
import type { MemberOnboarding, UpdateMemberPreferenceRequest } from '../../../lib/api/types';
import { setAppNotificationsEnabled } from '../../../lib/pwa/notifications';

const defaults: UpdateMemberPreferenceRequest = {
  preferredLanguage: 'fr', timeZone: 'America/Toronto', emailEvents: true,
  emailOpportunities: true, emailMentorship: true, emailServiceUpdates: true,
  emailNewsletter: true, pushNotifications: false,
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
      const { hasCompletedPreferences: _done, updatedAt: _updated, ...preferences } = response.data.preferences;
      setForm(preferences);
    }
  };
  useEffect(() => { void load(); }, []);

  const save = async (event: React.FormEvent) => {
    event.preventDefault(); setBusy(true); setNotice(null);
    try {
      const response = await memberAccountApi.updatePreferences(form);
      setNotice(response.success ? (fr ? 'Vos préférences sont enregistrées.' : 'Your preferences have been saved.') : response.message || 'Error');
      if (response.success) {
        setAppNotificationsEnabled(form.pushNotifications);
        await load();
      }
    } finally { setBusy(false); }
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
          <div className="grid gap-3 sm:grid-cols-2">{options.map(([key, label, icon]) => <label key={key} className={`flex cursor-pointer items-center gap-3 rounded-2xl border p-4 ${form[key] ? 'border-green/25 bg-green/[.045]' : 'border-line'}`}><span className="flex h-10 w-10 items-center justify-center rounded-xl bg-green/8 text-lg text-green"><i className={icon} /></span><span className="flex-1 text-sm font-semibold text-green-deep">{label}</span><input type="checkbox" className="h-5 w-5 accent-green" checked={Boolean(form[key])} onChange={async (event) => { let checked = event.target.checked; if (key === 'pushNotifications' && checked && 'Notification' in window) checked = (await Notification.requestPermission()) === 'granted'; setForm({ ...form, [key]: checked }); }} /></label>)}</div>
          {notice && <p className="rounded-xl border border-green/15 bg-green/5 px-4 py-3 text-sm text-green">{notice}</p>}
          <div className="flex justify-end"><Button type="submit" variant="secondary" disabled={busy}><i className={busy ? 'ri-loader-4-line animate-spin' : 'ri-save-line'} />{fr ? 'Enregistrer mes préférences' : 'Save my preferences'}</Button></div>
        </div>
      </form>
    </div>
  );
}

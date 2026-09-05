import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../ui';
import { securityApi } from '../../lib/api/security';
import type { AccountSession, MfaEnrollment, MfaStatus } from '../../lib/api/types';

export function AccountSecurityPanel() {
  const { i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const locale = fr ? 'fr-CA' : 'en-CA';
  const [status, setStatus] = useState<MfaStatus | null>(null);
  const [enrollment, setEnrollment] = useState<MfaEnrollment | null>(null);
  const [sessions, setSessions] = useState<AccountSession[]>([]);
  const [code, setCode] = useState('');
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);

  const copy = fr ? {
    title: 'Sécurité du compte', subtitle: 'Protégez votre accès et gardez le contrôle des appareils connectés.',
    mfa: 'Authentification multifacteur', enabled: 'Activée', disabled: 'Non activée',
    mfaHint: 'Ajoutez HCBE Canada dans votre application d’authentification pour demander un code à chaque connexion.',
    begin: 'Configurer le MFA', secret: 'Clé de configuration', open: 'Ouvrir l’application', verify: 'Vérifier et activer', code: 'Code à 6 chiffres',
    recovery: 'Codes de récupération', recoveryHint: 'Conservez ces codes hors ligne. Chaque code ne fonctionne qu’une fois.',
    disable: 'Désactiver le MFA', sessions: 'Appareils connectés', current: 'Session actuelle', revoke: 'Déconnecter', revokeOthers: 'Déconnecter les autres appareils',
    noSessions: 'Aucune session active.', expires: 'Expire', success: 'Sécurité du compte mise à jour.', error: 'Impossible de terminer cette action.',
  } : {
    title: 'Account security', subtitle: 'Protect your access and stay in control of connected devices.',
    mfa: 'Multi-factor authentication', enabled: 'Enabled', disabled: 'Not enabled',
    mfaHint: 'Add HCBE Canada to your authenticator app to require a code at every sign-in.',
    begin: 'Set up MFA', secret: 'Setup key', open: 'Open authenticator', verify: 'Verify and enable', code: '6-digit code',
    recovery: 'Recovery codes', recoveryHint: 'Store these codes offline. Each code works only once.',
    disable: 'Disable MFA', sessions: 'Connected devices', current: 'Current session', revoke: 'Sign out', revokeOthers: 'Sign out other devices',
    noSessions: 'No active sessions.', expires: 'Expires', success: 'Account security updated.', error: 'Unable to complete this action.',
  };

  const load = async () => {
    const [mfa, active] = await Promise.all([securityApi.getMfaStatus(), securityApi.getSessions()]);
    setStatus(mfa.data ?? null); setSessions(active.data ?? []);
  };
  useEffect(() => { void load().catch(() => setNotice(copy.error)); }, [fr]);

  const run = async (action: () => Promise<void>) => {
    setBusy(true); setNotice('');
    try { await action(); setNotice(copy.success); await load(); } catch { setNotice(copy.error); }
    finally { setBusy(false); }
  };

  const begin = () => run(async () => { const response = await securityApi.beginMfaEnrollment(); if (!response.success || !response.data) throw new Error(); setEnrollment(response.data); });
  const confirm = () => run(async () => { const response = await securityApi.confirmMfaEnrollment(code); if (!response.success || !response.data) throw new Error(); setRecoveryCodes(response.data.recoveryCodes); setEnrollment(null); setCode(''); });
  const disable = () => run(async () => { const response = await securityApi.disableMfa(code); if (!response.success) throw new Error(); setCode(''); setRecoveryCodes([]); });

  return (
    <section className="overflow-hidden rounded-[26px] border border-line bg-surface shadow-[0_18px_55px_rgba(0,59,27,.06)]">
      <header className="public-grid-pattern relative overflow-hidden bg-green-deep px-5 py-7 text-white sm:px-8">
        <div className="relative flex flex-wrap items-start justify-between gap-5">
          <div><p className="text-[10px] font-bold uppercase tracking-[.18em] text-gold">{fr ? 'Protection personnelle' : 'Personal protection'}</p><h2 className="mt-2 font-display text-2xl font-bold sm:text-3xl">{copy.title}</h2><p className="mt-2 max-w-2xl text-sm text-white/68">{copy.subtitle}</p></div>
          <span className={`rounded-full border px-4 py-2 text-[10px] font-bold uppercase tracking-[.13em] ${status?.enabled ? 'border-gold/40 bg-gold/10 text-gold' : 'border-white/20 bg-white/5 text-white/70'}`}><i className={`${status?.enabled ? 'ri-shield-check-fill' : 'ri-shield-line'} mr-2`} />{status?.enabled ? copy.enabled : copy.disabled}</span>
        </div>
      </header>
      <div className="grid gap-0 lg:grid-cols-2">
        <div className="border-b border-line p-5 sm:p-8 lg:border-b-0 lg:border-r">
          <div className="flex items-start gap-4"><span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-green/10 text-xl text-green"><i className="ri-key-2-line" /></span><div><h3 className="font-display text-xl font-bold text-green-deep">{copy.mfa}</h3><p className="mt-1 text-sm leading-6 text-ink-variant">{copy.mfaHint}</p></div></div>
          {!status?.enabled && !enrollment && <Button type="button" variant="secondary" onClick={begin} disabled={busy} className="mt-6"><i className="ri-add-line" />{copy.begin}</Button>}
          {enrollment && <div className="mt-6 rounded-2xl border border-gold/40 bg-gold/[.06] p-5">
            <p className="text-[10px] font-bold uppercase tracking-[.14em] text-green">{copy.secret}</p><code className="mt-2 block break-all rounded-xl bg-surface px-4 py-3 text-sm font-bold tracking-[.08em] text-green-deep">{enrollment.secret}</code>
            <a href={enrollment.otpAuthUri} className="mt-3 inline-flex min-h-11 items-center gap-2 text-sm font-bold text-green hover:text-red-link"><i className="ri-smartphone-line" />{copy.open}</a>
            <Field label={copy.code} htmlFor="mfa-enroll-code" required className="mt-4"><input id="mfa-enroll-code" value={code} onChange={(e) => setCode(e.target.value)} inputMode="numeric" autoComplete="one-time-code" className={inputClasses} placeholder="000 000" /></Field>
            <Button type="button" onClick={confirm} disabled={busy || code.trim().length < 6} className="mt-4"><i className="ri-shield-check-line" />{copy.verify}</Button>
          </div>}
          {status?.enabled && <div className="mt-6"><Field label={copy.code} htmlFor="mfa-disable-code"><input id="mfa-disable-code" value={code} onChange={(e) => setCode(e.target.value)} className={inputClasses} inputMode="numeric" /></Field><Button type="button" variant="secondary" onClick={disable} disabled={busy || code.trim().length < 6} className="mt-3"><i className="ri-lock-unlock-line" />{copy.disable}</Button></div>}
          {recoveryCodes.length > 0 && <div className="mt-6 rounded-2xl border border-green/20 bg-green/[.04] p-5"><h4 className="font-display text-lg font-bold text-green-deep">{copy.recovery}</h4><p className="mt-1 text-sm text-ink-variant">{copy.recoveryHint}</p><div className="mt-4 grid gap-2 sm:grid-cols-2">{recoveryCodes.map(value => <code key={value} className="rounded-lg bg-surface-container p-2 text-center text-xs font-bold text-ink">{value}</code>)}</div></div>}
        </div>
        <div className="p-5 sm:p-8">
          <div className="flex flex-wrap items-center justify-between gap-4"><div><h3 className="font-display text-xl font-bold text-green-deep">{copy.sessions}</h3><p className="mt-1 text-sm text-ink-variant">{sessions.length} {fr ? 'session(s) active(s)' : 'active session(s)'}</p></div>{sessions.length > 1 && <Button type="button" variant="secondary" onClick={() => run(async () => { await securityApi.revokeOtherSessions(); })} disabled={busy}>{copy.revokeOthers}</Button>}</div>
          <div className="admin-sidebar-scroll mt-5 max-h-[540px] divide-y divide-line overflow-y-auto border-y border-line pr-1">{sessions.length === 0 ? <p className="py-8 text-sm text-ink-variant">{copy.noSessions}</p> : sessions.map(session => <article key={session.id} className="flex items-center gap-4 py-4"><span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-surface-container text-green"><i className="ri-device-line" /></span><div className="min-w-0 flex-1"><div className="flex flex-wrap items-center gap-2"><strong className="text-sm text-ink">{session.deviceName === 'Appareil inconnu' || session.deviceName === 'Unknown device' ? (fr ? 'Appareil inconnu' : 'Unknown device') : session.deviceName}</strong>{session.isCurrent && <span className="rounded-full bg-green/10 px-2 py-1 text-[9px] font-bold uppercase text-green">{copy.current}</span>}</div><p className="mt-1 text-xs text-ink-variant">{session.ipAddress || '—'} · {copy.expires} {new Date(session.expiresAtUtc).toLocaleDateString(locale)}</p></div><button type="button" onClick={() => run(async () => { await securityApi.revokeSession(session.id); })} className="flex min-h-11 items-center rounded-lg px-3 text-xs font-bold text-red-link hover:bg-red-link/5">{copy.revoke}</button></article>)}</div>
        </div>
      </div>
      {notice && <p role="status" className="border-t border-line px-6 py-4 text-sm font-semibold text-green">{notice}</p>}
    </section>
  );
}

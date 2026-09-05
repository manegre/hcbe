import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { securityApi } from '../../lib/api/security';
import type { AdminAccountSession } from '../../lib/api/types';
import { Button } from '../ui';

export function AdminSessionCenter() {
  const { i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const locale = fr ? 'fr-CA' : 'en-CA';
  const [sessions, setSessions] = useState<AdminAccountSession[]>([]);
  const [busy, setBusy] = useState('');
  const [notice, setNotice] = useState('');
  const staleCutoff = useMemo(() => Date.now() - 30 * 24 * 60 * 60 * 1000, []);
  const copy = fr ? {
    eyebrow: 'Supervision des accès', title: 'Sessions administratives',
    subtitle: 'Repérez les appareils oubliés et révoquez immédiatement une session compromise.',
    empty: 'Aucune session administrative active.', current: 'Votre compte', stale: 'Inactive depuis 30 jours',
    lastSeen: 'Dernière activité', expires: 'Expiration', revoke: 'Révoquer', success: 'La session a été révoquée.', error: 'Impossible de mettre à jour les sessions.'
  } : {
    eyebrow: 'Access supervision', title: 'Administrator sessions',
    subtitle: 'Spot forgotten devices and immediately revoke a compromised session.',
    empty: 'No active administrator session.', current: 'Your account', stale: 'Inactive for 30 days',
    lastSeen: 'Last activity', expires: 'Expires', revoke: 'Revoke', success: 'The session was revoked.', error: 'Unable to update sessions.'
  };
  const load = async () => { const response = await securityApi.getAdminSessions(); setSessions(response.data ?? []); };
  useEffect(() => { void load().catch(() => setNotice(copy.error)); }, [fr]);
  const revoke = async (id: string) => { setBusy(id); setNotice(''); try { const response = await securityApi.revokeAdminSession(id); if (!response.success) throw new Error(); setNotice(copy.success); await load(); } catch { setNotice(copy.error); } finally { setBusy(''); } };

  return <section className="overflow-hidden rounded-[24px] border border-line bg-surface">
    <header className="flex flex-wrap items-start justify-between gap-4 border-b border-line bg-canvas/45 p-5 sm:p-7">
      <div><p className="text-[10px] font-bold uppercase tracking-[.15em] text-red-link">{copy.eyebrow}</p><h2 className="mt-1 font-display text-2xl font-bold text-green-deep">{copy.title}</h2><p className="mt-2 max-w-2xl text-sm leading-6 text-ink-variant">{copy.subtitle}</p></div>
      <span className="rounded-full bg-green/10 px-3 py-1.5 text-xs font-bold text-green">{sessions.length}</span>
    </header>
    <div className="grid gap-3 p-4 sm:p-6 lg:grid-cols-2">{sessions.length === 0 ? <p className="col-span-full rounded-2xl border border-dashed border-line p-8 text-center text-sm text-ink-variant">{copy.empty}</p> : sessions.map((session) => {
      const lastSeen = new Date(session.lastUsedAtUtc ?? session.createdAtUtc); const stale = lastSeen.getTime() < staleCutoff;
      return <article key={session.id} className={`rounded-2xl border p-5 ${stale ? 'border-gold/55 bg-gold/[.04]' : 'border-line bg-surface'}`}>
        <div className="flex items-start gap-4"><span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-green/10 text-xl text-green"><i className="ri-device-line" /></span><div className="min-w-0 flex-1"><strong className="block truncate text-sm text-ink">{session.userName || session.userEmail}</strong><span className="block truncate text-xs text-ink-variant">{session.userEmail}</span><div className="mt-2 flex flex-wrap gap-2">{session.isCurrentUser && <span className="rounded-full bg-green/10 px-2 py-1 text-[9px] font-bold uppercase text-green">{copy.current}</span>}{stale && <span className="rounded-full bg-gold/20 px-2 py-1 text-[9px] font-bold uppercase text-green-deep">{copy.stale}</span>}</div></div></div>
        <dl className="mt-4 grid gap-2 border-t border-line pt-4 text-xs text-ink-variant sm:grid-cols-2"><div><dt className="font-bold text-ink">{session.deviceName}</dt><dd>{session.ipAddress || '—'}</dd></div><div><dt>{copy.lastSeen}</dt><dd className="font-semibold text-ink">{lastSeen.toLocaleString(locale)}</dd></div><div><dt>{copy.expires}</dt><dd>{new Date(session.expiresAtUtc).toLocaleDateString(locale)}</dd></div><div className="sm:text-right"><Button type="button" variant="tertiary" disabled={Boolean(busy)} onClick={() => void revoke(session.id)}><i className={busy === session.id ? 'ri-loader-4-line animate-spin' : 'ri-logout-box-r-line'} />{copy.revoke}</Button></div></dl>
      </article>;
    })}</div>
    {notice && <p role="status" className="border-t border-line px-6 py-4 text-sm font-semibold text-green">{notice}</p>}
  </section>;
}

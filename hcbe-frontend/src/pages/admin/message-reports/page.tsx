import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AdminPageHeader } from '../../../components/admin/AdminPageHeader';
import { Button, Field, inputClasses } from '../../../components/ui';
import { messagingApi } from '../../../lib/api/messaging';
import type { ConversationReportDto } from '../../../lib/api/types';

const AdminMessageReportsPage = () => {
  const { i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const [reports, setReports] = useState<ConversationReportDto[]>([]);
  const [status, setStatus] = useState('Open');
  const [notes, setNotes] = useState<Record<string, string>>({});
  const [suspend, setSuspend] = useState<Record<string, boolean>>({});
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const c = fr ? {
    title: 'Modération des échanges', subtitle: 'Traitez les signalements sans surveiller les conversations privées de manière systématique.',
    open: 'À examiner', all: 'Tous', resolved: 'Traités', dismissed: 'Classés sans suite', empty: 'Aucun signalement pour ce filtre.',
    reportedBy: 'Signalé par', conversation: 'Conversation', reason: 'Motif communiqué', notes: 'Notes de traitement',
    suspend: 'Suspendre cette conversation', resolve: 'Résoudre', dismiss: 'Classer sans suite', saved: 'Le signalement a été traité.', error: 'Impossible de traiter ce signalement.',
  } : {
    title: 'Message moderation', subtitle: 'Handle reports without routinely monitoring private conversations.',
    open: 'To review', all: 'All', resolved: 'Resolved', dismissed: 'Dismissed', empty: 'No reports match this filter.',
    reportedBy: 'Reported by', conversation: 'Conversation', reason: 'Reported reason', notes: 'Review notes',
    suspend: 'Suspend this conversation', resolve: 'Resolve', dismiss: 'Dismiss', saved: 'The report was handled.', error: 'This report could not be handled.',
  };

  const load = async () => {
    setLoading(true);
    const result = await messagingApi.adminGetReports(status || undefined);
    if (result.success && result.data) setReports(result.data);
    setLoading(false);
  };
  useEffect(() => { void load(); }, [status]);

  const handle = async (report: ConversationReportDto, next: 'Resolved' | 'Dismissed') => {
    setBusy(true); setNotice(null);
    const result = await messagingApi.adminResolveReport(report.id, next, notes[report.id] || '', suspend[report.id] || false);
    setNotice(result.success ? c.saved : result.message || c.error);
    if (result.success) await load();
    setBusy(false);
  };

  return <div className="space-y-6">
    <AdminPageHeader title={c.title} subtitle={c.subtitle} icon="ri-shield-user-line" count={reports.length} />
    {notice && <p className="rounded-xl border border-line bg-surface px-4 py-3 text-sm text-ink-variant">{notice}</p>}
    <section className="overflow-hidden rounded-[18px] border border-line bg-surface">
      <div className="flex flex-wrap gap-2 border-b border-line p-4">{[["Open",c.open],["Resolved",c.resolved],["Dismissed",c.dismissed],["",c.all]].map(([value,label]) => <button key={value} type="button" onClick={() => setStatus(value)} className={`rounded-full border px-4 py-2 text-[10px] font-bold uppercase tracking-[.12em] ${status === value ? 'border-green bg-green text-white' : 'border-line text-ink-variant hover:border-green/40'}`}>{label}</button>)}</div>
      {loading ? <div className="py-20 text-center text-ink-variant"><i className="ri-loader-4-line animate-spin text-2xl" /></div> : reports.length === 0 ? <p className="p-10 text-center text-sm text-ink-variant">{c.empty}</p> : <div className="divide-y divide-line">{reports.map((report) => <article key={report.id} className="p-5 sm:p-6"><div className="flex flex-wrap items-start justify-between gap-4"><div><div className="flex flex-wrap items-center gap-2"><h2 className="font-display text-xl font-bold text-green-deep">{report.memberOneName} <span className="text-gold">↔</span> {report.memberTwoName}</h2><span className={`rounded-full border px-2.5 py-1 text-[9px] font-bold uppercase tracking-[.12em] ${report.status === 'Open' ? 'border-gold/40 bg-gold/10 text-green-deep' : 'border-line bg-canvas text-ink-variant'}`}>{report.status}</span></div><p className="mt-1 text-xs text-ink-variant">{c.reportedBy}: {report.reporterName} · {new Date(report.createdAt).toLocaleString(fr ? 'fr-CA' : 'en-CA')}</p></div><span className="flex h-10 w-10 items-center justify-center rounded-full bg-red-link/5 text-red-link"><i className="ri-flag-2-line" /></span></div><div className="mt-5 rounded-xl border-l-2 border-red-link bg-canvas px-4 py-3"><p className="text-[9px] font-bold uppercase tracking-[.14em] text-red-link">{c.reason}</p><p className="mt-2 text-sm leading-6 text-ink">{report.reason}</p></div>{report.status === 'Open' ? <div className="mt-5 grid items-end gap-4 lg:grid-cols-[1fr_auto]"><div><Field label={c.notes} htmlFor={`report-note-${report.id}`}><textarea id={`report-note-${report.id}`} rows={3} className={inputClasses} value={notes[report.id] || ''} onChange={(e) => setNotes({ ...notes, [report.id]: e.target.value })} /></Field><label className="mt-3 flex items-center gap-2 text-sm text-ink-variant"><input type="checkbox" className="h-4 w-4 accent-red-link" checked={suspend[report.id] || false} onChange={(e) => setSuspend({ ...suspend, [report.id]: e.target.checked })} />{c.suspend}</label></div><div className="flex flex-wrap gap-2"><Button type="button" variant="primary" disabled={busy} onClick={() => void handle(report, 'Resolved')}>{c.resolve}</Button><Button type="button" variant="tertiary" disabled={busy} onClick={() => void handle(report, 'Dismissed')}>{c.dismiss}</Button></div></div> : report.adminNotes && <p className="mt-4 text-sm text-ink-variant"><strong>{c.notes}:</strong> {report.adminNotes}</p>}</article>)}</div>}
    </section>
  </div>;
};

export default AdminMessageReportsPage;

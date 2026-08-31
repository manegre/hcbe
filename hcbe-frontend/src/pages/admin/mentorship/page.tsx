import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AdminPageHeader } from '../../../components/admin/AdminPageHeader';
import { Button, Field, inputClasses } from '../../../components/ui';
import { communityApi } from '../../../lib/api/community';
import type { MentorshipApplicationDto, MentorshipMatchDto } from '../../../lib/api/types';

const tones: Record<string, string> = {
  Pending: 'border-gold/40 bg-gold/10 text-green-deep', Approved: 'border-green/25 bg-green/10 text-green',
  Active: 'border-green/25 bg-green/10 text-green', Proposed: 'border-gold/40 bg-gold/10 text-green-deep',
  Rejected: 'border-red-link/25 bg-red-link/5 text-red-link', Declined: 'border-red-link/25 bg-red-link/5 text-red-link',
  Completed: 'border-line bg-canvas text-ink-variant', Cancelled: 'border-line bg-canvas text-ink-variant',
};

const Badge = ({ status }: { status: string }) => <span className={`rounded-full border px-2.5 py-1 text-[9px] font-bold uppercase tracking-[0.14em] ${tones[status] || tones.Completed}`}>{status}</span>;

const AdminMentorshipPage = () => {
  const { i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const [applications, setApplications] = useState<MentorshipApplicationDto[]>([]);
  const [matches, setMatches] = useState<MentorshipMatchDto[]>([]);
  const [status, setStatus] = useState('');
  const [role, setRole] = useState('');
  const [search, setSearch] = useState('');
  const [mentorId, setMentorId] = useState('');
  const [menteeId, setMenteeId] = useState('');
  const [matchNotes, setMatchNotes] = useState('');
  const [reviewNotes, setReviewNotes] = useState<Record<string, string>>({});
  const [notice, setNotice] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const c = fr ? {
    title: 'Mentorat et jumelages', subtitle: 'Examinez les candidatures et accompagnez chaque mise en relation avec discernement.',
    pending: 'À examiner', approved: 'Profils approuvés', matches: 'Jumelages', applications: 'Candidatures',
    allStatuses: 'Tous les statuts', allRoles: 'Tous les rôles', search: 'Rechercher un membre…',
    mentor: 'Mentor', mentee: 'Mentoré(e)', approve: 'Approuver', reject: 'Refuser', note: 'Note du comité',
    empty: 'Aucune candidature ne correspond à ces filtres.', createMatch: 'Composer un jumelage',
    chooseMentor: 'Choisir un mentor', chooseMentee: 'Choisir un mentoré', rationale: 'Raison du jumelage / consignes au binôme', propose: 'Proposer le jumelage',
    noApproved: 'Approuvez d’abord au moins un mentor et un mentoré.', acceptances: 'Accord des participants', complete: 'Marquer terminé', cancel: 'Annuler le jumelage',
    saved: 'La décision a été enregistrée.', proposed: 'Le jumelage a été proposé aux deux membres.', error: 'Impossible de terminer cette opération.',
  } : {
    title: 'Mentorship and matching', subtitle: 'Review applications and guide each introduction with care.',
    pending: 'To review', approved: 'Approved profiles', matches: 'Matches', applications: 'Applications',
    allStatuses: 'All statuses', allRoles: 'All roles', search: 'Search for a member…',
    mentor: 'Mentor', mentee: 'Mentee', approve: 'Approve', reject: 'Reject', note: 'Committee note',
    empty: 'No applications match these filters.', createMatch: 'Create a match',
    chooseMentor: 'Choose a mentor', chooseMentee: 'Choose a mentee', rationale: 'Match rationale / guidance for participants', propose: 'Propose match',
    noApproved: 'First approve at least one mentor and one mentee.', acceptances: 'Participant approvals', complete: 'Mark completed', cancel: 'Cancel match',
    saved: 'The decision was saved.', proposed: 'The match was proposed to both members.', error: 'This operation could not be completed.',
  };

  const load = async () => {
    setLoading(true);
    const [applicationResult, matchResult] = await Promise.all([
      communityApi.adminGetApplications(),
      communityApi.adminGetMatches(),
    ]);
    if (applicationResult.success && applicationResult.data) setApplications(applicationResult.data);
    if (matchResult.success && matchResult.data) setMatches(matchResult.data);
    setLoading(false);
  };

  useEffect(() => { void load(); }, []);

  const run = async (action: () => Promise<{ success: boolean; message?: string }>, message: string) => {
    setBusy(true); setNotice(null);
    const result = await action();
    setNotice(result.success ? message : result.message || c.error);
    if (result.success) await load();
    setBusy(false);
    return result.success;
  };

  const approvedMentors = useMemo(() => applications.filter((item) => item.status === 'Approved' && item.role === 'Mentor'), [applications]);
  const approvedMentees = useMemo(() => applications.filter((item) => item.status === 'Approved' && item.role === 'Mentee'), [applications]);
  const filteredApplications = useMemo(() => {
    const term = search.trim().toLowerCase();
    return applications.filter((item) =>
      (!status || item.status === status) &&
      (!role || item.role === role) &&
      (!term || [item.memberName, item.memberEmail, item.expertise].some((value) => value?.toLowerCase().includes(term))));
  }, [applications, role, search, status]);
  const pendingCount = applications.filter((item) => item.status === 'Pending').length;

  const submitMatch = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!mentorId || !menteeId) return;
    if (await run(() => communityApi.adminCreateMatch(mentorId, menteeId, matchNotes), c.proposed)) {
      setMentorId(''); setMenteeId(''); setMatchNotes('');
    }
  };

  return (
    <div className="space-y-6">
      <AdminPageHeader title={c.title} subtitle={c.subtitle} icon="ri-user-heart-line" count={applications.length} />
      {notice && <div className="rounded-xl border border-line bg-surface px-4 py-3 text-sm text-ink-variant shadow-sm">{notice}</div>}

      <section className="grid gap-3 sm:grid-cols-3">
        <div className="rounded-2xl border border-line bg-surface p-4"><p className="text-[9px] font-bold uppercase tracking-[.16em] text-ink-variant">{c.pending}</p><p className="mt-2 font-display text-3xl font-bold text-green-deep">{pendingCount}</p></div>
        <div className="rounded-2xl border border-line bg-surface p-4"><p className="text-[9px] font-bold uppercase tracking-[.16em] text-ink-variant">{c.approved}</p><p className="mt-2 font-display text-3xl font-bold text-green-deep">{approvedMentors.length + approvedMentees.length}</p></div>
        <div className="rounded-2xl border border-line bg-surface p-4"><p className="text-[9px] font-bold uppercase tracking-[.16em] text-ink-variant">{c.matches}</p><p className="mt-2 font-display text-3xl font-bold text-green-deep">{matches.length}</p></div>
      </section>

      <div className="grid items-start gap-6 2xl:grid-cols-[minmax(0,1.35fr)_minmax(360px,.65fr)]">
        <section className="overflow-hidden rounded-[18px] border border-line bg-surface">
          <div className="border-b border-line px-5 py-4"><div className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">{c.applications}</p><h2 className="mt-1 font-display text-2xl font-bold text-green-deep">{c.pending}</h2></div><div className="grid w-full gap-2 sm:w-auto sm:grid-cols-3"><input value={search} onChange={(e) => setSearch(e.target.value)} className={inputClasses} placeholder={c.search} /><select value={role} onChange={(e) => setRole(e.target.value)} className={inputClasses}><option value="">{c.allRoles}</option><option value="Mentor">{c.mentor}</option><option value="Mentee">{c.mentee}</option></select><select value={status} onChange={(e) => setStatus(e.target.value)} className={inputClasses}><option value="">{c.allStatuses}</option><option value="Pending">Pending</option><option value="Approved">Approved</option><option value="Rejected">Rejected</option><option value="Withdrawn">Withdrawn</option></select></div></div></div>
          <div className="divide-y divide-line">{loading ? <div className="py-16 text-center text-ink-variant"><i className="ri-loader-4-line animate-spin text-2xl" /></div> : filteredApplications.length === 0 ? <p className="p-8 text-center text-sm text-ink-variant">{c.empty}</p> : filteredApplications.map((item) => <article key={item.id} className="p-5"><div className="flex flex-wrap items-start justify-between gap-3"><div><div className="flex flex-wrap items-center gap-2"><h3 className="font-display text-xl font-bold text-green-deep">{item.memberName}</h3><Badge status={item.status} /></div><p className="mt-1 text-xs text-ink-variant">{item.memberEmail} · {item.role === 'Mentor' ? c.mentor : c.mentee} · {item.preferredLanguage.toUpperCase()}</p></div><span className="rounded-full bg-canvas px-3 py-1 text-[10px] font-semibold text-ink-variant">{new Date(item.createdAt).toLocaleDateString(fr ? 'fr-CA' : 'en-CA')}</span></div><div className="mt-4 grid gap-4 md:grid-cols-3"><div><p className="text-[9px] font-bold uppercase tracking-[.14em] text-ink-variant">Profil</p><p className="mt-1 text-sm leading-5 text-ink-variant">{item.professionalSummary}</p></div><div><p className="text-[9px] font-bold uppercase tracking-[.14em] text-ink-variant">{c.note.replace('Note du comité', 'Expertise').replace('Committee note', 'Expertise')}</p><p className="mt-1 text-sm leading-5 text-ink-variant">{item.expertise}</p></div><div><p className="text-[9px] font-bold uppercase tracking-[.14em] text-ink-variant">{fr ? 'Objectifs' : 'Goals'}</p><p className="mt-1 text-sm leading-5 text-ink-variant">{item.objectives}</p></div></div>{item.status === 'Pending' && <div className="mt-4 flex flex-col gap-3 border-t border-line pt-4 sm:flex-row sm:items-end"><div className="flex-1"><Field label={c.note} htmlFor={`note-${item.id}`}><input id={`note-${item.id}`} className={inputClasses} value={reviewNotes[item.id] || ''} onChange={(e) => setReviewNotes({ ...reviewNotes, [item.id]: e.target.value })} /></Field></div><div className="flex gap-2"><Button type="button" variant="primary" disabled={busy} onClick={() => void run(() => communityApi.adminReview(item.id, 'Approved', reviewNotes[item.id]), c.saved)}>{c.approve}</Button><Button type="button" variant="tertiary" disabled={busy} onClick={() => void run(() => communityApi.adminReview(item.id, 'Rejected', reviewNotes[item.id]), c.saved)}>{c.reject}</Button></div></div>}{item.committeeNotes && item.status !== 'Pending' && <p className="mt-4 border-l-2 border-gold pl-3 text-xs text-ink-variant">{item.committeeNotes}</p>}</article>)}</div>
        </section>

        <div className="space-y-6">
          <form onSubmit={submitMatch} className="rounded-[18px] border border-line bg-green-deep p-5 text-white shadow-[0_18px_45px_rgba(0,59,27,.15)] [&_label]:!text-green-dim"><p className="text-[9px] font-bold uppercase tracking-[.18em] text-gold">{c.matches}</p><h2 className="mt-1 font-display text-2xl font-bold !text-white">{c.createMatch}</h2>{approvedMentors.length === 0 || approvedMentees.length === 0 ? <p className="mt-4 rounded-xl border border-white/10 bg-white/5 p-4 text-sm leading-5 text-green-dim">{c.noApproved}</p> : <div className="mt-5 space-y-4"><Field label={c.chooseMentor} htmlFor="match-mentor"><select id="match-mentor" required className={inputClasses} value={mentorId} onChange={(e) => setMentorId(e.target.value)}><option value="">—</option>{approvedMentors.map((item) => <option key={item.id} value={item.id}>{item.memberName}</option>)}</select></Field><Field label={c.chooseMentee} htmlFor="match-mentee"><select id="match-mentee" required className={inputClasses} value={menteeId} onChange={(e) => setMenteeId(e.target.value)}><option value="">—</option>{approvedMentees.map((item) => <option key={item.id} value={item.id}>{item.memberName}</option>)}</select></Field><Field label={c.rationale} htmlFor="match-notes"><textarea id="match-notes" rows={3} className={inputClasses} value={matchNotes} onChange={(e) => setMatchNotes(e.target.value)} /></Field><Button type="submit" variant="secondary" className="w-full" disabled={busy}>{c.propose}</Button></div>}</form>

          <section className="rounded-[18px] border border-line bg-surface p-5"><h2 className="font-display text-xl font-bold text-green-deep">{c.matches}</h2><div className="mt-4 space-y-3">{matches.length === 0 ? <p className="rounded-xl border border-dashed border-line p-5 text-sm text-ink-variant">—</p> : matches.map((match) => <article key={match.id} className="rounded-xl border border-line p-4"><div className="flex items-start justify-between gap-3"><div><h3 className="font-semibold text-green-deep">{match.mentorName}</h3><p className="text-xs text-ink-variant"><i className="ri-arrow-down-line" /> {match.menteeName}</p></div><Badge status={match.status} /></div><p className="mt-3 text-[10px] font-bold uppercase tracking-[.12em] text-ink-variant">{c.acceptances}</p><div className="mt-2 flex gap-2 text-xs"><span className={match.mentorAccepted ? 'text-green' : 'text-ink-variant'}>Mentor {match.mentorAccepted ? '✓' : '○'}</span><span className={match.menteeAccepted ? 'text-green' : 'text-ink-variant'}>{c.mentee} {match.menteeAccepted ? '✓' : '○'}</span></div>{['Proposed', 'Active'].includes(match.status) && <div className="mt-4 flex flex-wrap gap-2">{match.status === 'Active' && <button type="button" disabled={busy} onClick={() => void run(() => communityApi.adminUpdateMatch(match.id, 'Completed'), c.saved)} className="text-[9px] font-bold uppercase tracking-[.12em] text-green">{c.complete}</button>}<button type="button" disabled={busy} onClick={() => void run(() => communityApi.adminUpdateMatch(match.id, 'Cancelled'), c.saved)} className="text-[9px] font-bold uppercase tracking-[.12em] text-red-link">{c.cancel}</button></div>}</article>)}</div></section>
        </div>
      </div>
    </div>
  );
};

export default AdminMentorshipPage;

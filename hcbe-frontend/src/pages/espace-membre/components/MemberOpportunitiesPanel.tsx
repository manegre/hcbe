import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Field, RichTextContent, plainTextFromRichText, inputClasses } from '../../../components/ui';
import { engagementApi } from '../../../lib/api/engagement';
import { opportunitiesApi } from '../../../lib/api/opportunities';
import type { Opportunity, OpportunityApplication, OpportunityMatch } from '../../../lib/api/types';
import { localized } from '../../../lib/i18n/localized';

type ApplyDraft = { message: string; experience: string; availability: string; file: File | null };
const blankApply: ApplyDraft = { message: '', experience: '', availability: '', file: null };
const types = ['All', 'Volunteer', 'Job', 'Training', 'Business'] as const;

export default function MemberOpportunitiesPanel() {
  const { i18n } = useTranslation();
  const en = i18n.language.startsWith('en');
  const locale = en ? 'en-CA' : 'fr-CA';
  const copy = en ? {
    eyebrow: 'Community opportunities', title: 'Put your talent in motion.', subtitle: 'Jobs, volunteering, training and business opportunities selected for the community.',
    matches: 'Recommended for you', applications: 'My applications', empty: 'No opportunity matches this filter yet.', apply: 'Apply', applied: 'Application sent', external: 'View opportunity', save: 'Save', unsave: 'Remove saved item',
    fit: 'match', why: 'Why this matches', skills: 'Skills', region: 'Region', availability: 'Availability', remote: 'Remote', deadline: 'Apply by', starts: 'Starts', commitment: 'Commitment', requirements: 'What we are looking for', benefits: 'What you gain',
    message: 'Why are you interested?', experience: 'Relevant experience', availabilityField: 'Your availability', document: 'Résumé or supporting document', send: 'Send application', cancel: 'Cancel', required: 'Please provide at least 20 characters.',
    pending: 'Under review', reviewed: 'Reviewed', accepted: 'Accepted', declined: 'Not selected', documents: 'Documents', hours: 'Volunteer hours', addHours: 'Record hours', date: 'Activity date', duration: 'Hours', activity: 'Activity completed', submitHours: 'Submit for approval', approved: 'approved', noHours: 'No hours recorded yet.', certificate: 'Download participation certificate', success: 'Saved successfully.',
  } : {
    eyebrow: 'Occasions communautaires', title: 'Mettez votre talent en mouvement.', subtitle: 'Emplois, bénévolat, formations et occasions d’affaires sélectionnés pour la communauté.',
    matches: 'Recommandées pour vous', applications: 'Mes candidatures', empty: 'Aucune occasion ne correspond encore à ce filtre.', apply: 'Postuler', applied: 'Candidature envoyée', external: 'Voir l’occasion', save: 'Enregistrer', unsave: 'Retirer des favoris',
    fit: 'compatible', why: 'Pourquoi cette occasion vous correspond', skills: 'Compétences', region: 'Région', availability: 'Disponibilité', remote: 'À distance', deadline: 'Postuler avant le', starts: 'Début', commitment: 'Engagement', requirements: 'Profil recherché', benefits: 'Ce que vous y gagnez',
    message: 'Pourquoi cette occasion vous intéresse-t-elle?', experience: 'Expérience pertinente', availabilityField: 'Vos disponibilités', document: 'CV ou document justificatif', send: 'Envoyer ma candidature', cancel: 'Annuler', required: 'Veuillez fournir au moins 20 caractères.',
    pending: 'En évaluation', reviewed: 'Évaluée', accepted: 'Acceptée', declined: 'Non retenue', documents: 'Documents', hours: 'Heures de bénévolat', addHours: 'Déclarer des heures', date: 'Date de l’activité', duration: 'Heures', activity: 'Activité réalisée', submitHours: 'Soumettre pour validation', approved: 'approuvées', noHours: 'Aucune heure déclarée.', certificate: 'Télécharger l’attestation de participation', success: 'Enregistré avec succès.',
  };
  const [matches, setMatches] = useState<OpportunityMatch[]>([]);
  const [mine, setMine] = useState<OpportunityApplication[]>([]);
  const [savedIds, setSavedIds] = useState<Set<string>>(new Set());
  const [filter, setFilter] = useState<(typeof types)[number]>('All');
  const [selected, setSelected] = useState<Opportunity | null>(null);
  const [draft, setDraft] = useState<ApplyDraft>(blankApply);
  const [hoursFor, setHoursFor] = useState<string | null>(null);
  const [hours, setHours] = useState({ activityDate: new Date().toISOString().slice(0, 10), hours: '1', description: '' });
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('');

  const load = async () => {
    const [suggestions, applications, saved] = await Promise.all([opportunitiesApi.getMatched(), opportunitiesApi.getMine(), engagementApi.getSaved()]);
    setMatches(suggestions.data ?? []); setMine(applications.data ?? []);
    setSavedIds(new Set((saved.data ?? []).filter((item) => item.entityType === 'Opportunity').map((item) => item.entityId)));
  };
  useEffect(() => { void load(); }, []);
  const applied = useMemo(() => new Set(mine.map((item) => item.opportunityId)), [mine]);
  const visible = filter === 'All' ? matches : matches.filter((item) => item.opportunity.type === filter);
  const typeLabel = (type: Opportunity['type']) => ({ Volunteer: en ? 'Volunteering' : 'Bénévolat', Job: en ? 'Job' : 'Emploi', Training: 'Formation', Business: en ? 'Business' : 'Affaires', Community: en ? 'Community' : 'Communauté' })[type];
  const reasonLabel = (reason: string) => ({ skills: copy.skills, region: copy.region, availability: copy.availability, remote: copy.remote }[reason] ?? reason);
  const statusLabel = (status: OpportunityApplication['status']) => ({ Submitted: copy.pending, Reviewed: copy.reviewed, Accepted: copy.accepted, Declined: copy.declined })[status];

  const submit = async (event: React.FormEvent) => {
    event.preventDefault(); if (!selected || draft.message.trim().length < 20) { setNotice(copy.required); return; }
    setBusy(true); setNotice('');
    try {
      const response = await opportunitiesApi.apply(selected.id, { message: draft.message, experience: draft.experience || undefined, availability: draft.availability || undefined });
      if (response.success && response.data) {
        if (draft.file) await opportunitiesApi.uploadDocument(response.data.id, draft.file);
        setSelected(null); setDraft(blankApply); setNotice(copy.success); await load();
      } else setNotice(response.message || copy.required);
    } catch (error) { setNotice(error instanceof Error ? error.message : copy.required); } finally { setBusy(false); }
  };
  const toggleSaved = async (id: string) => { if (savedIds.has(id)) await engagementApi.removeSaved('Opportunity', id); else await engagementApi.save('Opportunity', id); await load(); };
  const submitHours = async (event: React.FormEvent, applicationId: string) => {
    event.preventDefault(); setBusy(true);
    try { await opportunitiesApi.addHours(applicationId, { activityDate: new Date(`${hours.activityDate}T12:00:00Z`).toISOString(), hours: Number(hours.hours), description: hours.description }); setHoursFor(null); setHours({ activityDate: new Date().toISOString().slice(0, 10), hours: '1', description: '' }); await load(); } finally { setBusy(false); }
  };
  const downloadCertificate = async (applicationId: string) => {
    const result = await opportunitiesApi.downloadCertificate(applicationId); const url = URL.createObjectURL(result.blob);
    const link = document.createElement('a'); link.href = url; link.download = result.fileName ?? `attestation-hcbe-${applicationId}.pdf`; link.click(); URL.revokeObjectURL(url);
  };
  const downloadDocument = async (applicationId: string, documentId: string, fallbackName: string) => {
    const result = await opportunitiesApi.downloadDocument(applicationId, documentId); const url = URL.createObjectURL(result.blob);
    const link = document.createElement('a'); link.href = url; link.download = result.fileName ?? fallbackName; link.click(); URL.revokeObjectURL(url);
  };

  return <div className="space-y-8">
    <section className="relative overflow-hidden rounded-[28px] bg-green-deep px-6 py-8 text-white sm:px-9 lg:grid lg:grid-cols-[1fr_auto] lg:items-end lg:gap-8">
      <div className="absolute -right-14 -top-20 h-56 w-56 rounded-full border-[38px] border-gold/[.08]" />
      <div className="relative"><p className="text-[10px] font-bold uppercase tracking-[.2em] text-gold">{copy.eyebrow}</p><h2 className="mt-3 max-w-2xl font-display text-3xl font-bold text-white sm:text-5xl">{copy.title}</h2><p className="mt-3 max-w-2xl text-sm leading-6 text-green-dim">{copy.subtitle}</p></div>
      <div className="relative mt-6 rounded-2xl border border-white/15 bg-white/[.06] px-5 py-4 lg:mt-0"><strong className="font-display text-3xl text-gold">{matches.filter((x) => x.score >= 60).length}</strong><p className="mt-1 text-[9px] font-bold uppercase tracking-[.16em] text-green-dim">{copy.matches}</p></div>
    </section>

    {notice && <p role="status" className="rounded-xl border border-gold/40 bg-gold/10 px-4 py-3 text-sm text-green-deep">{notice}</p>}
    <section aria-labelledby="opportunity-list-title">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between"><div><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">01 · {copy.matches}</p><h3 id="opportunity-list-title" className="mt-1 font-display text-2xl font-bold text-green-deep">{visible.length} {en ? 'opportunities' : 'occasions'}</h3></div><div className="flex gap-2 overflow-x-auto pb-1">{types.map((type) => <button key={type} type="button" onClick={() => setFilter(type)} className={`min-h-10 whitespace-nowrap rounded-full border px-4 text-[10px] font-bold uppercase tracking-[.1em] transition ${filter === type ? 'border-green bg-green text-white' : 'border-line bg-surface text-ink-variant hover:border-green/40'}`}>{type === 'All' ? (en ? 'All' : 'Toutes') : typeLabel(type)}</button>)}</div></div>
      {visible.length === 0 ? <div className="mt-5 rounded-[24px] border border-dashed border-line bg-surface p-10 text-center text-sm text-ink-variant">{copy.empty}</div> : <div className="mt-5 grid gap-5 lg:grid-cols-2">{visible.map(({ opportunity: item, score, reasons }) => <article key={item.id} className="group flex min-w-0 flex-col overflow-hidden rounded-[24px] border border-line bg-surface shadow-[0_14px_40px_rgba(0,59,27,.06)] transition hover:-translate-y-0.5 hover:border-green/30">
        <div className="flex items-start justify-between gap-4 border-b border-line bg-canvas/45 p-5"><div className="min-w-0"><div className="flex flex-wrap items-center gap-2"><span className="rounded-full bg-gold/15 px-3 py-1 text-[9px] font-bold uppercase tracking-[.12em] text-green-deep">{typeLabel(item.type)}</span>{score >= 60 && <span className="rounded-full bg-green px-3 py-1 text-[9px] font-bold uppercase tracking-[.12em] text-white">{score}% {copy.fit}</span>}</div><h4 className="mt-4 font-display text-2xl font-bold leading-tight text-green-deep">{localized(item.title, item.titleEn, i18n.language)}</h4><p className="mt-2 text-xs font-semibold text-red-link">{item.organization}</p></div><button type="button" title={savedIds.has(item.id) ? copy.unsave : copy.save} aria-label={savedIds.has(item.id) ? copy.unsave : copy.save} onClick={() => void toggleSaved(item.id)} className={`grid h-10 w-10 shrink-0 place-items-center rounded-xl border ${savedIds.has(item.id) ? 'border-gold bg-gold text-green-deep' : 'border-line text-green'}`}><i className={savedIds.has(item.id) ? 'ri-bookmark-fill' : 'ri-bookmark-line'} /></button></div>
        <div className="flex flex-1 flex-col p-5"><p className="line-clamp-4 text-sm leading-6 text-ink-variant">{plainTextFromRichText(localized(item.description, item.descriptionEn, i18n.language))}</p>{reasons.length > 0 && <div className="mt-4"><p className="text-[9px] font-bold uppercase tracking-[.14em] text-ink-muted">{copy.why}</p><div className="mt-2 flex flex-wrap gap-2">{reasons.map((reason) => <span key={reason} className="rounded-lg bg-green/5 px-2.5 py-1.5 text-xs font-semibold text-green"><i className="ri-sparkling-line mr-1 text-gold-dark" />{reasonLabel(reason)}</span>)}</div></div>}
        <dl className="mt-5 grid gap-3 border-t border-line pt-4 text-xs sm:grid-cols-2"><div><dt className="font-bold uppercase tracking-wider text-ink-muted">{copy.region}</dt><dd className="mt-1 text-ink-variant">{item.isRemote ? copy.remote : item.location || item.region || 'Canada'}</dd></div>{item.commitment && <div><dt className="font-bold uppercase tracking-wider text-ink-muted">{copy.commitment}</dt><dd className="mt-1 text-ink-variant">{item.commitment}</dd></div>}{item.deadlineUtc && <div><dt className="font-bold uppercase tracking-wider text-ink-muted">{copy.deadline}</dt><dd className="mt-1 text-ink-variant">{new Date(item.deadlineUtc).toLocaleDateString(locale)}</dd></div>}{item.startsAtUtc && <div><dt className="font-bold uppercase tracking-wider text-ink-muted">{copy.starts}</dt><dd className="mt-1 text-ink-variant">{new Date(item.startsAtUtc).toLocaleDateString(locale)}</dd></div>}</dl>
        {(item.requirements || item.benefits) && <details className="mt-4 rounded-xl bg-canvas/60 p-3 text-sm"><summary className="cursor-pointer font-bold text-green-deep">{en ? 'Full details' : 'Tous les détails'}</summary>{item.requirements && <div className="mt-3"><strong className="text-green-deep">{copy.requirements}</strong><RichTextContent value={localized(item.requirements, item.requirementsEn, i18n.language)} className="mt-1 !text-sm !leading-6" /></div>}{item.benefits && <div className="mt-3"><strong className="text-green-deep">{copy.benefits}</strong><RichTextContent value={localized(item.benefits, item.benefitsEn, i18n.language)} className="mt-1 !text-sm !leading-6" /></div>}</details>}
        {item.applyUrl ? <a href={item.applyUrl} target="_blank" rel="noreferrer" className="mt-5 inline-flex min-h-11 items-center justify-center rounded-xl bg-green px-4 text-[10px] font-bold uppercase tracking-[.12em] text-white">{copy.external}<i className="ri-arrow-right-up-line ml-2" /></a> : <Button type="button" variant="secondary" className="mt-5" disabled={applied.has(item.id)} onClick={() => setSelected(item)}>{applied.has(item.id) ? copy.applied : copy.apply}</Button>}</div>
      </article>)}</div>}
    </section>

    <section aria-labelledby="my-applications-title"><div className="border-b border-line pb-3"><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">02 · {copy.applications}</p><h3 id="my-applications-title" className="mt-1 font-display text-2xl font-bold text-green-deep">{copy.applications}</h3></div><div className="mt-5 grid gap-4 xl:grid-cols-2">{mine.map((item) => <article key={item.id} className="rounded-[22px] border border-line bg-surface p-5"><div className="flex flex-wrap items-start justify-between gap-3"><div><h4 className="font-display text-xl font-bold text-green-deep">{localized(item.opportunityTitle, item.opportunityTitleEn, i18n.language)}</h4><p className="mt-1 text-xs text-ink-muted">{new Date(item.createdAt).toLocaleDateString(locale)}</p></div><span className={`rounded-full px-3 py-1 text-[9px] font-bold uppercase tracking-wider ${item.status === 'Accepted' ? 'bg-green/10 text-green' : item.status === 'Declined' ? 'bg-error/10 text-error' : 'bg-gold/15 text-green-deep'}`}>{statusLabel(item.status)}</span></div>
        {item.documents.length > 0 && <div className="mt-4"><p className="text-[9px] font-bold uppercase tracking-wider text-ink-muted">{copy.documents}</p><div className="mt-2 flex flex-wrap gap-2">{item.documents.map((document) => <button type="button" key={document.id} onClick={() => void downloadDocument(item.id, document.id, document.fileName)} className="rounded-lg border border-line px-3 py-2 text-xs font-semibold text-green"><i className="ri-file-text-line mr-1" />{document.fileName}</button>)}</div></div>}
        {item.status === 'Accepted' && item.opportunityType === 'Volunteer' && <div className="mt-5 border-t border-line pt-4"><div className="flex items-center justify-between gap-3"><div><p className="text-[9px] font-bold uppercase tracking-wider text-ink-muted">{copy.hours}</p><p className="mt-1 font-display text-2xl font-bold text-green-deep">{item.approvedVolunteerHours} h <span className="font-sans text-xs font-normal text-ink-muted">{copy.approved}</span></p></div><button type="button" onClick={() => setHoursFor(hoursFor === item.id ? null : item.id)} className="rounded-xl border border-green px-3 py-2 text-[9px] font-bold uppercase tracking-wider text-green">{copy.addHours}</button></div>{item.volunteerTimeEntries.length > 0 ? <div className="mt-3 space-y-2">{item.volunteerTimeEntries.map((entry) => <div key={entry.id} className="flex items-center justify-between rounded-lg bg-canvas/60 px-3 py-2 text-xs"><span>{new Date(entry.activityDate).toLocaleDateString(locale)} · {entry.description}</span><strong>{entry.hours} h · {entry.status}</strong></div>)}</div> : <p className="mt-3 text-xs text-ink-muted">{copy.noHours}</p>}
          {hoursFor === item.id && <form onSubmit={(event) => void submitHours(event, item.id)} className="mt-4 grid gap-3 rounded-xl bg-canvas/60 p-4 sm:grid-cols-2"><Field label={copy.date} htmlFor={`hours-date-${item.id}`}><input id={`hours-date-${item.id}`} required type="date" max={new Date().toISOString().slice(0, 10)} className={inputClasses} value={hours.activityDate} onChange={(event) => setHours({ ...hours, activityDate: event.target.value })} /></Field><Field label={copy.duration} htmlFor={`hours-count-${item.id}`}><input id={`hours-count-${item.id}`} required type="number" min="0.25" max="24" step="0.25" className={inputClasses} value={hours.hours} onChange={(event) => setHours({ ...hours, hours: event.target.value })} /></Field><div className="sm:col-span-2"><Field label={copy.activity} htmlFor={`hours-description-${item.id}`}><textarea id={`hours-description-${item.id}`} required minLength={5} rows={3} className={inputClasses} value={hours.description} onChange={(event) => setHours({ ...hours, description: event.target.value })} /></Field></div><Button type="submit" variant="secondary" disabled={busy}>{copy.submitHours}</Button></form>}
        </div>}
        {item.certificate && <button type="button" onClick={() => void downloadCertificate(item.id)} className="mt-5 flex w-full items-center justify-between rounded-xl bg-green-deep px-4 py-3 text-left text-xs font-bold uppercase tracking-wider text-white"><span><i className="ri-award-line mr-2 text-gold" />{copy.certificate}</span><i className="ri-download-2-line" /></button>}
      </article>)}</div></section>

    {selected && <div className="fixed inset-0 z-[90] grid place-items-center overflow-y-auto bg-green-deep/85 p-4"><form onSubmit={submit} className="my-6 w-full max-w-2xl rounded-[26px] bg-surface p-6 shadow-2xl sm:p-8"><div className="flex items-start justify-between gap-5"><div><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">{copy.apply}</p><h3 className="mt-2 font-display text-2xl font-bold text-green-deep">{localized(selected.title, selected.titleEn, i18n.language)}</h3></div><button type="button" onClick={() => setSelected(null)} aria-label={copy.cancel} className="grid h-10 w-10 place-items-center rounded-full border border-line text-green"><i className="ri-close-line text-xl" /></button></div><div className="mt-6 space-y-4"><Field label={copy.message} htmlFor="opportunity-message"><textarea id="opportunity-message" autoFocus required minLength={20} rows={5} className={inputClasses} value={draft.message} onChange={(e) => setDraft({ ...draft, message: e.target.value })} /></Field><div className="grid gap-4 sm:grid-cols-2"><Field label={copy.experience} htmlFor="opportunity-experience"><textarea id="opportunity-experience" rows={4} className={inputClasses} value={draft.experience} onChange={(e) => setDraft({ ...draft, experience: e.target.value })} /></Field><Field label={copy.availabilityField} htmlFor="opportunity-availability"><textarea id="opportunity-availability" rows={4} className={inputClasses} value={draft.availability} onChange={(e) => setDraft({ ...draft, availability: e.target.value })} /></Field></div><Field label={copy.document} htmlFor="opportunity-file"><input id="opportunity-file" type="file" accept=".pdf,.doc,.docx,.txt,.png,.jpg,.jpeg" className={`${inputClasses} file:mr-3 file:rounded-lg file:border-0 file:bg-green/10 file:px-3 file:py-2 file:text-xs file:font-bold file:text-green`} onChange={(event) => setDraft({ ...draft, file: event.target.files?.[0] ?? null })} /></Field></div><div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end"><Button type="button" variant="tertiary" onClick={() => setSelected(null)}>{copy.cancel}</Button><Button type="submit" variant="secondary" disabled={busy}>{copy.send}</Button></div></form></div>}
  </div>;
}

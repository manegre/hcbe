import { FormEvent, useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import Navbar from '../../../../components/feature/Navbar';
import Footer from '../../../../components/feature/Footer';
import { Button, EmptyState, PageHeader } from '../../../../components/ui';
import { consultationsApi } from '../../../../lib/api/consultations';
import type { Consultation } from '../../../../lib/api/types';
import { localized } from '../../../../lib/i18n/localized';

const ConsultationDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const [item, setItem] = useState<Consultation | null>(null);
  const [selected, setSelected] = useState('');
  const [comment, setComment] = useState('');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);

  const load = useCallback(async () => {
    if (!id) return;
    try {
      const response = await consultationsApi.getConsultationById(id);
      if (response.success && response.data) {
        setItem(response.data);
        setSelected(response.data.selectedOptionId || '');
      }
    } catch {
      setItem(null);
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { void load(); }, [load]);

  const vote = async (event: FormEvent) => {
    event.preventDefault();
    if (!id || !selected) return;
    setBusy(true); setNotice(null);
    try {
      const response = await consultationsApi.vote(id, selected);
      if (!response.success) throw new Error(response.message);
      setNotice({ tone: 'success', text: t('public.engagement.consultations.detail.voteSuccess') });
      await load();
    } catch (error) {
      setNotice({ tone: 'error', text: error instanceof Error ? error.message : t('public.engagement.consultations.detail.signIn') });
    } finally { setBusy(false); }
  };

  const addComment = async (event: FormEvent) => {
    event.preventDefault();
    if (!id || comment.trim().length < 3) return;
    setBusy(true); setNotice(null);
    try {
      const response = await consultationsApi.comment(id, comment.trim());
      if (!response.success) throw new Error(response.message);
      setComment('');
      setNotice({ tone: 'success', text: t('public.engagement.consultations.detail.commentSuccess') });
      await load();
    } catch (error) {
      setNotice({ tone: 'error', text: error instanceof Error ? error.message : t('public.engagement.consultations.detail.signIn') });
    } finally { setBusy(false); }
  };

  if (loading) return <div className="min-h-screen bg-background"><Navbar /><div className="container-page py-24"><div className="h-10 w-10 animate-spin rounded-full border-2 border-line border-t-green" /></div><Footer /></div>;
  if (!item) return <div className="min-h-screen bg-background"><Navbar /><div className="container-page py-20"><EmptyState tone="error" title={t('public.engagement.consultations.detail.notFound')} action={<Button to="/engagement/consultations" variant="secondary">{t('public.engagement.consultations.detail.back')}</Button>} /></div><Footer /></div>;

  const governance = item.governance;
  const title = localized(item.title, item.titleEn, i18n.language);
  const description = localized(item.description, item.descriptionEn, i18n.language);
  const date = (value?: string) => value ? new Intl.DateTimeFormat(i18n.language, { dateStyle: 'long', timeStyle: 'short' }).format(new Date(value)) : t('public.engagement.consultations.detail.noDeadline');

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <PageHeader variant="hero" title={title} description={description} />
      <main className="bg-surface-container py-10 md:py-16">
        <div className="container-page grid gap-8 lg:grid-cols-[minmax(0,1fr)_21rem]">
          <div className="space-y-8">
            {notice && <div role="status" className={`rounded-[14px] border px-5 py-4 text-body-md ${notice.tone === 'success' ? 'border-green/20 bg-green/5 text-green' : 'border-error/25 bg-error/5 text-error'}`}>{notice.text}</div>}

            {item.options.length > 0 && (
              <section className="rounded-[22px] border border-green/10 bg-white p-6 shadow-[0_18px_50px_rgba(0,59,27,.08)] md:p-9">
                <div className="mb-7 flex items-start gap-4">
                  <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-gold text-xl text-green"><i className="ri-check-double-line" aria-hidden="true" /></span>
                  <div><p className="text-label-sm uppercase tracking-widest text-red">{t(`public.engagement.consultations.detail.type.${item.governanceType}`)}</p><h2 className="mt-1 font-display text-headline-md text-green">{t('public.engagement.consultations.detail.makeChoice')}</h2></div>
                </div>
                {governance?.hasParticipated ? (
                  <div className="rounded-[14px] border border-green/15 bg-green/5 p-5 text-green"><i className="ri-checkbox-circle-line mr-2" />{t('public.engagement.consultations.detail.alreadyVoted')}</div>
                ) : (
                  <form onSubmit={vote}>
                    <fieldset disabled={busy || !governance?.canVote} className="space-y-3">
                      <legend className="sr-only">{t('public.engagement.consultations.detail.makeChoice')}</legend>
                      {item.options.map(option => (
                        <label key={option.id} className="flex min-h-[58px] cursor-pointer items-center gap-4 rounded-[14px] border border-line px-5 py-4 transition hover:border-green has-[:checked]:border-green has-[:checked]:bg-green/5">
                          <input type="radio" name="consultation-option" value={option.id} checked={selected === option.id} onChange={() => setSelected(option.id)} className="h-5 w-5 accent-green" />
                          <span className="text-body-md text-ink">{localized(option.label, option.labelEn, i18n.language)}</span>
                        </label>
                      ))}
                    </fieldset>
                    <div className="mt-6 flex flex-wrap items-center gap-4">
                      <Button type="submit" variant="primary" disabled={!selected || busy || !governance?.canVote}>{busy ? t('admin.common.loading') : t('public.engagement.consultations.detail.submitVote')}</Button>
                      {!governance?.isEligible && <Link className="text-body-sm font-semibold text-green underline" to="/espace-membre">{t('public.engagement.consultations.detail.signIn')}</Link>}
                    </div>
                  </form>
                )}
              </section>
            )}

            {governance?.resultsPublished && (
              <section className="rounded-[22px] border border-green/10 bg-white p-6 md:p-9">
                <h2 className="font-display text-headline-md text-green">{t('public.engagement.consultations.detail.results')}</h2>
                <div className="mt-6 space-y-5">
                  {governance.results.map(result => <div key={result.optionId}>
                    <div className="mb-2 flex justify-between gap-4 text-body-sm"><span>{localized(result.label, result.labelEn, i18n.language)}</span><strong>{result.percentage}% · {result.voteCount}</strong></div>
                    <div className="h-3 overflow-hidden rounded-full bg-surface-container"><div className="h-full rounded-full bg-gold" style={{ width: `${result.percentage}%` }} /></div>
                  </div>)}
                </div>
              </section>
            )}

            {item.allowComments && (
              <section className="rounded-[22px] border border-green/10 bg-white p-6 md:p-9">
                <h2 className="font-display text-headline-md text-green">{t('public.engagement.consultations.detail.discussion')}</h2>
                {governance?.canComment && <form onSubmit={addComment} className="mt-6"><label htmlFor="consultation-comment" className="text-label-sm uppercase text-ink-variant">{t('public.engagement.consultations.detail.commentLabel')}</label><textarea id="consultation-comment" value={comment} onChange={event => setComment(event.target.value)} maxLength={3000} rows={4} className="mt-2 w-full rounded-[12px] border border-outline bg-white px-4 py-3 text-ink focus:border-green focus:outline-none" /><Button type="submit" variant="secondary" disabled={busy || comment.trim().length < 3} className="mt-3">{t('public.engagement.consultations.detail.commentSubmit')}</Button></form>}
                <div className="mt-8 space-y-4">{item.comments.map(entry => <article key={entry.id} className="rounded-[14px] bg-surface-container p-5"><div className="flex flex-wrap justify-between gap-2"><strong className="text-green">{entry.memberName}</strong><time className="text-body-xs text-ink-variant">{date(entry.createdAtUtc)}</time></div><p className="mt-3 whitespace-pre-wrap text-body-md text-ink">{entry.body}</p></article>)}</div>
              </section>
            )}
          </div>

          <aside className="h-fit rounded-[22px] bg-green-deep p-7 text-white shadow-[0_22px_55px_rgba(0,59,27,.16)] lg:sticky lg:top-28">
            <p className="text-label-sm uppercase tracking-widest text-gold">{t('public.engagement.consultations.detail.participation')}</p>
            <dl className="mt-6 space-y-5 text-body-sm">
              <div><dt className="text-green-dim">{t('public.engagement.consultations.detail.status')}</dt><dd className="mt-1 font-semibold text-white">{t(`public.engagement.consultations.detail.statusValue.${governance?.status || 'Draft'}`)}</dd></div>
              <div><dt className="text-green-dim">{t('public.engagement.consultations.detail.opens')}</dt><dd className="mt-1 text-white">{date(item.opensAtUtc)}</dd></div>
              <div><dt className="text-green-dim">{t('public.engagement.consultations.detail.closes')}</dt><dd className="mt-1 text-white">{date(item.closesAtUtc)}</dd></div>
              <div><dt className="text-green-dim">{t('public.engagement.consultations.detail.mode')}</dt><dd className="mt-1 text-white">{t(`public.engagement.consultations.detail.modeValue.${item.votingMode}`)}</dd></div>
            </dl>
            <div className="mt-7 border-t border-white/15 pt-6"><div className="flex justify-between text-body-sm"><span>{t('public.engagement.consultations.detail.quorum')}</span><strong>{governance?.participantCount || 0}/{governance?.requiredParticipation || 0}</strong></div><div className="mt-3 h-2 rounded-full bg-white/15"><div className={`h-full rounded-full ${governance?.quorumReached ? 'bg-gold' : 'bg-white/60'}`} style={{ width: `${Math.min(100, ((governance?.participantCount || 0) / Math.max(1, governance?.requiredParticipation || 0)) * 100)}%` }} /></div></div>
            <Link to="/engagement/consultations" className="mt-7 inline-flex min-h-[44px] items-center gap-2 text-label-sm uppercase text-gold"><i className="ri-arrow-left-line" />{t('public.engagement.consultations.detail.back')}</Link>
          </aside>
        </div>
      </main>
      <Footer />
    </div>
  );
};

export default ConsultationDetailPage;

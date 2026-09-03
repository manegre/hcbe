import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, EmptyState, Field, inputClasses } from '../../../components/ui';
import { serviceCasesApi } from '../../../lib/api/service-cases';
import { resolveMediaUrl } from '../../../lib/api/media-url';
import type { ServiceCase } from '../../../lib/api/types';

const categories = ['integration', 'employment', 'legal', 'education', 'business', 'social-support', 'culture', 'other'];

const MemberServiceCasesPanel = () => {
  const { i18n } = useTranslation();
  const fr = i18n.language.startsWith('fr');
  const [items, setItems] = useState<ServiceCase[]>([]);
  const [selected, setSelected] = useState<ServiceCase | null>(null);
  const [creating, setCreating] = useState(false);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('');
  const [draft, setDraft] = useState({ category: 'integration', subject: '', description: '' });
  const [reply, setReply] = useState('');

  const labels: Record<string, string> = fr ? {
    integration: 'Accueil et intégration', employment: 'Emploi et carrière', legal: 'Aide juridique', education: 'Éducation', business: 'Entrepreneuriat', 'social-support': 'Soutien social', culture: 'Culture et communauté', other: 'Autre',
  } : {
    integration: 'Settlement and integration', employment: 'Employment and career', legal: 'Legal support', education: 'Education', business: 'Business', 'social-support': 'Social support', culture: 'Culture and community', other: 'Other',
  };

  const load = async () => {
    setLoading(true);
    try {
      const response = await serviceCasesApi.mine();
      if (response.success && response.data) {
        setItems(response.data);
        if (selected) setSelected(response.data.find((item) => item.id === selected.id) || null);
      }
    } catch (error) {
      setNotice(error instanceof Error ? error.message : (fr ? 'Chargement impossible.' : 'Unable to load requests.'));
    } finally { setLoading(false); }
  };
  useEffect(() => { void load(); }, []);

  const create = async (event: React.FormEvent) => {
    event.preventDefault(); setBusy(true); setNotice('');
    try {
      const response = await serviceCasesApi.create(draft.category, draft.subject, draft.description);
      if (response.success && response.data) {
        setItems((current) => [response.data!, ...current]);
        setSelected(response.data);
        setCreating(false);
        setDraft({ category: 'integration', subject: '', description: '' });
        setNotice(fr ? `Demande ${response.data.ticketNumber} créée.` : `Request ${response.data.ticketNumber} created.`);
      }
    } catch (error) { setNotice(error instanceof Error ? error.message : 'Error'); }
    finally { setBusy(false); }
  };

  const sendReply = async (event: React.FormEvent) => {
    event.preventDefault(); if (!selected || !reply.trim()) return; setBusy(true);
    try {
      const response = await serviceCasesApi.reply(selected.id, reply);
      if (response.success && response.data) { setSelected(response.data); setItems((current) => current.map((item) => item.id === response.data!.id ? response.data! : item)); setReply(''); }
    } catch (error) { setNotice(error instanceof Error ? error.message : 'Error'); }
    finally { setBusy(false); }
  };

  const uploadFile = async (file?: File) => {
    if (!selected || !file) return; setBusy(true);
    try { await serviceCasesApi.upload(selected.id, file); const response = await serviceCasesApi.getMine(selected.id); if (response.data) setSelected(response.data); }
    catch (error) { setNotice(error instanceof Error ? error.message : 'Error'); }
    finally { setBusy(false); }
  };

  if (loading) return <div className="flex justify-center py-16 text-green"><i className="ri-loader-4-line animate-spin text-2xl" /></div>;

  return (
    <div className="space-y-7">
      <header className="relative overflow-hidden rounded-[26px] bg-green-deep px-6 py-8 text-white sm:px-8">
        <div className="absolute -right-16 -top-20 h-52 w-52 rounded-full border-[34px] border-gold/[.09]" />
        <div className="relative flex flex-col gap-6 sm:flex-row sm:items-end sm:justify-between"><div className="max-w-2xl"><p className="text-[10px] font-bold uppercase tracking-[.18em] text-gold">{fr ? 'Services aux membres' : 'Member services'}</p><h2 className="mt-3 font-display text-3xl font-bold sm:text-4xl">{fr ? 'Comment pouvons-nous vous aider ?' : 'How can we help?'}</h2><p className="mt-3 text-sm leading-6 text-green-dim">{fr ? 'Adressez votre demande au bon comité et suivez chaque réponse dans un espace confidentiel.' : 'Send your request to the right committee and follow every response in a confidential space.'}</p></div><Button type="button" variant="primary" onClick={() => { setCreating(true); setSelected(null); }}><i className="ri-add-line" />{fr ? 'Nouvelle demande' : 'New request'}</Button></div>
      </header>
      {notice && <p className="rounded-xl border border-gold/30 bg-gold/[.08] px-4 py-3 text-sm text-green">{notice}</p>}
      <div className="grid gap-6 lg:grid-cols-[320px_minmax(0,1fr)]">
        <aside className="space-y-3">
          {items.length === 0 ? <EmptyState icon="ri-customer-service-2-line" title={fr ? 'Aucune demande' : 'No requests'} description={fr ? 'Créez une demande pour communiquer avec un comité.' : 'Create a request to contact a committee.'} /> : items.map((item) => <button key={item.id} type="button" onClick={() => { setSelected(item); setCreating(false); }} className={`w-full rounded-2xl border p-4 text-left transition-all ${selected?.id === item.id ? 'border-green bg-green text-white shadow-lg' : 'border-line bg-surface hover:border-green/25'}`}><span className={`text-[9px] font-bold uppercase tracking-[.13em] ${selected?.id === item.id ? 'text-gold' : 'text-red-link'}`}>{item.ticketNumber} · {labels[item.category]}</span><strong className="mt-2 block line-clamp-2 font-display text-lg">{item.subject}</strong><span className={`mt-3 inline-flex rounded-full px-2.5 py-1 text-[9px] font-bold uppercase ${selected?.id === item.id ? 'bg-white/10' : 'bg-canvas text-ink-variant'}`}>{item.status}</span></button>)}
        </aside>
        <main>
          {creating ? (
            <form onSubmit={create} className="rounded-[24px] border border-line bg-surface p-6 shadow-[0_16px_45px_rgba(0,59,27,.06)] sm:p-8"><p className="text-[10px] font-bold uppercase tracking-[.15em] text-red-link">{fr ? 'Nouvelle demande' : 'New request'}</p><h3 className="mt-2 font-display text-2xl font-bold text-green-deep">{fr ? 'Décrivez votre besoin' : 'Tell us what you need'}</h3><div className="mt-7 space-y-5"><Field label={fr ? 'Service concerné' : 'Service'} htmlFor="case-category"><select id="case-category" className={`${inputClasses} cursor-pointer`} value={draft.category} onChange={(event) => setDraft({ ...draft, category: event.target.value })}>{categories.map((category) => <option key={category} value={category}>{labels[category]}</option>)}</select></Field><Field label={fr ? 'Objet' : 'Subject'} htmlFor="case-subject"><input id="case-subject" required maxLength={180} className={inputClasses} value={draft.subject} onChange={(event) => setDraft({ ...draft, subject: event.target.value })} /></Field><Field label={fr ? 'Votre demande' : 'Your request'} htmlFor="case-description"><textarea id="case-description" required minLength={20} maxLength={5000} rows={8} className={`${inputClasses} resize-y`} value={draft.description} onChange={(event) => setDraft({ ...draft, description: event.target.value })} /></Field><div className="flex justify-end gap-3 border-t border-line pt-5"><Button type="button" variant="tertiary" onClick={() => setCreating(false)}>{fr ? 'Annuler' : 'Cancel'}</Button><Button type="submit" variant="primary" disabled={busy}>{fr ? 'Envoyer la demande' : 'Send request'}</Button></div></div></form>
          ) : selected ? (
            <article className="overflow-hidden rounded-[24px] border border-line bg-surface shadow-[0_16px_45px_rgba(0,59,27,.06)]"><header className="border-b border-line bg-green/[.045] p-6 sm:p-8"><div className="flex flex-wrap items-center justify-between gap-3"><span className="text-[10px] font-bold uppercase tracking-[.15em] text-red-link">{selected.ticketNumber}</span><span className="rounded-full border border-green/20 bg-green/[.07] px-3 py-1 text-[9px] font-bold uppercase text-green">{selected.status}</span></div><h3 className="mt-3 font-display text-2xl font-bold text-green-deep">{selected.subject}</h3><p className="mt-4 whitespace-pre-line text-sm leading-7 text-ink-variant">{selected.description}</p></header><div className="p-6 sm:p-8"><h4 className="font-display text-xl font-bold text-green-deep">{fr ? 'Conversation' : 'Conversation'}</h4><div className="mt-5 space-y-3">{selected.messages.length === 0 && <p className="rounded-xl bg-canvas p-4 text-sm text-ink-variant">{fr ? 'Votre demande a été transmise. Une réponse apparaîtra ici.' : 'Your request was sent. A response will appear here.'}</p>}{selected.messages.map((message) => <div key={message.id} className="rounded-2xl border border-line p-4"><div className="flex justify-between gap-3 text-[9px] font-bold uppercase tracking-[.1em] text-ink-variant"><span>{message.authorName || (fr ? 'Équipe HCBE' : 'HCBE team')}</span><time>{new Intl.DateTimeFormat(fr ? 'fr-CA' : 'en-CA', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(message.createdAt))}</time></div><p className="mt-2 whitespace-pre-line text-sm leading-6 text-ink">{message.body}</p></div>)}</div>{selected.attachments.length > 0 && <div className="mt-6 flex flex-wrap gap-2">{selected.attachments.map((file) => <a key={file.id} href={resolveMediaUrl(file.url)} target="_blank" rel="noreferrer" className="rounded-full border border-line px-3 py-2 text-xs font-semibold text-green"><i className="ri-attachment-line mr-1" />{file.fileName}</a>)}</div>}<form onSubmit={sendReply} className="mt-7 border-t border-line pt-6"><Field label={fr ? 'Ajouter une réponse' : 'Add a reply'} htmlFor="case-reply"><textarea id="case-reply" required minLength={2} rows={4} className={`${inputClasses} resize-y`} value={reply} onChange={(event) => setReply(event.target.value)} /></Field><div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><label className="inline-flex min-h-11 cursor-pointer items-center gap-2 text-xs font-bold uppercase tracking-[.08em] text-green"><i className="ri-attachment-2" />{fr ? 'Joindre un fichier' : 'Attach a file'}<input type="file" className="sr-only" onChange={(event) => void uploadFile(event.target.files?.[0])} /></label><Button type="submit" variant="secondary" disabled={busy}>{fr ? 'Envoyer' : 'Send'}</Button></div></form></div></article>
          ) : <EmptyState icon="ri-customer-service-line" title={fr ? 'Sélectionnez une demande' : 'Select a request'} description={fr ? 'Consultez son suivi ou créez une nouvelle demande.' : 'Review its progress or create a new request.'} />}
        </main>
      </div>
    </div>
  );
};

export default MemberServiceCasesPanel;

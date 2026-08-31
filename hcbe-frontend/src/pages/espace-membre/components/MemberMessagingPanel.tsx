import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../../../components/ui';
import { messagingApi } from '../../../lib/api/messaging';
import type { ConversationDto, MessagingContactDto, PrivateMessageDto } from '../../../lib/api/types';
import { createMessagingHubConnection } from '../../../lib/realtime/messaging-hub';
import type { HubConnection } from '@microsoft/signalr';

interface MemberMessagingPanelProps { onUnreadChange?: (count: number) => void; }

const MemberMessagingPanel = ({ onUnreadChange }: MemberMessagingPanelProps) => {
  const { i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const [contacts, setContacts] = useState<MessagingContactDto[]>([]);
  const [conversations, setConversations] = useState<ConversationDto[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [messages, setMessages] = useState<PrivateMessageDto[]>([]);
  const [draft, setDraft] = useState('');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [reporting, setReporting] = useState(false);
  const [reportReason, setReportReason] = useState('');
  const endRef = useRef<HTMLDivElement>(null);
  const activeIdRef = useRef<string | null>(null);
  const [hubConnection, setHubConnection] = useState<HubConnection | null>(null);

  const c = fr ? {
    title: 'Correspondances privées', intro: 'Discutez avec les membres qui ont accepté une mise en relation. Vos échanges ne sont pas publics.',
    newChat: 'Nouvelle conversation', eligible: 'Contacts autorisés', conversations: 'Conversations', search: 'Rechercher…',
    empty: 'Aucune conversation. Commencez avec un contact autorisé.', noContacts: 'Aucun nouveau contact disponible.',
    mentorship: 'Mentorat', networking: 'Réseautage', suspended: 'Cette conversation a été suspendue par la modération.',
    placeholder: 'Écrire un message…', send: 'Envoyer', report: 'Signaler', reportTitle: 'Signaler cette conversation',
    reportHint: 'Expliquez précisément le problème. Le comité examinera votre signalement.', reportLabel: 'Motif du signalement', submitReport: 'Envoyer le signalement', cancel: 'Annuler',
    reported: 'Le signalement a été transmis au comité.', error: 'Impossible de terminer cette opération.', today: 'Aujourd’hui',
  } : {
    title: 'Private correspondence', intro: 'Talk with members who accepted a connection. Your exchanges are not public.',
    newChat: 'New conversation', eligible: 'Eligible contacts', conversations: 'Conversations', search: 'Search…',
    empty: 'No conversations yet. Start with an eligible contact.', noContacts: 'No new contacts available.',
    mentorship: 'Mentorship', networking: 'Networking', suspended: 'This conversation has been suspended by moderation.',
    placeholder: 'Write a message…', send: 'Send', report: 'Report', reportTitle: 'Report this conversation',
    reportHint: 'Explain the issue clearly. The committee will review your report.', reportLabel: 'Reason for reporting', submitReport: 'Submit report', cancel: 'Cancel',
    reported: 'The report was sent to the committee.', error: 'This operation could not be completed.', today: 'Today',
  };

  const active = conversations.find((item) => item.id === activeId);
  const availableContacts = contacts.filter((item) => !item.hasConversation && item.memberName.toLowerCase().includes(search.toLowerCase()));
  const filteredConversations = useMemo(() => conversations.filter((item) => item.counterpartName.toLowerCase().includes(search.toLowerCase())), [conversations, search]);

  const loadShell = async (keepSelection = true) => {
    const [contactResult, conversationResult] = await Promise.all([messagingApi.getContacts(), messagingApi.getConversations()]);
    if (contactResult.success && contactResult.data) setContacts(contactResult.data);
    if (conversationResult.success && conversationResult.data) {
      setConversations(conversationResult.data);
      onUnreadChange?.(conversationResult.data.reduce((sum, item) => sum + item.unreadCount, 0));
      if (!keepSelection && conversationResult.data[0]) setActiveId(conversationResult.data[0].id);
    }
    setLoading(false);
  };

  const loadMessages = async (conversationId: string, quiet = false) => {
    const result = await messagingApi.getMessages(conversationId);
    if (result.success && result.data) {
      setMessages(result.data);
      await messagingApi.markRead(conversationId);
      if (!quiet) window.setTimeout(() => endRef.current?.scrollIntoView({ behavior: 'smooth' }), 20);
      await loadShell();
    }
  };

  useEffect(() => { void loadShell(false); }, []);
  useEffect(() => {
    activeIdRef.current = activeId;
  }, [activeId]);
  useEffect(() => {
    const connection = createMessagingHubConnection();
    connection.on('MessageReceived', (message: PrivateMessageDto) => {
      if (message.conversationId === activeIdRef.current) {
        setMessages((current) => current.some((item) => item.id === message.id) ? current : [...current, message]);
        window.setTimeout(() => endRef.current?.scrollIntoView({ behavior: 'smooth' }), 20);
      }
      void loadShell();
    });
    void connection.start().then(() => setHubConnection(connection)).catch(() => undefined);
    return () => { void connection.stop(); };
  }, []);
  useEffect(() => {
    if (!hubConnection || !activeId) return;
    void hubConnection.invoke('JoinConversation', activeId).catch(() => undefined);
    return () => { void hubConnection.invoke('LeaveConversation', activeId).catch(() => undefined); };
  }, [hubConnection, activeId]);
  useEffect(() => {
    if (!activeId) { setMessages([]); return; }
    void loadMessages(activeId);
    const timer = window.setInterval(() => void loadMessages(activeId, true), 30000);
    return () => window.clearInterval(timer);
  }, [activeId]);

  const start = async (contact: MessagingContactDto) => {
    setNotice(null);
    const result = await messagingApi.startConversation(contact.memberId);
    if (!result.success || !result.data) { setNotice(result.message || c.error); return; }
    await loadShell(); setActiveId(result.data.id); setSearch('');
  };

  const send = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!activeId || !draft.trim()) return;
    setSending(true); setNotice(null);
    const body = draft; setDraft('');
    const result = await messagingApi.sendMessage(activeId, body);
    if (!result.success) { setDraft(body); setNotice(result.message || c.error); }
    else await loadMessages(activeId);
    setSending(false);
  };

  const report = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!activeId) return;
    setSending(true);
    const result = await messagingApi.report(activeId, reportReason);
    setNotice(result.success ? c.reported : result.message || c.error);
    if (result.success) { setReporting(false); setReportReason(''); }
    setSending(false);
  };

  const time = (value: string) => new Date(value).toLocaleTimeString(fr ? 'fr-CA' : 'en-CA', { hour: '2-digit', minute: '2-digit' });

  return (
    <div className="overflow-hidden rounded-2xl border border-line bg-canvas/45">
      <div className="border-b border-line bg-surface px-5 py-4"><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">{c.conversations}</p><h3 className="mt-1 font-display text-2xl font-bold text-green-deep">{c.title}</h3><p className="mt-1 max-w-2xl text-sm text-ink-variant">{c.intro}</p></div>
      {notice && <p className="border-b border-line bg-gold/10 px-5 py-3 text-sm text-green-deep">{notice}</p>}
      <div className="grid min-h-[540px] lg:grid-cols-[310px_minmax(0,1fr)]">
        <aside className={`${activeId ? 'hidden lg:block' : 'block'} border-r border-line bg-surface`}>
          <div className="border-b border-line p-4"><input className={inputClasses} value={search} onChange={(e) => setSearch(e.target.value)} placeholder={c.search} /></div>
          <div className="max-h-[470px] overflow-y-auto p-2">
            {loading ? <div className="p-8 text-center text-ink-variant"><i className="ri-loader-4-line animate-spin text-xl" /></div> : null}
            {filteredConversations.map((item) => <button key={item.id} type="button" onClick={() => setActiveId(item.id)} className={`mb-1 flex w-full items-start gap-3 rounded-xl p-3 text-left transition-colors ${activeId === item.id ? 'bg-green text-white' : 'hover:bg-canvas'}`}><span className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-xs font-bold ${activeId === item.id ? 'bg-white/15 text-white' : 'bg-green/10 text-green'}`}>{item.counterpartName.split(' ').map((part) => part[0]).slice(0,2).join('')}</span><span className="min-w-0 flex-1"><span className="flex items-center justify-between gap-2"><strong className="truncate text-sm">{item.counterpartName}</strong>{item.unreadCount > 0 && <span className="rounded-full bg-gold px-2 py-0.5 text-[10px] font-bold text-green-deep">{item.unreadCount}</span>}</span><span className={`mt-1 block truncate text-xs ${activeId === item.id ? 'text-green-dim' : 'text-ink-variant'}`}>{item.lastMessage || (item.relationshipType === 'Mentorship' ? c.mentorship : c.networking)}</span></span></button>)}
            <div className="my-3 border-t border-line" />
            <p className="px-3 pb-2 text-[9px] font-bold uppercase tracking-[.16em] text-ink-variant">{c.newChat}</p>
            {availableContacts.length === 0 ? <p className="px-3 py-4 text-xs leading-5 text-ink-variant">{conversations.length ? c.noContacts : c.empty}</p> : availableContacts.map((item) => <button key={item.memberId} type="button" onClick={() => void start(item)} className="flex w-full items-center gap-3 rounded-xl p-3 text-left hover:bg-canvas"><span className="flex h-9 w-9 items-center justify-center rounded-full border border-line text-green"><i className="ri-add-line" /></span><span><strong className="block text-sm text-green-deep">{item.memberName}</strong><small className="text-ink-variant">{item.relationshipType === 'Mentorship' ? c.mentorship : c.networking}</small></span></button>)}
          </div>
        </aside>

        <section className={`${activeId ? 'flex' : 'hidden lg:flex'} min-w-0 flex-col bg-surface`}>
          {!active ? <div className="m-auto max-w-sm p-8 text-center"><span className="mx-auto flex h-14 w-14 items-center justify-center rounded-full border border-line bg-canvas text-xl text-green"><i className="ri-chat-smile-2-line" /></span><p className="mt-4 text-sm leading-6 text-ink-variant">{c.empty}</p></div> : <>
            <header className="flex items-center justify-between gap-3 border-b border-line px-4 py-3"><div className="flex min-w-0 items-center gap-3"><button type="button" onClick={() => setActiveId(null)} className="flex h-9 w-9 items-center justify-center rounded-full border border-line lg:hidden"><i className="ri-arrow-left-line" /></button><div><h4 className="truncate font-semibold text-green-deep">{active.counterpartName}</h4><p className="text-[10px] uppercase tracking-[.12em] text-ink-variant">{active.relationshipType === 'Mentorship' ? c.mentorship : c.networking}</p></div></div><button type="button" onClick={() => setReporting(true)} className="text-[9px] font-bold uppercase tracking-[.12em] text-red-link"><i className="ri-flag-line mr-1" />{c.report}</button></header>
            <div className="flex-1 overflow-y-auto bg-[radial-gradient(circle_at_top_left,rgba(255,205,0,.05),transparent_35%)] px-4 py-5 sm:px-6">{messages.map((message) => <div key={message.id} className={`mb-3 flex ${message.isMine ? 'justify-end' : 'justify-start'}`}><div className={`max-w-[82%] rounded-2xl px-4 py-3 text-sm leading-6 shadow-sm ${message.isMine ? 'rounded-br-sm bg-green text-white' : 'rounded-bl-sm border border-line bg-surface text-ink'}`}><p className="whitespace-pre-wrap break-words">{message.body}</p><p className={`mt-1 text-right text-[9px] ${message.isMine ? 'text-green-dim' : 'text-ink-variant'}`}>{time(message.createdAt)}{message.isMine && <span className="ml-1">{message.readAt ? '✓✓' : '✓'}</span>}</p></div></div>)}<div ref={endRef} /></div>
            {active.status === 'Suspended' ? <p className="border-t border-line bg-red-link/5 px-5 py-4 text-sm text-red-link">{c.suspended}</p> : <form onSubmit={send} className="flex items-end gap-3 border-t border-line p-4"><textarea rows={1} maxLength={2000} className={`${inputClasses} max-h-32 resize-none`} value={draft} onChange={(e) => setDraft(e.target.value)} onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); e.currentTarget.form?.requestSubmit(); } }} placeholder={c.placeholder} /><Button type="submit" variant="secondary" disabled={sending || !draft.trim()} className="h-11 shrink-0 px-4"><i className="ri-send-plane-fill" /><span className="hidden sm:inline">{c.send}</span></Button></form>}
          </>}
        </section>
      </div>

      {reporting && <div className="fixed inset-0 z-[90] flex items-center justify-center bg-green-deep/75 p-4"><form onSubmit={report} className="w-full max-w-lg rounded-2xl bg-surface p-6 shadow-2xl"><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">{c.report}</p><h3 className="mt-2 font-display text-2xl font-bold text-green-deep">{c.reportTitle}</h3><p className="mt-2 text-sm leading-5 text-ink-variant">{c.reportHint}</p><div className="mt-5"><Field label={c.reportLabel} htmlFor="report-reason"><textarea id="report-reason" autoFocus required minLength={10} rows={5} className={inputClasses} value={reportReason} onChange={(e) => setReportReason(e.target.value)} /></Field></div><div className="mt-5 flex gap-3"><Button type="submit" variant="secondary" disabled={sending}>{c.submitReport}</Button><Button type="button" variant="tertiary" onClick={() => setReporting(false)}>{c.cancel}</Button></div></form></div>}
    </div>
  );
};

export default MemberMessagingPanel;

import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../../../components/ui';
import { messagingApi } from '../../../lib/api/messaging';
import type { ConversationDto, MessagingContactDto, PrivateMessageDto } from '../../../lib/api/types';
import { createMessagingHubConnection } from '../../../lib/realtime/messaging-hub';
import type { HubConnection } from '@microsoft/signalr';
import { notifyFromApp } from '../../../lib/pwa/notifications';
import { engagementApi } from '../../../lib/api/engagement';
import type { MemberBlock } from '../../../lib/api/types';

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
  const [blocks, setBlocks] = useState<MemberBlock[]>([]);

  const c = fr ? {
    title: 'Correspondances privées', intro: 'Discutez avec les membres qui ont accepté une mise en relation. Vos échanges ne sont pas publics.',
    newChat: 'Nouvelle conversation', eligible: 'Contacts autorisés', conversations: 'Conversations', search: 'Rechercher…',
    empty: 'Aucune conversation. Commencez avec un contact autorisé.', noContacts: 'Aucun nouveau contact disponible.',
    mentorship: 'Mentorat', networking: 'Réseautage', suspended: 'Cette conversation a été suspendue par la modération.',
    placeholder: 'Écrire un message…', send: 'Envoyer', report: 'Signaler', reportTitle: 'Signaler cette conversation',
    reportHint: 'Expliquez précisément le problème. Le comité examinera votre signalement.', reportLabel: 'Motif du signalement', submitReport: 'Envoyer le signalement', cancel: 'Annuler',
    reported: 'Le signalement a été transmis au comité.', error: 'Impossible de terminer cette opération.', today: 'Aujourd’hui',
    workspace: 'Messagerie des membres', privateLabel: 'Échanges privés', conversationCount: 'conversation(s)', contactCount: 'nouveau(x) contact(s)',
    noConversationTitle: 'Choisissez une conversation', noConversationText: 'Sélectionnez un échange à gauche ou commencez une nouvelle conversation avec un contact autorisé.',
    protectedTitle: 'Un espace de confiance', protectedText: 'Seuls les contacts issus d’une mise en relation acceptée ou d’un jumelage peuvent vous écrire.',
    block: 'Bloquer', unblock: 'Débloquer', blocked: 'Vous avez bloqué ce membre. Les nouveaux messages sont désactivés.', blockConfirm: 'Bloquer ce membre et désactiver les nouveaux messages ?', unblocked: 'Le membre a été débloqué.',
  } : {
    title: 'Private correspondence', intro: 'Talk with members who accepted a connection. Your exchanges are not public.',
    newChat: 'New conversation', eligible: 'Eligible contacts', conversations: 'Conversations', search: 'Search…',
    empty: 'No conversations yet. Start with an eligible contact.', noContacts: 'No new contacts available.',
    mentorship: 'Mentorship', networking: 'Networking', suspended: 'This conversation has been suspended by moderation.',
    placeholder: 'Write a message…', send: 'Send', report: 'Report', reportTitle: 'Report this conversation',
    reportHint: 'Explain the issue clearly. The committee will review your report.', reportLabel: 'Reason for reporting', submitReport: 'Submit report', cancel: 'Cancel',
    reported: 'The report was sent to the committee.', error: 'This operation could not be completed.', today: 'Today',
    workspace: 'Member messaging', privateLabel: 'Private exchanges', conversationCount: 'conversation(s)', contactCount: 'new contact(s)',
    noConversationTitle: 'Choose a conversation', noConversationText: 'Select a conversation on the left or start a new one with an eligible contact.',
    protectedTitle: 'A trusted space', protectedText: 'Only contacts from an accepted connection or mentorship match can message you.',
    block: 'Block', unblock: 'Unblock', blocked: 'You blocked this member. New messages are disabled.', blockConfirm: 'Block this member and disable new messages?', unblocked: 'The member has been unblocked.',
  };

  const active = conversations.find((item) => item.id === activeId);
  const activeBlock = active ? blocks.find((item) => item.memberId === active.counterpartMemberId) : undefined;
  const availableContacts = contacts.filter((item) => !item.hasConversation && item.memberName.toLowerCase().includes(search.toLowerCase()));
  const filteredConversations = useMemo(() => conversations.filter((item) => item.counterpartName.toLowerCase().includes(search.toLowerCase())), [conversations, search]);

  const loadShell = async (keepSelection = true) => {
    const [contactResult, conversationResult, blockResult] = await Promise.all([messagingApi.getContacts(), messagingApi.getConversations(), engagementApi.getBlocks()]);
    if (contactResult.success && contactResult.data) setContacts(contactResult.data);
    if (conversationResult.success && conversationResult.data) {
      setConversations(conversationResult.data);
      onUnreadChange?.(conversationResult.data.reduce((sum, item) => sum + item.unreadCount, 0));
      if (!keepSelection && conversationResult.data[0]) setActiveId(conversationResult.data[0].id);
    }
    if (blockResult.success && blockResult.data) setBlocks(blockResult.data);
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
      if (document.hidden || message.conversationId !== activeIdRef.current) {
        void notifyFromApp(
          fr ? 'Nouveau message HCBE' : 'New HCBE message',
          message.body,
          '/espace-membre?section=messages',
        );
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

  const toggleBlock = async () => {
    if (!active) return;
    if (!activeBlock && !window.confirm(c.blockConfirm)) return;
    setNotice(null);
    const result = activeBlock ? await engagementApi.unblock(active.counterpartMemberId) : await engagementApi.block(active.counterpartMemberId);
    setNotice(result.success ? (activeBlock ? c.unblocked : c.blocked) : result.message || c.error);
    await loadShell();
  };

  const time = (value: string) => new Date(value).toLocaleTimeString(fr ? 'fr-CA' : 'en-CA', { hour: '2-digit', minute: '2-digit' });

  return (
    <div className="overflow-hidden rounded-[26px] border border-line bg-surface shadow-[0_18px_50px_rgba(0,59,27,.07)]">
      <header className="relative overflow-hidden bg-green-deep px-6 py-7 text-white sm:px-8">
        <div className="absolute -right-16 -top-20 h-52 w-52 rounded-full border-[34px] border-gold/[0.09]" aria-hidden="true" />
        <div className="relative flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
          <div className="max-w-2xl">
            <div className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/[0.06] px-3 py-1.5 text-[9px] font-bold uppercase tracking-[.16em] text-gold"><i className="ri-chat-private-line text-sm" aria-hidden="true" />{c.workspace}</div>
            <h2 className="mt-4 font-display text-3xl font-bold text-white sm:text-4xl">{c.title}</h2>
            <p className="mt-3 text-sm leading-6 text-green-dim">{c.intro}</p>
          </div>
          <div className="inline-flex w-fit items-center gap-2 rounded-xl border border-white/10 bg-white/[0.055] px-4 py-3 text-[9px] font-bold uppercase tracking-[.13em] text-green-dim"><i className="ri-lock-2-line text-base text-gold" aria-hidden="true" />{c.privateLabel}</div>
        </div>
      </header>

      {notice && <p className="flex items-start gap-2 border-b border-gold/25 bg-gold/[0.08] px-5 py-3 text-sm text-green-deep"><i className="ri-information-line mt-0.5 text-lg" aria-hidden="true" />{notice}</p>}

      <div className="grid min-h-[620px] lg:grid-cols-[330px_minmax(0,1fr)]">
        <aside className={`${activeId ? 'hidden lg:block' : 'block'} border-r border-line bg-canvas/45`}>
          <div className="border-b border-line bg-surface p-4">
            <label className="relative block"><i className="ri-search-2-line absolute left-4 top-1/2 -translate-y-1/2 text-lg text-ink-variant" aria-hidden="true" /><input aria-label={c.search} className={`${inputClasses} bg-canvas/45 pl-11`} value={search} onChange={(e) => setSearch(e.target.value)} placeholder={c.search} /></label>
            <div className="mt-3 flex items-center justify-between text-[9px] font-bold uppercase tracking-[.12em] text-ink-variant"><span>{conversations.length} {c.conversationCount}</span><span>{availableContacts.length} {c.contactCount}</span></div>
          </div>

          <div className="max-h-[535px] overflow-y-auto p-3">
            {loading && <div className="p-8 text-center text-ink-variant"><i className="ri-loader-4-line animate-spin text-xl" /></div>}
            <p className="px-2 pb-2 text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{c.conversations}</p>
            {filteredConversations.map((item) => (
              <button key={item.id} type="button" onClick={() => setActiveId(item.id)} className={`mb-1.5 flex w-full items-start gap-3 rounded-2xl border p-3 text-left transition-all ${activeId === item.id ? 'border-green bg-green text-white shadow-[0_10px_24px_rgba(0,59,27,.12)]' : 'border-transparent bg-surface/65 hover:border-line hover:bg-surface'}`}>
                <span className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl text-xs font-bold ${activeId === item.id ? 'bg-white/15 text-white' : 'bg-green/[0.09] text-green'}`}>{item.counterpartName.split(' ').map((part) => part[0]).slice(0,2).join('')}</span>
                <span className="min-w-0 flex-1"><span className="flex items-center justify-between gap-2"><strong className="truncate text-sm">{item.counterpartName}</strong>{item.unreadCount > 0 && <span className="rounded-full bg-gold px-2 py-0.5 text-[10px] font-bold text-green-deep">{item.unreadCount}</span>}</span><span className={`mt-1 block truncate text-xs ${activeId === item.id ? 'text-green-dim' : 'text-ink-variant'}`}>{item.lastMessage || (item.relationshipType === 'Mentorship' ? c.mentorship : c.networking)}</span></span>
              </button>
            ))}

            <div className="my-4 border-t border-line" />
            <p className="px-2 pb-2 text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{c.newChat}</p>
            {availableContacts.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-line bg-surface/55 p-4"><i className="ri-user-add-line text-xl text-green" aria-hidden="true" /><p className="mt-2 text-xs leading-5 text-ink-variant">{conversations.length ? c.noContacts : c.empty}</p></div>
            ) : availableContacts.map((item) => (
              <button key={item.memberId} type="button" onClick={() => void start(item)} className="mb-1 flex w-full items-center gap-3 rounded-2xl border border-transparent bg-surface/60 p-3 text-left transition-colors hover:border-line hover:bg-surface"><span className="flex h-10 w-10 items-center justify-center rounded-xl bg-green/[0.08] text-green"><i className="ri-add-line" /></span><span className="min-w-0"><strong className="block truncate text-sm text-green-deep">{item.memberName}</strong><small className="text-ink-variant">{item.relationshipType === 'Mentorship' ? c.mentorship : c.networking}</small></span></button>
            ))}
          </div>
        </aside>

        <section className={`${activeId ? 'flex' : 'hidden lg:flex'} min-w-0 flex-col bg-surface`}>
          {!active ? (
            <div className="m-auto max-w-md p-8 text-center">
              <div className="mx-auto flex w-fit -space-x-3" aria-hidden="true"><span className="flex h-12 w-12 items-center justify-center rounded-full border-4 border-surface bg-green text-xs font-bold text-white">HC</span><span className="flex h-12 w-12 items-center justify-center rounded-full border-4 border-surface bg-gold text-xs font-bold text-green-deep">BE</span></div>
              <h3 className="mt-5 font-display text-2xl font-bold text-green-deep">{c.noConversationTitle}</h3>
              <p className="mt-2 text-sm leading-6 text-ink-variant">{c.noConversationText}</p>
              <div className="mt-6 rounded-2xl border border-green/10 bg-green/[0.045] p-4 text-left"><p className="flex items-center gap-2 text-xs font-bold text-green"><i className="ri-shield-check-line text-base" />{c.protectedTitle}</p><p className="mt-2 text-xs leading-5 text-ink-variant">{c.protectedText}</p></div>
            </div>
          ) : <>
            <header className="flex items-center justify-between gap-3 border-b border-line bg-canvas/35 px-4 py-4 sm:px-5">
              <div className="flex min-w-0 items-center gap-3"><button type="button" onClick={() => setActiveId(null)} className="flex h-10 w-10 items-center justify-center rounded-xl border border-line bg-surface lg:hidden"><i className="ri-arrow-left-line" /></button><span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-green text-xs font-bold text-white">{active.counterpartName.split(' ').map((part) => part[0]).slice(0,2).join('')}</span><div className="min-w-0"><h4 className="truncate font-display text-lg font-bold text-green-deep">{active.counterpartName}</h4><p className="mt-0.5 inline-flex rounded-full bg-green/[0.07] px-2 py-1 text-[9px] font-bold uppercase tracking-[.11em] text-green">{active.relationshipType === 'Mentorship' ? c.mentorship : c.networking}</p></div></div>
              <div className="flex items-center gap-1"><button type="button" onClick={toggleBlock} className="inline-flex h-9 items-center gap-2 rounded-lg px-3 text-[9px] font-bold uppercase tracking-[.12em] text-ink-variant transition-colors hover:bg-canvas"><i className={activeBlock ? 'ri-user-follow-line' : 'ri-user-forbid-line'} />{activeBlock ? c.unblock : c.block}</button><button type="button" onClick={() => setReporting(true)} className="inline-flex h-9 items-center gap-2 rounded-lg px-3 text-[9px] font-bold uppercase tracking-[.12em] text-red-link transition-colors hover:bg-red-link/5"><i className="ri-flag-line" />{c.report}</button></div>
            </header>

            <div className="flex-1 overflow-y-auto bg-[radial-gradient(circle_at_top_left,rgba(255,205,0,.06),transparent_38%)] px-4 py-6 sm:px-6">
              {messages.map((message) => <div key={message.id} className={`mb-3 flex ${message.isMine ? 'justify-end' : 'justify-start'}`}><div className={`max-w-[84%] rounded-2xl px-4 py-3 text-sm leading-6 shadow-sm ${message.isMine ? 'rounded-br-sm bg-green text-white' : 'rounded-bl-sm border border-line bg-surface text-ink'}`}><p className="whitespace-pre-wrap break-words">{message.body}</p><p className={`mt-1 text-right text-[9px] ${message.isMine ? 'text-green-dim' : 'text-ink-variant'}`}>{time(message.createdAt)}{message.isMine && <span className="ml-1">{message.readAt ? '✓✓' : '✓'}</span>}</p></div></div>)}
              <div ref={endRef} />
            </div>

            {activeBlock ? <p className="border-t border-line bg-gold/[.08] px-5 py-4 text-sm text-green-deep"><i className="ri-user-forbid-line mr-2" />{c.blocked}</p> : active.status === 'Suspended' ? <p className="border-t border-line bg-red-link/5 px-5 py-4 text-sm text-red-link">{c.suspended}</p> : (
              <form onSubmit={send} className="border-t border-line bg-canvas/25 p-4"><div className="flex items-end gap-3 rounded-2xl border border-line bg-surface p-2 pl-4 shadow-[0_8px_24px_rgba(0,59,27,.05)]"><textarea rows={1} maxLength={2000} className="max-h-32 min-h-10 flex-1 resize-none bg-transparent py-2 text-sm text-ink outline-none placeholder:text-ink-variant/60" value={draft} onChange={(e) => setDraft(e.target.value)} onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); e.currentTarget.form?.requestSubmit(); } }} placeholder={c.placeholder} /><Button type="submit" variant="secondary" disabled={sending || !draft.trim()} className="h-11 shrink-0 rounded-xl px-4"><i className="ri-send-plane-fill" /><span className="hidden sm:inline">{c.send}</span></Button></div></form>
            )}
          </>}
        </section>
      </div>

      {reporting && <div className="fixed inset-0 z-[90] flex items-center justify-center bg-green-deep/80 p-4 backdrop-blur-sm"><form onSubmit={report} className="w-full max-w-lg overflow-hidden rounded-[24px] border border-white/10 bg-surface shadow-2xl"><div className="bg-red-link/[0.06] p-6"><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">{c.report}</p><h3 className="mt-2 font-display text-2xl font-bold text-green-deep">{c.reportTitle}</h3><p className="mt-2 text-sm leading-5 text-ink-variant">{c.reportHint}</p></div><div className="p-6"><Field label={c.reportLabel} htmlFor="report-reason"><textarea id="report-reason" autoFocus required minLength={10} rows={5} className={inputClasses} value={reportReason} onChange={(e) => setReportReason(e.target.value)} /></Field><div className="mt-5 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end"><Button type="button" variant="tertiary" onClick={() => setReporting(false)}>{c.cancel}</Button><Button type="submit" variant="secondary" disabled={sending}>{c.submitReport}</Button></div></div></form></div>}
    </div>
  );
};

export default MemberMessagingPanel;

import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AdminPageHeader } from '../../../components/admin/AdminPageHeader';
import { Button, EmptyState, Field, inputClasses } from '../../../components/ui';
import { serviceCasesApi } from '../../../lib/api/service-cases';
import { usersApi } from '../../../lib/api/users';
import { associationsApi } from '../../../lib/api/associations';
import type { AdminUser, Association, ServiceCase } from '../../../lib/api/types';

const statuses = ['Submitted', 'InReview', 'AwaitingMember', 'Resolved', 'Closed'];
const priorities = ['Low', 'Normal', 'High', 'Urgent'];
const categories = ['integration', 'employment', 'legal', 'education', 'business', 'social-support', 'culture', 'other'];

export default function AdminServiceCasesPage() {
  const { i18n } = useTranslation();
  const fr = i18n.language.startsWith('fr');
  const [items, setItems] = useState<ServiceCase[]>([]);
  const [selected, setSelected] = useState<ServiceCase | null>(null);
  const [admins, setAdmins] = useState<AdminUser[]>([]);
  const [organizations, setOrganizations] = useState<Association[]>([]);
  const [filters, setFilters] = useState({ status: '', category: '', search: '' });
  const [reply, setReply] = useState('');
  const [internal, setInternal] = useState(false);
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState('');

  const load = async () => {
    setLoading(true);
    try {
      const response = await serviceCasesApi.adminList(filters.status, filters.category, filters.search);
      if (response.success && response.data) {
        setItems(response.data);
        setSelected((current) => current ? response.data!.find((item) => item.id === current.id) || current : null);
      }
    } catch (error) { setNotice(error instanceof Error ? error.message : 'Error'); }
    finally { setLoading(false); }
  };

  useEffect(() => { Promise.all([usersApi.getAdminUsers(), associationsApi.getAssociationsForAdmin()]).then(([adminResponse, organizationResponse]) => { if (adminResponse.data) setAdmins(adminResponse.data); if (organizationResponse.data) setOrganizations(organizationResponse.data); }).catch(() => undefined); }, []);
  useEffect(() => { const timer = window.setTimeout(() => void load(), 250); return () => window.clearTimeout(timer); }, [filters.status, filters.category, filters.search]);

  const update = async (data: Parameters<typeof serviceCasesApi.adminUpdate>[1]) => {
    if (!selected) return;
    try {
      const response = await serviceCasesApi.adminUpdate(selected.id, data);
      if (response.data) {
        setSelected(response.data);
        setItems((current) => current.map((item) => item.id === response.data!.id ? response.data! : item));
        setNotice(fr ? 'Dossier mis à jour.' : 'Case updated.');
      }
    } catch (error) { setNotice(error instanceof Error ? error.message : 'Error'); }
  };

  const send = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selected || !reply.trim()) return;
    try {
      const response = await serviceCasesApi.adminReply(selected.id, reply, internal);
      if (response.data) { setSelected(response.data); setReply(''); setNotice(fr ? 'Message ajouté.' : 'Message added.'); }
    } catch (error) { setNotice(error instanceof Error ? error.message : 'Error'); }
  };

  return (
    <div className="space-y-6">
      <AdminPageHeader title={fr ? 'Demandes et accompagnement' : 'Requests and support'} subtitle={fr ? 'Assignez les demandes, échangez avec les membres et suivez leur résolution.' : 'Assign requests, communicate with members and track resolution.'} icon="ri-customer-service-2-line" count={items.length} />
      {notice && <p className="rounded-xl border border-gold/30 bg-gold/[.08] px-4 py-3 text-sm text-green">{notice}</p>}
      <section className="overflow-hidden rounded-[24px] border border-line bg-surface shadow-[0_16px_50px_rgba(0,59,27,.07)]">
        <div className="grid gap-3 border-b border-line p-4 md:grid-cols-[1fr_190px_190px]">
          <input className={inputClasses} placeholder={fr ? 'Rechercher un dossier…' : 'Search cases…'} value={filters.search} onChange={(event) => setFilters({ ...filters, search: event.target.value })} />
          <select className={inputClasses} value={filters.status} onChange={(event) => setFilters({ ...filters, status: event.target.value })}><option value="">{fr ? 'Tous les statuts' : 'All statuses'}</option>{statuses.map((item) => <option key={item}>{item}</option>)}</select>
          <select className={inputClasses} value={filters.category} onChange={(event) => setFilters({ ...filters, category: event.target.value })}><option value="">{fr ? 'Tous les services' : 'All services'}</option>{categories.map((item) => <option key={item}>{item}</option>)}</select>
        </div>
        <div className="grid min-h-[600px] lg:grid-cols-[360px_minmax(0,1fr)]">
          <aside className="border-b border-line bg-canvas/40 lg:border-b-0 lg:border-r">
            <div className="max-h-[700px] overflow-y-auto p-3">
              {loading ? <p className="p-8 text-center text-green"><i className="ri-loader-4-line animate-spin text-xl" /></p> : items.length === 0 ? <EmptyState icon="ri-inbox-line" title={fr ? 'Aucune demande' : 'No requests'} /> : items.map((item) => (
                <button key={item.id} onClick={() => setSelected(item)} className={`mb-2 w-full rounded-2xl border p-4 text-left ${selected?.id === item.id ? 'border-green bg-green text-white' : 'border-line bg-surface hover:border-green/25'}`}>
                  <div className="flex items-center justify-between gap-3"><span className={`text-[9px] font-bold uppercase tracking-[.12em] ${selected?.id === item.id ? 'text-gold' : 'text-red-link'}`}>{item.ticketNumber}</span><span className="text-[9px] uppercase">{item.priority}</span></div>
                  <strong className="mt-2 block line-clamp-2 font-display text-lg">{item.subject}</strong>
                  <span className={`mt-2 block text-xs ${selected?.id === item.id ? 'text-white/65' : 'text-ink-variant'}`}>{item.memberName} · {item.status}</span>
                </button>
              ))}
            </div>
          </aside>
          <main className="p-5 sm:p-7">
            {selected ? <div className="space-y-7">
              <header><p className="text-[10px] font-bold uppercase tracking-[.14em] text-red-link">{selected.ticketNumber} · {selected.category}</p><h2 className="mt-2 font-display text-3xl font-bold text-green-deep">{selected.subject}</h2><p className="mt-2 text-sm text-ink-variant">{selected.memberName} · {selected.memberEmail}</p><p className="mt-5 whitespace-pre-line rounded-2xl bg-canvas p-5 text-sm leading-7 text-ink">{selected.description}</p></header>
              <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                <Field label={fr ? 'Statut' : 'Status'} htmlFor="case-status"><select id="case-status" className={inputClasses} value={selected.status} onChange={(event) => void update({ status: event.target.value })}>{statuses.map((item) => <option key={item}>{item}</option>)}</select></Field>
                <Field label={fr ? 'Priorité' : 'Priority'} htmlFor="case-priority"><select id="case-priority" className={inputClasses} value={selected.priority} onChange={(event) => void update({ priority: event.target.value })}>{priorities.map((item) => <option key={item}>{item}</option>)}</select></Field>
                <Field label={fr ? 'Responsable' : 'Assignee'} htmlFor="case-assignee"><select id="case-assignee" className={inputClasses} value={selected.assignedToUserId || ''} onChange={(event) => void update(event.target.value ? { assignedToUserId: event.target.value } : { clearAssignee: true })}><option value="">{fr ? 'Non assigné' : 'Unassigned'}</option>{admins.map((admin) => <option key={admin.id} value={admin.id}>{admin.firstName} {admin.lastName}</option>)}</select></Field>
                <Field label={fr ? 'Organisation responsable' : 'Responsible organization'} htmlFor="case-organization"><select id="case-organization" className={inputClasses} value={selected.assignedAssociationId || ''} onChange={(event) => void update(event.target.value ? { assignedAssociationId: event.target.value } : { clearAssociation: true })}><option value="">{fr ? 'Équipe centrale HCBE' : 'HCBE central team'}</option>{organizations.filter((item) => item.isActive).map((organization) => <option key={organization.id} value={organization.id}>{organization.name}</option>)}</select></Field>
              </section>
              <section><h3 className="font-display text-xl font-bold text-green-deep">{fr ? 'Historique' : 'History'}</h3><div className="mt-4 space-y-3">{selected.messages.map((message) => <div key={message.id} className={`rounded-2xl border p-4 ${message.isInternal ? 'border-gold/30 bg-gold/[.07]' : 'border-line'}`}><div className="flex justify-between text-[9px] font-bold uppercase tracking-[.1em] text-ink-variant"><span>{message.authorName || 'HCBE'} {message.isInternal && '· NOTE INTERNE'}</span><time>{new Date(message.createdAt).toLocaleString()}</time></div><p className="mt-2 whitespace-pre-line text-sm leading-6">{message.body}</p></div>)}</div></section>
              <form onSubmit={send} className="border-t border-line pt-6"><Field label={internal ? (fr ? 'Note interne' : 'Internal note') : (fr ? 'Répondre au membre' : 'Reply to member')} htmlFor="admin-case-reply"><textarea id="admin-case-reply" required rows={4} className={`${inputClasses} resize-y`} value={reply} onChange={(event) => setReply(event.target.value)} /></Field><div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><label className="flex cursor-pointer items-center gap-2 text-xs font-semibold text-ink-variant"><input type="checkbox" checked={internal} onChange={(event) => setInternal(event.target.checked)} className="accent-green" />{fr ? 'Visible uniquement par les admins' : 'Visible to admins only'}</label><Button type="submit" variant="secondary">{fr ? 'Ajouter' : 'Add'}</Button></div></form>
              <Field label={fr ? 'Notes internes du dossier' : 'Case internal notes'} htmlFor="case-notes"><textarea id="case-notes" rows={4} className={`${inputClasses} resize-y`} value={selected.internalNotes || ''} onChange={(event) => setSelected({ ...selected, internalNotes: event.target.value })} onBlur={() => void update({ internalNotes: selected.internalNotes })} /></Field>
            </div> : <EmptyState icon="ri-customer-service-line" title={fr ? 'Sélectionnez un dossier' : 'Select a case'} description={fr ? 'Choisissez une demande pour commencer son traitement.' : 'Choose a request to begin handling it.'} />}
          </main>
        </div>
      </section>
    </div>
  );
}

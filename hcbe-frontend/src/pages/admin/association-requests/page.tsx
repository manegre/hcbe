import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { associationsApi } from '../../../lib/api/associations';
import type { AssociationClaim } from '../../../lib/api/types';
import { AdminPageHeader } from '../../../components/admin/AdminPageHeader';
import { Button, EmptyState } from '../../../components/ui';

export default function AssociationRequestsPage() {
  const { t } = useTranslation();
  const [items, setItems] = useState<AssociationClaim[]>([]);
  const [busy, setBusy] = useState('');
  const load = async () => { const response = await associationsApi.getClaimsForAdmin(); setItems(response.data ?? []); };
  useEffect(() => { void load(); }, []);
  const review = async (id: string, status: 'Approved' | 'Rejected') => {
    const notes = window.prompt(t('admin.associationClaims.notePrompt')) ?? undefined;
    setBusy(id); await associationsApi.reviewClaim(id, status, notes); await load(); setBusy('');
  };
  return <div className="space-y-6">
    <AdminPageHeader title={t('admin.associationClaims.title')} subtitle={t('admin.associationClaims.subtitle')} icon="ri-building-2-line" count={items.filter((item) => item.status === 'Pending').length} />
    {items.length === 0 ? <EmptyState title={t('admin.associationClaims.emptyTitle')} description={t('admin.associationClaims.emptyDescription')} /> : <div className="grid gap-4 xl:grid-cols-2">{items.map((item) => <article key={item.id} className="rounded-2xl border border-line bg-surface p-5 shadow-[0_12px_32px_rgba(0,59,27,.05)]"><div className="flex items-start justify-between gap-4"><div><p className="text-[9px] font-bold uppercase tracking-[.14em] text-red-link">{t(`admin.associationClaims.status.${item.status}`)}</p><h2 className="mt-1 font-display text-xl font-bold text-green-deep">{item.associationName}</h2><p className="mt-1 text-sm text-ink-variant">{item.memberName} · {item.memberEmail}</p></div><span className="flex h-11 w-11 items-center justify-center rounded-xl bg-green/8 text-xl text-green"><i className="ri-community-line" /></span></div><p className="mt-4 rounded-xl bg-canvas/60 p-4 text-sm leading-6 text-ink-variant">{item.message}</p>{item.adminNotes && <p className="mt-3 text-xs text-ink-variant">{t('admin.associationClaims.note')}: {item.adminNotes}</p>}{item.status === 'Pending' && <div className="mt-5 flex justify-end gap-2"><Button type="button" variant="tertiary" disabled={busy === item.id} onClick={() => void review(item.id, 'Rejected')}>{t('admin.associationClaims.reject')}</Button><Button type="button" variant="secondary" disabled={busy === item.id} onClick={() => void review(item.id, 'Approved')}>{t('admin.associationClaims.approve')}</Button></div>}</article>)}</div>}
  </div>;
}

import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import Navbar from '../../../components/feature/Navbar';
import Footer from '../../../components/feature/Footer';
import { financeApi } from '../../../lib/api/finance';
import type { MembershipVerification } from '../../../lib/api/types';

export default function MembershipVerificationPage() {
  const { code = '' } = useParams(); const [data, setData] = useState<MembershipVerification | null>(null);
  useEffect(() => { financeApi.verifyMembership(code).then((response) => response.data && setData(response.data)).catch(() => setData({ isValid: false, status: 'Invalid', memberName: '', verificationCode: code })); }, [code]);
  return <div className="min-h-screen bg-canvas"><Navbar /><main className="container-page py-20"><section className="mx-auto max-w-2xl overflow-hidden rounded-[30px] border border-line bg-surface shadow-[0_25px_75px_rgba(0,45,22,.12)]"><div className={`p-10 text-center text-white ${data?.isValid ? 'bg-green-deep' : 'bg-red-link'}`}><i className={`${data?.isValid ? 'ri-shield-check-fill' : 'ri-shield-cross-fill'} text-6xl text-gold`} /><p className="mt-5 text-[10px] font-bold uppercase tracking-[.2em] text-white/65">Vérification HCBE Canada</p><h1 className="mt-2 font-display text-4xl font-bold text-white">{!data ? 'Vérification…' : data.isValid ? 'Adhésion valide' : 'Adhésion non valide'}</h1></div>{data && <dl className="grid grid-cols-[150px_1fr] p-7 text-sm sm:p-10"><dt className="border-b border-line py-4 text-[10px] font-bold uppercase tracking-[.12em] text-ink-variant">Membre</dt><dd className="border-b border-line py-4 font-semibold text-green-deep">{data.memberName || '—'}</dd><dt className="border-b border-line py-4 text-[10px] font-bold uppercase tracking-[.12em] text-ink-variant">Formule</dt><dd className="border-b border-line py-4">{data.planName || '—'}</dd><dt className="border-b border-line py-4 text-[10px] font-bold uppercase tracking-[.12em] text-ink-variant">Statut</dt><dd className="border-b border-line py-4">{data.status}</dd><dt className="py-4 text-[10px] font-bold uppercase tracking-[.12em] text-ink-variant">Valide jusqu’au</dt><dd className="py-4">{data.validUntilUtc ? new Date(data.validUntilUtc).toLocaleDateString('fr-CA') : '—'}</dd></dl>}</section></main><Footer /></div>;
}

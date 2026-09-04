import { useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '../../../components/ui';
import { privacyApi } from '../../../lib/api/privacy';
import type { PrivacyRequest } from '../../../lib/api/types';

interface MemberPrivacyPanelProps {
  fr: boolean;
  withdrawing: boolean;
  onWithdrawOptional: () => Promise<void>;
}

export default function MemberPrivacyPanel({ fr, withdrawing, onWithdrawOptional }: MemberPrivacyPanelProps) {
  const [request, setRequest] = useState<PrivacyRequest | null>(null);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);
  const [acting, setActing] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [confirmation, setConfirmation] = useState('');
  const [notice, setNotice] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);
  const confirmationWord = fr ? 'SUPPRIMER' : 'DELETE';

  useEffect(() => {
    void privacyApi.getDeletionRequest()
      .then((response) => setRequest(response.data ?? null))
      .catch(() => setNotice({ tone: 'error', text: fr ? 'Impossible de charger l’état de votre compte.' : 'Unable to load your account status.' }))
      .finally(() => setLoading(false));
  }, [fr]);

  const exportData = async () => {
    setExporting(true);
    setNotice(null);
    try {
      const { blob, fileName } = await privacyApi.exportData();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = fileName ?? `hcbe-data-export-${new Date().toISOString().slice(0, 10)}.json`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
      setNotice({ tone: 'success', text: fr ? 'Votre copie de données a été téléchargée.' : 'Your data copy has been downloaded.' });
    } catch (error) {
      setNotice({ tone: 'error', text: error instanceof Error ? error.message : (fr ? 'Le téléchargement a échoué.' : 'The download failed.') });
    } finally {
      setExporting(false);
    }
  };

  const requestDeletion = async () => {
    setActing(true);
    setNotice(null);
    try {
      const response = await privacyApi.requestDeletion();
      if (!response.success || !response.data) throw new Error(response.message || 'Request failed');
      setRequest(response.data);
      setConfirming(false);
      setConfirmation('');
      setNotice({ tone: 'success', text: fr ? 'Votre demande de suppression est enregistrée.' : 'Your deletion request has been recorded.' });
    } catch (error) {
      setNotice({ tone: 'error', text: error instanceof Error ? error.message : (fr ? 'La demande a échoué.' : 'The request failed.') });
    } finally {
      setActing(false);
    }
  };

  const cancelDeletion = async () => {
    setActing(true);
    setNotice(null);
    try {
      const response = await privacyApi.cancelDeletion();
      if (!response.success) throw new Error(response.message || 'Request failed');
      setRequest((current) => current ? { ...current, status: 'Cancelled', cancelledAtUtc: new Date().toISOString() } : null);
      setNotice({ tone: 'success', text: fr ? 'La suppression a été annulée. Vos communications restent désactivées jusqu’à ce que vous les réactiviez.' : 'Deletion was cancelled. Your communications remain off until you turn them back on.' });
    } catch (error) {
      setNotice({ tone: 'error', text: error instanceof Error ? error.message : (fr ? 'L’annulation a échoué.' : 'Cancellation failed.') });
    } finally {
      setActing(false);
    }
  };

  const pending = request?.status === 'Pending';
  const executeDate = pending
    ? new Intl.DateTimeFormat(fr ? 'fr-CA' : 'en-CA', { dateStyle: 'long', timeStyle: 'short' }).format(new Date(request.executeAfterUtc))
    : null;

  return (
    <section className="overflow-hidden rounded-[26px] border border-line bg-surface shadow-[0_16px_45px_rgba(0,59,27,.06)]" aria-labelledby="privacy-centre-title">
      <header className="relative overflow-hidden border-b border-line bg-green-deep p-6 text-white sm:px-8 sm:py-7">
        <div className="absolute -right-12 -top-20 h-44 w-44 rounded-full border-[28px] border-gold/[.08]" />
        <div className="relative max-w-2xl">
          <p className="text-[9px] font-bold uppercase tracking-[.18em] text-gold">{fr ? 'Loi 25 · Vos choix' : 'Law 25 · Your choices'}</p>
          <h3 id="privacy-centre-title" className="mt-2 font-display text-2xl font-bold text-white sm:text-3xl">{fr ? 'Confidentialité et données personnelles' : 'Privacy and personal data'}</h3>
          <p className="mt-3 text-sm leading-6 text-green-dim">{fr ? 'Téléchargez vos informations, retirez vos consentements ou demandez la suppression de votre compte depuis un seul endroit.' : 'Download your information, withdraw consent, or request account deletion from one place.'}</p>
        </div>
      </header>

      <div className="p-6 sm:p-8">
        <div className="grid gap-4 lg:grid-cols-3">
          <PrivacyAction icon="ri-download-cloud-2-line" eyebrow={fr ? 'Accès et portabilité' : 'Access and portability'} title={fr ? 'Télécharger mes données' : 'Download my data'} body={fr ? 'Recevez une copie JSON structurée des données liées à votre compte.' : 'Get a structured JSON copy of the data associated with your account.'}>
            <Button type="button" variant="secondary" onClick={exportData} disabled={exporting} className="w-full justify-center">
              <i className={exporting ? 'ri-loader-4-line animate-spin' : 'ri-download-line'} aria-hidden="true" />{exporting ? (fr ? 'Préparation…' : 'Preparing…') : (fr ? 'Télécharger' : 'Download')}
            </Button>
          </PrivacyAction>

          <PrivacyAction icon="ri-notification-off-line" eyebrow={fr ? 'Retrait du consentement' : 'Withdraw consent'} title={fr ? 'Refuser les communications' : 'Opt out of communications'} body={fr ? 'Désactivez les courriels facultatifs et les notifications. Les messages essentiels de sécurité ou de service peuvent toujours être envoyés.' : 'Turn off optional emails and notifications. Essential security or service messages may still be sent.'}>
            <Button type="button" variant="secondary" onClick={onWithdrawOptional} disabled={withdrawing} className="w-full justify-center">
              <i className={withdrawing ? 'ri-loader-4-line animate-spin' : 'ri-forbid-2-line'} aria-hidden="true" />{fr ? 'Tout désactiver' : 'Turn all off'}
            </Button>
          </PrivacyAction>

          <PrivacyAction icon="ri-edit-circle-line" eyebrow={fr ? 'Rectification' : 'Correction'} title={fr ? 'Corriger mon profil' : 'Correct my profile'} body={fr ? 'Mettez à jour vos coordonnées et contrôlez séparément votre visibilité dans l’annuaire privé.' : 'Update your details and separately control your private-directory visibility.'}>
            <Link to="/espace-membre?section=profile" className="inline-flex min-h-11 w-full items-center justify-center gap-2 rounded-control border-2 border-green px-5 text-label-md uppercase text-green transition-colors hover:bg-green hover:text-white">
              <i className="ri-user-settings-line" aria-hidden="true" />{fr ? 'Modifier mon profil' : 'Edit my profile'}
            </Link>
          </PrivacyAction>
        </div>

        <div className="mt-6 rounded-[22px] border border-red-link/20 bg-red-link/[.035] p-5 sm:p-6">
          <div className="flex flex-col gap-5 md:flex-row md:items-start md:justify-between">
            <div className="max-w-2xl">
              <p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{fr ? 'Suppression du compte' : 'Account deletion'}</p>
              <h4 className="mt-2 font-display text-xl font-bold text-green-deep">{pending ? (fr ? 'Suppression programmée' : 'Deletion scheduled') : (fr ? 'Quitter la communauté et supprimer mes données' : 'Leave the community and delete my data')}</h4>
              <p className="mt-2 text-sm leading-6 text-ink-variant">
                {pending
                  ? (fr ? `Votre compte sera désactivé et vos données personnelles anonymisées le ${executeDate}. Vous pouvez annuler jusque-là.` : `Your account will be disabled and your personal data anonymized on ${executeDate}. You can cancel until then.`)
                  : (fr ? 'Une période de grâce de 30 jours vous permet d’annuler. À l’échéance, le compte est désactivé, les sessions sont révoquées et les renseignements personnels sont supprimés ou anonymisés, sauf conservation requise par la loi ou nécessaire à la sécurité.' : 'A 30-day grace period lets you cancel. At the end, the account is disabled, sessions are revoked, and personal information is deleted or anonymized, except where retention is legally required or needed for security.')}
              </p>
            </div>
            {pending ? (
              <Button type="button" variant="secondary" onClick={cancelDeletion} disabled={acting} className="shrink-0">
                <i className={acting ? 'ri-loader-4-line animate-spin' : 'ri-arrow-go-back-line'} aria-hidden="true" />{fr ? 'Annuler la suppression' : 'Cancel deletion'}
              </Button>
            ) : (
              <Button type="button" variant="destructive" onClick={() => setConfirming(true)} disabled={loading || acting} className="shrink-0">
                <i className="ri-delete-bin-6-line" aria-hidden="true" />{fr ? 'Demander la suppression' : 'Request deletion'}
              </Button>
            )}
          </div>

          {confirming && !pending && (
            <div className="mt-5 border-t border-red-link/15 pt-5">
              <label htmlFor="delete-account-confirmation" className="block text-xs font-semibold leading-5 text-green-deep">
                {fr ? <>Cette action affectera tout votre espace membre. Tapez <strong>{confirmationWord}</strong> pour confirmer.</> : <>This affects your entire member space. Type <strong>{confirmationWord}</strong> to confirm.</>}
              </label>
              <div className="mt-3 flex flex-col gap-3 sm:flex-row">
                <input id="delete-account-confirmation" value={confirmation} onChange={(event) => setConfirmation(event.target.value)} autoComplete="off" className="min-h-11 flex-1 rounded-control border border-line bg-canvas px-4 text-sm outline-none focus:border-red-link focus:ring-2 focus:ring-red-link/10" />
                <Button type="button" variant="destructive" onClick={requestDeletion} disabled={acting || confirmation !== confirmationWord}>
                  <i className={acting ? 'ri-loader-4-line animate-spin' : 'ri-delete-bin-line'} aria-hidden="true" />{fr ? 'Confirmer la demande' : 'Confirm request'}
                </Button>
                <Button type="button" variant="secondary" onClick={() => { setConfirming(false); setConfirmation(''); }} disabled={acting}>{fr ? 'Annuler' : 'Cancel'}</Button>
              </div>
            </div>
          )}
        </div>

        {notice && <p role="status" className={`mt-5 rounded-xl border px-4 py-3 text-sm ${notice.tone === 'success' ? 'border-green/15 bg-green/5 text-green' : 'border-red-link/20 bg-red-link/5 text-red-link'}`}>{notice.text}</p>}

        <div className="mt-6 flex flex-col gap-3 border-t border-line pt-5 text-xs leading-5 text-ink-variant sm:flex-row sm:items-center sm:justify-between">
          <p>{fr ? 'Responsable de la protection des renseignements personnels : HCBE Canada · contact@hcbe.ca' : 'Privacy Officer: HCBE Canada · contact@hcbe.ca'}</p>
          <Link to="/confidentialite" className="inline-flex items-center gap-2 font-semibold text-green hover:text-red-link">{fr ? 'Lire la politique de confidentialité' : 'Read the privacy policy'}<i className="ri-arrow-right-line" aria-hidden="true" /></Link>
        </div>
      </div>
    </section>
  );
}

function PrivacyAction({ icon, eyebrow, title, body, children }: { icon: string; eyebrow: string; title: string; body: string; children: ReactNode }) {
  return (
    <article className="flex min-h-[250px] flex-col rounded-[22px] border border-line bg-canvas/55 p-5">
      <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-green/10 text-xl text-green"><i className={icon} aria-hidden="true" /></span>
      <p className="mt-5 text-[9px] font-bold uppercase tracking-[.14em] text-red-link">{eyebrow}</p>
      <h4 className="mt-2 font-display text-xl font-bold text-green-deep">{title}</h4>
      <p className="mt-2 flex-1 text-sm leading-6 text-ink-variant">{body}</p>
      <div className="mt-5">{children}</div>
    </article>
  );
}

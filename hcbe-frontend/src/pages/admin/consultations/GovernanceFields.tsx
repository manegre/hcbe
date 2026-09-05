import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../../../components/ui';
import type { ConsultationOptionRequest, CreateConsultationRequest } from '../../../lib/api/types';

export type GovernanceForm = Pick<CreateConsultationRequest,
  'governanceType' | 'opensAtUtc' | 'closesAtUtc' | 'commentClosesAtUtc' | 'votingMode' |
  'eligibilityRule' | 'quorumPercentage' | 'minimumParticipation' | 'allowComments' | 'options'>;

interface Props {
  value: GovernanceForm;
  onChange: <K extends keyof GovernanceForm>(field: K, value: GovernanceForm[K]) => void;
}

export const normalizeGovernanceDates = <T extends GovernanceForm>(value: T): T => ({
  ...value,
  opensAtUtc: value.opensAtUtc ? new Date(value.opensAtUtc).toISOString() : undefined,
  closesAtUtc: value.closesAtUtc ? new Date(value.closesAtUtc).toISOString() : undefined,
  commentClosesAtUtc: value.commentClosesAtUtc ? new Date(value.commentClosesAtUtc).toISOString() : undefined,
});

export const toDateTimeInput = (value?: string) => value ? value.slice(0, 16) : '';

const GovernanceFields = ({ value, onChange }: Props) => {
  const { t } = useTranslation();
  const options = value.options || [];
  const needsOptions = value.governanceType === 'Survey' || value.governanceType === 'Vote';

  const updateOption = (index: number, patch: Partial<ConsultationOptionRequest>) => {
    onChange('options', options.map((option, optionIndex) => optionIndex === index ? { ...option, ...patch } : option));
  };

  return (
    <section className="mt-9 border-t border-line pt-7">
      <div className="mb-6 flex items-start gap-3">
        <span className="flex h-10 w-10 items-center justify-center rounded-full bg-gold text-green"><i className="ri-government-line" aria-hidden="true" /></span>
        <div><h2 className="font-display text-headline-sm text-green">{t('admin.consultations.governance.title')}</h2><p className="mt-1 text-body-sm text-ink-variant">{t('admin.consultations.governance.subtitle')}</p></div>
      </div>
      <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
        <Field label={t('admin.consultations.governance.type')} htmlFor="governanceType"><select id="governanceType" className={inputClasses} value={value.governanceType || 'Information'} onChange={event => onChange('governanceType', event.target.value as GovernanceForm['governanceType'])}>{['Information', 'Survey', 'Proposal', 'Vote'].map(type => <option key={type} value={type}>{t(`admin.consultations.governance.typeValue.${type}`)}</option>)}</select></Field>
        <Field label={t('admin.consultations.governance.eligibility')} htmlFor="eligibilityRule"><select id="eligibilityRule" className={inputClasses} value={value.eligibilityRule || 'ActiveMembers'} onChange={event => onChange('eligibilityRule', event.target.value as GovernanceForm['eligibilityRule'])}>{['AllMembers', 'ActiveMembers', 'Administrators'].map(rule => <option key={rule} value={rule}>{t(`admin.consultations.governance.eligibilityValue.${rule}`)}</option>)}</select></Field>
        <Field label={t('admin.consultations.governance.opens')} htmlFor="opensAtUtc"><input id="opensAtUtc" type="datetime-local" className={inputClasses} value={toDateTimeInput(value.opensAtUtc)} onChange={event => onChange('opensAtUtc', event.target.value || undefined)} /></Field>
        <Field label={t('admin.consultations.governance.closes')} htmlFor="closesAtUtc"><input id="closesAtUtc" type="datetime-local" className={inputClasses} value={toDateTimeInput(value.closesAtUtc)} onChange={event => onChange('closesAtUtc', event.target.value || undefined)} /></Field>
        <Field label={t('admin.consultations.governance.commentCloses')} htmlFor="commentClosesAtUtc"><input id="commentClosesAtUtc" type="datetime-local" className={inputClasses} value={toDateTimeInput(value.commentClosesAtUtc)} onChange={event => onChange('commentClosesAtUtc', event.target.value || undefined)} /></Field>
        <Field label={t('admin.consultations.governance.mode')} htmlFor="votingMode"><select id="votingMode" className={inputClasses} value={value.votingMode || 'Named'} onChange={event => onChange('votingMode', event.target.value as GovernanceForm['votingMode'])}><option value="Named">{t('admin.consultations.governance.modeValue.Named')}</option><option value="Anonymous">{t('admin.consultations.governance.modeValue.Anonymous')}</option></select></Field>
        <Field label={t('admin.consultations.governance.quorum')} htmlFor="quorumPercentage"><input id="quorumPercentage" type="number" min={0} max={100} className={inputClasses} value={value.quorumPercentage || 0} onChange={event => onChange('quorumPercentage', Number(event.target.value))} /></Field>
        <Field label={t('admin.consultations.governance.minimum')} htmlFor="minimumParticipation"><input id="minimumParticipation" type="number" min={0} className={inputClasses} value={value.minimumParticipation || 0} onChange={event => onChange('minimumParticipation', Number(event.target.value))} /></Field>
        <label className="flex min-h-[52px] cursor-pointer items-center gap-3 rounded-[12px] border border-line px-4 md:col-span-2"><input type="checkbox" checked={value.allowComments || false} onChange={event => onChange('allowComments', event.target.checked)} className="h-5 w-5 accent-green" /><span className="text-body-md text-ink">{t('admin.consultations.governance.comments')}</span></label>
      </div>

      {needsOptions && <div className="mt-7 rounded-[16px] border border-line bg-surface-container p-5"><div className="flex flex-wrap items-center justify-between gap-4"><div><h3 className="font-display text-headline-sm text-green">{t('admin.consultations.governance.options')}</h3><p className="text-body-sm text-ink-variant">{t('admin.consultations.governance.optionsHint')}</p></div><Button type="button" variant="secondary" onClick={() => onChange('options', [...options, { label: '', labelEn: '' }])}>{t('admin.consultations.governance.addOption')}</Button></div><div className="mt-5 space-y-4">{options.map((option, index) => <div key={index} className="grid gap-3 md:grid-cols-[1fr_1fr_auto]"><input aria-label={`${t('admin.consultations.governance.optionFr')} ${index + 1}`} placeholder={t('admin.consultations.governance.optionFr')} value={option.label} onChange={event => updateOption(index, { label: event.target.value })} className={inputClasses} required /><input aria-label={`${t('admin.consultations.governance.optionEn')} ${index + 1}`} placeholder={t('admin.consultations.governance.optionEn')} value={option.labelEn || ''} onChange={event => updateOption(index, { labelEn: event.target.value })} className={inputClasses} /><Button type="button" variant="ghost" onClick={() => onChange('options', options.filter((_, optionIndex) => optionIndex !== index))} aria-label={t('admin.consultations.governance.removeOption')}><i className="ri-delete-bin-line" /></Button></div>)}</div></div>}
    </section>
  );
};

export default GovernanceFields;

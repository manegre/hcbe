interface AdminStatCardProps {
  value: number | string;
  label: string;
  icon: string;
  tone?: 'green' | 'gold' | 'red' | 'neutral';
}

const toneClasses = {
  green: 'bg-green/8 text-green',
  gold: 'bg-gold/15 text-gold-ink',
  red: 'bg-red/8 text-red-link',
  neutral: 'bg-surface-container text-ink-variant',
};

export const AdminStatCard = ({ value, label, icon, tone = 'green' }: AdminStatCardProps) => (
  <div className="admin-panel group flex min-h-[116px] items-center justify-between gap-4 overflow-hidden p-5 transition-transform duration-200 hover:-translate-y-0.5">
    <div>
      <p className="font-display text-[34px] font-bold leading-none tabular-nums text-green-deep">{value}</p>
      <p className="mt-2 text-[9px] font-bold uppercase tracking-[0.14em] text-ink-variant/75">{label}</p>
    </div>
    <span className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl ${toneClasses[tone]}`}>
      <i className={`${icon} text-lg`} aria-hidden="true" />
    </span>
  </div>
);

import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

interface AdminBackButtonProps {
  to: string;
  label?: string;
}

export const AdminBackButton = ({ to, label }: AdminBackButtonProps) => {
  const { t } = useTranslation();

  return (
    <Link
      to={to}
      className="group inline-flex min-h-[36px] items-center gap-2 rounded-full border border-line/55 bg-surface/65 px-3.5 text-[9px] font-bold uppercase tracking-[0.15em] text-green transition-all hover:border-gold/70 hover:text-green-deep"
    >
      <i className="ri-arrow-left-line text-sm transition-transform group-hover:-translate-x-0.5" aria-hidden="true"></i>
      {label ?? t('admin.common.backToList')}
    </Link>
  );
};

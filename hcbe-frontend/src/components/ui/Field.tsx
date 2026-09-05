import type { ReactNode } from 'react';

// `focus:border-2` remplaçait la bordure de 1px par une de 2px au focus, ce qui
// décalait le contenu du champ d'un pixel. Le liseré vient désormais d'un
// `outline` posé en retrait : même emphase, aucun déplacement.
export const inputClasses =
  'w-full min-h-[48px] rounded-[14px] border border-outline/85 bg-surface px-4 py-2.5 text-body-md text-ink shadow-[0_1px_2px_rgba(0,59,27,.025)] transition-[border-color,box-shadow,background-color] duration-200 placeholder:text-ink-variant/55 hover:border-green/45 focus:border-green focus:outline-none focus:ring-2 focus:ring-green/15 disabled:cursor-not-allowed disabled:bg-surface-container disabled:opacity-65';

interface FieldProps {
  label: string;
  htmlFor: string;
  required?: boolean;
  error?: string;
  hint?: string;
  className?: string;
  children: ReactNode;
}

export const Field = ({ label, htmlFor, required, error, hint, className = '', children }: FieldProps) => (
  <div className={`admin-field flex min-w-0 flex-col gap-2.5 ${className}`}>
    <label htmlFor={htmlFor} className="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-[.13em] text-green-deep/75 dark:text-green-dim">
      {label}
      {required && <span className="ml-1 text-red-link">*</span>}
    </label>
    {children}
    {hint && !error && <p className="flex items-start gap-1.5 text-xs leading-5 text-ink-variant"><i className="ri-information-line mt-0.5 shrink-0 text-green/70" aria-hidden="true" />{hint}</p>}
    {error && <p className="flex items-start gap-1.5 text-xs leading-5 text-error" role="alert"><i className="ri-error-warning-line mt-0.5 shrink-0" aria-hidden="true" />{error}</p>}
  </div>
);

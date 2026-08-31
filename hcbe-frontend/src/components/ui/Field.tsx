import type { ReactNode } from 'react';

// `focus:border-2` remplaçait la bordure de 1px par une de 2px au focus, ce qui
// décalait le contenu du champ d'un pixel. Le liseré vient désormais d'un
// `outline` posé en retrait : même emphase, aucun déplacement.
export const inputClasses =
  'w-full min-h-[44px] rounded-control border border-outline bg-surface px-4 py-2 text-body-md text-ink transition-colors duration-200 placeholder:text-ink-variant/60 hover:border-ink-variant focus:border-green focus:outline focus:outline-2 focus:-outline-offset-2 focus:outline-green disabled:cursor-not-allowed disabled:opacity-60';

interface FieldProps {
  label: string;
  htmlFor: string;
  required?: boolean;
  error?: string;
  hint?: string;
  children: ReactNode;
}

export const Field = ({ label, htmlFor, required, error, hint, children }: FieldProps) => (
  <div className="flex flex-col gap-2">
    <label htmlFor={htmlFor} className="text-label-md uppercase text-ink-variant">
      {label}
      {required && <span className="ml-1 text-red-link">*</span>}
    </label>
    {children}
    {hint && !error && <p className="text-body-md text-ink-variant">{hint}</p>}
    {error && <p className="text-body-md text-error">{error}</p>}
  </div>
);

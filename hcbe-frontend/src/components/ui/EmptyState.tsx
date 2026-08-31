import type { ReactNode } from 'react';

interface EmptyStateProps {
  icon?: string;
  title: string;
  description?: string;
  action?: ReactNode;
  tone?: 'empty' | 'error';
}

export const EmptyState = ({
  icon = 'ri-inbox-line',
  title,
  description,
  action,
  tone = 'empty',
}: EmptyStateProps) => (
  <div className={`border bg-surface px-6 py-16 text-center ${tone === 'error' ? 'border-error' : 'border-line'}`}>
    <span
      className={`mx-auto mb-6 flex h-14 w-14 items-center justify-center border bg-surface-container text-2xl ${
        tone === 'error' ? 'border-error text-error' : 'border-line text-ink-variant'
      }`}
    >
      <i className={icon} aria-hidden="true"></i>
    </span>
    <p className={`font-display text-headline-md ${tone === 'error' ? 'text-error' : 'text-green'}`}>{title}</p>
    {description && <p className="mx-auto mt-3 max-w-xl text-body-md text-ink-variant">{description}</p>}
    {action && <div className="mt-8 flex justify-center">{action}</div>}
  </div>
);

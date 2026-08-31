import type { ReactNode } from 'react';

export const Tag = ({ children, className = '' }: { children: ReactNode; className?: string }) => (
  <span className={`inline-flex items-center rounded-control border border-line px-3 py-1 text-body-md text-ink-variant ${className}`}>
    {children}
  </span>
);

const statuses = {
  pending: 'border-gold text-gold-ink',
  approved: 'border-green text-green',
  published: 'border-green text-green',
  rejected: 'border-error text-error',
  draft: 'border-outline text-ink-variant',
  past: 'border-outline text-ink-variant',
} as const;

interface StatusChipProps {
  status: keyof typeof statuses;
  label: string;
}

export const StatusChip = ({ status, label }: StatusChipProps) => (
  <span className={`inline-flex items-center rounded-control border px-3 py-1 text-label-md uppercase ${statuses[status]}`}>
    {label}
  </span>
);

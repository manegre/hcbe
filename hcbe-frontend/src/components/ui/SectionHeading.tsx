import type { ReactNode } from 'react';

interface SectionHeadingProps {
  title: string;
  description?: string;
  action?: ReactNode;
}

export const SectionHeading = ({ title, description, action }: SectionHeadingProps) => (
  <div className="mb-12">
    <div className="mb-4 flex items-center gap-3" aria-hidden="true"><span className="h-0.5 w-10 bg-gold" /><span className="h-1.5 w-1.5 rounded-full bg-red" /></div>
    <div className="flex flex-wrap items-end justify-between gap-4">
      <h2 className="max-w-3xl font-display text-headline-lg text-green-deep md:text-[36px] md:leading-[1.15]">{title}</h2>
      {action}
    </div>
    {description && <p className="mt-4 max-w-3xl text-body-md text-ink-variant">{description}</p>}
  </div>
);

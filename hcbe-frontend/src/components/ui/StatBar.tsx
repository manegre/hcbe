import type { ReactNode } from 'react';

interface StatBarProps {
  items: { value: string | ReactNode; label: string }[];
}

export const StatBar = ({ items }: StatBarProps) => (
  <section className="relative z-10 bg-background">
    <div className="container-page -translate-y-8">
      <div className="grid grid-cols-2 overflow-hidden rounded-[18px] border border-green/10 bg-white shadow-[0_22px_55px_rgba(0,59,27,.13)] md:grid-cols-4">
        {items.map((item, index) => (
          <div key={item.label} className="group relative flex min-h-[116px] flex-col justify-center px-5 py-5 text-left sm:px-7">
            {index > 0 && <span className="absolute inset-y-6 left-0 hidden w-px bg-green/10 md:block" aria-hidden="true" />}
            <span className="absolute right-4 top-3 font-display text-[42px] font-bold leading-none text-green/[0.035]" aria-hidden="true">0{index + 1}</span>
            <p className="relative font-display text-[30px] font-bold leading-none text-green-deep transition-transform duration-300 group-hover:-translate-y-0.5">{item.value}</p>
            <p className="relative mt-3 max-w-[170px] text-[10px] font-bold uppercase leading-4 tracking-[0.14em] text-ink-variant">
              {item.label}
            </p>
            <span className="absolute inset-x-0 bottom-0 h-0.5 origin-left scale-x-0 bg-gold transition-transform duration-300 group-hover:scale-x-100" aria-hidden="true" />
          </div>
        ))}
      </div>
    </div>
  </section>
);

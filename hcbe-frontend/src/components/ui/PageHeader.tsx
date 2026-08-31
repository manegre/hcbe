import type { ReactNode } from 'react';

interface PageHeaderProps {
  title: string;
  description?: string;
  variant?: 'hero' | 'interior';
  align?: 'left' | 'center';
  actions?: ReactNode;
  aside?: ReactNode;
  bare?: boolean;
  immersive?: boolean;
}

export const PageHeader = ({
  title,
  description,
  variant = 'interior',
  align = 'left',
  actions,
  aside,
  bare = false,
  immersive = false,
}: PageHeaderProps) => {
  if (variant === 'hero') {
    const content =
      align === 'center' ? (
        <div className="container-page flex min-h-[420px] flex-col items-center justify-center py-20 text-center md:min-h-[520px] md:py-28">
          <div className="public-header-enter max-w-4xl">
            <span className="mb-6 inline-flex items-center gap-2 rounded-full border border-white/20 bg-white/10 px-4 py-2 text-[10px] font-bold uppercase tracking-[0.18em] text-white backdrop-blur-md"><span className="h-1.5 w-1.5 rounded-full bg-gold" /> HCBE Canada</span>
            <h1 className="hero-title font-display text-[38px] font-bold leading-[1.06] tracking-[-0.03em] text-white md:text-[62px]">{title}</h1>
            {description && (
              <>
                <div className="mx-auto mt-7 h-0.5 w-14 bg-gold" aria-hidden="true"></div>
                <p className="hero-standfirst mx-auto mt-5 max-w-3xl text-body-lg text-white/90">{description}</p>
              </>
            )}
            {actions && (
              <div className="hero-actions mt-10 flex flex-wrap items-center justify-center gap-6">{actions}</div>
            )}
          </div>
          {aside && <div className="mt-10 w-full max-w-3xl">{aside}</div>}
        </div>
      ) : (
        <div className={`container-page public-header-enter grid grid-cols-1 content-center gap-gutter lg:grid-cols-12 ${immersive ? 'min-h-[500px] py-20 md:min-h-[570px]' : 'min-h-[340px] py-16 md:min-h-[390px] md:py-20'}`}>
          <div className="lg:col-span-8 xl:col-span-7">
            <span className="mb-6 inline-flex items-center gap-2 rounded-full border border-white/20 bg-white/10 px-4 py-2 text-[10px] font-bold uppercase tracking-[0.18em] text-white backdrop-blur-md"><span className="h-1.5 w-1.5 rounded-full bg-gold" /> HCBE Canada</span>
            <h1 className={`hero-title max-w-4xl font-display font-bold leading-[1.03] tracking-[-0.035em] text-white ${immersive ? 'text-[40px] md:text-[60px] lg:text-[68px]' : 'text-[36px] md:text-[50px] lg:text-[56px]'}`}>{title}</h1>
            {description && (
              <p className="hero-standfirst mt-7 max-w-2xl border-l-2 border-gold pl-5 text-[17px] leading-7 text-white/80">
                {description}
              </p>
            )}
            {actions && <div className="hero-actions mt-9 flex flex-wrap items-center gap-5">{actions}</div>}
          </div>
          {aside && <div className="lg:col-span-5 lg:col-start-8">{aside}</div>}
        </div>
      );

    if (bare) return content;

    return <section className="public-grid-pattern relative overflow-hidden border-b border-green bg-green-deep">{content}</section>;
  }

  return (
    <section className="public-grid-pattern relative overflow-hidden bg-green-deep py-16 md:py-20">
      <div className="pointer-events-none absolute -right-20 -top-28 h-72 w-72 rounded-full border-[54px] border-white/[0.035]" aria-hidden="true" />
      <div className="container-page public-header-enter relative">
        <div className="mb-5 flex items-center gap-3"><span className="h-0.5 w-9 bg-gold" /><span className="text-[10px] font-bold uppercase tracking-[0.18em] text-gold">HCBE Canada</span></div>
        <h1 className="max-w-4xl font-display text-headline-xl-m text-white md:text-[48px] md:leading-[1.1]">{title}</h1>
        {description && <p className="mt-5 max-w-3xl text-body-lg text-white/75">{description}</p>}
        {actions && <div className="mt-8 flex flex-wrap items-center gap-6">{actions}</div>}
      </div>
    </section>
  );
};

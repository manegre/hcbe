import { Button } from '../../../components/ui';

const CTASection = () => {
  const { t } = useTranslation();

  return (
    <section className="relative bg-background py-16 md:py-20">
      <div className="container-page">
        <div className="public-grid-pattern relative overflow-hidden rounded-[24px] bg-green-deep px-6 py-8 shadow-[0_22px_55px_rgba(0,59,27,.16)] sm:px-9 md:flex md:items-center md:justify-between md:gap-10 lg:px-12">
          <div className="pointer-events-none absolute -right-16 -top-24 h-64 w-64 rounded-full border-[48px] border-gold/[0.08]" aria-hidden="true" />
          <div className="relative md:max-w-2xl">
            <p className="text-[10px] font-bold uppercase tracking-[0.18em] text-gold">{t('public.home.cta.label')}</p>
            <h2 className="mt-2 font-display text-[26px] font-bold leading-tight text-white md:text-[32px]">{t('public.home.cta.title')}</h2>
            <p className="mt-3 max-w-xl text-[15px] leading-6 text-white/70">{t('public.home.cta.subtitle')}</p>
          </div>
          <div className="relative mt-6 flex shrink-0 flex-wrap gap-3 md:mt-0 md:justify-end">
          <Button to="/espace-membre" variant="primary">
            {t('public.home.cta.member')}
          </Button>
          <Button
            to="/contact"
            variant="secondary"
            className="border-white text-white hover:bg-white hover:text-green"
          >
            {t('public.home.cta.contact')}
          </Button>
          </div>
        </div>
      </div>
    </section>
  );
};

export default CTASection;

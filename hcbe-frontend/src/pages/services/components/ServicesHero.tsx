import { PageHeader } from '../../../components/ui';

const ServicesHero = () => {
  const { t } = useTranslation();

  return (
    <PageHeader
      variant="hero"
      title={t('public.services.hero.title')}
      description={t('public.services.hero.subtitle')}
    />
  );
};

export default ServicesHero;

import { useTranslation } from 'react-i18next';
import { ArrowLink, Card, Reveal, SectionHeading } from '../../../components/ui';

// Les clés i18n restent écrites en toutes lettres : elles doivent rester
// trouvables au grep, la règle anti-doublon de `src/i18n/local/` en dépend.
const domains = [
  {
    id: 'documents',
    icon: 'ri-file-text-line',
    to: '/services/documents-officiels',
    accent: 'red',
    titleKey: 'public.home.mission.documents.title',
    descriptionKey: 'public.home.mission.documents.description',
    linkKey: 'public.home.mission.documents.link',
  },
  {
    id: 'comites',
    icon: 'ri-team-line',
    to: '/services/comites',
    accent: 'gold',
    titleKey: 'public.home.mission.comites.title',
    descriptionKey: 'public.home.mission.comites.description',
    linkKey: 'public.home.mission.comites.link',
  },
  {
    id: 'bourses',
    icon: 'ri-graduation-cap-line',
    to: '/services/bourses',
    accent: 'green',
    titleKey: 'public.home.mission.bourses.title',
    descriptionKey: 'public.home.mission.bourses.description',
    linkKey: 'public.home.mission.bourses.link',
  },
] as const;

// L'accent ne colore que la pastille d'icône et la bordure au survol. Le lien
// d'action reste rouge partout : trois cartes sœurs avec trois couleurs
// d'action différentes se lisent comme un accident, pas comme un système.
const accents: Record<(typeof domains)[number]['accent'], string> = {
  red: 'border-red text-red',
  gold: 'border-gold text-gold-ink',
  green: 'border-green text-green',
};

const MissionVisionSection = () => {
  const { t } = useTranslation();

  return (
    <section className="relative overflow-hidden bg-background py-24">
      <div className="pointer-events-none absolute -left-32 top-10 h-80 w-80 rounded-full bg-gold/[0.08] blur-3xl" aria-hidden="true" />
      <div className="container-page">
        <SectionHeading title={t('public.home.mission.sectionTitle')} />

        <div className="grid grid-cols-1 gap-gutter md:grid-cols-3">
          {domains.map((domain, index) => (
            <Reveal key={domain.id} delay={index * 80} className="h-full">
              {/* `h-full` + `mt-auto` sur le lien : sans eux les cartes ne sont
                  hautes que de leur contenu et les liens se désalignent dès que
                  les descriptions n'ont pas la même longueur. */}
              <Card hover={domain.accent} className="flex h-full flex-col">
                <span
                  className={`mb-6 flex h-12 w-12 shrink-0 items-center justify-center rounded-xl border bg-white text-2xl ${accents[domain.accent]}`}
                >
                  <i className={domain.icon} aria-hidden="true"></i>
                </span>
                <h3 className="font-display text-headline-md text-ink">{t(domain.titleKey)}</h3>
                <p className="mt-4 text-body-md text-ink-variant">{t(domain.descriptionKey)}</p>
                <ArrowLink to={domain.to} tone="red" className="mt-auto pt-8">
                  {t(domain.linkKey)}
                </ArrowLink>
              </Card>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
};

export default MissionVisionSection;

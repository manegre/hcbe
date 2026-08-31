import { ArrowLink, Tag } from '../../../components/ui';

const comites = [
  {
    id: 1,
    name: 'Comité Juridique',
    scope: "Services offerts moyennant des frais d'honoraires professionnels",
    mission:
      "Accompagnement juridique complet pour tous vos besoins en matière d'immigration et de démarches administratives au Canada.",
    topics: [
      "Conseils juridiques en droit de l'immigration",
      'Accompagnement pour les demandes de résidence permanente',
      'Assistance pour les demandes de citoyenneté canadienne',
      "Conseils pour les permis de travail et d'études",
      'Orientation pour les démarches de parrainage familial',
      'Représentation lors de procédures administratives',
    ],
  },
  {
    id: 2,
    name: 'Comité Ressources Humaines',
    scope: 'Services gratuits pour tous les membres',
    mission:
      'Soutien professionnel pour votre développement de carrière et votre intégration dans le marché du travail canadien.',
    topics: [
      'Orientation et conseils professionnels personnalisés',
      'Révision et optimisation de CV et lettres de motivation',
      "Préparation aux entrevues d'embauche",
      "Accompagnement pour la recherche d'emploi",
      'Programme de mentorat professionnel',
      'Réseautage et mise en contact avec des employeurs',
      'Ateliers de développement des compétences',
      "Conseils pour l'équivalence des diplômes",
    ],
  },
  {
    id: 3,
    name: 'Comité SONGRÉ',
    scope: 'Services de solidarité communautaire',
    mission:
      'Soutien social et solidarité communautaire pour faire face aux situations difficiles et aux urgences.',
    topics: [
      "Aide d'urgence en cas de détresse",
      'Soutien financier et matériel en cas de décès',
      'Accompagnement lors de situations de crise',
      'Mobilisation communautaire pour les membres en difficulté',
      'Soutien psychosocial et écoute',
      'Coordination des funérailles et rapatriement',
      'Assistance pour les familles endeuillées',
      "Réseau de solidarité et d'entraide",
    ],
  },
  {
    id: 4,
    name: 'Comité Finance',
    scope: 'Services gratuits et ateliers réguliers',
    mission:
      'Éducation financière et accompagnement pour une gestion saine de vos finances personnelles au Canada.',
    topics: [
      "Ateliers d'éducation financière",
      'Conseils pour la gestion budgétaire',
      "Orientation pour l'épargne et les investissements",
      "Accompagnement pour l'accès au crédit",
      "Conseils pour l'achat d'une première maison",
      'Planification financière personnelle',
      'Information sur les produits financiers canadiens',
      "Conseils pour la déclaration d'impôts",
    ],
  },
];

const ComitesSection = () => {
  const { t } = useTranslation();

  return (
    <section className="bg-background pb-24 pt-12">
      <div className="container-page">
        <div className="grid grid-cols-1 gap-8 lg:grid-cols-2">
          {comites.map((comite, index) => (
            <article key={comite.id} className="group relative overflow-hidden rounded-[20px] border border-green/10 bg-white p-7 shadow-[0_12px_38px_rgba(0,59,27,.06)] transition-all duration-300 hover:-translate-y-1 hover:shadow-[0_20px_48px_rgba(0,59,27,.11)] sm:p-8">
              <span className="absolute -right-2 -top-6 font-display text-[100px] font-bold leading-none text-green/[0.035]" aria-hidden="true">0{index + 1}</span>
              <div className="relative flex flex-wrap items-start justify-between gap-4 border-b border-green/10 pb-5">
                <div className="flex items-center gap-4">
                  <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-green-deep text-xl text-gold"><i className={['ri-scales-3-line','ri-user-heart-line','ri-heart-3-line','ri-line-chart-line'][index]} aria-hidden="true" /></span>
                  <h3 className="font-display text-headline-md text-green-deep">{comite.name}</h3>
                </div>
                <Tag className="bg-surface-container text-sm">{comite.scope}</Tag>
              </div>
              <p className="mt-6 max-w-3xl text-body-md text-ink-variant">{comite.mission}</p>
              <ul className="mt-6 grid grid-cols-1 gap-2 md:grid-cols-2">
                {comite.topics.map((topic) => (
                  <li key={topic} className="flex items-start gap-3 text-body-md text-ink-variant">
                    <i className="ri-check-line mt-1 rounded-full bg-green/8 p-0.5 text-green" aria-hidden="true"></i>
                    {topic}
                  </li>
                ))}
              </ul>
              <ArrowLink to="/contact" tone="red" className="mt-6">
                {t('public.common.writeToUs')}
              </ArrowLink>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
};

export default ComitesSection;

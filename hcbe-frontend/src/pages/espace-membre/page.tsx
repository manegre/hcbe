import Navbar from '../../components/feature/Navbar';
import Footer from '../../components/feature/Footer';
import { features } from '../../config/features';
import MemberLoginForm from './components/MemberLoginForm';
import { Button, ArrowLink, PageHeader, Field, inputClasses } from '../../components/ui';
import { membershipApplicationsApi } from '../../lib/api/membership-applications';

const memberAdvantageKeys = [
  'public.member.advantages.items.events',
  'public.member.advantages.items.networking',
  'public.member.advantages.items.mentoring',
  'public.member.advantages.items.documents',
  'public.member.advantages.items.decisions',
  'public.member.advantages.items.support',
  'public.member.advantages.items.discounts',
  'public.member.advantages.items.newsletter',
] as const;

const provinces = [
  'Alberta',
  'Colombie-Britannique',
  'Manitoba',
  'Nouveau-Brunswick',
  'Terre-Neuve-et-Labrador',
  'Nouvelle-Écosse',
  'Ontario',
  'Île-du-Prince-Édouard',
  'Québec',
  'Saskatchewan',
];

const domainesProfessionnels = [
  "Technologies de l'information",
  'Santé et services sociaux',
  'Éducation et formation',
  'Finance et comptabilité',
  'Droit et services juridiques',
  'Ingénierie et sciences',
  'Commerce et gestion',
  'Arts et culture',
  'Construction et métiers',
  'Transport et logistique',
  'Autre',
];

const EspaceMembrePage = () => {
  const { t } = useTranslation();
  const [inscriptionData, setInscriptionData] = useState({
    prenom: '',
    nom: '',
    email: '',
    telephone: '',
    ville: '',
    province: '',
    profession: '',
    domaineProfessionnel: '',
    motivationAdhesion: '',
    password: '',
    confirmPassword: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitStatus, setSubmitStatus] = useState<'idle' | 'success' | 'error'>('idle');

  const updateField = (field: keyof typeof inscriptionData, value: string) => {
    setInscriptionData((current) => ({ ...current, [field]: value }));
  };

  const handleInscriptionSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (inscriptionData.motivationAdhesion.length > 500) {
      alert('La motivation ne peut pas dépasser 500 caractères.');
      return;
    }

    if (inscriptionData.password.length < 8 || inscriptionData.password !== inscriptionData.confirmPassword) {
      setSubmitStatus('error');
      return;
    }

    setIsSubmitting(true);
    setSubmitStatus('idle');

    try {
      const response = await membershipApplicationsApi.submit({
        firstName: inscriptionData.prenom.trim(),
        lastName: inscriptionData.nom.trim(),
        email: inscriptionData.email.trim(),
        phone: inscriptionData.telephone.trim(),
        city: inscriptionData.ville.trim(),
        province: inscriptionData.province,
        profession: inscriptionData.profession.trim(),
        expertise: inscriptionData.domaineProfessionnel,
        motivation: inscriptionData.motivationAdhesion.trim(),
        password: inscriptionData.password,
      });

      if (response.success) {
        setSubmitStatus('success');
        setInscriptionData({
          prenom: '',
          nom: '',
          email: '',
          telephone: '',
          ville: '',
          province: '',
          profession: '',
          domaineProfessionnel: '',
          motivationAdhesion: '',
          password: '',
          confirmPassword: '',
        });
      } else {
        setSubmitStatus('error');
      }
    } catch {
      setSubmitStatus('error');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <PageHeader
        variant="hero"
        title={t('public.member.hero.title')}
        description={t('public.member.hero.subtitle')}
        aside={
          <div className="border border-white/25 p-6">
            <p className="text-label-md uppercase text-gold">{t('public.member.hero.card.label')}</p>
            <p className="mt-4 text-body-lg font-semibold text-white">{t('public.member.hero.card.title')}</p>
            <p className="mt-3 text-body-md text-green-dim">{t('public.member.hero.card.description')}</p>
          </div>
        }
      />

      <section className="bg-background py-16 md:py-24">
        <div className="container-page grid grid-cols-1 gap-10 lg:grid-cols-[0.9fr_1.1fr] lg:gap-12">
          <aside className="space-y-8">
            {features.memberLoginEnabled && <MemberLoginForm />}

            <div>
              <p className="text-label-md uppercase text-red-link">{t('public.member.advantages.label')}</p>
              <h2 className="mt-3 font-display text-headline-lg text-green">
                {t('public.member.advantages.title')}
              </h2>
              <ul className="mt-6">
                {memberAdvantageKeys.map((key) => (
                  <li key={key} className="flex items-start gap-3 border-t border-line py-4">
                    <i className="ri-check-line mt-1 shrink-0 text-green" aria-hidden="true"></i>
                    <span className="text-body-md text-ink-variant">{t(key)}</span>
                  </li>
                ))}
              </ul>
            </div>

            <div className="border border-line bg-surface p-6">
              <h3 className="font-display text-headline-md text-green">{t('public.member.help.title')}</h3>
              <p className="mt-2 text-body-md text-ink-variant">{t('public.member.help.description')}</p>
              <ArrowLink to="/contact" tone="goldInk" className="mt-4">
                {t('public.member.help.cta')}
              </ArrowLink>
            </div>
          </aside>

          <div className="border border-line bg-surface p-8">
            <p className="text-label-md uppercase text-red-link">{t('public.member.form.label')}</p>
            <h2 className="mt-3 font-display text-headline-lg text-green">{t('public.member.form.title')}</h2>
            <p className="mt-3 text-body-md text-ink-variant">{t('public.member.form.intro')}</p>

            <form
              id="inscription-membre-form"
              data-readdy-form
              onSubmit={handleInscriptionSubmit}
              className="mt-8 space-y-8"
            >
              <div className="space-y-6">
                <h3 className="border-b border-line pb-3 text-label-md uppercase text-ink-variant">
                  {t('public.member.form.sections.contact')}
                </h3>
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  <Field label={t('public.member.form.fields.firstName')} htmlFor="prenom" required>
                    <input
                      type="text"
                      id="prenom"
                      name="prenom"
                      value={inscriptionData.prenom}
                      onChange={(e) => updateField('prenom', e.target.value)}
                      required
                      className={inputClasses}
                    />
                  </Field>
                  <Field label={t('public.member.form.fields.lastName')} htmlFor="nom" required>
                    <input
                      type="text"
                      id="nom"
                      name="nom"
                      value={inscriptionData.nom}
                      onChange={(e) => updateField('nom', e.target.value)}
                      required
                      className={inputClasses}
                    />
                  </Field>
                </div>

                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  <Field label={t('public.member.form.fields.email')} htmlFor="email" required>
                    <input
                      type="email"
                      id="email"
                      name="email"
                      value={inscriptionData.email}
                      onChange={(e) => updateField('email', e.target.value)}
                      required
                      className={inputClasses}
                    />
                  </Field>
                  <Field label={t('public.member.form.fields.phone')} htmlFor="telephone" required>
                    <input
                      type="tel"
                      id="telephone"
                      name="telephone"
                      value={inscriptionData.telephone}
                      onChange={(e) => updateField('telephone', e.target.value)}
                      required
                      className={inputClasses}
                    />
                  </Field>
                </div>

                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  <Field label={t('public.member.form.fields.city')} htmlFor="ville" required>
                    <input
                      type="text"
                      id="ville"
                      name="ville"
                      value={inscriptionData.ville}
                      onChange={(e) => updateField('ville', e.target.value)}
                      required
                      className={inputClasses}
                    />
                  </Field>
                  <Field label={t('public.member.form.fields.province')} htmlFor="province" required>
                    <select
                      id="province"
                      name="province"
                      value={inscriptionData.province}
                      onChange={(e) => updateField('province', e.target.value)}
                      required
                      className={`${inputClasses} cursor-pointer`}
                    >
                      <option value="">{t('public.member.form.select')}</option>
                      {provinces.map((prov) => (
                        <option key={prov} value={prov}>
                          {prov}
                        </option>
                      ))}
                    </select>
                  </Field>
                </div>
              </div>

              <div className="space-y-6 border-t border-line pt-8">
                <h3 className="border-b border-line pb-3 text-label-md uppercase text-ink-variant">
                  {t('public.member.form.sections.professional')}
                </h3>
                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  <Field label={t('public.member.form.fields.profession')} htmlFor="profession" required>
                    <input
                      type="text"
                      id="profession"
                      name="profession"
                      value={inscriptionData.profession}
                      onChange={(e) => updateField('profession', e.target.value)}
                      required
                      className={inputClasses}
                    />
                  </Field>
                  <Field label={t('public.member.form.fields.domain')} htmlFor="domaineProfessionnel" required>
                    <select
                      id="domaineProfessionnel"
                      name="domaineProfessionnel"
                      value={inscriptionData.domaineProfessionnel}
                      onChange={(e) => updateField('domaineProfessionnel', e.target.value)}
                      required
                      className={`${inputClasses} cursor-pointer`}
                    >
                      <option value="">{t('public.member.form.select')}</option>
                      {domainesProfessionnels.map((domaine) => (
                        <option key={domaine} value={domaine}>
                          {domaine}
                        </option>
                      ))}
                    </select>
                  </Field>
                </div>

                <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                  <Field label={t('public.member.form.fields.password')} htmlFor="member-password" required>
                    <input
                      id="member-password"
                      type="password"
                      minLength={8}
                      value={inscriptionData.password}
                      onChange={(e) => updateField('password', e.target.value)}
                      className={inputClasses}
                      autoComplete="new-password"
                      required
                    />
                  </Field>
                  <Field label={t('public.member.form.fields.confirmPassword')} htmlFor="member-confirm-password" required>
                    <input
                      id="member-confirm-password"
                      type="password"
                      minLength={8}
                      value={inscriptionData.confirmPassword}
                      onChange={(e) => updateField('confirmPassword', e.target.value)}
                      className={inputClasses}
                      autoComplete="new-password"
                      required
                    />
                  </Field>
                </div>

                <Field
                  label={t('public.member.form.fields.motivation')}
                  htmlFor="motivationAdhesion"
                  required
                  hint={t('public.member.form.charCount', { count: inscriptionData.motivationAdhesion.length })}
                >
                  <textarea
                    id="motivationAdhesion"
                    name="motivationAdhesion"
                    value={inscriptionData.motivationAdhesion}
                    onChange={(e) => updateField('motivationAdhesion', e.target.value)}
                    required
                    rows={5}
                    maxLength={500}
                    className={`${inputClasses} resize-none`}
                    placeholder={t('public.member.form.motivation.placeholder')}
                  />
                </Field>
              </div>

              {submitStatus === 'success' && (
                <div className="border border-green bg-surface p-6">
                  <div className="flex items-start gap-3">
                    <i className="ri-checkbox-circle-fill mt-1 shrink-0 text-xl text-green" aria-hidden="true"></i>
                    <div>
                      <h4 className="font-display text-headline-md text-green">
                        {t('public.member.form.success.title')}
                      </h4>
                      <p className="mt-1 text-body-md text-ink-variant">{t('public.member.form.success.message')}</p>
                    </div>
                  </div>
                </div>
              )}

              {submitStatus === 'error' && (
                <div className="border border-error bg-surface p-6">
                  <div className="flex items-start gap-3">
                    <i className="ri-error-warning-fill mt-1 shrink-0 text-xl text-error" aria-hidden="true"></i>
                    <div>
                      <h4 className="font-display text-headline-md text-error">
                        {t('public.member.form.error.title')}
                      </h4>
                      <p className="mt-1 text-body-md text-ink-variant">{t('public.member.form.error.message')}</p>
                    </div>
                  </div>
                </div>
              )}

              <Button type="submit" variant="primary" disabled={isSubmitting} className="w-full sm:w-auto">
                {isSubmitting ? (
                  <>
                    <i className="ri-loader-4-line animate-spin" aria-hidden="true"></i>
                    {t('public.member.form.submit.loading')}
                  </>
                ) : (
                  <>
                    <i className="ri-send-plane-fill" aria-hidden="true"></i>
                    {t('public.member.form.submit.label')}
                  </>
                )}
              </Button>

              <p className="text-body-md text-ink-variant">{t('public.member.form.consent')}</p>
            </form>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
};

export default EspaceMembrePage;

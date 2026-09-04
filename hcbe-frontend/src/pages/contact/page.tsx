import Navbar from '../../components/feature/Navbar';
import Footer from '../../components/feature/Footer';
import { SOCIAL_LINKS } from '../../lib/social-links';
import { Button, PageHeader, Field } from '../../components/ui';
import { publicSubmissionsApi } from '../../lib/api/public-submissions';
import { useSearchParams } from 'react-router-dom';
import type { PublicSubmissionType } from '../../lib/api/types';

const contactInputClasses =
  'w-full min-h-[48px] rounded-xl border border-green/15 bg-surface-container px-4 py-3 text-body-md text-ink transition-[background-color,border-color,box-shadow] duration-200 placeholder:text-ink-variant/55 hover:border-green/35 focus:border-green focus:bg-surface focus:outline-none focus:ring-4 focus:ring-green/10 disabled:cursor-not-allowed disabled:opacity-60';

const ContactPage = () => {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const requestedType = searchParams.get('type') as PublicSubmissionType | null;
  const allowedTypes: PublicSubmissionType[] = ['contact', 'volunteer', 'event-registration', 'grant-application', 'consultation-response', 'project-contribution'];
  const submissionType = requestedType && allowedTypes.includes(requestedType) ? requestedType : 'contact';
  const referenceId = searchParams.get('referenceId') || undefined;
  const referenceLabel = searchParams.get('label') || undefined;
  const seededSubject = submissionType === 'event-registration'
    ? 'Événements'
    : submissionType === 'grant-application'
      ? 'Services'
      : submissionType === 'project-contribution'
        ? 'Projets'
        : submissionType === 'volunteer'
          ? 'Bénévolat'
          : 'Autre';
  const [formData, setFormData] = useState({
    nom: '',
    prenom: '',
    email: '',
    telephone: '',
    sujet: requestedType ? seededSubject : '',
    message: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitStatus, setSubmitStatus] = useState<'idle' | 'success' | 'error'>('idle');
  const [openFaqIndex, setOpenFaqIndex] = useState<number | null>(null);

  const subjectOptions = useMemo(
    () => [
      { value: 'Adhésion', labelKey: 'public.contact.form.subject.membership' },
      { value: 'Services', labelKey: 'public.contact.form.subject.services' },
      { value: 'Événements', labelKey: 'public.contact.form.subject.events' },
      { value: 'Projets', labelKey: 'public.contact.form.subject.projects' },
      { value: 'Bénévolat', labelKey: 'public.contact.form.subject.volunteer' },
      { value: 'Partenariat', labelKey: 'public.contact.form.subject.partnership' },
      { value: 'Autre', labelKey: 'public.contact.form.subject.other' },
    ],
    [],
  );

  const faqItems = useMemo(
    () => [
      { questionKey: 'public.contact.faq.items.membership.q', answerKey: 'public.contact.faq.items.membership.a' },
      { questionKey: 'public.contact.faq.items.fees.q', answerKey: 'public.contact.faq.items.fees.a' },
      { questionKey: 'public.contact.faq.items.committees.q', answerKey: 'public.contact.faq.items.committees.a' },
      { questionKey: 'public.contact.faq.items.projects.q', answerKey: 'public.contact.faq.items.projects.a' },
      { questionKey: 'public.contact.faq.items.events.q', answerKey: 'public.contact.faq.items.events.a' },
      { questionKey: 'public.contact.faq.items.passport.q', answerKey: 'public.contact.faq.items.passport.a' },
    ],
    [],
  );

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (formData.message.length > 500) {
      alert(t('public.contact.form.validation.messageTooLong'));
      return;
    }

    setIsSubmitting(true);
    setSubmitStatus('idle');

    try {
      const response = await publicSubmissionsApi.submit({
        type: submissionType,
        firstName: formData.prenom.trim(),
        lastName: formData.nom.trim(),
        email: formData.email.trim(),
        phone: formData.telephone.trim(),
        subject: formData.sujet,
        details: formData.message.trim(),
        metadata: {
          ...(referenceId ? { referenceId } : {}),
          ...(referenceLabel ? { referenceLabel } : {}),
          sourcePath: window.location.pathname,
        },
      });

      if (response.success) {
        setSubmitStatus('success');
        setFormData({
          nom: '',
          prenom: '',
          email: '',
          telephone: '',
          sujet: requestedType ? seededSubject : '',
          message: '',
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
        title={t('public.contact.hero.title')}
        description={t('public.contact.hero.subtitle')}
        aside={
          <div className="overflow-hidden rounded-[20px] border border-white/15 bg-white/[0.06] shadow-[0_24px_70px_rgba(0,0,0,.14)] backdrop-blur-sm">
            <div className="flex items-center justify-between border-b border-white/15 px-6 py-5">
              <p className="text-[10px] font-bold uppercase tracking-[0.18em] text-gold">
                {t('public.contact.hero.card.label')}
              </p>
              <span className="flex h-10 w-10 items-center justify-center rounded-full border border-white/15 bg-white/5 text-gold">
                <i className="ri-chat-smile-2-line text-xl" aria-hidden="true" />
              </span>
            </div>
            <div className="grid grid-cols-3 divide-x divide-white/10 px-3 md:block md:divide-x-0 md:divide-y md:px-6">
              {[
                ['01', t('public.contact.form.fields.subject')],
                ['02', t('public.contact.form.fields.message')],
                ['03', t('public.contact.hero.card.label')],
              ].map(([number, label]) => (
                <div key={number} className="flex flex-col items-start gap-2 px-3 py-4 md:flex-row md:items-center md:gap-4 md:px-0">
                  <span className="text-[10px] font-bold tabular-nums text-gold">{number}</span>
                  <span className="text-sm font-semibold text-white/90">{label}</span>
                  <i className="ri-arrow-right-up-line ml-auto hidden text-white/35 md:block" aria-hidden="true" />
                </div>
              ))}
            </div>
            <div className="bg-black/10 px-6 py-5">
              <p className="text-body-md font-semibold text-white">{t('public.contact.hero.card.title')}</p>
              <p className="mt-2 hidden text-sm leading-6 text-white/60 md:block">{t('public.contact.hero.card.description')}</p>
            </div>
          </div>
        }
      />

      <section className="bg-surface-container py-16 md:py-24">
        <div className="container-page">
          <div className="mb-10 flex flex-col justify-between gap-5 md:flex-row md:items-end">
            <div>
              <div className="mb-4 flex items-center gap-3" aria-hidden="true">
                <span className="h-0.5 w-10 bg-gold" />
                <span className="h-1.5 w-1.5 rounded-full bg-red-link" />
              </div>
              <h2 className="max-w-2xl font-display text-[34px] font-bold leading-tight text-green md:text-[44px]">
                {t('public.contact.form.title')}
              </h2>
            </div>
            <p className="max-w-md text-body-md leading-7 text-ink-variant">
              {t('public.contact.hero.card.description')}
            </p>
          </div>

          <div className="grid grid-cols-1 gap-7 lg:grid-cols-12 lg:items-start">
            <div className="rounded-[22px] border border-green/10 bg-white p-6 shadow-[0_22px_65px_rgba(0,59,27,.08)] md:p-10 lg:col-span-7">
              <div className="flex items-center justify-between border-b border-green/10 pb-6">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-red-link">
                    {t('public.contact.hero.badge')}
                  </p>
                  <p className="mt-2 text-sm text-ink-variant">contact@hcbe.ca</p>
                </div>
                <span className="flex h-12 w-12 items-center justify-center rounded-full bg-gold text-green shadow-[0_8px_24px_rgba(255,205,0,.25)]">
                  <i className="ri-mail-send-line text-xl" aria-hidden="true" />
                </span>
              </div>

              <form id="contact-form" data-readdy-form onSubmit={handleSubmit} className="mt-8 space-y-6">
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <Field label={t('public.contact.form.fields.firstName')} htmlFor="prenom" required>
                  <input
                    type="text"
                    id="prenom"
                    name="prenom"
                    value={formData.prenom}
                    onChange={(e) => setFormData({ ...formData, prenom: e.target.value })}
                    required
                    className={contactInputClasses}
                  />
                </Field>
                <Field label={t('public.contact.form.fields.lastName')} htmlFor="nom" required>
                  <input
                    type="text"
                    id="nom"
                    name="nom"
                    value={formData.nom}
                    onChange={(e) => setFormData({ ...formData, nom: e.target.value })}
                    required
                    className={contactInputClasses}
                  />
                </Field>
              </div>

              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <Field label={t('public.contact.form.fields.email')} htmlFor="email" required>
                  <input
                    type="email"
                    id="email"
                    name="email"
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    required
                    className={contactInputClasses}
                  />
                </Field>
                <Field label={t('public.contact.form.fields.phone')} htmlFor="telephone">
                  <input
                    type="tel"
                    id="telephone"
                    name="telephone"
                    value={formData.telephone}
                    onChange={(e) => setFormData({ ...formData, telephone: e.target.value })}
                    className={contactInputClasses}
                  />
                </Field>
              </div>

              <Field label={t('public.contact.form.fields.subject')} htmlFor="sujet" required>
                <select
                  id="sujet"
                  name="sujet"
                  value={formData.sujet}
                  onChange={(e) => setFormData({ ...formData, sujet: e.target.value })}
                  required
                  className={`${contactInputClasses} cursor-pointer`}
                >
                  <option value="">{t('public.contact.form.subject.placeholder')}</option>
                  {subjectOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {t(option.labelKey)}
                    </option>
                  ))}
                </select>
              </Field>

              <Field
                label={t('public.contact.form.fields.message')}
                htmlFor="message"
                required
                hint={t('public.contact.form.charCount', { count: formData.message.length })}
              >
                <textarea
                  id="message"
                  name="message"
                  value={formData.message}
                  onChange={(e) => setFormData({ ...formData, message: e.target.value })}
                  required
                  rows={6}
                  maxLength={500}
                  className={`${contactInputClasses} min-h-[168px] resize-none`}
                  placeholder={t('public.contact.form.message.placeholder')}
                />
              </Field>

              {submitStatus === 'success' && (
                <div className="rounded-xl border border-green/30 bg-green/5 p-5">
                  <div className="flex items-start gap-3">
                    <i className="ri-checkbox-circle-fill mt-1 shrink-0 text-xl text-green" aria-hidden="true"></i>
                    <div>
                      <h4 className="font-display text-headline-md text-green">
                        {t('public.contact.form.success.title')}
                      </h4>
                      <p className="mt-1 text-body-md text-ink-variant">{t('public.contact.form.success.message')}</p>
                    </div>
                  </div>
                </div>
              )}

              {submitStatus === 'error' && (
                <div className="rounded-xl border border-error/30 bg-error/5 p-5">
                  <div className="flex items-start gap-3">
                    <i className="ri-error-warning-fill mt-1 shrink-0 text-xl text-error" aria-hidden="true"></i>
                    <div>
                      <h4 className="font-display text-headline-md text-error">
                        {t('public.contact.form.error.title')}
                      </h4>
                      <p className="mt-1 text-body-md text-ink-variant">{t('public.contact.form.error.message')}</p>
                    </div>
                  </div>
                </div>
              )}

              <div className="flex flex-col gap-4 border-t border-green/10 pt-6 sm:flex-row sm:items-center sm:justify-between">
                <p className="max-w-xs text-xs leading-5 text-ink-variant">
                  {t('public.contact.hero.card.title')}
                </p>
                <Button type="submit" variant="primary" disabled={isSubmitting} className="w-full px-8 sm:w-auto">
                {isSubmitting ? (
                  <>
                    <i className="ri-loader-4-line animate-spin" aria-hidden="true"></i>
                    {t('public.contact.form.submit.loading')}
                  </>
                ) : (
                  <>
                    <i className="ri-send-plane-fill" aria-hidden="true"></i>
                    {t('public.contact.form.submit.label')}
                  </>
                )}
                </Button>
              </div>
              </form>
            </div>

            <aside className="space-y-7 lg:sticky lg:top-24 lg:col-span-5">
              <div className="public-grid-pattern overflow-hidden rounded-[22px] bg-green-deep text-white shadow-[0_22px_65px_rgba(0,59,27,.14)]">
                <div className="border-b border-white/10 p-7 md:p-8">
                  <div className="flex items-center justify-between gap-4">
                    <div>
                      <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-gold">
                        {t('public.contact.coordinates.title')}
                      </p>
                      <h3 className="mt-3 font-display text-[28px] font-bold leading-tight text-white">
                        HCBE Canada
                      </h3>
                    </div>
                    <span className="flex h-12 w-12 items-center justify-center rounded-full border border-white/15 bg-white/5 text-gold">
                      <i className="ri-customer-service-2-line text-2xl" aria-hidden="true" />
                    </span>
                  </div>
                </div>

                <div className="divide-y divide-white/10 px-7 md:px-8">
                  <a href="mailto:contact@hcbe.ca" className="group flex items-center gap-4 py-6">
                    <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-white/10 text-gold transition-colors group-hover:bg-gold group-hover:text-green">
                      <i className="ri-mail-line text-xl" aria-hidden="true" />
                    </span>
                    <span>
                      <span className="block text-[10px] font-bold uppercase tracking-[0.14em] text-white/50">
                        {t('public.contact.coordinates.email')}
                      </span>
                      <span className="mt-1 block text-sm font-semibold text-white">contact@hcbe.ca</span>
                    </span>
                    <i className="ri-arrow-right-up-line ml-auto text-white/40 transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5" aria-hidden="true" />
                  </a>
                  <div className="flex items-center gap-4 py-6">
                    <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-white/10 text-gold">
                      <i className="ri-map-pin-line text-xl" aria-hidden="true" />
                    </span>
                    <span>
                      <span className="block text-[10px] font-bold uppercase tracking-[0.14em] text-white/50">
                        {t('public.contact.coordinates.address')}
                      </span>
                      <span className="mt-1 block text-sm font-semibold text-white">
                        {t('public.contact.coordinates.country')}
                      </span>
                    </span>
                  </div>
                </div>

                <div className="border-t border-white/10 bg-black/10 p-7 md:p-8">
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-gold">
                    {t('public.contact.social.title')}
                  </p>
                  <p className="mt-3 max-w-sm text-sm leading-6 text-white/60">{t('public.contact.social.intro')}</p>
                  <ul className="mt-5 flex flex-wrap gap-3">
                    {SOCIAL_LINKS.map((network) => (
                      <li key={network.id}>
                        <a
                          href={network.href}
                          target="_blank"
                          rel="noopener noreferrer"
                          aria-label={network.label}
                          className="flex h-11 w-11 items-center justify-center rounded-full border border-white/15 text-lg text-white transition-all hover:-translate-y-0.5 hover:border-gold hover:bg-gold hover:text-green"
                        >
                          <i className={network.iconClass} aria-hidden="true" />
                        </a>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>

              <div className="rounded-[20px] border border-green/10 bg-white p-7 shadow-[0_14px_40px_rgba(0,59,27,.06)] md:p-8">
                <h3 className="font-display text-[24px] font-bold text-green">{t('public.contact.links.title')}</h3>
                <ul className="mt-4 divide-y divide-green/10">
                  {[
                    ['https://ambabf-ca.org/home-en/', t('public.contact.links.embassy')],
                    ['https://www.canada.ca/fr/immigration-refugies-citoyennete.html', t('public.contact.links.ircc')],
                    ['https://www.canada.ca', t('public.contact.links.govCanada')],
                  ].map(([href, label]) => (
                    <li key={href}>
                    <a
                      href={href}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="group flex min-h-[58px] items-center gap-3 py-3 text-sm font-medium leading-5 text-ink-variant transition-colors hover:text-green"
                    >
                      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-green/5 text-green transition-colors group-hover:bg-green group-hover:text-white">
                        <i className="ri-external-link-line" aria-hidden="true" />
                      </span>
                      <span>{label}</span>
                    </a>
                  </li>
                  ))}
                </ul>
              </div>
            </aside>
          </div>
        </div>
      </section>

      <section className="bg-white py-16 md:py-24">
        <div className="container-page grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-16">
          <div className="lg:col-span-4">
            <div className="lg:sticky lg:top-24">
              <div className="mb-5 flex items-center gap-3" aria-hidden="true">
                <span className="h-0.5 w-10 bg-gold" />
                <span className="h-1.5 w-1.5 rounded-full bg-red-link" />
              </div>
              <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-red-link">
                HCBE Canada
              </p>
              <h2 className="mt-4 font-display text-[34px] font-bold leading-tight text-green md:text-[42px]">
                {t('public.contact.faq.title')}
              </h2>
              <p className="mt-4 max-w-sm text-body-md leading-7 text-ink-variant">
                {t('public.contact.faq.subtitle')}
              </p>
              <a
                href="mailto:contact@hcbe.ca"
                className="mt-7 inline-flex min-h-[44px] items-center gap-2 text-label-md uppercase text-red-link transition-colors hover:text-green"
              >
                {t('public.contact.form.title')}
                <i className="ri-arrow-right-line" aria-hidden="true" />
              </a>
            </div>
          </div>

          <div className="overflow-hidden rounded-[20px] border border-green/10 bg-background lg:col-span-8">
            {faqItems.map((item, index) => (
              <div key={item.questionKey} className="border-b border-green/10 last:border-b-0">
                <button
                  type="button"
                  onClick={() => setOpenFaqIndex(openFaqIndex === index ? null : index)}
                  aria-expanded={openFaqIndex === index}
                  aria-controls={`contact-faq-${index}`}
                  className="group flex w-full items-center gap-4 px-5 py-6 text-left transition-colors hover:bg-white md:px-7"
                >
                  <span className="text-[10px] font-bold tabular-nums text-gold-ink">{String(index + 1).padStart(2, '0')}</span>
                  <span className="flex-1 text-[16px] font-semibold leading-6 text-ink group-hover:text-green">
                    {t(item.questionKey)}
                  </span>
                  <span className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-full border transition-colors ${openFaqIndex === index ? 'border-green bg-green text-white' : 'border-green/15 bg-white text-green group-hover:border-green'}`}>
                    <i className={`ri-${openFaqIndex === index ? 'subtract' : 'add'}-line text-lg`} aria-hidden="true" />
                  </span>
                </button>
                {openFaqIndex === index && (
                  <div id={`contact-faq-${index}`} className="px-5 pb-7 pl-[68px] md:px-7 md:pl-[76px]">
                    <p className="max-w-2xl text-body-md leading-7 text-ink-variant">{t(item.answerKey)}</p>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
};

export default ContactPage;

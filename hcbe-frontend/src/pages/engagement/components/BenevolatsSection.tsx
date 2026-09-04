import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../../../components/ui';
import { publicSubmissionsApi } from '../../../lib/api/public-submissions';

const BenevolatsSection = () => {
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    nom: '',
    prenom: '',
    email: '',
    telephone: '',
    ville: '',
    competences: '',
    disponibilite: '',
    motivation: '',
  });

  const [isSubmitted, setIsSubmitted] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value,
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const response = await publicSubmissionsApi.submit({
        type: 'volunteer',
        firstName: formData.prenom.trim(),
        lastName: formData.nom.trim(),
        email: formData.email.trim(),
        phone: formData.telephone.trim(),
        city: formData.ville.trim(),
        subject: t('public.volunteer.subject'),
        details: formData.motivation.trim(),
        metadata: {
          competences: formData.competences.trim(),
          disponibilite: formData.disponibilite,
        },
      });

      if (response.success) {
        setIsSubmitted(true);
        setFormData({
          nom: '',
          prenom: '',
          email: '',
          telephone: '',
          ville: '',
          competences: '',
          disponibilite: '',
          motivation: '',
        });
        setTimeout(() => setIsSubmitted(false), 5000);
      }
    } catch (error) {
      console.error('Erreur lors de la soumission:', error);
    }
  };

  return (
    <section className="bg-background py-20">
      <div className="container-page max-w-4xl">
        <div className="mb-16 text-center">
          <h2 className="font-display text-headline-lg text-green">{t('public.volunteer.title')}</h2>
          <p className="mx-auto mt-4 max-w-3xl text-body-lg text-ink-variant">
            {t('public.volunteer.subtitle')}
          </p>
        </div>

        <div className="border border-line bg-surface p-8">
          <form id="benevolat-form" data-readdy-form onSubmit={handleSubmit} className="space-y-6">
            <div className="grid gap-6 md:grid-cols-2">
              <Field label={t('public.volunteer.lastName')} htmlFor="nom" required>
                <input
                  type="text"
                  id="nom"
                  name="nom"
                  value={formData.nom}
                  onChange={handleChange}
                  required
                  className={inputClasses}
                />
              </Field>
              <Field label={t('public.volunteer.firstName')} htmlFor="prenom" required>
                <input
                  type="text"
                  id="prenom"
                  name="prenom"
                  value={formData.prenom}
                  onChange={handleChange}
                  required
                  className={inputClasses}
                />
              </Field>
            </div>

            <div className="grid gap-6 md:grid-cols-2">
              <Field label={t('public.volunteer.email')} htmlFor="email" required>
                <input
                  type="email"
                  id="email"
                  name="email"
                  value={formData.email}
                  onChange={handleChange}
                  required
                  className={inputClasses}
                />
              </Field>
              <Field label={t('public.volunteer.phone')} htmlFor="telephone" required>
                <input
                  type="tel"
                  id="telephone"
                  name="telephone"
                  value={formData.telephone}
                  onChange={handleChange}
                  required
                  className={inputClasses}
                />
              </Field>
            </div>

            <Field label={t('public.volunteer.city')} htmlFor="ville" required>
              <input
                type="text"
                id="ville"
                name="ville"
                value={formData.ville}
                onChange={handleChange}
                required
                className={inputClasses}
              />
            </Field>

            <Field label={t('public.volunteer.skills')} htmlFor="competences" required>
              <input
                type="text"
                id="competences"
                name="competences"
                value={formData.competences}
                onChange={handleChange}
                placeholder={t('public.volunteer.skillsPlaceholder')}
                required
                className={inputClasses}
              />
            </Field>

            <Field label={t('public.volunteer.availability')} htmlFor="disponibilite" required>
              <select
                id="disponibilite"
                name="disponibilite"
                value={formData.disponibilite}
                onChange={handleChange}
                required
                className={inputClasses}
              >
                <option value="">{t('public.volunteer.availabilityPlaceholder')}</option>
                <option value="quelques-heures-semaine">{t('public.volunteer.availabilityWeekly')}</option>
                <option value="quelques-heures-mois">{t('public.volunteer.availabilityMonthly')}</option>
                <option value="evenements-ponctuels">{t('public.volunteer.availabilityEvents')}</option>
                <option value="flexible">{t('public.volunteer.availabilityFlexible')}</option>
              </select>
            </Field>

            <Field label={t('public.volunteer.motivation')} htmlFor="motivation" required>
              <textarea
                id="motivation"
                name="motivation"
                value={formData.motivation}
                onChange={handleChange}
                maxLength={500}
                rows={4}
                placeholder={t('public.volunteer.motivationPlaceholder')}
                required
                className={inputClasses}
              ></textarea>
              <div className="mt-1 text-right text-body-md text-ink-variant">
                {t('public.volunteer.characterCount', { count: formData.motivation.length })}
              </div>
            </Field>

            <Button type="submit" variant="primary" className="w-full justify-center">
              {t('public.volunteer.submit')}
            </Button>
          </form>

          {isSubmitted && (
            <div className="mt-6 border border-green bg-surface p-4 text-green">
              <i className="ri-check-line mr-2" aria-hidden="true"></i>
              {t('public.volunteer.success')}
            </div>
          )}
        </div>
      </div>
    </section>
  );
};

export default BenevolatsSection;

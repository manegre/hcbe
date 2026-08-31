import { useState } from 'react';
import { Button, Field, inputClasses } from '../../../components/ui';
import { publicSubmissionsApi } from '../../../lib/api/public-submissions';

const BenevolatsSection = () => {
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
        subject: 'Bénévolat',
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
          <h2 className="font-display text-headline-lg text-green">Devenez Bénévole</h2>
          <p className="mx-auto mt-4 max-w-3xl text-body-lg text-ink-variant">
            Contribuez au développement de notre communauté en partageant votre temps et vos compétences
          </p>
        </div>

        <div className="border border-line bg-surface p-8">
          <form id="benevolat-form" data-readdy-form onSubmit={handleSubmit} className="space-y-6">
            <div className="grid gap-6 md:grid-cols-2">
              <Field label="Nom" htmlFor="nom" required>
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
              <Field label="Prénom" htmlFor="prenom" required>
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
              <Field label="Email" htmlFor="email" required>
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
              <Field label="Téléphone" htmlFor="telephone" required>
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

            <Field label="Ville de résidence" htmlFor="ville" required>
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

            <Field label="Compétences et domaines d'expertise" htmlFor="competences" required>
              <input
                type="text"
                id="competences"
                name="competences"
                value={formData.competences}
                onChange={handleChange}
                placeholder="Ex: Juridique, RH, Finance, Communication..."
                required
                className={inputClasses}
              />
            </Field>

            <Field label="Disponibilité" htmlFor="disponibilite" required>
              <select
                id="disponibilite"
                name="disponibilite"
                value={formData.disponibilite}
                onChange={handleChange}
                required
                className={inputClasses}
              >
                <option value="">Sélectionnez votre disponibilité</option>
                <option value="quelques-heures-semaine">Quelques heures par semaine</option>
                <option value="quelques-heures-mois">Quelques heures par mois</option>
                <option value="evenements-ponctuels">Événements ponctuels</option>
                <option value="flexible">Flexible</option>
              </select>
            </Field>

            <Field label="Motivation (max 500 caractères)" htmlFor="motivation" required>
              <textarea
                id="motivation"
                name="motivation"
                value={formData.motivation}
                onChange={handleChange}
                maxLength={500}
                rows={4}
                placeholder="Pourquoi souhaitez-vous devenir bénévole au HCBE ?"
                required
                className={inputClasses}
              ></textarea>
              <div className="mt-1 text-right text-body-md text-ink-variant">
                {formData.motivation.length}/500 caractères
              </div>
            </Field>

            <Button type="submit" variant="primary" className="w-full justify-center">
              Soumettre ma candidature
            </Button>
          </form>

          {isSubmitted && (
            <div className="mt-6 border border-green bg-surface p-4 text-green">
              <i className="ri-check-line mr-2" aria-hidden="true"></i>
              Merci pour votre candidature ! Nous vous contacterons bientôt.
            </div>
          )}
        </div>
      </div>
    </section>
  );
};

export default BenevolatsSection;

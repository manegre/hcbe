import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Button, inputClasses } from '../ui';
import { newsletterApi } from '../../lib/api/newsletter';
import type { SubscribeNewsletterRequest } from '../../lib/api/types';

const NewsletterSignup = () => {
  const { t, i18n } = useTranslation();
  const location = useLocation();
  const source: SubscribeNewsletterRequest['source'] = location.pathname === '/' ? 'home' : 'footer';
  const preferredLanguage: SubscribeNewsletterRequest['preferredLanguage'] =
    i18n.language.startsWith('en') ? 'en' : 'fr';

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [consentAccepted, setConsentAccepted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);

    if (!consentAccepted) {
      setError(t('public.newsletter.errorConsent'));
      return;
    }

    const payload: SubscribeNewsletterRequest = {
      fullName: fullName.trim(),
      email: email.trim(),
      preferredLanguage,
      consentAccepted: true,
      source,
    };

    try {
      setIsSubmitting(true);
      const response = await newsletterApi.subscribe(payload);
      if (response.success) {
        setIsSuccess(true);
        setFullName('');
        setEmail('');
        setConsentAccepted(false);
        return;
      }
      setError(response.message || t('public.newsletter.errorGeneric'));
    } catch (err) {
      console.error('Newsletter subscribe failed:', err);
      setError(t('public.newsletter.errorGeneric'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="bg-surface-container py-10 md:py-12">
      <div className="container-page grid gap-7 rounded-[20px] border border-green/10 bg-white p-6 shadow-[0_14px_40px_rgba(0,59,27,.07)] md:grid-cols-[0.8fr_1.6fr] md:items-center md:p-8">
        <div className="flex items-start gap-4 md:max-w-sm">
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-gold text-xl text-green-deep"><i className="ri-mail-open-line" aria-hidden="true" /></span>
          <div>
          <h2 className="font-display text-headline-md text-green-deep">{t('public.newsletter.homeTitle')}</h2>
          <p className="mt-1 text-sm leading-6 text-ink-variant">{t('public.newsletter.homeSubtitle')}</p>
          </div>
        </div>

        <div className="w-full">
          {isSuccess ? (
            <div className="border border-line bg-surface p-4 text-body-md">{t('public.newsletter.success')}</div>
          ) : (
            <form onSubmit={handleSubmit} className="flex flex-col gap-3">
              <div className="grid gap-3 sm:grid-cols-[1fr_1fr_auto]">
                <label className="sr-only" htmlFor="newsletter-fullname">
                  {t('public.newsletter.fullName')}
                </label>
                <input
                  id="newsletter-fullname"
                  type="text"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  required
                  placeholder={t('public.newsletter.fullName')}
                  className={`${inputClasses} border-line/80 bg-background/50 text-ink`}
                />
                <label className="sr-only" htmlFor="newsletter-email">
                  {t('public.newsletter.email')}
                </label>
                <input
                  id="newsletter-email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  placeholder={t('public.newsletter.email')}
                  className={`${inputClasses} border-line/80 bg-background/50 text-ink`}
                />
                <Button type="submit" variant="primary" disabled={isSubmitting} className="shrink-0 px-5">
                  {isSubmitting ? t('public.newsletter.submitting') : t('public.newsletter.submit')}
                </Button>
              </div>

              <label className="flex min-h-[32px] items-start gap-2.5 text-[13px] leading-5 text-ink-variant">
                <input
                  type="checkbox"
                  checked={consentAccepted}
                  onChange={(e) => setConsentAccepted(e.target.checked)}
                  className="mt-0.5 h-4 w-4 shrink-0 accent-green"
                />
                <span>
                  {t('public.newsletter.consent')}{' '}
                  <Link to="/confidentialite" className="font-semibold text-green underline-offset-2 hover:underline">
                    {t('public.newsletter.privacyLink')}
                  </Link>
                </span>
              </label>

              {error && <p className="border border-error p-4 text-body-md text-error">{error}</p>}
            </form>
          )}
        </div>
      </div>
    </section>
  );
};

export default NewsletterSignup;

import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import Navbar from '../../components/feature/Navbar';
import Footer from '../../components/feature/Footer';
import { ArrowLink, PageHeader } from '../../components/ui';
import { useAuth } from '../../contexts/AuthContext';
import MemberLoginForm from './components/MemberLoginForm';
import MemberRegistrationForm from './components/MemberRegistrationForm';

type GatewayMode = 'login' | 'signup';

const highlightedBenefits = [
  ['ri-calendar-event-line', 'public.member.advantages.items.events'],
  ['ri-team-line', 'public.member.advantages.items.networking'],
  ['ri-user-star-line', 'public.member.advantages.items.mentoring'],
  ['ri-folder-shield-2-line', 'public.member.advantages.items.documents'],
] as const;

const EspaceMembrePage = () => {
  const { t } = useTranslation();
  const { user } = useAuth();
  const [mode, setMode] = useState<GatewayMode>('login');
  const hasMemberSession = Boolean(user?.memberId);

  return (
    <div className="min-h-screen bg-canvas text-ink">
      <Navbar />
      {!hasMemberSession && (
        <PageHeader
          variant="interior"
          title={t('public.member.gateway.heroTitle')}
          description={t('public.member.gateway.heroDescription')}
        />
      )}

      <main className={hasMemberSession ? 'w-full bg-canvas' : 'container-page py-10 md:py-16'}>
        {hasMemberSession ? (
          <MemberLoginForm embedded />
        ) : (
          <section className="mx-auto grid max-w-[1120px] overflow-hidden rounded-[28px] border border-line bg-surface shadow-[0_28px_90px_rgba(0,59,27,.12)] lg:grid-cols-[0.78fr_1.22fr]">
            <aside className="relative order-2 overflow-hidden bg-green-deep px-7 py-9 text-white sm:px-10 sm:py-12 lg:order-1 lg:px-12 lg:py-14">
              <div className="absolute -left-28 bottom-[-7rem] h-72 w-72 rounded-full border-[44px] border-gold/[0.07]" aria-hidden="true" />
              <div className="absolute -right-20 -top-20 h-56 w-56 rounded-full border-[36px] border-white/[0.04]" aria-hidden="true" />

              <div className="relative flex h-full flex-col">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.24em] text-gold">{t('public.member.gateway.panelEyebrow')}</p>
                  <h2 className="mt-4 max-w-md font-display text-3xl font-bold leading-[1.12] !text-white sm:text-[38px]">
                    {t('public.member.gateway.panelTitle')}
                  </h2>
                  <p className="mt-5 max-w-md text-sm leading-7 text-green-dim">
                    {t('public.member.gateway.panelDescription')}
                  </p>
                </div>

                <ul className="mt-9 grid gap-3 sm:grid-cols-2 lg:grid-cols-1" aria-label={t('public.member.advantages.title')}>
                  {highlightedBenefits.map(([icon, key]) => (
                    <li key={key} className="flex items-center gap-3 rounded-xl border border-white/10 bg-white/[0.045] px-4 py-3.5">
                      <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-gold/15 text-base text-gold">
                        <i className={icon} aria-hidden="true" />
                      </span>
                      <span className="text-sm leading-5 text-white/90">{t(key)}</span>
                    </li>
                  ))}
                </ul>

                <div className="relative mt-auto pt-10">
                  <div className="border-t border-white/15 pt-6">
                    <p className="text-xs font-bold uppercase tracking-[0.16em] text-green-dim">{t('public.member.help.title')}</p>
                    <p className="mt-2 text-sm leading-6 text-white/75">{t('public.member.help.description')}</p>
                    <ArrowLink to="/contact" tone="white" className="mt-4">{t('public.member.help.cta')}</ArrowLink>
                  </div>
                </div>
              </div>
            </aside>

            <div className="order-1 px-6 py-8 sm:px-10 sm:py-10 lg:order-2 lg:px-12 lg:py-12">
              <div
                className="grid grid-cols-2 rounded-xl border border-line bg-canvas p-1.5"
                role="tablist"
                aria-label={t('public.member.gateway.modeLabel')}
              >
                <GatewayTab
                  active={mode === 'login'}
                  controls="member-login-panel"
                  icon="ri-login-circle-line"
                  onClick={() => setMode('login')}
                >
                  {t('public.member.gateway.loginTab')}
                </GatewayTab>
                <GatewayTab
                  active={mode === 'signup'}
                  controls="member-signup-panel"
                  icon="ri-user-add-line"
                  onClick={() => setMode('signup')}
                >
                  {t('public.member.gateway.signupTab')}
                </GatewayTab>
              </div>

              <div className="mt-8">
                {mode === 'login' ? (
                  <div id="member-login-panel" role="tabpanel">
                    <MemberLoginForm embedded />
                  </div>
                ) : (
                  <div id="member-signup-panel" role="tabpanel">
                    <MemberLoginForm mode="signup" embedded />
                    <MemberRegistrationForm onSwitchToLogin={() => setMode('login')} />
                  </div>
                )}
              </div>
            </div>
          </section>
        )}
      </main>
      {!hasMemberSession && <Footer />}
    </div>
  );
};

const GatewayTab = ({
  active,
  controls,
  icon,
  onClick,
  children,
}: {
  active: boolean;
  controls: string;
  icon: string;
  onClick: () => void;
  children: ReactNode;
}) => (
  <button
    type="button"
    role="tab"
    aria-selected={active}
    aria-controls={controls}
    onClick={onClick}
    className={`flex min-h-12 items-center justify-center gap-2 rounded-lg px-3 text-[11px] font-bold uppercase tracking-[0.12em] transition-all focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gold ${
      active
        ? 'bg-green-deep text-white shadow-[0_8px_22px_rgba(0,59,27,.18)]'
        : 'text-ink-variant hover:bg-surface hover:text-green'
    }`}
  >
    <i className={icon} aria-hidden="true" />
    <span>{children}</span>
  </button>
);

export default EspaceMembrePage;

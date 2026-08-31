import { useAuth } from '../../../contexts/AuthContext';
import { LanguageSwitcher } from '../../../components/admin/LanguageSwitcher';
import { HcbeLogoMark } from '../../../components/brand/HcbeLogo';
import { Button, Field } from '../../../components/ui';
import ThemeToggle from '../../../components/feature/ThemeToggle';

const loginInputClasses =
  'min-h-[52px] w-full rounded-xl border border-green/15 bg-surface-container py-3 pl-12 pr-4 text-body-md text-ink transition-[background-color,border-color,box-shadow] duration-200 placeholder:text-ink-variant/50 hover:border-green/35 focus:border-green focus:bg-surface focus:outline-none focus:ring-4 focus:ring-green/10';

const mapLoginError = (message: string | undefined, t: (key: string) => string) => {
  const normalized = (message ?? '').toLowerCase();
  if (
    !message ||
    normalized.includes('invalid email') ||
    normalized.includes('invalid password') ||
    normalized.includes('unauthorized') ||
    normalized.includes('401')
  ) {
    return t('admin.login.invalidCredentials');
  }
  return t('admin.login.failed');
};

export const AdminLoginPage = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const { t } = useTranslation();

  const { login, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname || '/admin/dashboard';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      const result = await login(email.trim(), password);

      if (result.success) {
        const storedUser = localStorage.getItem('hcbe_user');
        const loggedInUser = storedUser ? JSON.parse(storedUser) : null;

        if (!loggedInUser?.isAdmin) {
          logout();
          setError(t('admin.login.notAdmin'));
          return;
        }

        navigate(from, { replace: true });
      } else {
        setError(mapLoginError(result.message, t));
      }
    } catch {
      setError(t('admin.common.errorUnexpected'));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-surface-container lg:grid lg:grid-cols-[minmax(0,1.08fr)_minmax(480px,0.92fr)]">
      <section className="public-grid-pattern relative flex min-h-[460px] flex-col overflow-hidden bg-green-deep px-5 py-7 text-white sm:px-10 sm:py-10 lg:min-h-screen lg:px-14 lg:py-12 xl:px-20">
        <div className="pointer-events-none absolute -left-24 bottom-[-160px] h-[420px] w-[420px] rounded-full border-[72px] border-white/[0.035]" aria-hidden="true" />
        <div className="pointer-events-none absolute -right-20 top-24 h-56 w-56 rounded-full border-[42px] border-gold/[0.055]" aria-hidden="true" />
        <div className="absolute inset-y-0 right-0 w-px bg-white/10" aria-hidden="true" />

        <div className="relative flex items-center justify-between gap-5">
          <Link to="/" className="group inline-flex items-center gap-3 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-gold">
            <HcbeLogoMark size="md" tone="dark" />
            <span className="hidden h-5 w-px bg-white/20 sm:block" aria-hidden="true" />
            <span className="hidden text-[10px] font-bold uppercase tracking-[0.16em] text-white/55 transition-colors group-hover:text-gold sm:block">
              {t('admin.login.badge')}
            </span>
          </Link>
          <div className="flex items-center gap-2">
            <ThemeToggle variant="onDark" />
            <LanguageSwitcher variant="onDark" />
          </div>
        </div>

        <div className="relative my-auto max-w-[620px] py-12 lg:py-16">
          <div className="inline-flex items-center gap-3 rounded-full border border-white/15 bg-white/[0.06] px-4 py-2 backdrop-blur-sm">
            <span className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-gold opacity-40 motion-reduce:animate-none" />
              <span className="relative inline-flex h-2 w-2 rounded-full bg-gold" />
            </span>
            <span className="text-[10px] font-bold uppercase tracking-[0.18em] text-white/85">
              {t('admin.login.workspaceLabel')}
            </span>
          </div>

          <h1 className="mt-6 max-w-[580px] font-display text-[40px] font-bold leading-[1.02] tracking-[-0.035em] text-white sm:text-[52px] lg:text-[60px]">
            {t('admin.login.title')}
          </h1>
          <p className="mt-6 max-w-[570px] border-l-2 border-gold pl-5 text-[16px] leading-7 text-white/68 sm:text-[17px]">
            {t('admin.login.subtitle')}
          </p>

          <div className="mt-9 hidden overflow-hidden rounded-[18px] border border-white/12 bg-white/[0.045] backdrop-blur-sm sm:grid sm:grid-cols-3">
            {[
              ['ri-file-list-3-line', t('admin.nav.groups.content')],
              ['ri-community-line', t('admin.nav.groups.community')],
              ['ri-team-line', t('admin.nav.groups.members')],
            ].map(([icon, label], index) => (
              <div key={label} className="border-white/10 p-5 sm:border-r sm:last:border-r-0">
                <div className="flex items-center justify-between">
                  <i className={`${icon} text-xl text-gold`} aria-hidden="true" />
                  <span className="text-[10px] font-bold tabular-nums text-white/30">0{index + 1}</span>
                </div>
                <p className="mt-5 text-[11px] font-bold uppercase tracking-[0.13em] text-white/80">{label}</p>
              </div>
            ))}
          </div>
        </div>

        <div className="relative hidden items-center justify-between gap-6 text-xs text-white/45 lg:flex">
          <Link to="/" className="group inline-flex min-h-[44px] items-center gap-2 font-bold uppercase tracking-[0.12em] transition-colors hover:text-gold">
            <i className="ri-arrow-left-line transition-transform group-hover:-translate-x-1" aria-hidden="true" />
            {t('admin.login.backToSite')}
          </Link>
          <span className="inline-flex items-center gap-2">
            <i className="ri-shield-check-line text-gold" aria-hidden="true" />
            {t('admin.login.sessionProtected')}
          </span>
        </div>
      </section>

      <section className="relative flex min-h-[620px] items-center justify-center overflow-hidden px-5 py-12 sm:px-10 lg:min-h-screen lg:py-16">
        <div className="pointer-events-none absolute right-[-120px] top-[-90px] h-72 w-72 rounded-full border-[54px] border-green/[0.035]" aria-hidden="true" />

        <div className="relative w-full max-w-[490px]">
          <div className="overflow-hidden rounded-[24px] border border-green/10 bg-white shadow-[0_30px_90px_rgba(0,59,27,.12)]">
            <div className="flex items-center justify-between border-b border-green/10 px-6 py-5 sm:px-8">
              <div className="flex items-center gap-3">
                <span className="flex h-10 w-10 items-center justify-center rounded-full bg-green text-gold">
                  <i className="ri-lock-2-line text-lg" aria-hidden="true" />
                </span>
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-red-link">
                    {t('admin.login.badge')}
                  </p>
                  <p className="mt-0.5 text-xs text-ink-variant">HCBE Canada</p>
                </div>
              </div>
              <span className="inline-flex items-center gap-2 rounded-full bg-green/5 px-3 py-1.5 text-[10px] font-bold uppercase tracking-[0.12em] text-green">
                <span className="h-1.5 w-1.5 rounded-full bg-green" aria-hidden="true" />
                {t('admin.login.secureAccess')}
              </span>
            </div>

            <div className="px-6 py-8 sm:px-8 sm:py-9">
              <h2 className="font-display text-[32px] font-bold leading-tight text-green">
                {t('admin.login.signIn')}
              </h2>
              <p className="mt-3 max-w-sm text-body-md leading-6 text-ink-variant">{t('admin.login.formHint')}</p>

              <form className="mt-8 flex flex-col gap-6" onSubmit={handleSubmit}>
                {error && (
                  <div role="alert" aria-live="polite" className="flex items-start gap-3 rounded-xl border border-error/25 bg-error/5 p-4 text-body-md text-error">
                    <i className="ri-error-warning-line mt-0.5 shrink-0 text-lg" aria-hidden="true" />
                    <span>{error}</span>
                  </div>
                )}

                <Field label={t('admin.common.email')} htmlFor="email" required>
                  <div className="relative">
                    <i className="ri-mail-line pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-lg text-green/55" aria-hidden="true" />
                    <input
                      id="email"
                      name="email"
                      type="email"
                      autoComplete="email"
                      required
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      className={loginInputClasses}
                      placeholder={t('admin.login.emailPlaceholder')}
                    />
                  </div>
                </Field>

                <Field label={t('admin.common.password')} htmlFor="password" required>
                  <div className="relative">
                    <i className="ri-key-2-line pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-lg text-green/55" aria-hidden="true" />
                    <input
                      id="password"
                      name="password"
                      type={showPassword ? 'text' : 'password'}
                      autoComplete="current-password"
                      required
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      className={`${loginInputClasses} pr-12`}
                      placeholder={t('admin.login.passwordPlaceholder')}
                    />
                    <button
                      type="button"
                      onClick={() => setShowPassword((visible) => !visible)}
                      aria-label={t(showPassword ? 'admin.login.hidePassword' : 'admin.login.showPassword')}
                      aria-pressed={showPassword}
                      className="absolute right-2 top-1/2 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-lg text-lg text-ink-variant transition-colors hover:bg-green/5 hover:text-green focus-visible:outline focus-visible:outline-2 focus-visible:outline-green"
                    >
                      <i className={showPassword ? 'ri-eye-off-line' : 'ri-eye-line'} aria-hidden="true" />
                    </button>
                  </div>
                </Field>

                <Button type="submit" variant="primary" disabled={isLoading} className="mt-1 w-full py-3.5 shadow-[0_12px_30px_rgba(255,205,0,.22)]">
                  {isLoading ? (
                    <>
                      <i className="ri-loader-4-line animate-spin" aria-hidden="true" />
                      {t('admin.login.signingIn')}
                    </>
                  ) : (
                    <>
                      {t('admin.login.signIn')}
                      <i className="ri-arrow-right-line" aria-hidden="true" />
                    </>
                  )}
                </Button>
              </form>
            </div>

            <div className="flex items-start gap-3 border-t border-line/50 bg-surface-container px-6 py-5 sm:px-8">
              <i className="ri-shield-keyhole-line mt-0.5 shrink-0 text-lg text-green" aria-hidden="true" />
              <p className="text-xs leading-5 text-ink-variant">{t('admin.login.secureAccessHint')}</p>
            </div>
          </div>

          <Link to="/" className="mt-6 inline-flex min-h-[44px] items-center gap-2 text-[10px] font-bold uppercase tracking-[0.13em] text-green transition-colors hover:text-red-link lg:hidden">
            <i className="ri-arrow-left-line" aria-hidden="true" />
            {t('admin.login.backToSite')}
          </Link>
        </div>
      </section>
    </div>
  );
};

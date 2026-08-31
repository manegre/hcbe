import { useTranslation } from 'react-i18next';
import { useTheme } from '../../contexts/ThemeContext';

interface ThemeToggleProps {
  variant?: 'default' | 'onDark';
  className?: string;
}

const ThemeToggle = ({ variant = 'default', className = '' }: ThemeToggleProps) => {
  const { theme, toggleTheme } = useTheme();
  const { t } = useTranslation();
  const isDark = theme === 'dark';
  const label = t(isDark ? 'theme.enableLight' : 'theme.enableDark');

  return (
    <button
      type="button"
      onClick={toggleTheme}
      aria-label={label}
      title={label}
      aria-pressed={isDark}
      className={`group relative flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-full border transition-all duration-300 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 ${
        variant === 'onDark'
          ? 'border-white/20 bg-white/[0.07] text-gold hover:border-gold/60 hover:bg-white/[0.12] focus-visible:outline-gold'
          : 'border-green/10 bg-surface text-green shadow-[0_5px_18px_rgba(0,59,27,.07)] hover:-translate-y-0.5 hover:border-gold/70 focus-visible:outline-green'
      } ${className}`}
    >
      <span className="absolute inset-0 scale-0 rounded-full bg-gold/12 transition-transform duration-300 group-hover:scale-100" aria-hidden="true" />
      <i className={`${isDark ? 'ri-sun-line' : 'ri-moon-clear-line'} relative text-lg transition-transform duration-300 group-hover:rotate-12`} aria-hidden="true" />
    </button>
  );
};

export default ThemeToggle;

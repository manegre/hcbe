type HcbeLogoSize = 'sm' | 'md' | 'lg';

const wordmarkSize: Record<HcbeLogoSize, string> = {
  sm: 'text-base',
  md: 'text-xl',
  lg: 'text-2xl',
};

const flagSize: Record<HcbeLogoSize, string> = {
  sm: 'h-4 w-6',
  md: 'h-5 w-8',
  lg: 'h-6 w-9',
};

const BurkinaFlag = ({ className }: { className: string }) => (
  <svg viewBox="0 0 24 16" className={className} aria-hidden="true">
    <rect width="24" height="8" fill="#EF3340" />
    <rect y="8" width="24" height="8" fill="#14532D" />
    <path d="M12 4.4l1.1 2.8 3 .2-2.3 1.9.8 2.9L12 10.5 9.4 12.2l.8-2.9L7.9 7.4l3-.2z" fill="#FFCD00" />
  </svg>
);

const CanadaFlag = ({ className }: { className: string }) => (
  <svg viewBox="0 0 24 16" className={className} aria-hidden="true">
    <rect width="24" height="16" fill="#FFFFFF" />
    <rect width="6" height="16" fill="#D52B1E" />
    <rect x="18" width="6" height="16" fill="#D52B1E" />
    <path d="M12 4l1.1 2.6 2.2-.7-1 2.4 1.5 1.1-2 .5.2 2-2-1.4-2 1.4.2-2-2-.5 1.5-1.1-1-2.4 2.2.7z" fill="#D52B1E" />
  </svg>
);

interface HcbeLogoMarkProps {
  size?: HcbeLogoSize;
  tone?: 'light' | 'dark';
  className?: string;
}

export const HcbeLogoMark = ({ size = 'md', tone = 'light', className = '' }: HcbeLogoMarkProps) => (
  <span className={`inline-flex shrink-0 items-center gap-2 ${className}`}>
    <BurkinaFlag className={flagSize[size]} />
    <span className={`font-sans font-bold ${wordmarkSize[size]}`}>
      <span className={tone === 'dark' ? 'text-white' : 'text-green'}>HC</span>
      <span className="text-gold">BE</span>
      <span className="ml-1 text-red">Canada</span>
    </span>
    <CanadaFlag className={flagSize[size]} />
  </span>
);

interface HcbeLogoProps {
  size?: HcbeLogoSize;
  showWordmark?: boolean;
  subtitle?: string;
  tone?: 'light' | 'dark';
  className?: string;
}

export const HcbeLogo = ({
  size = 'md',
  showWordmark = true,
  subtitle,
  tone = 'light',
  className = '',
}: HcbeLogoProps) => (
  <div className={`flex flex-col gap-1 ${className}`}>
    {showWordmark && <HcbeLogoMark size={size} tone={tone} />}
    {subtitle && (
      <span className={`text-body-md ${tone === 'dark' ? 'text-green-dim' : 'text-ink-variant'}`}>{subtitle}</span>
    )}
  </div>
);

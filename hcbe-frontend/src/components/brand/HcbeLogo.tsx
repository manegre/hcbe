import canadaFlag from '../../assets/flags/canada.png';

type HcbeLogoSize = 'sm' | 'md' | 'lg';

const wordmarkSize: Record<HcbeLogoSize, string> = {
  sm: 'text-[15px]',
  md: 'text-[19px]',
  lg: 'text-[23px]',
};

const burkinaFlagSize: Record<HcbeLogoSize, string> = {
  sm: 'h-4 w-6',
  md: 'h-5 w-[30px]',
  lg: 'h-6 w-9',
};

const canadaFlagSize: Record<HcbeLogoSize, string> = {
  sm: 'h-4 w-8',
  md: 'h-5 w-10',
  lg: 'h-6 w-12',
};

const BurkinaFlag = ({ className }: { className: string }) => (
  <svg viewBox="0 0 36 24" className={className} aria-hidden="true" xmlns="http://www.w3.org/2000/svg">
    <rect width="36" height="12" fill="#EF2B2D" />
    <rect y="12" width="36" height="12" fill="#009E49" />
    <polygon
      points="18,7.2 19.3,11.2 23.5,11.2 20.1,13.7 21.4,17.7 18,15.2 14.6,17.7 15.9,13.7 12.5,11.2 16.7,11.2"
      fill="#FCD116"
    />
  </svg>
);

const CanadaFlag = ({ className }: { className: string }) => (
  <img src={canadaFlag} alt="" aria-hidden="true" className={`object-cover ${className}`} />
);

interface HcbeLogoMarkProps {
  size?: HcbeLogoSize;
  tone?: 'light' | 'dark';
  className?: string;
}

export const HcbeLogoMark = ({ size = 'md', tone = 'light', className = '' }: HcbeLogoMarkProps) => (
  <span className={`inline-flex shrink-0 items-center gap-2.5 ${className}`}>
    <span className="overflow-hidden rounded-[3px] shadow-[0_1px_3px_rgba(0,0,0,.14)] ring-1 ring-black/10">
      <BurkinaFlag className={`${burkinaFlagSize[size]} block`} />
    </span>
    <span className={`inline-flex items-baseline whitespace-nowrap font-sans font-bold leading-none ${wordmarkSize[size]}`}>
      <span className={`tracking-[-0.035em] ${tone === 'dark' ? 'text-white' : 'text-green-deep'}`}>HCBE</span>
      <span className="mx-1.5 self-center text-[0.52em] text-gold" aria-hidden="true">◆</span>
      <span className={`tracking-[-0.025em] ${tone === 'dark' ? 'text-white/90' : 'text-red-link'}`}>Canada</span>
    </span>
    <span className="overflow-hidden rounded-[3px] shadow-[0_1px_3px_rgba(0,0,0,.14)] ring-1 ring-black/10">
      <CanadaFlag className={`${canadaFlagSize[size]} block`} />
    </span>
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

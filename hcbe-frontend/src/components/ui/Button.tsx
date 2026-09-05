import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';

type Variant = 'primary' | 'secondary' | 'tertiary' | 'destructive';

const variants: Record<Variant, string> = {
  primary:
    'bg-gold text-green-deep hover:bg-gold-dim border border-transparent px-6 py-3 focus-visible:outline-green',
  secondary:
    'bg-transparent text-green border-2 border-green hover:bg-green hover:text-white px-6 py-3 focus-visible:outline-green',
  tertiary: 'bg-transparent text-red-link hover:text-green border-0 focus-visible:outline-red-link',
  destructive:
    'bg-error text-white hover:bg-error-deep border border-transparent px-6 py-3 focus-visible:outline-error',
};

interface ButtonProps {
  variant?: Variant;
  to?: string;
  href?: string;
  type?: 'button' | 'submit';
  disabled?: boolean;
  onClick?: () => void;
  className?: string;
  children: ReactNode;
}

export const Button = ({
  variant = 'primary',
  to,
  href,
  type = 'button',
  disabled = false,
  onClick,
  className = '',
  children,
}: ButtonProps) => {
  // `active:translate-y-px` donne le retour d'enfoncement sans ombre ni
  // changement de taille ; `motion-reduce` le neutralise.
  const base =
    'inline-flex min-h-[44px] items-center justify-center gap-2 rounded-control text-label-md uppercase transition-[background-color,border-color,color,transform] duration-200 active:translate-y-px motion-reduce:transform-none focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:opacity-50 disabled:pointer-events-none';
  const classes = `${base} ${variants[variant]} ${className}`;

  if (to) return <Link to={to} className={classes}>{children}</Link>;
  if (href) return <a href={href} className={classes}>{children}</a>;
  return (
    <button type={type} disabled={disabled} onClick={onClick} className={classes}>
      {children}
    </button>
  );
};

const tones = {
  red: 'text-red-link hover:text-green focus-visible:outline-red-link',
  green: 'text-green hover:text-green-deep focus-visible:outline-green',
  gold: 'text-gold hover:text-white focus-visible:outline-gold',
  goldInk: 'text-gold-ink hover:text-green focus-visible:outline-gold-ink',
  white: 'text-white hover:text-gold focus-visible:outline-white',
};

interface ArrowLinkProps {
  to: string;
  tone?: keyof typeof tones;
  className?: string;
  children: ReactNode;
}

export const ArrowLink = ({ to, tone = 'red', className = '', children }: ArrowLinkProps) => (
  <Link
    to={to}
    className={`group inline-flex min-h-[44px] items-center gap-2 rounded-control text-label-md uppercase transition-colors duration-200 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 ${tones[tone]} ${className}`}
  >
    {children}
    <i
      className="ri-arrow-right-line text-base transition-transform duration-200 group-hover:translate-x-1 motion-reduce:transform-none"
      aria-hidden="true"
    ></i>
  </Link>
);

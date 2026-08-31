import type { ReactNode } from 'react';

const hovers = {
  red: 'hover:border-red',
  gold: 'hover:border-gold',
  green: 'hover:border-green',
  none: '',
};

interface CardProps {
  hover?: keyof typeof hovers;
  className?: string;
  children: ReactNode;
}

export const Card = ({ hover = 'none', className = '', children }: CardProps) => (
  <div
    className={`rounded-[18px] border border-green/10 bg-surface p-7 shadow-[0_10px_35px_rgba(0,59,27,.055)] transition-[transform,border-color,box-shadow] duration-300 hover:-translate-y-1 hover:shadow-[0_18px_45px_rgba(0,59,27,.11)] ${hovers[hover]} ${className}`}
  >
    {children}
  </div>
);

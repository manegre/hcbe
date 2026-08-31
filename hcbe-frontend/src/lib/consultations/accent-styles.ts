export const getConsultationAccentClasses = (accentColor: string) => {
  if (accentColor === 'amber') {
    return {
      iconBg: 'bg-gold/10 text-gold-ink',
      button: 'bg-gold text-green hover:bg-gold-dim',
    };
  }

  return {
    iconBg: 'bg-green/10 text-green',
    button: 'bg-green text-white hover:bg-green-deep',
  };
};

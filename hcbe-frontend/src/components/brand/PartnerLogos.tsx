// Logotypes fictifs servant à composer le bandeau « Nos partenaires ».
// Aucune de ces huit entreprises n'existe : ce sont des espaces réservés qui
// doivent être remplacés par les marques des partenaires réels (et par des
// accords de partenariat confirmés) avant la mise en ligne du site.
//
// Ils sont composés en HTML plutôt qu'en <text> SVG : la largeur d'un mot
// dépend de la fonte chargée, et un viewBox fixe obligerait à comprimer les
// glyphes pour atteindre une largeur devinée. En HTML chaque logotype prend sa
// largeur naturelle, ce qui donne au passage la variété de gabarits d'une vraie
// rangée de partenaires. Tout hérite de `currentColor`.

interface PartnerLogoProps {
  className?: string;
}

const root = (className: string) => `inline-flex items-center ${className}`;

export const FasoEnergieLogo = ({ className = '' }: PartnerLogoProps) => (
  <span className={root(`gap-2.5 ${className}`)}>
    <span className="h-2.5 w-2.5 shrink-0 rounded-full bg-current" />
    <span className="text-[13px] font-semibold uppercase leading-none tracking-[0.18em]">
      Faso Énergie
    </span>
  </span>
);

export const NakomseCapitalLogo = ({ className = '' }: PartnerLogoProps) => (
  <span className={root(`gap-3 ${className}`)}>
    <span className="font-display text-[21px] font-bold leading-none">Nakomsé</span>
    <span className="h-5 w-px shrink-0 bg-current" />
    <span className="font-display text-[21px] font-semibold leading-none">Capital</span>
  </span>
);

export const SahelLogistiqueLogo = ({ className = '' }: PartnerLogoProps) => (
  <span className={root(className)}>
    <span className="text-[12px] font-normal uppercase leading-none tracking-[0.32em]">
      Sahel Logistique
    </span>
  </span>
);

export const BorealAssuranceLogo = ({ className = '' }: PartnerLogoProps) => (
  <span className={root(`flex-col items-start gap-1 ${className}`)}>
    <span className="font-display text-[18px] font-bold leading-none">Boréal</span>
    <span className="text-[9px] font-normal uppercase leading-none tracking-[0.26em]">
      Assurance
    </span>
  </span>
);

export const KariteCooperativeLogo = ({ className = '' }: PartnerLogoProps) => (
  <span className={root(`gap-2.5 ${className}`)}>
    <span className="h-3 w-3 shrink-0 rounded-full border-2 border-current" />
    <span className="font-display text-[21px] font-semibold italic leading-none">
      Karité Coopérative
    </span>
  </span>
);

export const OuagaTechLogo = ({ className = '' }: PartnerLogoProps) => (
  <span className={root(`gap-2 ${className}`)}>
    <span className="text-[20px] font-semibold lowercase leading-none tracking-[-0.03em]">
      ouaga tech
    </span>
    <span className="h-2 w-2 shrink-0 bg-current" />
  </span>
);

export const ZongoFilsLogo = ({ className = '' }: PartnerLogoProps) => (
  <span className={root(className)}>
    <span className="font-display border-y border-current py-1.5 text-[13px] font-semibold uppercase leading-none tracking-[0.2em]">
      Zongo &amp; Fils
    </span>
  </span>
);

export const LaurentideMobiliteLogo = ({ className = '' }: PartnerLogoProps) => (
  <span className={root(`gap-2.5 ${className}`)}>
    <svg viewBox="0 0 10 12" className="h-3 w-2.5 shrink-0" fill="currentColor" aria-hidden="true">
      <path d="M0 0 L10 6 L0 12 Z" />
    </svg>
    <span className="text-[18px] font-semibold leading-none tracking-[-0.01em]">
      Laurentide Mobilité
    </span>
  </span>
);

export const partnerLogos = [
  { name: 'Faso Énergie', Logo: FasoEnergieLogo },
  { name: 'Nakomsé Capital', Logo: NakomseCapitalLogo },
  { name: 'Sahel Logistique', Logo: SahelLogistiqueLogo },
  { name: 'Boréal Assurance', Logo: BorealAssuranceLogo },
  { name: 'Karité Coopérative', Logo: KariteCooperativeLogo },
  { name: 'Ouaga Tech', Logo: OuagaTechLogo },
  { name: 'Zongo & Fils', Logo: ZongoFilsLogo },
  { name: 'Laurentide Mobilité', Logo: LaurentideMobiliteLogo },
] as const;

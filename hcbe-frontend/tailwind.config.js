/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        green: { DEFAULT: '#14532D', deep: '#003B1B', dim: '#96D5A3' },
        gold: { DEFAULT: '#FFCD00', dim: '#F0C100', ink: '#735C00' },
        red: { DEFAULT: '#EF3340', link: '#C1121F', deep: '#9E0F19' },
        error: { DEFAULT: '#BA1A1A', deep: '#93000A' },
        paper: 'rgb(var(--color-paper) / <alpha-value>)',
        background: 'rgb(var(--color-background) / <alpha-value>)',
        surface: {
          DEFAULT: 'rgb(var(--color-surface) / <alpha-value>)',
          container: 'rgb(var(--color-surface-container) / <alpha-value>)',
        },
        line: 'rgb(var(--color-line) / <alpha-value>)',
        outline: 'rgb(var(--color-outline) / <alpha-value>)',
        ink: {
          DEFAULT: 'rgb(var(--color-ink) / <alpha-value>)',
          variant: 'rgb(var(--color-ink-variant) / <alpha-value>)',
        },
      },
      fontFamily: {
        display: ['Newsreader', 'Georgia', 'Times New Roman', 'serif'],
        sans: ['"Public Sans"', 'Helvetica', 'Arial', 'sans-serif'],
      },
      fontSize: {
        'headline-xl': ['40px', { lineHeight: '44px', letterSpacing: '-0.025em', fontWeight: '700' }],
        'headline-xl-m': ['30px', { lineHeight: '34px', letterSpacing: '-0.02em', fontWeight: '700' }],
        'headline-lg': ['28px', { lineHeight: '34px', letterSpacing: '-0.02em', fontWeight: '700' }],
        'headline-md': ['22px', { lineHeight: '28px', letterSpacing: '-0.01em', fontWeight: '600' }],
        'headline-sm': ['18px', { lineHeight: '26px', letterSpacing: '-0.005em', fontWeight: '600' }],
        'body-lg': ['17px', { lineHeight: '26px' }],
        'body-md': ['16px', { lineHeight: '24px' }],
        'label-md': ['13px', { lineHeight: '18px', letterSpacing: '0.08em', fontWeight: '600' }],
      },
      // `control` n'adoucit que ce qui se clique — boutons, champs, puces.
      // Les cartes, panneaux et tableaux restent à angle vif : la profondeur de
      // ce système reste portée par les filets de 1px, pas par l'arrondi.
      borderRadius: {
        DEFAULT: '0px',
        none: '0px',
        control: '6px',
        'control-sm': '3px',
        full: '9999px',
      },
      boxShadow: { none: 'none' },
      spacing: { gutter: '24px', 'margin-mobile': '16px', 'margin-desktop': '64px' },
      maxWidth: { container: '1200px' },
    },
  },
  plugins: [],
};

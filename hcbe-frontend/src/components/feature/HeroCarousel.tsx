import type { ReactNode } from 'react';
import { InstitutionalFlags } from '../brand/InstitutionalFlags';

interface Slide {
  src: string;
  alt: string;
}

interface HeroCarouselProps {
  slides: Slide[];
  children: ReactNode;
}

const SLIDE_MS = 7000;

export const HeroCarousel = ({ slides, children }: HeroCarouselProps) => {
  const [active, setActive] = useState(0);
  const [paused, setPaused] = useState(false);
  const [mounted, setMounted] = useState(false);
  const { t } = useTranslation();

  const reducedMotion =
    typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  useEffect(() => {
    if (slides.length < 2 || paused || reducedMotion) return;
    const id = window.setInterval(() => setActive((i) => (i + 1) % slides.length), SLIDE_MS);
    return () => window.clearInterval(id);
  }, [slides.length, paused, reducedMotion]);

  useEffect(() => {
    const id = window.requestAnimationFrame(() => setMounted(true));
    return () => window.cancelAnimationFrame(id);
  }, []);

  return (
    <section
      className="relative isolate flex min-h-[570px] flex-col overflow-hidden bg-green-deep md:min-h-[650px]"
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onFocusCapture={() => setPaused(true)}
      onBlurCapture={() => setPaused(false)}
    >
      {slides.map((slide, index) => (
        <img
          key={slide.src}
          src={slide.src}
          alt=""
          aria-hidden="true"
          className={`pointer-events-none absolute inset-0 h-full w-full object-cover transition-[opacity,transform] duration-[1400ms] ${
            index === active ? 'scale-105 opacity-100' : 'scale-100 opacity-0'
          }`}
        />
      ))}

      <div className="pointer-events-none absolute inset-0 bg-gradient-to-r from-[#001f11]/95 via-[#002c19]/72 to-[#00180b]/15" aria-hidden="true"></div>
      <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-[#001b0e]/70 via-transparent to-[#001b0e]/15" aria-hidden="true"></div>
      <div className="public-grid-pattern pointer-events-none absolute inset-y-0 left-0 w-[56%] opacity-25" aria-hidden="true"></div>

      <div className={`hero-content relative z-10 flex flex-1 items-center ${mounted ? 'is-visible' : ''}`}>
        {children}
      </div>

      {slides.length > 1 && (
        <div className="container-page relative z-10 flex items-center justify-between pb-10">
          <div className="hidden sm:block">
            <InstitutionalFlags variant="signature" />
          </div>
          <div className="ml-auto flex items-center gap-2 rounded-full border border-white/15 bg-black/15 px-3 py-1.5 backdrop-blur-md">
          {slides.map((slide, index) => (
            <button
              key={slide.src}
              type="button"
              onClick={() => setActive(index)}
              aria-label={t('public.home.hero.slide', { index: index + 1 })}
              aria-current={index === active}
              className="group flex h-8 items-center justify-center"
            >
              <span
                className={`block h-1 rounded-full transition-all duration-500 ${
                  index === active ? 'w-9 bg-gold' : 'w-4 bg-white/35 group-hover:bg-white/70'
                }`}
              ></span>
            </button>
          ))}
          <span className="ml-2 min-w-[34px] border-l border-white/15 pl-3 font-mono text-[11px] text-white/70">
            0{active + 1}
          </span>
          </div>
        </div>
      )}
    </section>
  );
};

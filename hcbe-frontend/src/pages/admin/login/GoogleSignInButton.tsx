interface GoogleCredentialResponse {
  credential?: string;
}

interface GoogleIdentityServices {
  accounts: {
    id: {
      initialize: (configuration: {
        client_id: string;
        callback: (response: GoogleCredentialResponse) => void;
        auto_select?: boolean;
        cancel_on_tap_outside?: boolean;
      }) => void;
      renderButton: (
        parent: HTMLElement,
        options: {
          type: 'standard';
          theme: 'outline';
          size: 'large';
          text: 'continue_with';
          shape: 'rectangular';
          logo_alignment: 'left';
          width: number;
          locale: string;
        },
      ) => void;
    };
  };
}

declare global {
  interface Window {
    google?: GoogleIdentityServices;
  }
}

const googleScriptId = 'google-identity-services';

interface GoogleSignInButtonProps {
  disabled?: boolean;
  onCredential: (credential: string) => void;
  onUnavailable: () => void;
}

export const GoogleSignInButton = ({
  disabled = false,
  onCredential,
  onUnavailable,
}: GoogleSignInButtonProps) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const { i18n } = useTranslation();
  const clientId = (import.meta.env.VITE_GOOGLE_CLIENT_ID ?? '').trim();

  useEffect(() => {
    if (!clientId) return;

    let active = true;
    const render = () => {
      const container = containerRef.current;
      if (!active || !container || !window.google) return;

      container.replaceChildren();
      window.google.accounts.id.initialize({
        client_id: clientId,
        auto_select: false,
        cancel_on_tap_outside: true,
        callback: (response) => {
          if (response.credential) onCredential(response.credential);
          else onUnavailable();
        },
      });
      window.google.accounts.id.renderButton(container, {
        type: 'standard',
        theme: 'outline',
        size: 'large',
        text: 'continue_with',
        shape: 'rectangular',
        logo_alignment: 'left',
        width: Math.min(400, Math.max(240, container.clientWidth)),
        locale: i18n.resolvedLanguage?.startsWith('en') ? 'en' : 'fr',
      });
    };

    if (window.google) {
      render();
      return () => {
        active = false;
      };
    }

    let script = document.getElementById(googleScriptId) as HTMLScriptElement | null;
    if (!script) {
      script = document.createElement('script');
      script.id = googleScriptId;
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      document.head.appendChild(script);
    }

    script.addEventListener('load', render);
    script.addEventListener('error', onUnavailable);
    return () => {
      active = false;
      script?.removeEventListener('load', render);
      script?.removeEventListener('error', onUnavailable);
    };
  }, [clientId, i18n.resolvedLanguage, onCredential, onUnavailable]);

  if (!clientId) return null;

  return (
    <div className={`relative flex min-h-11 justify-center transition-opacity ${disabled ? 'opacity-55' : ''}`}>
      <div ref={containerRef} className="flex min-h-11 w-full justify-center" />
      {disabled && <span className="absolute inset-0 cursor-wait" aria-hidden="true" />}
    </div>
  );
};

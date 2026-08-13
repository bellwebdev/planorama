import { useEffect, useRef } from "react";

interface GoogleCredentialResponse {
  credential: string;
}

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string;
            callback: (response: GoogleCredentialResponse) => void;
          }) => void;
          renderButton: (
            parent: HTMLElement,
            options: { type?: string; theme?: string; size?: string; shape?: string; width?: number },
          ) => void;
        };
      };
    };
  }
}

/** Renders Google's own "Sign in with Google" button into the returned ref's element.
 * Requires the GSI script tag in index.html (loaded async/defer, so we poll briefly for it). */
export function useGoogleSignIn(onCredential: (idToken: string) => void) {
  const buttonRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
    if (!clientId || !buttonRef.current) return;

    let cancelled = false;

    function render() {
      if (cancelled || !window.google || !buttonRef.current) return;
      window.google.accounts.id.initialize({
        client_id: clientId,
        callback: (response) => onCredential(response.credential),
      });
      window.google.accounts.id.renderButton(buttonRef.current, {
        type: "standard",
        theme: "outline",
        size: "large",
        shape: "pill",
        width: 320,
      });
    }

    if (window.google) {
      render();
      return;
    }

    const interval = setInterval(() => {
      if (window.google) {
        clearInterval(interval);
        render();
      }
    }, 100);

    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, [onCredential]);

  return buttonRef;
}

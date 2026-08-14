import { useCallback, useEffect, useRef, useState } from "react";

declare global {
  interface Window {
    turnstile?: {
      render: (
        container: HTMLElement,
        options: {
          sitekey: string;
          callback: (token: string) => void;
          "expired-callback"?: () => void;
          "error-callback"?: () => void;
        },
      ) => string;
      reset: (widgetId: string) => void;
    };
  }
}

/** Renders a Cloudflare Turnstile widget into the returned ref's element and tracks its current
 * token. Requires the Turnstile script tag in index.html (loaded async/defer, so we poll briefly
 * for it). Call `reset()` after every submit attempt — tokens are single-use.
 *
 * `containerRef` is a callback ref (state-backed), not a plain ref object — callers that render
 * the container div conditionally (e.g. only after a prior step succeeds) still get the widget
 * mounted correctly, since the effect re-runs when the div actually appears rather than only once
 * on the owning component's initial mount. */
export function useTurnstile() {
  const [container, setContainer] = useState<HTMLDivElement | null>(null);
  const widgetIdRef = useRef<string | null>(null);
  const [token, setToken] = useState<string | null>(null);

  const containerRef = useCallback((node: HTMLDivElement | null) => setContainer(node), []);

  useEffect(() => {
    const siteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY;
    if (!siteKey || !container) return;

    let cancelled = false;

    function render() {
      if (cancelled || !window.turnstile || !container) return;
      widgetIdRef.current = window.turnstile.render(container, {
        sitekey: siteKey,
        callback: (t) => setToken(t),
        "expired-callback": () => setToken(null),
        "error-callback": () => setToken(null),
      });
    }

    if (window.turnstile) {
      render();
      return;
    }

    const interval = setInterval(() => {
      if (window.turnstile) {
        clearInterval(interval);
        render();
      }
    }, 100);

    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, [container]);

  function reset() {
    setToken(null);
    if (window.turnstile && widgetIdRef.current) {
      window.turnstile.reset(widgetIdRef.current);
    }
  }

  return { containerRef, token, reset };
}

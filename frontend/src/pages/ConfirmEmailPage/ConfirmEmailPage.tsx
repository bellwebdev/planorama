import { useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { AuthLayout } from "../../components/AuthLayout/AuthLayout";
import { Button } from "../../components/Button/Button";
import { ErrorBanner } from "../../components/ErrorBanner/ErrorBanner";
import { TextField } from "../../components/TextField/TextField";
import { useAuth } from "../../lib/auth/AuthContext";
import { useTurnstile } from "../../lib/turnstile/useTurnstile";
import styles from "./ConfirmEmailPage.module.css";

type Status = "confirming" | "success" | "error";

export function ConfirmEmailPage() {
  const [searchParams] = useSearchParams();
  const { confirmEmail, resendConfirmation } = useAuth();
  const [status, setStatus] = useState<Status>("confirming");
  const [error, setError] = useState<unknown>(null);
  const [resendEmail, setResendEmail] = useState("");
  const [resent, setResent] = useState(false);
  const hasAttempted = useRef(false);
  const resendTurnstile = useTurnstile();

  useEffect(() => {
    if (hasAttempted.current) return;
    hasAttempted.current = true;

    const userId = searchParams.get("userId");
    const token = searchParams.get("token");
    if (!userId || !token) {
      setStatus("error");
      return;
    }
    confirmEmail(userId, token)
      .then(() => setStatus("success"))
      .catch((err: unknown) => {
        setError(err);
        setStatus("error");
      });
    // Confirms once, from the link's own params — not re-run on subsequent renders.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleResend() {
    if (!resendEmail.trim() || !resendTurnstile.token) return;
    setError(null);
    try {
      await resendConfirmation(resendEmail.trim(), resendTurnstile.token);
      setResent(true);
    } catch (err) {
      setError(err);
    } finally {
      resendTurnstile.reset();
    }
  }

  if (status === "confirming") {
    return (
      <AuthLayout title="Confirming your email">
        <p className={styles.hint}>Just a moment…</p>
      </AuthLayout>
    );
  }

  if (status === "success") {
    return (
      <AuthLayout title="Email confirmed" subtitle="Your account is ready.">
        <Button fullWidth onClick={() => window.location.assign("/login")}>
          Sign in
        </Button>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title="Link expired or invalid" subtitle="Enter your email for a new confirmation link.">
      <div className={styles.body}>
        <ErrorBanner error={error} />
        {resent ? (
          <p className={styles.hint}>Sent — check your inbox.</p>
        ) : (
          <>
            <TextField
              label="Email"
              type="email"
              value={resendEmail}
              onChange={(e) => setResendEmail(e.target.value)}
              autoComplete="email"
              required
            />
            <div ref={resendTurnstile.containerRef} />
            <Button fullWidth disabled={!resendTurnstile.token} onClick={() => void handleResend()}>
              Resend confirmation email
            </Button>
          </>
        )}
        <Link to="/login">Back to sign in</Link>
      </div>
    </AuthLayout>
  );
}

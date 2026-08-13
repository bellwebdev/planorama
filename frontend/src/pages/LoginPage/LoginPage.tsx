import { useCallback, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AuthLayout } from "../../components/AuthLayout/AuthLayout";
import { Button } from "../../components/Button/Button";
import { ErrorBanner } from "../../components/ErrorBanner/ErrorBanner";
import { TextField } from "../../components/TextField/TextField";
import { ApiError } from "../../lib/api/client";
import { useAuth } from "../../lib/auth/AuthContext";
import { useGoogleSignIn } from "../../lib/google/useGoogleSignIn";
import styles from "./LoginPage.module.css";

export function LoginPage() {
  const { login, loginWithGoogle, resendConfirmation } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const [resent, setResent] = useState(false);

  const handleGoogleCredential = useCallback(
    (idToken: string) => {
      setError(null);
      loginWithGoogle(idToken)
        .then(() => navigate("/profile", { replace: true }))
        .catch((err: unknown) => setError(err));
    },
    [loginWithGoogle, navigate],
  );
  const googleButtonRef = useGoogleSignIn(handleGoogleCredential);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setResent(false);
    setSubmitting(true);
    try {
      await login(email, password);
      navigate("/profile", { replace: true });
    } catch (err) {
      setError(err);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleResend() {
    if (!email.trim()) return;
    try {
      await resendConfirmation(email.trim());
      setResent(true);
    } catch (err) {
      setError(err);
    }
  }

  const isUnconfirmedEmail = error instanceof ApiError && error.title === "Email not confirmed";

  return (
    <AuthLayout title="Sign in" subtitle="Welcome back to Planorama.">
      <form className={styles.form} onSubmit={(event) => void handleSubmit(event)}>
        <ErrorBanner error={error} />
        {isUnconfirmedEmail &&
          (resent ? (
            <p className={styles.hint}>Confirmation email sent again.</p>
          ) : (
            <Button variant="secondary" fullWidth onClick={() => void handleResend()}>
              Resend confirmation email
            </Button>
          ))}
        <TextField
          label="Email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          autoComplete="email"
          required
        />
        <TextField
          label="Password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
          required
        />
        <Button type="submit" fullWidth disabled={submitting}>
          {submitting ? "Signing in…" : "Sign in"}
        </Button>
      </form>
      <div className={styles.divider}>
        <span>or</span>
      </div>
      <div ref={googleButtonRef} className={styles.googleButton} />
      <p className={styles.footer}>
        New here?{" "}
        <Link to="/register" className={styles.link}>
          Create an account
        </Link>
      </p>
    </AuthLayout>
  );
}

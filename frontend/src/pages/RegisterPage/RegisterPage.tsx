import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { AuthLayout } from "../../components/AuthLayout/AuthLayout";
import { Button } from "../../components/Button/Button";
import { ErrorBanner } from "../../components/ErrorBanner/ErrorBanner";
import { TextField } from "../../components/TextField/TextField";
import { ApiError } from "../../lib/api/client";
import { useAuth } from "../../lib/auth/AuthContext";
import { useTurnstile } from "../../lib/turnstile/useTurnstile";
import styles from "./RegisterPage.module.css";

// Mirrors backend/src/Planorama.Api/Validation/RegisterRequestValidator.cs for immediate
// feedback — the server response, not this, is the real source of truth.
const PASSWORD_RULES: { test: (value: string) => boolean; message: string }[] = [
  { test: (v) => v.length >= 10, message: "At least 10 characters." },
  { test: (v) => /[A-Z]/.test(v), message: "One uppercase letter." },
  { test: (v) => /[a-z]/.test(v), message: "One lowercase letter." },
  { test: (v) => /[0-9]/.test(v), message: "One digit." },
  { test: (v) => /[^a-zA-Z0-9]/.test(v), message: "One symbol." },
];

function passwordError(password: string): string | undefined {
  return PASSWORD_RULES.find((rule) => !rule.test(password))?.message;
}

function fieldErrorsFromApi(error: ApiError): Record<string, string> {
  const mapped: Record<string, string> = {};
  for (const [field, messages] of Object.entries(error.fieldErrors ?? {})) {
    mapped[field.charAt(0).toLowerCase() + field.slice(1)] = messages[0] ?? "Invalid value.";
  }
  return mapped;
}

export function RegisterPage() {
  const { register, resendConfirmation } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [apiError, setApiError] = useState<unknown>(null);
  const [submitting, setSubmitting] = useState(false);
  const [registeredEmail, setRegisteredEmail] = useState<string | null>(null);
  const [resent, setResent] = useState(false);
  const registerTurnstile = useTurnstile();
  const resendTurnstile = useTurnstile();

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setApiError(null);

    const errors: Record<string, string> = {};
    if (!displayName.trim()) errors.displayName = "Display name is required.";
    const pwError = passwordError(password);
    if (pwError) errors.password = pwError;
    if (password !== confirmPassword) errors.confirmPassword = "Passwords don't match.";
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    if (!registerTurnstile.token) {
      setApiError(new Error("Please complete the verification challenge."));
      return;
    }

    setSubmitting(true);
    try {
      await register(email, password, displayName.trim(), registerTurnstile.token);
      setRegisteredEmail(email);
    } catch (error) {
      if (error instanceof ApiError) {
        setFieldErrors(fieldErrorsFromApi(error));
      }
      setApiError(error);
    } finally {
      registerTurnstile.reset();
      setSubmitting(false);
    }
  }

  async function handleResend() {
    if (!registeredEmail || !resendTurnstile.token) return;
    setApiError(null);
    try {
      await resendConfirmation(registeredEmail, resendTurnstile.token);
      setResent(true);
    } catch (error) {
      setApiError(error);
    } finally {
      resendTurnstile.reset();
    }
  }

  if (registeredEmail) {
    return (
      <AuthLayout title="Check your email" subtitle={`We sent a confirmation link to ${registeredEmail}.`}>
        <ErrorBanner error={apiError} />
        {resent ? (
          <p className={styles.hint}>Sent again — check your inbox.</p>
        ) : (
          <>
            <div ref={resendTurnstile.containerRef} />
            <Button variant="secondary" fullWidth disabled={!resendTurnstile.token} onClick={() => void handleResend()}>
              Resend confirmation email
            </Button>
          </>
        )}
        <Link to="/login" className={styles.link}>
          Back to sign in
        </Link>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title="Create your account" subtitle="Plan family trips together.">
      <form className={styles.form} onSubmit={(event) => void handleSubmit(event)}>
        <ErrorBanner error={apiError} />
        <TextField
          label="Display name"
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          error={fieldErrors.displayName}
          autoComplete="name"
          required
        />
        <TextField
          label="Email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          error={fieldErrors.email}
          autoComplete="email"
          required
        />
        <TextField
          label="Password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          error={fieldErrors.password}
          autoComplete="new-password"
          required
        />
        <TextField
          label="Confirm password"
          type="password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          error={fieldErrors.confirmPassword}
          autoComplete="new-password"
          required
        />
        <div ref={registerTurnstile.containerRef} />
        <Button type="submit" fullWidth disabled={submitting || !registerTurnstile.token}>
          {submitting ? "Creating account…" : "Create account"}
        </Button>
      </form>
      <p className={styles.footer}>
        Already have an account?{" "}
        <Link to="/login" className={styles.link}>
          Sign in
        </Link>
      </p>
    </AuthLayout>
  );
}

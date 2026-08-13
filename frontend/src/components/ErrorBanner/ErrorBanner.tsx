import { ApiError } from "../../lib/api/client";
import styles from "./ErrorBanner.module.css";

interface ErrorBannerProps {
  error: unknown;
}

export function ErrorBanner({ error }: ErrorBannerProps) {
  if (!error) return null;

  if (error instanceof ApiError) {
    return (
      <div className={styles.banner} role="alert">
        <p className={styles.title}>{error.title}</p>
        {error.detail && <p className={styles.detail}>{error.detail}</p>}
      </div>
    );
  }

  const message = error instanceof Error ? error.message : "Something went wrong.";
  return (
    <div className={styles.banner} role="alert">
      <p className={styles.title}>{message}</p>
    </div>
  );
}

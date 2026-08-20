import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Card } from "../../components/Card/Card";
import { ErrorBanner } from "../../components/ErrorBanner/ErrorBanner";
import * as invitesApi from "../../lib/api/invites";
import styles from "./AcceptInvitePage.module.css";

export function AcceptInvitePage() {
  const { token } = useParams<{ token: string }>();
  const navigate = useNavigate();
  const [error, setError] = useState<unknown>(null);
  const attempted = useRef(false);

  useEffect(() => {
    if (!token || attempted.current) return;
    attempted.current = true;

    invitesApi
      .acceptInvite(token)
      .then((trip) => navigate(`/trips/${trip.id}`, { replace: true }))
      .catch((err: unknown) => setError(err));
  }, [token, navigate]);

  if (!error) {
    return <p className={styles.hint}>Joining trip…</p>;
  }

  return (
    <Card className={styles.card}>
      <ErrorBanner error={error} />
      <p>
        This invite may have expired. <Link to="/trips">Back to your trips</Link>
      </p>
    </Card>
  );
}

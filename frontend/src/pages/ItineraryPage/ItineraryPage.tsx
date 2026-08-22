import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Card } from "../../components/Card/Card";
import { ErrorBanner } from "../../components/ErrorBanner/ErrorBanner";
import { ItineraryPanel } from "../../components/ItineraryPanel/ItineraryPanel";
import * as tripsApi from "../../lib/api/trips";
import { useAuth } from "../../lib/auth/AuthContext";
import type { TripResponse } from "../../types/api";
import styles from "./ItineraryPage.module.css";

export function ItineraryPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();

  const [trip, setTrip] = useState<TripResponse | null>(null);
  const [loadError, setLoadError] = useState<unknown>(null);

  useEffect(() => {
    if (!id) return;
    tripsApi
      .getTrip(id)
      .then(setTrip)
      .catch((err: unknown) => setLoadError(err));
  }, [id]);

  if (loadError) {
    return <ErrorBanner error={loadError} />;
  }

  if (!trip) {
    return <p className={styles.hint}>Loading…</p>;
  }

  const isCreator = user?.id === trip.creatorId;

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <Link to={`/trips/${trip.id}`} className={styles.backLink}>
          ← {trip.name}
        </Link>
        <h1>Itinerary</h1>
      </div>

      <Card className={styles.card}>
        <ItineraryPanel tripId={trip.id} isCreator={isCreator} />
      </Card>
    </div>
  );
}

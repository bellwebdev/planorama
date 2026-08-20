import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Button } from "../../components/Button/Button";
import { Card } from "../../components/Card/Card";
import { ErrorBanner } from "../../components/ErrorBanner/ErrorBanner";
import * as tripsApi from "../../lib/api/trips";
import type { TripResponse } from "../../types/api";
import styles from "./TripsListPage.module.css";

function formatDateRange(startDate: string, endDate: string): string {
  const start = new Date(`${startDate}T00:00:00`);
  const end = new Date(`${endDate}T00:00:00`);
  const format: Intl.DateTimeFormatOptions = { month: "short", day: "numeric" };
  return startDate === endDate
    ? start.toLocaleDateString(undefined, { ...format, year: "numeric" })
    : `${start.toLocaleDateString(undefined, format)} – ${end.toLocaleDateString(undefined, { ...format, year: "numeric" })}`;
}

export function TripsListPage() {
  const navigate = useNavigate();
  const [trips, setTrips] = useState<TripResponse[] | null>(null);
  const [error, setError] = useState<unknown>(null);

  useEffect(() => {
    tripsApi
      .listTrips()
      .then(setTrips)
      .catch((err: unknown) => setError(err));
  }, []);

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1>Your trips</h1>
        <Button onClick={() => navigate("/trips/new")}>New trip</Button>
      </div>

      <ErrorBanner error={error} />

      {trips === null && !error && <p className={styles.hint}>Loading…</p>}

      {trips !== null && trips.length === 0 && (
        <Card className={styles.empty}>
          <p>No trips yet.</p>
          <Button onClick={() => navigate("/trips/new")}>Plan your first trip</Button>
        </Card>
      )}

      {trips !== null && trips.length > 0 && (
        <div className={styles.list}>
          {trips.map((trip) => (
            <Link key={trip.id} to={`/trips/${trip.id}`} className={styles.tripCardLink}>
              <Card className={styles.tripCard}>
                <div className={styles.tripCardHeader}>
                  <h2 className={styles.tripName}>{trip.name}</h2>
                  <span className={styles.status}>{trip.status}</span>
                </div>
                <p className={styles.tripMeta}>{trip.locationName}</p>
                <p className={styles.tripMeta}>{formatDateRange(trip.startDate, trip.endDate)}</p>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

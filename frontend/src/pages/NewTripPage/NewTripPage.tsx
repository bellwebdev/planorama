import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "../../components/Button/Button";
import { Card } from "../../components/Card/Card";
import { ErrorBanner } from "../../components/ErrorBanner/ErrorBanner";
import { TripForm } from "../../components/TripForm/TripForm";
import * as tripsApi from "../../lib/api/trips";
import { guessLocalTimezone } from "../../lib/trips/timezones";
import { validateTripForm, type TripFormValues } from "../../lib/trips/validateTripForm";
import styles from "./NewTripPage.module.css";

const INITIAL_VALUES: TripFormValues = {
  name: "",
  description: "",
  locationName: "",
  stayAddress: "",
  startDate: "",
  endDate: "",
  timezone: guessLocalTimezone(),
  defaultVotingWindowHours: 48,
};

export function NewTripPage() {
  const navigate = useNavigate();
  const [values, setValues] = useState<TripFormValues>(INITIAL_VALUES);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState<unknown>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit() {
    const errors = validateTripForm(values);
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setError(null);
    setSubmitting(true);
    try {
      const trip = await tripsApi.createTrip({
        name: values.name.trim(),
        description: values.description.trim() || null,
        locationName: values.locationName.trim(),
        stayAddress: values.stayAddress.trim(),
        startDate: values.startDate,
        endDate: values.endDate,
        timezone: values.timezone,
        defaultVotingWindowHours: values.defaultVotingWindowHours,
      });
      navigate(`/trips/${trip.id}`, { replace: true });
    } catch (err) {
      setError(err);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className={styles.page}>
      <h1>Plan a new trip</h1>
      <Card className={styles.card}>
        <ErrorBanner error={error} />
        <TripForm values={values} onChange={setValues} errors={fieldErrors} disabled={submitting} />
        <div className={styles.actions}>
          <Button onClick={() => void handleSubmit()} disabled={submitting}>
            {submitting ? "Creating…" : "Create trip"}
          </Button>
          <Button variant="secondary" onClick={() => navigate(-1)} disabled={submitting}>
            Cancel
          </Button>
        </div>
      </Card>
    </div>
  );
}

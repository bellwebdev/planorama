import { TextArea } from "../TextArea/TextArea";
import { TextField } from "../TextField/TextField";
import { listIanaTimezones } from "../../lib/trips/timezones";
import type { TripFormValues } from "../../lib/trips/validateTripForm";
import type { TripStatus } from "../../types/api";
import styles from "./TripForm.module.css";

const TIMEZONES = listIanaTimezones();
const STATUSES: TripStatus[] = ["Draft", "Planning", "Active", "Completed"];

interface TripFormProps {
  values: TripFormValues;
  onChange: (values: TripFormValues) => void;
  errors: Record<string, string>;
  disabled?: boolean;
  status?: TripStatus;
  onStatusChange?: (status: TripStatus) => void;
}

export function TripForm({ values, onChange, errors, disabled, status, onStatusChange }: TripFormProps) {
  function set<K extends keyof TripFormValues>(key: K, value: TripFormValues[K]) {
    onChange({ ...values, [key]: value });
  }

  return (
    <div className={styles.grid}>
      <TextField
        label="Trip name"
        value={values.name}
        onChange={(e) => set("name", e.target.value)}
        error={errors.name}
        disabled={disabled}
      />
      <TextArea
        label="Description"
        value={values.description}
        onChange={(e) => set("description", e.target.value)}
        error={errors.description}
        disabled={disabled}
        rows={3}
      />
      <TextField
        label="Location"
        placeholder="Lake Tahoe, CA"
        value={values.locationName}
        onChange={(e) => set("locationName", e.target.value)}
        error={errors.locationName}
        disabled={disabled}
      />
      <TextField
        label="Stay address"
        value={values.stayAddress}
        onChange={(e) => set("stayAddress", e.target.value)}
        error={errors.stayAddress}
        disabled={disabled}
      />
      <div className={styles.row}>
        <TextField
          label="Start date"
          type="date"
          value={values.startDate}
          onChange={(e) => set("startDate", e.target.value)}
          error={errors.startDate}
          disabled={disabled}
        />
        <TextField
          label="End date"
          type="date"
          value={values.endDate}
          onChange={(e) => set("endDate", e.target.value)}
          error={errors.endDate}
          disabled={disabled}
        />
      </div>
      <div className={styles.row}>
        <div className={styles.field}>
          <label htmlFor="trip-timezone" className={styles.label}>
            Timezone
          </label>
          <select
            id="trip-timezone"
            className={styles.select}
            value={values.timezone}
            onChange={(e) => set("timezone", e.target.value)}
            disabled={disabled}
          >
            <option value="" disabled>
              Select a timezone…
            </option>
            {TIMEZONES.map((tz) => (
              <option key={tz} value={tz}>
                {tz}
              </option>
            ))}
          </select>
          {errors.timezone && <p className={styles.error}>{errors.timezone}</p>}
        </div>
        <TextField
          label="Voting window (hours)"
          type="number"
          min={1}
          value={values.defaultVotingWindowHours}
          onChange={(e) => set("defaultVotingWindowHours", Number(e.target.value))}
          error={errors.defaultVotingWindowHours}
          disabled={disabled}
        />
      </div>
      {status && onStatusChange && (
        <div className={styles.field}>
          <label htmlFor="trip-status" className={styles.label}>
            Status
          </label>
          <select
            id="trip-status"
            className={styles.select}
            value={status}
            onChange={(e) => onStatusChange(e.target.value as TripStatus)}
            disabled={disabled}
          >
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>
      )}
    </div>
  );
}

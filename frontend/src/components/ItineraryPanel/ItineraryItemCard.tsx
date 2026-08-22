import { useState } from "react";
import * as itineraryApi from "../../lib/api/itinerary";
import type { ItineraryItemResponse } from "../../types/api";
import { Button } from "../Button/Button";
import { ErrorBanner } from "../ErrorBanner/ErrorBanner";
import { TextField } from "../TextField/TextField";
import styles from "./ItineraryPanel.module.css";

interface ItineraryItemCardProps {
  item: ItineraryItemResponse;
  isCreator: boolean;
  onChange: (updated: ItineraryItemResponse) => void;
}

interface EditValues {
  date: string;
  startTime: string;
  endTime: string;
  sortOrder: string;
  timezone: string;
}

function toEditValues(item: ItineraryItemResponse): EditValues {
  return {
    date: item.date ?? "",
    startTime: item.startTime ?? "",
    endTime: item.endTime ?? "",
    sortOrder: String(item.sortOrder),
    timezone: item.timezone ?? "",
  };
}

export function ItineraryItemCard({ item, isCreator, onChange }: ItineraryItemCardProps) {
  const [values, setValues] = useState<EditValues>(() => toEditValues(item));
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<unknown>(null);

  const dirty =
    values.date !== (item.date ?? "") ||
    values.startTime !== (item.startTime ?? "") ||
    values.endTime !== (item.endTime ?? "") ||
    values.sortOrder !== String(item.sortOrder) ||
    values.timezone !== (item.timezone ?? "");

  async function handleSave() {
    setSaving(true);
    setSaveError(null);
    try {
      const updated = await itineraryApi.updateItineraryItem(item.id, {
        date: values.date || null,
        startTime: values.startTime || null,
        endTime: values.endTime || null,
        sortOrder: Number(values.sortOrder) || 0,
        timezone: values.timezone.trim() || null,
      });
      onChange(updated);
      setValues(toEditValues(updated));
    } catch (err) {
      setSaveError(err);
    } finally {
      setSaving(false);
    }
  }

  return (
    <li className={styles.card}>
      <div className={styles.cardHeader}>
        <p className={styles.cardTitle}>{item.title ?? "Untitled item"}</p>
        {item.startTime && (
          <span className={styles.cardTime}>
            {item.startTime}
            {item.endTime && ` – ${item.endTime}`}
          </span>
        )}
      </div>

      {item.address && <p className={styles.cardAddress}>{item.address}</p>}
      {item.description && <p className={styles.cardDescription}>{item.description}</p>}

      {isCreator && (
        <>
          <div className={styles.editRow}>
            <TextField
              label="Date"
              type="date"
              value={values.date}
              onChange={(e) => setValues({ ...values, date: e.target.value })}
              disabled={saving}
            />
            <TextField
              label="Start time"
              type="time"
              value={values.startTime}
              onChange={(e) => setValues({ ...values, startTime: e.target.value })}
              disabled={saving || !values.date}
            />
            <TextField
              label="End time"
              type="time"
              value={values.endTime}
              onChange={(e) => setValues({ ...values, endTime: e.target.value })}
              disabled={saving || !values.date}
            />
            <TextField
              label="Order"
              type="number"
              value={values.sortOrder}
              onChange={(e) => setValues({ ...values, sortOrder: e.target.value })}
              disabled={saving}
            />
            <TextField
              label="Timezone (optional)"
              placeholder="Trip default"
              value={values.timezone}
              onChange={(e) => setValues({ ...values, timezone: e.target.value })}
              disabled={saving}
            />
          </div>
          <ErrorBanner error={saveError} />
          <Button onClick={() => void handleSave()} disabled={saving || !dirty}>
            {saving ? "Saving…" : "Save"}
          </Button>
        </>
      )}
    </li>
  );
}

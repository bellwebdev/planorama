import { useEffect, useState } from "react";
import { Card } from "../../components/Card/Card";
import { ErrorBanner } from "../../components/ErrorBanner/ErrorBanner";
import { Toggle } from "../../components/Toggle/Toggle";
import * as settingsApi from "../../lib/api/settings";
import type { ReminderOffset, SettingsResponse } from "../../types/api";
import styles from "./SettingsPage.module.css";

const REMINDER_LABELS: Record<ReminderOffset, string> = {
  OneHour: "1 hour before",
  TwelveHours: "12 hours before",
  TwentyFourHours: "24 hours before",
};

export function SettingsPage() {
  const [settings, setSettings] = useState<SettingsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<unknown>(null);

  useEffect(() => {
    settingsApi
      .getSettings()
      .then(setSettings)
      .finally(() => setLoading(false));
  }, []);

  async function save(next: SettingsResponse) {
    setSettings(next);
    setError(null);
    setSaving(true);
    try {
      const updated = await settingsApi.updateSettings(next.reminderOffset, next.notifyEmail, next.notifyPush);
      setSettings(updated);
    } catch (err) {
      setError(err);
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return <p className={styles.hint}>Loading…</p>;
  }

  if (!settings) {
    return <ErrorBanner error={new Error("Couldn't load your settings.")} />;
  }

  return (
    <div className={styles.page}>
      <h1>Settings</h1>
      <Card className={styles.card}>
        <ErrorBanner error={error} />
        <div className={styles.field}>
          <label htmlFor="reminder-offset" className={styles.label}>
            Reminder timing
          </label>
          <select
            id="reminder-offset"
            className={styles.select}
            value={settings.reminderOffset}
            disabled={saving}
            onChange={(e) => void save({ ...settings, reminderOffset: e.target.value as ReminderOffset })}
          >
            {Object.entries(REMINDER_LABELS).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>

        <Toggle
          label="Email notifications"
          checked={settings.notifyEmail}
          disabled={saving}
          onChange={(e) => void save({ ...settings, notifyEmail: e.target.checked })}
        />
        <Toggle
          label="Push notifications"
          checked={settings.notifyPush}
          disabled={saving}
          onChange={(e) => void save({ ...settings, notifyPush: e.target.checked })}
        />
      </Card>
    </div>
  );
}

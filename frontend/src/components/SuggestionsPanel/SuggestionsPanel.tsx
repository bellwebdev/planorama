import { useEffect, useState } from "react";
import * as suggestionsApi from "../../lib/api/suggestions";
import type { SuggestionResponse } from "../../types/api";
import { Button } from "../Button/Button";
import { ErrorBanner } from "../ErrorBanner/ErrorBanner";
import { TextArea } from "../TextArea/TextArea";
import { TextField } from "../TextField/TextField";
import { SuggestionCard } from "./SuggestionCard";
import styles from "./SuggestionsPanel.module.css";

interface CustomSuggestionForm {
  title: string;
  address: string;
  description: string;
  proposedDate: string;
  proposedStartTime: string;
  durationMinutes: string;
}

const EMPTY_FORM: CustomSuggestionForm = {
  title: "",
  address: "",
  description: "",
  proposedDate: "",
  proposedStartTime: "",
  durationMinutes: "",
};

interface SuggestionsPanelProps {
  tripId: string;
  /** Bumped by a sibling panel (e.g. "suggest this place" from search) to trigger a refetch. */
  refreshKey?: number;
  isCreator?: boolean;
}

export function SuggestionsPanel({ tripId, refreshKey, isCreator = false }: SuggestionsPanelProps) {
  const [suggestions, setSuggestions] = useState<SuggestionResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<unknown>(null);

  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CustomSuggestionForm>(EMPTY_FORM);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<unknown>(null);

  useEffect(() => {
    setLoading(true);
    suggestionsApi
      .listSuggestions(tripId)
      .then(setSuggestions)
      .catch((err: unknown) => setLoadError(err))
      .finally(() => setLoading(false));
  }, [tripId, refreshKey]);

  function updateSuggestion(updated: SuggestionResponse) {
    setSuggestions((current) => current.map((s) => (s.id === updated.id ? updated : s)));
  }

  async function handleCreate() {
    if (!form.title.trim()) return;

    setCreating(true);
    setCreateError(null);
    try {
      const created = await suggestionsApi.createSuggestion(tripId, {
        title: form.title.trim(),
        address: form.address.trim() || null,
        description: form.description.trim() || null,
        proposedDate: form.proposedDate || null,
        proposedStartTime: form.proposedStartTime || null,
        durationMinutes: form.durationMinutes ? Number(form.durationMinutes) : null,
      });
      setSuggestions((current) => [created, ...current]);
      setForm(EMPTY_FORM);
      setShowForm(false);
    } catch (err) {
      setCreateError(err);
    } finally {
      setCreating(false);
    }
  }

  return (
    <div className={styles.panel}>
      <div className={styles.toolbar}>
        <Button variant="secondary" onClick={() => setShowForm((v) => !v)}>
          {showForm ? "Cancel" : "Add a custom suggestion"}
        </Button>
      </div>

      {showForm && (
        <div className={styles.form}>
          <ErrorBanner error={createError} />
          <TextField
            label="Title"
            value={form.title}
            onChange={(e) => setForm({ ...form, title: e.target.value })}
            disabled={creating}
          />
          <TextField
            label="Address (optional)"
            value={form.address}
            onChange={(e) => setForm({ ...form, address: e.target.value })}
            disabled={creating}
          />
          <TextArea
            label="Notes (optional)"
            value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
            disabled={creating}
            rows={2}
          />
          <div className={styles.formRow}>
            <TextField
              label="Proposed date (optional)"
              type="date"
              value={form.proposedDate}
              onChange={(e) => setForm({ ...form, proposedDate: e.target.value })}
              disabled={creating}
            />
            <TextField
              label="Start time (optional)"
              type="time"
              value={form.proposedStartTime}
              onChange={(e) => setForm({ ...form, proposedStartTime: e.target.value })}
              disabled={creating || !form.proposedDate}
            />
            <TextField
              label="Duration, minutes (optional)"
              type="number"
              min={1}
              max={1440}
              value={form.durationMinutes}
              onChange={(e) => setForm({ ...form, durationMinutes: e.target.value })}
              disabled={creating}
            />
          </div>
          <Button onClick={() => void handleCreate()} disabled={creating || !form.title.trim()}>
            {creating ? "Adding…" : "Add suggestion"}
          </Button>
        </div>
      )}

      <ErrorBanner error={loadError} />

      {loading && <p className={styles.hint}>Loading suggestions…</p>}

      {!loading && !loadError && suggestions.length === 0 && (
        <p className={styles.hint}>No suggestions yet. Search for a place above or add a custom one.</p>
      )}

      <ul className={styles.list}>
        {suggestions.map((s) => (
          <SuggestionCard key={s.id} suggestion={s} onChange={updateSuggestion} isCreator={isCreator} />
        ))}
      </ul>
    </div>
  );
}

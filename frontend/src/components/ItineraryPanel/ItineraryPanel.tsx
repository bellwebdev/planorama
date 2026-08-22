import { useEffect, useMemo, useState } from "react";
import * as itineraryApi from "../../lib/api/itinerary";
import type { ItineraryItemResponse } from "../../types/api";
import { ErrorBanner } from "../ErrorBanner/ErrorBanner";
import { ItineraryItemCard } from "./ItineraryItemCard";
import styles from "./ItineraryPanel.module.css";

interface ItineraryPanelProps {
  tripId: string;
  isCreator: boolean;
}

interface ItineraryGroup {
  date: string | null;
  items: ItineraryItemResponse[];
}

function groupByDate(items: ItineraryItemResponse[]): ItineraryGroup[] {
  const byDate = new Map<string | null, ItineraryItemResponse[]>();
  for (const item of items) {
    const list = byDate.get(item.date) ?? [];
    list.push(item);
    byDate.set(item.date, list);
  }
  for (const list of byDate.values()) {
    list.sort((a, b) => a.sortOrder - b.sortOrder || (a.startTime ?? "").localeCompare(b.startTime ?? ""));
  }

  const groups: ItineraryGroup[] = [];
  if (byDate.has(null)) groups.push({ date: null, items: byDate.get(null)! });
  for (const date of [...byDate.keys()].filter((d): d is string => d !== null).sort()) {
    groups.push({ date, items: byDate.get(date)! });
  }
  return groups;
}

export function ItineraryPanel({ tripId, isCreator }: ItineraryPanelProps) {
  const [items, setItems] = useState<ItineraryItemResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<unknown>(null);

  useEffect(() => {
    setLoading(true);
    itineraryApi
      .listItinerary(tripId)
      .then(setItems)
      .catch((err: unknown) => setLoadError(err))
      .finally(() => setLoading(false));
  }, [tripId]);

  function updateItem(updated: ItineraryItemResponse) {
    setItems((current) => current.map((i) => (i.id === updated.id ? updated : i)));
  }

  const groups = useMemo(() => groupByDate(items), [items]);

  if (loading) return <p className={styles.hint}>Loading itinerary…</p>;
  if (loadError) return <ErrorBanner error={loadError} />;
  if (items.length === 0) {
    return <p className={styles.hint}>Nothing on the itinerary yet — approved suggestions will show up here.</p>;
  }

  return (
    <div className={styles.panel}>
      {groups.map((group) => (
        <section key={group.date ?? "unscheduled"} className={styles.group}>
          <h3 className={styles.groupTitle}>{group.date ?? "Unscheduled"}</h3>
          <ul className={styles.list}>
            {group.items.map((item) => (
              <ItineraryItemCard key={item.id} item={item} isCreator={isCreator} onChange={updateItem} />
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}

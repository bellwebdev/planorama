import { useEffect, useState } from "react";
import * as placesApi from "../../lib/api/places";
import { ApiError } from "../../lib/api/client";
import { formatDistance, formatDuration } from "../../lib/places/formatDistance";
import { useDebouncedValue } from "../../lib/places/useDebouncedValue";
import type { PlaceCategory, PlaceCategoryResponse, PlaceDetailResponse, PlaceResponse, RouteResponse, TravelMode } from "../../types/api";
import { Button } from "../Button/Button";
import { ErrorBanner } from "../ErrorBanner/ErrorBanner";
import { TextField } from "../TextField/TextField";
import styles from "./PlaceSearchPanel.module.css";

const RADIUS_OPTIONS = [
  { label: "1 mi", meters: 1_609 },
  { label: "5 mi", meters: 8_047 },
  { label: "10 mi", meters: 16_093 },
  { label: "25 mi", meters: 40_234 },
];

const TRAVEL_MODES: { label: string; value: TravelMode }[] = [
  { label: "Driving", value: "Drive" },
  { label: "Walking", value: "Walk" },
  { label: "Cycling", value: "Bicycle" },
  { label: "Transit", value: "Transit" },
];

interface PlaceSearchPanelProps {
  tripId: string;
  /** The trip's stay address hasn't been resolved to a coordinate yet — search can't run. */
  stayNotGeocoded: boolean;
}

export function PlaceSearchPanel({ tripId, stayNotGeocoded }: PlaceSearchPanelProps) {
  const [categories, setCategories] = useState<PlaceCategoryResponse[]>([]);
  const [category, setCategory] = useState<PlaceCategory | null>(null);
  const [radius, setRadius] = useState(RADIUS_OPTIONS[1].meters);
  const [query, setQuery] = useState("");
  const debouncedQuery = useDebouncedValue(query, 400);

  const [results, setResults] = useState<PlaceResponse[]>([]);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState<unknown>(null);

  const [selected, setSelected] = useState<PlaceResponse | null>(null);

  useEffect(() => {
    placesApi
      .listPlaceCategories()
      .then((all) => {
        setCategories(all);
        setCategory((current) => current ?? all[0]?.value ?? null);
      })
      .catch(() => {
        // The chip bar just stays empty; search is unusable without a category anyway,
        // and the page around this panel already has its own load-error handling.
      });
  }, []);

  useEffect(() => {
    if (!category || stayNotGeocoded) {
      setResults([]);
      return;
    }

    const controller = new AbortController();
    setSearching(true);
    setSearchError(null);

    placesApi
      .searchPlacesNearStay(tripId, { category, radius, q: debouncedQuery.trim() || undefined }, controller.signal)
      .then((found) => {
        setResults(found);
        setSelected(null);
      })
      .catch((err: unknown) => {
        if (err instanceof DOMException && err.name === "AbortError") return;
        setSearchError(err);
      })
      .finally(() => setSearching(false));

    return () => controller.abort();
  }, [tripId, category, radius, debouncedQuery, stayNotGeocoded]);

  if (stayNotGeocoded) {
    return (
      <p className={styles.hint}>
        We couldn't pin this trip's stay address on the map, so place search isn't available yet. Edit the stay
        address into something more specific and it'll resolve automatically.
      </p>
    );
  }

  return (
    <div className={styles.panel}>
      <div className={styles.chips}>
        {categories.map((c) => (
          <button
            key={c.value}
            type="button"
            className={`${styles.chip} ${category === c.value ? styles.chipActive : ""}`}
            onClick={() => setCategory(c.value)}
          >
            {c.label}
          </button>
        ))}
      </div>

      <div className={styles.controls}>
        <TextField
          label="Search by name"
          placeholder="Optional — narrows the category above"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
        <div className={styles.radiusGroup}>
          <span className={styles.radiusLabel}>Within</span>
          {RADIUS_OPTIONS.map((option) => (
            <button
              key={option.meters}
              type="button"
              className={`${styles.chip} ${radius === option.meters ? styles.chipActive : ""}`}
              onClick={() => setRadius(option.meters)}
            >
              {option.label}
            </button>
          ))}
        </div>
      </div>

      <ErrorBanner error={searchError} />

      {searching && <p className={styles.hint}>Searching…</p>}

      {!searching && !searchError && results.length === 0 && (
        <p className={styles.hint}>No places found nearby. Try a wider radius.</p>
      )}

      <ul className={styles.results}>
        {results.map((place) => (
          <li key={place.providerPlaceId}>
            <button
              type="button"
              className={`${styles.result} ${selected?.providerPlaceId === place.providerPlaceId ? styles.resultActive : ""}`}
              onClick={() => setSelected(place)}
            >
              <span className={styles.resultName}>{place.name}</span>
              {place.address && <span className={styles.resultAddress}>{place.address}</span>}
              {place.distanceMeters !== null && (
                <span className={styles.resultDistance}>{formatDistance(place.distanceMeters)} from stay</span>
              )}
            </button>
          </li>
        ))}
      </ul>

      {selected && <PlaceDetailPanel tripId={tripId} place={selected} onClose={() => setSelected(null)} />}
    </div>
  );
}

interface PlaceDetailPanelProps {
  tripId: string;
  place: PlaceResponse;
  onClose: () => void;
}

function PlaceDetailPanel({ tripId, place, onClose }: PlaceDetailPanelProps) {
  const [detail, setDetail] = useState<PlaceDetailResponse | null>(null);
  const [detailError, setDetailError] = useState<unknown>(null);

  const [mode, setMode] = useState<TravelMode>("Drive");
  const [route, setRoute] = useState<RouteResponse | null>(null);
  const [routeError, setRouteError] = useState<unknown>(null);
  const [routing, setRouting] = useState(false);

  useEffect(() => {
    setDetail(null);
    setDetailError(null);
    placesApi
      .getPlaceDetail(place.providerPlaceId)
      .then(setDetail)
      .catch((err: unknown) => setDetailError(err));
  }, [place.providerPlaceId]);

  useEffect(() => {
    setRoute(null);
    setRouteError(null);
    setRouting(true);
    placesApi
      .getRouteFromStay(tripId, place.lat, place.lng, mode)
      .then(setRoute)
      .catch((err: unknown) => setRouteError(err))
      .finally(() => setRouting(false));
  }, [tripId, place.lat, place.lng, mode]);

  return (
    <div className={styles.detail}>
      <div className={styles.detailHeader}>
        <h3>{place.name}</h3>
        <Button variant="tertiary" onClick={onClose}>
          Close
        </Button>
      </div>

      <ErrorBanner error={detailError} />
      {detail?.description && <p className={styles.detailDescription}>{detail.description}</p>}
      {detail?.website && (
        <a className={styles.detailLink} href={detail.website} target="_blank" rel="noreferrer">
          {detail.website}
        </a>
      )}

      <div className={styles.modeGroup}>
        {TRAVEL_MODES.map((option) => (
          <button
            key={option.value}
            type="button"
            className={`${styles.chip} ${mode === option.value ? styles.chipActive : ""}`}
            onClick={() => setMode(option.value)}
          >
            {option.label}
          </button>
        ))}
      </div>

      {routing && <p className={styles.hint}>Calculating route…</p>}
      {!routing && route && (
        <p className={styles.routeSummary}>
          {formatDistance(route.distanceMeters)} · {formatDuration(route.durationSeconds)} from your stay
        </p>
      )}
      {!routing && Boolean(routeError) && !(routeError instanceof ApiError && routeError.status === 404) && (
        <ErrorBanner error={routeError} />
      )}
      {!routing && routeError instanceof ApiError && routeError.status === 404 && (
        <p className={styles.hint}>No {mode.toLowerCase()} route found between your stay and this place.</p>
      )}
    </div>
  );
}

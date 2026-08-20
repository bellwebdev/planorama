import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Button } from "../../components/Button/Button";
import { Card } from "../../components/Card/Card";
import { ErrorBanner } from "../../components/ErrorBanner/ErrorBanner";
import { PlaceSearchPanel } from "../../components/PlaceSearchPanel/PlaceSearchPanel";
import { TextField } from "../../components/TextField/TextField";
import { TripForm } from "../../components/TripForm/TripForm";
import * as tripsApi from "../../lib/api/trips";
import { useAuth } from "../../lib/auth/AuthContext";
import { validateTripForm, type TripFormValues } from "../../lib/trips/validateTripForm";
import type { InvitedVia, InviteResponse, TripResponse, TripStatus } from "../../types/api";
import styles from "./TripDetailPage.module.css";

function toFormValues(trip: TripResponse): TripFormValues {
  return {
    name: trip.name,
    description: trip.description ?? "",
    locationName: trip.locationName,
    stayAddress: trip.stayAddress,
    startDate: trip.startDate,
    endDate: trip.endDate,
    timezone: trip.timezone,
    defaultVotingWindowHours: trip.defaultVotingWindowHours,
  };
}

export function TripDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();

  const [trip, setTrip] = useState<TripResponse | null>(null);
  const [loadError, setLoadError] = useState<unknown>(null);

  const [editing, setEditing] = useState(false);
  const [values, setValues] = useState<TripFormValues | null>(null);
  const [status, setStatus] = useState<TripStatus>("Draft");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [saveError, setSaveError] = useState<unknown>(null);
  const [saving, setSaving] = useState(false);

  const [inviteEmail, setInviteEmail] = useState("");
  const [invite, setInvite] = useState<InviteResponse | null>(null);
  const [inviteError, setInviteError] = useState<unknown>(null);
  const [invitingVia, setInvitingVia] = useState<InvitedVia | null>(null);

  useEffect(() => {
    if (!id) return;
    tripsApi
      .getTrip(id)
      .then(setTrip)
      .catch((err: unknown) => setLoadError(err));
  }, [id]);

  function startEditing() {
    if (!trip) return;
    setValues(toFormValues(trip));
    setStatus(trip.status);
    setFieldErrors({});
    setSaveError(null);
    setEditing(true);
  }

  async function handleSave() {
    if (!trip || !values) return;
    const errors = validateTripForm(values);
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setSaveError(null);
    setSaving(true);
    try {
      const updated = await tripsApi.updateTrip(trip.id, {
        name: values.name.trim(),
        description: values.description.trim() || null,
        locationName: values.locationName.trim(),
        stayAddress: values.stayAddress.trim(),
        startDate: values.startDate,
        endDate: values.endDate,
        timezone: values.timezone,
        defaultVotingWindowHours: values.defaultVotingWindowHours,
        status,
      });
      setTrip(updated);
      setEditing(false);
    } catch (err) {
      setSaveError(err);
    } finally {
      setSaving(false);
    }
  }

  async function handleCreateLinkInvite() {
    if (!trip) return;
    setInviteError(null);
    setInvitingVia("Link");
    try {
      setInvite(await tripsApi.createInvite(trip.id, { via: "Link" }));
    } catch (err) {
      setInviteError(err);
    } finally {
      setInvitingVia(null);
    }
  }

  async function handleCreateEmailInvite() {
    if (!trip || !inviteEmail.trim()) return;
    setInviteError(null);
    setInvitingVia("Email");
    try {
      setInvite(await tripsApi.createInvite(trip.id, { via: "Email", contact: inviteEmail.trim() }));
      setInviteEmail("");
    } catch (err) {
      setInviteError(err);
    } finally {
      setInvitingVia(null);
    }
  }

  if (loadError) {
    return <ErrorBanner error={loadError} />;
  }

  if (!trip) {
    return <p className={styles.hint}>Loading…</p>;
  }

  const isCreator = user?.id === trip.creatorId;
  const inviteLink = invite?.invitedVia === "Link" ? `${window.location.origin}/invites/${invite.token}` : null;

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1>{trip.name}</h1>
        <span className={styles.status}>{trip.status}</span>
      </div>

      <Card className={styles.card}>
        {editing && values ? (
          <>
            <ErrorBanner error={saveError} />
            <TripForm
              values={values}
              onChange={setValues}
              errors={fieldErrors}
              disabled={saving}
              status={status}
              onStatusChange={setStatus}
            />
            <div className={styles.actions}>
              <Button onClick={() => void handleSave()} disabled={saving}>
                {saving ? "Saving…" : "Save changes"}
              </Button>
              <Button variant="secondary" onClick={() => setEditing(false)} disabled={saving}>
                Cancel
              </Button>
            </div>
          </>
        ) : (
          <>
            <dl className={styles.meta}>
              <div>
                <dt>Location</dt>
                <dd>{trip.locationName}</dd>
              </div>
              <div>
                <dt>Stay address</dt>
                <dd>{trip.stayAddress}</dd>
              </div>
              <div>
                <dt>Dates</dt>
                <dd>
                  {trip.startDate} – {trip.endDate}
                </dd>
              </div>
              <div>
                <dt>Timezone</dt>
                <dd>{trip.timezone}</dd>
              </div>
              {trip.description && (
                <div>
                  <dt>Description</dt>
                  <dd>{trip.description}</dd>
                </div>
              )}
            </dl>
            {isCreator && <Button onClick={startEditing}>Edit trip</Button>}
          </>
        )}
      </Card>

      <Card className={styles.card}>
        <h2 className={styles.sectionTitle}>Find places nearby</h2>
        <PlaceSearchPanel tripId={trip.id} stayNotGeocoded={trip.stayLat === null || trip.stayLng === null} />
      </Card>

      {isCreator && (
        <Card className={styles.card}>
          <h2 className={styles.sectionTitle}>Invite family members</h2>
          <ErrorBanner error={inviteError} />

          <div className={styles.inviteRow}>
            <Button variant="secondary" onClick={() => void handleCreateLinkInvite()} disabled={invitingVia !== null}>
              {invitingVia === "Link" ? "Generating…" : "Get shareable link"}
            </Button>
          </div>

          <div className={styles.inviteRow}>
            <TextField
              label="Invite by email"
              type="email"
              value={inviteEmail}
              onChange={(e) => setInviteEmail(e.target.value)}
              placeholder="family@example.com"
              disabled={invitingVia !== null}
            />
            <Button
              variant="secondary"
              onClick={() => void handleCreateEmailInvite()}
              disabled={invitingVia !== null || !inviteEmail.trim()}
            >
              {invitingVia === "Email" ? "Sending…" : "Send invite"}
            </Button>
          </div>

          {invite && (
            <div className={styles.inviteResult}>
              {inviteLink ? (
                <p>
                  Share this link: <span className={styles.inviteLink}>{inviteLink}</span>
                </p>
              ) : (
                <p>Invite sent to {invite.contact}.</p>
              )}
            </div>
          )}
        </Card>
      )}
    </div>
  );
}

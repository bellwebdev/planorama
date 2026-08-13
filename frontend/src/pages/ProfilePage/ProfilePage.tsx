import { useEffect, useRef, useState, type ChangeEvent } from "react";
import { Avatar } from "../../components/Avatar/Avatar";
import { Button } from "../../components/Button/Button";
import { Card } from "../../components/Card/Card";
import { ErrorBanner } from "../../components/ErrorBanner/ErrorBanner";
import { TextField } from "../../components/TextField/TextField";
import * as meApi from "../../lib/api/me";
import { updateUser } from "../../lib/auth/tokenStore";
import type { MeResponse } from "../../types/api";
import styles from "./ProfilePage.module.css";

const MAX_AVATAR_BYTES = 5 * 1024 * 1024;

export function ProfilePage() {
  const [profile, setProfile] = useState<MeResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [displayName, setDisplayName] = useState("");
  const [savingName, setSavingName] = useState(false);
  const [nameError, setNameError] = useState<unknown>(null);
  const [uploading, setUploading] = useState(false);
  const [avatarError, setAvatarError] = useState<unknown>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    meApi
      .getProfile()
      .then((result) => {
        setProfile(result);
        setDisplayName(result.displayName);
      })
      .finally(() => setLoading(false));
  }, []);

  async function handleSaveName() {
    if (!profile || !displayName.trim() || displayName === profile.displayName) return;
    setNameError(null);
    setSavingName(true);
    try {
      const updated = await meApi.updateProfile(displayName.trim());
      setProfile(updated);
      updateUser({ displayName: updated.displayName });
    } catch (error) {
      setNameError(error);
    } finally {
      setSavingName(false);
    }
  }

  function handleFileSelect(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    setAvatarError(null);
    if (!file.type.startsWith("image/")) {
      setAvatarError(new Error("Please choose an image file."));
      return;
    }
    if (file.size > MAX_AVATAR_BYTES) {
      setAvatarError(new Error("Images must be 5MB or smaller."));
      return;
    }

    const objectUrl = URL.createObjectURL(file);
    setAvatarPreview(objectUrl);
    setUploading(true);
    meApi
      .uploadAvatar(file)
      .then((updated) => setProfile(updated))
      .catch((error: unknown) => setAvatarError(error))
      .finally(() => {
        setUploading(false);
        URL.revokeObjectURL(objectUrl);
        setAvatarPreview(null);
      });
  }

  if (loading) {
    return <p className={styles.hint}>Loading…</p>;
  }

  if (!profile) {
    return <ErrorBanner error={new Error("Couldn't load your profile.")} />;
  }

  return (
    <div className={styles.page}>
      <h1>Profile</h1>
      <Card className={styles.card}>
        <div className={styles.avatarRow}>
          <Avatar name={profile.displayName} src={avatarPreview ?? profile.avatarUrl} size="lg" />
          <div>
            <Button variant="secondary" onClick={() => fileInputRef.current?.click()} disabled={uploading}>
              {uploading ? "Uploading…" : "Change photo"}
            </Button>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              className={styles.fileInput}
              onChange={handleFileSelect}
            />
          </div>
        </div>
        <ErrorBanner error={avatarError} />

        <div className={styles.field}>
          <TextField label="Display name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
          <Button
            variant="secondary"
            onClick={() => void handleSaveName()}
            disabled={savingName || !displayName.trim() || displayName === profile.displayName}
          >
            {savingName ? "Saving…" : "Save"}
          </Button>
        </div>
        <ErrorBanner error={nameError} />

        <dl className={styles.meta}>
          <div>
            <dt>Email</dt>
            <dd>{profile.email}</dd>
          </div>
          <div>
            <dt>Member since</dt>
            <dd>{new Date(profile.createdAt).toLocaleDateString()}</dd>
          </div>
        </dl>
      </Card>
    </div>
  );
}

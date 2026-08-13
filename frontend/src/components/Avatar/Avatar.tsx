import styles from "./Avatar.module.css";

interface AvatarProps {
  name: string;
  src?: string | null;
  size?: "sm" | "md" | "lg";
}

const PALETTE = [
  "var(--brand-primary)",
  "var(--brand-secondary)",
  "var(--status-coinflip)",
  "var(--status-voting)",
  "var(--day-2)",
  "var(--day-4)",
];

function initialsFor(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  const first = parts[0]?.[0] ?? "";
  const last = parts.length > 1 ? (parts[parts.length - 1]?.[0] ?? "") : "";
  return (first + last).toUpperCase();
}

function colorFor(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = (hash * 31 + name.charCodeAt(i)) >>> 0;
  }
  return PALETTE[hash % PALETTE.length]!;
}

export function Avatar({ name, src, size = "md" }: AvatarProps) {
  if (src) {
    return <img src={src} alt={name} className={`${styles.avatar} ${styles[size]}`} />;
  }
  return (
    <span
      className={`${styles.avatar} ${styles[size]} ${styles.initials}`}
      style={{ backgroundColor: colorFor(name) }}
      title={name}
    >
      {initialsFor(name)}
    </span>
  );
}

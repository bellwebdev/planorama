import type { ReactNode } from "react";
import { Card } from "../Card/Card";
import styles from "./AuthLayout.module.css";

interface AuthLayoutProps {
  title: string;
  subtitle?: string;
  children: ReactNode;
}

export function AuthLayout({ title, subtitle, children }: AuthLayoutProps) {
  return (
    <div className={styles.page}>
      <div className={styles.content}>
        <span className={styles.logo}>Planorama</span>
        <Card className={styles.card}>
          <h1 className={styles.title}>{title}</h1>
          {subtitle && <p className={styles.subtitle}>{subtitle}</p>}
          {children}
        </Card>
      </div>
    </div>
  );
}

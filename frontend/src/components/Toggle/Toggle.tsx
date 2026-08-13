import { useId, type InputHTMLAttributes } from "react";
import styles from "./Toggle.module.css";

interface ToggleProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "type"> {
  label: string;
}

export function Toggle({ label, id, ...props }: ToggleProps) {
  const generatedId = useId();
  const toggleId = id ?? generatedId;

  return (
    <div className={styles.row}>
      <label htmlFor={toggleId} className={styles.label}>
        {label}
      </label>
      <span className={styles.switch}>
        <input id={toggleId} type="checkbox" className={styles.input} {...props} />
        <span className={styles.track} />
        <span className={styles.thumb} />
      </span>
    </div>
  );
}

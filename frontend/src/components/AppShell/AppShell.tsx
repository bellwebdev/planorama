import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../../lib/auth/AuthContext";
import { Avatar } from "../Avatar/Avatar";
import { Button } from "../Button/Button";
import styles from "./AppShell.module.css";

function navLinkClassName({ isActive }: { isActive: boolean }) {
  return isActive ? `${styles.link} ${styles.linkActive}` : styles.link;
}

export function AppShell() {
  const { user, logout } = useAuth();

  return (
    <div className={styles.shell}>
      <header className={styles.header}>
        <div className={styles.headerInner}>
          <span className={styles.logo}>Planorama</span>
          <nav className={styles.nav}>
            <NavLink to="/profile" className={navLinkClassName}>
              Profile
            </NavLink>
            <NavLink to="/settings" className={navLinkClassName}>
              Settings
            </NavLink>
          </nav>
          <div className={styles.actions}>
            {user && <Avatar name={user.displayName} src={user.avatarUrl} size="sm" />}
            <Button variant="tertiary" onClick={() => void logout()}>
              Log out
            </Button>
          </div>
        </div>
      </header>
      <main className={styles.main}>
        <Outlet />
      </main>
    </div>
  );
}

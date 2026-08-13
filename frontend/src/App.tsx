import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "./components/AppShell/AppShell";
import { AuthProvider } from "./lib/auth/AuthContext";
import { RequireAuth } from "./lib/auth/RequireAuth";
import { ConfirmEmailPage } from "./pages/ConfirmEmailPage/ConfirmEmailPage";
import { LoginPage } from "./pages/LoginPage/LoginPage";
import { ProfilePage } from "./pages/ProfilePage/ProfilePage";
import { RegisterPage } from "./pages/RegisterPage/RegisterPage";
import { SettingsPage } from "./pages/SettingsPage/SettingsPage";

function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/confirm-email" element={<ConfirmEmailPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route
          element={
            <RequireAuth>
              <AppShell />
            </RequireAuth>
          }
        >
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Route>
        <Route path="/" element={<Navigate to="/profile" replace />} />
        <Route path="*" element={<Navigate to="/profile" replace />} />
      </Routes>
    </AuthProvider>
  );
}

export default App;

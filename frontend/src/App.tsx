import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "./components/AppShell/AppShell";
import { AuthProvider } from "./lib/auth/AuthContext";
import { RequireAuth } from "./lib/auth/RequireAuth";
import { AcceptInvitePage } from "./pages/AcceptInvitePage/AcceptInvitePage";
import { ConfirmEmailPage } from "./pages/ConfirmEmailPage/ConfirmEmailPage";
import { LoginPage } from "./pages/LoginPage/LoginPage";
import { NewTripPage } from "./pages/NewTripPage/NewTripPage";
import { ProfilePage } from "./pages/ProfilePage/ProfilePage";
import { RegisterPage } from "./pages/RegisterPage/RegisterPage";
import { SettingsPage } from "./pages/SettingsPage/SettingsPage";
import { TripDetailPage } from "./pages/TripDetailPage/TripDetailPage";
import { TripsListPage } from "./pages/TripsListPage/TripsListPage";

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
          <Route path="/trips" element={<TripsListPage />} />
          <Route path="/trips/new" element={<NewTripPage />} />
          <Route path="/trips/:id" element={<TripDetailPage />} />
          <Route path="/invites/:token" element={<AcceptInvitePage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Route>
        <Route path="/" element={<Navigate to="/trips" replace />} />
        <Route path="*" element={<Navigate to="/trips" replace />} />
      </Routes>
    </AuthProvider>
  );
}

export default App;

import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { selectIsAuthenticated, useAuthStore } from '@/stores/authStore';
import { useMe } from '@/features/auth/hooks';

export function RequireAuth() {
  const isAuthed = useAuthStore(selectIsAuthenticated);
  const location = useLocation();

  useMe();

  if (!isAuthed) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }
  return <Outlet />;
}

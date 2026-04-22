import { Navigate, Outlet } from 'react-router-dom';
import { selectIsAuthenticated, useAuthStore } from '@/stores/authStore';

export function PublicLayout() {
  const isAuthed = useAuthStore(selectIsAuthenticated);
  if (isAuthed) return <Navigate to="/" replace />;
  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/30 p-4">
      <Outlet />
    </div>
  );
}

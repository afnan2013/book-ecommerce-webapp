import { Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { UserType } from '@/lib/types/user';

export function RoleLandingRedirect() {
  const user = useAuthStore((s) => s.user);
  if (!user) return <Navigate to="/login" replace />;
  switch (user.userType) {
    case UserType.Admin:
      return <Navigate to="/admin" replace />;
    case UserType.Buyer:
      return <Navigate to="/buyer" replace />;
    case UserType.Seller:
      return <Navigate to="/seller" replace />;
    default:
      return <Navigate to="/forbidden" replace />;
  }
}

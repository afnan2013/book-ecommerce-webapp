import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import type { UserType } from '@/lib/types/user';

interface Props {
  userType: UserType;
  children: ReactNode;
}

export function RequireUserType({ userType, children }: Props) {
  const user = useAuthStore((s) => s.user);
  if (!user) return <Navigate to="/login" replace />;
  if (user.userType !== userType) return <Navigate to="/forbidden" replace />;
  return <>{children}</>;
}

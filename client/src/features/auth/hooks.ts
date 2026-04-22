import { useMutation, useQuery } from '@tanstack/react-query';
import {
  fetchMe,
  login,
  register,
  type LoginRequest,
  type RegisterRequest,
} from './api';
import { useAuthStore } from '@/stores/authStore';
import { queryKeys } from '@/lib/queryKeys';

export function useLogin() {
  const setAuth = useAuthStore((s) => s.setAuth);
  return useMutation({
    mutationFn: (req: LoginRequest) => login(req),
    onSuccess: ({ accessToken, user }) => setAuth(user, accessToken),
  });
}

export function useRegister() {
  const setAuth = useAuthStore((s) => s.setAuth);
  return useMutation({
    mutationFn: (req: RegisterRequest) => register(req),
    onSuccess: ({ accessToken, user }) => setAuth(user, accessToken),
  });
}

export function useMe() {
  const token = useAuthStore((s) => s.token);
  const setUser = useAuthStore((s) => s.setUser);
  return useQuery({
    queryKey: queryKeys.auth.me,
    queryFn: async () => {
      const user = await fetchMe();
      setUser(user);
      return user;
    },
    enabled: Boolean(token),
    staleTime: 60_000,
  });
}

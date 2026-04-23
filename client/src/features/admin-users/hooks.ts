import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createUser,
  deleteUser,
  getUser,
  listUsers,
  updateUserPermissions,
  updateUserRoles,
  type CreateUserRequest,
  type UpdateUserPermissionsRequest,
  type UpdateUserRolesRequest,
} from './api';
import { queryKeys } from '@/lib/queryKeys';
import { ApiError } from '@/lib/api/ApiError';

export function useUsers() {
  return useQuery({
    queryKey: queryKeys.users.list(),
    queryFn: listUsers,
  });
}

export function useUser(id: number) {
  return useQuery({
    queryKey: queryKeys.users.byId(id),
    queryFn: () => getUser(id),
    enabled: Number.isFinite(id) && id > 0,
  });
}

export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (req: CreateUserRequest) => createUser(req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.users.all });
    },
  });
}

export function useDeleteUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deleteUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.users.all });
    },
  });
}

export function useUpdateUserRoles(id: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (req: UpdateUserRolesRequest) => updateUserRoles(id, req),
    onSuccess: (user) => {
      queryClient.setQueryData(queryKeys.users.byId(id), user);
      queryClient.invalidateQueries({ queryKey: queryKeys.users.list() });
    },
    onError: (err) => {
      if (err instanceof ApiError && err.kind === 'conflict') {
        queryClient.invalidateQueries({ queryKey: queryKeys.users.byId(id) });
      }
    },
  });
}

export function useUpdateUserPermissions(id: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (req: UpdateUserPermissionsRequest) =>
      updateUserPermissions(id, req),
    onSuccess: (user) => {
      queryClient.setQueryData(queryKeys.users.byId(id), user);
      queryClient.invalidateQueries({ queryKey: queryKeys.users.list() });
    },
    onError: (err) => {
      if (err instanceof ApiError && err.kind === 'conflict') {
        queryClient.invalidateQueries({ queryKey: queryKeys.users.byId(id) });
      }
    },
  });
}

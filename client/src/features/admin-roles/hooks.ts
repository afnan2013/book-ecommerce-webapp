import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createRole,
  deleteRole,
  getRole,
  listRoles,
  setRolePermissions,
  updateRole,
  type CreateRoleRequest,
  type SetRolePermissionsRequest,
  type UpdateRoleRequest,
} from './api';
import { queryKeys } from '@/lib/queryKeys';
import { ApiError } from '@/lib/api/ApiError';

export function useRoles() {
  return useQuery({
    queryKey: queryKeys.roles.list(),
    queryFn: listRoles,
  });
}

export function useRole(id: number) {
  return useQuery({
    queryKey: queryKeys.roles.byId(id),
    queryFn: () => getRole(id),
    enabled: Number.isFinite(id) && id > 0,
  });
}

export function useCreateRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (req: CreateRoleRequest) => createRole(req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.roles.all });
    },
  });
}

export function useUpdateRole(id: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (req: UpdateRoleRequest) => updateRole(id, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.roles.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.users.all });
    },
    onError: (err) => {
      if (err instanceof ApiError && err.kind === 'conflict') {
        queryClient.invalidateQueries({ queryKey: queryKeys.roles.byId(id) });
      }
    },
  });
}

export function useDeleteRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deleteRole(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.roles.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.users.all });
    },
  });
}

export function useSetRolePermissions(id: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (req: SetRolePermissionsRequest) =>
      setRolePermissions(id, req),
    onSuccess: (role) => {
      queryClient.setQueryData(queryKeys.roles.byId(id), role);
      queryClient.invalidateQueries({ queryKey: queryKeys.roles.list() });
    },
    onError: (err) => {
      if (err instanceof ApiError && err.kind === 'conflict') {
        queryClient.invalidateQueries({ queryKey: queryKeys.roles.byId(id) });
      }
    },
  });
}

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  getUser,
  listUsers,
  updateUserPermissions,
  updateUserRoles,
  type UpdateUserPermissionsRequest,
  type UpdateUserRolesRequest,
} from './api';
import { queryKeys } from '@/lib/queryKeys';

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

export function useUpdateUserRoles(id: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (req: UpdateUserRolesRequest) => updateUserRoles(id, req),
    onSuccess: (user) => {
      queryClient.setQueryData(queryKeys.users.byId(id), user);
      queryClient.invalidateQueries({ queryKey: queryKeys.users.list() });
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
  });
}

import { useQuery } from '@tanstack/react-query';
import { listPermissions } from './api';
import { queryKeys } from '@/lib/queryKeys';

export function usePermissions() {
  return useQuery({
    queryKey: queryKeys.permissions.list(),
    queryFn: listPermissions,
    staleTime: 5 * 60_000,
  });
}

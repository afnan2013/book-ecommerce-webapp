import type { QueryClient, QueryKey } from '@tanstack/react-query';
import { toast } from 'sonner';
import { isApiProblem, isConflict } from '@/lib/types/problem';

interface HandleErrorOptions {
  queryClient: QueryClient;
  invalidateKey: QueryKey;
  entityLabel: string;
}

/**
 * Common error translator for admin mutations. 409 is treated as an
 * optimistic-concurrency conflict: toast + refetch so the user sees the
 * latest state and can re-apply their changes.
 */
export function handleMutationError(
  err: unknown,
  { queryClient, invalidateKey, entityLabel }: HandleErrorOptions,
): void {
  if (isConflict(err)) {
    toast.error(
      `This ${entityLabel} was modified by someone else. We've refreshed to the latest — please re-apply your changes.`,
    );
    queryClient.invalidateQueries({ queryKey: invalidateKey });
    return;
  }
  if (isApiProblem(err)) {
    toast.error(err.title || `Failed to update ${entityLabel}.`);
    return;
  }
  toast.error(`Failed to update ${entityLabel}.`);
}

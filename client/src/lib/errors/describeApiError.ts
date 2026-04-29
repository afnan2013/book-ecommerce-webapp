import { ApiError } from '@/lib/api/ApiError';

/**
 * Turns any error thrown from an api-client call into a user-facing string.
 *
 * - If the server provided a message (title or detail), use it.
 * - For 409 conflicts, fall back to an entity-aware "modified by someone else"
 *   message when the server didn't send its own.
 * - For anything else (network errors, non-ApiError throws), use `fallback`.
 */
export function describeApiError(
  err: unknown,
  fallback: string,
  entity?: string,
): string {
  if (err instanceof ApiError) {
    if (err.kind === 'conflict') {
      return (
        err.detail ??
        `This ${entity ?? 'record'} was modified by someone else. Please refresh and try again.`
      );
    }
    return err.detail ?? err.title ?? fallback;
  }
  return fallback;
}

/**
 * Wire shape of an error body from the server. Follows RFC 7807
 * (application/problem+json) but also tolerates the current server's
 * ad-hoc `{ error: "..." }` bodies — that retrofit is a separate task.
 */
export interface ApiErrorDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: unknown;
  error?: string;
  [key: string]: unknown;
}

/**
 * Discriminator for branching on error meaning rather than raw HTTP status.
 * Callers should prefer `err.kind === 'conflict'` over `err.status === 409`.
 */
export type ApiErrorKind =
  | 'conflict'
  | 'validation'
  | 'unauthorized'
  | 'forbidden'
  | 'notFound'
  | 'server'
  | 'unknown';

export class ApiError extends Error {
  readonly status: number;
  readonly kind: ApiErrorKind;
  readonly title?: string;
  readonly detail?: string;
  readonly details: ApiErrorDetails;

  constructor(status: number, details: ApiErrorDetails) {
    const title =
      details.title ??
      (typeof details.error === 'string' ? details.error : undefined);
    const message = title ?? details.detail ?? `HTTP ${status}`;
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.kind = kindFromStatus(status);
    this.title = title;
    this.detail = details.detail;
    this.details = details;
  }
}

function kindFromStatus(status: number): ApiErrorKind {
  switch (status) {
    case 400:
      return 'validation';
    case 401:
      return 'unauthorized';
    case 403:
      return 'forbidden';
    case 404:
      return 'notFound';
    case 409:
      return 'conflict';
    default:
      return status >= 500 ? 'server' : 'unknown';
  }
}

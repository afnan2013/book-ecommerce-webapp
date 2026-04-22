export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: unknown;
  error?: string;
  [key: string]: unknown;
}

export class ApiProblem extends Error {
  readonly status: number;
  readonly title: string;
  readonly detail?: string;
  readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    const message =
      problem.title ??
      problem.detail ??
      (typeof problem.error === 'string' ? problem.error : undefined) ??
      `HTTP ${status}`;
    super(message);
    this.name = 'ApiProblem';
    this.status = status;
    this.title = problem.title ?? `HTTP ${status}`;
    this.detail = problem.detail;
    this.problem = problem;
  }
}

export function isApiProblem(err: unknown): err is ApiProblem {
  return err instanceof ApiProblem;
}

export function isConflict(err: unknown): boolean {
  return isApiProblem(err) && err.status === 409;
}

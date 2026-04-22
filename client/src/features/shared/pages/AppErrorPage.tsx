import { Link, isRouteErrorResponse, useRouteError } from 'react-router-dom';
import { Button } from '@/components/ui/button';

export function AppErrorPage() {
  const error = useRouteError();

  let title = 'Something went wrong';
  let detail: string | undefined;

  if (isRouteErrorResponse(error)) {
    title = `${error.status} ${error.statusText}`;
    detail = typeof error.data === 'string' ? error.data : undefined;
  } else if (error instanceof Error) {
    detail = error.message;
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-4 p-4 text-center">
      <h1 className="text-3xl font-semibold">{title}</h1>
      {detail && (
        <pre className="text-sm text-muted-foreground max-w-xl whitespace-pre-wrap">
          {detail}
        </pre>
      )}
      <Button asChild variant="outline">
        <Link to="/">Go home</Link>
      </Button>
    </div>
  );
}

import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';

export function ForbiddenPage() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-4 p-4 text-center">
      <h1 className="text-3xl font-semibold">Forbidden</h1>
      <p className="text-muted-foreground max-w-md">
        You don't have access to this page. If you think this is a mistake,
        contact an administrator.
      </p>
      <Button asChild variant="outline">
        <Link to="/">Go home</Link>
      </Button>
    </div>
  );
}

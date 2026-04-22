import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';

interface Props {
  title: string;
  backTo: string;
  backLabel: string;
}

export function NotFoundState({ title, backTo, backLabel }: Props) {
  return (
    <div className="space-y-4">
      <h2 className="text-2xl font-semibold">{title}</h2>
      <Button asChild variant="outline">
        <Link to={backTo}>{backLabel}</Link>
      </Button>
    </div>
  );
}

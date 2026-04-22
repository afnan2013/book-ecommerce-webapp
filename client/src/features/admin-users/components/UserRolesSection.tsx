import { useMemo, useState } from 'react';
import { toast } from 'sonner';
import { useQueryClient } from '@tanstack/react-query';
import { useUpdateUserRoles } from '../hooks';
import { handleMutationError } from '@/lib/handleMutationError';
import { queryKeys } from '@/lib/queryKeys';
import type { Role } from '@/lib/types/role';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';

interface Props {
  userId: number;
  concurrencyStamp: string;
  initialRoleIds: number[];
  allRoles: Role[];
  rolesLoading: boolean;
}

export function UserRolesSection({
  userId,
  concurrencyStamp,
  initialRoleIds,
  allRoles,
  rolesLoading,
}: Props) {
  const initialSet = useMemo(() => new Set(initialRoleIds), [initialRoleIds]);
  const [selected, setSelected] = useState<Set<number>>(() => initialSet);
  const mutation = useUpdateUserRoles(userId);
  const queryClient = useQueryClient();

  const hasChanges = useMemo(() => {
    if (selected.size !== initialSet.size) return true;
    for (const id of initialSet) if (!selected.has(id)) return true;
    return false;
  }, [selected, initialSet]);

  const toggle = (id: number) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleSave = () => {
    mutation.mutate(
      { roleIds: Array.from(selected), concurrencyStamp },
      {
        onSuccess: () => toast.success('Roles updated.'),
        onError: (err) =>
          handleMutationError(err, {
            queryClient,
            invalidateKey: queryKeys.users.byId(userId),
            entityLabel: 'user',
          }),
      },
    );
  };

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Roles</CardTitle>
          <CardDescription>
            Roles grant bundles of permissions. A user can have multiple.
          </CardDescription>
        </div>
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => setSelected(new Set(initialSet))}
            disabled={!hasChanges || mutation.isPending}
          >
            Reset
          </Button>
          <Button
            type="button"
            size="sm"
            onClick={handleSave}
            disabled={!hasChanges || mutation.isPending}
          >
            {mutation.isPending ? 'Saving…' : 'Save roles'}
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {rolesLoading && (
          <p className="text-muted-foreground text-sm">Loading roles…</p>
        )}
        {!rolesLoading && allRoles.length === 0 && (
          <p className="text-muted-foreground text-sm">No roles defined.</p>
        )}
        <ul className="divide-y">
          {allRoles.map((role) => (
            <li key={role.id} className="flex items-start gap-3 py-3">
              <Checkbox
                id={`role-${role.id}`}
                checked={selected.has(role.id)}
                onCheckedChange={() => toggle(role.id)}
                disabled={mutation.isPending}
              />
              <div className="flex-1">
                <Label htmlFor={`role-${role.id}`} className="font-medium">
                  {role.name}
                </Label>
                <p className="text-sm text-muted-foreground">
                  {role.permissions.length} permission
                  {role.permissions.length === 1 ? '' : 's'}
                </p>
              </div>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  );
}

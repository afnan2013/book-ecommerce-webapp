import { useMemo, useState } from 'react';
import { toast } from 'sonner';
import { useQueryClient } from '@tanstack/react-query';
import { useUpdateUserPermissions } from '../hooks';
import { handleMutationError } from '@/lib/handleMutationError';
import { queryKeys } from '@/lib/queryKeys';
import type { Permission } from '@/lib/types/permission';
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
  initialPermissionIds: number[];
  allPermissions: Permission[];
  permissionsLoading: boolean;
}

export function UserDirectPermissionsSection({
  userId,
  concurrencyStamp,
  initialPermissionIds,
  allPermissions,
  permissionsLoading,
}: Props) {
  const initialSet = useMemo(
    () => new Set(initialPermissionIds),
    [initialPermissionIds],
  );
  const [selected, setSelected] = useState<Set<number>>(() => initialSet);
  const mutation = useUpdateUserPermissions(userId);
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
      { permissionIds: Array.from(selected), concurrencyStamp },
      {
        onSuccess: () => toast.success('Direct permissions updated.'),
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
          <CardTitle>Direct permissions</CardTitle>
          <CardDescription>
            Granted to this user regardless of their roles. Use sparingly — prefer
            roles when the permission applies to a group.
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
            {mutation.isPending ? 'Saving…' : 'Save permissions'}
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {permissionsLoading && (
          <p className="text-muted-foreground text-sm">Loading permissions…</p>
        )}
        {!permissionsLoading && allPermissions.length === 0 && (
          <p className="text-muted-foreground text-sm">No permissions defined.</p>
        )}
        <ul className="divide-y">
          {allPermissions.map((perm) => (
            <li key={perm.id} className="flex items-start gap-3 py-3">
              <Checkbox
                id={`direct-perm-${perm.id}`}
                checked={selected.has(perm.id)}
                onCheckedChange={() => toggle(perm.id)}
                disabled={mutation.isPending}
              />
              <div className="flex-1">
                <Label htmlFor={`direct-perm-${perm.id}`} className="font-medium">
                  {perm.name}
                </Label>
                {perm.description && (
                  <p className="text-sm text-muted-foreground">
                    {perm.description}
                  </p>
                )}
              </div>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  );
}

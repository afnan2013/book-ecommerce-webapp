import { useMemo, useState } from 'react';
import { toast } from 'sonner';
import { useSetRolePermissions } from '../hooks';
import { describeApiError } from '@/lib/errors/describeApiError';
import type { Permission } from '@/lib/types/permission';
import { PermissionPicker } from '@/components/PermissionPicker';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface Props {
  roleId: string;
  concurrencyStamp: string;
  initialPermissionIds: string[];
  allPermissions: Permission[];
  permissionsLoading: boolean;
}

export function RolePermissionsSection({
  roleId,
  concurrencyStamp,
  initialPermissionIds,
  allPermissions,
  permissionsLoading,
}: Props) {
  const initialSet = useMemo(
    () => new Set(initialPermissionIds),
    [initialPermissionIds],
  );
  const [selected, setSelected] = useState<Set<string>>(() => initialSet);
  const setPermsMutation = useSetRolePermissions(roleId);

  const hasChanges = useMemo(() => {
    if (selected.size !== initialSet.size) return true;
    for (const id of initialSet) if (!selected.has(id)) return true;
    return false;
  }, [selected, initialSet]);

  const handleSave = () => {
    setPermsMutation.mutate(
      {
        permissionIds: Array.from(selected),
        concurrencyStamp,
      },
      {
        onSuccess: () => toast.success('Permissions updated.'),
        onError: (err) =>
          toast.error(
            describeApiError(err, 'Failed to update permissions.', 'role'),
          ),
      },
    );
  };

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Permissions</CardTitle>
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => setSelected(new Set(initialSet))}
            disabled={!hasChanges || setPermsMutation.isPending}
          >
            Reset
          </Button>
          <Button
            type="button"
            size="sm"
            onClick={handleSave}
            disabled={!hasChanges || setPermsMutation.isPending}
          >
            {setPermsMutation.isPending ? 'Saving…' : 'Save permissions'}
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
        <PermissionPicker
          permissions={allPermissions}
          selected={selected}
          onChange={setSelected}
          disabled={setPermsMutation.isPending}
          idPrefix="perm"
        />
      </CardContent>
    </Card>
  );
}

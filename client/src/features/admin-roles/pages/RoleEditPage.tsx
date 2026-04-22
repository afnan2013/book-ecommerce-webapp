import { useMemo, useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { useQueryClient } from '@tanstack/react-query';
import { useRole, useSetRolePermissions, useUpdateRole } from '../hooks';
import { usePermissions } from '@/features/permissions/hooks';
import { handleMutationError } from '@/lib/handleMutationError';
import { isApiProblem } from '@/lib/types/problem';
import { queryKeys } from '@/lib/queryKeys';
import type { Permission } from '@/lib/types/permission';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export function RoleEditPage() {
  const { id: idParam } = useParams<{ id: string }>();
  const id = Number(idParam);
  const roleQuery = useRole(id);
  const permissionsQuery = usePermissions();

  if (!Number.isFinite(id) || id <= 0) {
    return <NotFound />;
  }

  return (
    <div className="space-y-4">
      <Button asChild variant="outline" size="sm">
        <Link to="/admin/roles">← Back to roles</Link>
      </Button>

      {roleQuery.isLoading && <p className="text-muted-foreground">Loading…</p>}

      {roleQuery.isError && (
        <div className="rounded-md border border-destructive/50 bg-destructive/5 p-4">
          <p className="text-sm text-destructive">
            {isApiProblem(roleQuery.error) ? roleQuery.error.title : 'Failed to load role.'}
          </p>
        </div>
      )}

      {roleQuery.data && (
        <>
          <RenameSection
            key={`rename-${roleQuery.data.concurrencyStamp}`}
            roleId={id}
            initialName={roleQuery.data.name}
            concurrencyStamp={roleQuery.data.concurrencyStamp}
          />
          <PermissionsSection
            key={`perms-${roleQuery.data.concurrencyStamp}`}
            roleId={id}
            concurrencyStamp={roleQuery.data.concurrencyStamp}
            initialPermissionIds={roleQuery.data.permissions.map((p) => p.id)}
            allPermissions={permissionsQuery.data ?? []}
            permissionsLoading={permissionsQuery.isLoading}
          />
        </>
      )}
    </div>
  );
}

function NotFound() {
  return (
    <div className="space-y-4">
      <h2 className="text-2xl font-semibold">Role not found</h2>
      <Button asChild variant="outline">
        <Link to="/admin/roles">Back to roles</Link>
      </Button>
    </div>
  );
}

function RenameSection({
  roleId,
  initialName,
  concurrencyStamp,
}: {
  roleId: number;
  initialName: string;
  concurrencyStamp: string;
}) {
  const [name, setName] = useState(initialName);
  const updateMutation = useUpdateRole(roleId);
  const queryClient = useQueryClient();

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const trimmed = name.trim();
    if (!trimmed || trimmed === initialName) return;
    updateMutation.mutate(
      { name: trimmed, concurrencyStamp },
      {
        onSuccess: () => toast.success('Role renamed.'),
        onError: (err) =>
          handleMutationError(err, {
            queryClient,
            invalidateKey: queryKeys.roles.byId(roleId),
            entityLabel: 'role',
          }),
      },
    );
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Rename</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="flex items-end gap-3 max-w-md">
          <div className="flex-1 space-y-2">
            <Label htmlFor="role-name">Role name</Label>
            <Input
              id="role-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              minLength={1}
            />
          </div>
          <Button
            type="submit"
            disabled={
              updateMutation.isPending ||
              name.trim() === '' ||
              name.trim() === initialName
            }
          >
            {updateMutation.isPending ? 'Saving…' : 'Save'}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function PermissionsSection({
  roleId,
  concurrencyStamp,
  initialPermissionIds,
  allPermissions,
  permissionsLoading,
}: {
  roleId: number;
  concurrencyStamp: string;
  initialPermissionIds: number[];
  allPermissions: Permission[];
  permissionsLoading: boolean;
}) {
  const initialSet = useMemo(
    () => new Set(initialPermissionIds),
    [initialPermissionIds],
  );
  const [selected, setSelected] = useState<Set<number>>(() => initialSet);
  const setPermsMutation = useSetRolePermissions(roleId);
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
    setPermsMutation.mutate(
      {
        permissionIds: Array.from(selected),
        concurrencyStamp,
      },
      {
        onSuccess: () => toast.success('Permissions updated.'),
        onError: (err) =>
          handleMutationError(err, {
            queryClient,
            invalidateKey: queryKeys.roles.byId(roleId),
            entityLabel: 'role',
          }),
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
        <ul className="divide-y">
          {allPermissions.map((perm) => (
            <li key={perm.id} className="flex items-start gap-3 py-3">
              <Checkbox
                id={`perm-${perm.id}`}
                checked={selected.has(perm.id)}
                onCheckedChange={() => toggle(perm.id)}
                disabled={setPermsMutation.isPending}
              />
              <div className="flex-1">
                <Label htmlFor={`perm-${perm.id}`} className="font-medium">
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

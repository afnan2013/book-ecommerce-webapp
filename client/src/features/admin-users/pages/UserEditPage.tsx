import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { useQueryClient } from '@tanstack/react-query';
import {
  useUpdateUserPermissions,
  useUpdateUserRoles,
  useUser,
} from '../hooks';
import { useRoles } from '@/features/admin-roles/hooks';
import { usePermissions } from '@/features/permissions/hooks';
import { handleMutationError } from '@/lib/handleMutationError';
import { isApiProblem } from '@/lib/types/problem';
import { queryKeys } from '@/lib/queryKeys';
import { UserTypeLabel } from '@/lib/types/user';
import type { Permission } from '@/lib/types/permission';
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

export function UserEditPage() {
  const { id: idParam } = useParams<{ id: string }>();
  const id = Number(idParam);
  const userQuery = useUser(id);
  const rolesQuery = useRoles();
  const permissionsQuery = usePermissions();

  if (!Number.isFinite(id) || id <= 0) {
    return <NotFound />;
  }

  return (
    <div className="space-y-4">
      <Button asChild variant="outline" size="sm">
        <Link to="/admin/users">← Back to users</Link>
      </Button>

      {userQuery.isLoading && <p className="text-muted-foreground">Loading…</p>}

      {userQuery.isError && (
        <div className="rounded-md border border-destructive/50 bg-destructive/5 p-4">
          <p className="text-sm text-destructive">
            {isApiProblem(userQuery.error) ? userQuery.error.title : 'Failed to load user.'}
          </p>
        </div>
      )}

      {userQuery.data && (
        <>
          <Card>
            <CardHeader>
              <CardTitle>{userQuery.data.email}</CardTitle>
              <CardDescription>
                {userQuery.data.fullName || <span className="italic">No name</span>} ·{' '}
                {UserTypeLabel[userQuery.data.userType]}
              </CardDescription>
            </CardHeader>
          </Card>

          <RolesSection
            key={`roles-${userQuery.data.concurrencyStamp}`}
            userId={id}
            concurrencyStamp={userQuery.data.concurrencyStamp}
            initialRoleIds={userQuery.data.roles.map((r) => r.id)}
            allRoles={rolesQuery.data ?? []}
            rolesLoading={rolesQuery.isLoading}
          />

          <DirectPermissionsSection
            key={`perms-${userQuery.data.concurrencyStamp}`}
            userId={id}
            concurrencyStamp={userQuery.data.concurrencyStamp}
            initialPermissionIds={userQuery.data.directPermissions.map((p) => p.id)}
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
      <h2 className="text-2xl font-semibold">User not found</h2>
      <Button asChild variant="outline">
        <Link to="/admin/users">Back to users</Link>
      </Button>
    </div>
  );
}

function RolesSection({
  userId,
  concurrencyStamp,
  initialRoleIds,
  allRoles,
  rolesLoading,
}: {
  userId: number;
  concurrencyStamp: string;
  initialRoleIds: number[];
  allRoles: Role[];
  rolesLoading: boolean;
}) {
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

function DirectPermissionsSection({
  userId,
  concurrencyStamp,
  initialPermissionIds,
  allPermissions,
  permissionsLoading,
}: {
  userId: number;
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

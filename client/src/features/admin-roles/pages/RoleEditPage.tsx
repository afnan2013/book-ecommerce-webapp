import { Link, useParams } from 'react-router-dom';
import { useRole } from '../hooks';
import { RoleRenameSection } from '../components/RoleRenameSection';
import { RolePermissionsSection } from '../components/RolePermissionsSection';
import { usePermissions } from '@/features/permissions/hooks';
import { describeApiError } from '@/lib/errors/describeApiError';
import { Button } from '@/components/ui/button';
import { NotFoundState } from '@/components/NotFoundState';

export function RoleEditPage() {
  const { id: idParam } = useParams<{ id: string }>();
  const id = Number(idParam);
  const roleQuery = useRole(id);
  const permissionsQuery = usePermissions();

  if (!Number.isFinite(id) || id <= 0) {
    return (
      <NotFoundState
        title="Role not found"
        backTo="/admin/roles"
        backLabel="Back to roles"
      />
    );
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
            {describeApiError(roleQuery.error, 'Failed to load role.')}
          </p>
        </div>
      )}

      {roleQuery.data && (
        <>
          <RoleRenameSection
            key={`rename-${roleQuery.data.concurrencyStamp}`}
            roleId={id}
            initialName={roleQuery.data.name}
            concurrencyStamp={roleQuery.data.concurrencyStamp}
          />
          <RolePermissionsSection
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

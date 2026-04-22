import { Link, useParams } from 'react-router-dom';
import { useUser } from '../hooks';
import { UserRolesSection } from '../components/UserRolesSection';
import { UserDirectPermissionsSection } from '../components/UserDirectPermissionsSection';
import { useRoles } from '@/features/admin-roles/hooks';
import { usePermissions } from '@/features/permissions/hooks';
import { isApiProblem } from '@/lib/types/problem';
import { UserTypeLabel } from '@/lib/types/user';
import { Button } from '@/components/ui/button';
import { NotFoundState } from '@/components/NotFoundState';
import {
  Card,
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
    return (
      <NotFoundState
        title="User not found"
        backTo="/admin/users"
        backLabel="Back to users"
      />
    );
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

          <UserRolesSection
            key={`roles-${userQuery.data.concurrencyStamp}`}
            userId={id}
            concurrencyStamp={userQuery.data.concurrencyStamp}
            initialRoleIds={userQuery.data.roles.map((r) => r.id)}
            allRoles={rolesQuery.data ?? []}
            rolesLoading={rolesQuery.isLoading}
          />

          <UserDirectPermissionsSection
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

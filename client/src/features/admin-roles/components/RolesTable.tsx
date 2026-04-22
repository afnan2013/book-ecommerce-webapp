import { Link } from 'react-router-dom';
import type { Role } from '@/lib/types/role';
import { Button } from '@/components/ui/button';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';

function isSuperAdmin(role: Role): boolean {
  return role.name.toLowerCase() === 'superadmin';
}

interface Props {
  roles: Role[] | undefined;
  isLoading: boolean;
  onDelete: (role: Role) => void;
}

export function RolesTable({ roles, isLoading, onDelete }: Props) {
  return (
    <div className="rounded-md border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Name</TableHead>
            <TableHead>Permissions</TableHead>
            <TableHead className="w-48 text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {isLoading && (
            <TableRow>
              <TableCell
                colSpan={3}
                className="text-center text-muted-foreground py-8"
              >
                Loading…
              </TableCell>
            </TableRow>
          )}
          {!isLoading && roles?.length === 0 && (
            <TableRow>
              <TableCell
                colSpan={3}
                className="text-center text-muted-foreground py-8"
              >
                No roles yet.
              </TableCell>
            </TableRow>
          )}
          {roles?.map((role) => (
            <RoleTableRow key={role.id} role={role} onDelete={onDelete} />
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

function RoleTableRow({
  role,
  onDelete,
}: {
  role: Role;
  onDelete: (role: Role) => void;
}) {
  const locked = isSuperAdmin(role);
  return (
    <TableRow>
      <TableCell className="font-medium">{role.name}</TableCell>
      <TableCell className="text-sm text-muted-foreground">
        {role.permissions.length} permission
        {role.permissions.length === 1 ? '' : 's'}
      </TableCell>
      <TableCell className="text-right space-x-2">
        <Button asChild variant="outline" size="sm" disabled={locked}>
          <Link to={`/admin/roles/${role.id}`}>Edit</Link>
        </Button>
        <Button
          variant="outline"
          size="sm"
          disabled={locked}
          title={locked ? 'SuperAdmin cannot be deleted' : undefined}
          onClick={() => onDelete(role)}
        >
          Delete
        </Button>
      </TableCell>
    </TableRow>
  );
}

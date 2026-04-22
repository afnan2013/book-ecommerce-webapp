import { Link } from 'react-router-dom';
import { UserTypeLabel } from '@/lib/types/user';
import type { UserDetail } from '@/lib/types/user';
import { Button } from '@/components/ui/button';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';

interface Props {
  users: UserDetail[];
  isLoading: boolean;
  emptyLabel: string;
  currentUserId: number | undefined;
  onDelete: (user: UserDetail) => void;
}

export function UsersTable({
  users,
  isLoading,
  emptyLabel,
  currentUserId,
  onDelete,
}: Props) {
  return (
    <div className="rounded-md border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Email</TableHead>
            <TableHead>Full name</TableHead>
            <TableHead>Type</TableHead>
            <TableHead>Roles</TableHead>
            <TableHead>Direct perms</TableHead>
            <TableHead className="w-40 text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {isLoading && (
            <TableRow>
              <TableCell
                colSpan={6}
                className="text-center text-muted-foreground py-8"
              >
                Loading…
              </TableCell>
            </TableRow>
          )}
          {!isLoading && users.length === 0 && (
            <TableRow>
              <TableCell
                colSpan={6}
                className="text-center text-muted-foreground py-8"
              >
                {emptyLabel}
              </TableCell>
            </TableRow>
          )}
          {users.map((user) => (
            <UserTableRow
              key={user.id}
              user={user}
              isSelf={user.id === currentUserId}
              onDelete={onDelete}
            />
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

function UserTableRow({
  user,
  isSelf,
  onDelete,
}: {
  user: UserDetail;
  isSelf: boolean;
  onDelete: (user: UserDetail) => void;
}) {
  return (
    <TableRow>
      <TableCell className="font-medium">{user.email}</TableCell>
      <TableCell>
        {user.fullName || <span className="text-muted-foreground">—</span>}
      </TableCell>
      <TableCell>{UserTypeLabel[user.userType]}</TableCell>
      <TableCell>
        {user.roles.length === 0 ? (
          <span className="text-muted-foreground">—</span>
        ) : (
          <span className="text-sm">
            {user.roles.map((r) => r.name).join(', ')}
          </span>
        )}
      </TableCell>
      <TableCell className="text-sm">{user.directPermissions.length}</TableCell>
      <TableCell className="text-right space-x-2">
        <Button asChild variant="outline" size="sm">
          <Link to={`/admin/users/${user.id}`}>Edit</Link>
        </Button>
        <Button
          variant="outline"
          size="sm"
          disabled={isSelf}
          title={isSelf ? 'You cannot delete your own account' : undefined}
          onClick={() => onDelete(user)}
        >
          Delete
        </Button>
      </TableCell>
    </TableRow>
  );
}

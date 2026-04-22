import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useUsers } from '../hooks';
import { UserTypeLabel } from '@/lib/types/user';
import { isApiProblem } from '@/lib/types/problem';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';

export function UsersListPage() {
  const [query, setQuery] = useState('');
  const { data, isLoading, isError, error, refetch, isFetching } = useUsers();

  const filtered = useMemo(() => {
    if (!data) return [];
    const q = query.trim().toLowerCase();
    if (!q) return data;
    return data.filter(
      (u) =>
        u.email.toLowerCase().includes(q) ||
        u.fullName.toLowerCase().includes(q),
    );
  }, [data, query]);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-semibold">Users</h2>
          <p className="text-sm text-muted-foreground">
            Manage user accounts, roles, and direct permissions.
          </p>
        </div>
        <Input
          placeholder="Search by email or name…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          className="max-w-xs"
        />
      </div>

      {isError && (
        <div className="rounded-md border border-destructive/50 bg-destructive/5 p-4">
          <p className="text-sm text-destructive">
            {isApiProblem(error) ? error.title : 'Failed to load users.'}
          </p>
          <Button
            variant="outline"
            size="sm"
            className="mt-2"
            onClick={() => refetch()}
          >
            Try again
          </Button>
        </div>
      )}

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Email</TableHead>
              <TableHead>Full name</TableHead>
              <TableHead>Type</TableHead>
              <TableHead>Roles</TableHead>
              <TableHead>Direct perms</TableHead>
              <TableHead className="w-24 text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-muted-foreground py-8">
                  Loading…
                </TableCell>
              </TableRow>
            )}
            {!isLoading && filtered.length === 0 && (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-muted-foreground py-8">
                  {query ? 'No users match your search.' : 'No users yet.'}
                </TableCell>
              </TableRow>
            )}
            {filtered.map((user) => (
              <TableRow key={user.id}>
                <TableCell className="font-medium">{user.email}</TableCell>
                <TableCell>{user.fullName || <span className="text-muted-foreground">—</span>}</TableCell>
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
                <TableCell className="text-sm">
                  {user.directPermissions.length}
                </TableCell>
                <TableCell className="text-right">
                  <Button asChild variant="outline" size="sm">
                    <Link to={`/admin/users/${user.id}`}>Edit</Link>
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {isFetching && !isLoading && (
        <p className="text-xs text-muted-foreground">Refreshing…</p>
      )}
    </div>
  );
}

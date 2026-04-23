import { useMemo, useState } from 'react';
import { useUsers } from '../hooks';
import { CreateUserDialog } from '../components/CreateUserDialog';
import { DeleteUserDialog } from '../components/DeleteUserDialog';
import { UsersTable } from '../components/UsersTable';
import { useAuthStore } from '@/stores/authStore';
import { describeApiError } from '@/lib/errors/describeApiError';
import type { UserDetail } from '@/lib/types/user';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

export function UsersListPage() {
  const [query, setQuery] = useState('');
  const [createOpen, setCreateOpen] = useState(false);
  const [toDelete, setToDelete] = useState<UserDetail | null>(null);
  const currentUserId = useAuthStore((s) => s.user?.id);
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
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <div>
          <h2 className="text-2xl font-semibold">Users</h2>
          <p className="text-sm text-muted-foreground">
            Manage user accounts, roles, and direct permissions.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Input
            placeholder="Search by email or name…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="w-64"
          />
          <Button onClick={() => setCreateOpen(true)}>+ New user</Button>
        </div>
      </div>

      {isError && (
        <div className="rounded-md border border-destructive/50 bg-destructive/5 p-4">
          <p className="text-sm text-destructive">
            {describeApiError(error, 'Failed to load users.')}
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

      <UsersTable
        users={filtered}
        isLoading={isLoading}
        emptyLabel={query ? 'No users match your search.' : 'No users yet.'}
        currentUserId={currentUserId}
        onDelete={setToDelete}
      />

      {isFetching && !isLoading && (
        <p className="text-xs text-muted-foreground">Refreshing…</p>
      )}

      <CreateUserDialog open={createOpen} onOpenChange={setCreateOpen} />
      <DeleteUserDialog user={toDelete} onClose={() => setToDelete(null)} />
    </div>
  );
}

import { useState } from 'react';
import { useRoles } from '../hooks';
import { CreateRoleDialog } from '../components/CreateRoleDialog';
import { DeleteRoleDialog } from '../components/DeleteRoleDialog';
import { RolesTable } from '../components/RolesTable';
import { isApiProblem } from '@/lib/types/problem';
import type { Role } from '@/lib/types/role';
import { Button } from '@/components/ui/button';

export function RolesListPage() {
  const { data, isLoading, isError, error, refetch } = useRoles();
  const [createOpen, setCreateOpen] = useState(false);
  const [toDelete, setToDelete] = useState<Role | null>(null);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-semibold">Roles</h2>
          <p className="text-sm text-muted-foreground">
            Define roles and attach permissions. Users inherit permissions from
            their roles and may have direct permissions.
          </p>
        </div>
        <Button onClick={() => setCreateOpen(true)}>+ New role</Button>
      </div>

      {isError && (
        <div className="rounded-md border border-destructive/50 bg-destructive/5 p-4">
          <p className="text-sm text-destructive">
            {isApiProblem(error) ? error.title : 'Failed to load roles.'}
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

      <RolesTable roles={data} isLoading={isLoading} onDelete={setToDelete} />

      <CreateRoleDialog open={createOpen} onOpenChange={setCreateOpen} />
      <DeleteRoleDialog role={toDelete} onClose={() => setToDelete(null)} />
    </div>
  );
}

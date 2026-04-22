import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';
import { useCreateRole, useDeleteRole, useRoles } from '../hooks';
import { isApiProblem } from '@/lib/types/problem';
import type { Role } from '@/lib/types/role';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
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
                <TableCell colSpan={3} className="text-center text-muted-foreground py-8">
                  Loading…
                </TableCell>
              </TableRow>
            )}
            {!isLoading && data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={3} className="text-center text-muted-foreground py-8">
                  No roles yet.
                </TableCell>
              </TableRow>
            )}
            {data?.map((role) => (
              <TableRow key={role.id}>
                <TableCell className="font-medium">{role.name}</TableCell>
                <TableCell className="text-sm text-muted-foreground">
                  {role.permissions.length} permission
                  {role.permissions.length === 1 ? '' : 's'}
                </TableCell>
                <TableCell className="text-right space-x-2">
                  <Button asChild variant="outline" size="sm" disabled={isSuperAdmin(role)}>
                    <Link to={`/admin/roles/${role.id}`}>Edit</Link>
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={isSuperAdmin(role)}
                    title={isSuperAdmin(role) ? 'SuperAdmin cannot be deleted' : undefined}
                    onClick={() => setToDelete(role)}
                  >
                    Delete
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <CreateRoleDialog open={createOpen} onOpenChange={setCreateOpen} />
      <DeleteRoleDialog role={toDelete} onClose={() => setToDelete(null)} />
    </div>
  );
}

function CreateRoleDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const [name, setName] = useState('');
  const createMutation = useCreateRole();

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return;
    createMutation.mutate(
      { name: trimmed },
      {
        onSuccess: () => {
          toast.success(`Role "${trimmed}" created.`);
          setName('');
          onOpenChange(false);
        },
        onError: (err) => {
          toast.error(isApiProblem(err) ? err.title : 'Failed to create role.');
        },
      },
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Create role</DialogTitle>
            <DialogDescription>
              Role names should be short and descriptive, e.g. "Moderator".
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2 py-4">
            <Label htmlFor="role-name">Name</Label>
            <Input
              id="role-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              autoFocus
              required
              minLength={1}
            />
          </div>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending ? 'Creating…' : 'Create'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteRoleDialog({
  role,
  onClose,
}: {
  role: Role | null;
  onClose: () => void;
}) {
  const deleteMutation = useDeleteRole();

  const handleConfirm = () => {
    if (!role) return;
    deleteMutation.mutate(role.id, {
      onSuccess: () => {
        toast.success(`Role "${role.name}" deleted.`);
        onClose();
      },
      onError: (err) => {
        toast.error(isApiProblem(err) ? err.title : 'Failed to delete role.');
      },
    });
  };

  return (
    <Dialog open={role !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Delete role?</DialogTitle>
          <DialogDescription>
            {role && (
              <>
                This permanently deletes "<strong>{role.name}</strong>" and
                removes it from all users that have it assigned. This cannot be
                undone.
              </>
            )}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button type="button" variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button
            type="button"
            variant="destructive"
            disabled={deleteMutation.isPending}
            onClick={handleConfirm}
          >
            {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

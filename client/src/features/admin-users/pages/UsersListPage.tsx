import { useMemo, useState, type FormEvent, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { useCreateUser, useDeleteUser, useUsers } from '../hooks';
import { createUserSchema, type CreateUserInput } from '../schemas';
import { UserType, UserTypeLabel } from '@/lib/types/user';
import type { UserDetail } from '@/lib/types/user';
import { isApiProblem } from '@/lib/types/problem';
import { useAuthStore } from '@/stores/authStore';
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
} from '@/components/ui/dialog';
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
              <TableHead className="w-40 text-right">Actions</TableHead>
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
            {filtered.map((user) => {
              const isSelf = user.id === currentUserId;
              return (
                <TableRow key={user.id}>
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
                  <TableCell className="text-sm">
                    {user.directPermissions.length}
                  </TableCell>
                  <TableCell className="text-right space-x-2">
                    <Button asChild variant="outline" size="sm">
                      <Link to={`/admin/users/${user.id}`}>Edit</Link>
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={isSelf}
                      title={isSelf ? 'You cannot delete your own account' : undefined}
                      onClick={() => setToDelete(user)}
                    >
                      Delete
                    </Button>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>

      {isFetching && !isLoading && (
        <p className="text-xs text-muted-foreground">Refreshing…</p>
      )}

      <CreateUserDialog open={createOpen} onOpenChange={setCreateOpen} />
      <DeleteUserDialog user={toDelete} onClose={() => setToDelete(null)} />
    </div>
  );
}

function CreateUserDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const createMutation = useCreateUser();

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<CreateUserInput>({
    resolver: zodResolver(createUserSchema),
    defaultValues: {
      email: '',
      password: '',
      fullName: '',
      userType: UserType.Buyer,
    },
  });

  const onSubmit = (values: CreateUserInput) => {
    createMutation.mutate(values, {
      onSuccess: (user) => {
        toast.success(`Created ${user.email}.`);
        reset();
        onOpenChange(false);
      },
      onError: (err) => {
        toast.error(isApiProblem(err) ? err.title : 'Failed to create user.');
      },
    });
  };

  const handleOpenChange = (next: boolean) => {
    if (!next) reset();
    onOpenChange(next);
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogHeader>
            <DialogTitle>Create user</DialogTitle>
            <DialogDescription>
              Provision a new account. Roles and direct permissions can be
              assigned after the user is created.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3 py-4">
            <Field label="Email" error={errors.email?.message}>
              <Input type="email" autoComplete="off" {...register('email')} />
            </Field>
            <Field label="Full name" error={errors.fullName?.message}>
              <Input autoComplete="off" {...register('fullName')} />
            </Field>
            <Field label="Password" error={errors.password?.message}>
              <Input type="password" autoComplete="new-password" {...register('password')} />
            </Field>
            <Field label="Account type" error={errors.userType?.message}>
              <Controller
                control={control}
                name="userType"
                render={({ field }) => (
                  <select
                    value={field.value}
                    onChange={(e) => field.onChange(Number(e.target.value))}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  >
                    <option value={UserType.Admin}>Admin</option>
                    <option value={UserType.Seller}>Seller</option>
                    <option value={UserType.Buyer}>Buyer</option>
                  </select>
                )}
              />
            </Field>
          </div>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending ? 'Creating…' : 'Create user'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteUserDialog({
  user,
  onClose,
}: {
  user: UserDetail | null;
  onClose: () => void;
}) {
  const deleteMutation = useDeleteUser();

  const handleConfirm = (e: FormEvent) => {
    e.preventDefault();
    if (!user) return;
    deleteMutation.mutate(user.id, {
      onSuccess: () => {
        toast.success(`Deleted ${user.email}.`);
        onClose();
      },
      onError: (err) => {
        toast.error(isApiProblem(err) ? err.title : 'Failed to delete user.');
      },
    });
  };

  return (
    <Dialog open={user !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <form onSubmit={handleConfirm}>
          <DialogHeader>
            <DialogTitle>Delete user?</DialogTitle>
            <DialogDescription>
              {user && (
                <>
                  This permanently removes "<strong>{user.email}</strong>",
                  along with their role assignments and direct permissions.
                  This cannot be undone.
                </>
              )}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button
              type="submit"
              variant="destructive"
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: ReactNode;
}) {
  return (
    <div className="space-y-2">
      <Label>{label}</Label>
      {children}
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
}

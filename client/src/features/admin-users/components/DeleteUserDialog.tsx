import type { FormEvent } from 'react';
import { toast } from 'sonner';
import { useDeleteUser } from '../hooks';
import { describeApiError } from '@/lib/errors/describeApiError';
import type { UserDetail } from '@/lib/types/user';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

interface Props {
  user: UserDetail | null;
  onClose: () => void;
}

export function DeleteUserDialog({ user, onClose }: Props) {
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
        toast.error(describeApiError(err, 'Failed to delete user.', 'user'));
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

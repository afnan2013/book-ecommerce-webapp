import { toast } from 'sonner';
import { useDeleteRole } from '../hooks';
import { describeApiError } from '@/lib/errors/describeApiError';
import type { Role } from '@/lib/types/role';
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
  role: Role | null;
  onClose: () => void;
}

export function DeleteRoleDialog({ role, onClose }: Props) {
  const deleteMutation = useDeleteRole();

  const handleConfirm = () => {
    if (!role) return;
    deleteMutation.mutate(role.id, {
      onSuccess: () => {
        toast.success(`Role "${role.name}" deleted.`);
        onClose();
      },
      onError: (err) => {
        toast.error(describeApiError(err, 'Failed to delete role.', 'role'));
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

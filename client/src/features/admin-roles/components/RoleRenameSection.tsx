import { useState, type FormEvent } from 'react';
import { toast } from 'sonner';
import { useQueryClient } from '@tanstack/react-query';
import { useUpdateRole } from '../hooks';
import { handleMutationError } from '@/lib/handleMutationError';
import { queryKeys } from '@/lib/queryKeys';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface Props {
  roleId: number;
  initialName: string;
  concurrencyStamp: string;
}

export function RoleRenameSection({
  roleId,
  initialName,
  concurrencyStamp,
}: Props) {
  const [name, setName] = useState(initialName);
  const updateMutation = useUpdateRole(roleId);
  const queryClient = useQueryClient();

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const trimmed = name.trim();
    if (!trimmed || trimmed === initialName) return;
    updateMutation.mutate(
      { name: trimmed, concurrencyStamp },
      {
        onSuccess: () => toast.success('Role renamed.'),
        onError: (err) =>
          handleMutationError(err, {
            queryClient,
            invalidateKey: queryKeys.roles.byId(roleId),
            entityLabel: 'role',
          }),
      },
    );
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Rename</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="flex items-end gap-3 max-w-md">
          <div className="flex-1 space-y-2">
            <Label htmlFor="role-name">Role name</Label>
            <Input
              id="role-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              minLength={1}
            />
          </div>
          <Button
            type="submit"
            disabled={
              updateMutation.isPending ||
              name.trim() === '' ||
              name.trim() === initialName
            }
          >
            {updateMutation.isPending ? 'Saving…' : 'Save'}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

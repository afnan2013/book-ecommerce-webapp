import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { useCreateUser } from '../hooks';
import { createUserSchema, type CreateUserInput } from '../schemas';
import { UserType } from '@/lib/types/user';
import { describeApiError } from '@/lib/errors/describeApiError';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { FormField } from '@/components/FormField';
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CreateUserDialog({ open, onOpenChange }: Props) {
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
        toast.error(describeApiError(err, 'Failed to create user.', 'user'));
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
            <FormField label="Email" error={errors.email?.message}>
              <Input type="email" autoComplete="off" {...register('email')} />
            </FormField>
            <FormField label="Full name" error={errors.fullName?.message}>
              <Input autoComplete="off" {...register('fullName')} />
            </FormField>
            <FormField label="Password" error={errors.password?.message}>
              <Input
                type="password"
                autoComplete="new-password"
                {...register('password')}
              />
            </FormField>
            <FormField label="Account type" error={errors.userType?.message}>
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
            </FormField>
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

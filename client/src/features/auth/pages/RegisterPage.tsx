import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, Navigate } from 'react-router-dom';
import { toast } from 'sonner';
import { registerSchema, type RegisterInput } from '../schemas';
import { useRegister } from '../hooks';
import { selectIsAuthenticated, useAuthStore } from '@/stores/authStore';
import { UserType } from '@/lib/types/user';
import { isApiProblem } from '@/lib/types/problem';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { FormField } from '@/components/FormField';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';

export function RegisterPage() {
  const isAuthed = useAuthStore(selectIsAuthenticated);
  const registerMutation = useRegister();

  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<RegisterInput>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      email: '',
      password: '',
      fullName: '',
      userType: UserType.Buyer,
    },
  });

  if (isAuthed) return <Navigate to="/" replace />;

  const onSubmit = (values: RegisterInput) => {
    registerMutation.mutate(values, {
      onError: (err) => {
        toast.error(isApiProblem(err) ? err.title : 'Registration failed');
      },
    });
  };

  return (
    <Card className="w-full max-w-sm">
      <CardHeader>
        <CardTitle>Create an account</CardTitle>
        <CardDescription>
          Buyers and sellers only — admins are provisioned.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <FormField label="Full name" error={errors.fullName?.message}>
            <Input autoComplete="name" {...register('fullName')} />
          </FormField>
          <FormField label="Email" error={errors.email?.message}>
            <Input type="email" autoComplete="email" {...register('email')} />
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
                  <option value={UserType.Buyer}>Buyer</option>
                  <option value={UserType.Seller}>Seller</option>
                </select>
              )}
            />
          </FormField>
          <Button
            type="submit"
            className="w-full"
            disabled={registerMutation.isPending}
          >
            {registerMutation.isPending ? 'Creating…' : 'Create account'}
          </Button>
          <p className="text-center text-sm text-muted-foreground">
            Already have an account?{' '}
            <Link to="/login" className="underline">
              Sign in
            </Link>
          </p>
        </form>
      </CardContent>
    </Card>
  );
}

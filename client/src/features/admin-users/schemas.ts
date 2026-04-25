import { z } from 'zod';
import { UserType } from '@/lib/types/user';

export const createUserSchema = z.object({
  email: z.email('Enter a valid email'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  fullName: z.string().min(1, 'Full name is required'),
  userType: z.union([
    z.literal(UserType.Employee),
    z.literal(UserType.Seller),
    z.literal(UserType.Buyer),
  ]),
});

export type CreateUserInput = z.infer<typeof createUserSchema>;

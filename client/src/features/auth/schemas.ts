import { z } from 'zod';
import { UserType } from '@/lib/types/user';

export const loginSchema = z.object({
  email: z.string().email('Enter a valid email'),
  password: z.string().min(1, 'Password is required'),
});

export type LoginInput = z.infer<typeof loginSchema>;

export const registerSchema = z.object({
  email: z.string().email('Enter a valid email'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  fullName: z.string().min(1, 'Full name is required'),
  userType: z.union([z.literal(UserType.Seller), z.literal(UserType.Buyer)]),
});

export type RegisterInput = z.infer<typeof registerSchema>;

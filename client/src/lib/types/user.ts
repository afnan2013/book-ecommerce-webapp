import type { Permission } from './permission';

export const UserType = {
  Employee: 1,
  Seller: 2,
  Buyer: 3,
} as const;

export type UserType = (typeof UserType)[keyof typeof UserType];

export const UserTypeLabel: Record<UserType, string> = {
  [UserType.Employee]: 'Employee',
  [UserType.Seller]: 'Seller',
  [UserType.Buyer]: 'Buyer',
};

export interface User {
  id: number;
  email: string;
  fullName: string;
  userType: UserType;
}

export interface UserRole {
  id: number;
  name: string;
}

export interface UserDetail extends User {
  concurrencyStamp: string;
  roles: UserRole[];
  directPermissions: Permission[];
}

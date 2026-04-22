import type { Permission } from './permission';

export interface Role {
  id: number;
  name: string;
  concurrencyStamp: string;
  permissions: Permission[];
}

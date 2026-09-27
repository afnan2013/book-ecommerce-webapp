import type { Permission } from './permission';

export interface Role {
  id: string;
  name: string;
  concurrencyStamp: string;
  permissions: Permission[];
}

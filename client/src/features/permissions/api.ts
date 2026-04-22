import { apiClient } from '@/lib/apiClient';
import type { Permission } from '@/lib/types/permission';

export async function listPermissions(): Promise<Permission[]> {
  const { data } = await apiClient.get<Permission[]>('/permissions');
  return data;
}

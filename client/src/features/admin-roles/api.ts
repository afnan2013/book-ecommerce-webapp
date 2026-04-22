import { apiClient } from '@/lib/apiClient';
import type { Role } from '@/lib/types/role';

export async function listRoles(): Promise<Role[]> {
  const { data } = await apiClient.get<Role[]>('/roles');
  return data;
}

export async function getRole(id: number): Promise<Role> {
  const { data } = await apiClient.get<Role>(`/roles/${id}`);
  return data;
}

export interface CreateRoleRequest {
  name: string;
}

export async function createRole(req: CreateRoleRequest): Promise<Role> {
  const { data } = await apiClient.post<Role>('/roles', req);
  return data;
}

export interface UpdateRoleRequest {
  name: string;
  concurrencyStamp: string;
}

export async function updateRole(
  id: number,
  req: UpdateRoleRequest,
): Promise<void> {
  await apiClient.put(`/roles/${id}`, req);
}

export async function deleteRole(id: number): Promise<void> {
  await apiClient.delete(`/roles/${id}`);
}

export interface SetRolePermissionsRequest {
  permissionIds: number[];
  concurrencyStamp: string;
}

export async function setRolePermissions(
  id: number,
  req: SetRolePermissionsRequest,
): Promise<Role> {
  const { data } = await apiClient.put<Role>(`/roles/${id}/permissions`, req);
  return data;
}

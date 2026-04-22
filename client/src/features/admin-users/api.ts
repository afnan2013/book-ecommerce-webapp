import { apiClient } from '@/lib/apiClient';
import type { UserDetail, UserType } from '@/lib/types/user';

export async function listUsers(): Promise<UserDetail[]> {
  const { data } = await apiClient.get<UserDetail[]>('/users');
  return data;
}

export async function getUser(id: number): Promise<UserDetail> {
  const { data } = await apiClient.get<UserDetail>(`/users/${id}`);
  return data;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  fullName: string;
  userType: UserType;
}

export async function createUser(req: CreateUserRequest): Promise<UserDetail> {
  const { data } = await apiClient.post<UserDetail>('/users', req);
  return data;
}

export async function deleteUser(id: number): Promise<void> {
  await apiClient.delete(`/users/${id}`);
}

export interface UpdateUserRolesRequest {
  roleIds: number[];
  concurrencyStamp: string;
}

export async function updateUserRoles(
  id: number,
  req: UpdateUserRolesRequest,
): Promise<UserDetail> {
  const { data } = await apiClient.put<UserDetail>(`/users/${id}/roles`, req);
  return data;
}

export interface UpdateUserPermissionsRequest {
  permissionIds: number[];
  concurrencyStamp: string;
}

export async function updateUserPermissions(
  id: number,
  req: UpdateUserPermissionsRequest,
): Promise<UserDetail> {
  const { data } = await apiClient.put<UserDetail>(
    `/users/${id}/permissions`,
    req,
  );
  return data;
}

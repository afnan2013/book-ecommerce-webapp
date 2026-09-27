import { apiClient } from '@/lib/api/client';
import type { User, UserType } from '@/lib/types/user';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  userType: UserType;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}

export interface RegisterResponse {
  id: string;
  email: string;
  fullName: string;
  userType: UserType;
}

export async function login(req: LoginRequest): Promise<LoginResponse> {
  const { data } = await apiClient.post<LoginResponse>('/auth/login', req);
  return data;
}

export async function register(req: RegisterRequest): Promise<RegisterResponse> {
  const { data } = await apiClient.post<RegisterResponse>('/auth/register', req);
  return data;
}

export async function fetchMe(): Promise<User> {
  const { data } = await apiClient.get<User>('/users/me');
  return data;
}

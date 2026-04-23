import axios, { AxiosError } from 'axios';
import { useAuthStore } from '@/stores/authStore';
import { ApiError, type ApiErrorDetails } from './ApiError';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token;
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiErrorDetails>) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().clear();
    }
    const status = error.response?.status;
    const data = error.response?.data;
    if (status && data && typeof data === 'object') {
      return Promise.reject(new ApiError(status, data));
    }
    return Promise.reject(error);
  },
);

import axios from 'axios';
import type {
  ClassInfo,
  CreateClassRequest,
  CreateClassResponse,
  HoldResponse,
  ConfirmResponse,
  MyHoldResponse,
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Classes
export const createClass = async (data: CreateClassRequest): Promise<CreateClassResponse> => {
  const response = await api.post<CreateClassResponse>('/classes', data);
  return response.data;
};

export const getClasses = async (): Promise<ClassInfo[]> => {
  const response = await api.get<ClassInfo[]>('/classes');
  return response.data;
};

export const getClass = async (classId: string): Promise<ClassInfo> => {
  const response = await api.get<ClassInfo>(`/classes/${classId}`);
  return response.data;
};

// Holds
export const createHold = async (classId: string, studentId: string): Promise<HoldResponse> => {
  const response = await api.post<HoldResponse>(`/classes/${classId}/holds`, null, {
    headers: { 'X-Student-Id': studentId },
  });
  return response.data;
};

export const getHold = async (holdId: string): Promise<HoldResponse> => {
  const response = await api.get<HoldResponse>(`/holds/${holdId}`);
  return response.data;
};

export const confirmHold = async (holdId: string, studentId: string): Promise<ConfirmResponse> => {
  const response = await api.post<ConfirmResponse>(`/holds/${holdId}/confirm`, null, {
    headers: { 'X-Student-Id': studentId },
  });
  return response.data;
};

export const cancelHold = async (holdId: string, studentId: string): Promise<void> => {
  await api.delete(`/holds/${holdId}`, {
    headers: { 'X-Student-Id': studentId },
  });
};

export const getMyHolds = async (studentId: string): Promise<MyHoldResponse[]> => {
  const response = await api.get<MyHoldResponse[]>('/me/holds', {
    headers: { 'X-Student-Id': studentId },
  });
  return response.data;
};

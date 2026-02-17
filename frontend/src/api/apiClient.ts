import type {
  User,
  Task,
  TaskTypeInfo,
  CreateTaskRequest,
  ChangeStatusRequest,
} from '../types';
import { ApiError } from '../types';

const API_BASE = import.meta.env.VITE_API_BASE_URL || '/api';

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let errorMessage = 'An error occurred';
    let errors: string[] | undefined;

    try {
      const contentType = response.headers.get('content-type');
      if (contentType?.includes('application/json') || contentType?.includes('application/problem+json')) {
        const problem = await response.json();
        errorMessage = problem.detail || problem.title || errorMessage;
        errors = problem.errors;
      } else {
        errorMessage = await response.text() || errorMessage;
      }
    } catch {
      // If parsing fails, use default message
    }

    throw new ApiError(errorMessage, response.status, errors);
  }

  return response.json();
}

export const apiClient = {
  // Users
  getUsers: async (): Promise<User[]> => {
    const response = await fetch(`${API_BASE}/users`);
    return handleResponse<User[]>(response);
  },

  getUserTasks: async (userId: number): Promise<Task[]> => {
    const response = await fetch(`${API_BASE}/users/${userId}/tasks`);
    return handleResponse<Task[]>(response);
  },

  // Task types
  getTaskTypes: async (): Promise<TaskTypeInfo[]> => {
    const response = await fetch(`${API_BASE}/task-types`);
    return handleResponse<TaskTypeInfo[]>(response);
  },

  // Task operations
  createTask: async (request: CreateTaskRequest): Promise<Task> => {
    const response = await fetch(`${API_BASE}/tasks`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    return handleResponse<Task>(response);
  },

  changeStatus: async (taskId: number, request: ChangeStatusRequest): Promise<Task> => {
    const response = await fetch(`${API_BASE}/tasks/${taskId}/status`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    return handleResponse<Task>(response);
  },

  closeTask: async (taskId: number): Promise<Task> => {
    const response = await fetch(`${API_BASE}/tasks/${taskId}/close`, {
      method: 'PUT',
    });
    return handleResponse<Task>(response);
  },
};

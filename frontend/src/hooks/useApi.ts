import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../api/apiClient';
import type { CreateTaskRequest, ChangeStatusRequest } from '../types';

// Query: Get all users
export function useUsers() {
  return useQuery({
    queryKey: ['users'],
    queryFn: apiClient.getUsers,
    staleTime: 5 * 60 * 1000, // Users rarely change, cache for 5 minutes
  });
}

// Query: Get task type configurations
export function useTaskTypes() {
  return useQuery({
    queryKey: ['taskTypes'],
    queryFn: apiClient.getTaskTypes,
    staleTime: 5 * 60 * 1000, // Task types rarely change, cache for 5 minutes
  });
}

// Query: Get tasks for a specific user
export function useUserTasks(userId: number | null) {
  return useQuery({
    queryKey: ['tasks', userId],
    queryFn: () => apiClient.getUserTasks(userId!),
    enabled: userId !== null, // Only run query if userId is selected
  });
}

// Mutation: Create a new task
export function useCreateTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateTaskRequest) => apiClient.createTask(request),
    onSuccess: () => {
      // Invalidate tasks query to refetch
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
    },
  });
}

// Mutation: Change task status
export function useChangeStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ taskId, request }: { taskId: number; request: ChangeStatusRequest }) =>
      apiClient.changeStatus(taskId, request),
    onSuccess: () => {
      // Invalidate tasks query to refetch
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
    },
  });
}

// Mutation: Close a task
export function useCloseTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (taskId: number) => apiClient.closeTask(taskId),
    onSuccess: () => {
      // Invalidate tasks query to refetch
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
    },
  });
}

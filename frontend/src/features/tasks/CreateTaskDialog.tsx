import { useState } from 'react';
import type { FormEvent } from 'react';
import { useUsers, useTaskTypes, useCreateTask } from '../../hooks/useApi';
import { ApiError } from '../../types';

interface CreateTaskDialogProps {
  onClose: () => void;
}

export function CreateTaskDialog({ onClose }: CreateTaskDialogProps) {
  const [taskType, setTaskType] = useState('');
  const [title, setTitle] = useState('');
  const [assignedUserId, setAssignedUserId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const { data: users } = useUsers();
  const { data: taskTypes } = useTaskTypes();
  const createTaskMutation = useCreateTask();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!taskType || !title.trim() || !assignedUserId) {
      setError('Please fill in all fields');
      return;
    }

    try {
      await createTaskMutation.mutateAsync({
        taskType,
        title: title.trim(),
        assignedUserId,
      });
      onClose();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('An error occurred while creating the task');
      }
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 className="text-xl font-bold text-gray-900 mb-4">Create New Task</h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Task Type */}
          <div>
            <label htmlFor="taskType" className="block text-sm font-medium text-gray-700 mb-1">
              Task Type
            </label>
            <select
              id="taskType"
              value={taskType}
              onChange={(e) => setTaskType(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              required
            >
              <option value="">Select a task type...</option>
              {taskTypes?.map((tt) => (
                <option key={tt.taskType} value={tt.taskType}>
                  {tt.taskType}
                </option>
              ))}
            </select>
          </div>

          {/* Title */}
          <div>
            <label htmlFor="title" className="block text-sm font-medium text-gray-700 mb-1">
              Title
            </label>
            <input
              id="title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Enter task title"
              required
            />
          </div>

          {/* Assigned User */}
          <div>
            <label htmlFor="assignedUser" className="block text-sm font-medium text-gray-700 mb-1">
              Assign To
            </label>
            <select
              id="assignedUser"
              value={assignedUserId ?? ''}
              onChange={(e) => setAssignedUserId(Number(e.target.value))}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              required
            >
              <option value="">Select a user...</option>
              {users?.map((user) => (
                <option key={user.id} value={user.id}>
                  {user.name}
                </option>
              ))}
            </select>
          </div>

          {/* Error Message */}
          {error && (
            <div className="p-3 bg-red-50 border border-red-200 rounded text-sm text-red-700">
              {error}
            </div>
          )}

          {/* Buttons */}
          <div className="flex gap-3 justify-end pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500 focus:ring-offset-2"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createTaskMutation.isPending}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50"
            >
              {createTaskMutation.isPending ? 'Creating...' : 'Create Task'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

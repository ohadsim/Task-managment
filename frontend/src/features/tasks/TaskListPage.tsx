import { useState } from 'react';
import { useUserStore } from '../../stores/userStore';
import { useUserTasks } from '../../hooks/useApi';
import { TaskCard } from './TaskCard';
import { CreateTaskDialog } from './CreateTaskDialog';

export function TaskListPage() {
  const { currentUserId } = useUserStore();
  const { data: tasks, isLoading, error } = useUserTasks(currentUserId);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);

  if (!currentUserId) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-gray-500 text-lg">
          Select a user to view tasks
        </p>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-gray-500">Loading tasks...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-red-600">Error loading tasks: {error.message}</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header with Create Button */}
      <div className="flex justify-between items-center">
        <h2 className="text-xl font-semibold text-gray-900">
          Tasks {tasks && `(${tasks.length})`}
        </h2>
        <button
          onClick={() => setIsCreateDialogOpen(true)}
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
        >
          Create Task
        </button>
      </div>

      {/* Task List */}
      {!tasks || tasks.length === 0 ? (
        <div className="flex items-center justify-center h-64 border-2 border-dashed border-gray-300 rounded-lg">
          <div className="text-center">
            <p className="text-gray-500 mb-4">No tasks found for this user</p>
            <button
              onClick={() => setIsCreateDialogOpen(true)}
              className="text-blue-600 hover:text-blue-700 font-medium"
            >
              Create your first task
            </button>
          </div>
        </div>
      ) : (
        <div className="grid gap-4">
          {tasks.map((task) => (
            <TaskCard key={task.id} task={task} />
          ))}
        </div>
      )}

      {/* Create Task Dialog */}
      {isCreateDialogOpen && (
        <CreateTaskDialog onClose={() => setIsCreateDialogOpen(false)} />
      )}
    </div>
  );
}

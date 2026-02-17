import { useState } from 'react';
import type { Task } from '../../types';
import { StatusHistory } from './StatusHistory';
import { ChangeStatusDialog } from './ChangeStatusDialog';
import { useCloseTask, useTaskTypes } from '../../hooks/useApi';

interface TaskCardProps {
  task: Task;
}

export function TaskCard({ task }: TaskCardProps) {
  const [isChangeStatusOpen, setIsChangeStatusOpen] = useState(false);
  const [statusChangeDirection, setStatusChangeDirection] = useState<'forward' | 'backward'>('forward');
  const closeTaskMutation = useCloseTask();
  const { data: taskTypes } = useTaskTypes();

  // Find the task type configuration
  const taskTypeConfig = taskTypes?.find((tt) => tt.taskType === task.taskType);
  const isAtFinalStatus = taskTypeConfig && task.currentStatus === taskTypeConfig.maxStatus;

  const handleAdvance = () => {
    setStatusChangeDirection('forward');
    setIsChangeStatusOpen(true);
  };

  const handleMoveBack = () => {
    setStatusChangeDirection('backward');
    setIsChangeStatusOpen(true);
  };

  const handleClose = async () => {
    if (window.confirm('Are you sure you want to close this task?')) {
      await closeTaskMutation.mutateAsync(task.id);
    }
  };

  return (
    <>
      <div className="border border-gray-200 rounded-lg p-4 bg-white shadow-sm">
        {/* Header */}
        <div className="flex justify-between items-start mb-3">
          <div className="flex-1">
            <div className="flex items-center gap-2 mb-1">
              <h3 className="font-semibold text-lg text-gray-900">{task.title}</h3>
              <span className="px-2 py-0.5 text-xs font-medium rounded bg-purple-100 text-purple-700">
                {task.taskType}
              </span>
            </div>
            <p className="text-sm text-gray-600">
              Status: {task.currentStatusLabel} ({task.currentStatus})
            </p>
          </div>
          <span
            className={`px-3 py-1 text-xs font-semibold rounded ${
              task.isClosed
                ? 'bg-gray-200 text-gray-700'
                : 'bg-green-100 text-green-700'
            }`}
          >
            {task.isClosed ? 'Closed' : 'Open'}
          </span>
        </div>

        {/* Assigned User */}
        <div className="mb-3">
          <p className="text-sm text-gray-700">
            <span className="font-medium">Assigned to:</span> {task.assignedUserName}
          </p>
        </div>

        {/* Custom Data */}
        {Object.keys(task.customData).length > 0 && (
          <div className="mb-3 p-3 bg-gray-50 rounded border border-gray-200">
            <h4 className="text-sm font-medium text-gray-700 mb-2">Custom Data</h4>
            <div className="space-y-1">
              {Object.entries(task.customData).map(([key, value]) => (
                <div key={key} className="text-sm">
                  <span className="font-medium text-gray-700">{key}:</span>{' '}
                  <span className="text-gray-600">
                    {typeof value === 'string' || typeof value === 'number'
                      ? value
                      : JSON.stringify(value)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Action Buttons */}
        {!task.isClosed && taskTypeConfig && (
          <div className="flex gap-2 mb-3">
            {/* Advance button - only if not at final status */}
            {!isAtFinalStatus && (
              <button
                onClick={handleAdvance}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
              >
                Advance
              </button>
            )}

            {/* Move Back button - only if status > 1 */}
            {task.currentStatus > 1 && (
              <button
                onClick={handleMoveBack}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-200 rounded hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-gray-500 focus:ring-offset-2"
              >
                Move Back
              </button>
            )}

            {/* Close button - only if at final status */}
            {isAtFinalStatus && (
              <button
                onClick={handleClose}
                disabled={closeTaskMutation.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 disabled:opacity-50"
              >
                {closeTaskMutation.isPending ? 'Closing...' : 'Close'}
              </button>
            )}
          </div>
        )}

        {/* Status History */}
        <StatusHistory history={task.statusHistory} />
      </div>

      {/* Change Status Dialog */}
      {isChangeStatusOpen && taskTypeConfig && (
        <ChangeStatusDialog
          task={task}
          taskTypeConfig={taskTypeConfig}
          direction={statusChangeDirection}
          onClose={() => setIsChangeStatusOpen(false)}
        />
      )}
    </>
  );
}

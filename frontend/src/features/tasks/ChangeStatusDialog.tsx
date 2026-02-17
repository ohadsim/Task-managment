import { useState } from 'react';
import type { FormEvent } from 'react';
import type { Task, TaskTypeInfo } from '../../types';
import { useUsers, useChangeStatus } from '../../hooks/useApi';
import { ApiError } from '../../types';

interface ChangeStatusDialogProps {
  task: Task;
  taskTypeConfig: TaskTypeInfo;
  direction: 'forward' | 'backward';
  onClose: () => void;
}

export function ChangeStatusDialog({
  task,
  taskTypeConfig,
  direction,
  onClose,
}: ChangeStatusDialogProps) {
  // For forward: target is currentStatus + 1
  // For backward: user selects from dropdown
  const [targetStatus, setTargetStatus] = useState<number>(
    direction === 'forward' ? task.currentStatus + 1 : 1
  );
  const [assignedUserId, setAssignedUserId] = useState<number | null>(null);
  const [customData, setCustomData] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);

  const { data: users } = useUsers();
  const changeStatusMutation = useChangeStatus();

  // Get field definitions for the target status (only for forward moves)
  const fieldDefinitions =
    direction === 'forward'
      ? taskTypeConfig.fieldsByStatus[targetStatus] || []
      : [];

  // Get the target status label
  const targetStatusLabel =
    taskTypeConfig.statuses.find((s) => s.status === targetStatus)?.label || `Status ${targetStatus}`;

  // Generate backward status options (1 to currentStatus - 1)
  const backwardStatusOptions =
    direction === 'backward'
      ? Array.from({ length: task.currentStatus - 1 }, (_, i) => i + 1)
      : [];

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!assignedUserId) {
      setError('Please select a user to assign');
      return;
    }

    // Validate required fields for forward moves
    if (direction === 'forward') {
      for (const field of fieldDefinitions) {
        if (field.required && !customData[field.fieldName]?.trim()) {
          setError(`${field.label} is required`);
          return;
        }
      }
    }

    try {
      await changeStatusMutation.mutateAsync({
        taskId: task.id,
        request: {
          targetStatus,
          assignedUserId,
          customData: direction === 'forward' ? customData : {},
        },
      });
      onClose();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('An error occurred while changing status');
      }
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6 max-h-[90vh] overflow-y-auto">
        <h2 className="text-xl font-bold text-gray-900 mb-2">Change Task Status</h2>
        <p className="text-sm text-gray-600 mb-4">
          {direction === 'forward' ? (
            <>
              <span className="font-medium">Advancing to:</span> {targetStatusLabel} ({targetStatus})
            </>
          ) : (
            <>
              <span className="font-medium">Moving backward</span>
            </>
          )}
        </p>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Backward: Status Selection Dropdown */}
          {direction === 'backward' && (
            <div>
              <label htmlFor="targetStatus" className="block text-sm font-medium text-gray-700 mb-1">
                Select Target Status
              </label>
              <select
                id="targetStatus"
                value={targetStatus}
                onChange={(e) => setTargetStatus(Number(e.target.value))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              >
                {backwardStatusOptions.map((status) => {
                  const statusDef = taskTypeConfig.statuses.find((s) => s.status === status);
                  return (
                    <option key={status} value={status}>
                      {statusDef?.label || `Status ${status}`} ({status})
                    </option>
                  );
                })}
              </select>
            </div>
          )}

          {/* Forward: Dynamic Custom Fields */}
          {direction === 'forward' && fieldDefinitions.length > 0 && (
            <div className="space-y-3">
              <h3 className="text-sm font-semibold text-gray-700 border-b border-gray-200 pb-2">
                Required Information
              </h3>
              {fieldDefinitions.map((field) => (
                <div key={field.fieldName}>
                  <label
                    htmlFor={field.fieldName}
                    className="block text-sm font-medium text-gray-700 mb-1"
                  >
                    {field.label}
                    {field.required && <span className="text-red-500 ml-1">*</span>}
                  </label>
                  <input
                    id={field.fieldName}
                    type="text"
                    value={customData[field.fieldName] || ''}
                    onChange={(e) =>
                      setCustomData((prev) => ({
                        ...prev,
                        [field.fieldName]: e.target.value,
                      }))
                    }
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder={`Enter ${field.label.toLowerCase()}`}
                    required={field.required}
                  />
                </div>
              ))}
            </div>
          )}

          {/* Assigned User */}
          <div>
            <label htmlFor="assignedUser" className="block text-sm font-medium text-gray-700 mb-1">
              Assign To
              <span className="text-red-500 ml-1">*</span>
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
              disabled={changeStatusMutation.isPending}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50"
            >
              {changeStatusMutation.isPending ? 'Updating...' : 'Update Status'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

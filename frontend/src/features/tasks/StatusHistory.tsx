import { useState } from 'react';
import type { StatusHistoryEntry } from '../../types';

interface StatusHistoryProps {
  history: StatusHistoryEntry[];
}

export function StatusHistory({ history }: StatusHistoryProps) {
  const [isExpanded, setIsExpanded] = useState(false);

  if (history.length === 0) {
    return null;
  }

  return (
    <div className="mt-4 border-t border-gray-200 pt-4">
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="flex items-center gap-2 text-sm font-medium text-gray-700 hover:text-gray-900"
      >
        <span>{isExpanded ? '▼' : '▶'}</span>
        <span>Status History ({history.length})</span>
      </button>

      {isExpanded && (
        <div className="mt-3 space-y-2">
          {history.map((entry, index) => (
            <div
              key={index}
              className="text-sm bg-gray-50 rounded p-3 border border-gray-200"
            >
              <div className="font-medium text-gray-900">
                Status {entry.fromStatus} → {entry.toStatus}
              </div>
              <div className="text-gray-600 mt-1">
                Assigned to: {entry.assignedUserName}
              </div>
              <div className="text-gray-500 text-xs mt-1">
                {new Date(entry.changedAt).toLocaleString()}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

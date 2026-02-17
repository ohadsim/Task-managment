import { useUsers } from '../../hooks/useApi';
import { useUserStore } from '../../stores/userStore';

export function UserSelector() {
  const { data: users, isLoading, error } = useUsers();
  const { currentUserId, setCurrentUserId } = useUserStore();

  if (isLoading) {
    return (
      <div className="flex items-center gap-2">
        <label className="text-sm font-medium">Current User:</label>
        <span className="text-sm text-gray-500">Loading users...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center gap-2">
        <label className="text-sm font-medium">Current User:</label>
        <span className="text-sm text-red-600">Error loading users</span>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-2">
      <label htmlFor="user-select" className="text-sm font-medium">
        Current User:
      </label>
      <select
        id="user-select"
        value={currentUserId || ''}
        onChange={(e) => setCurrentUserId(Number(e.target.value))}
        className="px-3 py-1.5 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option value="">Select a user...</option>
        {users?.map((user) => (
          <option key={user.id} value={user.id}>
            {user.name}
          </option>
        ))}
      </select>
    </div>
  );
}

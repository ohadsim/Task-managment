// User types
export interface User {
  id: number;
  name: string;
  email: string;
}

// Task types
export interface Task {
  id: number;
  taskType: string;
  title: string;
  currentStatus: number;
  currentStatusLabel: string;
  isClosed: boolean;
  assignedUserId: number;
  assignedUserName: string;
  customData: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
  statusHistory: StatusHistoryEntry[];
}

export interface StatusHistoryEntry {
  fromStatus: number;
  toStatus: number;
  assignedUserId: number;
  assignedUserName: string;
  changedAt: string;
}

// Task type configuration
export interface TaskTypeInfo {
  taskType: string;
  maxStatus: number;
  statuses: StatusDefinition[];
  fieldsByStatus: Record<number, FieldDefinition[]>;
}

export interface StatusDefinition {
  status: number;
  label: string;
}

export interface FieldDefinition {
  fieldName: string;
  label: string;
  fieldType: string;
  required: boolean;
  arrayLength: number | null;
}

// Request types
export interface CreateTaskRequest {
  taskType: string;
  title: string;
  assignedUserId: number;
}

export interface ChangeStatusRequest {
  targetStatus: number;
  assignedUserId: number;
  customData: Record<string, unknown>;
}

// Error type
export class ApiError extends Error {
  status: number;
  errors?: string[];

  constructor(message: string, status: number, errors?: string[]) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
  }
}

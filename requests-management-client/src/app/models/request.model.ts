
export enum RequestStatus {
  New = 0,
  InProgress = 1,
  Waiting = 2,
  Completed = 3
}

export enum RequestPriority {
  Low = 0,
  Medium = 1,
  High = 2
}

export interface RequestItem {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface RequestFilter {
  search?: string;
  status?: RequestStatus;
  priority?: RequestPriority;
  organizationName?: string;
  assignedTo?: string;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
  pageNumber: number;
  pageSize: number;
}

export interface StatusHistory {
  id: number;
  requestId: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  changedAt: string;
  changedBy: string;
}

export interface StatisticsGroup {
  status?: string;
  priority?: string;
  count: number;
}

export interface RequestStatistics {
  totalRequests: number;
  byStatus: StatisticsGroup[];
  byPriority: StatisticsGroup[];
}

export interface BulkUpdateItem {
  id: number;
  newStatus: RequestStatus;
  rowVersion: string;
}

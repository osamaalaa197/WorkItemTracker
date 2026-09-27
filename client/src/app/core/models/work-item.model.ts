export type WorkItemStatus = 'Todo' | 'InProgress' | 'Done';

export const WORK_ITEM_STATUSES: WorkItemStatus[] = [
  'Todo',
  'InProgress',
  'Done',
];

export const NEXT_STATUS: Record<WorkItemStatus, WorkItemStatus | null> = {
  Todo: 'InProgress',
  InProgress: 'Done',
  Done: null,
};

export const STATUS_LABELS: Record<WorkItemStatus, string> = {
  Todo: 'To do',
  InProgress: 'In progress',
  Done: 'Done',
};

export interface WorkItem {
  id: number;
  title: string;
  description: string | null;
  status: WorkItemStatus;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateWorkItemRequest {
  title: string;
  description: string | null;
}

export interface WorkItemQuery {
  search?: string;
  status?: WorkItemStatus | '';
  page: number;
  pageSize: number;
}

/** Shape of the ProblemDetails-style JSON the API's exception middleware returns. */
export interface ApiProblem {
  title?: string;
  detail?: string;
  status?: number;
}

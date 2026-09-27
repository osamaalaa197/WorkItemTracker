import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateWorkItemRequest,
  PagedResult,
  WorkItem,
  WorkItemQuery,
  WorkItemStatus,
} from '../models/work-item.model';

@Injectable({ providedIn: 'root' })
export class WorkItemService {
  private readonly baseUrl = `${environment.apiBaseUrl}/work-items`;

  constructor(private readonly http: HttpClient) {}

  create(request: CreateWorkItemRequest): Observable<WorkItem> {
    return this.http.post<WorkItem>(this.baseUrl, request);
  }

  search(query: WorkItemQuery): Observable<PagedResult<WorkItem>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.status) {
      params = params.set('status', query.status);
    }

    return this.http.get<PagedResult<WorkItem>>(this.baseUrl, { params });
  }

  changeStatus(id: number, status: WorkItemStatus): Observable<WorkItem> {
    return this.http.patch<WorkItem>(`${this.baseUrl}/${id}/status`, { status });
  }
}


import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  BulkUpdateItem,
  PagedResult,
  RequestFilter,
  RequestItem,
  RequestStatistics,
  RequestStatus,
  StatusHistory
} from '../models/request.model';

@Injectable({
  providedIn: 'root'
})
export class RequestsService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'https://localhost:7094/api/Requests';

  getRequests(
    filter: RequestFilter
  ): Observable<PagedResult<RequestItem>> {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber)
      .set('pageSize', filter.pageSize)
      .set('sortBy', filter.sortBy)
      .set('sortDirection', filter.sortDirection);

    if (filter.search) {
      params = params.set('search', filter.search);
    }

    if (filter.status !== undefined) {
      params = params.set('status', filter.status);
    }

    if (filter.priority !== undefined) {
      params = params.set('priority', filter.priority);
    }

    if (filter.organizationName) {
      params = params.set(
        'organizationName',
        filter.organizationName
      );
    }

    if (filter.assignedTo) {
      params = params.set('assignedTo', filter.assignedTo);
    }

    return this.http.get<PagedResult<RequestItem>>(
      this.apiUrl,
      { params }
    );
  }

  getStatistics(): Observable<RequestStatistics> {
    return this.http.get<RequestStatistics>(
      `${this.apiUrl}/statistics`
    );
  }

  getHistory(id: number): Observable<StatusHistory[]> {
    return this.http.get<StatusHistory[]>(
      `${this.apiUrl}/${id}/history`
    );
  }

  updateStatus(
    id: number,
    newStatus: RequestStatus,
    changedBy: string
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${id}/status`,
      { newStatus, changedBy }
    );
  }

  bulkUpdateStatus(
    items: BulkUpdateItem[],
    changedBy: string
  ): Observable<{ message: string; updatedCount: number }> {
    return this.http.patch<{
      message: string;
      updatedCount: number;
    }>(
      `${this.apiUrl}/bulk/status`,
      { items, changedBy }
    );
  }
  bulkUpdateMatching(
  filter: RequestFilter,
  newStatus: RequestStatus,
  changedBy: string
): Observable<{ updatedCount: number }> {
  return this.http.patch<{ updatedCount: number }>(
    `${this.apiUrl}/bulk/matching/status`,
    {
      filter,
      newStatus,
      changedBy
    }
  );
}
}

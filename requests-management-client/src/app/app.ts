
import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';

import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';

import {
  BehaviorSubject,
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  finalize,
  of,
  switchMap,
  takeUntil
} from 'rxjs';

import { RequestsService } from './services/requests.service';

import {
  BulkUpdateItem,
  PagedResult,
  RequestFilter,
  RequestItem,
  RequestPriority,
  RequestStatistics,
  RequestStatus,
  StatusHistory
} from './models/request.model';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatProgressSpinnerModule,
    MatCheckboxModule,
    MatSnackBarModule,
    MatDividerModule
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly requestsService = inject(RequestsService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly RequestStatus = RequestStatus;
  readonly RequestPriority = RequestPriority;

  readonly columns = [
    'select', 'id', 'title', 'organizationName', 'priority',
    'status', 'assignedTo', 'createdAt', 'actions'
  ];

  readonly statuses = [
    { value: RequestStatus.New, label: 'חדש' },
    { value: RequestStatus.InProgress, label: 'בטיפול' },
    { value: RequestStatus.Waiting, label: 'ממתין' },
    { value: RequestStatus.Completed, label: 'הושלם' }
  ];

  readonly priorities = [
    { value: RequestPriority.Low, label: 'נמוכה' },
    { value: RequestPriority.Medium, label: 'בינונית' },
    { value: RequestPriority.High, label: 'גבוהה' }
  ];

  requests: RequestItem[] = [];
  statistics: RequestStatistics | null = null;
  history: StatusHistory[] = [];

  selectedIds = new Set<number>();
  selectAllMatching = false;
  selectedRequest: RequestItem | null = null;

  searchText = '';
  selectedStatus: RequestStatus | undefined;
  selectedPriority: RequestPriority | undefined;
  selectedBulkStatus: RequestStatus | undefined;
  selectedAssignedTo = '';

  totalCount = 0;
  pageNumber = 1;
  pageSize = 20;
  sortBy = 'CreatedAt';
  sortDirection: 'asc' | 'desc' = 'desc';

  loading = false;
  statisticsLoading = false;
  historyLoading = false;
  updating = false;

  errorMessage = '';
  historyError = '';
  changedBy = 'Admin';

  private readonly search$ = new Subject<string>();
  private readonly refresh$ = new BehaviorSubject<void>(undefined);
  private readonly destroyed$ = new Subject<void>();

  ngOnInit(): void {
    this.search$
      .pipe(
        debounceTime(350),
        distinctUntilChanged(),
        takeUntil(this.destroyed$)
      )
      .subscribe(() => {
        this.pageNumber = 1;
        this.loadRequests();
      });

    this.refresh$
      .pipe(
        switchMap(() => {
          this.loading = true;
          this.errorMessage = '';
          this.cdr.detectChanges();

          const filter: RequestFilter = {
            search: this.searchText.trim(),
            status: this.selectedStatus,
            priority: this.selectedPriority,
            assignedTo: this.selectedAssignedTo.trim() || undefined,
            pageNumber: this.pageNumber,
            pageSize: this.pageSize,
            sortBy: this.sortBy,
            sortDirection: this.sortDirection
          };

          return this.requestsService.getRequests(filter).pipe(
            catchError((error: HttpErrorResponse) => {
              console.error('REQUESTS ERROR:', error);
              this.errorMessage = this.getErrorMessage(error);
              return of({
                items: this.requests,
                totalCount: this.totalCount,
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                totalPages: Math.ceil(this.totalCount / this.pageSize)
              } as PagedResult<RequestItem>);
            })
          );
        }),
        takeUntil(this.destroyed$)
      )
      .subscribe(result => {
        console.log('REQUESTS RESULT:', result);
        console.log('ITEMS COUNT:', result.items?.length);
        this.requests = result.items ?? [];
        this.totalCount = result.totalCount ?? 0;
        // Do not clear selection when paging or sorting.
        this.loading = false;
        this.cdr.detectChanges();
      });

    this.loadStatistics();

    this.destroyRef.onDestroy(() => {
      this.destroyed$.next();
      this.destroyed$.complete();
    });
  }

  private resetSelection(): void {
    this.selectAllMatching = false;
    this.selectedIds.clear();
    this.selectedBulkStatus = undefined;
  }

  onSearchChange(value: string): void {
    this.searchText = value;
    this.resetSelection();
    this.search$.next(value);
  }

  loadRequests(): void {
    this.refresh$.next();
  }

  applyFilters(): void {
    this.resetSelection();
    this.pageNumber = 1;
    this.loadRequests();
  }

  clearFilters(): void {
    this.resetSelection();
    this.searchText = '';
    this.selectedStatus = undefined;
    this.selectedPriority = undefined;
    this.selectedAssignedTo = '';
    this.pageNumber = 1;
    this.loadRequests();
  }

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadRequests();
  }

  onSortChange(event: Sort): void {
    this.sortBy = event.active || 'CreatedAt';
    this.sortDirection = event.direction === 'asc' ? 'asc' : 'desc';
    this.pageNumber = 1;
    this.loadRequests();
  }

  loadStatistics(): void {
    console.log('START loading statistics');
    this.statisticsLoading = true;
    this.cdr.detectChanges();

    this.requestsService.getStatistics()
      .pipe(
        takeUntil(this.destroyed$),
        finalize(() => {
          this.statisticsLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: result => {
          console.log('STATISTICS SUCCESS:', result);
          this.statistics = result;
          this.cdr.detectChanges();
        },
        error: error => {
          console.error('STATISTICS FAILED:', error);
          this.cdr.detectChanges();
        }
      });
  }

  getStatusLabel(status: RequestStatus): string {
    return this.statuses.find(x => x.value === status)?.label ?? 'לא ידוע';
  }

  getPriorityLabel(priority: RequestPriority): string {
    return this.priorities.find(x => x.value === priority)?.label ?? 'לא ידוע';
  }

  getStatusCount(status: RequestStatus): number {
    const statusNames: Record<RequestStatus, string> = {
      [RequestStatus.New]: 'חדש',
      [RequestStatus.InProgress]: 'בתהליך',
      [RequestStatus.Waiting]: 'ממתין',
      [RequestStatus.Completed]: 'הושלם'
    };
    const name = statusNames[status];
    return this.statistics?.byStatus.find(
      x => x.status === name ||
        x.status === RequestStatus[status] ||
        x.status === String(status)
    )?.count ?? 0;
  }

  isSelected(id: number): boolean {
    return this.selectAllMatching || this.selectedIds.has(id);
  }

  toggleSelection(id: number, checked: boolean): void {
    if (this.selectAllMatching) return;
    if (checked) this.selectedIds.add(id);
    else this.selectedIds.delete(id);
  }

  isAllSelected(): boolean {
    return this.requests.length > 0 &&
      (this.selectAllMatching ||
        this.requests.every(r => this.selectedIds.has(r.id)));
  }

  toggleAll(checked: boolean): void {
    if (this.selectAllMatching) return;
    for (const request of this.requests) {
      if (checked) this.selectedIds.add(request.id);
      else this.selectedIds.delete(request.id);
    }
  }

  toggleAllMatching(): void {
    this.selectAllMatching = !this.selectAllMatching;
    this.selectedIds.clear();
    this.selectedBulkStatus = undefined;
    this.cdr.detectChanges();
  }

  updateStatus(request: RequestItem, status: RequestStatus): void {
    if (request.status === status || this.updating) return;
    this.updating = true;
    this.cdr.detectChanges();

this.requestsService.updateStatus(
  request.id,
  status,
  request.assignedTo || 'Admin'
)      .pipe(
        takeUntil(this.destroyed$),
        finalize(() => {
          this.updating = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: () => {
          this.notify('הסטטוס עודכן בהצלחה');
          this.loadRequests();
          this.loadStatistics();
        },
        error: (error: HttpErrorResponse) => {
          this.handleUpdateError(error);
          this.loadRequests();
        }
      });
  }


bulkUpdate(): void {
  if (
    this.selectedBulkStatus === undefined ||
    this.updating
  ) {
    return;
  }

  if (this.selectAllMatching) {
    if (this.totalCount === 0) {
      this.notify('אין פניות לעדכון');
      return;
    }

    const confirmed = window.confirm(
      `האם לעדכן את כל ${this.totalCount} הפניות שנבחרו?`
    );

    if (!confirmed) {
      return;
    }

    const filter: RequestFilter = {
      search: this.searchText.trim(),
      status: this.selectedStatus,
      priority: this.selectedPriority,
      assignedTo: this.selectedAssignedTo.trim() || undefined,
      pageNumber: 1,
      pageSize: 20,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection
    };

    this.updating = true;
    this.cdr.detectChanges();

    this.requestsService
      .bulkUpdateMatching(
        filter,
        this.selectedBulkStatus,
        this.changedBy
      )
      .pipe(
        takeUntil(this.destroyed$),
        finalize(() => {
          this.updating = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: result => {
          this.notify(
            `${result.updatedCount} פניות עודכנו בהצלחה`
          );

          this.selectAllMatching = false;
          this.selectedIds.clear();
          this.selectedBulkStatus = undefined;

          this.loadRequests();
          this.loadStatistics();
        },
        error: (error: HttpErrorResponse) => {
          this.handleUpdateError(error);
          this.loadRequests();
          this.loadStatistics();
        }
      });

    return;
  }

  // עדכון רגיל של הפניות שנבחרו בעמוד הנוכחי
  if (this.selectedIds.size === 0) {
    return;
  }

  const selectedOnPage = this.requests.filter(
    r => this.selectedIds.has(r.id)
  );

  if (selectedOnPage.length !== this.selectedIds.size) {
    this.notify(
      'נבחרו פניות מעמודים נוספים. יש לעדכן אותן בנפרד'
    );
    return;
  }

  const items: BulkUpdateItem[] = selectedOnPage.map(r => ({
    id: r.id,
    newStatus: this.selectedBulkStatus!,
    rowVersion: r.rowVersion
  }));

  if (items.some(x => !x.rowVersion)) {
    this.notify('חסרה גרסת נתונים. יש לרענן את הרשימה');
    return;
  }

  this.updating = true;
  this.cdr.detectChanges();

  this.requestsService
    .bulkUpdateStatus(items, this.changedBy)
    .pipe(
      takeUntil(this.destroyed$),
      finalize(() => {
        this.updating = false;
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      next: result => {
        this.notify(
          `${result.updatedCount} פניות עודכנו בהצלחה`
        );

        this.selectedIds.clear();
        this.selectedBulkStatus = undefined;

        this.loadRequests();
        this.loadStatistics();
      },
      error: (error: HttpErrorResponse) => {
        this.handleUpdateError(error);
        this.loadRequests();
      }
    });
}

  openHistory(request: RequestItem): void {
    this.selectedRequest = request;
    this.history = [];
    this.historyError = '';
    this.historyLoading = true;
    this.cdr.detectChanges();

    this.requestsService.getHistory(request.id)
      .pipe(
        takeUntil(this.destroyed$),
        finalize(() => {
          this.historyLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: result => {
          if (this.selectedRequest?.id !== request.id) return;
          this.history = result;
          this.cdr.detectChanges();
        },
        error: (error: HttpErrorResponse) => {
          if (this.selectedRequest?.id !== request.id) return;
          this.historyError = this.getErrorMessage(error);
          this.cdr.detectChanges();
        }
      });
  }

  closeHistory(): void {
    this.selectedRequest = null;
    this.history = [];
    this.historyError = '';
    this.cdr.detectChanges();
  }

  refreshAll(): void {
    this.loadRequests();
    this.loadStatistics();
  }

  private handleUpdateError(error: HttpErrorResponse): void {
    if (error.status === 409) {
      this.notify('הפנייה עודכנה על ידי משתמש אחר. הרשימה תתרענן');
      return;
    }
    this.notify(this.getErrorMessage(error));
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) return 'לא ניתן להתחבר לשרת. בדקי שה־API פועל';
    if (error.status === 409) return 'התנגשות בעדכון. יש לרענן את הנתונים';
    if (error.status === 400) return 'הבקשה אינה תקינה';
    if (error.status === 404) return 'הפנייה לא נמצאה';
    if (error.status === 500 || error.status === 504) {
      return 'השרת נתקל בשגיאה או שזמן הבקשה הסתיים';
    }
    return 'אירעה שגיאה בטעינת הנתונים';
  }

  private notify(message: string): void {
    this.snackBar.open(message, 'סגירה', {
      duration: 4500,
      horizontalPosition: 'center',
      verticalPosition: 'bottom'
    });
  }
}

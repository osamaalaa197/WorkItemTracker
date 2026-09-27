import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import {
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  merge,
  of,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import {
  ApiProblem,
  NEXT_STATUS,
  STATUS_LABELS,
  WORK_ITEM_STATUSES,
  WorkItem,
  WorkItemStatus,
} from '../../../core/models/work-item.model';
import { WorkItemService } from '../../../core/services/work-item.service';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';

@Component({
  selector: 'app-work-item-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StatusBadgeComponent],
  templateUrl: './work-item-list.component.html',
  styleUrl: './work-item-list.component.scss',
})
export class WorkItemListComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  readonly statuses = WORK_ITEM_STATUSES;
  readonly statusLabels = STATUS_LABELS;
  readonly nextStatus = NEXT_STATUS;
  readonly pageSize = 10;

  readonly searchControl = new FormControl<string>('', { nonNullable: true });
  readonly statusControl = new FormControl<WorkItemStatus | ''>('', {
    nonNullable: true,
  });

  items: WorkItem[] = [];
  totalCount = 0;
  page = 1;

  loading = false;
  errorMessage: string | null = null;

  /** Id of the item currently mid-transition, so only its own button shows a busy state. */
  advancingId: number | null = null;
  rowErrors = new Map<number, string>();

  private readonly reload$ = new Subject<void>();

  constructor(private readonly workItemService: WorkItemService) {}

  ngOnInit(): void {
    merge(
      this.searchControl.valueChanges.pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => (this.page = 1)),
      ),
      this.statusControl.valueChanges.pipe(tap(() => (this.page = 1))),
      this.reload$,
    )
      .pipe(
        startWith(undefined),
        switchMap(() => this.fetchPage()),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  get totalPages(): number {
    return this.totalCount === 0
      ? 1
      : Math.ceil(this.totalCount / this.pageSize);
  }

  refresh(): void {
    this.page = 1;
    this.reload$.next();
  }

  goToPage(target: number): void {
    if (target < 1 || target > this.totalPages || target === this.page) {
      return;
    }
    this.page = target;
    this.reload$.next();
  }

  advance(item: WorkItem): void {
    const next = this.nextStatus[item.status];
    if (!next) {
      return; // Done is terminal; the template already hides the button in this case.
    }

    this.rowErrors.delete(item.id);
    this.advancingId = item.id;

    this.workItemService.changeStatus(item.id, next).subscribe({
      next: (updated) => {
        item.status = updated.status;
        this.advancingId = null;
      },
      error: (err: HttpErrorResponse) => {
        this.advancingId = null;
        this.rowErrors.set(item.id, this.describeAdvanceError(err));
      },
    });
  }

  private fetchPage() {
    this.loading = true;
    this.errorMessage = null;

    const search = this.searchControl.value.trim();
    const status = this.statusControl.value;

    return this.workItemService
      .search({
        search: search || undefined,
        status: status || undefined,
        page: this.page,
        pageSize: this.pageSize,
      })
      .pipe(
        tap((result) => {
          this.items = result.items;
          this.totalCount = result.totalCount;
          this.loading = false;
        }),
        catchError(() => {
          this.items = [];
          this.totalCount = 0;
          this.loading = false;
          this.errorMessage =
            'Could not load work items. Check your connection and try again.';
          return of(null);
        }),
      );
  }

  private describeAdvanceError(err: HttpErrorResponse): string {
    if (err.status === 404) {
      return 'This item no longer exists. Refresh the list.';
    }
    if (err.status === 409) {
      return 'This item was already moved by someone else. Refresh to see its latest status.';
    }
    const problem = err.error as ApiProblem | undefined;
    return problem?.detail ?? 'Could not update the status. Please try again.';
  }
}

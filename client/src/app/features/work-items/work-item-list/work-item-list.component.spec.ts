import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { PagedResult, WorkItem } from '../../../core/models/work-item.model';
import { WorkItemListComponent } from './work-item-list.component';

const baseUrl = `${environment.apiBaseUrl}/work-items`;

function page(items: WorkItem[], totalCount = items.length): PagedResult<WorkItem> {
  return { items, totalCount, page: 1, pageSize: 10, totalPages: Math.max(1, Math.ceil(totalCount / 10)) };
}

function item(id: number, title: string, status: WorkItem['status'] = 'Todo'): WorkItem {
  return { id, title, description: null, status, createdAt: new Date().toISOString() };
}

describe('WorkItemListComponent', () => {
  let fixture: ComponentFixture<WorkItemListComponent>;
  let component: WorkItemListComponent;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkItemListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkItemListComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads the first page on init and shows results', fakeAsync(() => {
    fixture.detectChanges(); // triggers ngOnInit -> initial fetch (via startWith)

    const req = httpMock.expectOne((r) => r.url === baseUrl);
    req.flush(page([item(1, 'Alpha')]));
    tick();

    expect(component.loading).toBeFalse();
    expect(component.items.length).toBe(1);
    expect(component.errorMessage).toBeNull();
  }));

  it('shows the error state when the request fails', fakeAsync(() => {
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url === baseUrl);
    req.flush('boom', { status: 500, statusText: 'Server Error' });
    tick();

    expect(component.errorMessage).toContain('Could not load');
    expect(component.items.length).toBe(0);
  }));

  it('debounces search input and never applies a stale response', fakeAsync(() => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === baseUrl).flush(page([]));

    // Two keystrokes within the debounce window collapse into a single request for the
    // settled term — the intermediate "a" is debounced away before it ever fires.
    component.searchControl.setValue('a');
    tick(100);
    component.searchControl.setValue('al');
    tick(300);
    const firstRequest = httpMock.expectOne((r) => r.url === baseUrl && r.params.get('search') === 'al');

    // Now prove switchMap discards a slow, superseded response: a second search fires and
    // resolves BEFORE the first one, and the first one resolving late must be ignored.
    component.searchControl.setValue('alpha');
    tick(300);
    const secondRequest = httpMock.expectOne((r) => r.url === baseUrl && r.params.get('search') === 'alpha');

    secondRequest.flush(page([item(2, 'Alpha')]));
    tick();

    // The stale request was actually cancelled by switchMap (not just ignored) — Angular's
    // testing harness will throw if you try to flush an already-cancelled request, which is
    // exactly the proof we want: it can never resolve and overwrite the newer result.
    expect(firstRequest.cancelled).toBeTrue();
    expect(component.items.map((i) => i.title)).toEqual(['Alpha']);
  }));

  it('advances an item and reflects the new status without a full reload', fakeAsync(() => {
    fixture.detectChanges();
    const initial = item(1, 'Ship feature');
    httpMock.expectOne((r) => r.url === baseUrl).flush(page([initial]));
    tick();

    component.advance(component.items[0]);
    const req = httpMock.expectOne(`${baseUrl}/1/status`);
    expect(req.request.method).toBe('PATCH');
    req.flush(item(1, 'Ship feature', 'InProgress'));
    tick();

    expect(component.items[0].status).toBe('InProgress');
    expect(component.advancingId).toBeNull();
  }));

  it('shows a row-level message when a transition is rejected with 409', fakeAsync(() => {
    fixture.detectChanges();
    const initial = item(1, 'Ship feature', 'Done');
    // Force nextStatus lookup path by advancing from Todo normally, then simulate a 409 reply.
    initial.status = 'Todo';
    httpMock.expectOne((r) => r.url === baseUrl).flush(page([initial]));
    tick();

    component.advance(component.items[0]);
    const req = httpMock.expectOne(`${baseUrl}/1/status`);
    req.flush({ title: 'Conflict', detail: 'Invalid transition' }, { status: 409, statusText: 'Conflict' });
    tick();

    expect(component.rowErrors.get(1)).toContain('already moved');
  }));
});

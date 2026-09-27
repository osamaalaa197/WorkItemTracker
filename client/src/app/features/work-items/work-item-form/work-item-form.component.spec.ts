import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { WorkItemFormComponent } from './work-item-form.component';

describe('WorkItemFormComponent', () => {
  let fixture: ComponentFixture<WorkItemFormComponent>;
  let component: WorkItemFormComponent;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkItemFormComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkItemFormComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('marks the title as required and does not call the API when blank', () => {
    component.submit();

    expect(component.title.hasError('required')).toBeTrue();
    httpMock.expectNone(`${environment.apiBaseUrl}/work-items`);
  });

  it('rejects a title longer than 120 characters', () => {
    component.form.controls.title.setValue('a'.repeat(121));

    expect(component.title.hasError('maxlength')).toBeTrue();
  });

  it('submits a trimmed title and emits the created item on success', () => {
    component.form.controls.title.setValue('  Write more tests  ');
    let emitted: unknown;
    component.created.subscribe((item) => (emitted = item));

    component.submit();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/work-items`);
    expect(req.request.body.title).toBe('Write more tests');
    req.flush({ id: 1, title: 'Write more tests', description: null, status: 'Todo', createdAt: new Date().toISOString() });

    expect(emitted).toBeTruthy();
    expect(component.form.controls.title.value).toBe(''); // form reset after success
  });

  it('surfaces the server error message on a 400 response', () => {
    component.form.controls.title.setValue('Valid title');

    component.submit();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/work-items`);
    req.flush({ title: 'Bad Request', detail: 'Title is required.' }, { status: 400, statusText: 'Bad Request' });

    expect(component.serverError).toBe('Title is required.');
  });
});

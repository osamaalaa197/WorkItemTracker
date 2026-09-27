import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ApiProblem, WorkItem } from '../../../core/models/work-item.model';
import { WorkItemService } from '../../../core/services/work-item.service';

@Component({
  selector: 'app-work-item-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './work-item-form.component.html',
  styleUrl: './work-item-form.component.scss',
})
export class WorkItemFormComponent {
  @Output() created = new EventEmitter<WorkItem>();

  readonly titleMaxLength = 120;

  submitting = false;
  serverError: string | null = null;

  readonly form = this.fb.group({
    title: this.fb.control('', {
      validators: [
        Validators.required,
        Validators.maxLength(this.titleMaxLength),
      ],
      nonNullable: true,
    }),
    description: this.fb.control(''),
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly workItemService: WorkItemService,
  ) {}

  get title() {
    return this.form.controls.title;
  }

  submit(): void {
    this.serverError = null;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const { title, description } = this.form.getRawValue();

    this.workItemService
      .create({ title: title.trim(), description: description?.trim() || null })
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: (item) => {
          this.form.reset({ title: '', description: '' });
          this.created.emit(item);
        },
        error: (err: HttpErrorResponse) => {
          const problem = err.error as ApiProblem | undefined;
          this.serverError =
            problem?.detail ??
            'Could not create the work item. Please try again.';
        },
      });
  }
}

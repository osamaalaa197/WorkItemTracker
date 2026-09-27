import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { STATUS_LABELS, WorkItemStatus } from '../../../core/models/work-item.model';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `<span class="badge" [class]="status">{{ label }}</span>`,
  styles: [
    `
      .badge {
        display: inline-block;
        padding: 0.2rem 0.65rem;
        border-radius: 999px;
        font-size: 0.75rem;
        font-weight: 600;
        letter-spacing: 0.02em;
        text-transform: uppercase;
        white-space: nowrap;
      }
      .Todo {
        background: #e5e7eb;
        color: #374151;
      }
      .InProgress {
        background: #dbeafe;
        color: #1d4ed8;
      }
      .Done {
        background: #dcfce7;
        color: #15803d;
      }
    `,
  ],
})
export class StatusBadgeComponent {
  @Input({ required: true }) status!: WorkItemStatus;

  get label(): string {
    return STATUS_LABELS[this.status];
  }
}

import { Component, ViewChild } from '@angular/core';
import { WorkItem } from './core/models/work-item.model';
import { WorkItemFormComponent } from './features/work-items/work-item-form/work-item-form.component';
import { WorkItemListComponent } from './features/work-items/work-item-list/work-item-list.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [WorkItemFormComponent, WorkItemListComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  @ViewChild(WorkItemListComponent) list!: WorkItemListComponent;

  onCreated(_item: WorkItem): void {
    this.list.refresh();
  }
}

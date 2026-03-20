import { ToasterService } from '@abp/ng.theme.shared';
import { Component, inject, OnInit } from '@angular/core';
import { TodoService } from '../proxy/services';
import { TodoItemDto } from '../proxy/services/dtos';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { LocalizationPipe, PermissionDirective } from '@abp/ng.core';

@Component({
  selector: 'app-todo',
  standalone: true,
  imports: [CommonModule,
  FormsModule,
  LocalizationPipe,
  PermissionDirective],
  templateUrl: './todo.html',
  styleUrls: ['./todo.scss'],
})
export class Todo implements OnInit {
  todoItems: TodoItemDto[] = [];
  newTodoText: string;

  private readonly todoService = inject(TodoService);
  private readonly toasterService = inject(ToasterService);

  ngOnInit(): void {
    this.todoService.getList().subscribe(response => {
      this.todoItems = response;
    });
  }

  create(): void {
    this.todoService.create(this.newTodoText).subscribe(result => {
      this.todoItems = this.todoItems.concat(result);
      this.newTodoText = null;
      this.toasterService.success('::CREATEDTODOITEM');
    });
  }

  delete(id: string): void {
    this.todoService.delete(id).subscribe(() => {
      this.todoItems = this.todoItems.filter(item => item.id !== id);
      this.toasterService.info('::DELETEDTODOITEM');
    });
  }
}

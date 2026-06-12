import { Component, output, input } from '@angular/core';

@Component({
  selector: 'app-admin-search',
  imports: [],
  templateUrl: './admin-search.html',
  styleUrl: './admin-search.css',
})
export class AdminSearch {
  readonly query = input('');
  readonly queryChange = output<string>();

  protected onInput(event: Event): void {
    this.queryChange.emit((event.target as HTMLInputElement).value);
  }

  protected clear(): void {
    this.queryChange.emit('');
  }
}
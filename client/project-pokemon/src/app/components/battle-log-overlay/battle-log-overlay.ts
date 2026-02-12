import { Component, Input, Output, EventEmitter, OnChanges, OnDestroy, SimpleChanges, ChangeDetectorRef } from '@angular/core';

@Component({
  selector: 'app-battle-log-overlay',
  standalone: true,
  templateUrl: './battle-log-overlay.html',
  styleUrls: ['./battle-log-overlay.css']
})
export class BattleLogOverlay implements OnChanges, OnDestroy {
  @Input() log: string[] = [];
  @Input() visible = false;
  @Output() lineChanged = new EventEmitter<number>();
  
  currentLineIndex = 0;
  currentLine = '';
  private intervalId?: number;

  constructor(private cdr: ChangeDetectorRef) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['log'] && this.log.length > 0) {
      this.startDisplayingLines();
    }
    
    if (changes['visible'] && !this.visible) {
      this.stopDisplaying();
    }
  }

  ngOnDestroy(): void {
    this.stopDisplaying();
  }

  private startDisplayingLines(): void {
    this.stopDisplaying();
    this.currentLineIndex = 0;
    this.currentLine = this.log[0] || '';
    this.cdr.markForCheck();
    this.lineChanged.emit(0);
    
    this.intervalId = window.setInterval(() => {
      this.currentLineIndex++;
      if (this.currentLineIndex < this.log.length) {
        this.currentLine = this.log[this.currentLineIndex];
        this.cdr.markForCheck();
        this.lineChanged.emit(this.currentLineIndex);
      } else {
        this.stopDisplaying();
      }
    }, 3000);
  }

  private stopDisplaying(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
      this.intervalId = undefined;
    }
  }
}

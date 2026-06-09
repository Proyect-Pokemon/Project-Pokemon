import { Component, inject, Input, signal, effect, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SocketService } from '../../services/websocket-service';

interface ChatMessage {
  from: string;
  content: string;
  isOwn: boolean;
  type: 'chat' | 'system';
}

@Component({
  selector: 'app-battle-chat',
  imports: [CommonModule],
  templateUrl: './battle-chat.html',
  styleUrl: './battle-chat.css',
})
export class BattleChat {
  @Input() currentUsername: string = 'Usuario';
  @Input() isDisabled: boolean = false;
  @Input() battleId: string | null = null;
  @Input() systemMessages: string[] = [];

  messages = signal<ChatMessage[]>([]);
  isInputDisabled = signal(false);
  private processedSystemMessages = 0;

  private socketService = inject(SocketService);

  constructor() {
    // Escuchar mensajes del WebSocket
    effect(() => {
      const message = this.socketService.onChatMessage();
      if (message && this.battleId && message.battleId === this.battleId) {
        this.addMessage(message.from, message.content);
      }
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['battleId']) {
      this.messages.set([]);
      this.processedSystemMessages = 0;
    }

    if (changes['systemMessages']) {
      const nextBatch = this.systemMessages.slice(this.processedSystemMessages);
      for (const message of nextBatch) {
        this.addSystemMessage(message);
      }

      this.processedSystemMessages = this.systemMessages.length;
    }
  }

  sendMessage(content: string) {
    if (!content.trim() || this.isInputDisabled()) return;

    // Agregar el mensaje propio al chat
    this.messages.update(msgs => [...msgs, {
      from: this.currentUsername,
      content: content.trim(),
      isOwn: true,
      type: 'chat',
    }]);

    // Enviar el mensaje a través del WebSocket
    this.socketService.sendChatMessage(content.trim(), this.battleId ?? undefined);

    // Scroll al final
    this.scrollToBottom();
  }

  addMessage(from: string, content: string) {
    const isOwn = from.trim().toLowerCase() === this.currentUsername.trim().toLowerCase();
    if (isOwn) {
      return;
    }

    this.messages.update(msgs => [...msgs, { from, content, isOwn, type: 'chat' }]);
    this.scrollToBottom();
  }

  private addSystemMessage(content: string) {
    const message = content.trim();
    if (!message) {
      return;
    }

    this.messages.update(msgs => [...msgs, {
      from: 'Combate',
      content: message,
      isOwn: false,
      type: 'system',
    }]);
    this.scrollToBottom();
  }

  private scrollToBottom() {
    setTimeout(() => {
      const messagesDiv = document.querySelector('.chat-messages');
      if (messagesDiv) {
        messagesDiv.scrollTop = messagesDiv.scrollHeight;
      }
    });
  }
}

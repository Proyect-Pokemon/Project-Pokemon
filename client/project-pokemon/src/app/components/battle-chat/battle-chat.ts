import { Component, inject, Input, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SocketService } from '../../services/websocket-service';

interface ChatMessage {
  from: string;
  content: string;
  isOwn: boolean;
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

  messages = signal<ChatMessage[]>([]);
  isInputDisabled = signal(false);

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

  sendMessage(content: string) {
    if (!content.trim() || this.isInputDisabled()) return;

    // Agregar el mensaje propio al chat
    this.messages.update(msgs => [...msgs, {
      from: this.currentUsername,
      content: content.trim(),
      isOwn: true,
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

    this.messages.update(msgs => [...msgs, { from, content, isOwn }]);
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

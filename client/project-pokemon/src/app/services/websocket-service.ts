import { inject, Injectable, signal } from '@angular/core';

interface ChatMessage {
  from: string;
  content: string;
}

@Injectable({
  providedIn: 'root'
})
export class SocketService {

  private socket: WebSocket | null = null;
  private jwt?: string;
  private shouldReconnect = true;
  private reconnectAttempts = 0;
  private maxReconnectDelay = 30000; // 30s

  // Signal para notificar cuando llega un mensaje de chat
  onChatMessage = signal<ChatMessage | null>(null);

  constructor() {
    window.addEventListener("online", () => {
      console.log("Internet restaurado");
      if (this.shouldReconnect && this.jwt && !this.socket) {
        this.connect(this.jwt);
      }
    });

    window.addEventListener("offline", () => {
      console.log("Sin conexión a internet");
    });
  }

  /** Calcula delay de reconexión con backoff exponencial + jitter */
  private getReconnectDelay() {
    const baseDelay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), this.maxReconnectDelay);
    const jitter = Math.random() * 1000;
    return baseDelay + jitter;
  }

  /** Intento de reconexión seguro */
  private attemptReconnect() {
    if (this.shouldReconnect && this.jwt && localStorage.getItem("jwt") === this.jwt) {
      const delay = this.getReconnectDelay();
      console.log(`Reintentando conexión WS en ${Math.round(delay)}ms`);
      setTimeout(() => {
        this.reconnectAttempts++;
        this.connect(this.jwt!);
      }, delay);
    } else {
      console.log("No hay JWT válido: no se reconecta");
    }
  }

  /** Función que ejecuta al cerrar el socket */
  private socketOnClose = () => {
    console.log("WebSocket cerrado");
    this.socket = null;
    this.attemptReconnect();
  }

  /** Conecta WS */
  connect(jwt: string) {
    this.jwt = jwt;
    this.shouldReconnect = true;

    if (this.socket && this.socket.readyState !== WebSocket.CLOSED) return;

    if (this.socket) {
      try {
        this.socket.close();
      } catch { }
      this.socket = null;
    }

    const wsUrl = `wss://${location.hostname}:7277/ws?access_token=${jwt}`;

    console.log('Intentando conectar WS a', wsUrl);

    this.socket = new WebSocket(wsUrl);

    this.socket.onopen = () => {
      console.log('WebSocket conectado:', this.socket?.url);
      this.reconnectAttempts = 0;
    };

    this.socket.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data);

        if (message.type === 'chat') {
          const { from, content } = message.data;
          console.log(`Mensaje de ${from}: ${content}`);
          this.onChatMessage.set({ from, content });
        }

        if (message.type === 'battle') {
          // Todavía nada...
        }

      } catch (err) {
        console.error('Error parsing WS message', err);
      }
    };

    this.socket.onclose = this.socketOnClose;

    this.socket.onerror = (err) => {
      console.error('WebSocket error', err);
    };
  }

  /** Envía un mensaje de chat a través del WebSocket */
  sendChatMessage(content: string) {
    if (!this.socket || this.socket.readyState !== WebSocket.OPEN) {
      console.error('WebSocket no está conectado');
      return;
    }

    const message = {
      type: 'chat',
      data: { content }
    };

    try {
      this.socket.send(JSON.stringify(message));
    } catch (err) {
      console.error('Error al enviar mensaje de chat', err);
    }
  }

  /** Cierra WS y evita reconexiones automáticas */
  disconnect() {
    this.shouldReconnect = false;

    if (this.socket && this.socket.readyState === WebSocket.OPEN) {
      this.socket.close();
    }

    this.socket = null;
  }
}
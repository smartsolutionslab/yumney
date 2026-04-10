import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ENDPOINTS } from './api-endpoints';
import type { ChatRequest, ChatResponse } from './chat-message';
import type { ImportRecipeResponse } from './import-recipe-response';

@Injectable({ providedIn: 'root' })
export class ChatApiService {
  private http = inject(HttpClient);

  send(request: ChatRequest): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(API_ENDPOINTS.recipes.chat, request);
  }

  importFromText(text: string): Observable<ImportRecipeResponse> {
    return this.http.post<ImportRecipeResponse>(API_ENDPOINTS.recipes.importFromText, { text });
  }
}

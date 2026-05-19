import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { firstValueFrom } from 'rxjs';
import { ChatApiService } from '@yumney/shared/chat-api';
import { RecipeApiService } from '@yumney/shared/api-recipes';
import { ChatMessageDispatcher } from './chat-message-dispatcher.service';

describe('ChatMessageDispatcher', () => {
  let dispatcher: ChatMessageDispatcher;
  let chatApi: { send: ReturnType<typeof vi.fn> };
  let recipeApi: { importRecipe: ReturnType<typeof vi.fn>; importFromText: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    chatApi = {
      send: vi.fn().mockReturnValue(of({ reply: 'chat reply', suggestions: [], actions: [] })),
    };
    recipeApi = {
      importRecipe: vi
        .fn()
        .mockReturnValue(of({ title: 'Imported Recipe', ingredients: [1, 2, 3], steps: [1, 2] })),
      importFromText: vi
        .fn()
        .mockReturnValue(of({ title: 'Text Recipe', ingredients: [1], steps: [1, 2, 3] })),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: ChatApiService, useValue: chatApi },
        { provide: RecipeApiService, useValue: recipeApi },
      ],
    });

    dispatcher = TestBed.inject(ChatMessageDispatcher);
  });

  it('routes plain text to chatApi.send', async () => {
    const result = await firstValueFrom(dispatcher.dispatch('what is dinner?', []));

    expect(chatApi.send).toHaveBeenCalledWith({ message: 'what is dinner?', history: [] });
    expect(result.reply).toBe('chat reply');
  });

  it('routes a single URL to recipeApi.importRecipe', async () => {
    const result = await firstValueFrom(dispatcher.dispatch('https://example.com/recipe', []));

    expect(recipeApi.importRecipe).toHaveBeenCalledWith({ url: 'https://example.com/recipe' });
    expect(result.suggestions[0]).toEqual({ recipeIdentifier: null, title: 'Imported Recipe', reason: 'Extracted from URL' });
  });

  it('routes long recipe text (3+ lines) to recipeApi.importFromText', async () => {
    const text = 'Title\nIngredients\nSteps';
    const result = await firstValueFrom(dispatcher.dispatch(text, []));

    expect(recipeApi.importFromText).toHaveBeenCalledWith(text);
    expect(result.suggestions[0].reason).toBe('Extracted from text');
  });

  it('routes long recipe text (30+ words) to recipeApi.importFromText', async () => {
    const text = Array.from({ length: 30 }, (_, idx) => `word${idx}`).join(' ');
    await firstValueFrom(dispatcher.dispatch(text, []));

    expect(recipeApi.importFromText).toHaveBeenCalled();
  });

  it('builds a reply that names the imported recipe and counts', async () => {
    const result = await firstValueFrom(dispatcher.dispatch('https://example.com/r', []));

    expect(result.reply).toContain('Imported Recipe');
    expect(result.reply).toContain('3 ingredients');
    expect(result.reply).toContain('2 steps');
  });

  it('passes history through to chatApi.send for non-import messages', async () => {
    const history = [{ role: 'user' as const, content: 'hi' }];
    await firstValueFrom(dispatcher.dispatch('any reply', history));

    expect(chatApi.send).toHaveBeenCalledWith({ message: 'any reply', history });
  });

  it('returns empty actions array when chatApi omits the field', async () => {
    chatApi.send.mockReturnValue(of({ reply: 'r', suggestions: [] }));

    const result = await firstValueFrom(dispatcher.dispatch('hi', []));

    expect(result.actions).toEqual([]);
  });
});

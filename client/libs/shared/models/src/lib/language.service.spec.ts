import { LanguageService } from './language.service';

describe('LanguageService', () => {
  let service: LanguageService;
  let mockTransloco: {
    getActiveLang: ReturnType<typeof vi.fn>;
    setActiveLang: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    localStorage.clear();
    mockTransloco = {
      getActiveLang: vi.fn().mockReturnValue('en'),
      setActiveLang: vi.fn().mockImplementation((lang: string) => {
        mockTransloco.getActiveLang.mockReturnValue(lang);
      }),
    };

    service = new LanguageService(mockTransloco as any);
  });

  afterEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it('should use stored language on initialize', () => {
    localStorage.setItem('yn-language', 'de');
    service.initialize();
    expect(mockTransloco.setActiveLang).toHaveBeenCalledWith('de');
  });

  it('should detect browser language on initialize when no stored language', () => {
    Object.defineProperty(navigator, 'language', {
      value: 'de-AT',
      configurable: true,
    });
    service.initialize();
    expect(mockTransloco.setActiveLang).toHaveBeenCalledWith('de');
  });

  it('should fallback to en for unsupported browser language', () => {
    Object.defineProperty(navigator, 'language', {
      value: 'ja-JP',
      configurable: true,
    });
    service.initialize();
    expect(mockTransloco.setActiveLang).toHaveBeenCalledWith('en');
  });

  it('should switch language and persist to localStorage', () => {
    service.switchTo('de');
    expect(mockTransloco.setActiveLang).toHaveBeenCalledWith('de');
    expect(localStorage.getItem('yn-language')).toBe('de');
  });

  it('should ignore unsupported language', () => {
    service.switchTo('fr' as never);
    expect(mockTransloco.setActiveLang).not.toHaveBeenCalled();
    expect(localStorage.getItem('yn-language')).toBeNull();
  });

  it('should return active language from Transloco', () => {
    mockTransloco.getActiveLang.mockReturnValue('de');
    expect(service.activeLang).toBe('de');
  });
});

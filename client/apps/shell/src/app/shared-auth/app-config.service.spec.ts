import { TestBed } from '@angular/core/testing';
import { AppConfigService, getAppConfigGatewayUrl } from '@yumney/shared/auth';

interface AppConfig {
  keycloakUrl: string;
  keycloakRealm: string;
  keycloakClientId: string;
  gatewayUrl?: string;
}

const GLOBAL_KEY = '__yumneyAppConfig';
type ConfigHost = { [GLOBAL_KEY]?: AppConfig };

const fetchedConfig: AppConfig = {
  keycloakUrl: 'https://kc.example.com',
  keycloakRealm: 'yumney-prod',
  keycloakClientId: 'yumney-web',
  gatewayUrl: 'https://gateway.example.com/',
};

describe('AppConfigService', () => {
  let service: AppConfigService;
  let originalFetch: typeof fetch;

  beforeEach(() => {
    delete (globalThis as ConfigHost)[GLOBAL_KEY];
    originalFetch = globalThis.fetch;
    TestBed.configureTestingModule({});
    service = TestBed.inject(AppConfigService);
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    delete (globalThis as ConfigHost)[GLOBAL_KEY];
  });

  it('returns the default config when load() has not been called', () => {
    expect(service.get()).toEqual({
      keycloakUrl: 'http://localhost:8080',
      keycloakRealm: 'yumney',
      keycloakClientId: 'yumney-web',
    });
  });

  it('publishes the fetched config on globalThis when load succeeds', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(fetchedConfig),
    } as Response);

    await service.load();

    expect((globalThis as ConfigHost)[GLOBAL_KEY]).toEqual(fetchedConfig);
    expect(service.get()).toEqual(fetchedConfig);
  });

  it('falls back to the default config when fetch returns a non-OK response', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({ ok: false } as Response);

    await service.load();

    expect(service.get().keycloakUrl).toBe('http://localhost:8080');
  });

  it('falls back to the default config when fetch throws', async () => {
    globalThis.fetch = vi.fn().mockRejectedValue(new TypeError('network down'));

    await service.load();

    expect(service.get().keycloakUrl).toBe('http://localhost:8080');
  });

  it('exposes the gateway URL with any trailing slash stripped', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(fetchedConfig),
    } as Response);

    await service.load();

    expect(service.gatewayUrl).toBe('https://gateway.example.com');
    expect(getAppConfigGatewayUrl()).toBe('https://gateway.example.com');
  });

  it('returns an empty gateway URL when no config is loaded', () => {
    expect(getAppConfigGatewayUrl()).toBe('');
  });
});

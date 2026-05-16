import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideTransloco, TranslocoConfig } from '@jsverse/transloco';
import { DocsMcpComponent } from './docs-mcp.component';
import { DiscoveredCapabilitiesResponse } from './discovered-capability';

class StubTranslocoLoader {
  getTranslation = (lang: string): Promise<Record<string, string>> => Promise.resolve({ lang });
}

const transloco: Partial<TranslocoConfig> = {
  availableLangs: ['en'],
  defaultLang: 'en',
  reRenderOnLangChange: false,
  prodMode: true,
};

describe('DocsMcpComponent', () => {
  let fixture: ComponentFixture<DocsMcpComponent>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocsMcpComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTransloco({ config: transloco, loader: StubTranslocoLoader }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DocsMcpComponent);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('renders the MCP server URL in the hero', () => {
    fixture.detectChanges();
    http.expectOne('/discovered-capabilities').flush(emptyResponse());

    const html = fixture.nativeElement as HTMLElement;
    expect(html.textContent).toContain('yumney-gateway');
    expect(html.textContent).toContain('/mcp');
  });

  it('filters discovered capabilities to the MCP surface', () => {
    fixture.detectChanges();
    http.expectOne('/discovered-capabilities').flush({
      serviceCount: 1,
      capabilityCount: 2,
      mcpToolCount: 1,
      services: ['recipes-api'],
      capabilities: [
        { name: 'search_recipes', description: 'Search', httpMethod: 'GET', routePattern: '/r', surfaces: 'Chat, Mcp' },
        { name: 'chat_only', description: 'Chat only', httpMethod: 'POST', routePattern: '/c', surfaces: 'Chat' },
      ],
    });
    fixture.detectChanges();

    const html = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(html).toContain('search_recipes');
    expect(html).not.toContain('chat_only');
  });

  it('falls back to an empty list when discovery fails', () => {
    fixture.detectChanges();
    http.expectOne('/discovered-capabilities').error(new ProgressEvent('Network error'));
    fixture.detectChanges();

    // Stub Transloco loader leaves keys untranslated, so assert against the
    // structural marker (the empty-state paragraph) instead of the message text.
    const emptyMarker = (fixture.nativeElement as HTMLElement).querySelector('.tools__empty');
    expect(emptyMarker).not.toBeNull();
  });

  it('serialises the Claude Desktop config snippet with the mcp-remote args', () => {
    fixture.detectChanges();
    http.expectOne('/discovered-capabilities').flush(emptyResponse());
    fixture.detectChanges();

    const html = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(html).toContain('mcp-remote@latest');
    expect(html).toContain('"yumney"');
  });
});

function emptyResponse(): DiscoveredCapabilitiesResponse {
  return { serviceCount: 0, capabilityCount: 0, mcpToolCount: 0, services: [], capabilities: [] };
}

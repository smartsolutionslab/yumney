import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoModule } from '@jsverse/transloco';
import { DiscoveredCapabilitiesService } from './discovered-capabilities.service';
import { DiscoveredCapability } from './discovered-capability';

const MCP_PUBLIC_URL = 'https://yumney-gateway.calmsky-ae1ea5be.canadacentral.azurecontainerapps.io/mcp';
const CLAUDE_AI_ADD_URL = `https://claude.ai/settings/connectors/add?url=${encodeURIComponent(MCP_PUBLIC_URL)}`;
const CHATGPT_GPT_URL = 'https://chatgpt.com/g/yumney';

@Component({
  selector: 'yn-docs-mcp',
  imports: [TranslocoModule],
  templateUrl: './docs-mcp.component.html',
  styleUrl: './docs-mcp.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocsMcpComponent implements OnInit {
  protected readonly mcpUrl = MCP_PUBLIC_URL;
  protected readonly claudeAiAddUrl = CLAUDE_AI_ADD_URL;
  protected readonly chatGptGptUrl = CHATGPT_GPT_URL;

  protected readonly desktopConfig = JSON.stringify(
    {
      mcpServers: {
        yumney: {
          command: 'npx',
          args: ['-y', 'mcp-remote@latest', MCP_PUBLIC_URL],
        },
      },
    },
    null,
    2,
  );

  protected readonly tools = signal<DiscoveredCapability[] | null>(null);
  protected readonly copied = signal(false);
  protected readonly toolsLoading = computed(() => this.tools() === null);

  private readonly capabilitiesService = inject(DiscoveredCapabilitiesService);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.capabilitiesService
      .fetchMcpTools()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((tools) => this.tools.set(tools));
  }

  copyDesktopConfig(): void {
    navigator.clipboard
      .writeText(this.desktopConfig)
      .then(() => {
        this.copied.set(true);
        setTimeout(() => this.copied.set(false), 2000);
      })
      .catch(() => {
        // Clipboard write can fail on insecure contexts or older browsers — the
        // textarea below is selectable as a fallback, so swallow silently.
      });
  }
}

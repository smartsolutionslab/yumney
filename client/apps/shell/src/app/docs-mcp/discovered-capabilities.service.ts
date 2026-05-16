import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { DiscoveredCapabilitiesResponse, DiscoveredCapability } from './discovered-capability';

@Injectable({ providedIn: 'root' })
export class DiscoveredCapabilitiesService {
  private readonly http = inject(HttpClient);

  // The MCP server's /discovered-capabilities endpoint is anonymous (no bearer
  // required) so the marketing page works for visitors without a Yumney login.
  // Filters to MCP-surface capabilities client-side — the LLM landing page
  // shouldn't advertise tools that aren't reachable from an MCP client.
  fetchMcpTools(): Observable<DiscoveredCapability[]> {
    return this.http.get<DiscoveredCapabilitiesResponse>('/discovered-capabilities').pipe(
      map((response) => response.capabilities.filter((capability) => capability.surfaces.includes('Mcp'))),
      catchError(() => of([] as DiscoveredCapability[])),
    );
  }
}

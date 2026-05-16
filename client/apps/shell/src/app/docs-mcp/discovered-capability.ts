export interface DiscoveredCapability {
  readonly name: string;
  readonly description: string;
  readonly httpMethod: string;
  readonly routePattern: string;
  readonly surfaces: string;
}

export interface DiscoveredCapabilitiesResponse {
  readonly serviceCount: number;
  readonly capabilityCount: number;
  readonly mcpToolCount: number;
  readonly services: string[];
  readonly capabilities: DiscoveredCapability[];
}

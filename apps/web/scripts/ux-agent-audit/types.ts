export type ViewportPreset = 'desktop' | 'tablet' | 'mobile';
export type CaptureMode = 'safe' | 'local-full';
export type AutonomousDataMode = 'fixture' | 'fullstack';

export interface AutonomousFullstackDevAuthOptions {
  enabled: boolean;
  tenantId?: string;
  userId?: string;
  email?: string;
  permissions?: string[];
}

export type FindingSeverity = 'critical' | 'high' | 'medium' | 'low';

export interface PersonaDefinition {
  id: string;
  displayName: string;
  reviewGuidance: string[];
  defaultViewport: ViewportPreset;
  severityFocus: FindingSeverity;
}

export interface MissionDefinition<TPersonaId extends string> {
  id: string;
  personaId: TPersonaId;
  goal: string;
  maxSteps: number;
  viewport: ViewportPreset;
  successCriteria: string[];
}

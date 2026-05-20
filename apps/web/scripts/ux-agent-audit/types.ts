export type ViewportPreset = 'desktop' | 'tablet' | 'mobile';

export type FindingSeverity = 'critical' | 'high' | 'medium' | 'low';

export interface PersonaDefinition {
  id: string;
  displayName: string;
  reviewGuidance: string[];
  defaultViewport: ViewportPreset;
  severityFocus: FindingSeverity;
}

export interface MissionDefinition {
  id: string;
  personaId: string;
  goal: string;
  maxSteps: number;
  viewport: ViewportPreset;
  successCriteria: string[];
}
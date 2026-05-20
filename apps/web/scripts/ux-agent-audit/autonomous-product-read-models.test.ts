import { describe, expect, it } from 'vitest';

import {
  autonomousProductPaths,
  resolveAutonomousProductApiResponse,
} from './autonomous-product-read-models.ts';

describe('autonomous local product read models', () => {
  it('seeds the normal app cockpit instead of relying on the demo catalog', () => {
    const response = resolveAutonomousProductApiResponse('GET', '/workspace-overview');

    expect(response?.status).toBe(200);
    expect(response?.json).toEqual(
      expect.objectContaining({
        studyCollections: expect.objectContaining({
          sampleStudies: expect.arrayContaining([
            expect.objectContaining({
              name: 'Setup readiness sample',
              id: autonomousProductPaths.setupSampleSeriesId,
            }),
          ]),
          ownStudies: expect.arrayContaining([
            expect.objectContaining({
              name: 'New team study',
              id: autonomousProductPaths.ownStudySeriesId,
            }),
          ]),
        }),
      })
    );
  });

  it('serves selected study workspaces needed by autonomous persona missions', () => {
    expect(
      resolveAutonomousProductApiResponse(
        'GET',
        `/campaign-series/${autonomousProductPaths.completedSampleSeriesId}/setup-workspace`
      )?.json
    ).toEqual(expect.objectContaining({ series: expect.objectContaining({ name: 'Quarterly pulse' }) }));
    expect(
      resolveAutonomousProductApiResponse(
        'GET',
        `/campaign-series/${autonomousProductPaths.completedSampleSeriesId}/reports-widget-manifest`
      )?.json
    ).toEqual(expect.objectContaining({ surface: 'reports' }));
    expect(
      resolveAutonomousProductApiResponse(
        'GET',
        `/campaign-series/${autonomousProductPaths.longitudinalSampleSeriesId}/waves-workspace`
      )?.json
    ).toEqual(expect.objectContaining({ summary: expect.objectContaining({ submittedWaveCount: 2 }) }));
  });

  it('serves setup support read models needed by recipient selection controls', () => {
    expect(resolveAutonomousProductApiResponse('GET', '/subjects')?.json).toEqual(
      expect.objectContaining({
        summary: expect.objectContaining({ subjectCount: 2 }),
        subjects: expect.arrayContaining([
          expect.objectContaining({ displayName: 'Respondent 1' }),
        ]),
      })
    );
    expect(resolveAutonomousProductApiResponse('GET', '/subject-groups')?.json).toEqual(
      expect.objectContaining({
        groups: expect.arrayContaining([
          expect.objectContaining({ name: 'Research team' }),
        ]),
      })
    );
  });

  it('does not mask unsupported product API calls as successful data', () => {
    expect(resolveAutonomousProductApiResponse('POST', '/campaign-series')).toBeUndefined();
    expect(resolveAutonomousProductApiResponse('GET', '/registration/session')).toBeUndefined();
  });
});

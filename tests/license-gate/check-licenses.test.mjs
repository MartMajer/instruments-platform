import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import {
  classifyDependency,
  parseNpmLockDependencies,
  parseNugetLicenseFromNuspec,
  renderThirdPartyNotices
} from '../../tools/check-licenses.mjs';

const policy = {
  allowedLicenseExpressions: ['MIT', 'Apache-2.0', 'BSD-2-Clause', 'BSD-3-Clause', '0BSD', 'ISC'],
  conditionalLicenseExpressions: ['MPL-2.0', 'LGPL-3.0-only', 'PostgreSQL', 'BlueOak-1.0.0'],
  forbiddenLicensePatterns: ['AGPL', 'SSPL', 'BUSL', 'GPL-'],
  reviewedDependencies: [
    {
      ecosystem: 'npm',
      namePattern: '^lightningcss($|-)',
      license: 'MPL-2.0',
      approvedAs: 'MPL-2.0',
      reason: 'CSS tooling dependency; MPL applies to package files, not platform source.'
    }
  ]
};

describe('license policy classification', () => {
  it('allows default permissive licenses', () => {
    const result = classifyDependency(
      { ecosystem: 'npm', name: 'svelte', version: '5.55.2', license: 'MIT' },
      policy
    );

    assert.equal(result.status, 'allowed');
    assert.equal(result.issue, null);
  });

  it('blocks forbidden copyleft licenses', () => {
    const result = classifyDependency(
      { ecosystem: 'npm', name: 'bad-package', version: '1.0.0', license: 'AGPL-3.0-only' },
      policy
    );

    assert.equal(result.status, 'forbidden');
    assert.match(result.issue, /forbidden/i);
  });

  it('requires explicit review for conditional licenses', () => {
    const result = classifyDependency(
      { ecosystem: 'npm', name: 'unreviewed-mpl', version: '1.0.0', license: 'MPL-2.0' },
      policy
    );

    assert.equal(result.status, 'needs_review');
    assert.match(result.issue, /requires review/i);
  });

  it('treats LGPL as conditional review instead of pure-GPL forbidden', () => {
    const result = classifyDependency(
      { ecosystem: 'npm', name: 'unreviewed-lgpl', version: '1.0.0', license: 'LGPL-3.0-only' },
      policy
    );

    assert.equal(result.status, 'needs_review');
    assert.match(result.issue, /requires review/i);
  });

  it('allows reviewed conditional packages by pattern', () => {
    const result = classifyDependency(
      { ecosystem: 'npm', name: 'lightningcss-win32-x64-msvc', version: '2.0.0', license: 'MPL-2.0' },
      policy
    );

    assert.equal(result.status, 'reviewed');
    assert.equal(result.review.reason, 'CSS tooling dependency; MPL applies to package files, not platform source.');
  });
});

describe('dependency discovery helpers', () => {
  it('parses npm package-lock dependencies including scoped package names', () => {
    const deps = parseNpmLockDependencies({
      packages: {
        '': { name: 'web' },
        'node_modules/@scope/pkg': { version: '1.2.3', license: 'Apache-2.0' },
        'node_modules/plain': { version: '4.5.6', license: 'MIT' }
      }
    });

    assert.deepEqual(
      deps.map((dependency) => `${dependency.ecosystem}:${dependency.name}@${dependency.version}:${dependency.license}`),
      ['npm:@scope/pkg@1.2.3:Apache-2.0', 'npm:plain@4.5.6:MIT']
    );
  });

  it('extracts NuGet license expression and licenseUrl metadata from nuspec text', () => {
    assert.equal(parseNugetLicenseFromNuspec('<license type="expression">MIT</license>'), 'MIT');
    assert.equal(
      parseNugetLicenseFromNuspec('<licenseUrl>https://example.test/license.txt</licenseUrl>'),
      'licenseUrl:https://example.test/license.txt'
    );
  });

  it('renders an attribution artifact with dependency rows', () => {
    const markdown = renderThirdPartyNotices([
      {
        ecosystem: 'npm',
        name: 'svelte',
        version: '5.55.2',
        license: 'MIT',
        effectiveLicense: 'MIT',
        status: 'allowed',
        review: null
      }
    ]);

    assert.match(markdown, /Third-Party Notices/);
    assert.match(markdown, /npm/);
    assert.match(markdown, /svelte/);
    assert.match(markdown, /MIT/);
  });
});

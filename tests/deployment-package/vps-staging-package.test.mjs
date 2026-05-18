import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { describe, it } from 'node:test';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const read = (relativePath) => readFileSync(path.join(repoRoot, relativePath), 'utf8');
const exists = (relativePath) => existsSync(path.join(repoRoot, relativePath));
const serviceSection = (compose, serviceName) => {
  const marker = new RegExp(`(?:^|\\r?\\n)  ${serviceName}:\\r?\\n`);
  const match = marker.exec(compose);

  if (!match) {
    return '';
  }

  const start = match.index + match[0].length;
  const rest = compose.slice(start);
  const nextService = rest.search(/\r?\n  [a-zA-Z0-9_-]+:\r?\n/);

  return nextService === -1 ? rest : rest.slice(0, nextService);
};

describe('portable VPS staging deployment package', () => {
  it('contains the required VPS staging files', () => {
    for (const file of [
      'deploy/staging/vps.env.example',
      'deploy/staging/docker-compose.vps.yml',
      'deploy/staging/nginx.example.conf',
      'docs/v2/40-ops/vps-staging-runbook.md'
    ]) {
      assert.equal(exists(file), true, `${file} should exist`);
    }
  });

  it('keeps host exposure loopback-only for API and web', () => {
    const compose = read('deploy/staging/docker-compose.vps.yml');

    assert.match(compose, /ports:\s*!override/);
    assert.match(compose, /127\.0\.0\.1:\$\{API_HTTP_PORT:-5055\}:8080/);
    assert.match(compose, /127\.0\.0\.1:\$\{WEB_HTTP_PORT:-5174\}:3000/);
    assert.doesNotMatch(compose, /0\.0\.0\.0:\$\{API_HTTP_PORT/);
    assert.doesNotMatch(compose, /0\.0\.0\.0:\$\{WEB_HTTP_PORT/);
  });

  it('does not expose Postgres to the VPS host in the override', () => {
    const compose = read('deploy/staging/docker-compose.vps.yml');
    const postgresSection = serviceSection(compose, 'postgres');

    assert.notEqual(postgresSection, '');
    assert.match(postgresSection, /ports:\s*!reset\s*\[\]/);
    assert.doesNotMatch(postgresSection, /\$\{POSTGRES_PORT/);
    assert.doesNotMatch(postgresSection, /127\.0\.0\.1/);
    assert.doesNotMatch(postgresSection, /0\.0\.0\.0/);
  });

  it('documents subdomain nginx routing to the loopback services', () => {
    const nginx = read('deploy/staging/nginx.example.conf');

    assert.match(nginx, /server_name staging\.example\.com/);
    assert.match(nginx, /server_name staging-api\.example\.com/);
    assert.match(nginx, /proxy_pass http:\/\/127\.0\.0\.1:5174/);
    assert.match(nginx, /proxy_pass http:\/\/127\.0\.0\.1:5055/);
  });

  it('keeps VPS docs honest about staging-only status', () => {
    const env = read('deploy/staging/vps.env.example');
    const runbook = read('docs/v2/40-ops/vps-staging-runbook.md');

    assert.match(env, /COMPOSE_PROJECT_NAME=instruments-platform-staging/);
    assert.match(env, /PUBLIC_API_BASE_URL=https:\/\/staging-api\.example\.com/);
    assert.doesNotMatch(env, /BEGIN (RSA|OPENSSH) PRIVATE KEY/);
    assert.match(runbook, /not production/i);
    assert.match(runbook, /Q-020/);
    assert.match(runbook, /DEP01-B is the last deployment slice for now/);
  });
});

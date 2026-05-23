import { mkdir, writeFile } from 'node:fs/promises';
import { join } from 'node:path';

import { resolveFullstackDevAuthHeaders } from './fullstack-dev-auth.ts';
import { getRealisticAuditCase, type RealisticAuditCase } from './realistic-cases.ts';
import {
  seedRealisticFullstackCase,
  type RealisticFullstackSeedResult,
} from './realistic-fullstack-seed.ts';
import type { AutonomousFullstackDevAuthOptions } from './types.ts';

export interface PersonaDemoSeedOptions {
  apiBaseUrl: string;
  outputRoot: string;
  fullstackDevAuth: AutonomousFullstackDevAuthOptions;
  fetchImpl?: typeof fetch;
}

export interface PersonaDemoAccountDefinition {
  id: 'osh-consultant' | 'work-ergonomics-specialist' | 'busy-professor';
  displayName: string;
  email: string;
  userId: string;
  roleCode: 'tenant_owner';
  locale: 'en';
}

export interface PersonaDemoStudyDefinition {
  accountId: PersonaDemoAccountDefinition['id'];
  realisticCaseId: string;
  mode: 'single-wave' | 'repeated-wave';
  purpose: string;
}

export interface PersonaDemoSeedResult {
  seedMode: 'local-persona-demo-workspace';
  tenantId: string;
  generatedAt: string;
  manifestPath: string;
  accounts: PersonaDemoSeededAccount[];
  studies: PersonaDemoSeededStudy[];
  stagingPortability: {
    auth0Required: true;
    note: string;
  };
}

export interface PersonaDemoSeededAccount {
  id: PersonaDemoAccountDefinition['id'];
  displayName: string;
  email: string;
  userId: string;
  roleCode: string;
  identitySource: 'tenant-member' | 'dev-auth-virtual';
  devAuth: {
    tenantId: string;
    userId: string;
    email: string;
    permissions: string[];
  };
}

export interface PersonaDemoSeededStudy {
  accountId: PersonaDemoAccountDefinition['id'];
  realisticCaseId: string;
  studyName: string;
  purpose: string;
  mode: 'single-wave' | 'repeated-wave';
  questions: RealisticAuditCase['questions'];
  fullstackSeed: RealisticFullstackSeedResult;
  appUrls: {
    setup: string;
    collection: string;
    waves: string;
    results: string;
  };
}

type ApiClient = ReturnType<typeof createPersonaDemoApiClient>;

const defaultTenantId = '11111111-1111-4111-8111-111111111111';
const defaultPermissions = ['setup.manage', 'team.manage', 'export.read'];

export const personaDemoAccounts = [
  {
    id: 'osh-consultant',
    displayName: 'Marko Horvat - OSH consultant',
    email: 'demo.osh@validatedscale.test',
    userId: '33333333-3333-4333-8333-333333333331',
    roleCode: 'tenant_owner',
    locale: 'en',
  },
  {
    id: 'work-ergonomics-specialist',
    displayName: 'Nina Peric - work ergonomics specialist',
    email: 'demo.ergonomics@validatedscale.test',
    userId: '33333333-3333-4333-8333-333333333332',
    roleCode: 'tenant_owner',
    locale: 'en',
  },
  {
    id: 'busy-professor',
    displayName: 'Prof. Ivana Radic - academic PI',
    email: 'demo.professor@validatedscale.test',
    userId: '33333333-3333-4333-8333-333333333333',
    roleCode: 'tenant_owner',
    locale: 'en',
  },
] satisfies PersonaDemoAccountDefinition[];

export const personaDemoStudies = [
  {
    accountId: 'osh-consultant',
    realisticCaseId: 'osh-warehouse-workload-recovery-pulse',
    mode: 'single-wave',
    purpose:
      'Client-ready warehouse workload, recovery, support, and musculoskeletal strain review.',
  },
  {
    accountId: 'work-ergonomics-specialist',
    realisticCaseId: 'office-ergonomics-hybrid-work-followup',
    mode: 'repeated-wave',
    purpose:
      'Office and hybrid-work ergonomics baseline and follow-up after workstation adjustments.',
  },
  {
    accountId: 'busy-professor',
    realisticCaseId: 'academic-workload-recovery-followup',
    mode: 'repeated-wave',
    purpose:
      'Academic workload and recovery repeated-wave study with anonymous longitudinal comparison.',
  },
] satisfies PersonaDemoStudyDefinition[];

export async function seedPersonaDemoWorkspace(
  options: PersonaDemoSeedOptions
): Promise<PersonaDemoSeedResult> {
  const apiBaseUrl = normalizeLocalApiBaseUrl(options.apiBaseUrl);
  const fetchImpl = options.fetchImpl ?? fetch;
  const client = createPersonaDemoApiClient(apiBaseUrl, fetchImpl);
  const adminAuth = normalizeAdminAuth(options.fullstackDevAuth);
  const tenantId = adminAuth.tenantId ?? defaultTenantId;
  const accounts: PersonaDemoSeededAccount[] = [];

  for (const account of personaDemoAccounts) {
    const member = await ensureTenantMember(client, adminAuth, account);
    accounts.push({
      id: account.id,
      displayName: account.displayName,
      email: member.email,
      userId: member.userId,
      roleCode: account.roleCode,
      identitySource: member.identitySource,
      devAuth: {
        tenantId,
        userId: member.userId,
        email: member.email,
        permissions: defaultPermissions,
      },
    });
  }

  const accountById = new Map(accounts.map((account) => [account.id, account]));
  const studies: PersonaDemoSeededStudy[] = [];

  for (const plan of personaDemoStudies) {
    const account = accountById.get(plan.accountId);
    if (!account) {
      throw new Error(`Missing seeded persona account: ${plan.accountId}`);
    }

    const realisticCase = requireRealisticCase(plan.realisticCaseId);
    const personaAuth: AutonomousFullstackDevAuthOptions = {
      enabled: true,
      tenantId,
      userId: account.userId,
      email: account.email,
      permissions: defaultPermissions,
    };
    const series = await client.post<{ id: string }>(
      '/campaign-series',
      { name: realisticCase.studyName },
      personaAuth
    );
    const fullstackSeed = await seedRealisticFullstackCase({
      apiBaseUrl,
      fullstackDevAuth: personaAuth,
      seriesId: series.id,
      realisticCase,
      mode: plan.mode,
      responseMode: 'test-data-simulator',
      fetchImpl,
    });

    studies.push({
      accountId: plan.accountId,
      realisticCaseId: realisticCase.id,
      studyName: realisticCase.studyName,
      purpose: plan.purpose,
      mode: plan.mode,
      questions: realisticCase.questions,
      fullstackSeed,
      appUrls: {
        setup: `/app/campaign-series/${fullstackSeed.seriesId}/setup`,
        collection: fullstackSeed.operationsPath,
        waves: fullstackSeed.wavesPath,
        results: fullstackSeed.reportsPath,
      },
    });
  }

  const generatedAt = new Date().toISOString();
  const manifestDirectory = join(
    options.outputRoot,
    `persona-demo-${generatedAt.replace(/[:.]/g, '-')}`
  );
  const manifestPath = join(manifestDirectory, 'manifest.json');
  const result: PersonaDemoSeedResult = {
    seedMode: 'local-persona-demo-workspace',
    tenantId,
    generatedAt,
    manifestPath,
    accounts,
    studies,
    stagingPortability: {
      auth0Required: true,
      note:
        'Local demo accounts use development-auth headers. Staging demo accounts must be created or linked through Auth0, then the same study seed can be run under the real platform memberships.',
    },
  };

  await mkdir(manifestDirectory, { recursive: true });
  await writeFile(manifestPath, `${JSON.stringify(result, null, 2)}\n`, 'utf8');

  return result;
}

function createPersonaDemoApiClient(apiBaseUrl: string, fetchImpl: typeof fetch) {
  async function request<T>(
    method: string,
    path: string,
    body: unknown,
    auth: AutonomousFullstackDevAuthOptions
  ): Promise<T> {
    const headers = resolveFullstackDevAuthHeaders(auth);
    if (!headers) {
      throw new Error('Persona demo seeding requires --fullstack-dev-auth.');
    }

    const response = await fetchImpl(new URL(path, `${apiBaseUrl}/`).toString(), {
      method,
      headers: {
        ...headers,
        ...(body === undefined ? {} : { 'content-type': 'application/json' }),
      },
      ...(body === undefined ? {} : { body: JSON.stringify(body) }),
    });

    if (!response.ok) {
      throw new Error(
        `Persona demo API request failed: ${method} ${path} returned ${response.status} ${await response.text()}`
      );
    }

    return (await response.json()) as T;
  }

  return {
    get<T>(path: string, auth: AutonomousFullstackDevAuthOptions) {
      return request<T>('GET', path, undefined, auth);
    },
    post<T>(path: string, body: unknown, auth: AutonomousFullstackDevAuthOptions) {
      return request<T>('POST', path, body, auth);
    },
  };
}

async function ensureTenantMember(
  client: ApiClient,
  adminAuth: AutonomousFullstackDevAuthOptions,
  account: PersonaDemoAccountDefinition
) {
  const existing = await client.get<TenantMemberRosterResponse>('/tenant-members', adminAuth);
  const member = existing.members.find(
    (candidate) => candidate.email.toLowerCase() === account.email.toLowerCase()
  );
  if (member) {
    return { ...member, identitySource: 'tenant-member' as const };
  }

  const roles = await client.get<TenantRoleListResponse>('/tenant-roles', adminAuth);
  const canAssignRole = roles.roles.some(
    (role) => role.code.toLowerCase() === account.roleCode
  );
  if (!canAssignRole) {
    return {
      userId: account.userId,
      email: account.email,
      identitySource: 'dev-auth-virtual' as const,
    };
  }

  const created = await client.post<TenantMemberMutationResponse>(
    '/tenant-members',
    {
      email: account.email,
      roleCode: account.roleCode,
      locale: account.locale,
    },
    adminAuth
  );

  return { ...created.member, identitySource: 'tenant-member' as const };
}

function normalizeAdminAuth(
  fullstackDevAuth: AutonomousFullstackDevAuthOptions
): AutonomousFullstackDevAuthOptions {
  if (fullstackDevAuth.enabled !== true) {
    throw new Error('Persona demo seeding requires --fullstack-dev-auth.');
  }

  return {
    ...fullstackDevAuth,
    tenantId: fullstackDevAuth.tenantId ?? defaultTenantId,
    permissions: fullstackDevAuth.permissions?.length
      ? fullstackDevAuth.permissions
      : defaultPermissions,
  };
}

function requireRealisticCase(id: string) {
  const realisticCase = getRealisticAuditCase(id);
  if (!realisticCase) {
    throw new Error(`Unknown realistic audit case: ${id}`);
  }

  return realisticCase;
}

function normalizeLocalApiBaseUrl(value: string) {
  const url = new URL(value);
  const localHosts = new Set(['localhost', '127.0.0.1', '::1', '0.0.0.0']);

  if (url.protocol !== 'http:' || !localHosts.has(url.hostname)) {
    throw new Error('Persona demo seeding requires a local loopback API URL.');
  }

  url.pathname = url.pathname.replace(/\/+$/g, '');
  url.search = '';
  url.hash = '';

  return url.toString();
}

interface TenantMemberRosterResponse {
  tenantId: string;
  members: TenantMemberResponse[];
}

interface TenantMemberMutationResponse {
  member: TenantMemberResponse;
}

interface TenantMemberResponse {
  userId: string;
  email: string;
}

interface TenantRoleListResponse {
  roles: TenantRoleResponse[];
}

interface TenantRoleResponse {
  code: string;
}

import { describe, expect, it } from 'vitest';
import {
	createProductApi,
	type CampaignSeriesReportsWidgetManifestResponse,
	type ExportArtifactLibraryResponse,
	type TenantSettingsWorkspaceResponse,
	type WorkspaceOverviewResponse
} from './product';

describe('createProductApi', () => {
	it('requests workspace overview', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return sampleWorkspaceOverview as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.getWorkspaceOverview();

		expect(calls).toEqual(['/workspace-overview']);
	});

	it('requests tenant settings', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return sampleTenantSettings as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.getTenantSettings();

		expect(calls).toEqual(['/tenant-settings']);
	});

	it('requests export artifact library', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return sampleExportArtifactLibrary as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.listExportArtifacts();

		expect(calls).toEqual(['/export-artifacts']);
	});

	it('requests tenant member roster', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return {
					tenantId: 'tenant-id',
					summary: {
						totalCount: 0,
						activeCount: 0,
						invitedCount: 0,
						suspendedCount: 0,
						teamManagerCount: 0
					},
					members: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.listTenantMembers();

		expect(calls).toEqual(['/tenant-members']);
	});

	it('requests tenant roles', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return { roles: [] } as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.listTenantRoles();

		expect(calls).toEqual(['/tenant-roles']);
	});

	it('requests subject directory', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return {
					tenantId: 'tenant-id',
					summary: {
						subjectCount: 0,
						groupCount: 0,
						managerRelationshipCount: 0
					},
					subjects: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.listSubjects();

		expect(calls).toEqual(['/subjects']);
	});

	it('requests subject directory with search, paging, filters, and sort query', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return {
					tenantId: 'tenant-id',
					summary: {
						subjectCount: 2000,
						filteredSubjectCount: 2000,
						returnedSubjectCount: 25,
						groupCount: 0,
						managerRelationshipCount: 0,
						pageOffset: 25,
						pageSize: 25,
						hasMore: true
					},
					subjects: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.listSubjects({
			search: 'ana',
			skip: 25,
			take: 25,
			sort: 'department_asc',
			source: 'microsoft_graph',
			status: 'active',
			groupId: 'group-id',
			manager: 'missing',
			contact: 'missing_email'
		});

		expect(calls).toEqual([
			'/subjects?search=ana&skip=25&take=25&sort=department_asc&source=microsoft_graph&status=active&groupId=group-id&manager=missing&contact=missing_email'
		]);
	});

	it('creates a subject directory entry', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return sampleSubject as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.createSubject({
			displayName: 'Ana Analyst',
			email: 'ana@example.test',
			externalId: 'emp-001',
			locale: 'hr',
			attributes: '{"title":"Analyst"}'
		});

		expect(calls).toEqual([
			{
				path: '/subjects',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({
						displayName: 'Ana Analyst',
						email: 'ana@example.test',
						externalId: 'emp-001',
						locale: 'hr',
						attributes: '{"title":"Analyst"}'
					})
				}
			}
		]);
	});

	it('imports subject directory CSV rows', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const csvContent = [
			'external_id,email,display_name,group_type,group_name',
			'emp-001,ana@example.test,Ana Analyst,department,Research'
		].join('\n');
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					tenantId: 'tenant-id',
					rowCount: 1,
					importedRowCount: 1,
					createdSubjectCount: 1,
					updatedSubjectCount: 0,
					createdGroupCount: 1,
					addedMembershipCount: 1,
					skippedMembershipCount: 0,
					rows: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.importSubjectDirectoryCsv({ csvContent });

		expect(calls).toEqual([
			{
				path: '/subjects/imports/csv',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({ csvContent })
				}
			}
		]);
	});

	it('requests Microsoft Graph directory import workspace and commands', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					tenantId: 'tenant-id',
					connections: [],
					rules: [],
					recentRuns: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.getDirectoryImportWorkspace();
		await api.startMicrosoftGraphAdminConsent();
		await api.createDirectoryConnection({
			externalTenantId: 'customer-tenant',
			displayName: 'Algebra sandbox',
			primaryDomain: 'algebra.example',
			grantedScopes: ['User.Read.All', 'Group.Read.All']
		});
		await api.createDirectoryImportRule({
			connectionId: 'connection/id',
			name: 'Third year students',
			criteria: {
				accountEnabled: true,
				departments: ['Psychology'],
				includeManagerChain: true
			},
			fieldSelection: {
				fields: ['displayName', 'mail', 'department']
			},
			mirrorMode: true,
			mirrorConfirmation: 'MIRROR MICROSOFT DIRECTORY'
		});
		await api.previewDirectoryImportRule('rule/id');
		await api.applyDirectoryImportRun('run/id');

		expect(calls).toEqual([
			{ path: '/directory-imports/workspace', init: undefined },
			{
				path: '/directory-connections/microsoft-graph/admin-consent/start',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({})
				}
			},
			{
				path: '/directory-connections',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({
						externalTenantId: 'customer-tenant',
						displayName: 'Algebra sandbox',
						primaryDomain: 'algebra.example',
						grantedScopes: ['User.Read.All', 'Group.Read.All']
					})
				}
			},
			{
				path: '/directory-import-rules',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({
						connectionId: 'connection/id',
						name: 'Third year students',
						criteria: {
							accountEnabled: true,
							departments: ['Psychology'],
							includeManagerChain: true
						},
						fieldSelection: {
							fields: ['displayName', 'mail', 'department']
						},
						mirrorMode: true,
						mirrorConfirmation: 'MIRROR MICROSOFT DIRECTORY'
					})
				}
			},
			{
				path: '/directory-import-rules/rule%2Fid/preview',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({})
				}
			},
			{
				path: '/directory-import-runs/run%2Fid/apply',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({})
				}
			}
		]);
	});

	it('updates a subject directory entry by encoded subject id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return sampleSubject as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.updateSubject('subject/id', {
			displayName: 'Ana Analyst',
			email: 'ana@example.test',
			externalId: 'emp-001',
			locale: 'hr',
			attributes: '{}'
		});

		expect(calls).toEqual([
			{
				path: '/subjects/subject%2Fid',
				init: {
					method: 'PUT',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({
						displayName: 'Ana Analyst',
						email: 'ana@example.test',
						externalId: 'emp-001',
						locale: 'hr',
						attributes: '{}'
					})
				}
			}
		]);
	});

	it('deactivates a subject directory entry by encoded subject id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					...sampleSubject,
					status: 'deactivated',
					statusLabel: 'Deactivated'
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.deactivateSubject('subject/id', { reason: 'Imported by mistake' });

		expect(calls).toEqual([
			{
				path: '/subjects/subject%2Fid/deactivate',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({ reason: 'Imported by mistake' })
				}
			}
		]);
	});

	it('sets a subject directory status by encoded subject id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					...sampleSubject,
					status: 'active',
					statusLabel: 'Active'
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.setSubjectStatus('subject/id', { status: 'active', reason: 'Undo mistake' });

		expect(calls).toEqual([
			{
				path: '/subjects/subject%2Fid/status',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({ status: 'active', reason: 'Undo mistake' })
				}
			}
		]);
	});

	it('requests subject groups', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return { tenantId: 'tenant-id', groups: [] } as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.listSubjectGroups();

		expect(calls).toEqual(['/subject-groups']);
	});

	it('creates a subject group', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return sampleSubjectGroup as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.createSubjectGroup({
			type: 'department',
			name: 'Research',
			parentGroupId: 'parent-group',
			attributes: '{"campus":"Zagreb"}'
		});

		expect(calls).toEqual([
			{
				path: '/subject-groups',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({
						type: 'department',
						name: 'Research',
						parentGroupId: 'parent-group',
						attributes: '{"campus":"Zagreb"}'
					})
				}
			}
		]);
	});

	it('adds a subject group member by encoded group id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					groupId: 'group/id',
					groupType: 'department',
					groupName: 'Research',
					roleInGroup: 'member',
					validFrom: '2026-05-15',
					validTo: null
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.addSubjectGroupMember('group/id', {
			subjectId: 'subject-id',
			roleInGroup: 'member',
			validFrom: '2026-05-15',
			validTo: null
		});

		expect(calls).toEqual([
			{
				path: '/subject-groups/group%2Fid/members',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({
						subjectId: 'subject-id',
						roleInGroup: 'member',
						validFrom: '2026-05-15',
						validTo: null
					})
				}
			}
		]);
	});

	it('removes a subject group member by encoded group and subject ids', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					groupId: 'group/id',
					subjectId: 'subject/id',
					removed: true
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.removeSubjectGroupMember('group/id', 'subject/id');

		expect(calls).toEqual([
			{
				path: '/subject-groups/group%2Fid/members/subject%2Fid',
				init: {
					method: 'DELETE'
				}
			}
		]);
	});

	it('sets a subject manager by encoded subject id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return sampleSubject as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.setSubjectManager('subject/id', {
			managerSubjectId: 'manager-id',
			validFrom: '2026-05-15'
		});

		expect(calls).toEqual([
			{
				path: '/subjects/subject%2Fid/manager',
				init: {
					method: 'PUT',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({
						managerSubjectId: 'manager-id',
						validFrom: '2026-05-15'
					})
				}
			}
		]);
	});

	it('creates a tenant member with selected role', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return { member: sampleTenantMember } as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.createTenantMember({
			email: 'new.member@example.test',
			roleCode: 'analyst',
			locale: 'hr'
		});

		expect(calls).toEqual([
			{
				path: '/tenant-members',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({
						email: 'new.member@example.test',
						roleCode: 'analyst',
						locale: 'hr'
					})
				}
			}
		]);
	});

	it('changes a tenant member role by encoded user id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return { member: sampleTenantMember } as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.changeTenantMemberRole('user/id', { roleCode: 'tenant_owner' });

		expect(calls).toEqual([
			{
				path: '/tenant-members/user%2Fid/tenant-role',
				init: {
					method: 'PUT',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({ roleCode: 'tenant_owner' })
				}
			}
		]);
	});

	it('suspends a tenant member by encoded user id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return { member: sampleTenantMember } as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.suspendTenantMember('user/id');

		expect(calls).toEqual([
			{
				path: '/tenant-members/user%2Fid/suspend',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: '{}'
				}
			}
		]);
	});

	it('reactivates a tenant member by encoded user id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return { member: sampleTenantMember } as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.reactivateTenantMember('user/id');

		expect(calls).toEqual([
			{
				path: '/tenant-members/user%2Fid/reactivate',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: '{}'
				}
			}
		]);
	});

	it('removes a tenant member by encoded user id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return { userId: 'user/id', removed: true } as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.removeTenantMember('user/id');

		expect(calls).toEqual([
			{
				path: '/tenant-members/user%2Fid',
				init: {
					method: 'DELETE'
				}
			}
		]);
	});

	it('requests campaign series list', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return { items: [] } as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.listCampaignSeries();

		expect(calls).toEqual(['/campaign-series']);
	});

	it('serializes campaign series portfolio query parameters', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return { items: [] } as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.listCampaignSeries({
			search: 'gamma proof',
			status: 'proof_only',
			sort: 'name_asc',
			visibility: 'archived'
		});

		expect(calls).toEqual([
			'/campaign-series?search=gamma+proof&status=proof_only&sort=name_asc&visibility=archived'
		]);
	});

	it('renames campaign series by id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					id: 'series/id',
					name: 'Renamed pulse',
					updatedAt: '2026-05-09T12:30:00Z'
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.renameCampaignSeries('series/id', { name: 'Renamed pulse' });

		expect(calls).toEqual([
			{
				path: '/campaign-series/series%2Fid',
				init: {
					method: 'PATCH',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({ name: 'Renamed pulse' })
				}
			}
		]);
	});

	it('duplicates sample campaign series by encoded id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					id: 'copy-id',
					name: 'Copy of Starter sample',
					studyKind: 'own',
					isSample: false,
					sourceCampaignSeriesId: 'series/id'
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		const result = await api.duplicateCampaignSeries('series/id', {
			name: 'Copy of Starter sample'
		});

		expect(result).toEqual({
			id: 'copy-id',
			name: 'Copy of Starter sample',
			studyKind: 'own',
			isSample: false,
			sourceCampaignSeriesId: 'series/id'
		});
		expect(calls).toEqual([
			{
				path: '/campaign-series/series%2Fid/duplicate',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({ name: 'Copy of Starter sample' })
				}
			}
		]);
	});

	it('archives campaign series by id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					id: 'series/id',
					archived: true,
					updatedAt: '2026-05-11T13:15:00Z',
					archivedAt: '2026-05-11T13:15:00Z',
					archivedByUserId: 'actor-id',
					archiveReason: 'Completed pilot'
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.archiveCampaignSeries('series/id', { reason: 'Completed pilot' });

		expect(calls).toEqual([
			{
				path: '/campaign-series/series%2Fid/archive',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({ reason: 'Completed pilot' })
				}
			}
		]);
	});

	it('restores campaign series by id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					id: 'series/id',
					archived: false,
					updatedAt: '2026-05-11T13:30:00Z',
					archivedAt: null,
					archivedByUserId: null,
					archiveReason: null
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.restoreCampaignSeries('series/id');

		expect(calls).toEqual([
			{
				path: '/campaign-series/series%2Fid/restore',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({})
				}
			}
		]);
	});

	it('closes campaign by series and campaign id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					id: 'campaign/id',
					status: 'closed',
					updatedAt: '2026-05-11T14:30:00Z',
					closedAt: '2026-05-11T14:30:00Z',
					closedByUserId: 'actor-id',
					closeReason: 'Collection complete'
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.closeCampaign('series/id', 'campaign/id', { reason: 'Collection complete' });

		expect(calls).toEqual([
			{
				path: '/campaign-series/series%2Fid/campaigns/campaign%2Fid/close',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify({ reason: 'Collection complete' })
				}
			}
		]);
	});

	it('previews respondent rule by series and campaign id', async () => {
		const calls: Array<{ path: string; init?: RequestInit }> = [];
		const request = {
			rule: JSON.stringify({ kind: 'manager_of_target' }),
			targetSubjectId: 'subject/id',
			groupId: null,
			maxRows: 25
		};
		const api = createProductApi({
			request: async <T>(path: string, init?: RequestInit): Promise<T> => {
				calls.push({ path, init });
				return {
					campaignSeriesId: 'series/id',
					campaignId: 'campaign/id',
					ruleKind: 'manager_of_target',
					role: 'manager',
					summary: {
						targetCount: 1,
						respondentCount: 1,
						assignmentPairCount: 1,
						skippedCount: 0,
						warningCount: 0,
						truncated: false
					},
					rows: [],
					warnings: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.previewRespondentRule('series/id', 'campaign/id', request);

		expect(calls).toEqual([
			{
				path: '/campaign-series/series%2Fid/campaigns/campaign%2Fid/respondent-rule-preview',
				init: {
					method: 'POST',
					headers: {
						'content-type': 'application/json'
					},
					body: JSON.stringify(request)
				}
			}
		]);
	});

	it('requests campaign series hub by id', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return {
					id: 'series-id',
					name: 'Quarterly pulse',
					createdAt: '2026-05-01T08:00:00Z',
					updatedAt: '2026-05-02T09:00:00Z',
					totals: {
						campaignCount: 0,
						liveCampaignCount: 0,
						submittedResponseCount: 0,
						scoreCount: 0,
						exportArtifactCount: 0
					},
					governance: {
						consentStatus: 'not_configured',
						retentionStatus: 'not_configured',
						disclosureStatus: 'not_configured',
						scoringStatus: 'not_configured'
					},
					campaigns: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.getCampaignSeriesHub('series-id');

		expect(calls).toEqual(['/campaign-series/series-id']);
	});

	it('encodes campaign series hub id path segments', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return {
					id: 'series id/with slash',
					name: 'Quarterly pulse',
					createdAt: '2026-05-01T08:00:00Z',
					updatedAt: '2026-05-02T09:00:00Z',
					totals: {
						campaignCount: 0,
						liveCampaignCount: 0,
						submittedResponseCount: 0,
						scoreCount: 0,
						exportArtifactCount: 0
					},
					governance: {
						consentStatus: 'not_configured',
						retentionStatus: 'not_configured',
						disclosureStatus: 'not_configured',
						scoringStatus: 'not_configured'
					},
					campaigns: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.getCampaignSeriesHub('series id/with slash');

		expect(calls).toEqual(['/campaign-series/series%20id%2Fwith%20slash']);
	});

	it('requests campaign series setup workspace by id', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return {
					series: {
						id: 'series-id',
						name: 'Quarterly pulse',
						createdAt: '2026-05-01T08:00:00Z',
						updatedAt: '2026-05-02T09:00:00Z'
					},
					summary: {
						campaignCount: 0,
						liveCampaignCount: 0,
						missingPrerequisiteCount: 6
					},
					selectedCampaign: null,
					template: null,
					scoring: null,
					policies: {
						consent: { id: null, version: null, status: 'not_configured' },
						retention: { id: null, version: null, status: 'not_configured' },
						disclosure: { id: null, version: null, status: 'not_configured' }
					},
					readiness: { campaignId: null, status: 'not_available', ready: false },
					missingPrerequisites: [],
					campaigns: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.getCampaignSeriesSetupWorkspace('series id/with slash');

		expect(calls).toEqual(['/campaign-series/series%20id%2Fwith%20slash/setup-workspace']);
	});

	it('requests campaign series operations workspace by id', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return {
					series: {
						id: 'series-id',
						name: 'Quarterly pulse',
						createdAt: '2026-05-01T08:00:00Z',
						updatedAt: '2026-05-02T09:00:00Z'
					},
					summary: {
						campaignCount: 0,
						liveCampaignCount: 0,
						openLinkAssignmentCount: 0,
						queuedInvitationCount: 0,
						sentInvitationCount: 0,
						failedInvitationCount: 0,
						deliveryAttemptCount: 0,
						submittedResponseCount: 0,
						missingPrerequisiteCount: 5
					},
					selectedCampaign: null,
					missingPrerequisites: [],
					campaigns: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.getCampaignSeriesOperationsWorkspace('series id/with slash');

		expect(calls).toEqual(['/campaign-series/series%20id%2Fwith%20slash/operations-workspace']);
	});

	it('requests campaign series reports workspace by id', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return {
					series: {
						id: 'series-id',
						name: 'Quarterly pulse',
						createdAt: '2026-05-01T08:00:00Z',
						updatedAt: '2026-05-02T09:00:00Z'
					},
					summary: {
						campaignCount: 0,
						liveCampaignCount: 0,
						reportableCampaignCount: 0,
						submittedResponseCount: 0,
						scoreCount: 0,
						exportArtifactCount: 0,
						visibleScoreCount: 0,
						suppressedScoreCount: 0,
						missingPrerequisiteCount: 5
					},
					selectedCampaign: null,
					missingPrerequisites: [],
					campaigns: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.getCampaignSeriesReportsWorkspace('series id/with slash');

		expect(calls).toEqual(['/campaign-series/series%20id%2Fwith%20slash/reports-workspace']);
	});

	it('requests campaign series reports widget manifest by encoded id', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return sampleReportsWidgetManifest as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		const result = await api.getCampaignSeriesReportsWidgetManifest('series id/with slash');

		expect(calls).toEqual(['/campaign-series/series%20id%2Fwith%20slash/reports-widget-manifest']);
		expect(result.widgets[0]).toMatchObject({
			id: 'score-coverage',
			kind: 'score-coverage-summary/v1',
			state: 'ready',
			size: 'half'
		});
	});

	it('requests campaign series waves workspace by id', async () => {
		const calls: string[] = [];
		const api = createProductApi({
			request: async <T>(path: string): Promise<T> => {
				calls.push(path);
				return {
					series: {
						id: 'series-id',
						name: 'Quarterly pulse',
						createdAt: '2026-05-01T08:00:00Z',
						updatedAt: '2026-05-02T09:00:00Z'
					},
					summary: {
						campaignCount: 0,
						liveCampaignCount: 0,
						longitudinalWaveCount: 0,
						submittedWaveCount: 0,
						linkedTrajectoryCount: 0,
						completeTrajectoryCount: 0,
						comparableScoreCount: 0,
						visibleComparisonCount: 0,
						suppressedComparisonCount: 0,
						blockedComparisonCount: 0,
						missingPrerequisiteCount: 5
					},
					selectedBaselineWave: null,
					selectedComparisonWave: null,
					comparison: {
						status: 'blocked',
						disclosureState: 'not_available',
						compatibilityState: 'not_available',
						interpretationStatus: 'not_available',
						disclosureKMin: null,
						linkedPairCount: 0,
						visibleScoreCount: 0,
						suppressedScoreCount: 0,
						blockedScoreCount: 0
					},
					missingPrerequisites: [],
					waves: []
				} as T;
			},
			requestText: async () => {
				throw new Error('not used');
			}
		});

		await api.getCampaignSeriesWavesWorkspace('series id/with slash');

		expect(calls).toEqual(['/campaign-series/series%20id%2Fwith%20slash/waves-workspace']);
	});
});

const sampleWorkspaceOverview: WorkspaceOverviewResponse = {
	tenantId: 'tenant-id',
	totals: {
		campaignSeriesCount: 1,
		campaignCount: 2,
		liveCampaignCount: 1,
		submittedResponseCount: 14,
		exportArtifactCount: 3
	},
	commandCenter: {
		items: []
	},
	studyCollections: {
		sampleStudies: [],
		ownStudies: []
	},
	recentSeries: [
		{
			id: 'series-id',
			name: 'Quarterly pulse',
			studyKind: 'own',
			isSample: false,
			sampleScenario: null,
			readOnlyReason: null,
			createdAt: '2026-05-01T08:00:00Z',
			updatedAt: '2026-05-02T09:00:00Z',
			campaignCount: 2,
			liveCampaignCount: 1,
			submittedResponseCount: 14,
			latestLaunchAt: '2026-05-02T10:00:00Z',
			latestSubmissionAt: '2026-05-03T11:00:00Z',
			readinessStatus: 'proof_only',
			archived: false,
			archivedAt: null,
			archivedByUserId: null,
			archiveReason: null
		}
	]
};

const sampleTenantSettings: TenantSettingsWorkspaceResponse = {
	profile: {
		tenantId: 'tenant-id',
		slug: 'occupational-health-lab',
		name: 'Occupational Health Lab',
		region: 'eu',
		defaultLocale: 'en',
		status: 'active',
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-12T09:30:00Z'
	},
	counts: {
		campaignSeriesCount: 3,
		campaignCount: 9,
		liveCampaignCount: 2,
		submittedResponseCount: 128,
		subjectCount: 42,
		subjectGroupCount: 6,
		tenantMemberCount: 4,
		tenantRoleCount: 3,
		exportArtifactCount: 5
	},
	managementLinks: [
		{
			id: 'campaign-series',
			label: 'Campaign series',
			description: 'Manage tenant study series and selected-series workspaces.',
			route: '/app/campaign-series'
		}
	]
};

const sampleExportArtifactLibrary: ExportArtifactLibraryResponse = {
	tenantId: 'tenant-id',
	summary: {
		totalCount: 1,
		downloadableCount: 1,
		failedCount: 0,
		pendingCount: 0
	},
	artifacts: [
		{
			id: 'artifact-id',
			targetKind: 'campaign',
			targetId: 'campaign-id',
			targetLabel: 'Baseline wave',
			campaignId: 'campaign-id',
			campaignName: 'Baseline wave',
			artifactType: 'report_proof_csv_codebook',
			status: 'succeeded',
			format: 'csv_codebook',
			fileName: 'baseline-report.csv',
			rowCount: 12,
			byteSize: 2048,
			checksumSha256: 'a'.repeat(64),
			createdAt: '2026-05-16T08:00:00Z',
			completedAt: '2026-05-16T08:00:03Z',
			startedAt: null,
			failedAt: null,
			expiresAt: null,
			deletedAt: null,
			failureReasonCode: null,
			canDownload: true,
			campaignStatus: 'closed',
			campaignClosedAt: '2026-05-16T09:00:00Z',
			dataFinality: 'closed_wave'
		}
	]
};

const sampleTenantMember = {
	userId: '44444444-4444-4444-8444-444444444444',
	email: 'new.member@example.test',
	locale: 'hr',
	createdAt: '2026-05-12T08:00:00Z',
	lastLoginAt: null,
	identityStatus: 'pending_provider_link',
	status: 'invited',
	statusLabel: 'Invited',
	roles: [
		{
			roleId: '55555555-5555-4555-8555-555555555555',
			code: 'analyst',
			name: 'Analyst',
			scopeType: 'tenant',
			scopeId: null,
			grantedAt: '2026-05-12T08:30:00Z'
		}
	],
	permissions: ['export.read']
};

const sampleSubject = {
	id: 'subject-id',
	displayName: 'Ana Analyst',
	email: 'ana@example.test',
	externalId: 'emp-001',
	locale: 'hr',
	attributes: '{"title":"Analyst"}',
	managerSubjectId: null,
	managerDisplayName: null,
	directReportCount: 0,
	groups: []
};

const sampleSubjectGroup = {
	id: 'group-id',
	type: 'department',
	name: 'Research',
	parentGroupId: null,
	attributes: '{}',
	memberCount: 0
};

const sampleReportsWidgetManifest: CampaignSeriesReportsWidgetManifestResponse = {
	campaignSeriesId: 'series-id',
	surface: 'reports',
	surfaceVersion: 'reports-widget-manifest/v1',
	layout: {
		kind: 'dashboard-grid/v1',
		density: 'standard'
	},
	widgets: [
		{
			id: 'score-coverage',
			kind: 'score-coverage-summary/v1',
			title: 'Score coverage',
			size: 'half',
			state: 'ready',
			message: null,
			data: {
				submittedResponseCount: 14,
				scoredSubmittedResponseCount: 14,
				unscoredSubmittedResponseCount: 0,
				notConfiguredSubmittedResponseCount: 0,
				campaignsWithScoringRuleCount: 1,
				campaignsWithoutScoringRuleCount: 0,
				latestScoringActivityAt: '2026-05-15T08:30:00Z',
				status: 'complete',
				guidance: 'All submitted responses have successful scoring activity.'
			},
			dataSource: null,
			actions: []
		}
	]
};

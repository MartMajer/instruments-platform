import type { AuthSessionResponse } from '$lib/api/setup';
import type { AppLocale } from '$lib/i18n/localization';
import { setupManagePermission, teamManagePermission } from './auth-context';

export type SessionProfileTechnicalRow = {
	label: string;
	value: string;
	mono?: boolean;
};

export type SessionProfileView = {
	accountLabel: string;
	permissionSummary: string;
	permissionBadges: string[];
	technicalRows: SessionProfileTechnicalRow[];
};

const reportPermissionPrefixes = ['report.', 'export.'];

type SessionProfileCopy = {
	signedInPlatformUser: string;
	setup: string;
	team: string;
	reports: string;
	tenantMember: string;
	workspaceAdministrationAndReportingAccess: string;
	workspaceAdministrationAccess: string;
	studySetupAccess: string;
	teamAdministrationAccess: string;
	reportingAccess: string;
	access: string;
	and: string;
};

const sessionProfileCopy: Record<AppLocale, SessionProfileCopy> = {
	en: {
		signedInPlatformUser: 'Signed-in platform user',
		setup: 'Setup',
		team: 'Team',
		reports: 'Reports',
		tenantMember: 'Tenant member',
		workspaceAdministrationAndReportingAccess: 'Workspace administration and reporting access',
		workspaceAdministrationAccess: 'Workspace administration access',
		studySetupAccess: 'Study setup access',
		teamAdministrationAccess: 'Team administration access',
		reportingAccess: 'Reporting access',
		access: 'access',
		and: 'and'
	},
	'hr-HR': {
		signedInPlatformUser: 'Prijavljeni korisnik platforme',
		setup: 'Postavljanje',
		team: 'Tim',
		reports: 'Izvještaji',
		tenantMember: 'Član radnog prostora',
		workspaceAdministrationAndReportingAccess: 'Administracija radnog prostora i pristup izvještajima',
		workspaceAdministrationAccess: 'Administracija radnog prostora',
		studySetupAccess: 'Pristup postavljanju studija',
		teamAdministrationAccess: 'Administracija tima',
		reportingAccess: 'Pristup izvještajima',
		access: 'pristup',
		and: 'i'
	}
};

export function toSessionProfileView(
	session: AuthSessionResponse,
	locale: AppLocale = 'en'
): SessionProfileView {
	const copy = sessionProfileCopy[locale];
	const permissionBadges = toPermissionBadges(session.permissions, copy);

	return {
		accountLabel: session.email?.trim() || copy.signedInPlatformUser,
		permissionSummary: toPermissionSummary(session.permissions, permissionBadges, copy),
		permissionBadges,
		technicalRows: []
	};
}

function toPermissionBadges(permissions: string[], copy: SessionProfileCopy) {
	const badges: string[] = [];

	if (permissions.includes(setupManagePermission)) {
		badges.push(copy.setup);
	}

	if (permissions.includes(teamManagePermission)) {
		badges.push(copy.team);
	}

	if (
		permissions.some((permission) =>
			reportPermissionPrefixes.some((prefix) => permission.startsWith(prefix))
		)
	) {
		badges.push(copy.reports);
	}

	return badges.length > 0 ? badges : [copy.tenantMember];
}

function toPermissionSummary(
	permissions: string[],
	permissionBadges: string[],
	copy: SessionProfileCopy
) {
	const hasSetupManagement = permissions.includes(setupManagePermission);
	const hasTeamManagement = permissions.includes(teamManagePermission);
	const hasReports = permissionBadges.includes(copy.reports);

	if (hasSetupManagement && hasTeamManagement) {
		return hasReports
			? copy.workspaceAdministrationAndReportingAccess
			: copy.workspaceAdministrationAccess;
	}

	if (hasSetupManagement) {
		return copy.studySetupAccess;
	}

	if (hasTeamManagement) {
		return copy.teamAdministrationAccess;
	}

	const summaryBadges = permissionBadges;
	if (summaryBadges.length === 1) {
		const [badge] = summaryBadges;
		if (badge === copy.reports) {
			return copy.reportingAccess;
		}

		return `${badge} ${copy.access}`;
	}

	const [lastBadge] = summaryBadges.slice(-1);
	const leadingBadges = summaryBadges.slice(0, -1);
	return `${leadingBadges.join(', ')} ${copy.and} ${lastBadge} ${copy.access}`;
}

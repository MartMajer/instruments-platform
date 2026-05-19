import type { AuthSessionResponse } from '$lib/api/setup';
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

export function toSessionProfileView(session: AuthSessionResponse): SessionProfileView {
	const permissionBadges = toPermissionBadges(session.permissions);

	return {
		accountLabel: session.email?.trim() || 'Signed-in platform user',
		permissionSummary: toPermissionSummary(session.permissions, permissionBadges),
		permissionBadges,
		technicalRows: []
	};
}

function toPermissionBadges(permissions: string[]) {
	const badges: string[] = [];

	if (permissions.includes(setupManagePermission)) {
		badges.push('Setup');
	}

	if (permissions.includes(teamManagePermission)) {
		badges.push('Team');
	}

	if (
		permissions.some((permission) =>
			reportPermissionPrefixes.some((prefix) => permission.startsWith(prefix))
		)
	) {
		badges.push('Reports');
	}

	return badges.length > 0 ? badges : ['Tenant member'];
}

function toPermissionSummary(permissions: string[], permissionBadges: string[]) {
	const hasSetupManagement = permissions.includes(setupManagePermission);
	const hasTeamManagement = permissions.includes(teamManagePermission);
	const hasReports = permissionBadges.includes('Reports');

	if (hasSetupManagement && hasTeamManagement) {
		return hasReports ? 'Workspace administration and reporting access' : 'Workspace administration access';
	}

	if (hasSetupManagement) {
		return 'Study setup access';
	}

	if (hasTeamManagement) {
		return 'Team administration access';
	}

	const summaryBadges = permissionBadges;
	if (summaryBadges.length === 1) {
		const [badge] = summaryBadges;
		if (badge === 'Reports') {
			return 'Reporting access';
		}

		return `${badge} access`;
	}

	const [lastBadge] = summaryBadges.slice(-1);
	const leadingBadges = summaryBadges.slice(0, -1);
	return `${leadingBadges.join(', ')} and ${lastBadge} access`;
}

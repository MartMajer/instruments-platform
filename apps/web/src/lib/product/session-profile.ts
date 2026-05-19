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
		badges.push('Setup management');
	}

	if (permissions.includes(teamManagePermission)) {
		badges.push('Team management');
	}

	if (
		permissions.some((permission) =>
			reportPermissionPrefixes.some((prefix) => permission.startsWith(prefix))
		)
	) {
		badges.push('Report access');
	}

	return badges.length > 0 ? badges : ['Tenant member'];
}

function toPermissionSummary(permissions: string[], permissionBadges: string[]) {
	const managementBadges = [];
	if (permissions.includes(setupManagePermission)) {
		managementBadges.push('Setup management');
	}

	if (permissions.includes(teamManagePermission)) {
		managementBadges.push('team management');
	}

	const summaryBadges = managementBadges.length > 0 ? managementBadges : permissionBadges;
	if (summaryBadges.length === 1) {
		const [badge] = summaryBadges;
		return badge.toLowerCase().endsWith(' access') ? badge : `${badge} access`;
	}

	const [lastBadge] = summaryBadges.slice(-1);
	const leadingBadges = summaryBadges.slice(0, -1);
	return `${leadingBadges.join(', ')} and ${lastBadge} access`;
}

import type { AppLocale } from './localization';

type LocaleDictionary<T> = Record<AppLocale, T>;

const en = {
	common: {
		product: 'Product',
		createWorkspace: 'Create workspace',
		signIn: 'Sign in',
		open: 'Open',
		retry: 'Retry',
		email: 'Email',
		privateBeta: 'Private beta'
	},
	publicEntry: {
		metaTitle: 'Instruments Platform | Research study operations',
		metaDescription:
			'EU-hosted private-beta workspace for study setup, response collection, results review, and exports.',
		brandSubtitle: 'Research studies and wellbeing programs',
		navAria: 'Product entry actions',
		mobileNavAria: 'Mobile product entry actions',
		menu: 'Menu',
		openMenu: 'Open menu',
		closeMenu: 'Close menu',
		workflow: 'Workflow',
		trustModel: 'Trust model',
		heroKicker: 'EU-hosted workspace for research and wellbeing studies',
		heroTitle: 'Run research studies from setup to defensible results.',
		heroBody:
			'Build questionnaires, collect anonymous or identified responses, review scoring context, and export datasets without stitching together forms, spreadsheets, scripts, and screenshots.',
		previewAria: 'Product preview',
		previewChrome: 'workspace / study operations',
		previewCollect: 'Collect',
		previewResults: 'Results',
		selectedStudy: 'Selected study',
		previewStudyName: 'Workplace wellbeing pulse',
		liveCollection: 'Live collection',
		responseSignal: 'Response signal',
		responseProgress: '412 responses · 33% complete',
		prepare: 'Prepare',
		launchChecklist: 'Launch checklist',
		prepareBody: 'Questionnaire, scoring, audience, and collection settings stay visible before launch.',
		export: 'Export',
		datasetCodebook: 'Dataset + codebook',
		exportBody: 'Exports keep source, finality, and suppression context attached to the file.',
		trustAria: 'Trust model',
		workflowRibbon: 'Setup, collection, scoring, reports, and exports',
		access: 'Access',
		accessRibbon: 'Tenant-scoped authenticated workspaces',
		dataControls: 'Data controls',
		dataControlsRibbon: 'Consent, retention, finality, and export provenance',
		productStage: 'Product stage',
		productStageRibbon: 'Private beta with staged onboarding',
		workspaceOverview: 'Workspace overview',
		suiteTitle: 'See what is ready, blocked, live, or ready to export.',
		suiteBody:
			"Keep every study's next action visible: preparation gaps, live collection, result review, and export readiness.",
		nextAction: 'Next action',
		nextActionQuestion: 'What should the team do next?',
		appAreas: 'App areas',
		studyStatus: 'Study status',
		workflowTitle: 'A clear path from study design to reusable evidence.',
		portfolio: 'Portfolio',
		portfolioBody: 'Create, compare, and return to active study programs from one workspace.',
		prepareStepBody: 'Define questionnaire, scoring, policies, audience, and launch checks.',
		collectStepBody: 'Open links or invite lists, track response progress, and monitor delivery.',
		reviewStepBody: 'Inspect reports, compare waves, and export datasets with provenance.'
	},
	signIn: {
		metaTitle: 'Sign in | Instruments Platform',
		metaDescription:
			'Find your Instruments Platform workspace before signing in with your sign-in provider.',
		brandSubtitle: 'Workspace sign-in',
		navAria: 'Sign-in actions',
		eyebrow: 'Workspace access',
		title: 'Sign in to your workspace.',
		body:
			'Enter the email used for the workspace. We find the workspace first, then send you to your sign-in provider for password and MFA.',
		stepsAria: 'Sign-in steps',
		stepFind: 'Find workspace',
		stepFindBody: 'Use the same email that owns or belongs to the workspace.',
		stepSignIn: 'Sign in',
		stepSignInBody: 'Your sign-in provider handles password, account selection, and MFA.',
		stepOpenApp: 'Open app',
		stepOpenAppBody:
			'The platform opens only after matching the selected account to workspace membership.',
		formAria: 'Workspace sign-in form',
		panelEyebrow: 'Existing workspace',
		panelTitle: 'Continue with your workspace email',
		createInsteadPrefix: 'Need a new workspace?',
		createInsteadLink: 'Create one instead',
		recentWorkspace: 'Recent workspace found',
		continueAs: (email: string) => `Continue as ${email}, or enter another workspace email below.`,
		continueRecentBody: 'Continue to your recent workspace, or enter another workspace email below.',
		continueRecent: 'Continue to recent workspace',
		openingWorkspace: 'Opening workspace sign-in.',
		findingWorkspace: 'Finding workspace...',
		continueToSignIn: 'Continue to sign in',
		betaBoundaryBody: 'Use demo or owner-controlled data only until production review is closed.',
		workspaceNotFound: 'No workspace exists for this email yet. Create a workspace first.',
		emailInvalid: 'Enter the email used for the workspace.',
		fallbackError: 'We could not find a workspace for this email.'
	},
	register: {
		metaTitle: 'Create workspace | Instruments Platform',
		metaDescription:
			'Create a private beta workspace for Instruments Platform and finish sign-in with your sign-in provider.',
		brandSubtitle: 'Private beta workspace',
		navAria: 'Registration actions',
		eyebrow: 'Private beta access',
		title: 'Create your workspace.',
		body:
			'Use the email that should own the workspace. Password and MFA stay with your sign-in provider; this page only names the workspace and checks beta access.',
		stepsAria: 'Registration steps',
		stepCreate: 'Create account',
		stepCreateBody: 'Enter the email, workspace name, and beta code before opening account setup.',
		stepVerify: 'Verify email',
		stepVerifyBody: 'If your sign-in provider asks for verification, confirm the email and continue here.',
		stepOpen: 'Open the app',
		stepOpenBody: 'The workspace is created from the approved registration and opens with your session.',
		formAria: 'Create workspace form',
		panelEyebrow: 'Workspace signup',
		panelTitle: 'Create account and workspace',
		alreadyHave: 'Already have a workspace?',
		signInInstead: 'Sign in instead',
		checkingAccount: 'Checking account status...',
		chooseStartedAccount: 'Choose the account you started with',
		verifyThenSignIn: 'Verify email, then sign in',
		mismatchBody:
			'The selected sign-in account did not match the workspace email you entered. Sign out completely if the browser keeps choosing the wrong account.',
		verifyBody:
			'Open the verification email from your sign-in provider, then retry registration sign-in with the same email.',
		retryRegistration: 'Retry registration sign-in',
		startOver: 'Start over',
		workspaceName: 'Workspace name',
		workspacePlaceholder: 'Your lab, team, or company',
		betaAccessCode: 'Beta access code',
		accessCodePlaceholder: 'Access code',
		openingAccount: 'Opening account setup.',
		openingAccountButton: 'Opening account setup...',
		createAccount: 'Create account',
		useDifferentEmail: 'Use a different email',
		accountReady: (email: string) => `Account ready: ${email}. This email will manage the workspace.`,
		workspaceCreated: 'Workspace created. Opening your app.',
		creatingWorkspace: 'Creating workspace...',
		boundaryTitle: 'Private beta',
		boundaryBody:
			'Real participant use starts only after the required legal and deployment review for the workspace.',
		sessionError:
			'We could not confirm the account step. Continue with account setup again before creating the workspace.',
		disabled:
			'Private beta sign-up is not open on this environment. Sign in if you already have a workspace.',
		expired: 'Your account step expired. Continue with account setup again, then create the workspace.',
		forbidden:
			'This account cannot create a workspace. Sign out and use an approved beta account, or ask for beta access.',
		conflict: 'This account or workspace is already set up. Sign in instead.',
		invalidCode: 'That access code does not match the private beta list.',
		emailExists: 'A workspace already exists for this email. Sign in instead.',
		organizationInvalid: 'Enter the workspace or organization name you want to use.',
		fallbackError: 'We could not create the workspace. Check the beta access code and try again.'
	},
	workspaceHome: {
		eyebrow: 'Workspace',
		title: 'Home',
		description:
			'Pick the next useful task: create a study, continue work, invite people, manage audiences, or download results.',
		loading: 'Loading workspace overview',
		errorTitle: 'Workspace overview unavailable',
		retry: 'Retry overview',
		start: 'Start',
		setupWorkspace: 'Set up your workspace',
		chooseNext: 'Choose what to do next',
		firstRunActions: [
			'Create first study',
			'Invite team',
			'Set up directory',
			'Review instruments'
		],
		firstRunActionStatuses: ['Start here', 'Access', 'People', 'Library'],
		firstRunActionDescriptions: [
			'Start a real study and continue through setup, collection, and results.',
			'Prepare tenant member access before sharing the first sign-in link.',
			'Create people, groups, memberships, and manager links for targeting.',
			'Confirm which instruments are available before starting production study work.'
		],
		nextActions: 'Next actions',
		openStudies: 'Open Studies',
		examples: 'Examples',
		sampleStudies: 'Sample studies',
		yourWork: 'Your work',
		yourStudies: 'Your studies',
		workspaceOverview: 'Workspace overview'
	},
	portfolio: {
		eyebrow: 'Study workspace',
		title: 'Studies',
		description: 'Create a study or open an existing one. Samples stay separated from real workspace studies.',
		loading: 'Loading studies',
		errorTitle: 'Studies unavailable',
		retry: 'Retry studies',
		guidedDesign: 'Guided study design',
		startBlueprint: 'Choose a study starting point',
		selectedStartingPoint: 'Selected starting point',
		studyModelTitle: 'What this creates',
		studyModelBody:
			'This creates a study container first. The selected starting point only seeds Setup; you can edit the questionnaire, result outputs, recipients, and waves before launch.',
		studyModelStudy: 'Study',
		studyModelStudyBody:
			'The durable study container for setup, collection waves, results, and export files.',
		studyModelStartingPoint: 'Starting point',
		studyModelStartingPointBody:
			'Seeds the first Setup draft. It is not the final questionnaire and not a reusable instrument record.',
		studyModelSetup: 'Setup',
		studyModelSetupBody:
			'Where you turn the starting point into the questionnaire, results setup, recipient plan, and launch check.',
		studyName: 'Study name',
		continueSetup: 'Continue to guided setup',
		creating: 'Creating...',
		readOnlyAccess: 'Read-only access',
		readOnlyBody: 'Creating and changing studies requires setup management access.',
		studyList: 'Study list',
		openStudy: 'Open a study',
		searchStudies: 'Search studies',
		searchPlaceholder: 'Search by study name',
		readiness: 'Readiness',
		sort: 'Sort',
		visibility: 'Visibility'
	},
	instruments: {
		eyebrow: 'Instrument library',
		title: 'Instruments',
		description:
			'Review reusable questionnaire sources that can seed a study. The study itself is built inside Setup.',
		loading: 'Loading instrument library',
		errorTitle: 'Instrument library unavailable',
		retry: 'Retry instruments',
		summary: 'Library summary',
		visibleInstruments: 'Visible instruments',
		noInstruments: 'No instruments',
		noInstrumentsBody: 'No tenant-visible instruments are available yet.',
		nextStep: 'Next step',
		createOrOpen: 'Create or open a study',
		studiesBody: 'Select a study to build questionnaires, scoring, audiences, and launch state.'
	},
	exports: {
		eyebrow: 'Exports',
		title: 'Download files',
		description: 'Find CSV and codebook files created from study Results pages.',
		loading: 'Loading export files',
		errorTitle: 'Export files unavailable',
		retry: 'Retry exports',
		files: 'Files',
		downloadable: 'Downloadable files and next use',
		noFiles: 'No export files',
		noFilesBody: 'Create an export from a study results page after results are available.',
		reports: 'Reports',
		counts: 'Export counts',
		countsBody: 'Use these counts when checking whether files are ready, pending, or failed.'
	},
	directory: {
		eyebrow: 'Audience directory',
		title: 'People and groups',
		description: 'Create respondents, reusable audiences, and manager links for study targeting.',
		accessTitle: 'Directory access requires setup management',
		accessMessage: 'Subject directory data is only available to setup managers.',
		setup: 'Directory setup',
		buildAudience: 'Build the audience list first',
		buildAudienceBody:
			'Add people, organize them into groups, then use those groups when choosing a study audience.',
		addPeopleOrGroups: 'Add people or groups',
		people: 'People',
		groups: 'Groups',
		memberships: 'Memberships',
		managerLinks: 'Manager links',
		howUsed: 'How directory data is used',
		csvImport: 'CSV import',
		csvTitle: 'Preview, then import people and groups',
		peopleInWorkspace: 'People in this workspace',
		audienceGroups: 'Audience groups',
		addRecords: 'Add records',
		membershipManager: 'Membership and manager',
		countsAria: 'People and targeting counts',
		csvBody: 'Use this when a study audience is already prepared in a spreadsheet. First preview the rows so you can confirm who will be created, updated, grouped, or rejected. Apply only after the preview looks right.',
		downloadTemplate: 'Download template',
		csvFile: 'CSV file',
		csvRows: 'CSV rows',
		csvHelp: 'Required identity: external_id or email. Optional grouping: group_type, group_name, role_in_group. One person can appear in multiple rows when they belong to multiple groups.',
		previewing: 'Previewing...',
		previewCsv: 'Preview CSV',
		applying: 'Applying...',
		applyImport: 'Apply import',
		fixFailedRows: 'Fix the failed rows before applying this Directory import.',
		applyingImport: 'Applying the reviewed Directory import...',
		checkingRows: 'Checking rows without changing Directory records...',
		peopleToCreate: 'People to create',
		peopleCreated: 'People created',
		peopleToUpdate: 'People to update',
		peopleUpdated: 'People updated',
		groupsToCreate: 'Groups to create',
		groupsCreated: 'Groups created',
		membershipsToAdd: 'Memberships to add',
		membershipsAdded: 'Memberships added',
		membershipsPresent: 'Memberships already present',
		peopleDirectoryAria: 'People directory',
		directoryGraphCounts: 'Directory graph counts',
		refresh: 'Refresh',
		email: 'Email',
		roleInGroup: 'Role in group',
		directoryRelationshipsAria: 'Directory relationships',
		noSubjectSelected: 'No person selected',
		rootGroup: 'Root group',
		saving: 'Saving...',
		savePerson: 'Save person',
		addMembership: 'Add membership',
		saveManager: 'Save manager'
	},
	team: {
		eyebrow: 'Workspace access',
		title: 'Team',
		description: 'Invite teammates, assign roles, and confirm who can manage studies or access results.',
		loadingOverview: 'Loading team access overview',
		tenantTeam: 'Tenant team',
		overviewTitle: 'Team access overview',
		overviewBody: 'Who can enter the tenant, prepare studies, and manage access.',
		prepareTitle: 'Prepare member access, then share sign-in',
		prepareBody:
			'Add the email, choose a role, then share the generated sign-in link from the roster. Passwords and MFA stay in Auth0.',
		readOnlyTitle: 'Read-only access',
		readOnlyBody: 'Member preparation and role changes require team management access.',
		rosterTitle: 'Members and roles',
		memberSingular: 'member',
		memberPlural: 'members',
		teamOverviewCountsAria: 'Team access overview counts',
		capabilityCoverageAria: 'Team capability coverage',
		tenantRolesUnavailable: 'Tenant roles unavailable',
		retryRoles: 'Retry roles',
		memberEmail: 'Member email',
		memberRole: 'Member role',
		memberLocale: 'Member locale',
		adding: 'Adding...',
		addMember: 'Add member',
		loadingRoles: 'Loading tenant roles.',
		pendingNoticeSuffix: 'The roster marks the member pending until the first matching Auth0 sign-in.',
		readOnlyAria: 'Read-only team access',
		teamRoster: 'Team roster',
		loadingMembers: 'Loading team members',
		teamMembersUnavailable: 'Team members unavailable',
		retryMembers: 'Retry members',
		rosterCountsAria: 'Tenant member roster counts',
		membersLabel: 'Members',
		noMembersTitle: 'No tenant members',
		noMembersBody: 'No active tenant role assignments are available for this tenant.',
		currentUser: 'Current user',
		localeLabel: 'Locale',
		created: 'Created',
		lastLogin: 'Last login',
		roles: 'Roles',
		capabilities: 'Capabilities',
		firstSignIn: 'First sign-in',
		firstSignInBody: (email: string) => `Send this link to ${email}. They stay pending until Auth0 returns the same email for this workspace.`,
		openLink: 'Open link',
		copyLink: 'Copy link',
		copied: 'Copied',
		roleFor: (email: string) => `Role for ${email}`,
		changeRoleAria: (email: string) => `Change role for ${email}`,
		changeRole: 'Change role',
		saving: 'Saving...'
	},
	settings: {
		eyebrow: 'Workspace',
		title: 'Workspace settings',
		description: 'Manage workspace access, people, data, and study defaults from one place.',
		loading: 'Loading tenant settings',
		errorTitle: 'Workspace settings unavailable',
		retry: 'Retry settings',
		hub: 'Settings hub',
		whatManage: 'What can you manage here?',
		whatManageBody:
			'Workspace-level settings are being assembled here. For now, use these shortcuts to manage the active areas of the product.',
		teamAccess: 'Team access',
		teamBody: 'Invite workspace members, review pending access, and manage workspace roles.',
		directoryBody: 'Manage people, groups, and hierarchy data used for study audiences.',
		studySetup: 'Study setup',
		studySetupBody: 'Create or continue studies, questionnaires, collection waves, and results setup.',
		exportsBody: 'Review generated export files and download analysis-ready outputs.',
		workspaceDetails: 'Workspace details',
		profile: 'Workspace profile',
		scale: 'Workspace scale',
		footprint: 'Current footprint',
		directoryShortcut: 'Directory',
		exportsShortcut: 'Exports',
		shortcutsAria: 'Workspace setting shortcuts',
		workspaceDetailsAria: 'Workspace details',
		profileDetailsAria: 'Workspace profile details',
		countsAria: 'Workspace counts'
	},
	respondent: {
		metaFallback: 'Respondent survey',
		loadingSurvey: 'Loading survey',
		surveyUnavailable: 'Survey unavailable',
		tryAgain: 'Try again',
		responseReceipt: 'Response receipt',
		participantCode: 'Participant code',
		continue: 'Continue',
		reviewKicker: 'Review',
		reviewTitle: 'Review response',
		savedAnswers: (count: number) => count + ' ' + (count === 1 ? 'answer' : 'answers') + ' saved.',
		session: 'Session',
		backToEdit: 'Back to edit',
		submitReviewed: 'Submit reviewed response',
		saveAndReview: 'Save and review',
		linkUnavailable: 'This link is no longer available.',
		requiredConsent: 'Required consent grants must be accepted before continuing.',
		participantCodeRequired: 'Participant code is required before continuing.',
		sessionNotReady: 'Response session is not ready.',
		saveBeforeSubmit: 'Save answers before submitting.',
		responseSessionUnavailable: 'This response session is no longer available.',
		requestFailed: 'Request failed.',
		questionRequiresAnswer: (code: string) => code + ' requires an answer.',
		questionMustBeNumber: (code: string) => code + ' must be a number.',
		questionBetween: (code: string, min: number, max: number) =>
			code + ' must be between ' + min + ' and ' + max + '.'
	},
	unsubscribe: {
		metaTitle: 'Unsubscribe from study invitations - Instruments Platform',
		kicker: 'Study invitation email',
		title: 'Unsubscribe from future invitations',
		body:
			'Use this page only if you want this email address added to the workspace do-not-contact list for study invitation emails.',
		button: 'Unsubscribe this email address',
		submitting: 'Applying your do-not-contact request...',
		done:
			'This email address has been added to the workspace do-not-contact list for future study invitation emails. You can close this page.',
		retry: 'Try again',
		fallbackError:
			'This invitation could not be unsubscribed. The link may be invalid or already removed.'
	},
	selectedStudy: {
		overview: {
			eyebrow: 'Selected study',
			title: 'Overview',
			description:
				'See where this study stands, then continue setup, collection, results, or wave comparison.',
			ariaLabel: 'Selected study overview',
			loading: 'Loading study overview',
			errorTitle: 'Study overview unavailable',
			retry: 'Retry overview',
			missingId: 'Study id is missing.',
			unavailableFallback: 'Selected study overview could not be loaded.',
			restoreFailed: 'Study could not be restored.',
			duplicateFailed: 'Sample study could not be duplicated.',
			selectedStudy: 'Selected study',
			studyDetails: 'Study details',
			statusRecords: 'Status and records',
			statusDescription: 'Review readiness, governance, and campaigns linked to this study.',
			dates: 'Dates',
			datesAria: 'Selected study dates',
			studyModel: 'Study model',
			lifecycle: 'Lifecycle',
			governance: 'Governance',
			governanceAria: 'Governance status',
			policyScoring: 'Policy and scoring status',
			campaigns: 'Campaigns',
			campaignsAria: 'Selected series campaign rows',
			campaignsInStudy: 'Campaigns in this study',
			noCampaigns: 'No campaigns are linked to this series.',
			restore: 'Restore',
			restoring: 'Restoring...',
			readOnly: 'Read-only',
			duplicateAsStudy: 'Duplicate as study',
			duplicating: 'Duplicating...',
			duplicateAria: (title: string) => `Duplicate as study ${title}`
		},
		surfaces: {
			setup: {
				eyebrow: 'Setup',
				title: 'Study setup',
				description: 'Work through the setup steps in order before collection starts.',
				ariaLabel: 'Setup workspace'
			},
			operations: {
				eyebrow: 'Study collection',
				title: 'Collect responses',
				description:
					'Start collection, share respondent access, monitor submissions, and close the response window.',
				ariaLabel: 'Collection workspace'
			},
			reports: {
				eyebrow: 'Study results',
				title: 'Review results',
				description:
					'Review available findings, score coverage, limitations, and export next use for this study.',
				ariaLabel: 'Results workspace'
			},
			waves: {
				eyebrow: 'Wave comparison',
				title: 'Waves',
				description:
					'Create follow-up collection waves, then compare linked longitudinal change when the study is ready.',
				ariaLabel: 'Waves and linked trajectories'
			}
		},
		setupBody: {
			progressAriaLabel: 'Study setup progress',
			progressKicker: 'Study setup',
			progressTitle: 'Study setup progress',
			progressBody:
				'Build the study in order: source, questionnaire, results, wave, recipients, then launch readiness.',
			readOnlyTitle: 'Read-only access',
			readOnlyBody: 'Setup workflow actions require setup management access.',
			requiredStepsComplete: (completed: number, total: number) =>
				`${completed} of ${total} required steps complete`,
			currentSetupStep: 'Current setup step',
			selectedSetupStep: 'Selected setup step',
			status: {
				blocked: 'Blocked',
				current: 'Current',
				done: 'Done',
				failed: 'Failed',
				pending: 'Pending',
				ready: 'Ready',
				saved: 'Saved',
				working: 'Working'
			},
			questionnaire: {
				paletteTitle: 'Choose an editable question set',
				paletteBody:
					'Start from a structured questionnaire set, then edit questions, response formats, and result outputs for this study.',
				addQuestion: 'Add question',
				saveQuestionnaire: 'Save questionnaire',
				authoringSummary: 'Questionnaire summary',
				blueprintTitle: 'Questionnaire design review',
				studyDimensions: 'Study dimensions',
				questionText: 'Question text',
				answerFormat: 'Answer format',
				respondentPreview: 'Respondent preview',
				errorsLabel: 'Questionnaire errors',
				paletteOptions: {
					blank: {
						label: 'Blank questionnaire',
						category: 'Custom',
						summary: 'Start with empty editable questions and build the instrument yourself.',
						detail: 'Use this when the study does not match a prepared workplace-health template.'
					},
					workload_recovery: {
						label: 'Workload and recovery pulse',
						category: 'Workplace health',
						summary: 'A short editable set for workload pressure, recovery need, and recovery capacity.',
						detail: 'Useful for first occupational-health or ergonomics studies.'
					},
					osh_ergonomics: {
						label: 'OSH / ergonomics',
						category: 'Original editable starter',
						summary: 'Original editable workplace-risk items for posture, repetition, discomfort, and recovery.',
						detail: 'Useful for occupational-safety discovery work. Not a validated named instrument by itself.'
					},
					office_ergonomics: {
						label: 'Office ergonomics',
						category: 'Persona starter',
						summary:
							'Original editable office-work items for workstation fit, screen strain, input-device strain, and interruptions.',
						detail: 'Designed for hybrid or desk-based teams where ergonomics and focus conditions both matter.'
					},
					academic_workload: {
						label: 'Academic workload',
						category: 'Persona starter',
						summary:
							'Original editable items for teaching/research load, admin pressure, supervision clarity, and recovery.',
						detail: 'Useful for professor-led studies and department workload checks. Keep interpretation study-specific.'
					},
					team_climate: {
						label: 'Team climate pulse',
						category: 'Original editable starter',
						summary:
							'Original editable items for role clarity, support, communication, fairness, and psychological safety.',
						detail: 'A compact team-health pulse for repeated waves or a one-off internal review.'
					},
					healthcare_staff_strain: {
						label: 'Healthcare staff strain',
						category: 'Persona starter',
						summary:
							'Original editable items for shift fatigue, emotional load, staffing pressure, handoff clarity, and recovery.',
						detail:
							'Useful for owner-controlled rehearsal and future hospital discovery without clinical or diagnostic claims.'
					},
					burnout_risk: {
						label: 'Burnout risk screen',
						category: 'Workplace health',
						summary: 'A compact editable screen for exhaustion, disengagement, and recovery signals.',
						detail: 'Keeps wording generic and avoids marketing a named proprietary scale.'
					},
					ergonomics_baseline: {
						label: 'Ergonomics baseline',
						category: 'Ergonomics',
						summary: 'A starting point for posture, discomfort, tools, and workstation context.',
						detail: 'Use it when the study starts from workplace setup and physical strain.'
					},
					psychosocial_safety: {
						label: 'Psychosocial safety pulse',
						category: 'Organizational climate',
						summary: 'Editable questions for support, clarity, workload fairness, and psychological safety.',
						detail: 'Designed for internal improvement, not external diagnosis.'
					}
				}
			},
			scoring: {
				resultsTitle: 'Result outputs',
				resultsBody: 'Define the scores and export columns this questionnaire should produce.',
				saveResults: 'Save results setup',
				errorsLabel: 'Results setup errors'
			},
			wave: {
				responseMode: {
					anonymousLabel: 'Anonymous',
					anonymousLongitudinalLabel: 'Anonymous with repeated participation',
					identifiedLabel: 'Identified',
					anonymousHelp: 'Responses are not linked back to a known person in reporting.',
					anonymousLongitudinalHelp:
						'Respondents remain anonymous in reporting, but repeated waves can be linked for change over time.',
					identifiedHelp: 'Responses can be connected to known respondents for operational follow-up.'
				}
			},
			recipients: {
				audienceRules: {
					selfLabel: 'Each recipient answers for themselves',
					managerLabel: 'Managers answer for their team',
					externalEmailsLabel: 'One-time email import',
					selfHelp: 'One saved recipient creates one invitation.',
					managerHelp: 'Managers receive invitations for the people in their reporting scope.',
					externalEmailsHelp: 'Paste external email addresses for this wave without adding them to the directory.'
				},
				roles: {
					respondent: 'Respondent',
					manager: 'Manager',
					external: 'External recipient'
				},
				warnings: {
					audienceMissing:
						'Campaign audience has no active members; preview uses all active tenant subjects.',
					empty: 'Preview did not resolve any respondents.',
					truncated: 'Preview is truncated.'
				}
			}
		},
		setupWorkflow: {
			stepNumber: (number: number) => `${number}`,
			defaultWaveName: (number: number) => `Wave ${number}`,
			steps: {
				instrument: {
					title: 'Study source',
					description:
						'Confirm reusable or imported source content. This seeds the questionnaire; it is not the study itself.'
				},
				template: {
					title: 'Questionnaire',
					description: 'Build the saved question set respondents will answer for this study.'
				},
				scoring: {
					title: 'Results setup',
					description:
						'Choose which questionnaire answers become study results and how missing answers are handled.'
				},
				campaign: {
					title: 'Wave and recipients',
					description: 'Prepare the collection round, response mode, and recipients for this study.'
				},
				readiness: {
					title: 'Launch check',
					description:
						'Check the questionnaire, results setup, wave, recipients, and policies before collection starts.'
				}
			},
			disabled: {
				confirmInstrument: 'Confirm the study source first.',
				saveQuestionnaire: 'Save the questionnaire first.',
				createCollectionWave: 'Create the collection wave first.'
			},
			pathDisplay: {
				done: 'Done',
				current: 'Current',
				selected: 'Selected',
				next: 'Next',
				blocked: 'Blocked'
			},
			launchState: {
				createWaveFirstStatus: 'Create collection wave first',
				createWaveFirstNext: 'Create and save the collection wave before checking launch.',
				runLaunchCheckFirst: 'Run launch check first',
				launchPassedSaveRecipients: 'Launch check passed; save recipients for identified access',
				launchPassedChooseAccess: 'Launch check passed; choose public link or save recipients',
				saveRecipientsForIdentified:
					'Save recipients below before launch so Collection can create identified access.',
				openCollectionOrSaveRecipients:
					'Open Collection to launch with a public link, or save recipients below before launch.',
				launchPassedWithRecipients: 'Launch check passed with saved recipients',
				openCollectionStartSavedRecipients:
					'Open Collection to start the wave and send the saved recipients.',
				openCollectionLaunch: 'Open Collection launch',
				runLaunchCheck: 'Run launch check',
				needsAttention: 'Needs attention',
				resolveBeforeCollection:
					'Run the launch check and resolve any listed issues before opening Collection.',
				loadingSavedRecipients: 'Loading saved recipient selection...',
				savedSelections: (selectionCount: number, pairCount: number) =>
					`${selectionCount} ${selectionCount === 1 ? 'selection' : 'selections'} saved, ${pairCount} ${
						pairCount === 1 ? 'invitation pair' : 'invitation pairs'
					} ready.`,
				noSavedIdentified: 'No saved recipients yet; save recipients before invite-only launch.',
				noSavedLongitudinal:
					'No saved recipients; save recipients for invite-only access, or use a public link and let respondents enter their repeat-participation code.',
				noSavedAnonymous: 'No saved recipients; launch with a public link or save recipients below.'
			},
			launchPlan: {
				title: 'Launch plan',
				summary: 'Prepare the wave, response mode, recipients, and Collection handoff before launch.',
				draftWave: 'Draft wave',
				wave: 'Wave',
				responseMode: 'Response mode',
				recipients: 'Recipients',
				collectionHandoff: 'Collection handoff',
				waveDraftReady: (waveName: string) => `${waveName} is the draft wave for this study.`,
				waveWillBeCreated: (waveName: string) =>
					`${waveName} will be created when you save this step.`,
				identifiedModeDetail:
					'Identified collection requires saved recipients so each respondent can receive assigned access.',
				longitudinalModeDetail:
					'Repeat-participation collection can use public access or saved recipients; respondents use their own repeat code for comparison.',
				anonymousModeDetail: 'Anonymous collection can use a public link or saved email recipients.',
				chooseModeDetail: 'Choose how respondents should enter this wave.',
				savedRecipientDetail: (selectionCount: number, pairCount: number) =>
					`${selectionCount} saved ${selectionCount === 1 ? 'selection' : 'selections'} with ${pairCount} ${
						pairCount === 1 ? 'invitation pair' : 'invitation pairs'
					}.`,
				identifiedNeedsRecipients: 'Identified collection needs saved recipients before launch.',
				longitudinalNoRecipients:
					'No saved recipients yet. You can use a public link, or save recipients for invite-only repeat participation.',
				anonymousNoRecipients:
					'No saved recipients yet. You can still launch anonymous collection with a public link.',
				saveRecipientsBeforeIdentifiedLaunch:
					'Save recipients before opening Collection for identified launch.',
				launchPassedOpenCollection: 'Launch check passed; open Collection to start the wave.',
				runLaunchCheckBeforeCollection: 'Run launch check before opening Collection.'
			},
			designMap: {
				title: 'Study design map',
				summary:
					'This map reflects saved setup artifacts, not the starting point chosen when the study was created.',
				source: 'Study source',
				questionnaire: 'Questionnaire',
				results: 'Results setup',
				waves: 'Collection waves',
				sourceReady: 'Source content is ready for this questionnaire.',
				sourceMissing: 'Confirm reusable or imported source content before saving the questionnaire.',
				questionnaireSaved: (name: string, questionCount: number) =>
					`${name} is saved with ${questionCount} ${questionCount === 1 ? 'question' : 'questions'}.`,
				questionnaireMissing: 'Save the questionnaire before results setup or launch checks.',
				resultsReady: (ruleKey: string) => `Results setup is saved as ${ruleKey}.`,
				resultsMissing: 'Choose which questionnaire answers become study results.',
				noWaves: 'No collection wave exists yet.',
				draftWaveNeedsReadiness: (count: number) =>
					`${count} draft ${count === 1 ? 'wave is' : 'waves are'} prepared; launch readiness still needs attention.`,
				waveReady: (count: number) =>
					`${count} draft ${count === 1 ? 'wave is' : 'waves are'} ready for Collection.`,
				liveWave: (count: number) =>
					`${count} ${count === 1 ? 'wave is' : 'waves are'} collecting responses.`,
				closedWave: (count: number) =>
					`${count} ${count === 1 ? 'wave has' : 'waves have'} closed data for Results review.`
			},
			waveContext: {
				prepareForCollection: (waveName: string) => `Prepare ${waveName} for collection`,
				firstWaveSetup: 'First wave setup',
				currentDraftWave: 'Current draft wave',
				followUpDraftWave: 'Follow-up draft wave',
				futureWaveSetup: 'Future wave setup',
				firstWaveSummary: 'Use this step to create the first collection wave and decide who can answer.',
				currentDraftSummary: 'Use this step to finish the current draft wave before opening Collection.',
				followUpDraftSummary: (waveName: string) =>
					`${waveName} is a draft follow-up wave. Use it only when the next collection round is intentional.`,
				closedOneWaveSummary: (
					previousWaveName: string,
					previousWaveStatus: string,
					nextWaveName: string
				) =>
					`${previousWaveName} is already ${previousWaveStatus}. Create ${nextWaveName} only when the next collection round is intentional.`,
				multipleWaveSummary: (existingWaveCount: number, nextWaveName: string) =>
					`${existingWaveCount} waves already exist. Create ${nextWaveName} only after the current wave results have been reviewed.`,
				createFirstAfterSetup:
					'Create Wave 1 only after the questionnaire and results setup are saved.',
				recipientBelongsUntilLaunch: (waveName: string) =>
					`Recipient selection belongs to ${waveName} until this wave is launched.`,
				reviewResultsBeforeFollowup:
					'Review the previous wave in Results before treating this as a follow-up collection.',
				doNotAssumeRecipients:
					'Do not assume recipients are unchanged; save the intended people or group for this wave.',
				reviewBeforePreparing: (previousWaveName: string, nextWaveName: string) =>
					`Review ${previousWaveName} before preparing ${nextWaveName}`,
				reviewExistingBeforePreparing: (nextWaveName: string) =>
					`Review existing waves before preparing ${nextWaveName}`,
				openResultsBeforeCreating: (reviewTarget: string, nextWaveName: string) =>
					`Open Results to review or export ${reviewTarget} before creating ${nextWaveName}.`,
				createOnlyWhenIntentional: (nextWaveName: string) =>
					`Create ${nextWaveName} only when the next collection round is intentional.`,
				recipientBelongsToNewDraft: (previousLabel: string) =>
					`Recipient selection in this step will belong to the new draft wave, not to ${previousLabel}.`,
				previousWaves: 'the previous waves'
			},
			misc: {
				notEditable: 'not editable',
				and: 'and'
			}
		},
		operationsBody: {
			progressAriaLabel: 'Study collection flow',
			progressKicker: 'Study collection',
			progressTitle: 'Collection flow',
			progressBody:
				'Start the wave, share respondent access, monitor submissions, and close collection when the study is finished.',
			stepsComplete: (completed: number, total: number) => `${completed}/${total} steps complete`,
			statusKicker: 'Collection status',
			nextAction: 'Next action',
			pathAriaLabel: 'Collection path',
			stepAriaLabel: 'Collection step',
			readOnlyTitle: 'Read-only access',
			readOnlyBody: 'Collection actions require workspace management access.',
			stepStatus: {
				working: 'Working',
				saved: 'Saved',
				failed: 'Failed',
				ready: 'Ready'
			},
			pathStatus: {
				done: 'Done',
				current: 'Current',
				blocked: 'Blocked'
			},
			common: {
				available: 'Available',
				blocked: 'Blocked',
				closed: 'Closed',
				created: 'Created',
				missing: 'Missing',
				notAvailable: 'Not available',
				notChecked: 'Not checked',
				ready: 'Ready',
				status: 'Status',
				collectionWave: 'Collection wave',
				setupCheck: 'Setup check',
				started: 'Started',
				submitted: 'Submitted',
				inProgress: 'In progress',
				latestActivity: 'Latest activity',
				reportReadiness: 'Report readiness',
				loaded: (count: string) => `${count} loaded`,
				reconciled: (count: string) => `${count} reconciled`
			},
			readiness: {
				body:
					'Use this before opening collection. The check confirms that the questionnaire, results, recipients, and policy setup can support responses and reporting.',
				issuesAria: 'Readiness issues',
				warningsTitle: 'Setup warnings',
				blockersTitle: 'Before collection can start',
				warningsBody:
					'These items do not block collection, but review them before sharing access.',
				blockersBody: 'Fix the blocking setup items, then run the pre-launch check again.',
				blocking: 'Blocking',
				warning: 'Warning',
				openSetup: 'Open Setup',
				returnAndCheck: 'Return here and run the check again after saving setup.',
				blockedTitle: 'Setup is blocked',
				blockedBody:
					'The check did not return itemized blockers. Open Setup, review incomplete steps, save changes, then run this check again.',
				runCheck: 'Run pre-launch check'
			},
			launch: {
				body:
					'Starting collection opens the selected wave for responses and records the setup version that reports will use later.',
				start: 'Start collection',
				resultLabel: 'Collection'
			},
			shareAccess: {
				body:
					'Choose how respondents enter this wave. Saved Directory and group selections become private invitations. Use the one-off importer only to add ad hoc recipients after launch, or create an open respondent link when broad access is acceptable.',
				identifiedEntryLabel: 'Identified entry',
				inviteOnlyLabel: 'Invite-only access',
				openLinkLabel: 'Open respondent link',
				identifiedEntryTitle: 'Create an identified respondent entry',
				privateInvitationsTitle: 'Private invitations are active',
				openLinkReadyTitle: 'Open link already created',
				createShareableLinkTitle: 'Create a shareable link',
				openLinkDisabled: 'Open link disabled',
				openLinkActive: 'Open link active',
				openLinkNotCreated: 'Not created',
				inviteOnly: 'Invite-only',
				replaceLostLink: 'Replace lost link',
				replaced: 'Replaced',
				oneActiveLink: 'One active link',
				createIdentifiedAccessLink: 'Create identified access link',
				createRespondentLink: 'Create respondent link',
				shareLink: 'Share link',
				respondentLinkReady: 'Respondent link ready',
				identifiedHelp: 'Use this only when respondents should be connected to known subject records.',
				inviteOnlyHelp:
					'This wave already has private email invitations. Open links are disabled so participation stays limited to invited recipients.',
				openLinkReadyHelp:
					'This wave already has one active open link. If the link was lost, replace it here. The old link will stop accepting new respondents; existing response sessions can still finish through their private session handles.',
				openLinkHelp:
					'Use this when broad anonymous participation is acceptable and you do not need an invite-only recipient list.'
			},
			emailSetup: {
				label: 'Email sending setup',
				title: 'Check delivery configuration before sending',
				body:
					'This check shows whether this environment can send real SMTP invitations or is still in test mode or missing settings. It never exposes provider secrets or SMTP credentials.',
				mode: 'Mode',
				realEmailSend: 'Real email send',
				providerEvents: 'Provider events',
				webhookConfigured: 'Webhook configured',
				webhookDisabled: 'Webhook disabled',
				checkEmailSetup: 'Check email setup'
			},
			simulation: {
				label: 'Test responses',
				title: 'Simulate response data',
				body:
					'Use this in non-production environments to create believable submitted responses without sending email.',
				responses: 'Responses',
				averageTarget: 'Average target',
				variation: 'Variation',
				tight: 'Tight',
				normal: 'Normal',
				noisy: 'Noisy',
				simulateCollection: 'Simulate collection',
				includeComments: 'Add short synthetic text answers when the questionnaire has comment fields.',
				answersSaved: 'Answers saved',
				scoredResponses: 'Scored responses'
			},
			monitor: {
				body:
					'Watch response movement while collection is open. These numbers refresh from the workspace state and do not change study setup.',
				deliveryDiagnostics: 'Delivery diagnostics',
				recentEmailEvents: 'Recent email delivery events',
				noEventsYet: 'No events yet',
				providerEventsBody:
					'Use this only when troubleshooting email sending. It shows accepted, delivered, bounced, and spam-complaint counts without exposing recipients, internal ids, provider ids, or provider reason text.',
				accepted: 'Accepted',
				delivered: 'Delivered',
				bounced: 'Bounced',
				complained: 'Complained',
				latestProviderEvent: 'Latest provider event',
				loadProviderEvents: 'Load recent provider events',
				noRecentProviderEvents: 'No recent provider events are recorded for this workspace yet.',
				refreshStatus: 'Refresh status'
			},
			cleanup: {
				label: 'Email delivery cleanup',
				title: 'Repair readiness',
				needsReview: 'Needs review',
				noCleanup: 'No cleanup',
				notChecked: 'Not checked',
				body:
					'Check this before retrying failed invitation emails. It separates stale handoffs, ambiguous failures, retryable failures, and suppressed recipients without changing delivery state.',
				staleHandoffs: 'Stale handoffs',
				ambiguousFailures: 'Ambiguous failures',
				retryableFailures: 'Retryable failures',
				suppressedFailures: 'Suppressed failures',
				deliveryEvents: 'Delivery events',
				checkCleanupReadiness: 'Check cleanup readiness',
				retryPossible: 'Retry possible'
			},
			close: {
				body:
					'Close collection when the response window is finished. Submitted responses remain available for scoring and reports.',
				closeCollection: 'Close collection'
			},
			navigation: {
				ariaLabel: 'Collection step navigation',
				previousStep: 'Previous step',
				nextStep: 'Next step',
				goToResults: 'Go to results'
			},
			email: {
				subject: 'Study invitation',
				body:
					'You have been invited to complete a study.\n\nFor privacy, this email does not include the study title or topic. The link opens the study page before you decide whether to respond.\n\nOpen your study link:\n[unique respondent link]\n\nIf you already responded, you can ignore this email.\n\nIf you should not receive future study invitations from this workspace, unsubscribe here:\n[unsubscribe link]\n\n[workspace invitation footer]'
			}
		},		operationsWorkflow: {
			stepNumber: (number: number) => `${number}`,
			actions: {
				readiness: {
					title: 'Pre-launch check',
					description: 'Confirm the questionnaire, results setup, recipients, and policies are ready.'
				},
				launch: {
					title: 'Start collection',
					description: 'Open this wave for responses and record the setup used for reporting.'
				},
				openLink: {
					title: 'Share access',
					description: 'Send saved invitations or create an open respondent link for this wave.'
				},
				monitor: {
					title: 'Monitor responses',
					description: 'Track starts, drafts, submissions, and report readiness while collection runs.'
				},
				close: {
					title: 'Close collection',
					description: 'Stop accepting new responses while keeping submitted data reportable.'
				}
			},
			disabled: {
				createWaveBeforeReadiness: 'Create a collection wave in setup before checking readiness.',
				createWaveBeforeStart: 'Create a collection wave before starting collection.',
				startBeforeAccess: 'Start collection before preparing respondent access.',
				startBeforeMonitor: 'Start collection before monitoring responses.',
				createWaveBeforeClose: 'Create a collection wave before closing collection.',
				waveClosed: 'This collection wave is closed.',
				alreadyLive: 'Collection is already live.',
				startedThisSession: 'Collection was started in this session.',
				runPrelaunchAndSetup:
					'Run the pre-launch check. If it says Blocked, open Setup and finish the listed items first.',
				onlyLiveClosable: 'Only a live collection wave can be closed.'
			},
			status: {
				lifecycleLabel: 'Collection lifecycle',
				responseProgressLabel: 'Response progress',
				accessLabel: 'Access',
				reportingReadinessLabel: 'Reporting readiness',
				noWaveSelectedTitle: 'No wave selected',
				noWaveSelectedDetail: 'Create or select a collection wave before collecting responses.',
				noResponsesYetTitle: 'No responses yet',
				noResponsesYetDetail: 'Response counts appear after a wave is started.',
				noRecipientAccessTitle: 'No recipient access prepared',
				noRecipientAccessDetail: 'Choose recipients or create respondent access after setup is ready.',
				reportingNotAvailableTitle: 'Not available',
				reportingNotAvailableDetail: 'Reporting readiness appears after collection has a selected wave.',
				createWaveFirstHeadline: 'Create a collection wave first',
				createWaveFirstGuidance: 'Collection starts after setup has a campaign wave.',
				createWaveFirstNextAction: 'Open Setup and create a collection wave.',
				closedTitle: 'Closed',
				closedDetail: 'This wave no longer accepts new responses.',
				liveTitle: 'Live: accepting responses',
				liveDetail: 'Respondents can still submit. Results remain preliminary until collection closes.',
				draftTitle: 'Draft: not collecting yet',
				draftDetail: 'Run the pre-launch check, then start collection.',
				submittedTitle: (submitted: string) => `${submitted} submitted`,
				responseActivityDetail: (started: string, drafts: string, submitted: string) =>
					`${started} started, ${drafts} in progress, ${submitted} submitted.`,
				waitingForResponsesTitle: 'Waiting for responses',
				waitingForResponsesDetail: 'Collection is open, but no response activity has been recorded yet.',
				notCollectingTitle: 'Not collecting yet',
				notCollectingDetail: 'Start collection before monitoring responses.',
				accessNotPreparedTitle: 'Access not prepared',
				accessNotPreparedDetail: 'Create a respondent link or prepare invitations before expecting responses.',
				accessWaitsForLaunchTitle: 'Access waits for launch',
				accessWaitsForLaunchDetail:
					'Save recipients in Setup before launch, or start collection before creating an open respondent link.',
				resultsPreliminaryDetail:
					'Results can be reviewed, but live collection data should be treated as preliminary until closed.',
				reportingUsefulAfterSubmitted: 'Reporting becomes useful after submitted responses are available.',
				closedOverallLabel: 'Closed',
				closedHeadline: (submitted: string, submittedCount: number) =>
					`Closed: ${submitted} submitted response${submittedCount === 1 ? '' : 's'}`,
				closedGuidance: 'Collection is closed. Submitted responses are stable for Results review.',
				closedNextAction: 'Open Results to review findings and exports.',
				liveOverallLabel: 'Live',
				liveHeadline: (submitted: string) => `Live: accepting responses with ${submitted} submitted`,
				liveGuidance:
					'Use this page to monitor response progress and recipient access. Close collection when the response window is finished.',
				liveNextWithResponses:
					'Keep collecting, review preliminary Results, or close collection when ready.',
				liveNextNoResponses: 'Share respondent access and wait for submitted responses.',
				draftOverallLabel: 'Draft',
				draftHeadline: 'Draft: collection has not started',
				draftGuidance: 'Run the pre-launch check before sharing respondent access.',
				draftNextAction: 'Run the pre-launch check.',
				identifiedAccessTitle: 'Identified access prepared',
				inviteOnlyAccessTitle: 'Invite-only access prepared',
				openLinkAccessTitle: 'Open-link access prepared',
				recipientAccessTitle: 'Recipient access prepared',
				identifiedAccessDetail: (openLinkCount: string, pluralSuffix: string) =>
					`${openLinkCount} identified access link${pluralSuffix} prepared. Respondents are connected to known subject records for this wave.`,
				inviteOnlyDetail: (invitationCount: string, verb: string, boundary: string) =>
					`${invitationCount} saved email invitation${verb} ready for this wave. Only saved recipients receive private access, and ${boundary}`,
				mixedAccessDetail: (
					openLinkCount: string,
					openPluralSuffix: string,
					invitationCount: string,
					invitationPluralSuffix: string,
					boundary: string
				) =>
					`${openLinkCount} open respondent link${openPluralSuffix} and ${invitationCount} saved email invitation${invitationPluralSuffix}. Open-link access is broad; invite-only email access limits entry to saved recipients. ${boundary}`,
				openLinkDetail: (openLinkCount: string, verb: string) =>
					`${openLinkCount} open respondent link${verb} active. Anyone with the link can enter this wave; use saved invitations when access should be limited.`,
				createAccessBeforeResponses:
					'Create a respondent link or saved email invitations before expecting responses.',
				anonymousBoundary: 'anonymous reports still do not show who answered.',
				anonymousBoundarySentence: 'Anonymous reports still keep respondent identity out of results.',
				longitudinalBoundary:
					'repeat-participation results use participant codes instead of showing who answered.',
				longitudinalBoundarySentence:
					'Repeat-participation comparison uses participant codes; email recipient lists are not shown in results.',
				notAvailable: 'Not available'
			}
		},
		reportsWorkflow: {
			stepNumber: (number: number) => `Step ${number}`,
			actions: {
				reportProof: {
					title: 'Review results',
					description: 'Preview disclosure-safe result summaries for the selected wave.'
				},
				exportArtifact: {
					title: 'Create report-summary export',
					optionalTitle: 'Report-summary export optional',
					description:
						'Create the aggregate results CSV and codebook. Use it outside the team only after interpretation and finality are ready.',
					optionalDescription:
						'A response dataset already exists. A report-summary export is optional and not required before download.'
				},
				responseExport: {
					title: 'Create response export',
					description: 'Create analysis-ready response rows and a codebook for this study.'
				},
				fetchArtifact: {
					title: 'Review export file',
					description: 'Review the latest export file details before downloading.'
				},
				downloadCsv: {
					responseDatasetTitle: 'Download response dataset CSV',
					responseDatasetDescription:
						'Download the analysis-ready response dataset CSV and codebook when it is ready.',
					reportSummaryTitle: 'Download report-summary CSV',
					reportSummaryDescription:
						'Download the report-summary CSV for review packets only. This is not an analysis-ready response dataset.'
				}
			},
			disabled: {
				createOrSelectWaveBeforeReviewingResults: 'Create or select a wave before reviewing results.',
				resolveReportPrerequisitesBeforeReviewingResults:
					'Resolve report prerequisites before reviewing results.',
				reviewResultsBeforeCreatingReportExport: 'Review results before creating a report export.',
				resolveReportPrerequisitesBeforeCreatingReportExport:
					'Resolve report prerequisites before creating a report export.',
				reportExportCreatedThisSession: 'Report export was created in this session.',
				responseDatasetAlreadyExistsReportOptional:
					'Response dataset already exists; report-summary export is optional.',
				reportSummaryExportAlreadyExists: 'Report-summary export already exists for this study.',
				reviewResultsBeforeCreatingResponseExport: 'Review results before creating a response export.',
				resolveReportPrerequisitesBeforeCreatingResponseExport:
					'Resolve report prerequisites before creating a response export.',
				responseExportCreatedThisSession: 'Response export was created in this session.',
				responseExportAlreadyExists: 'Response export already exists for this study.',
				createOrSelectExportBeforeReview: 'Create or select an export file before reviewing it.',
				createOrSelectExportBeforeDownload: 'Create or select an export file before downloading CSV.',
				selectDownloadableExportBeforeDownload:
					'Select a downloadable export file before downloading CSV.'
			},
			packetReview: {
				title: 'Can these results be used?',
				description:
					'Check whether you have responses, visible scores, an export file, and a clear use limit.',
				primaryAction: {
					noCampaign: 'Create or select a wave before reviewing results.',
					noResponses: 'Collect responses before reviewing results.',
					noVisibleScores:
						'Use raw response export for internal analysis, or review Results setup scoring, missing-answer rules, and disclosure.',
					createExport:
						'Create a response export for analysis, or create a report-summary file for internal review.',
					downloadDataset: 'Download the response dataset for analysis.',
					documentInterpretation:
						'Use the response dataset internally; document score meaning before sharing conclusions.',
					preliminary: 'Use as preliminary internal data until collection is closed.'
				}
			},
			scoreMethodReview: {
				title: 'How were these scores produced?',
				description:
					'Review score outputs, coverage, missing-answer handling, and interpretation limits before using results.'
			},
			exportPreview: {
				title: 'What is in this export?',
				description:
					'Review file purpose, row shape, wave fields, trajectory keys, variables, missingness, and score outputs before downloading.',
				createOrSelectWaveFirst: 'Create or select a wave first',
				reviewExportFileFirst: 'Review export file first',
				selectWavePendingDetail: 'Select a wave before preparing export files.',
				reviewFilePendingDetail: 'Review the export file to inspect its CSV and codebook contents.',
				downloadResponseDatasetCsv: 'Download response dataset CSV',
				downloadReportSummaryCsv: 'Download report-summary CSV'
			}
		},		wavesWorkflow: {
			stepNumber: (number: number) => `Step ${number}`,
			plan: {
				createFirstTitle: 'Create the first wave',
				createFirstDescription: 'Start by creating Wave 1 as the first collection round for this study.',
				openSetupLabel: 'Open setup',
				createFirstGuidance: [
					'Each wave is a collection round inside this study. Create Wave 1 in Setup, then launch it from Collection.',
					'After responses arrive, review the wave in Results before adding a follow-up wave.',
					'Use anonymous longitudinal from the first wave if you need linked change-over-time comparison later.'
				],
				reviewWavePairTitle: (wavePairTitle: string) => `Review ${wavePairTitle}`,
				groupTrendReviewDescription:
					'These waves can be reviewed as group-level results. Linked same-respondent change needs repeat participation from the first wave.',
				reviewGroupTrendLabel: 'Review group trend',
				groupTrendReviewGuidance: (nextWaveNumber: number) => [
					'Review these waves as a group-level trend. Do not describe the change as same-respondent movement because the waves are anonymous.',
					'Use repeat participation from Wave 1 when the study needs linked change-over-time comparison later.',
					`Review or export Wave 1 and Wave 2 before using Setup to create Wave ${nextWaveNumber}.`
				],
				oneWaveTitle: (nextWaveNumber: number) => `Review Wave 1 before planning Wave ${nextWaveNumber}`,
				oneWaveDescription:
					'Wave 1 exists. Review the current results first; plan a follow-up only when the next collection round is intentional.',
				reviewWaveResultsLabel: (waveNumber: number) => `Review Wave ${waveNumber} results`,
				planWaveLaterLabel: (waveNumber: number) => `Plan Wave ${waveNumber} later`,
				oneWaveGuidance: (nextWaveNumber: number) => [
					`Review or export Wave 1 before using Setup to create Wave ${nextWaveNumber}.`,
					'Use anonymous longitudinal when the same respondent should be linked across waves for change-over-time comparison.',
					'Review recipients before launching the new wave; do not assume the recipient list is unchanged unless Collection shows it.'
				],
				checkReadinessTitle: 'Check comparison readiness',
				checkReadinessDescription:
					'Two longitudinal waves exist. Now confirm linked trajectories and scoring compatibility.',
				runChecksBelowLabel: 'Run checks below',
				reviewResultsLabel: 'Review results',
				checkReadinessGuidance: [
					'Use the checks below to confirm both waves can be linked safely.',
					'Results remain wave-by-wave until linked trajectories, disclosure, and scoring compatibility are ready.',
					'If the comparison is blocked, use the details section to see which prerequisite is missing.'
				],
				sameRespondentTitle: 'Check same-respondent change',
				sameRespondentDescription:
					'Two repeat-participation waves exist. Run the comparison checks before treating this as same-respondent change.',
				runLinkedChecksBelowLabel: 'Run linked checks below',
				sameRespondentGuidance: [
					'Use the comparison checks below to confirm linked responses, disclosure, scoring compatibility, and visible deltas before making change-over-time claims.',
					'Use Results for wave-level exports; use Waves only when you need reviewed change-over-time context.',
					'Create another follow-up wave from Setup when the next collection round starts.'
				]
			},
			groupTrend: {
				notReadyTitle: 'Group trend not ready',
				notReadyDescription: 'Collect responses in at least two waves before reviewing wave-level trend.',
				sameRespondentComparisonLabel: 'Same-respondent comparison',
				notReadySameRespondentValue: 'Not available until two repeated waves exist',
				disclosureStatusLabel: 'Disclosure status',
				notReadyDisclosureValue: 'Review after follow-up wave results exist',
				notReadyGuidance: [
					'A group trend compares wave-level results. It does not require respondent linking.',
					'Launch and collect a follow-up wave before reading a trend.',
					'Use repeat participation if you need same-respondent change instead of wave-level movement.'
				],
				title: (baselineName: string, comparisonName: string) =>
					`Aggregate group trend only: ${baselineName} to ${comparisonName}`,
				readyDescription:
					'Aggregate group-level results are ready to review as a trend. This is not same-respondent change.',
				pendingDescription:
					'Both waves have responses. Finish score output before treating the trend as ready.',
				firstWaveScoresLabel: 'First wave scores',
				secondWaveScoresLabel: 'Second wave scores',
				runComparisonChecksValue: 'Run comparison checks before making same-respondent claims',
				notConfiguredValue: 'Not configured for same-respondent linked change',
				disclosureNotAvailableValue: 'Review wave-level disclosure in Results before making claims',
				suppressedLinkedComparisonsLabel: 'Suppressed linked comparisons',
				openResultsLabel: 'Open Results',
				readyGuidance: [
					'Use this for anonymous or unlinked waves where the question is whether the group moved between rounds.',
					'Do not describe this as individual improvement or decline unless linked change is ready.',
					'Review scoring and disclosure in Results before making claims from the trend.'
				]
			},
			comparisonReview: {
				title: 'Comparison plan',
				description:
					'See whether this study is ready for a follow-up wave, aggregate group trend, or same-respondent linked change.'
			},
			scoreMethodReview: {
				title: 'What is being compared?',
				description:
					'Review scoring rules, linked-pair method, compared outputs, missingness, and interpretation limits before using wave change.'
			},
			actions: {
				twoWaveProof: {
					title: 'Check linked change readiness',
					description:
						'Confirm this study has repeat-participation waves and linked responses for same-respondent comparison.'
				},
				waveComparisonProof: {
					title: 'Review linked change',
					description: 'Review disclosure-safe same-respondent change between the selected waves.'
				}
			},
			disabled: {
				unlinkedWavesUseGroupTrend:
					'Linked same-respondent comparison is unavailable because these waves were not created with repeat participation. Review group trend instead.',
				addRepeatedWaves: 'Add at least two repeated waves before comparing change over time.',
				chooseBaselineAndComparison: 'Choose baseline and comparison waves before reviewing change over time.',
				checkReadinessBeforeReview: 'Check comparison readiness before reviewing change over time.'
			},
			inactiveReason: {
				groupTrend:
					'This study supports aggregate group trend only. Linked-change checks are not required and would be misleading here.',
				noWaves: 'Create and collect the first waves before linked-change checks apply.',
				oneWave:
					'Review Wave 1 in Results. Plan Wave 2 from Setup only when the next collection round is intentional.',
				needScoredResponses: 'Collect scored responses in at least two waves before comparison tasks apply.'
			}
		},
		waveSnapshot: {
			status: {
				notAvailable: 'Not available',
				blocked: 'Blocked',
				previewReady: 'Preview ready',
				previewAvailable: 'Preview available',
				failed: 'Failed',
				loading: 'Loading',
				ready: 'Ready'
			},
			disabled: {
				selectComparableWaves:
					'Select two comparable waves before loading the wave comparison snapshot.',
				runLinkedTrajectoryCheck:
					'Run the linked trajectory check before loading the wave comparison snapshot.'
			},
			dashboard: {
				unavailableTitle: 'Wave dashboard unavailable',
				unavailableMessage: 'Select two comparable waves before reviewing the wave dashboard.',
				title: (baselineName: string, comparisonName: string) =>
					`${baselineName} vs ${comparisonName} wave dashboard`,
				campaigns: 'Campaigns',
				longitudinalWaves: 'Longitudinal waves',
				submittedWaves: 'Submitted waves',
				missingPrerequisites: 'Missing prerequisites',
				baselineWave: 'Baseline wave',
				baselineStatus: 'Baseline status',
				baselineSubmittedResponses: 'Baseline submitted responses',
				comparisonWave: 'Comparison wave',
				comparisonStatus: 'Comparison status',
				comparisonSubmittedResponses: 'Comparison submitted responses',
				linkedTrajectories: 'Linked trajectories',
				completeTrajectories: 'Complete trajectories',
				previewStatus: 'Preview status',
				interpretation: 'Interpretation',
				linkedPairs: 'Linked pairs',
				disclosure: 'Disclosure',
				disclosureK: 'Disclosure k',
				compatibility: 'Compatibility',
				visibleScores: 'Visible scores',
				suppressedScores: 'Suppressed scores',
				blockedScores: 'Blocked scores',
				baselineLaunchSnapshot: 'Baseline launch snapshot',
				baselineLatestLaunch: 'Baseline latest launch',
				baselineScoringRule: 'Baseline scoring rule',
				baselineDisclosurePolicy: 'Baseline disclosure policy',
				comparisonLaunchSnapshot: 'Comparison launch snapshot',
				comparisonLatestLaunch: 'Comparison latest launch',
				comparisonScoringRule: 'Comparison scoring rule',
				comparisonDisclosurePolicy: 'Comparison disclosure policy',
				untitledWave: 'Untitled wave'
			},
			chrome: {
				sectionAria: 'Wave comparison preview',
				kicker: 'Wave comparison',
				title: 'Compared waves',
				description: 'Disclosure-safe comparison for the selected baseline and comparison waves.',
				summaryAria: 'Wave comparison summary',
				readinessKicker: 'Comparison readiness',
				readinessTitle: 'Can these waves be compared?',
				readinessDescription:
					'Checks whether the selected waves can be compared without exposing small groups.',
				waveReadinessAria: 'Wave readiness',
				waveReadinessKicker: 'Readiness',
				waveReadinessTitle: 'Wave readiness',
				comparisonAria: 'Comparison status',
				comparisonKicker: 'Comparison',
				comparisonTitle: 'Comparison status',
				guardrailsAria: 'Disclosure and compatibility',
				guardrailsKicker: 'Guardrails',
				guardrailsTitle: 'Disclosure and compatibility',
				sourceAria: 'Wave source context',
				sourceKicker: 'Based on',
				sourceTitle: 'Launch and policy context',
				resolvePrerequisites: 'Resolve wave comparison prerequisites before loading the snapshot.',
				loadFailed: 'Wave comparison snapshot could not be loaded.',
				loadingComparison: 'Loading comparison',
				refreshComparison: 'Refresh wave comparison',
				study: 'Study'
			},
			codeLabels: {
				proof_only: 'preview',
				not_validated_interpretation: 'not validated interpretation',
				visible: 'visible',
				suppressed: 'suppressed',
				compatible: 'compatible',
				not_available: 'not available',
				live: 'live',
				draft: 'draft',
				closed: 'closed'
			}
		},
		reportWidgets: {
			notAvailable: 'Not available',
			yes: 'Yes',
			no: 'No',
			labels: {
				available: 'Available',
				campaign: 'Campaign',
				campaignStatus: 'Campaign status',
				closedAt: 'Closed at',
				closedWave: 'Closed wave',
				completed: 'Completed',
				coverageStatus: 'Coverage status',
				created: 'Created',
				currentResultSummary: 'Current result summary',
				dataFinality: 'Data finality',
				disclosure: 'Disclosure',
				disabled: 'Disabled',
				download: 'Download',
				enabled: 'Enabled',
				exportActions: 'Export actions',
				exportFiles: 'Export files',
				exportFileDataUnavailable: 'Export file data is unavailable.',
				exportState: 'Export state',
				failureReason: 'Failure reason',
				finalityDataUnavailable: 'Finality and provenance data is unavailable.',
				interpretation: 'Interpretation',
				latestExport: 'Latest export',
				latestLaunch: 'Latest launch',
				latestScoringActivity: 'Latest scoring activity',
				listed: 'Listed',
				noExportFiles: 'No export files recorded.',
				notConfigured: 'Not configured',
				notConfiguredState: 'Not configured',
				notSelected: 'Not selected',
				previewReady: 'Preview ready',
				previewSource: 'Preview source',
				preliminaryLive: 'Preliminary live',
				readinessDataUnavailable: 'Readiness data is unavailable.',
				reportable: 'Reportable',
				reportableCampaigns: 'Reportable campaigns',
				reportReadinessPrerequisites: 'Report readiness prerequisites',
				reportPreview: 'report preview',
				reportStatus: 'Report status',
				ready: 'Ready',
				readyToRun: 'Ready to run',
				resultsPreview: 'Results preview',
				resultsPreviewLoading: 'The results preview is loading.',
				resultsPreviewUnavailable: 'Results preview unavailable',
				resultsPreviewUnavailableExport:
					'The export workflow can still be used while the preview is unavailable.',
				resultsPreviewWidgets: 'Results preview widgets',
				resultsSummary: 'Results summary',
				rows: 'Rows',
				scoreCoverageDataUnavailable: 'Score coverage data is unavailable.',
				scored: 'Scored',
				scores: 'Scores',
				selectedCampaign: 'Selected campaign',
				selectedCampaignReportStateUnavailable: 'Selected campaign report state is unavailable.',
				size: 'Size',
				submitted: 'Submitted',
				submittedResponses: 'Submitted responses',
				suppressed: 'Suppressed',
				suppressedScores: 'Suppressed scores',
				unavailable: 'Unavailable',
				unscored: 'Unscored',
				visible: 'Visible',
				visibleScores: 'Visible scores',
				visualAnalyticsDataUnavailable: 'Visual analytics entry data is unavailable.'
			},
			codeLabels: {
				proof_only: 'preview',
				not_validated_interpretation: 'not validated interpretation',
				not_available: 'not available',
				visible: 'visible',
				suppressed: 'suppressed',
				complete: 'complete',
				partial: 'partial',
				no_submissions: 'no submissions',
				ready_for_aggregate_report: 'ready for aggregate report',
				closed_wave: 'closed wave',
				preliminary_live: 'preliminary live data',
				not_reportable: 'not reportable',
				succeeded: 'succeeded',
				failed: 'failed',
				queued: 'queued',
				rendering: 'rendering',
				csv: 'CSV'
			}
		},
		surfaceChrome: {
			loadingContext: (surface: string) => `Loading ${surface} context`,
			missingStudy: 'Select a study before opening this surface.',
			surfaceUnavailableFallback: 'Campaign series surface could not be loaded.',
			resultsSummaryUnavailable: 'Results summary could not be loaded.',
			errorTitle: 'Campaign series unavailable',
			retry: 'Retry surface',
			readOnlyStateAria: 'Sample study read-only state',
			ownershipKicker: 'Study ownership',
			readOnly: 'Read-only',
			scoreCoverageUnavailable: 'Score coverage is not available.',
			noMissingScores: 'No missing submitted scores to remediate.',
			actionStates: {
				running: 'Running',
				done: 'Done',
				failed: 'Failed',
				ready: 'Ready'
			},
			collectionDetails: {
				summary: 'Collection details',
				kicker: 'Collection details',
				title: 'Operational details',
				description:
					'Audit and troubleshooting details for this collection wave. Most collection work should happen in the workflow above.',
				monitorAria: 'Collection monitor',
				monitorKicker: 'Collection monitor',
				monitorTitle: 'Response collection',
				scoreCoverageAria: 'Score coverage',
				scoreCoverageKicker: 'Score coverage',
				scoreCoverageTitle: 'Submitted scoring coverage',
				remediatingScores: 'Remediating scores',
				remediateMissingScores: 'Remediate missing scores',
				remediationRequiresAccess: 'Score remediation requires setup management access.',
				resultAria: 'Score remediation result',
				submittedResponses: 'Submitted responses',
				eligibleSubmitted: 'Eligible submitted',
				alreadyScored: 'Already scored',
				remediated: 'Remediated',
				notConfigured: 'Not configured',
				latestScoringActivity: 'Latest scoring activity',
				prerequisitesAria: 'Missing collection prerequisites',
				prerequisitesKicker: 'Prerequisites',
				prerequisitesTitle: 'Missing collection requirements'
			},
			resultsDetails: {
				summary: 'Results details',
				kicker: 'Results details',
				title: 'Audit and troubleshooting',
				description:
					'Use these details when results or exports are blocked. Normal review and export work should happen in the workflow above.',
				readinessAria: 'Results readiness',
				readinessKicker: 'Readiness',
				readinessTitle: 'What is ready?',
				scoreCoverageAria: 'Score coverage',
				scoreCoverageKicker: 'Score coverage',
				scoreCoverageTitle: 'Report readiness',
				selectedCampaignAria: 'Results selected campaign',
				selectedWaveKicker: 'Selected wave',
				reportStateTitle: 'Report state',
				sourceAria: 'Results source context',
				basedOn: 'Based on',
				launchPolicyExport: 'Launch, policy, and export context',
				prerequisitesAria: 'Missing results prerequisites',
				prerequisitesKicker: 'Prerequisites',
				prerequisitesTitle: 'Missing result requirements',
				waves: 'Waves',
				includedWaves: 'Included result waves'
			},
			wavesDetails: {
				summary: 'Waves details',
				kicker: 'Waves details',
				title: 'Comparison details',
				description:
					'Use these details when a wave comparison is blocked or needs audit context. Normal comparison work should happen in the workflow above.',
				comparedWavesAria: 'Compared waves',
				comparedWavesKicker: 'Compared waves',
				selectedComparison: 'Selected comparison',
				baselineWave: 'Baseline wave',
				comparisonWave: 'Comparison wave',
				comparisonStatus: 'Comparison status',
				disclosure: 'Disclosure',
				compatibility: 'Compatibility',
				missing: 'Missing',
				readinessAria: 'Wave readiness',
				readinessKicker: 'Comparison readiness',
				availableTitle: 'What is available?',
				sourceAria: 'Wave source context',
				basedOn: 'Based on',
				launchPolicy: 'Launch and policy context',
				prerequisitesAria: 'Missing wave prerequisites',
				prerequisitesKicker: 'Blocked comparison',
				prerequisitesTitle: 'What needs attention?',
				availableWavesAria: 'Available waves',
				availableWavesKicker: 'Available waves',
				waveHistory: 'Wave history'
			},
			fallback: {
				selectedSeriesContext: 'selected-series context',
				productWorkflow: 'Product workflow',
				previewWorkflow: 'Preview workflow',
				governance: 'Governance',
				selectedSeriesReadiness: 'Selected-series readiness',
				campaignRows: 'Campaign rows',
				campaignRowsAria: 'Selected series campaign rows',
				campaignContext: 'Selected-series campaign context',
				readOnlyAccess: 'Read-only access',
				workflowRequiresSetup: 'Workflow actions require setup management access.'
			}
		}
	}
};

const hr: typeof en = {
	common: {
		product: 'Proizvod',
		createWorkspace: 'Izradi radni prostor',
		signIn: 'Prijava',
		open: 'Otvori',
		retry: 'Pokušaj ponovno',
		email: 'E-pošta',
		privateBeta: 'Privatna beta'
	},
	publicEntry: {
		metaTitle: 'Instruments Platform | Operacije istraživačkih studija',
		metaDescription:
			'EU radni prostor u privatnoj beti za postavljanje studija, prikupljanje odgovora, pregled rezultata i izvoz.',
		brandSubtitle: 'Istraživačke studije i programi dobrobiti',
		navAria: 'Radnje ulaza u proizvod',
		mobileNavAria: 'Mobilne radnje ulaza u proizvod',
		menu: 'Izbornik',
		openMenu: 'Otvori izbornik',
		closeMenu: 'Zatvori izbornik',
		workflow: 'Tijek rada',
		trustModel: 'Model povjerenja',
		heroKicker: 'EU radni prostor za istraživanja i studije dobrobiti',
		heroTitle: 'Vodite istraživačke studije od postavljanja do obranjivih rezultata.',
		heroBody:
			'Izradite upitnike, prikupite anonimne ili identificirane odgovore, pregledajte kontekst bodovanja i izvezite podatke bez spajanja obrazaca, tablica, skripti i snimki zaslona.',
		previewAria: 'Pregled proizvoda',
		previewChrome: 'radni prostor / operacije studije',
		previewCollect: 'Prikupljanje',
		previewResults: 'Rezultati',
		selectedStudy: 'Odabrana studija',
		previewStudyName: 'Puls dobrobiti na poslu',
		liveCollection: 'Prikupljanje u tijeku',
		responseSignal: 'Signal odgovora',
		responseProgress: '412 odgovora · 33% dovršeno',
		prepare: 'Priprema',
		launchChecklist: 'Kontrolna lista pokretanja',
		prepareBody: 'Upitnik, bodovanje, publika i postavke prikupljanja ostaju vidljivi prije pokretanja.',
		export: 'Izvoz',
		datasetCodebook: 'Skup podataka + šifrarnik',
		exportBody: 'Izvozi uz datoteku čuvaju izvor, konačnost i kontekst potiskivanja.',
		trustAria: 'Model povjerenja',
		workflowRibbon: 'Postavljanje, prikupljanje, bodovanje, izvještaji i izvozi',
		access: 'Pristup',
		accessRibbon: 'Autentificirani radni prostori ograničeni na organizaciju',
		dataControls: 'Kontrole podataka',
		dataControlsRibbon: 'Privola, zadržavanje, konačnost i provenijencija izvoza',
		productStage: 'Faza proizvoda',
		productStageRibbon: 'Privatna beta s postupnim uključivanjem',
		workspaceOverview: 'Pregled radnog prostora',
		suiteTitle: 'Vidite što je spremno, blokirano, aktivno ili spremno za izvoz.',
		suiteBody:
			'Neka sljedeća radnja svake studije bude vidljiva: praznine u pripremi, aktivno prikupljanje, pregled rezultata i spremnost izvoza.',
		nextAction: 'Sljedeća radnja',
		nextActionQuestion: 'Što tim treba napraviti sljedeće?',
		appAreas: 'Područja aplikacije',
		studyStatus: 'Status studije',
		workflowTitle: 'Jasan put od dizajna studije do ponovno upotrebljivih dokaza.',
		portfolio: 'Portfelj',
		portfolioBody: 'Izradite, usporedite i nastavite aktivne programe studija iz jednog radnog prostora.',
		prepareStepBody: 'Definirajte upitnik, bodovanje, politike, publiku i provjere pokretanja.',
		collectStepBody: 'Otvorite poveznice ili pozive, pratite napredak odgovora i dostavu.',
		reviewStepBody: 'Pregledajte izvještaje, usporedite valove i izvezite podatke s provenijencijom.'
	},
	signIn: {
		metaTitle: 'Prijava | Instruments Platform',
		metaDescription: 'Pronađite radni prostor prije prijave preko pružatelja prijave.',
		brandSubtitle: 'Prijava u radni prostor',
		navAria: 'Radnje prijave',
		eyebrow: 'Pristup radnom prostoru',
		title: 'Prijavite se u svoj radni prostor.',
		body:
			'Unesite e-poštu koja se koristi za radni prostor. Prvo pronalazimo radni prostor, zatim vas šaljemo pružatelju prijave za lozinku i MFA.',
		stepsAria: 'Koraci prijave',
		stepFind: 'Pronađi radni prostor',
		stepFindBody: 'Upotrijebite istu e-poštu koja je vlasnik ili član radnog prostora.',
		stepSignIn: 'Prijava',
		stepSignInBody: 'Pružatelj prijave obrađuje lozinku, odabir računa i MFA.',
		stepOpenApp: 'Otvori aplikaciju',
		stepOpenAppBody:
			'Platforma se otvara tek nakon što se odabrani račun poveže s članstvom u radnom prostoru.',
		formAria: 'Obrazac prijave u radni prostor',
		panelEyebrow: 'Postojeći radni prostor',
		panelTitle: 'Nastavite s e-poštom radnog prostora',
		createInsteadPrefix: 'Trebate novi radni prostor?',
		createInsteadLink: 'Izradite ga umjesto toga',
		recentWorkspace: 'Pronađen je nedavni radni prostor',
		continueAs: (email: string) => `Nastavite kao ${email} ili unesite drugu e-poštu radnog prostora.`,
		continueRecentBody: 'Nastavite u nedavni radni prostor ili unesite drugu e-poštu radnog prostora.',
		continueRecent: 'Nastavi u nedavni radni prostor',
		openingWorkspace: 'Otvaram prijavu u radni prostor.',
		findingWorkspace: 'Tražim radni prostor...',
		continueToSignIn: 'Nastavi na prijavu',
		betaBoundaryBody:
			'Koristite samo demo ili vlasnički kontrolirane podatke dok se produkcijski pregled ne zatvori.',
		workspaceNotFound: 'Za ovu e-poštu još ne postoji radni prostor. Prvo izradite radni prostor.',
		emailInvalid: 'Unesite e-poštu korištenu za radni prostor.',
		fallbackError: 'Nismo mogli pronaći radni prostor za ovu e-poštu.'
	},
	register: {
		metaTitle: 'Izradi radni prostor | Instruments Platform',
		metaDescription:
			'Izradite radni prostor privatne bete za Instruments Platform i dovršite prijavu preko pružatelja prijave.',
		brandSubtitle: 'Radni prostor privatne bete',
		navAria: 'Radnje registracije',
		eyebrow: 'Pristup privatnoj beti',
		title: 'Izradite svoj radni prostor.',
		body:
			'Upotrijebite e-poštu koja treba biti vlasnik radnog prostora. Lozinka i MFA ostaju kod pružatelja prijave; ova stranica samo imenuje radni prostor i provjerava beta pristup.',
		stepsAria: 'Koraci registracije',
		stepCreate: 'Izradi račun',
		stepCreateBody: 'Unesite e-poštu, naziv radnog prostora i beta kod prije otvaranja postavljanja računa.',
		stepVerify: 'Potvrdite e-poštu',
		stepVerifyBody: 'Ako pružatelj prijave zatraži potvrdu, potvrdite e-poštu i nastavite ovdje.',
		stepOpen: 'Otvori aplikaciju',
		stepOpenBody: 'Radni prostor nastaje iz odobrene registracije i otvara se s vašom sesijom.',
		formAria: 'Obrazac izrade radnog prostora',
		panelEyebrow: 'Registracija radnog prostora',
		panelTitle: 'Izradite račun i radni prostor',
		alreadyHave: 'Već imate radni prostor?',
		signInInstead: 'Prijavite se umjesto toga',
		checkingAccount: 'Provjeravam stanje računa...',
		chooseStartedAccount: 'Odaberite račun s kojim ste započeli',
		verifyThenSignIn: 'Potvrdite e-poštu, zatim se prijavite',
		mismatchBody:
			'Odabrani račun za prijavu ne odgovara e-pošti radnog prostora koju ste unijeli. Potpuno se odjavite ako preglednik stalno bira pogrešan račun.',
		verifyBody:
			'Otvorite poruku za potvrdu e-pošte od pružatelja prijave, zatim ponovno pokušajte registracijsku prijavu istom e-poštom.',
		retryRegistration: 'Pokušaj registracijsku prijavu ponovno',
		startOver: 'Kreni ispočetka',
		workspaceName: 'Naziv radnog prostora',
		workspacePlaceholder: 'Vaš laboratorij, tim ili tvrtka',
		betaAccessCode: 'Beta pristupni kod',
		accessCodePlaceholder: 'Pristupni kod',
		openingAccount: 'Otvaram postavljanje računa.',
		openingAccountButton: 'Otvaram postavljanje računa...',
		createAccount: 'Izradi račun',
		useDifferentEmail: 'Upotrijebi drugu e-poštu',
		accountReady: (email: string) =>
			`Račun je spreman: ${email}. Ova e-pošta upravljat će radnim prostorom.`,
		workspaceCreated: 'Radni prostor izrađen. Otvaram aplikaciju.',
		creatingWorkspace: 'Izrada radnog prostora...',
		boundaryTitle: 'Privatna beta',
		boundaryBody:
			'Stvarna upotreba sa sudionicima počinje tek nakon potrebnog pravnog i implementacijskog pregleda za radni prostor.',
		sessionError:
			'Nismo mogli potvrditi korak računa. Ponovno nastavite s postavljanjem računa prije izrade radnog prostora.',
		disabled:
			'Registracija privatne bete nije otvorena na ovom okruženju. Prijavite se ako već imate radni prostor.',
		expired:
			'Korak računa je istekao. Ponovno nastavite s postavljanjem računa, zatim izradite radni prostor.',
		forbidden:
			'Ovaj račun ne može izraditi radni prostor. Odjavite se i upotrijebite odobreni beta račun ili zatražite beta pristup.',
		conflict: 'Ovaj račun ili radni prostor već je postavljen. Prijavite se umjesto toga.',
		invalidCode: 'Taj pristupni kod ne odgovara popisu privatne bete.',
		emailExists: 'Radni prostor već postoji za ovu e-poštu. Prijavite se umjesto toga.',
		organizationInvalid: 'Unesite naziv radnog prostora ili organizacije koji želite koristiti.',
		fallbackError: 'Nismo mogli izraditi radni prostor. Provjerite beta pristupni kod i pokušajte ponovno.'
	},
	workspaceHome: {
		eyebrow: 'Radni prostor',
		title: 'Početna',
		description:
			'Odaberite sljedeći koristan zadatak: izradite studiju, nastavite rad, pozovite ljude, upravljajte publikama ili preuzmite rezultate.',
		loading: 'Učitavanje pregleda radnog prostora',
		errorTitle: 'Pregled radnog prostora nije dostupan',
		retry: 'Pokušaj pregled ponovno',
		start: 'Početak',
		setupWorkspace: 'Postavite radni prostor',
		chooseNext: 'Odaberite sljedeću radnju',
		firstRunActions: ['Izradi prvu studiju', 'Pozovi tim', 'Postavi imenik', 'Pregledaj instrumente'],
		firstRunActionStatuses: ['Počnite ovdje', 'Pristup', 'Ljudi', 'Knjižnica'],
		firstRunActionDescriptions: [
			'Pokrenite stvarnu studiju i nastavite kroz postavljanje, prikupljanje i rezultate.',
			'Pripremite pristup članova radnog prostora prije dijeljenja prve poveznice za prijavu.',
			'Izradite ljude, grupe, članstva i veze s voditeljima za ciljanje.',
			'Potvrdite koji su instrumenti dostupni prije početka produkcijskog rada na studiji.'
		],
		nextActions: 'Sljedeće radnje',
		openStudies: 'Otvori Studije',
		examples: 'Primjeri',
		sampleStudies: 'Primjeri studija',
		yourWork: 'Vaš rad',
		yourStudies: 'Vaše studije',
		workspaceOverview: 'Pregled radnog prostora'
	},
	portfolio: {
		eyebrow: 'Radni prostor studija',
		title: 'Studije',
		description:
			'Izradite studiju ili otvorite postojeću. Primjeri ostaju odvojeni od stvarnih studija radnog prostora.',
		loading: 'Učitavanje studija',
		errorTitle: 'Studije nisu dostupne',
		retry: 'Pokušaj studije ponovno',
		guidedDesign: 'Vođeni dizajn studije',
		startBlueprint: 'Započnite predložak studije',
		selectedStartingPoint: 'Odabrana početna točka',
		studyModelTitle: 'Što se ovdje izrađuje',
		studyModelBody:
			'Prvo se izrađuje spremnik studije. Odabrana početna točka samo priprema Postavljanje; upitnik, izlaze rezultata, primatelje i valove možete urediti prije pokretanja.',
		studyModelStudy: 'Studija',
		studyModelStudyBody:
			'Trajni spremnik u radnom prostoru za postavljanje, valove prikupljanja, rezultate i izvozne datoteke.',
		studyModelStartingPoint: 'Početna točka',
		studyModelStartingPointBody:
			'Priprema prvi nacrt u Postavljanju. To nije završni upitnik ni višekratni zapis instrumenta.',
		studyModelSetup: 'Postavljanje',
		studyModelSetupBody:
			'Mjesto gdje početnu točku pretvarate u upitnik, postavljanje rezultata, plan primatelja i provjeru pokretanja.',
		studyName: 'Naziv studije',
		continueSetup: 'Nastavi na vođeno postavljanje',
		creating: 'Izrada...',
		readOnlyAccess: 'Pristup samo za čitanje',
		readOnlyBody: 'Izrada i promjena studija zahtijeva pristup za upravljanje postavljanjem.',
		studyList: 'Popis studija',
		openStudy: 'Otvori studiju',
		searchStudies: 'Pretraži studije',
		searchPlaceholder: 'Pretraži po nazivu studije',
		readiness: 'Spremnost',
		sort: 'Sortiranje',
		visibility: 'Vidljivost'
	},
	instruments: {
		eyebrow: 'Knjižnica instrumenata',
		title: 'Instrumenti',
		description:
			'Pregledajte ponovno upotrebljive skupove pitanja koji mogu pokrenuti studiju. Izrada prilagođene studije odvija se u Postavljanju.',
		loading: 'Učitavanje knjižnice instrumenata',
		errorTitle: 'Knjižnica instrumenata nije dostupna',
		retry: 'Pokušaj instrumente ponovno',
		summary: 'Sažetak knjižnice',
		visibleInstruments: 'Vidljivi instrumenti',
		noInstruments: 'Nema instrumenata',
		noInstrumentsBody: 'Još nema instrumenata vidljivih ovoj organizaciji.',
		nextStep: 'Sljedeći korak',
		createOrOpen: 'Izradite ili otvorite studiju',
		studiesBody: 'Odaberite studiju za izradu upitnika, bodovanja, publike i stanja pokretanja.'
	},
	exports: {
		eyebrow: 'Izvozi',
		title: 'Preuzimanje datoteka',
		description: 'Pronađite CSV i šifrarnik datoteke izrađene na stranicama Rezultata studije.',
		loading: 'Učitavanje izvoznih datoteka',
		errorTitle: 'Izvozne datoteke nisu dostupne',
		retry: 'Pokušaj izvoze ponovno',
		files: 'Datoteke',
		downloadable: 'Datoteke za preuzimanje i sljedeća upotreba',
		noFiles: 'Nema izvoznih datoteka',
		noFilesBody: 'Izradite izvoz na stranici rezultata studije nakon što rezultati budu dostupni.',
		reports: 'Izvještaji',
		counts: 'Brojevi izvoza',
		countsBody: 'Koristite ove brojeve za provjeru jesu li datoteke spremne, na čekanju ili neuspjele.'
	},
	directory: {
		eyebrow: 'Imenik publike',
		title: 'Ljudi i grupe',
		description: 'Izradite sudionike, ponovno upotrebljive publike i managerske veze za ciljanje studija.',
		accessTitle: 'Pristup imeniku zahtijeva upravljanje postavljanjem',
		accessMessage: 'Podaci imenika osoba dostupni su samo setup managerima.',
		setup: 'Postavljanje imenika',
		buildAudience: 'Prvo izgradite popis publike',
		buildAudienceBody:
			'Dodajte ljude, organizirajte ih u grupe, zatim koristite te grupe pri odabiru publike studije.',
		addPeopleOrGroups: 'Dodaj ljude ili grupe',
		people: 'Ljudi',
		groups: 'Grupe',
		memberships: 'Članstva',
		managerLinks: 'Managerske veze',
		howUsed: 'Kako se koriste podaci imenika',
		csvImport: 'CSV uvoz',
		csvTitle: 'Pregledajte, zatim uvezite ljude i grupe',
		peopleInWorkspace: 'Ljudi u ovom radnom prostoru',
		audienceGroups: 'Grupe publike',
		addRecords: 'Dodaj zapise',
		membershipManager: 'Članstvo i manager',
		countsAria: 'Brojevi ljudi i ciljanja',
		csvBody: 'Koristite ovo kada je publika studije već pripremljena u tablici. Prvo pregledajte retke kako biste potvrdili tko će biti izrađen, ažuriran, grupiran ili odbijen. Primijenite tek kada pregled izgleda ispravno.',
		downloadTemplate: 'Preuzmi predložak',
		csvFile: 'CSV datoteka',
		csvRows: 'CSV retci',
		csvHelp: 'Obavezan identitet: external_id ili email. Opcionalno grupiranje: group_type, group_name, role_in_group. Jedna osoba može biti u više redaka kada pripada više grupa.',
		previewing: 'Pregled...',
		previewCsv: 'Pregledaj CSV',
		applying: 'Primjena...',
		applyImport: 'Primijeni uvoz',
		fixFailedRows: 'Popravite neuspjele retke prije primjene ovog uvoza imenika.',
		applyingImport: 'Primjenjujem pregledani uvoz imenika...',
		checkingRows: 'Provjeravam retke bez promjene zapisa imenika...',
		peopleToCreate: 'Ljudi za izradu',
		peopleCreated: 'Ljudi izrađeni',
		peopleToUpdate: 'Ljudi za ažuriranje',
		peopleUpdated: 'Ljudi ažurirani',
		groupsToCreate: 'Grupe za izradu',
		groupsCreated: 'Grupe izrađene',
		membershipsToAdd: 'Članstva za dodavanje',
		membershipsAdded: 'Članstva dodana',
		membershipsPresent: 'Članstva već postoje',
		peopleDirectoryAria: 'Imenik ljudi',
		directoryGraphCounts: 'Brojevi grafa imenika',
		refresh: 'Osvježi',
		email: 'E-pošta',
		roleInGroup: 'Uloga u grupi',
		directoryRelationshipsAria: 'Odnosi u imeniku',
		noSubjectSelected: 'Nijedna osoba nije odabrana',
		rootGroup: 'Korijenska grupa',
		saving: 'Spremanje...',
		savePerson: 'Spremi osobu',
		addMembership: 'Dodaj članstvo',
		saveManager: 'Spremi managera'
	},
	team: {
		eyebrow: 'Pristup radnom prostoru',
		title: 'Tim',
		description: 'Pozovite članove tima, dodijelite uloge i potvrdite tko može upravljati studijama ili rezultatima.',
		loadingOverview: 'Učitavanje pregleda pristupa timu',
		tenantTeam: 'Tim organizacije',
		overviewTitle: 'Pregled pristupa timu',
		overviewBody: 'Tko može ući u organizaciju, pripremati studije i upravljati pristupom.',
		prepareTitle: 'Pripremite pristup člana, zatim podijelite prijavu',
		prepareBody:
			'Dodajte e-poštu, odaberite ulogu, zatim podijelite generiranu poveznicu iz popisa. Lozinke i MFA ostaju u Auth0.',
		readOnlyTitle: 'Pristup samo za čitanje',
		readOnlyBody: 'Priprema članova i promjena uloga zahtijeva pristup upravljanju timom.',
		rosterTitle: 'Članovi i uloge',
		memberSingular: 'član',
		memberPlural: 'članova',
		teamOverviewCountsAria: 'Brojevi pregleda pristupa timu',
		capabilityCoverageAria: 'Pokrivenost mogućnosti tima',
		tenantRolesUnavailable: 'Uloge organizacije nisu dostupne',
		retryRoles: 'Pokušaj ponovno učitati uloge',
		memberEmail: 'E-pošta člana',
		memberRole: 'Uloga člana',
		memberLocale: 'Jezik člana',
		adding: 'Dodavanje...',
		addMember: 'Dodaj člana',
		loadingRoles: 'Učitavanje uloga organizacije.',
		pendingNoticeSuffix: 'Popis označava člana kao na čekanju dok Auth0 prvi put ne vrati istu e-poštu.',
		readOnlyAria: 'Pristup timu samo za čitanje',
		teamRoster: 'Popis tima',
		loadingMembers: 'Učitavanje članova tima',
		teamMembersUnavailable: 'Članovi tima nisu dostupni',
		retryMembers: 'Pokušaj ponovno učitati članove',
		rosterCountsAria: 'Brojevi popisa članova organizacije',
		membersLabel: 'Članovi',
		noMembersTitle: 'Nema članova organizacije',
		noMembersBody: 'Nema aktivnih dodjela uloga za ovu organizaciju.',
		currentUser: 'Trenutni korisnik',
		localeLabel: 'Jezik',
		created: 'Izrađeno',
		lastLogin: 'Zadnja prijava',
		roles: 'Uloge',
		capabilities: 'Mogućnosti',
		firstSignIn: 'Prva prijava',
		firstSignInBody: (email: string) => `Pošaljite ovu poveznicu na ${email}. Član ostaje na čekanju dok Auth0 ne vrati istu e-poštu za ovaj radni prostor.`,
		openLink: 'Otvori poveznicu',
		copyLink: 'Kopiraj poveznicu',
		copied: 'Kopirano',
		roleFor: (email: string) => `Uloga za ${email}`,
		changeRoleAria: (email: string) => `Promijeni ulogu za ${email}`,
		changeRole: 'Promijeni ulogu',
		saving: 'Spremanje...'
	},
	settings: {
		eyebrow: 'Radni prostor',
		title: 'Postavke radnog prostora',
		description: 'Upravljajte pristupom, ljudima, podacima i zadanim postavkama studija s jednog mjesta.',
		loading: 'Učitavanje postavki organizacije',
		errorTitle: 'Postavke radnog prostora nisu dostupne',
		retry: 'Pokušaj postavke ponovno',
		hub: 'Središte postavki',
		whatManage: 'Čime ovdje možete upravljati?',
		whatManageBody:
			'Postavke razine radnog prostora postupno se sastavljaju ovdje. Za sada koristite ove prečace za aktivna područja proizvoda.',
		teamAccess: 'Pristup timu',
		teamBody: 'Pozovite članove radnog prostora, pregledajte pristup na čekanju i upravljajte ulogama.',
		directoryBody: 'Upravljajte ljudima, grupama i hijerarhijskim podacima za publike studija.',
		studySetup: 'Postavljanje studije',
		studySetupBody: 'Izradite ili nastavite studije, upitnike, valove prikupljanja i postavljanje rezultata.',
		exportsBody: 'Pregledajte generirane izvozne datoteke i preuzmite podatke spremne za analizu.',
		workspaceDetails: 'Detalji radnog prostora',
		profile: 'Profil radnog prostora',
		scale: 'Veličina radnog prostora',
		footprint: 'Trenutni opseg',
		directoryShortcut: 'Imenik',
		exportsShortcut: 'Izvozi',
		shortcutsAria: 'Prečaci postavki radnog prostora',
		workspaceDetailsAria: 'Detalji radnog prostora',
		profileDetailsAria: 'Detalji profila radnog prostora',
		countsAria: 'Brojevi radnog prostora'
	},
	respondent: {
		metaFallback: 'Upitnik za sudionika',
		loadingSurvey: 'Učitavanje upitnika',
		surveyUnavailable: 'Upitnik nije dostupan',
		tryAgain: 'Pokušaj ponovno',
		responseReceipt: 'Potvrda odgovora',
		participantCode: 'Kod sudionika',
		continue: 'Nastavi',
		reviewKicker: 'Pregled',
		reviewTitle: 'Pregled odgovora',
		savedAnswers: (count: number) => count + ' ' + (count === 1 ? 'odgovor spremljen' : 'odgovora spremljeno'),
		session: 'Sesija',
		backToEdit: 'Natrag na uređivanje',
		submitReviewed: 'Predaj pregledani odgovor',
		saveAndReview: 'Spremi i pregledaj',
		linkUnavailable: 'Ova poveznica više nije dostupna.',
		requiredConsent: 'Obavezne privole moraju biti prihvaćene prije nastavka.',
		participantCodeRequired: 'Kod sudionika je obavezan prije nastavka.',
		sessionNotReady: 'Sesija odgovaranja nije spremna.',
		saveBeforeSubmit: 'Spremite odgovore prije predaje.',
		responseSessionUnavailable: 'Ova sesija odgovaranja više nije dostupna.',
		requestFailed: 'Zahtjev nije uspio.',
		questionRequiresAnswer: (code: string) => code + ' zahtijeva odgovor.',
		questionMustBeNumber: (code: string) => code + ' mora biti broj.',
		questionBetween: (code: string, min: number, max: number) =>
			code + ' mora biti između ' + min + ' i ' + max + '.'
	},
	unsubscribe: {
		metaTitle: 'Odjava od pozivnica za studije - Instruments Platform',
		kicker: 'E-pošta pozivnice za studiju',
		title: 'Odjava od budućih pozivnica',
		body:
			'Koristite ovu stranicu samo ako želite da se ova adresa e-pošte doda na popis radnog prostora za ne kontaktirati za pozivnice na studije.',
		button: 'Odjavi ovu adresu e-pošte',
		submitting: 'Primjenjujem zahtjev za ne kontaktirati...',
		done:
			'Ova adresa e-pošte dodana je na popis radnog prostora za ne kontaktirati za buduće pozivnice na studije. Možete zatvoriti ovu stranicu.',
		retry: 'Pokušaj ponovno',
		fallbackError:
			'Ova pozivnica se nije mogla odjaviti. Poveznica može biti neispravna ili već uklonjena.'
	},
	selectedStudy: {
		overview: {
			eyebrow: 'Odabrana studija',
			title: 'Pregled',
			description:
				'Pogledajte gdje studija stoji, zatim nastavite postavljanje, prikupljanje, rezultate ili usporedbu valova.',
			ariaLabel: 'Pregled odabrane studije',
			loading: 'Učitavanje pregleda studije',
			errorTitle: 'Pregled studije nije dostupan',
			retry: 'Pokušaj pregled ponovno',
			missingId: 'Nedostaje id studije.',
			unavailableFallback: 'Pregled odabrane studije nije se mogao učitati.',
			restoreFailed: 'Studija se nije mogla vratiti.',
			duplicateFailed: 'Primjer studije nije se mogao duplicirati.',
			selectedStudy: 'Odabrana studija',
			studyDetails: 'Detalji studije',
			statusRecords: 'Status i zapisi',
			statusDescription: 'Pregledajte spremnost, pravila i valove povezane s ovom studijom.',
			dates: 'Datumi',
			datesAria: 'Datumi odabrane studije',
			studyModel: 'Model studije',
			lifecycle: 'Životni ciklus',
			governance: 'Upravljanje',
			governanceAria: 'Status upravljanja',
			policyScoring: 'Status pravila i bodovanja',
			campaigns: 'Valovi',
			campaignsAria: 'Redci valova odabrane studije',
			campaignsInStudy: 'Valovi u ovoj studiji',
			noCampaigns: 'Nijedan val još nije povezan s ovom studijom.',
			restore: 'Vrati',
			restoring: 'Vraćanje...',
			readOnly: 'Samo za čitanje',
			duplicateAsStudy: 'Dupliciraj kao studiju',
			duplicating: 'Dupliciranje...',
			duplicateAria: (title: string) => `Dupliciraj kao studiju ${title}`
		},
		surfaces: {
			setup: {
				eyebrow: 'Postavljanje',
				title: 'Postavljanje studije',
				description: 'Prođite korake postavljanja redom prije početka prikupljanja.',
				ariaLabel: 'Radni prostor postavljanja'
			},
			operations: {
				eyebrow: 'Prikupljanje studije',
				title: 'Prikupljanje odgovora',
				description:
					'Pokrenite prikupljanje, podijelite pristup sudionicima, pratite odgovore i zatvorite prozor odgovaranja.',
				ariaLabel: 'Radni prostor prikupljanja'
			},
			reports: {
				eyebrow: 'Rezultati studije',
				title: 'Pregled rezultata',
				description:
					'Pregledajte dostupne nalaze, pokrivenost bodovanja, ograničenja i sljedeću upotrebu izvoza za ovu studiju.',
				ariaLabel: 'Radni prostor rezultata'
			},
			waves: {
				eyebrow: 'Usporedba valova',
				title: 'Valovi',
				description:
					'Izradite naknadne valove prikupljanja, zatim usporedite povezanu longitudinalnu promjenu kad je studija spremna.',
				ariaLabel: 'Valovi i povezane putanje'
			}
		},
		setupBody: {
			progressAriaLabel: 'Napredak postavljanja studije',
			progressKicker: 'Postavljanje studije',
			progressTitle: 'Napredak postavljanja studije',
			progressBody:
				'Izgradite studiju redom: izvor, upitnik, rezultati, val, primatelji, zatim provjera prije pokretanja.',
			readOnlyTitle: 'Pristup samo za čitanje',
			readOnlyBody: 'Radnje postavljanja zahtijevaju pravo za upravljanje postavljanjem.',
			requiredStepsComplete: (completed: number, total: number) =>
				`${completed} od ${total} obaveznih koraka dovršeno`,
			currentSetupStep: 'Trenutni korak postavljanja',
			selectedSetupStep: 'Odabrani korak postavljanja',
			status: {
				blocked: 'Blokirano',
				current: 'Trenutno',
				done: 'Dovršeno',
				failed: 'Neuspjelo',
				pending: 'Na čekanju',
				ready: 'Spremno',
				saved: 'Spremljeno',
				working: 'U tijeku'
			},
			questionnaire: {
				paletteTitle: 'Odaberite uređivi skup pitanja',
				paletteBody:
					'Krenite od strukturiranog skupa pitanja, zatim uredite pitanja, formate odgovora i izlaze rezultata za ovu studiju.',
				addQuestion: 'Dodaj pitanje',
				saveQuestionnaire: 'Spremi upitnik',
				authoringSummary: 'Sažetak upitnika',
				blueprintTitle: 'Pregled dizajna upitnika',
				studyDimensions: 'Dimenzije studije',
				questionText: 'Tekst pitanja',
				answerFormat: 'Format odgovora',
				respondentPreview: 'Pregled za ispitanika',
				errorsLabel: 'Pogreške upitnika',
				paletteOptions: {
					blank: {
						label: 'Prazan upitnik',
						category: 'Prilagođeno',
						summary: 'Počnite s praznim uređivim pitanjima i sami izgradite instrument.',
						detail: 'Koristite kada studija ne odgovara pripremljenom predlošku zdravlja na radu.'
					},
					workload_recovery: {
						label: 'Opterećenje i oporavak',
						category: 'Zdravlje na radu',
						summary: 'Kratak uređivi skup za radni pritisak, potrebu za oporavkom i kapacitet oporavka.',
						detail: 'Korisno za prve studije medicine rada ili ergonomije.'
					},
					osh_ergonomics: {
						label: 'Zaštita na radu i ergonomija',
						category: 'Izvorni uređivi početni skup',
						summary: 'Izvorne uređive stavke za držanje, ponavljanje, nelagodu i oporavak.',
						detail: 'Korisno za početno istraživanje zaštite na radu. Nije validirani imenovani instrument.'
					},
					office_ergonomics: {
						label: 'Uredska ergonomija',
						category: 'Početni skup za personu',
						summary:
							'Izvorne uređive stavke za prilagodbu radnog mjesta, naprezanje zbog ekrana, ulazne uređaje i prekide.',
						detail: 'Za hibridne ili uredske timove gdje su ergonomija i uvjeti fokusa važni.'
					},
					academic_workload: {
						label: 'Akademsko opterećenje',
						category: 'Početni skup za personu',
						summary:
							'Izvorne uređive stavke za nastavno/istraživačko opterećenje, administraciju, jasnoću očekivanja i oporavak.',
						detail: 'Korisno za profesorske studije i provjere opterećenja odjela. Tumačenje zadržite specifično za studiju.'
					},
					team_climate: {
						label: 'Puls timske klime',
						category: 'Izvorni uređivi početni skup',
						summary:
							'Izvorne uređive stavke za jasnoću uloga, podršku, komunikaciju, pravednost i psihološku sigurnost.',
						detail: 'Kompaktan puls zdravlja tima za ponovljene valove ili jednokratni interni pregled.'
					},
					healthcare_staff_strain: {
						label: 'Opterećenje zdravstvenog osoblja',
						category: 'Početni skup za personu',
						summary:
							'Izvorne uređive stavke za umor nakon smjene, emocionalno opterećenje, manjak osoblja, primopredaju i oporavak.',
						detail:
							'Korisno za vlasničku probu i buduće bolničko istraživanje bez kliničkih ili dijagnostičkih tvrdnji.'
					},
					burnout_risk: {
						label: 'Rizik iscrpljenosti',
						category: 'Zdravlje na radu',
						summary: 'Kompaktan uređivi pregled iscrpljenosti, distanciranja i signala oporavka.',
						detail: 'Zadržava generičan tekst i ne promovira imenovanu vlasničku skalu.'
					},
					ergonomics_baseline: {
						label: 'Ergonomska početna procjena',
						category: 'Ergonomija',
						summary: 'Polazište za držanje, nelagodu, alate i kontekst radnog mjesta.',
						detail: 'Koristite kada studija kreće od radnog okruženja i fizičkog opterećenja.'
					},
					psychosocial_safety: {
						label: 'Psihosocijalna sigurnost',
						category: 'Organizacijska klima',
						summary: 'Uređiva pitanja o podršci, jasnoći, pravednosti opterećenja i psihološkoj sigurnosti.',
						detail: 'Namijenjeno za interno poboljšanje, ne za vanjsku dijagnostiku.'
					}
				}
			},
			scoring: {
				resultsTitle: 'Izlazi rezultata',
				resultsBody: 'Definirajte bodove i izvozne stupce koje ovaj upitnik treba proizvesti.',
				saveResults: 'Spremi postavljanje rezultata',
				errorsLabel: 'Pogreške postavljanja rezultata'
			},
			wave: {
				responseMode: {
					anonymousLabel: 'Anonimno',
					anonymousLongitudinalLabel: 'Anonimno s ponovljenim sudjelovanjem',
					identifiedLabel: 'Identificirano',
					anonymousHelp: 'Odgovori se u izvještaju ne povezuju s poznatom osobom.',
					anonymousLongitudinalHelp:
						'Ispitanici ostaju anonimni u izvještaju, ali se ponovljeni valovi mogu povezati za promjenu kroz vrijeme.',
					identifiedHelp: 'Odgovori se mogu povezati s poznatim ispitanicima za operativno praćenje.'
				}
			},
			recipients: {
				audienceRules: {
					selfLabel: 'Svaki primatelj odgovara za sebe',
					managerLabel: 'Voditelji odgovaraju za svoj tim',
					externalEmailsLabel: 'Jednokratni uvoz e-pošte',
					selfHelp: 'Jedan spremljeni primatelj stvara jednu pozivnicu.',
					managerHelp: 'Voditelji dobivaju pozivnice za osobe u svom opsegu odgovornosti.',
					externalEmailsHelp: 'Zalijepite vanjske adrese e-pošte za ovaj val bez dodavanja u imenik.'
				},
				roles: {
					respondent: 'Ispitanik',
					manager: 'Voditelj',
					external: 'Vanjski primatelj'
				},
				warnings: {
					audienceMissing:
						'Publika kampanje nema aktivnih članova; pregled koristi sve aktivne osobe tenant prostora.',
					empty: 'Pregled nije pronašao ispitanike.',
					truncated: 'Pregled je skraćen.'
				}
			}
		},
		setupWorkflow: {
			stepNumber: (number: number) => `${number}`,
			defaultWaveName: (number: number) => `Val ${number}`,
			steps: {
				instrument: {
					title: 'Izvor studije',
					description:
						'Potvrdite izvorni ili uvezeni sadržaj. On pokreće upitnik, ali nije sama studija.'
				},
				template: {
					title: 'Upitnik',
					description: 'Izradite spremljeni skup pitanja na koji sudionici odgovaraju u ovoj studiji.'
				},
				scoring: {
					title: 'Postavljanje rezultata',
					description:
						'Odaberite koji odgovori iz upitnika postaju rezultati studije i kako se obrađuju nedostajući odgovori.'
				},
				campaign: {
					title: 'Val i primatelji',
					description: 'Pripremite krug prikupljanja, način odgovaranja i primatelje za ovu studiju.'
				},
				readiness: {
					title: 'Provjera pokretanja',
					description:
						'Provjerite upitnik, postavljanje rezultata, val, primatelje i pravila prije početka prikupljanja.'
				}
			},
			disabled: {
				confirmInstrument: 'Prvo potvrdite izvor studije.',
				saveQuestionnaire: 'Prvo spremite upitnik.',
				createCollectionWave: 'Prvo izradite val prikupljanja.'
			},
			pathDisplay: {
				done: 'Gotovo',
				current: 'Trenutno',
				selected: 'Odabrano',
				next: 'Sljedeće',
				blocked: 'Blokirano'
			},
			launchState: {
				createWaveFirstStatus: 'Prvo izradite val prikupljanja',
				createWaveFirstNext: 'Izradite i spremite val prikupljanja prije provjere pokretanja.',
				runLaunchCheckFirst: 'Prvo pokrenite provjeru',
				launchPassedSaveRecipients: 'Provjera je prošla; spremite primatelje za identificirani pristup',
				launchPassedChooseAccess: 'Provjera je prošla; odaberite javnu poveznicu ili spremite primatelje',
				saveRecipientsForIdentified:
					'Spremite primatelje ispod prije pokretanja kako bi Prikupljanje moglo izraditi identificirani pristup.',
				openCollectionOrSaveRecipients:
					'Otvorite Prikupljanje za pokretanje javnom poveznicom ili spremite primatelje ispod prije pokretanja.',
				launchPassedWithRecipients: 'Provjera je prošla sa spremljenim primateljima',
				openCollectionStartSavedRecipients:
					'Otvorite Prikupljanje za pokretanje vala i slanje spremljenim primateljima.',
				openCollectionLaunch: 'Otvori pokretanje u Prikupljanju',
				runLaunchCheck: 'Pokreni provjeru',
				needsAttention: 'Treba pažnju',
				resolveBeforeCollection:
					'Pokrenite provjeru pokretanja i riješite navedene probleme prije otvaranja Prikupljanja.',
				loadingSavedRecipients: 'Učitavanje spremljenog odabira primatelja...',
				savedSelections: (selectionCount: number, pairCount: number) =>
					`${selectionCount} ${selectionCount === 1 ? 'odabir spremljen' : 'odabira spremljeno'}, ${pairCount} ${
						pairCount === 1 ? 'pozivni par spreman' : 'pozivnih parova spremno'
					}.`,
				noSavedIdentified: 'Primatelji još nisu spremljeni; spremite ih prije invite-only pokretanja.',
				noSavedLongitudinal:
					'Nema spremljenih primatelja; spremite primatelje za invite-only pristup ili upotrijebite javnu poveznicu i neka sudionici unesu svoj kod ponovnog sudjelovanja.',
				noSavedAnonymous: 'Nema spremljenih primatelja; pokrenite javnom poveznicom ili spremite primatelje ispod.'
			},
			launchPlan: {
				title: 'Plan pokretanja',
				summary: 'Pripremite val, način odgovaranja, primatelje i prijenos u Prikupljanje prije pokretanja.',
				draftWave: 'Nacrt vala',
				wave: 'Val',
				responseMode: 'Način odgovaranja',
				recipients: 'Primatelji',
				collectionHandoff: 'Prijenos u Prikupljanje',
				waveDraftReady: (waveName: string) => `${waveName} je nacrt vala za ovu studiju.`,
				waveWillBeCreated: (waveName: string) =>
					`${waveName} nastat će kad spremite ovaj korak.`,
				identifiedModeDetail:
					'Identificirano prikupljanje zahtijeva spremljene primatelje kako bi svaki sudionik dobio dodijeljeni pristup.',
				longitudinalModeDetail:
					'Prikupljanje s ponovnim sudjelovanjem može koristiti javni pristup ili spremljene primatelje; sudionici koriste vlastiti ponovni kod za usporedbu.',
				anonymousModeDetail: 'Anonimno prikupljanje može koristiti javnu poveznicu ili spremljene pozive e-poštom.',
				chooseModeDetail: 'Odaberite kako sudionici ulaze u ovaj val.',
				savedRecipientDetail: (selectionCount: number, pairCount: number) =>
					`${selectionCount} ${selectionCount === 1 ? 'spremljen odabir' : 'spremljena odabira'} s ${pairCount} ${
						pairCount === 1 ? 'pozivnim parom' : 'pozivnih parova'
					}.`,
				identifiedNeedsRecipients: 'Identificirano prikupljanje treba spremljene primatelje prije pokretanja.',
				longitudinalNoRecipients:
					'Nema spremljenih primatelja. Možete koristiti javnu poveznicu ili spremiti primatelje za invite-only ponovno sudjelovanje.',
				anonymousNoRecipients:
					'Nema spremljenih primatelja. Anonimno prikupljanje još možete pokrenuti javnom poveznicom.',
				saveRecipientsBeforeIdentifiedLaunch:
					'Spremite primatelje prije otvaranja Prikupljanja za identificirano pokretanje.',
				launchPassedOpenCollection: 'Provjera je prošla; otvorite Prikupljanje za pokretanje vala.',
				runLaunchCheckBeforeCollection: 'Pokrenite provjeru prije otvaranja Prikupljanja.'
			},
			designMap: {
				title: 'Mapa dizajna studije',
				summary:
					'Ova mapa prikazuje spremljene dijelove postavljanja, a ne početnu opciju odabranu pri izradi studije.',
				source: 'Izvor studije',
				questionnaire: 'Upitnik',
				results: 'Postavljanje rezultata',
				waves: 'Valovi prikupljanja',
				sourceReady: 'Izvorni sadržaj spreman je za ovaj upitnik.',
				sourceMissing: 'Potvrdite izvorni ili uvezeni sadržaj prije spremanja upitnika.',
				questionnaireSaved: (name: string, questionCount: number) =>
					`${name} spremljen je s ${questionCount} ${questionCount === 1 ? 'pitanjem' : 'pitanja'}.`,
				questionnaireMissing: 'Spremite upitnik prije postavljanja rezultata ili provjere pokretanja.',
				resultsReady: (ruleKey: string) => `Postavljanje rezultata spremljeno je kao ${ruleKey}.`,
				resultsMissing: 'Odaberite koji odgovori iz upitnika postaju rezultati studije.',
				noWaves: 'Još nema vala prikupljanja.',
				draftWaveNeedsReadiness: (count: number) =>
					`${count} ${count === 1 ? 'nacrt vala pripremljen je' : 'nacrta valova pripremljena su'}; spremnost pokretanja još treba pažnju.`,
				waveReady: (count: number) =>
					`${count} ${count === 1 ? 'nacrt vala spreman je' : 'nacrta valova spremna su'} za Prikupljanje.`,
				liveWave: (count: number) =>
					`${count} ${count === 1 ? 'val prikuplja' : 'valova prikuplja'} odgovore.`,
				closedWave: (count: number) =>
					`${count} ${count === 1 ? 'val ima' : 'valova ima'} zatvorene podatke za pregled Rezultata.`
			},
			waveContext: {
				prepareForCollection: (waveName: string) => `Pripremite ${waveName} za prikupljanje`,
				firstWaveSetup: 'Postavljanje prvog vala',
				currentDraftWave: 'Trenutni nacrt vala',
				followUpDraftWave: 'Nacrt naknadnog vala',
				futureWaveSetup: 'Postavljanje budućeg vala',
				firstWaveSummary: 'Ovdje izradite prvi val prikupljanja i odlučite tko može odgovoriti.',
				currentDraftSummary: 'Dovršite trenutni nacrt vala prije otvaranja Prikupljanja.',
				followUpDraftSummary: (waveName: string) =>
					`${waveName} je nacrt naknadnog vala. Koristite ga samo kad je sljedeći krug prikupljanja namjeran.`,
				closedOneWaveSummary: (
					previousWaveName: string,
					previousWaveStatus: string,
					nextWaveName: string
				) =>
					`${previousWaveName} je već ${previousWaveStatus}. Izradite ${nextWaveName} samo kad je sljedeći krug prikupljanja namjeran.`,
				multipleWaveSummary: (existingWaveCount: number, nextWaveName: string) =>
					`${existingWaveCount} valova već postoji. Izradite ${nextWaveName} tek nakon pregleda rezultata trenutnog vala.`,
				createFirstAfterSetup:
					'Izradite Val 1 tek nakon što su upitnik i postavljanje rezultata spremljeni.',
				recipientBelongsUntilLaunch: (waveName: string) =>
					'Odabir primatelja pripada ' + waveName + ' dok se taj val ne pokrene.',
				reviewResultsBeforeFollowup:
					'Pregledajte prethodni val u Rezultatima prije nego ga tretirate kao naknadno prikupljanje.',
				doNotAssumeRecipients:
					'Nemojte pretpostaviti da su primatelji isti; spremite namjeravane ljude ili grupu za ovaj val.',
				reviewBeforePreparing: (previousWaveName: string, nextWaveName: string) =>
					`Pregledajte ${previousWaveName} prije pripreme ${nextWaveName}`,
				reviewExistingBeforePreparing: (nextWaveName: string) =>
					`Pregledajte postojeće valove prije pripreme ${nextWaveName}`,
				openResultsBeforeCreating: (reviewTarget: string, nextWaveName: string) =>
					`Otvorite Rezultate za pregled ili izvoz ${reviewTarget} prije izrade ${nextWaveName}.`,
				createOnlyWhenIntentional: (nextWaveName: string) =>
					`Izradite ${nextWaveName} samo kad je sljedeći krug prikupljanja namjeran.`,
				recipientBelongsToNewDraft: (previousLabel: string) =>
					`Odabir primatelja u ovom koraku pripadat će novom nacrtu vala, ne ${previousLabel}.`,
				previousWaves: 'prethodnim valovima'
			},
			misc: {
				notEditable: 'nije moguće uređivati',
				and: 'i'
			}
		},
		operationsBody: {
			progressAriaLabel: 'Tijek prikupljanja studije',
			progressKicker: 'Prikupljanje studije',
			progressTitle: 'Tijek prikupljanja',
			progressBody:
				'Pokrenite val, podijelite pristup sudionicima, pratite predaje i zatvorite prikupljanje kada studija završi.',
			stepsComplete: (completed: number, total: number) => `${completed}/${total} koraka dovršeno`,
			statusKicker: 'Status prikupljanja',
			nextAction: 'Sljedeća radnja',
			pathAriaLabel: 'Put prikupljanja',
			stepAriaLabel: 'Korak prikupljanja',
			readOnlyTitle: 'Pristup samo za čitanje',
			readOnlyBody: 'Radnje prikupljanja zahtijevaju pravo upravljanja radnim prostorom.',
			stepStatus: {
				working: 'U tijeku',
				saved: 'Spremljeno',
				failed: 'Neuspjelo',
				ready: 'Spremno'
			},
			pathStatus: {
				done: 'Gotovo',
				current: 'Trenutno',
				blocked: 'Blokirano'
			},
			common: {
				available: 'Dostupno',
				blocked: 'Blokirano',
				closed: 'Zatvoreno',
				created: 'Izrađeno',
				missing: 'Nedostaje',
				notAvailable: 'Nije dostupno',
				notChecked: 'Nije provjereno',
				ready: 'Spremno',
				status: 'Status',
				collectionWave: 'Val prikupljanja',
				setupCheck: 'Provjera postavljanja',
				started: 'Započeto',
				submitted: 'Predano',
				inProgress: 'U tijeku',
				latestActivity: 'Zadnja aktivnost',
				reportReadiness: 'Spremnost izvještaja',
				loaded: (count: string) => `${count} učitano`,
				reconciled: (count: string) => `${count} usklađeno`
			},
			readiness: {
				body:
					'Koristite ovo prije otvaranja prikupljanja. Provjera potvrđuje da upitnik, rezultati, primatelji i pravila mogu podržati odgovore i izvještavanje.',
				issuesAria: 'Problemi spremnosti',
				warningsTitle: 'Upozorenja postavljanja',
				blockersTitle: 'Prije početka prikupljanja',
				warningsBody:
					'Ove stavke ne blokiraju prikupljanje, ali ih pregledajte prije dijeljenja pristupa.',
				blockersBody: 'Popravite blokirajuće stavke postavljanja, zatim ponovno pokrenite provjeru.',
				blocking: 'Blokira',
				warning: 'Upozorenje',
				openSetup: 'Otvori Postavljanje',
				returnAndCheck: 'Vratite se ovdje i ponovno pokrenite provjeru nakon spremanja postavljanja.',
				blockedTitle: 'Postavljanje je blokirano',
				blockedBody:
					'Provjera nije vratila pojedinačne blokade. Otvorite Postavljanje, pregledajte nedovršene korake, spremite promjene i ponovno pokrenite provjeru.',
				runCheck: 'Pokreni provjeru prije pokretanja'
			},
			launch: {
				body:
					'Pokretanje prikupljanja otvara odabrani val za odgovore i bilježi verziju postavljanja koju će izvještaji kasnije koristiti.',
				start: 'Pokreni prikupljanje',
				resultLabel: 'Prikupljanje'
			},
			shareAccess: {
				body:
					'Odaberite kako sudionici ulaze u ovaj val. Spremljeni odabiri iz Imenika i grupa postaju privatne pozivnice. Jednokratni uvoz koristite samo za dodavanje ad hoc primatelja nakon pokretanja ili izradite otvorenu poveznicu kada je širok pristup prihvatljiv.',
				identifiedEntryLabel: 'Identificirani ulaz',
				inviteOnlyLabel: 'Pristup samo pozivnicom',
				openLinkLabel: 'Otvorena poveznica za sudionike',
				identifiedEntryTitle: 'Izradi identificirani ulaz za sudionika',
				privateInvitationsTitle: 'Privatne pozivnice su aktivne',
				openLinkReadyTitle: 'Otvorena poveznica je izrađena',
				createShareableLinkTitle: 'Izradi poveznicu za dijeljenje',
				openLinkDisabled: 'Otvorena poveznica isključena',
				openLinkActive: 'Otvorena poveznica aktivna',
				openLinkNotCreated: 'Nije izrađena',
				inviteOnly: 'Samo pozivnice',
				replaceLostLink: 'Zamijeni izgubljenu poveznicu',
				replaced: 'Zamijenjeno',
				oneActiveLink: 'Jedna aktivna poveznica',
				createIdentifiedAccessLink: 'Izradi identificiranu pristupnu poveznicu',
				createRespondentLink: 'Izradi poveznicu za sudionika',
				shareLink: 'Poveznica za dijeljenje',
				respondentLinkReady: 'Poveznica za sudionika spremna',
				identifiedHelp: 'Koristite samo kada odgovori trebaju biti povezani s poznatim zapisima osoba.',
				inviteOnlyHelp:
					'Ovaj val već ima privatne pozivnice e-poštom. Otvorene poveznice su isključene kako bi sudjelovanje ostalo ograničeno na pozvane primatelje.',
				openLinkReadyHelp:
					'Ovaj val već ima jednu aktivnu otvorenu poveznicu. Ako je poveznica izgubljena, zamijenite je ovdje. Stara poveznica prestat će prihvaćati nove sudionike; postojeće sesije odgovaranja mogu završiti kroz svoje privatne sesijske oznake.',
				openLinkHelp:
					'Koristite kada je široko anonimno sudjelovanje prihvatljivo i ne treba vam popis primatelja samo pozivnicom.'
			},
			emailSetup: {
				label: 'Postavljanje slanja e-pošte',
				title: 'Provjera slanja e-pošte prije slanja',
				body:
					'Ova provjera pokazuje može li okruženje slati stvarne SMTP pozivnice ili je još u testnom načinu rada odnosno nema potrebne postavke. Ne prikazuje tajne davatelja ni SMTP vjerodajnice.',
				mode: 'Način rada',
				realEmailSend: 'Stvarno slanje e-pošte',
				providerEvents: 'Događaji davatelja',
				webhookConfigured: 'Webhook je podešen',
				webhookDisabled: 'Webhook je isključen',
				checkEmailSetup: 'Provjeri slanje e-pošte'
			},
			simulation: {
				label: 'Testni odgovori',
				title: 'Simuliraj podatke odgovora',
				body:
					'Koristite u neprodukcijskim okruženjima za stvaranje uvjerljivih predanih odgovora bez slanja e-pošte.',
				responses: 'Odgovori',
				averageTarget: 'Ciljani prosjek',
				variation: 'Varijacija',
				tight: 'Usko',
				normal: 'Normalno',
				noisy: 'Šumovito',
				simulateCollection: 'Simuliraj prikupljanje',
				includeComments: 'Dodaj kratke sintetičke tekstualne odgovore kada upitnik ima polja za komentar.',
				answersSaved: 'Spremljeni odgovori',
				scoredResponses: 'Bodovani odgovori'
			},
			monitor: {
				body:
					'Pratite kretanje odgovora dok je prikupljanje otvoreno. Brojevi se osvježavaju iz stanja radnog prostora i ne mijenjaju postavljanje studije.',
				deliveryDiagnostics: 'Dijagnostika dostave',
				recentEmailEvents: 'Nedavni događaji dostave e-pošte',
				noEventsYet: 'Još nema događaja',
				providerEventsBody:
					'Koristite samo za rješavanje problema sa slanjem e-pošte. Prikazuje prihvaćeno, dostavljeno, odbijeno i spam-pritužbe bez prikaza primatelja, internih id-jeva, id-jeva davatelja ili razloga davatelja.',
				accepted: 'Prihvaćeno',
				delivered: 'Dostavljeno',
				bounced: 'Odbijeno',
				complained: 'Pritužbe',
				latestProviderEvent: 'Zadnji događaj davatelja',
				loadProviderEvents: 'Učitaj nedavne događaje davatelja',
				noRecentProviderEvents: 'Još nema nedavnih događaja davatelja za ovaj radni prostor.',
				refreshStatus: 'Osvježi status'
			},
			cleanup: {
				label: 'Čišćenje dostave e-pošte',
				title: 'Spremnost popravka',
				needsReview: 'Treba pregled',
				noCleanup: 'Nema čišćenja',
				notChecked: 'Nije provjereno',
				body:
					'Provjerite ovo prije ponovnog slanja neuspjelih pozivnica. Odvaja zastarjele predaje, nejasne neuspjehe, neuspjehe koje se može ponoviti i potisnute primatelje bez promjene stanja dostave.',
				staleHandoffs: 'Zastarjele predaje',
				ambiguousFailures: 'Nejasni neuspjesi',
				retryableFailures: 'Ponovljivi neuspjesi',
				suppressedFailures: 'Potisnuti neuspjesi',
				deliveryEvents: 'Događaji dostave',
				checkCleanupReadiness: 'Provjeri spremnost popravka',
				retryPossible: 'Ponovno slanje moguće'
			},
			close: {
				body:
					'Zatvorite prikupljanje kada prozor za odgovore završi. Predani odgovori ostaju dostupni za bodovanje i izvještaje.',
				closeCollection: 'Zatvori prikupljanje'
			},
			navigation: {
				ariaLabel: 'Navigacija koraka prikupljanja',
				previousStep: 'Prethodni korak',
				nextStep: 'Sljedeći korak',
				goToResults: 'Idi na rezultate'
			},
			email: {
				subject: 'Pozivnica za studiju',
				body:
					'Pozvani ste da ispunite studiju.\n\nRadi privatnosti, ova e-pošta ne uključuje naziv ni temu studije. Poveznica otvara stranicu studije prije nego odlučite hoćete li odgovoriti.\n\nOtvorite svoju poveznicu za studiju:\n[jedinstvena poveznica sudionika]\n\nAko ste već odgovorili, ovu poruku možete zanemariti.\n\nAko više ne biste trebali primati pozivnice za studije iz ovog radnog prostora, odjavite se ovdje:\n[poveznica za odjavu]\n\n[podnožje pozivnice radnog prostora]'
			}
		},		operationsWorkflow: {
			stepNumber: (number: number) => `${number}`,
			actions: {
				readiness: {
					title: 'Provjera prije pokretanja',
					description: 'Potvrdite da su upitnik, postavljanje rezultata, primatelji i pravila spremni.'
				},
				launch: {
					title: 'Pokretanje prikupljanja',
					description: 'Otvorite ovaj val za odgovore i zabilježite postavke korištene za izvještavanje.'
				},
				openLink: {
					title: 'Dijeljenje pristupa',
					description: 'Pošaljite spremljene pozive ili izradite otvorenu poveznicu za ovaj val.'
				},
				monitor: {
					title: 'Praćenje odgovora',
					description: 'Pratite početke, nacrte, predaje i spremnost izvještaja dok prikupljanje traje.'
				},
				close: {
					title: 'Zatvaranje prikupljanja',
					description: 'Zaustavite nove odgovore, a predane podatke zadržite dostupnima za izvještaje.'
				}
			},
			disabled: {
				createWaveBeforeReadiness: 'Izradite val prikupljanja u Postavljanju prije provjere spremnosti.',
				createWaveBeforeStart: 'Izradite val prikupljanja prije pokretanja prikupljanja.',
				startBeforeAccess: 'Pokrenite prikupljanje prije pripreme pristupa sudionicima.',
				startBeforeMonitor: 'Pokrenite prikupljanje prije praćenja odgovora.',
				createWaveBeforeClose: 'Izradite val prikupljanja prije zatvaranja prikupljanja.',
				waveClosed: 'Ovaj val prikupljanja je zatvoren.',
				alreadyLive: 'Prikupljanje je već aktivno.',
				startedThisSession: 'Prikupljanje je pokrenuto u ovoj sesiji.',
				runPrelaunchAndSetup:
					'Pokrenite provjeru prije pokretanja. Ako kaže Blokirano, otvorite Postavljanje i dovršite navedene stavke.',
				onlyLiveClosable: 'Zatvoriti se može samo aktivan val prikupljanja.'
			},
			status: {
				lifecycleLabel: 'Životni ciklus prikupljanja',
				responseProgressLabel: 'Napredak odgovora',
				accessLabel: 'Pristup',
				reportingReadinessLabel: 'Spremnost izvještaja',
				noWaveSelectedTitle: 'Nijedan val nije odabran',
				noWaveSelectedDetail: 'Izradite ili odaberite val prikupljanja prije prikupljanja odgovora.',
				noResponsesYetTitle: 'Još nema odgovora',
				noResponsesYetDetail: 'Brojevi odgovora pojavit će se nakon pokretanja vala.',
				noRecipientAccessTitle: 'Pristup sudionicima nije pripremljen',
				noRecipientAccessDetail: 'Odaberite primatelje ili izradite pristup sudionicima nakon što je postavljanje spremno.',
				reportingNotAvailableTitle: 'Nije dostupno',
				reportingNotAvailableDetail: 'Spremnost izvještaja prikazuje se nakon što prikupljanje ima odabrani val.',
				createWaveFirstHeadline: 'Prvo izradite val prikupljanja',
				createWaveFirstGuidance: 'Prikupljanje počinje nakon što Postavljanje ima val kampanje.',
				createWaveFirstNextAction: 'Otvorite Postavljanje i izradite val prikupljanja.',
				closedTitle: 'Zatvoreno',
				closedDetail: 'Ovaj val više ne prihvaća nove odgovore.',
				liveTitle: 'Aktivno: prihvaća odgovore',
				liveDetail: 'Sudionici još mogu predati odgovore. Rezultati ostaju preliminarni dok se prikupljanje ne zatvori.',
				draftTitle: 'Nacrt: još ne prikuplja',
				draftDetail: 'Pokrenite provjeru prije pokretanja, zatim pokrenite prikupljanje.',
				submittedTitle: (submitted: string) => `${submitted} predano`,
				responseActivityDetail: (started: string, drafts: string, submitted: string) =>
					`${started} započeto, ${drafts} u tijeku, ${submitted} predano.`,
				waitingForResponsesTitle: 'Čekanje odgovora',
				waitingForResponsesDetail: 'Prikupljanje je otvoreno, ali još nema zabilježene aktivnosti odgovora.',
				notCollectingTitle: 'Još ne prikuplja',
				notCollectingDetail: 'Pokrenite prikupljanje prije praćenja odgovora.',
				accessNotPreparedTitle: 'Pristup nije pripremljen',
				accessNotPreparedDetail: 'Izradite poveznicu za sudionike ili pripremite pozive prije očekivanja odgovora.',
				accessWaitsForLaunchTitle: 'Pristup čeka pokretanje',
				accessWaitsForLaunchDetail:
					'Spremite primatelje u Postavljanju prije pokretanja ili pokrenite prikupljanje prije izrade otvorene poveznice.',
				resultsPreliminaryDetail:
					'Rezultati se mogu pregledati, ali podatke aktivnog prikupljanja tretirajte kao preliminarne dok se prikupljanje ne zatvori.',
				reportingUsefulAfterSubmitted: 'Izvještavanje postaje korisno nakon što postoje predani odgovori.',
				closedOverallLabel: 'Zatvoreno',
				closedHeadline: (submitted: string) => `Zatvoreno: ${submitted} predanih odgovora`,
				closedGuidance: 'Prikupljanje je zatvoreno. Predani odgovori stabilni su za pregled Rezultata.',
				closedNextAction: 'Otvorite Rezultate za pregled nalaza i izvoza.',
				liveOverallLabel: 'Aktivno',
				liveHeadline: (submitted: string) => `Aktivno: prihvaća odgovore s ${submitted} predanim odgovorom`,
				liveGuidance:
					'Koristite ovu stranicu za praćenje napretka odgovora i pristupa sudionicima. Zatvorite prikupljanje kad prozor odgovaranja završi.',
				liveNextWithResponses:
					'Nastavite prikupljati, pregledajte preliminarne Rezultate ili zatvorite prikupljanje kad ste spremni.',
				liveNextNoResponses: 'Podijelite pristup sudionicima i pričekajte predane odgovore.',
				draftOverallLabel: 'Nacrt',
				draftHeadline: 'Nacrt: prikupljanje nije pokrenuto',
				draftGuidance: 'Pokrenite provjeru prije pokretanja prije dijeljenja pristupa sudionicima.',
				draftNextAction: 'Pokrenite provjeru prije pokretanja.',
				identifiedAccessTitle: 'Identificirani pristup pripremljen',
				inviteOnlyAccessTitle: 'Invite-only pristup pripremljen',
				openLinkAccessTitle: 'Otvorena poveznica pripremljena',
				recipientAccessTitle: 'Pristup primatelja pripremljen',
				identifiedAccessDetail: (openLinkCount: string, pluralSuffix: string) =>
					`${openLinkCount} identificirana pristupna poveznica${pluralSuffix} pripremljena. Sudionici su povezani s poznatim zapisima osoba za ovaj val.`,
				inviteOnlyDetail: (invitationCount: string, verb: string, boundary: string) =>
					`${invitationCount} spremljenih poziva e-poštom ${verb} za ovaj val. Samo spremljeni primatelji dobivaju privatni pristup, a ${boundary}`,
				mixedAccessDetail: (
					openLinkCount: string,
					openPluralSuffix: string,
					invitationCount: string,
					invitationPluralSuffix: string,
					boundary: string
				) =>
					`${openLinkCount} otvorenih poveznica${openPluralSuffix} i ${invitationCount} spremljenih poziva${invitationPluralSuffix}. Otvorena poveznica je širok pristup; invite-only e-pošta ograničava ulaz na spremljene primatelje. ${boundary}`,
				openLinkDetail: (openLinkCount: string, verb: string) =>
					`${openLinkCount} otvorenih poveznica ${verb}. Svatko s poveznicom može ući u ovaj val; koristite spremljene pozive kad pristup treba biti ograničen.`,
				createAccessBeforeResponses:
					'Izradite poveznicu za sudionike ili spremljene pozive e-poštom prije očekivanja odgovora.',
				anonymousBoundary: 'anonimni izvještaji i dalje ne prikazuju tko je odgovorio.',
				anonymousBoundarySentence: 'Anonimni izvještaji i dalje odvajaju identitet sudionika od rezultata.',
				longitudinalBoundary:
					'rezultati ponovnog sudjelovanja koriste kodove sudionika umjesto prikaza tko je odgovorio.',
				longitudinalBoundarySentence:
					'Usporedba ponovnog sudjelovanja koristi kodove sudionika; popisi e-pošte primatelja ne prikazuju se u rezultatima.',
				notAvailable: 'Nije dostupno'
			}
		},
		reportsWorkflow: {
			stepNumber: (number: number) => `${number}`,
			actions: {
				reportProof: {
					title: 'Pregled rezultata',
					description: 'Pregledajte sažetke rezultata za odabrani val bez narušavanja pravila prikaza.'
				},
				exportArtifact: {
					title: 'Izradi sažetak izvještaja',
					optionalTitle: 'Sažetak izvještaja nije obavezan',
					description:
						'Izradite agregirani CSV rezultata i knjigu kodova. Izvan tima ga koristite tek nakon pregleda značenja i konačnosti.',
					optionalDescription:
						'Skup odgovora već postoji. Sažetak izvještaja je neobavezan i nije potreban prije preuzimanja.'
				},
				responseExport: {
					title: 'Izradi izvoz odgovora',
					description: 'Izradite redove odgovora i knjigu kodova spremne za analizu ove studije.'
				},
				fetchArtifact: {
					title: 'Pregled datoteke izvoza',
					description: 'Pregledajte najnoviju datoteku izvoza prije preuzimanja.'
				},
				downloadCsv: {
					responseDatasetTitle: 'Preuzmi CSV skupa odgovora',
					responseDatasetDescription:
						'Preuzmite CSV skupa odgovora i knjigu kodova kada su spremni za analizu.',
					reportSummaryTitle: 'Preuzmi CSV sažetka izvještaja',
					reportSummaryDescription:
						'Preuzmite CSV sažetka izvještaja samo za interni pregled. To nije skup odgovora spreman za analizu.'
				}
			},
			disabled: {
				createOrSelectWaveBeforeReviewingResults: 'Izradite ili odaberite val prije pregleda rezultata.',
				resolveReportPrerequisitesBeforeReviewingResults:
					'Riješite preduvjete izvještavanja prije pregleda rezultata.',
				reviewResultsBeforeCreatingReportExport: 'Pregledajte rezultate prije izrade sažetka izvještaja.',
				resolveReportPrerequisitesBeforeCreatingReportExport:
					'Riješite preduvjete izvještavanja prije izrade sažetka izvještaja.',
				reportExportCreatedThisSession: 'Sažetak izvještaja izrađen je u ovoj sesiji.',
				responseDatasetAlreadyExistsReportOptional:
					'Skup odgovora već postoji; sažetak izvještaja nije obavezan.',
				reportSummaryExportAlreadyExists: 'Sažetak izvještaja već postoji za ovu studiju.',
				reviewResultsBeforeCreatingResponseExport: 'Pregledajte rezultate prije izrade izvoza odgovora.',
				resolveReportPrerequisitesBeforeCreatingResponseExport:
					'Riješite preduvjete izvještavanja prije izrade izvoza odgovora.',
				responseExportCreatedThisSession: 'Izvoz odgovora izrađen je u ovoj sesiji.',
				responseExportAlreadyExists: 'Izvoz odgovora već postoji za ovu studiju.',
				createOrSelectExportBeforeReview: 'Izradite ili odaberite datoteku izvoza prije pregleda.',
				createOrSelectExportBeforeDownload: 'Izradite ili odaberite datoteku izvoza prije preuzimanja CSV-a.',
				selectDownloadableExportBeforeDownload:
					'Odaberite datoteku izvoza koja se može preuzeti prije preuzimanja CSV-a.'
			},
			packetReview: {
				title: 'Mogu li se ovi rezultati koristiti?',
				description:
					'Provjerite imate li odgovore, vidljive rezultate, datoteku izvoza i jasnu granicu korištenja.',
				primaryAction: {
					noCampaign: 'Izradite ili odaberite val prije pregleda rezultata.',
					noResponses: 'Prikupite odgovore prije pregleda rezultata.',
					noVisibleScores:
						'Koristite sirovi izvoz odgovora za internu analizu ili pregledajte bodovanje, pravila nedostajućih odgovora i pravila prikaza.',
					createExport:
						'Izradite izvoz odgovora za analizu ili datoteku sažetka izvještaja za interni pregled.',
					downloadDataset: 'Preuzmite skup odgovora za analizu.',
					documentInterpretation:
						'Skup odgovora koristite interno; dokumentirajte značenje rezultata prije dijeljenja zaključaka.',
					preliminary: 'Koristite kao preliminarne interne podatke dok se prikupljanje ne zatvori.'
				}
			},
			scoreMethodReview: {
				title: 'Kako su ovi rezultati izračunati?',
				description:
					'Pregledajte izlaze bodovanja, pokrivenost, postupanje s nedostajućim odgovorima i granice tumačenja prije korištenja rezultata.'
			},
			exportPreview: {
				title: 'Što je u ovom izvozu?',
				description:
					'Pregledajte namjenu datoteke, oblik redaka, polja vala, ključeve praćenja, varijable, nedostajuće vrijednosti i rezultate prije preuzimanja.',
				createOrSelectWaveFirst: 'Prvo izradite ili odaberite val',
				reviewExportFileFirst: 'Prvo pregledajte datoteku izvoza',
				selectWavePendingDetail: 'Odaberite val prije pripreme datoteka izvoza.',
				reviewFilePendingDetail: 'Pregledajte datoteku izvoza kako biste provjerili CSV i knjigu kodova.',
				downloadResponseDatasetCsv: 'Preuzmi CSV skupa odgovora',
				downloadReportSummaryCsv: 'Preuzmi CSV sažetka izvještaja'
			}
		},		wavesWorkflow: {
			stepNumber: (number: number) => `${number}`,
			plan: {
				createFirstTitle: 'Izradite prvi val',
				createFirstDescription: 'Počnite izradom Vala 1 kao prvog kruga prikupljanja za ovu studiju.',
				openSetupLabel: 'Otvori Postavljanje',
				createFirstGuidance: [
					'Svaki val je krug prikupljanja unutar studije. Izradite Val 1 u Postavljanju, zatim ga pokrenite iz Prikupljanja.',
					'Nakon što odgovori stignu, pregledajte val u Rezultatima prije dodavanja sljedećeg vala.',
					'Koristite anonimno longitudinalno sudjelovanje od prvog vala ako kasnije trebate povezanu promjenu kroz vrijeme.'
				],
				reviewWavePairTitle: (wavePairTitle: string) => `Pregledajte ${wavePairTitle}`,
				groupTrendReviewDescription:
					'Ovi valovi mogu se pregledati kao rezultati na razini grupe. Povezana promjena istih sudionika traži ponovljeno sudjelovanje od prvog vala.',
				reviewGroupTrendLabel: 'Pregledaj grupni trend',
				groupTrendReviewGuidance: (nextWaveNumber: number) => [
					'Pregledajte ove valove kao trend na razini grupe. Nemojte ga opisivati kao promjenu istih sudionika jer su valovi anonimni.',
					'Koristite ponovljeno sudjelovanje od Vala 1 kada studija kasnije treba povezanu promjenu kroz vrijeme.',
					`Pregledajte ili izvezite Val 1 i Val 2 prije izrade Vala ${nextWaveNumber} u Postavljanju.`
				],
				oneWaveTitle: (nextWaveNumber: number) => `Pregledajte Val 1 prije planiranja Vala ${nextWaveNumber}`,
				oneWaveDescription:
					'Val 1 postoji. Prvo pregledajte trenutne rezultate; sljedeći val planirajte samo kada je novi krug prikupljanja namjeran.',
				reviewWaveResultsLabel: (waveNumber: number) => `Pregledaj rezultate Vala ${waveNumber}`,
				planWaveLaterLabel: (waveNumber: number) => `Planiraj Val ${waveNumber} kasnije`,
				oneWaveGuidance: (nextWaveNumber: number) => [
					`Pregledajte ili izvezite Val 1 prije izrade Vala ${nextWaveNumber} u Postavljanju.`,
					'Koristite anonimno longitudinalno sudjelovanje kada isti sudionik treba biti povezan kroz valove.',
					'Pregledajte primatelje prije pokretanja novog vala; nemojte pretpostaviti da je publika ista osim ako to Prikupljanje jasno pokazuje.'
				],
				checkReadinessTitle: 'Provjera povezane promjene',
				checkReadinessDescription:
					'Postoje dva longitudinalna vala. Sada potvrdite povezane putanje i kompatibilnost bodovanja.',
				runChecksBelowLabel: 'Pokreni provjere u nastavku',
				reviewResultsLabel: 'Pregledaj rezultate',
				checkReadinessGuidance: [
					'Koristite provjere u nastavku kako biste potvrdili da se oba vala mogu sigurno povezati.',
					'Rezultati ostaju po valovima dok putanje, prikaz i bodovanje nisu spremni za povezanu usporedbu.',
					'Ako je usporedba blokirana, u detaljima pogledajte koji preduvjet nedostaje.'
				],
				sameRespondentTitle: 'Provjeri promjenu istih sudionika',
				sameRespondentDescription:
					'Postoje dva vala s ponovljenim sudjelovanjem. Pokrenite provjere prije nego što ovo tretirate kao promjenu istih sudionika.',
				runLinkedChecksBelowLabel: 'Pokreni povezane provjere',
				sameRespondentGuidance: [
					'Provjerite povezane odgovore, pravila prikaza, kompatibilnost bodovanja i vidljive promjene prije tvrdnji o promjeni kroz vrijeme.',
					'Koristite Rezultate za izvoz po valovima; koristite Valove samo kada trebate pregledan kontekst promjene kroz vrijeme.',
					'Novi sljedeći val izradite u Postavljanju kada počinje novi krug prikupljanja.'
				]
			},
			groupTrend: {
				notReadyTitle: 'Grupni trend nije spreman',
				notReadyDescription: 'Prikupite odgovore u barem dva vala prije pregleda trenda po valovima.',
				sameRespondentComparisonLabel: 'Usporedba istih sudionika',
				notReadySameRespondentValue: 'Nije dostupno dok ne postoje dva ponovljena vala',
				disclosureStatusLabel: 'Status prikaza',
				notReadyDisclosureValue: 'Pregledajte nakon što postoje rezultati sljedećeg vala',
				notReadyGuidance: [
					'Grupni trend uspoređuje rezultate na razini vala. Ne traži povezivanje sudionika.',
					'Pokrenite i prikupite sljedeći val prije čitanja trenda.',
					'Koristite ponovljeno sudjelovanje ako trebate promjenu istih sudionika, a ne samo pomak na razini vala.'
				],
				title: (baselineName: string, comparisonName: string) =>
					`Samo grupni trend: ${baselineName} prema ${comparisonName}`,
				readyDescription:
					'Agregirani rezultati na razini grupe spremni su za pregled kao trend. To nije promjena istih sudionika.',
				pendingDescription:
					'Oba vala imaju odgovore. Dovršite rezultate bodovanja prije nego trend tretirate kao spreman.',
				firstWaveScoresLabel: 'Rezultati prvog vala',
				secondWaveScoresLabel: 'Rezultati drugog vala',
				runComparisonChecksValue: 'Pokrenite provjere prije tvrdnji o promjeni istih sudionika',
				notConfiguredValue: 'Nije konfigurirano za povezanu promjenu istih sudionika',
				disclosureNotAvailableValue: 'Pregledajte prikaz po valovima u Rezultatima prije tvrdnji',
				suppressedLinkedComparisonsLabel: 'Skrivene povezane usporedbe',
				openResultsLabel: 'Otvori Rezultate',
				readyGuidance: [
					'Koristite ovo za anonimne ili nepovezane valove kada je pitanje je li se grupa pomaknula između krugova.',
					'Nemojte ovo opisivati kao individualno poboljšanje ili pogoršanje osim ako je povezana promjena spremna.',
					'Pregledajte bodovanje i pravila prikaza u Rezultatima prije tvrdnji iz trenda.'
				]
			},
			comparisonReview: {
				title: 'Plan usporedbe',
				description:
					'Provjerite je li studija spremna za sljedeći val, agregirani grupni trend ili povezanu promjenu istih sudionika.'
			},
			scoreMethodReview: {
				title: 'Što se uspoređuje?',
				description:
					'Pregledajte pravila bodovanja, metodu povezanih parova, uspoređene izlaze, nedostajuće vrijednosti i granice tumačenja prije korištenja promjene kroz valove.'
			},
			actions: {
				twoWaveProof: {
					title: 'Provjera povezane promjene',
					description:
						'Potvrdite da studija ima valove s ponovljenim sudjelovanjem i povezane odgovore za usporedbu istih sudionika.'
				},
				waveComparisonProof: {
					title: 'Pregled povezane promjene',
					description: 'Pregledajte promjenu istih sudionika između odabranih valova bez narušavanja pravila prikaza.'
				}
			},
			disabled: {
				unlinkedWavesUseGroupTrend:
					'Povezana usporedba istih sudionika nije dostupna jer ovi valovi nisu izrađeni s ponovljenim sudjelovanjem. Pregledajte grupni trend.',
				addRepeatedWaves: 'Dodajte barem dva ponovljena vala prije usporedbe promjene kroz vrijeme.',
				chooseBaselineAndComparison: 'Odaberite početni i usporedni val prije pregleda promjene kroz vrijeme.',
				checkReadinessBeforeReview: 'Provjerite spremnost usporedbe prije pregleda promjene kroz vrijeme.'
			},
			inactiveReason: {
				groupTrend:
					'Ova studija podržava samo agregirani grupni trend. Provjere povezane promjene nisu potrebne i bile bi zavaravajuće.',
				noWaves: 'Izradite i prikupite prve valove prije provjera povezane promjene.',
				oneWave:
					'Pregledajte Val 1 u Rezultatima. Planirajte Val 2 iz Postavljanja samo kada je sljedeći krug prikupljanja namjeran.',
				needScoredResponses: 'Prikupite bodovane odgovore u barem dva vala prije zadataka usporedbe.'
			}
		},
		waveSnapshot: {
			status: {
				notAvailable: 'Nije dostupno',
				blocked: 'Blokirano',
				previewReady: 'Pregled spreman',
				previewAvailable: 'Pregled dostupan',
				failed: 'Neuspjelo',
				loading: 'Učitavanje',
				ready: 'Spremno'
			},
			disabled: {
				selectComparableWaves:
					'Odaberite dva usporediva vala prije učitavanja pregleda usporedbe.',
				runLinkedTrajectoryCheck:
					'Pokrenite provjeru povezanih putanja prije učitavanja pregleda usporedbe.'
			},
			dashboard: {
				unavailableTitle: 'Pregled valova nije dostupan',
				unavailableMessage: 'Odaberite dva usporediva vala prije pregleda nadzorne ploče valova.',
				title: (baselineName: string, comparisonName: string) =>
					`${baselineName} prema ${comparisonName} pregled valova`,
				campaigns: 'Valovi',
				longitudinalWaves: 'Longitudinalni valovi',
				submittedWaves: 'Valovi s odgovorima',
				missingPrerequisites: 'Nedostajući preduvjeti',
				baselineWave: 'Početni val',
				baselineStatus: 'Status početnog vala',
				baselineSubmittedResponses: 'Predani odgovori početnog vala',
				comparisonWave: 'Usporedni val',
				comparisonStatus: 'Status usporednog vala',
				comparisonSubmittedResponses: 'Predani odgovori usporednog vala',
				linkedTrajectories: 'Povezane putanje',
				completeTrajectories: 'Potpune putanje',
				previewStatus: 'Status pregleda',
				interpretation: 'Tumačenje',
				linkedPairs: 'Povezani parovi',
				disclosure: 'Prikaz',
				disclosureK: 'Minimalna grupa',
				compatibility: 'Kompatibilnost',
				visibleScores: 'Vidljivi rezultati',
				suppressedScores: 'Skriveni rezultati',
				blockedScores: 'Blokirani rezultati',
				baselineLaunchSnapshot: 'Početni zapis pokretanja',
				baselineLatestLaunch: 'Zadnje pokretanje početnog vala',
				baselineScoringRule: 'Bodovanje početnog vala',
				baselineDisclosurePolicy: 'Pravilo prikaza početnog vala',
				comparisonLaunchSnapshot: 'Usporedni zapis pokretanja',
				comparisonLatestLaunch: 'Zadnje pokretanje usporednog vala',
				comparisonScoringRule: 'Bodovanje usporednog vala',
				comparisonDisclosurePolicy: 'Pravilo prikaza usporednog vala',
				untitledWave: 'Neimenovani val'
			},
			chrome: {
				sectionAria: 'Pregled usporedbe valova',
				kicker: 'Usporedba valova',
				title: 'Uspoređeni valovi',
				description: 'Usporedba odabranog početnog i usporednog vala uz pravila prikaza.',
				summaryAria: 'Sažetak usporedbe valova',
				readinessKicker: 'Spremnost usporedbe',
				readinessTitle: 'Mogu li se ovi valovi usporediti?',
				readinessDescription:
					'Provjerava mogu li se odabrani valovi usporediti bez otkrivanja premalih grupa.',
				waveReadinessAria: 'Spremnost valova',
				waveReadinessKicker: 'Spremnost',
				waveReadinessTitle: 'Spremnost valova',
				comparisonAria: 'Status usporedbe',
				comparisonKicker: 'Usporedba',
				comparisonTitle: 'Status usporedbe',
				guardrailsAria: 'Prikaz i kompatibilnost',
				guardrailsKicker: 'Zaštitna pravila',
				guardrailsTitle: 'Prikaz i kompatibilnost',
				sourceAria: 'Kontekst izvora valova',
				sourceKicker: 'Temeljeno na',
				sourceTitle: 'Kontekst pokretanja i pravila',
				resolvePrerequisites: 'Riješite preduvjete usporedbe valova prije učitavanja pregleda.',
				loadFailed: 'Pregled usporedbe valova nije se mogao učitati.',
				loadingComparison: 'Učitavanje usporedbe',
				refreshComparison: 'Osvježi usporedbu valova',
				study: 'Studija'
			},
			codeLabels: {
				proof_only: 'pregled',
				not_validated_interpretation: 'tumačenje nije validirano',
				visible: 'vidljivo',
				suppressed: 'skriveno',
				compatible: 'kompatibilno',
				not_available: 'nije dostupno',
				live: 'u tijeku',
				draft: 'nacrt',
				closed: 'zatvoreno'
			}
		},
		reportWidgets: {
			notAvailable: 'Nije dostupno',
			yes: 'Da',
			no: 'Ne',
			labels: {
				available: 'Dostupno',
				campaign: 'Val',
				campaignStatus: 'Status vala',
				closedAt: 'Zatvoreno',
				closedWave: 'Zatvoren val',
				completed: 'Dovršeno',
				coverageStatus: 'Status pokrivenosti',
				created: 'Izrađeno',
				currentResultSummary: 'Trenutni sažetak rezultata',
				dataFinality: 'Finalnost podataka',
				disclosure: 'Prikaz',
				disabled: 'Onemogućeno',
				download: 'Preuzimanje',
				enabled: 'Omogućeno',
				exportActions: 'Akcije izvoza',
				exportFiles: 'Izvozne datoteke',
				exportFileDataUnavailable: 'Podaci o izvoznim datotekama nisu dostupni.',
				exportState: 'Stanje izvoza',
				failureReason: 'Razlog neuspjeha',
				finalityDataUnavailable: 'Podaci o finalnosti i porijeklu nisu dostupni.',
				interpretation: 'Tumačenje',
				latestExport: 'Zadnji izvoz',
				latestLaunch: 'Zadnje pokretanje',
				latestScoringActivity: 'Zadnja aktivnost bodovanja',
				listed: 'Prikazano',
				noExportFiles: 'Nema zabilježenih izvoznih datoteka.',
				notConfigured: 'Nije postavljeno',
				notConfiguredState: 'Nije postavljeno',
				notSelected: 'Nije odabrano',
				previewReady: 'Pregled spreman',
				previewSource: 'Izvor pregleda',
				preliminaryLive: 'Preliminarno uživo',
				readinessDataUnavailable: 'Podaci o spremnosti nisu dostupni.',
				reportable: 'Za izvještaj',
				reportableCampaigns: 'Valovi za izvještaj',
				reportReadinessPrerequisites: 'Preduvjeti spremnosti rezultata',
				reportPreview: 'pregled rezultata',
				reportStatus: 'Status rezultata',
				ready: 'Spremno',
				readyToRun: 'Spremno za pokretanje',
				resultsPreview: 'Pregled rezultata',
				resultsPreviewLoading: 'Pregled rezultata se učitava.',
				resultsPreviewUnavailable: 'Pregled rezultata nije dostupan',
				resultsPreviewUnavailableExport:
					'Tijek izvoza i dalje se može koristiti dok pregled nije dostupan.',
				resultsPreviewWidgets: 'Kartice pregleda rezultata',
				resultsSummary: 'Sažetak rezultata',
				rows: 'Redci',
				scoreCoverageDataUnavailable: 'Podaci o pokrivenosti bodovanja nisu dostupni.',
				scored: 'Bodovano',
				scores: 'Rezultati',
				selectedCampaign: 'Odabrani val',
				selectedCampaignReportStateUnavailable:
					'Stanje rezultata odabranog vala nije dostupno.',
				size: 'Veličina',
				submitted: 'Predano',
				submittedResponses: 'Predani odgovori',
				suppressed: 'Skriveno',
				suppressedScores: 'Skriveni rezultati',
				unavailable: 'Nedostupno',
				unscored: 'Nebodovano',
				visible: 'Vidljivo',
				visibleScores: 'Vidljivi rezultati',
				visualAnalyticsDataUnavailable: 'Podaci za vizualni pregled nisu dostupni.'
			},
			codeLabels: {
				proof_only: 'pregled',
				not_validated_interpretation: 'tumačenje nije validirano',
				not_available: 'nije dostupno',
				visible: 'vidljivo',
				suppressed: 'skriveno',
				complete: 'potpuno',
				partial: 'djelomično',
				no_submissions: 'bez odgovora',
				ready_for_aggregate_report: 'spremno za agregirani izvještaj',
				closed_wave: 'zatvoren val',
				preliminary_live: 'preliminarni podaci uživo',
				not_reportable: 'nije za izvještaj',
				succeeded: 'uspjelo',
				failed: 'neuspjelo',
				queued: 'u redu čekanja',
				rendering: 'izrada',
				csv: 'CSV'
			}
		},
		surfaceChrome: {
			loadingContext: (surface: string) => `Učitavanje konteksta: ${surface}`,
			missingStudy: 'Odaberite studiju prije otvaranja ove površine.',
			surfaceUnavailableFallback: 'Površina odabrane studije nije se mogla učitati.',
			resultsSummaryUnavailable: 'Sažetak rezultata nije se mogao učitati.',
			errorTitle: 'Odabrana studija nije dostupna',
			retry: 'Pokušaj ponovno',
			readOnlyStateAria: 'Stanje primjera studije samo za čitanje',
			ownershipKicker: 'Vlasništvo studije',
			readOnly: 'Samo za čitanje',
			scoreCoverageUnavailable: 'Pokrivenost bodovanja nije dostupna.',
			noMissingScores: 'Nema predanih odgovora bez bodovanja za popravak.',
			actionStates: {
				running: 'U tijeku',
				done: 'Gotovo',
				failed: 'Neuspjelo',
				ready: 'Spremno'
			},
			collectionDetails: {
				summary: 'Detalji prikupljanja',
				kicker: 'Detalji prikupljanja',
				title: 'Operativni detalji',
				description:
					'Detalji za audit i rješavanje problema za ovaj val prikupljanja. Većina rada prikupljanja treba biti u tijeku iznad.',
				monitorAria: 'Nadzor prikupljanja',
				monitorKicker: 'Nadzor prikupljanja',
				monitorTitle: 'Prikupljanje odgovora',
				scoreCoverageAria: 'Pokrivenost bodovanja',
				scoreCoverageKicker: 'Pokrivenost bodovanja',
				scoreCoverageTitle: 'Pokrivenost bodovanja predanih odgovora',
				remediatingScores: 'Popravljanje bodovanja',
				remediateMissingScores: 'Popravi nedostajuća bodovanja',
				remediationRequiresAccess: 'Popravak bodovanja zahtijeva pristup za upravljanje postavljanjem.',
				resultAria: 'Rezultat popravka bodovanja',
				submittedResponses: 'Predani odgovori',
				eligibleSubmitted: 'Prihvatljivi predani',
				alreadyScored: 'Već bodovano',
				remediated: 'Popravljeno',
				notConfigured: 'Nije postavljeno',
				latestScoringActivity: 'Zadnja aktivnost bodovanja',
				prerequisitesAria: 'Nedostajući uvjeti prikupljanja',
				prerequisitesKicker: 'Preduvjeti',
				prerequisitesTitle: 'Nedostajući uvjeti prikupljanja'
			},
			resultsDetails: {
				summary: 'Detalji rezultata',
				kicker: 'Detalji rezultata',
				title: 'Audit i rješavanje problema',
				description:
					'Koristite ove detalje kad su rezultati ili izvozi blokirani. Normalan pregled i izvoz trebaju biti u tijeku iznad.',
				readinessAria: 'Spremnost rezultata',
				readinessKicker: 'Spremnost',
				readinessTitle: 'Što je spremno?',
				scoreCoverageAria: 'Pokrivenost bodovanja',
				scoreCoverageKicker: 'Pokrivenost bodovanja',
				scoreCoverageTitle: 'Spremnost izvještaja',
				selectedCampaignAria: 'Odabrani val rezultata',
				selectedWaveKicker: 'Odabrani val',
				reportStateTitle: 'Stanje izvještaja',
				sourceAria: 'Kontekst izvora rezultata',
				basedOn: 'Temeljeno na',
				launchPolicyExport: 'Pokretanje, pravila i kontekst izvoza',
				prerequisitesAria: 'Nedostajući uvjeti rezultata',
				prerequisitesKicker: 'Preduvjeti',
				prerequisitesTitle: 'Nedostajući uvjeti rezultata',
				waves: 'Valovi',
				includedWaves: 'Uključeni valovi rezultata'
			},
			wavesDetails: {
				summary: 'Detalji valova',
				kicker: 'Detalji valova',
				title: 'Detalji usporedbe',
				description:
					'Koristite ove detalje kad je usporedba valova blokirana ili treba audit kontekst. Normalna usporedba treba biti u tijeku iznad.',
				comparedWavesAria: 'Uspoređeni valovi',
				comparedWavesKicker: 'Uspoređeni valovi',
				selectedComparison: 'Odabrana usporedba',
				baselineWave: 'Početni val',
				comparisonWave: 'Usporedni val',
				comparisonStatus: 'Status usporedbe',
				disclosure: 'Prikaz',
				compatibility: 'Kompatibilnost',
				missing: 'Nedostaje',
				readinessAria: 'Spremnost valova',
				readinessKicker: 'Spremnost usporedbe',
				availableTitle: 'Što je dostupno?',
				sourceAria: 'Kontekst izvora valova',
				basedOn: 'Temeljeno na',
				launchPolicy: 'Kontekst pokretanja i pravila',
				prerequisitesAria: 'Nedostajući uvjeti valova',
				prerequisitesKicker: 'Blokirana usporedba',
				prerequisitesTitle: 'Što treba pažnju?',
				availableWavesAria: 'Dostupni valovi',
				availableWavesKicker: 'Dostupni valovi',
				waveHistory: 'Povijest valova'
			},
			fallback: {
				selectedSeriesContext: 'kontekst odabrane studije',
				productWorkflow: 'Tijek rada proizvoda',
				previewWorkflow: 'Pregled tijeka rada',
				governance: 'Upravljanje',
				selectedSeriesReadiness: 'Spremnost odabrane studije',
				campaignRows: 'Redci valova',
				campaignRowsAria: 'Redci valova odabrane studije',
				campaignContext: 'Kontekst valova odabrane studije',
				readOnlyAccess: 'Pristup samo za čitanje',
				workflowRequiresSetup: 'Radnje tijeka rada zahtijevaju pristup za upravljanje postavljanjem.'
			}
		}
	}
};

const routePageCopies = {
	en,
	'hr-HR': hr
} satisfies LocaleDictionary<typeof en>;

export type RoutePageCopy = typeof en;

export function routePageCopy(locale: AppLocale): RoutePageCopy {
	return routePageCopies[locale];
}

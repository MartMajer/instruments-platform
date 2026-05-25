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
		privateBeta: 'Access note'
	},
	publicEntry: {
		metaTitle: 'Validated Scale | Research and wellbeing study platform',
		metaDescription:'Study operations for questionnaires, collection, results, repeat-participation waves, and analysis-ready exports in one workspace.',
		brandSubtitle: 'Research studies and wellbeing programs',
		navAria: 'Product entry actions',
		mobileNavAria: 'Mobile product entry actions',
		languageSwitchAria: 'Language',
		menu: 'Menu',
		openMenu: 'Open menu',
		closeMenu: 'Close menu',
		workflow: 'Workflow',
		trustModel: 'Study controls',
		heroKicker: 'Study operations in one workspace',
		heroTitle: 'Run studies, response collection, and results without rebuilding the data stack.',
		heroBody:'Build questionnaires, choose the response mode, track collection, compare waves, and export datasets with codebooks from one workspace instead of stitching forms, spreadsheets, scripts, and screenshots together.',
		previewAria: 'Product preview',
		previewChrome: 'study overview',
		governanceBody: 'Consent, retention, finality, and provenance',
		studyContextPreserved: 'Study context preserved',
		governanceLabel: 'Governance',
		responseReceiptBody: 'Completion state without exposing identity',
		responseReceipt: 'Response receipt',
		respondentLabel: 'Respondent',
		exportState: 'Export state',
		preparationState: 'Preparation state',
		responseTrendChart: 'Response trend chart',
		showcaseExports: 'Exports',
		showcaseResults: 'Results',
		showcaseCollect: 'Collect',
		showcaseStudies: 'Studies',
		previewCollect: 'Collect',
		previewResults: 'Results',
		selectedStudy: 'Selected study',
		previewStudyName: 'Workload and recovery pulse',
		liveCollection: 'Live collection',
		responseSignal: 'Collection signal',
		responseProgress: '412 responses · 33% complete',
		prepare: 'Prepare',
		launchChecklist: 'Launch checklist',
		prepareBody: 'Questionnaire, result outputs, audience, consent, and pre-launch checks stay visible before launch.',
		export: 'Export',
		datasetCodebook: 'Dataset + codebook',
		exportBody: 'Exports carry dataset shape, codebook metadata, finality, and source context together.',
		trustAria: 'Study controls',
		workflowRibbon: 'Questionnaires, collection, results, waves, and exports',
		access: 'Study access',
		accessRibbon: 'Anonymous, repeated-wave, invite-only, and identified collection paths',
		dataControls: 'Governance',
		dataControlsRibbon: 'Consent, retention, finality, disclosure, and export provenance',
		productStage: 'Analysis handoff',
		productStageRibbon: 'Keep datasets, data descriptions, and report context ready for analysis.',
		workspaceOverview: 'Study overview',
		suiteTitle: 'See what each study needs next.',
		suiteBody:'Preparation, live collection, result review, wave comparison, and export readiness stay visible in one place.',
		nextAction: 'Next action',
		nextActionQuestion: 'What needs attention now?',
		appAreas: 'Work areas',
		studyStatus: 'Study status',
		workflowTitle: 'From questionnaire to analysis-ready results.',
		portfolio: 'Portfolio',
		portfolioBody: 'Start a new study or return to active study programs, including repeated-wave programs.',
		prepareStepBody: 'Define questionnaire, result outputs, policies, recipients, and pre-launch checks.',
		collectStepBody: 'Share links or send invitations, track response progress, and monitor delivery.',
		reviewStepBody: 'Review results, compare waves, and export datasets with codebooks.'
	},
	signIn: {
		metaTitle: 'Sign in | Validated Scale',
		metaDescription:
			'Find your Validated Scale workspace and continue to the app.',
		brandSubtitle: 'Workspace sign-in',
		navAria: 'Sign-in actions',
		eyebrow: 'Workspace access',
		title: 'Sign in to your workspace.',
		body:
			'Enter the email used for the workspace. We find the right workspace first, then open the account step for password and MFA.',
		stepsAria: 'Sign-in steps',
		stepFind: 'Find workspace',
		stepFindBody: 'Use the same email that owns or belongs to the workspace.',
		stepSignIn: 'Sign in',
		stepSignInBody: 'Complete password, account selection, and MFA in the account step.',
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
		betaBoundaryBody:
			'Before inviting real participants, confirm the study approval path, participant notices, and launch checklist.',
		workspaceNotFound: 'No workspace exists for this email yet. Create a workspace first.',
		emailInvalid: 'Enter the email used for the workspace.',
		fallbackError: 'We could not find a workspace for this email.'
	},
	register: {
		metaTitle: 'Create workspace | Validated Scale',
		metaDescription:
			'Create a Validated Scale workspace for research or wellbeing studies.',
		brandSubtitle: 'Study workspace',
		navAria: 'Registration actions',
		eyebrow: 'Workspace access',
		title: 'Create your workspace.',
		body:
			'Use the email that should manage the workspace. The account step handles password and MFA; this page names the workspace and checks your access code.',
		stepsAria: 'Registration steps',
		stepCreate: 'Create account',
		stepCreateBody: 'Enter the email, workspace name, and access code before opening account setup.',
		stepVerify: 'Verify email',
		stepVerifyBody: 'If email verification is requested, confirm the email and continue here.',
		stepOpen: 'Open the app',
		stepOpenBody: 'The workspace is created after approved registration and opens with your session.',
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
			'Open the verification email, then retry account setup with the same email.',
		retryRegistration: 'Retry registration sign-in',
		startOver: 'Start over',
		workspaceName: 'Workspace name',
		workspacePlaceholder: 'Your lab, team, or company',
		betaAccessCode: 'Access code',
		accessCodePlaceholder: 'Access code',
		openingAccount: 'Opening account setup.',
		openingAccountButton: 'Opening account setup...',
		createAccount: 'Create account',
		useDifferentEmail: 'Use a different email',
		accountReady: (email: string) => `Account ready: ${email}. This email will manage the workspace.`,
		workspaceCreated: 'Workspace created. Opening your app.',
		creatingWorkspace: 'Creating workspace...',
		boundaryTitle: 'Access note',
		boundaryBody:
			'Before inviting real participants, confirm your approvals, participant-facing notices, and launch settings for the workspace.',
		sessionError:
			'We could not confirm the account step. Continue with account setup again before creating the workspace.',
		disabled:
			'Workspace creation is not open on this environment. Sign in if you already have a workspace.',
		expired: 'Your account step expired. Continue with account setup again, then create the workspace.',
		forbidden:
			'This account cannot create a workspace. Sign out and use an approved account, or ask for access.',
		conflict: 'This account or workspace is already set up. Sign in instead.',
		invalidCode: 'That access code is not accepted.',
		emailExists: 'A workspace already exists for this email. Sign in instead.',
		organizationInvalid: 'Enter the workspace or organization name you want to use.',
		fallbackError: 'We could not create the workspace. Check the access code and try again.'
	},
	workspaceHome: {
		eyebrow: 'Workspace',
		title: 'Home',
		description:
			'Start real study work, explore read-only examples, and continue active studies from one place.',
		loading: 'Loading workspace overview',
		errorTitle: 'Workspace overview unavailable',
		retry: 'Retry overview',
		homeAria: 'Workspace home',
		heroAria: 'Workspace onboarding',
		heroKicker: 'Research workspace',
		heroTitle: 'Build the study, collect responses, review results, and export evidence.',
		heroBody:
			'Use this home screen as the shortest path into the product: start a real study, learn from finished sample studies, or return to work already in progress.',
		primaryActionsAria: 'Primary workspace actions',
		createStudy: 'Create study',
		exploreSamples: 'Explore samples',
		workflowAria: 'Study workflow',
		workflowLabel: 'How the app works',
		start: 'Start',
		startAria: 'Start here',
		startTitle: 'Start with one concrete action.',
		startBody:
			'Most teams begin by creating a study, adding people, then launching collection after setup is ready.',
		setupWorkspace: 'Set up your workspace',
		chooseNext: 'Choose what to do next',
		firstRunActions: [
			'Create first study',
			'Invite team',
			'Set up directory'
		],
		firstRunActionStatuses: ['Start here', 'Access', 'People'],
		firstRunActionDescriptions: [
			'Start a real study and continue through setup, collection, and results.',
			'Prepare tenant member access before sharing the first sign-in link.',
			'Create people, groups, memberships, and manager links for targeting.'
		],
		nextActions: 'Next actions',
		nextActionsTitle: 'Continue the work that needs attention.',
		noWorkspaceActions: 'No urgent workspace actions',
		noWorkspaceActionsBody: 'Open Studies to create or continue study work.',
		openStudies: 'Open Studies',
		examples: 'Examples',
		sampleStudies: 'Sample studies',
		sampleStudiesBody:
			'Finished synthetic studies show the product flow without changing your workspace data.',
		sampleWorkloadTitle: 'Workload and recovery pulse',
		sampleWorkloadKicker: 'Two-wave workplace study',
		sampleWorkloadBody:
			'Explore a closed repeated-wave pulse with setup complete, responses collected, results ready, and comparison context.',
		sampleWorkloadMeta: 'Anonymous with repeated participation, two waves, mock report data',
		sampleErgonomicsTitle: 'Ergonomics risk review',
		sampleErgonomicsKicker: 'OSH review study',
		sampleErgonomicsBody:
			'Review a finished occupational-health style study with department-level aggregate results and export context.',
		sampleErgonomicsMeta: 'Closed collection, visible aggregate scores, mock exports',
		sampleStudentTitle: 'Student wellbeing and study load',
		sampleStudentKicker: 'Academic study example',
		sampleStudentBody:
			'See how a student wellbeing study can move from questionnaire setup to response review and wave comparison.',
		sampleStudentMeta: 'Repeated measurement, linked comparison, synthetic responses',
		openSample: 'Open read-only sample',
		sampleReadOnlyNote:
			'Sample studies are read-only examples. They do not create or change real workspace studies.',
		yourWork: 'Your work',
		yourStudies: 'Your studies',
		openPortfolio: 'Open Studies',
		noStudiesYet: 'No real studies yet',
		noStudiesYetBody: 'Your editable studies appear here after you create one.',
		workspaceOverview: 'Workspace overview',
		sampleDemo: {
			eyebrow: 'Read-only samples',
			title: 'Explore finished sample studies.',
			description:
				'See complete synthetic studies with setup, collection, results, and export context before creating your own.',
			status: 'Sample data',
			aria: 'Read-only sample study library',
			backToHome: 'Back to Home',
			synthetic: 'Synthetic data',
			readOnlyTitle: 'Read-only example',
			readOnlyBody:
				'This sample is for orientation only. It does not create participants, send invitations, or change workspace studies.',
			chooseSample: 'Choose a sample study',
			chooseSampleBody:
				'Switch between realistic study types to see how different research contexts move through the same workflow.',
			snapshot: 'Study snapshot',
			inspect: 'What you can inspect',
			questionnaire: 'Questionnaire excerpt',
			findings: 'Example result notes',
			files: 'Files shown',
			responseMetric: 'Responses',
			measurementMetric: 'Measurements',
			resultMetric: 'Result packet',
			exportMetric: 'Export',
			ready: 'Ready',
			closed: 'Closed',
			available: 'Available',
			workloadMetrics: [
				'412 submitted responses across selected teams.',
				'Two closed measurements prepared for change-over-time review.',
				'Coverage, limitations, and comparison notes are ready.',
				'Response dataset and data description are available.'
			],
			workloadChecks: [
				'Questionnaire and result outputs are complete.',
				'Collection is closed before the results packet is reviewed.',
				'Repeated participation supports linked change-over-time interpretation.',
				'Export context explains what each file can be used for.'
			],
			workloadQuestions: [
				'How manageable was your workload during the last two weeks?',
				'How often could you recover enough between demanding work periods?',
				'How confident are you that current work demands can be sustained?'
			],
			workloadFindings: [
				'Recovery capacity improved after schedule and workload planning changes.',
				'Workload strain remains elevated in one operational group.',
				'Interpretation notes separate observed signal from unsupported claims.'
			],
			workloadFiles: [
				'Response dataset with data description.',
				'Results summary export for review packets.',
				'Comparison notes for the two closed measurements.'
			],
			ergonomicsMetrics: [
				'128 submitted responses from the selected workplace groups.',
				'One closed measurement prepared for department-level review.',
				'Aggregate results are visible where disclosure rules allow.',
				'CSV export and review notes are available.'
			],
			ergonomicsChecks: [
				'Workplace groups are visible without exposing individual responses.',
				'Questionnaire sections separate physical load, workstation fit, and recovery.',
				'Action notes focus on follow-up review, not medical diagnosis.',
				'Export state is final because collection is closed.'
			],
			ergonomicsQuestions: [
				'How often did your workstation posture feel difficult to sustain?',
				'How much control did you have over breaks or movement during the day?',
				'How often did task layout or equipment make work harder than necessary?'
			],
			ergonomicsFindings: [
				'One role group shows higher workstation-fit concern than the workplace average.',
				'Break-control responses suggest a practical intervention area.',
				'Small groups stay hidden under disclosure rules.'
			],
			ergonomicsFiles: [
				'Department aggregate review export.',
				'Response dataset with data description.',
				'Action-note summary for internal follow-up.'
			],
			studentMetrics: [
				'275 submitted responses across course cohorts.',
				'Two measurements support before-and-after review.',
				'Wellbeing and study-load summaries are prepared.',
				'Analysis export is available with context notes.'
			],
			studentChecks: [
				'Study-load and wellbeing sections stay separate in result outputs.',
				'Comparison notes flag where cohorts are comparable.',
				'Open-text style findings are summarized as review prompts.',
				'Files are marked as synthetic and read-only.'
			],
			studentQuestions: [
				'How manageable was your study load during the last week?',
				'How confident did you feel about completing required work on time?',
				'How often did study demands interfere with recovery or sleep?'
			],
			studentFindings: [
				'Study-load pressure improved after assessment scheduling changes.',
				'Recovery remains weaker in one cohort and needs follow-up review.',
				'Result notes avoid individual-level claims.'
			],
			studentFiles: [
				'Cohort trend export with data description.',
				'Results summary for academic review.',
				'Comparison context for the two measurements.'
			]
		}
	},
	portfolio: {
		eyebrow: 'Study workspace',
		title: 'Studies',
		description: 'Create a study or open an existing one. Samples stay separated from real workspace studies.',
		loading: 'Loading studies',
		errorTitle: 'Studies unavailable',
		retry: 'Retry studies',
		guidedDesign: 'Guided study design',
		startBlueprint: 'Choose how to start the study',
		selectedStartingPoint: 'Selected starting point',
		studyModelTitle: 'Study, source, and questionnaire',
		studyModelBody:
			'This creates the study first. The starting point only provides source material for Setup; you still edit the questionnaire, result outputs, recipients, and waves before launch.',
		studyModelStudy: 'Study',
		studyModelStudyBody:
			'The durable study container for setup, collection waves, results, and export files.',
		studyModelStartingPoint: 'Starting point',
		studyModelStartingPointBody:
			'Provides source material for the first Setup draft. It is not the final questionnaire and not the study.',
		studyModelSetup: 'Setup',
		studyModelSetupBody:
			'Where you turn source material into the questionnaire, result outputs, recipient plan, and launch check.',
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
		visibility: 'Visibility',
		loadFailed: 'Studies could not be loaded.',
		enterStudyName: 'Enter a study name.',
		createFailed: 'Study could not be created.',
		renameFailed: 'Study could not be renamed.',
		restoreFailed: 'Study could not be restored.',
		archiveFailed: 'Study could not be archived.',
		duplicateSampleFailed: 'Sample study could not be duplicated.',
		saving: 'Saving...',
		saveName: 'Save name',
		duplicating: 'Duplicating...',
		renameStudyName: 'Rename study name',
		cancel: 'Cancel',
		rename: 'Rename'
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
		saveManager: 'Save manager',
		loadFailed: 'Subject directory could not be loaded.',
		enterPersonIdentity: 'Enter a display name, email, or external id.',
		personCreateFailed: 'Person could not be created.',
		csvRequired: 'Paste CSV rows or choose a CSV file first.',
		csvImportFailed: 'CSV audience import could not be completed.',
		selectPerson: 'Select a person.',
		personUpdateFailed: 'Person could not be updated.',
		enterGroupIdentity: 'Enter a group type and name.',
		groupCreateFailed: 'Subject group could not be created.',
		selectPersonAndGroup: 'Select a person and group.',
		membershipSaveFailed: 'Group membership could not be saved.',
		managerSaveFailed: 'Manager relationship could not be saved.',
		rowsNeedingAttention: 'Rows needing attention',
		subjects: 'Subjects',
		externalId: 'External id',
		locale: 'Locale',
		manager: 'Manager',
		directReports: 'Direct reports',
		noMemberships: 'No memberships',
		parent: 'Parent',
		members: 'Members',
		person: 'Person',
		newPerson: 'New person',
		displayName: 'Display name',
		attributesJson: 'Attributes JSON',
		newGroup: 'New group',
		parentGroup: 'Parent group',
		noParent: 'No parent',
		hierarchySetup: 'Hierarchy setup',
		validFrom: 'Valid from',
		selectedPerson: 'Selected person',
		noManager: 'No manager',
		managed: 'Managed',
		notAvailable: 'Not available',
		unmatchedRow: 'Unmatched row',
		previewed: 'Previewed',
		imported: 'Imported',
		loadingDirectory: 'Loading subject directory',
		unavailableTitle: 'Subject directory unavailable',
		retryDirectory: 'Retry directory',
		createRecordsAria: 'Create directory records',
		creating: 'Creating...',
		addPerson: 'Add person',
		createGroup: 'Create group'
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
		metaTitle: 'Unsubscribe from study invitations - Validated Scale',
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
			statusDescription: 'Review readiness, governance, and waves linked to this study.',
			dates: 'Dates',
			datesAria: 'Selected study dates',
			studyModel: 'Study model',
			lifecycle: 'Lifecycle',
			governance: 'Governance',
			governanceAria: 'Governance status',
			policyScoring: 'Policy and scoring status',
			campaigns: 'Waves',
			campaignsAria: 'Selected study wave rows',
			campaignsInStudy: 'Waves in this study',
			noCampaigns: 'No waves are linked to this study yet.',
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
					'Create follow-up collection waves, then compare linked repeat-participation change when the study is ready.',
				ariaLabel: 'Waves and linked repeat responses'
			}
		},
		setupBody: {
			progressAriaLabel: 'Study setup progress',
			progressKicker: 'Study setup',
			progressTitle: 'Study setup progress',
			progressBody:
				'Study is the project. Setup turns source material into the questionnaire, result outputs, Wave 1 recipients, and the launch check.',
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
				blueprintTitle: 'Questionnaire check',
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
				errorsLabel: 'Result output errors'
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
			locale: 'en' as AppLocale,
			stepNumber: (number: number) => `${number}`,
			defaultWaveName: (number: number) => `Wave ${number}`,
			steps: {
				instrument: {
					title: 'Questionnaire source',
					description:
						'Choose reusable or imported source material. It seeds the questionnaire; it is not the study and not the final questionnaire.'
				},
				template: {
					title: 'Questionnaire',
					description: 'Build the saved question set respondents will answer for this study.'
				},
				scoring: {
					title: 'Result outputs',
					description:
						'Choose which questionnaire answers become result outputs and how missing answers are handled.'
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
				confirmInstrument: 'Choose the questionnaire source first.',
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
				title: 'How this study is built',
				summary:
					'Study is the project container. Source material seeds the questionnaire; result outputs interpret answers; waves collect responses.',
				source: 'Questionnaire source',
				questionnaire: 'Questionnaire',
				results: 'Result outputs',
				waves: 'Collection waves',
				sourceReady: 'Source material is ready for this questionnaire.',
				sourceMissing: 'Choose reusable or imported source material before saving the questionnaire.',
				questionnaireSaved: (name: string, questionCount: number) =>
					`${name} is saved with ${questionCount} ${questionCount === 1 ? 'question' : 'questions'}.`,
				questionnaireMissing: 'Save the questionnaire before results setup or launch checks.',
				resultsReady: (ruleKey: string) => `Result outputs are saved as ${ruleKey}.`,
				resultsMissing: 'Choose which questionnaire answers become result outputs.',
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
			},
			component: {
				doNotContactList: 'Do-not-contact list',
				suppressEmailsBeforeInviting: 'Suppress emails before inviting',
				emailToSuppress: 'Email to suppress',
				internalNote: 'Internal note',
				addToDoNotContact: 'Add to do-not-contact',
				refreshDoNotContactList: 'Refresh do-not-contact list',
				emailInvitationStatus: 'Email invitation status',
				queued: 'Queued',
				suppressed: 'Suppressed',
				sendAttempts: 'Send attempts',
				providerAccepted: 'Provider accepted',
				providerDelivered: 'Provider delivered',
				providerBounced: 'Provider bounced',
				complaints: 'Complaints',
				invitedEmailAccess: 'Invited email access',
				addOneOffRecipientsAfterLaunch: 'Add one-off recipients after launch',
				postLaunchAdditions: 'Post-launch additions',
				addOneTimeRecipientsToThisWave: 'Add one-time recipients to this wave',
				nameForReview: 'Name for review',
				addToReviewList: 'Add to review list',
				importRecipients: 'Import recipients',
				reviewOrPasteSourceList: 'Review or paste source list',
				recipientSource: 'Recipient source',
				importReview: 'Import review',
				invalid: 'Invalid',
				duplicates: 'Duplicates',
				keepValidOnly: 'Keep valid only',
				clearList: 'Clear list',
				doNotContactMatch: 'Do-not-contact match',
				invitationEmailPreview: 'Invitation email preview',
				subject: 'Subject',
				retrySafetyCheck: 'Retry safety check',
				createAdHocInvitations: 'Create ad hoc invitations',
				sendNextEmailBatch: 'Send next email batch',
				retryFailedEmails: 'Retry failed emails',
				invitationsCreated: 'Invitations created',
				emailDeliveryBatch: 'Email delivery batch',
				failedEmailsRequeued: 'Failed emails requeued',
				demoTestData: 'Demo/test data',
				simulateCollectionResponses: 'Simulate collection responses',
				shareLink: 'Share link',
				notLoaded: 'Not loaded',
				anonymousInviteOnly: 'Anonymous invite-only',
				unavailable: 'Unavailable',
				noEmailFound: 'No email found',
				responsesCreated: 'Responses created',
				stagingDemo: 'Staging/demo',
				errors: {
					createWaveBeforeReadiness:
						'Create a collection wave before running the pre-launch check.',
					createWaveBeforeStart: 'Create a collection wave before starting collection.',
					createWaveBeforeAccess: 'Create a collection wave before creating respondent access.',
					privateInvitationsAlreadyActive:
						'This wave already uses private email invitations. Open links are disabled so access stays invite-only.',
					openLinkAlreadyActive:
						'This wave already has an open respondent link. Keep using the link you created, or create a new wave if the link was lost.',
					createWaveBeforeReplaceLink:
						'Create a collection wave before replacing the open respondent link.',
					replaceLinkOnlyForAnonymous:
						'Open respondent link replacement is available only for anonymous open-link collection.',
					createWaveBeforeInvitations: 'Create a collection wave before preparing invitations.',
					emailInvitationsRequireAnonymous:
						'Email invitations are available for anonymous or repeat-participation waves.',
					openLinkBlocksPrivateInvitations:
						'This wave already has an open respondent link. Private email invitations are disabled for open-link collection.',
					reviewRecipientsBeforeInviting:
						'Review the recipient list first. Remove invalid or duplicate emails before creating invitations.',
					recipientFileCouldNotRead: 'Recipient file could not be read.',
					enterOneValidEmail: 'Enter one valid email address.',
					recipientAlreadyInWaveList: 'This recipient is already in the wave list.',
					createWaveBeforeSending: 'Create a collection wave before sending invitations.',
					checkEmailSetupBeforeSending:
						'Check email sending setup before sending invitation emails.',
					resolveEmailSetupBeforeSending:
						'Resolve email sending setup blockers before sending.',
					createWaveBeforeRetrying: 'Create a collection wave before retrying failed emails.',
					confirmRetryBeforeRequeueing:
						'Confirm another invitation email is appropriate before requeueing.',
					checkEmailSetupBeforeRetrying:
						'Check email sending setup before retrying failed invitation emails.',
					resolveEmailSetupBeforeRetrying:
						'Resolve email sending setup blockers before retrying failed invitations.',
					createAndStartBeforeSimulating:
						'Create and start a collection wave before simulating responses.',
					createWaveBeforeRepairReadiness:
						'Create a collection wave before checking email repair readiness.',
					enterEmailBeforeSuppressing:
						'Enter an email address before adding it to do-not-contact.',
					refreshDoNotContactBeforeRelease:
						'Refresh the do-not-contact list before releasing this recipient.',
					collectionStatusRefreshError: 'Collection status could not be refreshed.',
					collectionStatusRefreshFailed: 'Collection status refresh failed.',
					refreshWarning: 'The action was saved, but this collection view could not refresh.',
					collectionActionFailed: 'Collection action failed.'
				},
				emailStatus: {
					notChecked: 'Not checked',
					smtpReady: 'SMTP ready',
					smtpSendReady: 'SMTP send ready',
					localProofMode: 'Local proof mode',
					needsConfig: 'Needs config',
					noQueuedEmails: 'No queued invitation emails are waiting to send.',
					noRetryableFailedEmails: 'No retryable failed invitation emails are waiting.'
				},
				deliveryGuidance: {
					sesSandbox:
						'AWS SES rejected at least one recipient because the account is still in sandbox. Verify the lowercase recipient email in the same SES region, or wait for SES production access, then use Retry failed emails.',
					sesSenderRejected:
						'AWS SES rejected the sender identity. Check the verified sender domain/from address in SES, then use Retry failed emails.',
					sesIdentityRejected:
						'AWS SES rejected a verified-identity check. Confirm sender and sandbox recipient identities in the configured SES region, then retry failed emails.',
					smtpAuth:
						'The SMTP provider rejected authentication. Check the SES SMTP username/password on the server, then retry failed emails.',
					smtpTls:
						'The SMTP TLS handshake failed. Check provider host, port, and TLS settings before retrying failed emails.',
					throttled:
						'AWS SES throttled this batch. Wait for the provider limit window to clear, then retry failed emails.',
					suppressedRecipient:
						'At least one recipient is on the workspace do-not-contact list. Review suppressions before retrying.',
					providerRejected:
						'The provider rejected at least one invitation. Check email setup and provider status, then use Retry failed emails when another send is appropriate.',
					sentAccepted:
						'Sent means the message was accepted by the SMTP handoff. Delivery, bounce, and complaint evidence appears later under Provider delivery evidence.',
					failedRetry:
						'Failed invitations can be retried after the provider issue is corrected. Use Retry failed emails in the respondent access step.',
					suppressedFailures:
						'Some failed invitations are suppressed by do-not-contact or provider feedback. Review the suppression list before sending again.',
					ambiguous:
						'Some handoffs are ambiguous. Treat them as possibly sent and retry only after checking provider evidence.',
					providerEvents:
						'Provider events have reconciled for this campaign. Load recent provider events to inspect accepted, delivered, bounced, or complained counts.',
					noCleanup: 'No email delivery cleanup is currently needed for this wave.'
				},
				readinessGuidance: {
					useDraftWaveTitle: 'Use a draft collection wave',
					useDraftWaveDetail:
						'This wave is no longer draft or scheduled. Open Setup, select or create a draft collection wave, then run this check again.',
					chooseResponseModeTitle: 'Choose the response mode',
					chooseResponseModeDetail:
						'Open Setup and save the Collection setup step with a valid response mode before starting collection.',
					connectQuestionnaireTitle: 'Connect the questionnaire to this wave',
					connectQuestionnaireDetail:
						'Open Setup, save the Questionnaire step, then save the Collection setup step so the wave uses that questionnaire.',
					finishQuestionnaireTitle: 'Finish the questionnaire',
					finishQuestionnaireDetail:
						'Open Setup and add at least one questionnaire section and question before starting collection.',
					finishResultsTitle: 'Finish result outputs',
					finishResultsDetail:
						'Open Setup and save result outputs so reports know which answers become scores.',
					completePoliciesTitle: 'Complete study policies',
					completePoliciesDetail:
						'Open Setup and save the consent, retention, and disclosure policies for this study before launch.',
					switchResponseModeTitle: 'Switch response mode',
					switchResponseModeDetail:
						'Saved specific-email lists are available for anonymous or repeat-participation waves. Open Setup and change the response mode, or remove the saved recipient list.',
					addEmailsTitle: 'Add recipient email addresses',
					addEmailsDetail:
						'Open Directory and add email addresses for everyone in the saved recipient selection, then rerun the pre-launch check.',
					selectRecipientTitle: 'Select at least one recipient',
					selectRecipientDetail:
						'Open Setup and save at least one recipient selection that resolves to active people.',
					emptyRecipientTitle: 'Recipient selection is empty',
					emptyRecipientDetail:
						'Add active people to the selected group in Setup, or remove the saved recipient selection if this wave should use a general respondent link.',
					fixAudienceTitle: 'Fix who can answer',
					fixAudienceDetail:
						'Open Setup and adjust the recipient selection until the preview finds the people you expect.',
					reviewInstrumentTitle: 'Review the instrument',
					reviewInstrumentDetail:
						'Open Setup and save the Instrument and Questionnaire steps again so this wave uses a launchable study instrument.',
					reviewSetupTitle: 'Review setup'
				}
			}
		},		operationsWorkflow: {
			locale: 'en' as AppLocale,
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
				createWaveFirstGuidance: 'Collection starts after Setup has a collection wave.',
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
			locale: "en",
			surface: {
				reviewActionsAria: "Review and export actions",
				flowKicker: "Study flow · Results",
				title: "Review and export results",
				description: "Review aggregate results, check whether they are ready to share, and create export files when ready.",
				useDecisionLabel: "Use decision",
				resultsUseReviewAria: "Results use review",
				nextActionLabel: "Next action",
				scoreMethodLabel: "Score method",
				scoreMethodReviewAria: "Score method review",
				exportPreviewLabel: "Export preview",
				exportPreviewAria: "Export preview"
			},

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
						'Use raw response export for internal analysis, or review result-output scoring, missing-answer rules, and disclosure.',
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
					'Review file purpose, row shape, wave fields, repeat-response keys, variables, missingness, and score outputs before downloading.',
				createOrSelectWaveFirst: 'Create or select a wave first',
				reviewExportFileFirst: 'Review export file first',
				selectWavePendingDetail: 'Select a wave before preparing export files.',
				reviewFilePendingDetail: 'Review the export file to inspect its CSV and codebook contents.',
				downloadResponseDatasetCsv: 'Download response dataset CSV',
				downloadReportSummaryCsv: 'Download report-summary CSV'
			},
			component: {
				state: {
					working: 'Working',
					saved: 'Saved',
					failed: 'Failed',
					ready: 'Ready',
					done: 'Done',
					current: 'Current',
					blocked: 'Blocked'
				},
				errors: {
					refreshFailed: 'Reports action saved, but the reports workspace refresh failed.',
					actionFailed: 'Reports action failed.',
					createWaveBeforeResults: 'Create or select a wave before reviewing results.',
					createWaveBeforeReportExport: 'Create or select a wave before creating a report export.',
					createStudyBeforeResponseExport:
						'Create or select a study before creating a response export.',
					createExportBeforeReview: 'Create or select an export file before reviewing it.',
					selectDownloadableExport:
						'Select a downloadable export file before downloading CSV.',
					createExportBeforeDownload: 'Create or select an export file before downloading CSV.'
				},
				currentPurpose: {
					responseDataset: 'Response dataset CSV and codebook',
					reportSummary: 'Report-summary CSV, not analysis-ready response dataset'
				},
				downloadAction: 'Download action',
				reviewPathAria: 'Review and export path',
				readOnlyTitle: 'Read-only access',
				readOnlyBody: 'Review and export actions require workspace management access.',
				currentTaskAria: 'Current review task',
				taskProgress: (completed: number, total: number) =>
					`${completed} of ${total} results tasks done`,
				currentTaskTitle: 'Current results task',
				selectedWave: 'Selected wave',
				previewStatus: 'Preview status',
				readyForReview: 'Ready for review',
				finishSetupFirst: 'Finish setup first',
				interpretation: 'Interpretation',
				missing: 'Missing',
				notAvailable: 'Not available',
				suppressed: 'suppressed',
				latestExport: 'Latest export',
				exportCount: 'Export count',
				reportExportResult: 'Report export result',
				reportExport: 'Report export',
				reportSummaryCsvCodebook: 'Report-summary CSV and codebook',
				createReportSummaryExport: 'Create report-summary export',
				exportFile: 'Export file',
				series: 'Series',
				latestResponseExport: 'Latest response export',
				responseExportResult: 'Response export result',
				responseExport: 'Response export',
				responseCsvCodebook: 'Response CSV and codebook',
				createResponseExport: 'Create response export',
				responseFile: 'Response file',
				downloadStatus: 'Download status',
				downloadable: 'Downloadable',
				notReady: 'Not ready',
				latestFile: 'Latest file',
				filePurpose: 'File purpose',
				reviewedFile: 'Reviewed file',
				downloadedFile: 'Downloaded file',
				reportPreviewAria: 'Report preview',
				resultsPreview: 'Results preview',
				aggregateResultPreview: 'Aggregate result preview',
				internalPreview: 'Internal preview',
				responsesSuffix: 'responses',
				minimumGroup: (kMin: number) => `Minimum group ${kMin}`,
				reportPreviewScoresAria: 'Report preview scores',
				reportScoreAria: (dimensionCode: string) => `Report score ${dimensionCode}`,
				scoreCount: (count: number | string) => `scores=${count}`,
				exportPreparing: 'Preparing',
				rowsLabel: 'Rows',
				rows: (count: number) => `${count} rows`,
				file: 'File',
				downloadedCsv: 'Downloaded CSV',
				bytes: (count: number) => `${count} bytes`,
				goToWaves: 'Go to waves',
				reviewed: 'reviewed',
				notReviewed: 'not reviewed',
				official: 'official',
				notOfficial: 'not official'
			},

		},		wavesWorkflow: {
			locale: 'en' as AppLocale,
			stepNumber: (number: number) => `Step ${number}`,
			surface: {
				reviewActionsAria: 'Wave comparison workflow',
				flowKicker: 'Study flow · Waves',
				title: 'Repeat the study and compare waves',
				description:
					'Create follow-up waves from Setup, collect responses from Collection, then compare closed waves here.',
				scoreMethodLabel: 'Score method',
				scoreMethodReviewAria: 'Wave score method review'
			},
			plan: {
				createFirstTitle: 'Create the first wave',
				createFirstDescription: 'Start by creating Wave 1 as the first collection round for this study.',
				openSetupLabel: 'Open setup',
				createFirstGuidance: [
					'Each wave is a collection round inside this study. Create Wave 1 in Setup, then launch it from Collection.',
					'After responses arrive, review the wave in Results before adding a follow-up wave.',
					'Use repeat participation from the first wave if you need linked change-over-time comparison later.'
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
					'Use repeat participation when the same respondent should be linked across waves for change-over-time comparison.',
					'Review recipients before launching the new wave; do not assume the recipient list is unchanged unless Collection shows it.'
				],
				checkReadinessTitle: 'Check comparison readiness',
				checkReadinessDescription:
					'Two repeat-participation waves exist. Now confirm linked repeat responses and scoring compatibility.',
				runChecksBelowLabel: 'Run checks below',
				reviewResultsLabel: 'Review results',
				checkReadinessGuidance: [
					'Use the checks below to confirm both waves can be linked safely.',
					'Results remain wave-by-wave until linked repeat responses, disclosure, and scoring compatibility are ready.',
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
			},
			component: {
				state: {
					working: 'Working',
					viewed: 'Viewed',
					failed: 'Failed',
					ready: 'Ready',
					done: 'Done',
					current: 'Current',
					blocked: 'Blocked'
				},
				errors: {
					refreshFailed: 'Waves action completed, but the waves workspace refresh failed.',
					actionFailed: 'Waves action failed.'
				},
				wavePlanAria: 'Wave plan',
				whereWavesFit: 'Where waves fit',
				waveComparisonPlanAria: 'Wave comparison plan',
				comparisonPlan: 'Comparison plan',
				groupTrendAria: 'Group trend review',
				groupTrend: 'Group trend',
				firstWave: 'First wave',
				firstWaveResponses: 'First wave responses',
				secondWave: 'Second wave',
				secondWaveResponses: 'Second wave responses',
				missing: 'Missing',
				wavesPathAria: 'Waves path',
				currentTaskAria: 'Current waves task',
				taskProgress: (completed: number, total: number) =>
					`${completed} of ${total} comparison tasks done`,
				currentTaskTitle: 'Current comparison task',
				selectedSeries: 'Selected series',
				repeatedWaves: 'Repeated waves',
				potentialCompleteTrajectories: 'Potential complete repeat-response pairs',
				runLinkedTrajectoryCheck: 'Check linked repeat responses',
				study: 'Study',
				baseline: 'Baseline',
				comparison: 'Comparison',
				compatibility: 'Compatibility',
				disclosure: 'Disclosure',
				minimumGroupSize: 'Minimum group size',
				notConfigured: 'Not configured',
				suppressedComparisons: 'Suppressed comparisons',
				reviewComparison: 'Review comparison',
				reviewed: 'Reviewed',
				linkedChangeTaskStatusAria: 'Linked change task status',
				linkedChangeWorkflow: 'Linked-change workflow',
				linkedChecksNotNeeded: 'Linked-change checks not needed',
				linkedChecksNotActiveYet: 'Linked-change checks not active yet',
				linkedTrajectoryCheckAria: 'Linked repeat response check',
				waveReadiness: 'Wave readiness',
				linkedTrajectoryCheck: 'Linked repeat response check',
				launchedWaves: (count: number) => `${count} launched waves`,
				wavesWithResponses: (count: number) => `${count} waves with responses`,
				linkedTrajectories: (count: number) => `${count} linked repeat responses`,
				completeTrajectories: (count: number) => `${count} complete repeat-response pairs`,
				waveAria: (name: string) => `Wave ${name}`,
				responseMode: 'Response mode',
				submittedResponses: 'Submitted responses',
				waveComparisonPreviewAria: 'Wave comparison preview',
				waveComparison: 'Wave comparison',
				disclosureGatedComparison: 'Disclosure-gated comparison',
				disclosureK: (kMin: number) => `Disclosure k=${kMin}`,
				waveComparisonScoresAria: 'Wave comparison scores',
				waveComparisonScoreAria: (dimensionCode: string) =>
					`Wave comparison ${dimensionCode}`,
				pairedDelta: (value: string) => `paired delta ${value}`,
				baselineMeta: (value: string) => `baseline ${value}`,
				comparisonMeta: (value: string) => `comparison ${value}`,
				baselineBand: (label: string) => `baseline band ${label}`,
				comparisonBand: (label: string) => `comparison band ${label}`,
				suppressed: 'suppressed',
				notAvailable: 'Not available',
				backToResults: 'Back to results',
				setUpNextWave: 'Set up next wave',
				reviewedInterpretation: 'reviewed',
				notReviewedInterpretation: 'not reviewed',
				officialInterpretation: 'official',
				notOfficialInterpretation: 'not official'
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
					'Run the linked repeat response check before loading the wave comparison snapshot.'
			},
			dashboard: {
				unavailableTitle: 'Wave dashboard unavailable',
				unavailableMessage: 'Select two comparable waves before reviewing the wave dashboard.',
				title: (baselineName: string, comparisonName: string) =>
					`${baselineName} vs ${comparisonName} wave dashboard`,
				campaigns: 'Campaigns',
				longitudinalWaves: 'Repeat-participation waves',
				submittedWaves: 'Submitted waves',
				missingPrerequisites: 'Missing prerequisites',
				baselineWave: 'Baseline wave',
				baselineStatus: 'Baseline status',
				baselineSubmittedResponses: 'Baseline submitted responses',
				comparisonWave: 'Comparison wave',
				comparisonStatus: 'Comparison status',
				comparisonSubmittedResponses: 'Comparison submitted responses',
				linkedTrajectories: 'Linked repeat responses',
				completeTrajectories: 'Complete repeat-response pairs',
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
				study: 'Study',
				aggregateSnapshotAria: 'Aggregate wave comparison snapshot',
				changeOverTimeTitle: 'Change over time',
				comparisonReady: 'Comparison ready',
				completeTrajectories: (count: number | string) => `complete repeat-response pairs ${count}`,
				linkedPairs: (count: number | string) => `linked pairs ${count}`,
				waveComparisonRowsAria: 'Wave comparison rows',
				waveComparisonScoreAria: (dimensionCode: string) =>
					`Wave comparison ${dimensionCode}`,
				baselineMean: (value: string) => `baseline mean ${value}`,
				comparisonMean: (value: string) => `comparison mean ${value}`,
				baselineMeta: (value: string) => `baseline ${value}`,
				comparisonMeta: (value: string) => `comparison ${value}`,
				aggregateDelta: (value: string) => `aggregate delta ${value}`,
				pairedDelta: (value: string) => `paired delta ${value}`,
				baselineBand: (value: string) => `baseline band ${value}`,
				comparisonBand: (value: string) => `comparison band ${value}`
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
				campaignRows: 'Wave rows',
				campaignRowsAria: 'Selected study wave rows',
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
		privateBeta: 'Napomena o pristupu'
	},
	publicEntry: {
		metaTitle: 'Validated Scale | Platforma za istraživanja i wellbeing programe',
		metaDescription:'Platforma za upitnike, prikupljanje odgovora, rezultate, ponovljena mjerenja i izvoz podataka s opisom podataka.',
		brandSubtitle: 'Istraživanja i wellbeing programi',
		navAria: 'Radnje ulaza u proizvod',
		mobileNavAria: 'Mobilne radnje ulaza u proizvod',
		languageSwitchAria: 'Jezik',
		menu: 'Izbornik',
		openMenu: 'Otvori izbornik',
		closeMenu: 'Zatvori izbornik',
		workflow: 'Kako radi',
		trustModel: 'Sigurnost i kontrola',
		heroKicker: 'Platforma za istraživanja i wellbeing programe',
		heroTitle: 'Vodite studije, prikupljanje odgovora i rezultate bez improviziranih tablica.',
		heroBody:'Složite upitnik, odaberite način prikupljanja, pratite odgovore, usporedite mjerenja i izvezite podatke s opisom podataka iz jednog radnog prostora.',
		previewAria: 'Pregled proizvoda',
		previewChrome: 'pregled studije',
		governanceBody: 'Privola, zadržavanje podataka, konačnost i trag izvoza',
		studyContextPreserved: 'Kontekst studije je sačuvan',
		governanceLabel: 'Kontrola',
		responseReceiptBody: 'Status završetka bez otkrivanja identiteta',
		responseReceipt: 'Potvrda odgovora',
		respondentLabel: 'Sudionik',
		exportState: 'Stanje izvoza',
		preparationState: 'Stanje pripreme',
		responseTrendChart: 'Graf trenda odgovora',
		showcaseExports: 'Izvoz',
		showcaseResults: 'Rezultati',
		showcaseCollect: 'Prikupljanje',
		showcaseStudies: 'Studije',
		previewCollect: 'Prikupljanje',
		previewResults: 'Rezultati',
		selectedStudy: 'Studija',
		previewStudyName: 'Opterećenje i oporavak',
		liveCollection: 'Prikupljanje u tijeku',
		responseSignal: 'Odaziv',
		responseProgress: '412 odgovora · 33% dovršeno',
		prepare: 'Priprema',
		launchChecklist: 'Provjera prije pokretanja',
		prepareBody: 'Upitnik, izlazi rezultata, sudionici, privola i provjera prije pokretanja ostaju vidljivi na jednom mjestu.',
		export: 'Izvoz',
		datasetCodebook: 'Podaci + opis podataka',
		exportBody: 'Izvoz uključuje podatke, opis podataka, status konačnosti i kontekst studije.',
		trustAria: 'Kontrole studije',
		workflowRibbon: 'Upitnik, prikupljanje, rezultati, mjerenja i izvoz',
		access: 'Pristup studiji',
		accessRibbon: 'Anonimni, ponovljeni, pozivni i identificirani način prikupljanja',
		dataControls: 'Kontrola podataka',
		dataControlsRibbon: 'Privola, zadržavanje podataka, konačnost, pragovi prikaza i trag izvoza',
		productStage: 'Predaja za analizu',
		productStageRibbon:'Podaci, opis podataka i kontekst izvještaja ostaju spremni za analizu.',
		workspaceOverview: 'Pregled studije',
		suiteTitle: 'Vidite što je sljedeće u svakoj studiji.',
		suiteBody:'Priprema, aktivno prikupljanje, pregled rezultata, usporedba mjerenja i izvoz nalaze se na jednom mjestu.',
		nextAction: 'Sljedeći korak',
		nextActionQuestion: 'Što prvo treba riješiti?',
		appAreas: 'Područja rada',
		studyStatus: 'Status studije',
		workflowTitle: 'Od upitnika do rezultata spremnih za analizu.',
		portfolio: 'Studije',
		portfolioBody: 'Pokrenite novu studiju ili nastavite rad na postojećoj, uključujući ponovljena mjerenja.',
		prepareStepBody: 'Definirajte upitnik, izlaze rezultata, pravila, sudionike i provjeru prije pokretanja.',
		collectStepBody: 'Podijelite poveznicu ili pošaljite pozive, pratite odgovore i status dostave.',
		reviewStepBody: 'Pregledajte rezultate, usporedite mjerenja i izvezite podatke s opisom podataka.'
	},
	signIn: {
		metaTitle: 'Prijava | Validated Scale',
		metaDescription: 'Pronađite radni prostor i nastavite u aplikaciju.',
		brandSubtitle: 'Prijava u radni prostor',
		navAria: 'Radnje prijave',
		eyebrow: 'Pristup radnom prostoru',
		title: 'Prijavite se u svoj radni prostor.',
		body:
			'Unesite e-poštu koja se koristi za radni prostor. Prvo pronalazimo pravi radni prostor, zatim otvaramo korak računa za lozinku i MFA.',
		stepsAria: 'Koraci prijave',
		stepFind: 'Pronađi radni prostor',
		stepFindBody: 'Upotrijebite istu e-poštu koja je vlasnik ili član radnog prostora.',
		stepSignIn: 'Prijava',
		stepSignInBody: 'Dovršite lozinku, odabir računa i MFA u koraku računa.',
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
			'Prije pozivanja stvarnih sudionika potvrdite put odobrenja studije, obavijesti za sudionike i kontrolnu listu pokretanja.',
		workspaceNotFound: 'Za ovu e-poštu još ne postoji radni prostor. Prvo izradite radni prostor.',
		emailInvalid: 'Unesite e-poštu korištenu za radni prostor.',
		fallbackError: 'Nismo mogli pronaći radni prostor za ovu e-poštu.'
	},
	register: {
		metaTitle: 'Izradi radni prostor | Validated Scale',
		metaDescription:
			'Izradite radni prostor za istraživačke studije ili studije dobrobiti.',
		brandSubtitle: 'Radni prostor studije',
		navAria: 'Radnje registracije',
		eyebrow: 'Pristup radnom prostoru',
		title: 'Izradite svoj radni prostor.',
		body:
			'Upotrijebite e-poštu koja treba upravljati radnim prostorom. Korak računa obrađuje lozinku i MFA; ova stranica imenuje radni prostor i provjerava pristupni kod.',
		stepsAria: 'Koraci registracije',
		stepCreate: 'Izradi račun',
		stepCreateBody: 'Unesite e-poštu, naziv radnog prostora i pristupni kod prije otvaranja postavljanja računa.',
		stepVerify: 'Potvrdite e-poštu',
		stepVerifyBody: 'Ako se zatraži potvrda e-pošte, potvrdite e-poštu i nastavite ovdje.',
		stepOpen: 'Otvori aplikaciju',
		stepOpenBody: 'Radni prostor nastaje nakon odobrene registracije i otvara se s vašom sesijom.',
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
			'Otvorite poruku za potvrdu e-pošte, zatim ponovno pokušajte postavljanje računa istom e-poštom.',
		retryRegistration: 'Pokušaj registracijsku prijavu ponovno',
		startOver: 'Kreni ispočetka',
		workspaceName: 'Naziv radnog prostora',
		workspacePlaceholder: 'Vaš laboratorij, tim ili tvrtka',
		betaAccessCode: 'Pristupni kod',
		accessCodePlaceholder: 'Pristupni kod',
		openingAccount: 'Otvaram postavljanje računa.',
		openingAccountButton: 'Otvaram postavljanje računa...',
		createAccount: 'Izradi račun',
		useDifferentEmail: 'Upotrijebi drugu e-poštu',
		accountReady: (email: string) =>
			`Račun je spreman: ${email}. Ova e-pošta upravljat će radnim prostorom.`,
		workspaceCreated: 'Radni prostor izrađen. Otvaram aplikaciju.',
		creatingWorkspace: 'Izrada radnog prostora...',
		boundaryTitle: 'Napomena o pristupu',
		boundaryBody:
			'Prije pozivanja stvarnih sudionika potvrdite odobrenja, obavijesti za sudionike i postavke pokretanja za radni prostor.',
		sessionError:
			'Nismo mogli potvrditi korak računa. Ponovno nastavite s postavljanjem računa prije izrade radnog prostora.',
		disabled:
			'Izrada radnog prostora nije otvorena na ovom okruženju. Prijavite se ako već imate radni prostor.',
		expired:
			'Korak računa je istekao. Ponovno nastavite s postavljanjem računa, zatim izradite radni prostor.',
		forbidden:
			'Ovaj račun ne može izraditi radni prostor. Odjavite se i upotrijebite odobreni račun ili zatražite pristup.',
		conflict: 'Ovaj račun ili radni prostor već je postavljen. Prijavite se umjesto toga.',
		invalidCode: 'Taj pristupni kod nije prihvaćen.',
		emailExists: 'Radni prostor već postoji za ovu e-poštu. Prijavite se umjesto toga.',
		organizationInvalid: 'Unesite naziv radnog prostora ili organizacije koji želite koristiti.',
		fallbackError: 'Nismo mogli izraditi radni prostor. Provjerite pristupni kod i pokušajte ponovno.'
	},
	workspaceHome: {
		eyebrow: 'Radni prostor',
		title: 'Početna',
		description:
			'Pokrenite stvaran rad na studiji, istražite primjere i nastavite aktivne studije s jednog mjesta.',
		loading: 'Učitavanje pregleda radnog prostora',
		errorTitle: 'Pregled radnog prostora nije dostupan',
		retry: 'Pokušaj pregled ponovno',
		homeAria: 'Početna radnog prostora',
		heroAria: 'Uvod u radni prostor',
		heroKicker: 'Radni prostor za istraživanja',
		heroTitle: 'Izradite studiju, prikupite odgovore, pregledajte rezultate i pripremite dokaze.',
		heroBody:
			'Ova početna stranica vodi vas najkraćim putem kroz proizvod: pokrenite stvarnu studiju, učite iz dovršenih primjera ili nastavite rad koji je već u tijeku.',
		primaryActionsAria: 'Glavne radnje radnog prostora',
		createStudy: 'Izradi studiju',
		exploreSamples: 'Istraži primjere',
		workflowAria: 'Tijek rada studije',
		workflowLabel: 'Kako aplikacija radi',
		start: 'Početak',
		startAria: 'Počnite ovdje',
		startTitle: 'Krenite s jednom konkretnom radnjom.',
		startBody:
			'Većina timova počinje izradom studije, dodavanjem ljudi i pokretanjem prikupljanja nakon što je postavljanje spremno.',
		setupWorkspace: 'Postavite radni prostor',
		chooseNext: 'Odaberite sljedeću radnju',
		firstRunActions: ['Izradi prvu studiju', 'Pozovi tim', 'Postavi imenik'],
		firstRunActionStatuses: ['Počnite ovdje', 'Pristup', 'Ljudi'],
		firstRunActionDescriptions: [
			'Pokrenite stvarnu studiju i nastavite kroz postavljanje, prikupljanje i rezultate.',
			'Pripremite pristup članova radnog prostora prije dijeljenja prve poveznice za prijavu.',
			'Izradite ljude, grupe, članstva i veze s voditeljima za ciljanje.'
		],
		nextActions: 'Sljedeće radnje',
		nextActionsTitle: 'Nastavite rad koji traži pažnju.',
		noWorkspaceActions: 'Nema hitnih radnji',
		noWorkspaceActionsBody: 'Otvorite Studije za izradu ili nastavak rada na studiji.',
		openStudies: 'Otvori Studije',
		examples: 'Primjeri',
		sampleStudies: 'Primjeri studija',
		sampleStudiesBody:
			'Dovršeni sintetički primjeri pokazuju tijek rada bez promjene stvarnih podataka radnog prostora.',
		sampleWorkloadTitle: 'Opterećenje i oporavak',
		sampleWorkloadKicker: 'Radna studija kroz dva mjerenja',
		sampleWorkloadBody:
			'Istražite zatvorenu ponovljenu studiju s dovršenim postavljanjem, prikupljenim odgovorima, rezultatima i usporedbom.',
		sampleWorkloadMeta: 'Anonimno povezano praćenje, dva mjerenja, ogledni izvještaj',
		sampleErgonomicsTitle: 'Pregled ergonomskog rizika',
		sampleErgonomicsKicker: 'Primjer zaštite na radu',
		sampleErgonomicsBody:
			'Pregledajte dovršenu studiju u stilu medicine rada s agregiranim rezultatima po skupinama i kontekstom izvoza.',
		sampleErgonomicsMeta: 'Zatvoreno prikupljanje, vidljivi agregati, ogledni izvoz',
		sampleStudentTitle: 'Dobrobit i opterećenje studenata',
		sampleStudentKicker: 'Akademski primjer studije',
		sampleStudentBody:
			'Pogledajte kako studentska wellbeing studija prolazi od upitnika do pregleda odgovora i usporedbe mjerenja.',
		sampleStudentMeta: 'Ponovljeno mjerenje, povezana usporedba, sintetički odgovori',
		openSample: 'Otvori primjer samo za čitanje',
		sampleReadOnlyNote:
			'Primjeri studija služe samo za čitanje. Ne izrađuju i ne mijenjaju stvarne studije radnog prostora.',
		yourWork: 'Vaš rad',
		yourStudies: 'Vaše studije',
		openPortfolio: 'Otvori Studije',
		noStudiesYet: 'Još nema stvarnih studija',
		noStudiesYetBody: 'Vaše uređive studije pojavit će se ovdje nakon izrade.',
		workspaceOverview: 'Pregled radnog prostora',
		sampleDemo: {
			eyebrow: 'Primjeri za pregled',
			title: 'Pregledajte završene primjere studija.',
			description:
				'Pogledajte završene sintetske studije s postavljanjem, prikupljanjem, rezultatima i izvozom prije izrade vlastite studije.',
			status: 'Primjer podataka',
			aria: 'Knjižnica primjera studija samo za čitanje',
			backToHome: 'Natrag na početnu',
			synthetic: 'Sintetski podaci',
			readOnlyTitle: 'Primjer samo za čitanje',
			readOnlyBody:
				'Ovaj primjer služi za orijentaciju. Ne izrađuje sudionike, ne šalje pozivnice i ne mijenja stvarne studije u radnom prostoru.',
			chooseSample: 'Odaberite primjer studije',
			chooseSampleBody:
				'Prebacujte se između realističnih tipova studija i vidite kako različiti istraživački konteksti prolaze kroz isti tijek rada.',
			snapshot: 'Sažetak studije',
			inspect: 'Što možete pregledati',
			questionnaire: 'Izvadak iz upitnika',
			findings: 'Primjeri bilješki rezultata',
			files: 'Prikazane datoteke',
			responseMetric: 'Odgovori',
			measurementMetric: 'Mjerenja',
			resultMetric: 'Paket rezultata',
			exportMetric: 'Izvoz',
			ready: 'Spremno',
			closed: 'Zatvoreno',
			available: 'Dostupno',
			workloadMetrics: [
				'412 predanih odgovora u odabranim timovima.',
				'Dva zatvorena mjerenja pripremljena za pregled promjene kroz vrijeme.',
				'Pokrivenost, ograničenja i bilješke usporedbe su spremni.',
				'Skup odgovora i opis podataka dostupni su za preuzimanje.'
			],
			workloadChecks: [
				'Upitnik i izlazi rezultata su dovršeni.',
				'Prikupljanje je zatvoreno prije pregleda paketa rezultata.',
				'Ponovljeno sudjelovanje podržava tumačenje promjene kroz vrijeme.',
				'Kontekst izvoza objašnjava čemu svaka datoteka služi.'
			],
			workloadQuestions: [
				'Koliko je radno opterećenje bilo upravljivo tijekom zadnja dva tjedna?',
				'Koliko često ste se mogli dovoljno oporaviti između zahtjevnih razdoblja rada?',
				'Koliko ste sigurni da se trenutni radni zahtjevi mogu održati?'
			],
			workloadFindings: [
				'Kapacitet oporavka poboljšao se nakon promjena u planiranju rasporeda i opterećenja.',
				'Radno opterećenje ostaje povišeno u jednoj operativnoj grupi.',
				'Bilješke tumačenja razdvajaju opaženi signal od tvrdnji koje podaci ne podržavaju.'
			],
			workloadFiles: [
				'Skup odgovora s opisom podataka.',
				'Izvoz sažetka rezultata za pregledni paket.',
				'Bilješke usporedbe za dva zatvorena mjerenja.'
			],
			ergonomicsMetrics: [
				'128 predanih odgovora iz odabranih radnih grupa.',
				'Jedno zatvoreno mjerenje pripremljeno za pregled po odjelima.',
				'Agregirani rezultati vidljivi su ondje gdje pravila prikaza to dopuštaju.',
				'CSV izvoz i bilješke pregleda su dostupni.'
			],
			ergonomicsChecks: [
				'Radne grupe vidljive su bez otkrivanja pojedinačnih odgovora.',
				'Dijelovi upitnika odvajaju fizičko opterećenje, prilagodbu radnog mjesta i oporavak.',
				'Bilješke za djelovanje usmjerene su na daljnji pregled, ne na medicinsku dijagnozu.',
				'Izvoz je konačan jer je prikupljanje zatvoreno.'
			],
			ergonomicsQuestions: [
				'Koliko često je položaj tijela na radnom mjestu bilo teško održati?',
				'Koliko ste kontrole imali nad pauzama ili kretanjem tijekom dana?',
				'Koliko često su raspored zadataka ili oprema nepotrebno otežavali rad?'
			],
			ergonomicsFindings: [
				'Jedna grupa uloga pokazuje veću zabrinutost za prilagodbu radnog mjesta od prosjeka.',
				'Odgovori o kontroli pauza upućuju na praktično područje intervencije.',
				'Male grupe ostaju skrivene prema pravilima prikaza.'
			],
			ergonomicsFiles: [
				'Izvoz agregiranog pregleda po odjelima.',
				'Skup odgovora s opisom podataka.',
				'Sažetak bilješki za interno praćenje.'
			],
			studentMetrics: [
				'275 predanih odgovora kroz studentske skupine.',
				'Dva mjerenja podržavaju pregled prije i poslije.',
				'Sažeci wellbeing stanja i opterećenja studijem su pripremljeni.',
				'Izvoz za analizu dostupan je s kontekstnim bilješkama.'
			],
			studentChecks: [
				'Opterećenje studijem i wellbeing ostaju odvojeni u izlazima rezultata.',
				'Bilješke usporedbe označavaju gdje su studentske skupine usporedive.',
				'Nalazi iz otvorenog teksta sažeti su kao poticaji za pregled.',
				'Datoteke su označene kao sintetske i samo za čitanje.'
			],
			studentQuestions: [
				'Koliko je opterećenje studijem bilo upravljivo tijekom zadnjeg tjedna?',
				'Koliko ste bili sigurni da možete dovršiti obvezni rad na vrijeme?',
				'Koliko često su studijski zahtjevi ometali oporavak ili san?'
			],
			studentFindings: [
				'Pritisak opterećenja studijem smanjio se nakon promjena rasporeda provjera.',
				'Oporavak ostaje slabiji u jednoj skupini i traži daljnji pregled.',
				'Bilješke rezultata izbjegavaju tvrdnje na razini pojedinca.'
			],
			studentFiles: [
				'Izvoz trenda skupine s opisom podataka.',
				'Sažetak rezultata za akademski pregled.',
				'Kontekst usporedbe za dva mjerenja.'
			]
		}
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
		startBlueprint: 'Odaberite kako započeti studiju',
		selectedStartingPoint: 'Odabrana početna točka',
		studyModelTitle: 'Studija, izvor i upitnik',
		studyModelBody:
			'Prvo se izrađuje studija. Početna točka samo daje izvorni materijal za Postavljanje; upitnik, izlaze rezultata, primatelje i mjerenja i dalje uređujete prije pokretanja.',
		studyModelStudy: 'Studija',
		studyModelStudyBody:
			'Trajni spremnik u radnom prostoru za postavljanje, mjerenja, rezultate i izvozne datoteke.',
		studyModelStartingPoint: 'Početna točka',
		studyModelStartingPointBody:
			'Daje izvorni materijal za prvi nacrt u Postavljanju. To nije završni upitnik ni sama studija.',
		studyModelSetup: 'Postavljanje',
		studyModelSetupBody:
			'Mjesto gdje izvorni materijal pretvarate u upitnik, izlaze rezultata, plan primatelja i provjeru pokretanja.',
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
		visibility: 'Vidljivost',
		loadFailed: 'Studije se nisu mogle učitati.',
		enterStudyName: 'Unesite naziv studije.',
		createFailed: 'Studiju nije moguće izraditi.',
		renameFailed: 'Studiju nije moguće preimenovati.',
		restoreFailed: 'Studiju nije moguće vratiti.',
		archiveFailed: 'Studiju nije moguće arhivirati.',
		duplicateSampleFailed: 'Primjer studije nije moguće duplicirati.',
		saving: 'Spremanje...',
		saveName: 'Spremi naziv',
		duplicating: 'Dupliciranje...',
		renameStudyName: 'Preimenuj studiju',
		cancel: 'Odustani',
		rename: 'Preimenuj'
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
		description: 'Pronađite CSV datoteke i opise podataka izrađene na stranicama Rezultata studije.',
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
		saveManager: 'Spremi managera',
		loadFailed: 'Imenik osoba nije se mogao učitati.',
		enterPersonIdentity: 'Unesite ime za prikaz, e-poštu ili vanjski id.',
		personCreateFailed: 'Osobu nije moguće izraditi.',
		csvRequired: 'Zalijepite CSV retke ili prvo odaberite CSV datoteku.',
		csvImportFailed: 'CSV uvoz publike nije moguće dovršiti.',
		selectPerson: 'Odaberite osobu.',
		personUpdateFailed: 'Osobu nije moguće ažurirati.',
		enterGroupIdentity: 'Unesite vrstu i naziv grupe.',
		groupCreateFailed: 'Grupu osoba nije moguće izraditi.',
		selectPersonAndGroup: 'Odaberite osobu i grupu.',
		membershipSaveFailed: 'Članstvo u grupi nije moguće spremiti.',
		managerSaveFailed: 'Odnos managera nije moguće spremiti.',
		rowsNeedingAttention: 'Redci koji traže pažnju',
		subjects: 'Osobe',
		externalId: 'Vanjski id',
		locale: 'Jezik',
		manager: 'Manager',
		directReports: 'Izravni članovi',
		noMemberships: 'Nema članstava',
		parent: 'Nadređena grupa',
		members: 'Članovi',
		person: 'Osoba',
		newPerson: 'Nova osoba',
		displayName: 'Ime za prikaz',
		attributesJson: 'Atributi JSON',
		newGroup: 'Nova grupa',
		parentGroup: 'Nadređena grupa',
		noParent: 'Nema nadređene grupe',
		hierarchySetup: 'Postavljanje hijerarhije',
		validFrom: 'Vrijedi od',
		selectedPerson: 'Odabrana osoba',
		noManager: 'Nema managera',
		managed: 'Ima managera',
		notAvailable: 'Nije dostupno',
		unmatchedRow: 'Nepovezan redak',
		previewed: 'Pregledano',
		imported: 'Uvezeno',
		loadingDirectory: 'Učitavanje imenika osoba',
		unavailableTitle: 'Imenik osoba nije dostupan',
		retryDirectory: 'Pokušaj ponovno',
		createRecordsAria: 'Izrada zapisa imenika',
		creating: 'Izrada...',
		addPerson: 'Dodaj osobu',
		createGroup: 'Izradi grupu'
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
		studySetupBody: 'Izradite ili nastavite studije, upitnike, mjerenja i postavljanje rezultata.',
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
		metaTitle: 'Odjava od pozivnica za studije - Validated Scale',
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
				'Pogledajte gdje studija stoji, zatim nastavite postavljanje, prikupljanje, rezultate ili usporedbu mjerenja.',
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
			statusDescription: 'Pregledajte spremnost, pravila i mjerenja povezana s ovom studijom.',
			dates: 'Datumi',
			datesAria: 'Datumi odabrane studije',
			studyModel: 'Model studije',
			lifecycle: 'Životni ciklus',
			governance: 'Upravljanje',
			governanceAria: 'Status upravljanja',
			policyScoring: 'Status pravila i bodovanja',
			campaigns: 'Mjerenja',
			campaignsAria: 'Popis mjerenja odabrane studije',
			campaignsInStudy: 'Mjerenja u ovoj studiji',
			noCampaigns: 'Nijedno mjerenje još nije povezan s ovom studijom.',
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
				eyebrow: 'Usporedba mjerenja',
				title: 'Mjerenja',
				description:
					'Izradite naknadna mjerenja, zatim usporedite povezanu promjenu kroz ponovljeno sudjelovanje kad je studija spremna.',
				ariaLabel: 'Mjerenja i povezani ponovljeni odgovori'
			}
		},
		setupBody: {
			progressAriaLabel: 'Napredak postavljanja studije',
			progressKicker: 'Postavljanje studije',
			progressTitle: 'Napredak postavljanja studije',
			progressBody:
				'Studija je projekt. Postavljanje pretvara izvorni materijal u upitnik, izlaze rezultata, primatelje za Mjerenje 1 i provjeru pokretanja.',
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
				blueprintTitle: 'Provjera upitnika',
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
						detail: 'Kompaktan puls zdravlja tima za ponovljena mjerenja ili jednokratni interni pregled.'
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
						'Ispitanici ostaju anonimni u izvještaju, ali se ponovljena mjerenja mogu povezati za promjenu kroz vrijeme.',
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
					externalEmailsHelp: 'Zalijepite vanjske adrese e-pošte za ovo mjerenje bez dodavanja u imenik.'
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
			locale: 'hr-HR' as AppLocale,
			stepNumber: (number: number) => `${number}`,
			defaultWaveName: (number: number) => `Mjerenje ${number}`,
			steps: {
				instrument: {
					title: 'Izvor upitnika',
					description:
						'Odaberite višekratni ili uvezeni izvorni materijal. On pokreće upitnik, ali nije studija ni završni upitnik.'
				},
				template: {
					title: 'Upitnik',
					description: 'Izradite spremljeni skup pitanja na koji sudionici odgovaraju u ovoj studiji.'
				},
				scoring: {
					title: 'Izlazi rezultata',
					description:
						'Odaberite koji odgovori iz upitnika postaju izlazi rezultata i kako se obrađuju nedostajući odgovori.'
				},
				campaign: {
					title: 'Mjerenje i primatelji',
					description: 'Pripremite krug prikupljanja, način odgovaranja i primatelje za ovu studiju.'
				},
				readiness: {
					title: 'Provjera pokretanja',
					description:
						'Provjerite upitnik, postavljanje rezultata, mjerenje, primatelje i pravila prije početka prikupljanja.'
				}
			},
			disabled: {
				confirmInstrument: 'Prvo odaberite izvor upitnika.',
				saveQuestionnaire: 'Prvo spremite upitnik.',
				createCollectionWave: 'Prvo izradite mjerenje.'
			},
			pathDisplay: {
				done: 'Gotovo',
				current: 'Trenutno',
				selected: 'Odabrano',
				next: 'Sljedeće',
				blocked: 'Blokirano'
			},
			launchState: {
				createWaveFirstStatus: 'Prvo izradite mjerenje',
				createWaveFirstNext: 'Izradite i spremite mjerenje prije provjere pokretanja.',
				runLaunchCheckFirst: 'Prvo pokrenite provjeru',
				launchPassedSaveRecipients: 'Provjera je prošla; spremite primatelje za identificirani pristup',
				launchPassedChooseAccess: 'Provjera je prošla; odaberite javnu poveznicu ili spremite primatelje',
				saveRecipientsForIdentified:
					'Spremite primatelje ispod prije pokretanja kako bi Prikupljanje moglo izraditi identificirani pristup.',
				openCollectionOrSaveRecipients:
					'Otvorite Prikupljanje za pokretanje javnom poveznicom ili spremite primatelje ispod prije pokretanja.',
				launchPassedWithRecipients: 'Provjera je prošla sa spremljenim primateljima',
				openCollectionStartSavedRecipients:
					'Otvorite Prikupljanje za pokretanje mjerenja i slanje spremljenim primateljima.',
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
				summary: 'Pripremite mjerenje, način odgovaranja, primatelje i prijenos u Prikupljanje prije pokretanja.',
				draftWave: 'Nacrt mjerenja',
				wave: 'Mjerenje',
				responseMode: 'Način odgovaranja',
				recipients: 'Primatelji',
				collectionHandoff: 'Prijenos u Prikupljanje',
				waveDraftReady: (waveName: string) => `${waveName} je nacrt mjerenja za ovu studiju.`,
				waveWillBeCreated: (waveName: string) =>
					`${waveName} nastat će kad spremite ovaj korak.`,
				identifiedModeDetail:
					'Identificirano prikupljanje zahtijeva spremljene primatelje kako bi svaki sudionik dobio dodijeljeni pristup.',
				longitudinalModeDetail:
					'Prikupljanje s ponovnim sudjelovanjem može koristiti javni pristup ili spremljene primatelje; sudionici koriste vlastiti ponovni kod za usporedbu.',
				anonymousModeDetail: 'Anonimno prikupljanje može koristiti javnu poveznicu ili spremljene pozive e-poštom.',
				chooseModeDetail: 'Odaberite kako sudionici ulaze u ovo mjerenje.',
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
				launchPassedOpenCollection: 'Provjera je prošla; otvorite Prikupljanje za pokretanje mjerenja.',
				runLaunchCheckBeforeCollection: 'Pokrenite provjeru prije otvaranja Prikupljanja.'
			},
			designMap: {
				title: 'Kako je ova studija sastavljena',
				summary:
					'Studija je projektni spremnik. Izvorni materijal pokreće upitnik; izlazi rezultata tumače odgovore; mjerenja prikupljajuju odgovore.',
				source: 'Izvor upitnika',
				questionnaire: 'Upitnik',
				results: 'Izlazi rezultata',
				waves: 'Mjerenja prikupljanja',
				sourceReady: 'Izvorni materijal spreman je za ovaj upitnik.',
				sourceMissing: 'Odaberite višekratni ili uvezeni izvorni materijal prije spremanja upitnika.',
				questionnaireSaved: (name: string, questionCount: number) =>
					`${name} spremljen je s ${questionCount} ${questionCount === 1 ? 'pitanjem' : 'pitanja'}.`,
				questionnaireMissing: 'Spremite upitnik prije postavljanja rezultata ili provjere pokretanja.',
				resultsReady: (ruleKey: string) => `Izlazi rezultata spremljeni su kao ${ruleKey}.`,
				resultsMissing: 'Odaberite koji odgovori iz upitnika postaju izlazi rezultata.',
				noWaves: 'Još nema mjerenja prikupljajunja.',
				draftWaveNeedsReadiness: (count: number) =>
					`${count} ${count === 1 ? 'nacrt mjerenja pripremljen je' : 'nacrta mjerenja pripremljena su'}; spremnost pokretanja još treba pažnju.`,
				waveReady: (count: number) =>
					`${count} ${count === 1 ? 'nacrt mjerenja spreman je' : 'nacrta mjerenja spremna su'} za Prikupljanje.`,
				liveWave: (count: number) =>
					`${count} ${count === 1 ? 'mjerenje prikuplja' : 'mjerenja prikupljaju'} odgovore.`,
				closedWave: (count: number) =>
					`${count} ${count === 1 ? 'mjerenje ima' : 'mjerenja imaju'} zatvorene podatke za pregled Rezultata.`
			},
			waveContext: {
				prepareForCollection: (waveName: string) => `Pripremite ${waveName} za prikupljanje`,
				firstWaveSetup: 'Postavljanje prvog mjerenja',
				currentDraftWave: 'Trenutni nacrt mjerenja',
				followUpDraftWave: 'Nacrt naknadnog mjerenja',
				futureWaveSetup: 'Postavljanje budućeg mjerenja',
				firstWaveSummary: 'Ovdje izradite prvo mjerenje i odlučite tko može odgovoriti.',
				currentDraftSummary: 'Dovršite trenutni nacrt mjerenja prije otvaranja Prikupljanja.',
				followUpDraftSummary: (waveName: string) =>
					`${waveName} je nacrt naknadnog mjerenja. Koristite ga samo kad je sljedeći krug prikupljanja namjeran.`,
				closedOneWaveSummary: (
					previousWaveName: string,
					previousWaveStatus: string,
					nextWaveName: string
				) =>
					`${previousWaveName} je već ${previousWaveStatus}. Izradite ${nextWaveName} samo kad je sljedeći krug prikupljanja namjeran.`,
				multipleWaveSummary: (existingWaveCount: number, nextWaveName: string) =>
					`${existingWaveCount} mjerenja već postoje. Izradite ${nextWaveName} tek nakon pregleda rezultata trenutnog mjerenja.`,
				createFirstAfterSetup:
					'Izradite Mjerenje 1 tek nakon što su upitnik i postavljanje rezultata spremljeni.',
				recipientBelongsUntilLaunch: (waveName: string) =>
					'Odabir primatelja pripada ' + waveName + ' dok se to mjerenje ne pokrene.',
				reviewResultsBeforeFollowup:
					'Pregledajte prethodno mjerenje u Rezultatima prije nego ga tretirate kao naknadno prikupljanje.',
				doNotAssumeRecipients:
					'Nemojte pretpostaviti da su primatelji isti; spremite namjeravane ljude ili grupu za ovo mjerenje.',
				reviewBeforePreparing: (previousWaveName: string, nextWaveName: string) =>
					`Pregledajte ${previousWaveName} prije pripreme ${nextWaveName}`,
				reviewExistingBeforePreparing: (nextWaveName: string) =>
					`Pregledajte postojeće mjerenja prije pripreme ${nextWaveName}`,
				openResultsBeforeCreating: (reviewTarget: string, nextWaveName: string) =>
					`Otvorite Rezultate za pregled ili izvoz ${reviewTarget} prije izrade ${nextWaveName}.`,
				createOnlyWhenIntentional: (nextWaveName: string) =>
					`Izradite ${nextWaveName} samo kad je sljedeći krug prikupljanja namjeran.`,
				recipientBelongsToNewDraft: (previousLabel: string) =>
					`Odabir primatelja u ovom koraku pripadat će novom nacrtu mjerenja, ne ${previousLabel}.`,
				previousWaves: 'prethodnim mjerenjima'
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
				'Pokrenite mjerenje, podijelite pristup sudionicima, pratite predaje i zatvorite prikupljanje kada studija završi.',
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
				collectionWave: 'Mjerenje prikupljanja',
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
					'Pokretanje prikupljanja otvara odabrano mjerenje za odgovore i bilježi verziju postavljanja koju će izvještaji kasnije koristiti.',
				start: 'Pokreni prikupljanje',
				resultLabel: 'Prikupljanje'
			},
			shareAccess: {
				body:
					'Odaberite kako sudionici ulaze u ovo mjerenje. Spremljeni odabiri iz Imenika i grupa postaju privatne pozivnice. Jednokratni uvoz koristite samo za dodavanje ad hoc primatelja nakon pokretanja ili izradite otvorenu poveznicu kada je širok pristup prihvatljiv.',
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
					'Ovo mjerenje već ima privatne pozivnice e-poštom. Otvorene poveznice su isključene kako bi sudjelovanje ostalo ograničeno na pozvane primatelje.',
				openLinkReadyHelp:
					'Ovo mjerenje već ima jednu aktivnu otvorenu poveznicu. Ako je poveznica izgubljena, zamijenite je ovdje. Stara poveznica prestat će prihvaćati nove sudionike; postojeće sesije odgovaranja mogu završiti kroz svoje privatne sesijske oznake.',
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
			},
			component: {
				doNotContactList: 'Popis bez kontaktiranja',
				suppressEmailsBeforeInviting: 'Blokiraj e-poštu prije slanja poziva',
				emailToSuppress: 'E-pošta za blokiranje',
				internalNote: 'Interna bilješka',
				addToDoNotContact: 'Dodaj na popis bez kontaktiranja',
				refreshDoNotContactList: 'Osvježi popis bez kontaktiranja',
				emailInvitationStatus: 'Status pozivnica e-poštom',
				queued: 'U redu čekanja',
				suppressed: 'Blokirano',
				sendAttempts: 'Pokušaji slanja',
				providerAccepted: 'Davatelj prihvatio',
				providerDelivered: 'Davatelj dostavio',
				providerBounced: 'Davatelj odbio',
				complaints: 'Pritužbe',
				invitedEmailAccess: 'Pristup pozivnicom e-pošte',
				addOneOffRecipientsAfterLaunch: 'Dodajte jednokratne primatelje nakon pokretanja',
				postLaunchAdditions: 'Dodavanje nakon pokretanja',
				addOneTimeRecipientsToThisWave: 'Dodaj jednokratne primatelje u ovo mjerenje',
				nameForReview: 'Ime za pregled',
				addToReviewList: 'Dodaj na popis za pregled',
				importRecipients: 'Uvezi primatelje',
				reviewOrPasteSourceList: 'Pregledajte ili zalijepite izvorni popis',
				recipientSource: 'Izvor primatelja',
				importReview: 'Pregled uvoza',
				invalid: 'Neispravno',
				duplicates: 'Duplikati',
				keepValidOnly: 'Zadrži samo ispravne',
				clearList: 'Očisti popis',
				doNotContactMatch: 'Podudaranje s popisom bez kontaktiranja',
				invitationEmailPreview: 'Pregled e-pošte pozivnice',
				subject: 'Predmet',
				retrySafetyCheck: 'Sigurnosna potvrda ponovnog slanja',
				createAdHocInvitations: 'Izradi ad hoc pozivnice',
				sendNextEmailBatch: 'Pošalji sljedeći paket e-pošte',
				retryFailedEmails: 'Ponovi neuspjele e-poruke',
				invitationsCreated: 'Pozivnice izrađene',
				emailDeliveryBatch: 'Paket dostave e-pošte',
				failedEmailsRequeued: 'Neuspjele e-poruke vraćene u red',
				demoTestData: 'Demo/testni podaci',
				simulateCollectionResponses: 'Simuliraj odgovore prikupljanja',
				shareLink: 'Poveznica za dijeljenje',
				notLoaded: 'Nije učitano',
				anonymousInviteOnly: 'Anonimno samo pozivnicom',
				unavailable: 'Nije dostupno',
				noEmailFound: 'E-pošta nije pronađena',
				responsesCreated: 'Odgovori izrađeni',
				stagingDemo: 'Staging/demo',
				errors: {
					createWaveBeforeReadiness:
						'Izradite mjerenje prije pokretanja provjere prije pokretanja.',
					createWaveBeforeStart: 'Izradite mjerenje prije pokretanja prikupljanja.',
					createWaveBeforeAccess: 'Izradite mjerenje prije izrade pristupa sudionicima.',
					privateInvitationsAlreadyActive:
						'Ovo mjerenje već koristi privatne pozivnice e-poštom. Otvorene poveznice su isključene kako bi pristup ostao samo pozivnicom.',
					openLinkAlreadyActive:
						'Ovo mjerenje već ima otvorenu poveznicu. Koristite postojeću poveznicu ili izradite novo mjerenje ako je poveznica izgubljena.',
					createWaveBeforeReplaceLink:
						'Izradite mjerenje prije zamjene otvorene poveznice.',
					replaceLinkOnlyForAnonymous:
						'Zamjena otvorene poveznice dostupna je samo za anonimno prikupljanje otvorenom poveznicom.',
					createWaveBeforeInvitations: 'Izradite mjerenje prije pripreme pozivnica.',
					emailInvitationsRequireAnonymous:
						'Pozivnice e-poštom dostupne su za anonimna mjerenja ili mjerenja s ponovljenim sudjelovanjem.',
					openLinkBlocksPrivateInvitations:
						'Ovo mjerenje već ima otvorenu poveznicu. Privatne pozivnice e-poštom isključene su za prikupljanje otvorenom poveznicom.',
					reviewRecipientsBeforeInviting:
						'Prvo pregledajte popis primatelja. Uklonite neispravne ili duplicirane adrese prije izrade pozivnica.',
					recipientFileCouldNotRead: 'Datoteku primatelja nije moguće pročitati.',
					enterOneValidEmail: 'Unesite jednu ispravnu adresu e-pošte.',
					recipientAlreadyInWaveList: 'Ovaj primatelj već je na popisu mjerenja.',
					createWaveBeforeSending: 'Izradite mjerenje prije slanja pozivnica.',
					checkEmailSetupBeforeSending:
						'Provjerite postavke slanja e-pošte prije slanja pozivnica.',
					resolveEmailSetupBeforeSending:
						'Riješite blokade postavki e-pošte prije slanja.',
					createWaveBeforeRetrying: 'Izradite mjerenje prije ponavljanja neuspjelih e-poruka.',
					confirmRetryBeforeRequeueing:
						'Potvrdite da je nova pozivnica prikladna prije vraćanja u red.',
					checkEmailSetupBeforeRetrying:
						'Provjerite postavke slanja e-pošte prije ponavljanja neuspjelih pozivnica.',
					resolveEmailSetupBeforeRetrying:
						'Riješite blokade postavki e-pošte prije ponavljanja neuspjelih pozivnica.',
					createAndStartBeforeSimulating:
						'Izradite i pokrenite mjerenje prije simulacije odgovora.',
					createWaveBeforeRepairReadiness:
						'Izradite mjerenje prije provjere spremnosti popravka e-pošte.',
					enterEmailBeforeSuppressing:
						'Unesite adresu e-pošte prije dodavanja na popis bez kontaktiranja.',
					refreshDoNotContactBeforeRelease:
						'Osvježite popis bez kontaktiranja prije oslobađanja ovog primatelja.',
					collectionStatusRefreshError: 'Status prikupljanja nije moguće osvježiti.',
					collectionStatusRefreshFailed: 'Osvježavanje statusa prikupljanja nije uspjelo.',
					refreshWarning:
						'Radnja je spremljena, ali ovaj prikaz prikupljanja nije se mogao osvježiti.',
					collectionActionFailed: 'Radnja prikupljanja nije uspjela.'
				},
				emailStatus: {
					notChecked: 'Nije provjereno',
					smtpReady: 'SMTP spreman',
					smtpSendReady: 'SMTP slanje spremno',
					localProofMode: 'Lokalni dokazni način',
					needsConfig: 'Nedostaju postavke',
					noQueuedEmails: 'Nema pozivnica e-poštom koje čekaju slanje.',
					noRetryableFailedEmails: 'Nema neuspjelih pozivnica koje čekaju ponavljanje.'
				},
				deliveryGuidance: {
					sesSandbox:
						'AWS SES je odbio barem jednog primatelja jer je račun još u sandboxu. Provjerite primatelja malim slovima u istoj SES regiji ili pričekajte produkcijski pristup, zatim ponovite neuspjele e-poruke.',
					sesSenderRejected:
						'AWS SES je odbio identitet pošiljatelja. Provjerite potvrđenu domenu/adresu pošiljatelja u SES-u, zatim ponovite neuspjele e-poruke.',
					sesIdentityRejected:
						'AWS SES je odbio provjeru potvrđenog identiteta. Potvrdite identitete pošiljatelja i sandbox primatelja u podešenoj SES regiji, zatim ponovite neuspjele e-poruke.',
					smtpAuth:
						'SMTP davatelj je odbio autentikaciju. Provjerite SES SMTP korisničko ime i lozinku na serveru, zatim ponovite neuspjele e-poruke.',
					smtpTls:
						'SMTP TLS rukovanje nije uspjelo. Provjerite host, port i TLS postavke prije ponavljanja neuspjelih e-poruka.',
					throttled:
						'AWS SES je ograničio ovaj paket. Pričekajte da prođe ograničenje davatelja, zatim ponovite neuspjele e-poruke.',
					suppressedRecipient:
						'Barem jedan primatelj je na popisu bez kontaktiranja. Pregledajte blokade prije ponavljanja.',
					providerRejected:
						'Davatelj je odbio barem jednu pozivnicu. Provjerite postavke e-pošte i status davatelja, zatim ponovite neuspjele e-poruke kada je novo slanje prikladno.',
					sentAccepted:
						'Poslano znači da je poruka prihvaćena u SMTP predaji. Dokazi dostave, odbijanja i pritužbi pojavit će se kasnije u dokazima dostave davatelja.',
					failedRetry:
						'Neuspjele pozivnice mogu se ponoviti nakon ispravka problema davatelja. Koristite ponavljanje neuspjelih e-poruka u koraku pristupa sudionicima.',
					suppressedFailures:
						'Neke neuspjele pozivnice blokirane su popisom bez kontaktiranja ili povratnom informacijom davatelja. Pregledajte popis blokada prije ponovnog slanja.',
					ambiguous:
						'Neke predaje nisu jasne. Tretirajte ih kao moguće poslane i ponavljajte tek nakon provjere dokaza davatelja.',
					providerEvents:
						'Događaji davatelja usklađeni su za ovo mjerenje. Učitajte nedavne događaje davatelja za pregled prihvaćanja, dostava, odbijanja ili pritužbi.',
					noCleanup: 'Za ovo mjerenje trenutno nije potrebno čišćenje dostave e-pošte.'
				},
				readinessGuidance: {
					useDraftWaveTitle: 'Koristite nacrt mjerenja',
					useDraftWaveDetail:
						'Ovo mjerenje više nije nacrt ili zakazano. Otvorite Postavljanje, odaberite ili izradite nacrt mjerenja i ponovno pokrenite provjeru.',
					chooseResponseModeTitle: 'Odaberite način odgovaranja',
					chooseResponseModeDetail:
						'Otvorite Postavljanje i spremite korak Prikupljanje s važećim načinom odgovaranja prije pokretanja.',
					connectQuestionnaireTitle: 'Povežite upitnik s ovim mjerenjem',
					connectQuestionnaireDetail:
						'Otvorite Postavljanje, spremite korak Upitnik, zatim spremite korak Prikupljanje kako bi mjerenje koristilo taj upitnik.',
					finishQuestionnaireTitle: 'Dovršite upitnik',
					finishQuestionnaireDetail:
						'Otvorite Postavljanje i dodajte barem jednu sekciju i pitanje prije pokretanja prikupljanja.',
					finishResultsTitle: 'Dovršite izlaze rezultata',
					finishResultsDetail:
						'Otvorite Postavljanje i spremite izlaze rezultata kako bi izvještaji znali koji odgovori postaju rezultati.',
					completePoliciesTitle: 'Dovršite pravila studije',
					completePoliciesDetail:
						'Otvorite Postavljanje i spremite pravila privole, zadržavanja i prikaza prije pokretanja studije.',
					switchResponseModeTitle: 'Promijenite način odgovaranja',
					switchResponseModeDetail:
						'Spremljeni popisi konkretnih e-adresa dostupni su za anonimna mjerenja ili mjerenja s ponovljenim sudjelovanjem. Otvorite Postavljanje i promijenite način odgovaranja ili uklonite spremljeni popis primatelja.',
					addEmailsTitle: 'Dodajte adrese e-pošte primatelja',
					addEmailsDetail:
						'Otvorite Imenik i dodajte adrese e-pošte za sve u spremljenom odabiru primatelja, zatim ponovno pokrenite provjeru.',
					selectRecipientTitle: 'Odaberite barem jednog primatelja',
					selectRecipientDetail:
						'Otvorite Postavljanje i spremite barem jedan odabir primatelja koji pronalazi aktivne osobe.',
					emptyRecipientTitle: 'Odabir primatelja je prazan',
					emptyRecipientDetail:
						'Dodajte aktivne osobe u odabranu grupu u Postavljanju ili uklonite spremljeni odabir primatelja ako ovo mjerenje treba koristiti opću poveznicu.',
					fixAudienceTitle: 'Popravite tko može odgovoriti',
					fixAudienceDetail:
						'Otvorite Postavljanje i prilagodite odabir primatelja dok pregled ne pronađe očekivane osobe.',
					reviewInstrumentTitle: 'Pregledajte izvor upitnika',
					reviewInstrumentDetail:
						'Otvorite Postavljanje i ponovno spremite korake Izvor upitnika i Upitnik kako bi ovo mjerenje koristilo upitnik spreman za pokretanje.',
					reviewSetupTitle: 'Pregledajte postavljanje'
				}
			}
		},		operationsWorkflow: {
			locale: 'hr-HR' as AppLocale,
			stepNumber: (number: number) => `${number}`,
			actions: {
				readiness: {
					title: 'Provjera prije pokretanja',
					description: 'Potvrdite da su upitnik, postavljanje rezultata, primatelji i pravila spremni.'
				},
				launch: {
					title: 'Pokretanje prikupljanja',
					description: 'Otvorite ovo mjerenje za odgovore i zabilježite postavke korištene za izvještavanje.'
				},
				openLink: {
					title: 'Dijeljenje pristupa',
					description: 'Pošaljite spremljene pozive ili izradite otvorenu poveznicu za ovo mjerenje.'
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
				createWaveBeforeReadiness: 'Izradite mjerenje u Postavljanju prije provjere spremnosti.',
				createWaveBeforeStart: 'Izradite mjerenje prije pokretanja prikupljanja.',
				startBeforeAccess: 'Pokrenite prikupljanje prije pripreme pristupa sudionicima.',
				startBeforeMonitor: 'Pokrenite prikupljanje prije praćenja odgovora.',
				createWaveBeforeClose: 'Izradite mjerenje prije zatvaranja prikupljanja.',
				waveClosed: 'Ovo mjerenje je zatvoren.',
				alreadyLive: 'Prikupljanje je već aktivno.',
				startedThisSession: 'Prikupljanje je pokrenuto u ovoj sesiji.',
				runPrelaunchAndSetup:
					'Pokrenite provjeru prije pokretanja. Ako kaže Blokirano, otvorite Postavljanje i dovršite navedene stavke.',
				onlyLiveClosable: 'Zatvoriti se može samo aktivan mjerenje.'
			},
			status: {
				lifecycleLabel: 'Životni ciklus prikupljanja',
				responseProgressLabel: 'Napredak odgovora',
				accessLabel: 'Pristup',
				reportingReadinessLabel: 'Spremnost izvještaja',
				noWaveSelectedTitle: 'Nijedno mjerenje nije odabran',
				noWaveSelectedDetail: 'Izradite ili odaberite mjerenje prije prikupljanja odgovora.',
				noResponsesYetTitle: 'Još nema odgovora',
				noResponsesYetDetail: 'Brojevi odgovora pojavit će se nakon pokretanja mjerenja.',
				noRecipientAccessTitle: 'Pristup sudionicima nije pripremljen',
				noRecipientAccessDetail: 'Odaberite primatelje ili izradite pristup sudionicima nakon što je postavljanje spremno.',
				reportingNotAvailableTitle: 'Nije dostupno',
				reportingNotAvailableDetail: 'Spremnost izvještaja prikazuje se nakon što prikupljanje ima odabrano mjerenje.',
				createWaveFirstHeadline: 'Prvo izradite mjerenje',
				createWaveFirstGuidance: 'Prikupljanje počinje nakon što Postavljanje ima mjerenje.',
				createWaveFirstNextAction: 'Otvorite Postavljanje i izradite mjerenje.',
				closedTitle: 'Zatvoreno',
				closedDetail: 'Ovo mjerenje više ne prihvaća nove odgovore.',
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
					`${openLinkCount} identificirana pristupna poveznica${pluralSuffix} pripremljena. Sudionici su povezani s poznatim zapisima osoba za ovo mjerenje.`,
				inviteOnlyDetail: (invitationCount: string, verb: string, boundary: string) =>
					`${invitationCount} spremljenih poziva e-poštom ${verb} za ovo mjerenje. Samo spremljeni primatelji dobivaju privatni pristup, a ${boundary}`,
				mixedAccessDetail: (
					openLinkCount: string,
					openPluralSuffix: string,
					invitationCount: string,
					invitationPluralSuffix: string,
					boundary: string
				) =>
					`${openLinkCount} otvorenih poveznica${openPluralSuffix} i ${invitationCount} spremljenih poziva${invitationPluralSuffix}. Otvorena poveznica je širok pristup; invite-only e-pošta ograničava ulaz na spremljene primatelje. ${boundary}`,
				openLinkDetail: (openLinkCount: string, verb: string) =>
					`${openLinkCount} otvorenih poveznica ${verb}. Svatko s poveznicom može ući u ovo mjerenje; koristite spremljene pozive kad pristup treba biti ograničen.`,
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
			locale: "hr-HR",
			surface: {
				reviewActionsAria: "Radnje pregleda i izvoza",
				flowKicker: "Tijek studije · Rezultati",
				title: "Pregled i izvoz rezultata",
				description: "Pregledajte agregirane rezultate, provjerite jesu li spremni za korištenje i izradite datoteke izvoza kada su spremne.",
				useDecisionLabel: "Odluka o korištenju",
				resultsUseReviewAria: "Pregled korištenja rezultata",
				nextActionLabel: "Sljedeći korak",
				scoreMethodLabel: "Metoda rezultata",
				scoreMethodReviewAria: "Pregled metode rezultata",
				exportPreviewLabel: "Pregled izvoza",
				exportPreviewAria: "Pregled izvoza"
			},

			stepNumber: (number: number) => `${number}`,
			actions: {
				reportProof: {
					title: 'Pregled rezultata',
					description: 'Pregledajte sažetke rezultata za odabrano mjerenje bez narušavanja pravila prikaza.'
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
				createOrSelectWaveBeforeReviewingResults: 'Izradite ili odaberite mjerenje prije pregleda rezultata.',
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
					noCampaign: 'Izradite ili odaberite mjerenje prije pregleda rezultata.',
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
					'Pregledajte namjenu datoteke, oblik redaka, polja mjerenja, ključeve praćenja, varijable, nedostajuće vrijednosti i rezultate prije preuzimanja.',
				createOrSelectWaveFirst: 'Prvo izradite ili odaberite mjerenje',
				reviewExportFileFirst: 'Prvo pregledajte datoteku izvoza',
				selectWavePendingDetail: 'Odaberite mjerenje prije pripreme datoteka izvoza.',
				reviewFilePendingDetail: 'Pregledajte datoteku izvoza kako biste provjerili CSV i knjigu kodova.',
				downloadResponseDatasetCsv: 'Preuzmi CSV skupa odgovora',
				downloadReportSummaryCsv: 'Preuzmi CSV sažetka izvještaja'
			},
			component: {
				state: {
					working: 'U tijeku',
					saved: 'Spremljeno',
					failed: 'Neuspjelo',
					ready: 'Spremno',
					done: 'Dovršeno',
					current: 'Trenutno',
					blocked: 'Blokirano'
				},
				errors: {
					refreshFailed:
						'Radnja je spremljena, ali osvježavanje prostora rezultata nije uspjelo.',
					actionFailed: 'Radnja nad rezultatima nije uspjela.',
					createWaveBeforeResults: 'Izradite ili odaberite mjerenje prije pregleda rezultata.',
					createWaveBeforeReportExport:
						'Izradite ili odaberite mjerenje prije izrade sažetka izvještaja.',
					createStudyBeforeResponseExport:
						'Izradite ili odaberite studiju prije izrade izvoza odgovora.',
					createExportBeforeReview: 'Izradite ili odaberite datoteku izvoza prije pregleda.',
					selectDownloadableExport:
						'Odaberite datoteku izvoza koja se može preuzeti prije preuzimanja CSV-a.',
					createExportBeforeDownload:
						'Izradite ili odaberite datoteku izvoza prije preuzimanja CSV-a.'
				},
				currentPurpose: {
					responseDataset: 'CSV skupa odgovora i opis podataka',
					reportSummary: 'CSV sažetka izvještaja, nije skup odgovora za analizu'
				},
				downloadAction: 'Radnja preuzimanja',
				reviewPathAria: 'Tijek pregleda i izvoza',
				readOnlyTitle: 'Samo za čitanje',
				readOnlyBody: 'Pregled i izvoz traže pristup za upravljanje radnim prostorom.',
				currentTaskAria: 'Trenutni zadatak pregleda',
				taskProgress: (completed: number, total: number) =>
					`${completed} od ${total} zadataka rezultata dovršeno`,
				currentTaskTitle: 'Trenutni zadatak rezultata',
				selectedWave: 'Odabrano mjerenje',
				previewStatus: 'Status pregleda',
				readyForReview: 'Spremno za pregled',
				finishSetupFirst: 'Prvo dovršite postavljanje',
				interpretation: 'Tumačenje',
				missing: 'Nedostaje',
				notAvailable: 'Nije dostupno',
				suppressed: 'skriveno',
				latestExport: 'Najnoviji izvoz',
				exportCount: 'Broj izvoza',
				reportExportResult: 'Rezultat izvoza sažetka',
				reportExport: 'Izvoz sažetka',
				reportSummaryCsvCodebook: 'CSV sažetka i opis podataka',
				createReportSummaryExport: 'Izradi izvoz sažetka',
				exportFile: 'Datoteka izvoza',
				series: 'Studija',
				latestResponseExport: 'Najnoviji izvoz odgovora',
				responseExportResult: 'Rezultat izvoza odgovora',
				responseExport: 'Izvoz odgovora',
				responseCsvCodebook: 'CSV odgovora i opis podataka',
				createResponseExport: 'Izradi izvoz odgovora',
				responseFile: 'Datoteka odgovora',
				downloadStatus: 'Status preuzimanja',
				downloadable: 'Dostupno za preuzimanje',
				notReady: 'Nije spremno',
				latestFile: 'Najnovija datoteka',
				filePurpose: 'Namjena datoteke',
				reviewedFile: 'Pregledana datoteka',
				downloadedFile: 'Preuzeta datoteka',
				reportPreviewAria: 'Pregled izvještaja',
				resultsPreview: 'Pregled rezultata',
				aggregateResultPreview: 'Pregled agregiranih rezultata',
				internalPreview: 'Interni pregled',
				responsesSuffix: 'odgovori',
				minimumGroup: (kMin: number) => `Najmanja grupa ${kMin}`,
				reportPreviewScoresAria: 'Rezultati u pregledu izvještaja',
				reportScoreAria: (dimensionCode: string) => `Rezultat ${dimensionCode}`,
				scoreCount: (count: number | string) => `rezultata=${count}`,
				exportPreparing: 'Priprema',
				rowsLabel: 'Redci',
				rows: (count: number) => `${count} redaka`,
				file: 'Datoteka',
				downloadedCsv: 'Preuzeti CSV',
				bytes: (count: number) => `${count} bajtova`,
				goToWaves: 'Idi na mjerenja',
				reviewed: 'pregledano',
				notReviewed: 'nije pregledano',
				official: 'službeno',
				notOfficial: 'nije službeno'
			},

		},		wavesWorkflow: {
			locale: 'hr-HR' as AppLocale,
			stepNumber: (number: number) => `${number}`,
			surface: {
				reviewActionsAria: 'Tijek usporedbe mjerenja',
				flowKicker: 'Tijek studije · Mjerenja',
				title: 'Ponovite studiju i usporedite mjerenja',
				description:
					'Izradite sljedeća mjerenja iz Postavljanja, prikupite odgovore iz Prikupljanja, zatim ovdje usporedite zatvorena mjerenja.',
				scoreMethodLabel: 'Metoda rezultata',
				scoreMethodReviewAria: 'Pregled metode rezultata za mjerenja'
			},
			plan: {
				createFirstTitle: 'Izradite prvo mjerenje',
				createFirstDescription: 'Počnite izradom Mjerenja 1 kao prvog kruga prikupljanja za ovu studiju.',
				openSetupLabel: 'Otvori Postavljanje',
				createFirstGuidance: [
					'Svako mjerenje je krug prikupljanja unutar studije. Izradite Mjerenje 1 u Postavljanju, zatim ga pokrenite iz Prikupljanja.',
					'Nakon što odgovori stignu, pregledajte mjerenje u Rezultatima prije dodavanja sljedećeg mjerenja.',
					'Koristite anonimno ponovljeno sudjelovanje od prvog mjerenja ako kasnije trebate povezanu promjenu kroz vrijeme.'
				],
				reviewWavePairTitle: (wavePairTitle: string) => `Pregledajte ${wavePairTitle}`,
				groupTrendReviewDescription:
					'Ova mjerenja mogu se pregledati kao rezultati na razini grupe. Povezana promjena istih sudionika traži ponovljeno sudjelovanje od prvog mjerenja.',
				reviewGroupTrendLabel: 'Pregledaj grupni trend',
				groupTrendReviewGuidance: (nextWaveNumber: number) => [
					'Pregledajte ove mjerenja kao trend na razini grupe. Nemojte ga opisivati kao promjenu istih sudionika jer su mjerenja anonimni.',
					'Koristite ponovljeno sudjelovanje od Mjerenja 1 kada studija kasnije treba povezanu promjenu kroz vrijeme.',
					`Pregledajte ili izvezite Mjerenje 1 i Mjerenje 2 prije izrade Mjerenja ${nextWaveNumber} u Postavljanju.`
				],
				oneWaveTitle: (nextWaveNumber: number) => `Pregledajte Mjerenje 1 prije planiranja Mjerenja ${nextWaveNumber}`,
				oneWaveDescription:
					'Mjerenje 1 postoji. Prvo pregledajte trenutne rezultate; sljedeće mjerenje planirajte samo kada je novi krug prikupljanja namjeran.',
				reviewWaveResultsLabel: (waveNumber: number) => `Pregledaj rezultate Mjerenja ${waveNumber}`,
				planWaveLaterLabel: (waveNumber: number) => `Planiraj Mjerenje ${waveNumber} kasnije`,
				oneWaveGuidance: (nextWaveNumber: number) => [
					`Pregledajte ili izvezite Mjerenje 1 prije izrade Mjerenja ${nextWaveNumber} u Postavljanju.`,
					'Koristite anonimno ponovljeno sudjelovanje kada isti sudionik treba biti povezan kroz mjerenja.',
					'Pregledajte primatelje prije pokretanja novog mjerenja; nemojte pretpostaviti da je publika ista osim ako to Prikupljanje jasno pokazuje.'
				],
				checkReadinessTitle: 'Provjera povezane promjene',
				checkReadinessDescription:
					'Postoje dva mjerenja s ponovljenim sudjelovanjem. Sada potvrdite povezani ponovljeni odgovori i kompatibilnost bodovanja.',
				runChecksBelowLabel: 'Pokreni provjere u nastavku',
				reviewResultsLabel: 'Pregledaj rezultate',
				checkReadinessGuidance: [
					'Koristite provjere u nastavku kako biste potvrdili da se oba mjerenja mogu sigurno povezati.',
					'Rezultati ostaju po mjerenjima dok povezani odgovori, prikaz i bodovanje nisu spremni za povezanu usporedbu.',
					'Ako je usporedba blokirana, u detaljima pogledajte koji preduvjet nedostaje.'
				],
				sameRespondentTitle: 'Provjeri promjenu istih sudionika',
				sameRespondentDescription:
					'Postoje dva mjerenja s ponovljenim sudjelovanjem. Pokrenite provjere prije nego što ovo tretirate kao promjenu istih sudionika.',
				runLinkedChecksBelowLabel: 'Pokreni povezane provjere',
				sameRespondentGuidance: [
					'Provjerite povezane odgovore, pravila prikaza, kompatibilnost bodovanja i vidljive promjene prije tvrdnji o promjeni kroz vrijeme.',
					'Koristite Rezultate za izvoz po mjerenjima; koristite Mjerenja samo kada trebate pregledan kontekst promjene kroz vrijeme.',
					'Novi sljedeće mjerenje izradite u Postavljanju kada počinje novi krug prikupljanja.'
				]
			},
			groupTrend: {
				notReadyTitle: 'Grupni trend nije spreman',
				notReadyDescription: 'Prikupite odgovore u barem dva mjerenja prije pregleda trenda po mjerenjima.',
				sameRespondentComparisonLabel: 'Usporedba istih sudionika',
				notReadySameRespondentValue: 'Nije dostupno dok ne postoje dva ponovljena mjerenja',
				disclosureStatusLabel: 'Status prikaza',
				notReadyDisclosureValue: 'Pregledajte nakon što postoje rezultati sljedećeg mjerenja',
				notReadyGuidance: [
					'Grupni trend uspoređuje rezultate na razini mjerenja. Ne traži povezivanje sudionika.',
					'Pokrenite i prikupite sljedeće mjerenje prije čitanja trenda.',
					'Koristite ponovljeno sudjelovanje ako trebate promjenu istih sudionika, a ne samo pomak na razini mjerenja.'
				],
				title: (baselineName: string, comparisonName: string) =>
					`Samo grupni trend: ${baselineName} prema ${comparisonName}`,
				readyDescription:
					'Agregirani rezultati na razini grupe spremni su za pregled kao trend. To nije promjena istih sudionika.',
				pendingDescription:
					'Oba mjerenja imajuju odgovore. Dovršite rezultate bodovanja prije nego trend tretirate kao spreman.',
				firstWaveScoresLabel: 'Rezultati prvog mjerenja',
				secondWaveScoresLabel: 'Rezultati drugog mjerenja',
				runComparisonChecksValue: 'Pokrenite provjere prije tvrdnji o promjeni istih sudionika',
				notConfiguredValue: 'Nije konfigurirano za povezanu promjenu istih sudionika',
				disclosureNotAvailableValue: 'Pregledajte prikaz po mjerenjima u Rezultatima prije tvrdnji',
				suppressedLinkedComparisonsLabel: 'Skrivene povezane usporedbe',
				openResultsLabel: 'Otvori Rezultate',
				readyGuidance: [
					'Koristite ovo za anonimne ili nepovezana mjerenja kada je pitanje je li se grupa pomaknula između krugova.',
					'Nemojte ovo opisivati kao individualno poboljšanje ili pogoršanje osim ako je povezana promjena spremna.',
					'Pregledajte bodovanje i pravila prikaza u Rezultatima prije tvrdnji iz trenda.'
				]
			},
			comparisonReview: {
				title: 'Plan usporedbe',
				description:
					'Provjerite je li studija spremna za sljedeće mjerenje, agregirani grupni trend ili povezanu promjenu istih sudionika.'
			},
			scoreMethodReview: {
				title: 'Što se uspoređuje?',
				description:
					'Pregledajte pravila bodovanja, metodu povezanih parova, uspoređene izlaze, nedostajuće vrijednosti i granice tumačenja prije korištenja promjene kroz mjerenja.'
			},
			actions: {
				twoWaveProof: {
					title: 'Provjera povezane promjene',
					description:
						'Potvrdite da studija ima mjerenja s ponovljenim sudjelovanjem i povezane odgovore za usporedbu istih sudionika.'
				},
				waveComparisonProof: {
					title: 'Pregled povezane promjene',
					description: 'Pregledajte promjenu istih sudionika između odabranih mjerenja bez narušavanja pravila prikaza.'
				}
			},
			disabled: {
				unlinkedWavesUseGroupTrend:
					'Povezana usporedba istih sudionika nije dostupna jer ova mjerenja nisu izrađeni s ponovljenim sudjelovanjem. Pregledajte grupni trend.',
				addRepeatedWaves: 'Dodajte barem dva ponovljena mjerenja prije usporedbe promjene kroz vrijeme.',
				chooseBaselineAndComparison: 'Odaberite početni i usporedno mjerenje prije pregleda promjene kroz vrijeme.',
				checkReadinessBeforeReview: 'Provjerite spremnost usporedbe prije pregleda promjene kroz vrijeme.'
			},
			inactiveReason: {
				groupTrend:
					'Ova studija podržava samo agregirani grupni trend. Provjere povezane promjene nisu potrebne i bile bi zavaravajuće.',
				noWaves: 'Izradite i prikupite prve mjerenja prije provjera povezane promjene.',
				oneWave:
					'Pregledajte Mjerenje 1 u Rezultatima. Planirajte Mjerenje 2 iz Postavljanja samo kada je sljedeći krug prikupljanja namjeran.',
				needScoredResponses: 'Prikupite bodovane odgovore u barem dva mjerenja prije zadataka usporedbe.'
			},
			component: {
				state: {
					working: 'U tijeku',
					viewed: 'Pregledano',
					failed: 'Neuspjelo',
					ready: 'Spremno',
					done: 'Dovršeno',
					current: 'Trenutno',
					blocked: 'Blokirano'
				},
				errors: {
					refreshFailed:
						'Radnja nad mjerenjima je dovršena, ali osvježavanje prostora nije uspjelo.',
					actionFailed: 'Radnja nad mjerenjima nije uspjela.'
				},
				wavePlanAria: 'Plan mjerenja',
				whereWavesFit: 'Uloga mjerenja',
				waveComparisonPlanAria: 'Plan usporedbe mjerenja',
				comparisonPlan: 'Plan usporedbe',
				groupTrendAria: 'Pregled grupnog trenda',
				groupTrend: 'Grupni trend',
				firstWave: 'Prvo mjerenje',
				firstWaveResponses: 'Odgovori prvog mjerenja',
				secondWave: 'Drugo mjerenje',
				secondWaveResponses: 'Odgovori drugog mjerenja',
				missing: 'Nedostaje',
				wavesPathAria: 'Tijek mjerenja',
				currentTaskAria: 'Trenutni zadatak mjerenja',
				taskProgress: (completed: number, total: number) =>
					`${completed} od ${total} zadataka usporedbe dovršeno`,
				currentTaskTitle: 'Trenutni zadatak usporedbe',
				selectedSeries: 'Odabrana studija',
				repeatedWaves: 'Ponovljena mjerenja',
				potentialCompleteTrajectories: 'Moguće potpuni parovi ponovljenih odgovora',
				runLinkedTrajectoryCheck: 'Provjeri povezane ponovljene odgovore',
				study: 'Studija',
				baseline: 'Početno mjerenje',
				comparison: 'Usporedno mjerenje',
				compatibility: 'Kompatibilnost',
				disclosure: 'Pravila prikaza',
				minimumGroupSize: 'Najmanja grupa',
				notConfigured: 'Nije postavljeno',
				suppressedComparisons: 'Skrivene usporedbe',
				reviewComparison: 'Pregledaj usporedbu',
				reviewed: 'Pregledano',
				linkedChangeTaskStatusAria: 'Status zadatka povezane promjene',
				linkedChangeWorkflow: 'Tijek povezane promjene',
				linkedChecksNotNeeded: 'Provjere povezane promjene nisu potrebne',
				linkedChecksNotActiveYet: 'Provjere povezane promjene još nisu aktivne',
				linkedTrajectoryCheckAria: 'Provjera povezanih ponovljenih odgovora',
				waveReadiness: 'Spremnost mjerenja',
				linkedTrajectoryCheck: 'Provjera povezanih ponovljenih odgovora',
				launchedWaves: (count: number) => `${count} pokrenutih mjerenja`,
				wavesWithResponses: (count: number) => `${count} mjerenja s odgovorima`,
				linkedTrajectories: (count: number) => `${count} povezanih ponovljenih odgovora`,
				completeTrajectories: (count: number) => `${count} potpunih parova ponovljenih odgovora`,
				waveAria: (name: string) => `Mjerenje ${name}`,
				responseMode: 'Način odgovaranja',
				submittedResponses: 'Predani odgovori',
				waveComparisonPreviewAria: 'Pregled usporedbe mjerenja',
				waveComparison: 'Usporedba mjerenja',
				disclosureGatedComparison: 'Usporedba uz pravila prikaza',
				disclosureK: (kMin: number) => `Pravila prikaza k=${kMin}`,
				waveComparisonScoresAria: 'Rezultati usporedbe mjerenja',
				waveComparisonScoreAria: (dimensionCode: string) =>
					`Usporedba mjerenja ${dimensionCode}`,
				pairedDelta: (value: string) => `promjena u paru ${value}`,
				baselineMeta: (value: string) => `početno ${value}`,
				comparisonMeta: (value: string) => `usporedno ${value}`,
				baselineBand: (label: string) => `početni raspon ${label}`,
				comparisonBand: (label: string) => `usporedni raspon ${label}`,
				suppressed: 'skriveno',
				notAvailable: 'Nije dostupno',
				backToResults: 'Natrag na rezultate',
				setUpNextWave: 'Postavi sljedeće mjerenje',
				reviewedInterpretation: 'pregledano',
				notReviewedInterpretation: 'nije pregledano',
				officialInterpretation: 'službeno',
				notOfficialInterpretation: 'nije službeno'
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
					'Odaberite dva usporediva mjerenja prije učitavanja pregleda usporedbe.',
				runLinkedTrajectoryCheck:
					'Provjerite povezane ponovljene odgovore prije učitavanja pregleda usporedbe.'
			},
			dashboard: {
				unavailableTitle: 'Pregled mjerenja nije dostupan',
				unavailableMessage: 'Odaberite dva usporediva mjerenja prije pregleda nadzorne ploče mjerenja.',
				title: (baselineName: string, comparisonName: string) =>
					`${baselineName} prema ${comparisonName} pregled mjerenja`,
				campaigns: 'Mjerenja',
				longitudinalWaves: 'Ponovljena mjerenja',
				submittedWaves: 'Mjerenja s odgovorima',
				missingPrerequisites: 'Nedostajući preduvjeti',
				baselineWave: 'Početno mjerenje',
				baselineStatus: 'Status početnog mjerenja',
				baselineSubmittedResponses: 'Predani odgovori početnog mjerenja',
				comparisonWave: 'Usporedno mjerenje',
				comparisonStatus: 'Status usporednog mjerenja',
				comparisonSubmittedResponses: 'Predani odgovori usporednog mjerenja',
				linkedTrajectories: 'Povezani ponovljeni odgovori',
				completeTrajectories: 'Potpuni parovi ponovljenih odgovora',
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
				baselineLatestLaunch: 'Zadnje pokretanje početnog mjerenja',
				baselineScoringRule: 'Bodovanje početnog mjerenja',
				baselineDisclosurePolicy: 'Pravilo prikaza početnog mjerenja',
				comparisonLaunchSnapshot: 'Usporedni zapis pokretanja',
				comparisonLatestLaunch: 'Zadnje pokretanje usporednog mjerenja',
				comparisonScoringRule: 'Bodovanje usporednog mjerenja',
				comparisonDisclosurePolicy: 'Pravilo prikaza usporednog mjerenja',
				untitledWave: 'Neimenovano mjerenje'
			},
			chrome: {
				sectionAria: 'Pregled usporedbe mjerenja',
				kicker: 'Usporedba mjerenja',
				title: 'Uspoređena mjerenja',
				description: 'Usporedba odabranog početnog i usporednog mjerenja uz pravila prikaza.',
				summaryAria: 'Sažetak usporedbe mjerenja',
				readinessKicker: 'Spremnost usporedbe',
				readinessTitle: 'Mogu li se ova mjerenja usporediti?',
				readinessDescription:
					'Provjerava mogu li se odabrana mjerenja usporediti bez otkrivanja premalih grupa.',
				waveReadinessAria: 'Spremnost mjerenja',
				waveReadinessKicker: 'Spremnost',
				waveReadinessTitle: 'Spremnost mjerenja',
				comparisonAria: 'Status usporedbe',
				comparisonKicker: 'Usporedba',
				comparisonTitle: 'Status usporedbe',
				guardrailsAria: 'Prikaz i kompatibilnost',
				guardrailsKicker: 'Zaštitna pravila',
				guardrailsTitle: 'Prikaz i kompatibilnost',
				sourceAria: 'Kontekst izvora mjerenja',
				sourceKicker: 'Temeljeno na',
				sourceTitle: 'Kontekst pokretanja i pravila',
				resolvePrerequisites: 'Riješite preduvjete usporedbe mjerenja prije učitavanja pregleda.',
				loadFailed: 'Pregled usporedbe mjerenja nije se mogao učitati.',
				loadingComparison: 'Učitavanje usporedbe',
				refreshComparison: 'Osvježi usporedbu mjerenja',
				study: 'Studija',
				aggregateSnapshotAria: 'Sažetak usporedbe mjerenja',
				changeOverTimeTitle: 'Promjena kroz vrijeme',
				comparisonReady: 'Usporedba spremna',
				completeTrajectories: (count: number | string) => `potpuni parovi ponovljenih odgovora ${count}`,
				linkedPairs: (count: number | string) => `povezanih parova ${count}`,
				waveComparisonRowsAria: 'Redci usporedbe mjerenja',
				waveComparisonScoreAria: (dimensionCode: string) =>
					`Usporedba mjerenja ${dimensionCode}`,
				baselineMean: (value: string) => `prosjek početnog mjerenja ${value}`,
				comparisonMean: (value: string) => `prosjek usporednog mjerenja ${value}`,
				baselineMeta: (value: string) => `početno ${value}`,
				comparisonMeta: (value: string) => `usporedno ${value}`,
				aggregateDelta: (value: string) => `agregirana promjena ${value}`,
				pairedDelta: (value: string) => `promjena u paru ${value}`,
				baselineBand: (value: string) => `početni raspon ${value}`,
				comparisonBand: (value: string) => `usporedni raspon ${value}`
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
				campaign: 'Mjerenje',
				campaignStatus: 'Status mjerenja',
				closedAt: 'Zatvoreno',
				closedWave: 'Zatvoreno mjerenje',
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
				reportableCampaigns: 'Mjerenja za izvještaj',
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
				selectedCampaign: 'Odabrano mjerenje',
				selectedCampaignReportStateUnavailable:
					'Stanje rezultata odabranog mjerenja nije dostupno.',
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
				closed_wave: 'zatvoreno mjerenje',
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
					'Detalji za audit i rješavanje problema za ovo mjerenje. Većina rada prikupljanja treba biti u tijeku iznad.',
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
				selectedCampaignAria: 'Odabrano mjerenje rezultata',
				selectedWaveKicker: 'Odabrano mjerenje',
				reportStateTitle: 'Stanje izvještaja',
				sourceAria: 'Kontekst izvora rezultata',
				basedOn: 'Temeljeno na',
				launchPolicyExport: 'Pokretanje, pravila i kontekst izvoza',
				prerequisitesAria: 'Nedostajući uvjeti rezultata',
				prerequisitesKicker: 'Preduvjeti',
				prerequisitesTitle: 'Nedostajući uvjeti rezultata',
				waves: 'Mjerenja',
				includedWaves: 'Uključena mjerenja rezultata'
			},
			wavesDetails: {
				summary: 'Detalji mjerenja',
				kicker: 'Detalji mjerenja',
				title: 'Detalji usporedbe',
				description:
					'Koristite ove detalje kad je usporedba mjerenja blokirana ili treba audit kontekst. Normalna usporedba treba biti u tijeku iznad.',
				comparedWavesAria: 'Uspoređena mjerenja',
				comparedWavesKicker: 'Uspoređena mjerenja',
				selectedComparison: 'Odabrana usporedba',
				baselineWave: 'Početno mjerenje',
				comparisonWave: 'Usporedno mjerenje',
				comparisonStatus: 'Status usporedbe',
				disclosure: 'Prikaz',
				compatibility: 'Kompatibilnost',
				missing: 'Nedostaje',
				readinessAria: 'Spremnost mjerenja',
				readinessKicker: 'Spremnost usporedbe',
				availableTitle: 'Što je dostupno?',
				sourceAria: 'Kontekst izvora mjerenja',
				basedOn: 'Temeljeno na',
				launchPolicy: 'Kontekst pokretanja i pravila',
				prerequisitesAria: 'Nedostajući uvjeti mjerenja',
				prerequisitesKicker: 'Blokirana usporedba',
				prerequisitesTitle: 'Što treba pažnju?',
				availableWavesAria: 'Dostupna mjerenja',
				availableWavesKicker: 'Dostupna mjerenja',
				waveHistory: 'Povijest mjerenja'
			},
			fallback: {
				selectedSeriesContext: 'kontekst odabrane studije',
				productWorkflow: 'Tijek rada proizvoda',
				previewWorkflow: 'Pregled tijeka rada',
				governance: 'Upravljanje',
				selectedSeriesReadiness: 'Spremnost odabrane studije',
				campaignRows: 'Popis mjerenja',
				campaignRowsAria: 'Popis mjerenja odabrane studije',
				campaignContext: 'Kontekst mjerenja odabrane studije',
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

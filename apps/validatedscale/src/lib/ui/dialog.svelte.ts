/** App-wide modal dialogs replacing native confirm()/prompt(). One DialogHost renders these. */
type DialogRequest = {
	kind: 'confirm' | 'prompt';
	title: string;
	body?: string;
	confirmLabel: string;
	cancelLabel: string;
	danger?: boolean;
	placeholder?: string;
	initialValue?: string;
	resolve: (value: string | boolean | null) => void;
};

export const dialogState = $state<{ current: DialogRequest | null }>({ current: null });

export function confirmDialog(options: {
	title: string;
	body?: string;
	confirmLabel?: string;
	danger?: boolean;
}): Promise<boolean> {
	return new Promise((resolve) => {
		dialogState.current = {
			kind: 'confirm',
			title: options.title,
			body: options.body,
			confirmLabel: options.confirmLabel ?? 'Confirm',
			cancelLabel: 'Cancel',
			danger: options.danger,
			resolve: (value) => resolve(value === true)
		};
	});
}

export function promptDialog(options: {
	title: string;
	body?: string;
	confirmLabel?: string;
	placeholder?: string;
	initialValue?: string;
}): Promise<string | null> {
	return new Promise((resolve) => {
		dialogState.current = {
			kind: 'prompt',
			title: options.title,
			body: options.body,
			confirmLabel: options.confirmLabel ?? 'Continue',
			cancelLabel: 'Cancel',
			placeholder: options.placeholder,
			initialValue: options.initialValue,
			resolve: (value) => resolve(typeof value === 'string' ? value : null)
		};
	});
}

export function settleDialog(value: string | boolean | null) {
	dialogState.current?.resolve(value);
	dialogState.current = null;
}

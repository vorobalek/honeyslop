export type ConfirmationRequest = {
    question: string;
    detail?: string;
    sender?: string;
    button?: string;
};

type Opener = (request: ConfirmationRequest, resolve: (confirmed: boolean) => void) => void;

let opener: Opener = (request, resolve) => resolve(window.confirm(request.question));

export function registerConfirmation(next: Opener): void {
    opener = next;
}

export function confirmAction(request: ConfirmationRequest): Promise<boolean> {
    return new Promise((resolve) => opener(request, resolve));
}

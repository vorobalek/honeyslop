import { ConfirmationRequest } from '../../lib/confirmation';
import { ResponseChangeModeType } from '../../types/responseChangeModeType';

export function changeConfirmation(mode: ResponseChangeModeType, count: number): ConfirmationRequest {
    const single = count === 1;
    const subject = single ? 'this entry' : `${count} selected entries`;
    const expires = single ? 'It expires' : 'They expire';
    return mode === 'regenerate'
        ? {
              question: `Regenerate ${subject}?`,
              detail: `${expires} and the model writes ${single ? 'a new response' : 'new responses'}.`,
              button: 'Regenerate',
          }
        : {
              question: `Replace the response of ${subject}?`,
              detail: `${expires} and your response is served from now on.`,
              button: 'Replace',
          };
}

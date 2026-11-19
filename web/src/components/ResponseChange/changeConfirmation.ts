import { ConfirmationRequest } from '../../lib/confirmation';
import { ResponseChangeModeType } from '../../types/responseChangeModeType';

export function changeConfirmation(mode: ResponseChangeModeType, count: number): ConfirmationRequest {
    const single = count === 1;
    const subject = single ? 'this entry' : `${count} selected entries`;
    const paths = single ? 'this path' : 'these paths';
    return mode === 'regenerate'
        ? {
              question: `Regenerate ${subject}?`,
              detail: `The model writes ${single ? 'a new response' : 'new responses'} for ${paths}.`,
              button: 'Regenerate',
          }
        : {
              question: `Replace the response of ${subject}?`,
              detail: `Your response is served for ${paths} from now on.`,
              button: 'Replace',
          };
}

import { ConfirmationRequest } from '../../lib/confirmation';
import { ResponseChangeModeType } from '../../types/responseChangeModeType';

export function createConfirmation(mode: ResponseChangeModeType): ConfirmationRequest {
    return mode === 'regenerate'
        ? {
              question: 'Create this entry?',
              detail: 'The model writes a response for this request.',
              button: 'Create',
          }
        : {
              question: 'Create this entry with your response?',
              detail: 'Your response is served for this request from now on.',
              button: 'Create',
          };
}

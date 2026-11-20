import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { NOISE_TIERS, noiseLabel, noiseSize } from '../../lib/noise';
import { postNotice } from '../../lib/noticeStore';
import { NoiseTierType } from '../../types/noiseTierType';
import React, { useEffect, useRef, useState, type FormEvent } from 'react';

type NoiseDialogProps = {
    title: string;
    noise: NoiseTierType | null;
    question: (tier: NoiseTierType | null) => string;
    detail: (tier: NoiseTierType | null) => string;
    notice: (tier: NoiseTierType | null) => string;
    save: (tier: NoiseTierType | null) => Promise<string | null>;
    onClose: () => void;
};

const NONE = 'None';

function choicesOf(noise: NoiseTierType | null): readonly (NoiseTierType | null)[] {
    return noise === null ? NOISE_TIERS : [null, ...NOISE_TIERS.filter((tier) => tier !== noise)];
}

function describe(tier: NoiseTierType | null): string {
    return tier === null ? 'None: ordinary answers again' : `${noiseLabel(tier)}: ${noiseSize(tier)} of zero bytes`;
}

const NoiseDialog: React.FC<NoiseDialogProps> = ({ title, noise, question, detail, notice, save, onClose }) => {
    const dialog = useRef<HTMLDialogElement>(null);
    const [error, setError] = useState<string | null>(null);
    const [sending, setSending] = useState(false);
    const choices = choicesOf(noise);

    useEffect(() => {
        if (dialog.current?.open === false) dialog.current.showModal();
    }, []);

    const send = async (tier: NoiseTierType | null) => {
        const confirmed = await confirmAction({ question: question(tier), detail: detail(tier), button: 'Change' });
        if (!confirmed) return;
        setSending(true);
        try {
            const refused = await save(tier);
            if (refused !== null) {
                setError(refused);
                return;
            }
            dialog.current?.close();
            postNotice(notice(tier));
        } finally {
            setSending(false);
        }
    };
    const submit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const value = String(new FormData(event.currentTarget).get('Noise') ?? NONE);
        perform(
            () => send(value === NONE ? null : (value as NoiseTierType)),
            (reason) => setError(reason),
        );
    };

    return (
        <dialog
            className='noise-dialog'
            ref={dialog}
            onClose={onClose}
        >
            <form onSubmit={submit}>
                <p className='dialog-title'>{title}</p>
                <fieldset disabled={sending}>
                    <div className='setting'>
                        {choices.map((tier, index) => (
                            <label
                                key={tier ?? NONE}
                                className='checkbox'
                            >
                                <input
                                    type='radio'
                                    id={`noise-${tier ?? NONE}`}
                                    name='Noise'
                                    value={tier ?? NONE}
                                    defaultChecked={index === 0}
                                />
                                {` ${describe(tier)}`}
                            </label>
                        ))}
                        {error && <span className='error'>{error}</span>}
                        <p className='hint'>
                            Applies while noise defence is on in the settings. A tier set on a client wins over the tier
                            of an entry.
                        </p>
                    </div>
                </fieldset>
                <div className='actions'>
                    <button
                        type='button'
                        onClick={() => dialog.current?.close()}
                    >
                        Cancel
                    </button>
                    <button
                        type='submit'
                        className='danger-filled'
                        disabled={sending}
                    >
                        Change the noise
                    </button>
                </div>
            </form>
        </dialog>
    );
};

export default React.memo(NoiseDialog);

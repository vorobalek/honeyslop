import React, { useEffect, useRef, useState } from 'react';

type CopyLetterButtonProps = {
    letterId: string;
};

const RESET_AFTER = 2000;

const CopyLetterButton: React.FC<CopyLetterButtonProps> = ({ letterId }) => {
    const [label, setLabel] = useState('Copy letter');
    const timer = useRef(0);
    useEffect(() => () => window.clearTimeout(timer.current), []);
    const copy = async () => {
        try {
            await navigator.clipboard.writeText(document.getElementById(letterId)?.textContent ?? '');
            setLabel('Copied');
        } catch {
            setLabel('Select the text and copy it');
        }
        window.clearTimeout(timer.current);
        timer.current = window.setTimeout(() => setLabel('Copy letter'), RESET_AFTER);
    };
    return (
        <button
            type='button'
            onClick={() => void copy()}
        >
            {label}
        </button>
    );
};

export default React.memo(CopyLetterButton);

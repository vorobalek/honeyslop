import { FILTER_DELAY } from '../lib/constants';
import { currentSorting, href } from '../lib/listQuery';
import { navigate } from '../lib/navigation';
import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react';

export type FilterValues = Record<string, string>;

export type AutoFilter = {
    formKey: number;
    onInput: (event: FormEvent<HTMLFormElement>) => void;
    onChange: (event: ChangeEvent<HTMLFormElement>) => void;
    onSubmit: (event: FormEvent<HTMLFormElement>) => void;
};

function keyOf(names: string[], values: FilterValues): string {
    return JSON.stringify(names.map((name) => values[name] ?? ''));
}

function valuesOf(form: HTMLFormElement): FilterValues {
    const values: FilterValues = {};
    new FormData(form).forEach((value, name) => {
        values[name] = String(value);
    });
    return values;
}

export function useAutoFilter(path: string, applied: FilterValues): AutoFilter {
    const names = Object.keys(applied);
    const appliedKey = keyOf(names, applied);
    const expected = useRef(appliedKey);
    const timer = useRef(0);
    const [formKey, setFormKey] = useState(0);

    useEffect(() => () => window.clearTimeout(timer.current), []);
    useEffect(() => {
        if (appliedKey === expected.current) return;
        expected.current = appliedKey;
        setFormKey((key) => key + 1);
    }, [appliedKey]);

    const apply = (form: HTMLFormElement) => {
        window.clearTimeout(timer.current);
        const values = valuesOf(form);
        expected.current = keyOf(names, values);
        navigate(href(path, { ...currentSorting(), ...values }), { replace: true });
    };

    return {
        formKey,
        onInput: (event) => {
            if (!(event.target instanceof HTMLInputElement)) return;
            const form = event.currentTarget;
            window.clearTimeout(timer.current);
            timer.current = window.setTimeout(() => apply(form), FILTER_DELAY);
        },
        onChange: (event) => {
            if (event.target instanceof HTMLSelectElement) apply(event.currentTarget);
        },
        onSubmit: (event) => {
            event.preventDefault();
            apply(event.currentTarget);
        },
    };
}

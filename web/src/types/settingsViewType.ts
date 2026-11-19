import { HoneyslopSettingsType } from './honeyslopSettingsType';

export interface SettingsViewType {
    settings: HoneyslopSettingsType;
    defaultUserAgent: string;
    defaultResponseDelayMilliseconds: number;
    defaultSenderName: string | null;
    personalSenderName: string | null;
}

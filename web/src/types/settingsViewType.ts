import { HoneyslopSettingsType } from './honeyslopSettingsType';

export interface SettingsViewType {
    settings: HoneyslopSettingsType;
    defaultUserAgent: string;
    defaultResponseDelay: string;
    defaultSenderName: string | null;
    personalSenderName: string | null;
}

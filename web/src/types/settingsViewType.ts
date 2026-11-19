import { HoneyslopSettingsType } from './honeyslopSettingsType';

export interface SettingsViewType {
    settings: HoneyslopSettingsType;
    defaultUserAgent: string;
    defaultSenderName: string | null;
    personalSenderName: string | null;
}

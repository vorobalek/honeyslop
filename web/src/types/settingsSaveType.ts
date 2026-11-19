import { MaintenanceType } from './maintenanceType';

export interface SettingsSaveType {
    errors: Record<string, string>;
    maintenance: MaintenanceType | null;
    saved: boolean;
}

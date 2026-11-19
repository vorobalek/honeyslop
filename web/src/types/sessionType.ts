export interface SessionType {
    signInEnabled: boolean;
    signedIn: boolean;
    name: string | null;
    userId: string | null;
    canOperate: boolean;
}

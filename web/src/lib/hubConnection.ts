import { LiveStatusType } from '../types/liveStatusType';
import { SessionType } from '../types/sessionType';
import {
    HttpTransportType,
    HubConnection,
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel,
} from '@microsoft/signalr';

const HUB_URL = import.meta.env.HONEYSLOP_HUB_URL;
const MAX_RETRY_DELAY = 15_000;

type Listener = () => void;

const statusListeners = new Set<Listener>();
const sessionListeners = new Set<Listener>();
const onlineCallbacks = new Set<Listener>();

let status: LiveStatusType = 'connecting';
let session: SessionType | undefined;
let resolveSession: () => void = () => {};
const sessionReady = new Promise<void>((resolve) => (resolveSession = resolve));

const connection: HubConnection = new HubConnectionBuilder()
    .withUrl(HUB_URL, { transport: HttpTransportType.WebSockets, skipNegotiation: true })
    .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (context) => Math.min(1000 * 2 ** context.previousRetryCount, MAX_RETRY_DELAY),
    })
    .configureLogging(LogLevel.None)
    .build();

function setStatus(next: LiveStatusType): void {
    if (status === next) return;
    status = next;
    for (const listener of statusListeners) listener();
}

async function goOnline(): Promise<void> {
    try {
        session = await connection.invoke<SessionType>('session');
        resolveSession();
        for (const listener of sessionListeners) listener();
    } catch {
        return;
    }
    setStatus('online');
    for (const callback of onlineCallbacks) callback();
}

async function start(delay: number): Promise<void> {
    await new Promise((resolve) => window.setTimeout(resolve, delay));
    try {
        await connection.start();
        await goOnline();
    } catch {
        setStatus('reconnecting');
        void start(Math.min(Math.max(delay * 2, 1000), MAX_RETRY_DELAY));
    }
}

connection.onreconnecting(() => setStatus('reconnecting'));
connection.onreconnected(() => void goOnline());
connection.onclose(() => {
    setStatus('reconnecting');
    void start(1000);
});

export function startHubConnection(): void {
    void start(0);
}

export function isConnected(): boolean {
    return connection.state === HubConnectionState.Connected;
}

export function onHubEvent(method: string, handler: (...args: never[]) => void): void {
    connection.on(method, handler as (...args: unknown[]) => void);
}

export function onOnline(callback: Listener): void {
    onlineCallbacks.add(callback);
}

export function sendToHub(method: string, ...args: unknown[]): void {
    if (isConnected()) connection.send(method, ...args).catch(() => {});
}

export function invokeHub<T>(method: string, ...args: unknown[]): Promise<T> {
    return connection.invoke<T>(method, ...args);
}

export function subscribeLiveStatus(listener: Listener): () => void {
    statusListeners.add(listener);
    return () => {
        statusListeners.delete(listener);
    };
}

export function getLiveStatus(): LiveStatusType {
    return status;
}

export function subscribeSession(listener: Listener): () => void {
    sessionListeners.add(listener);
    return () => {
        sessionListeners.delete(listener);
    };
}

export function getSession(): SessionType | undefined {
    return session;
}

export function waitForSession(): Promise<void> {
    return sessionReady;
}

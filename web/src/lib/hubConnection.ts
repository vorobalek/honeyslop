import { LiveStatusType } from '../types/liveStatusType';
import { SessionType } from '../types/sessionType';
import {
    HttpTransportType,
    HubConnection,
    HubConnectionBuilder,
    HubConnectionState,
    IHttpConnectionOptions,
    LogLevel,
} from '@microsoft/signalr';

const HUB_URL = import.meta.env.HONEYSLOP_HUB_URL;
const MAX_RETRY_DELAY = 15_000;

type Listener = () => void;
type Handler = (...args: unknown[]) => void;

const DIRECT: IHttpConnectionOptions = { transport: HttpTransportType.WebSockets, skipNegotiation: true };
const NEGOTIATED: IHttpConnectionOptions = {
    transport: HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling,
};

const statusListeners = new Set<Listener>();
const sessionListeners = new Set<Listener>();
const onlineCallbacks = new Set<Listener>();
const handlers: [string, Handler][] = [];

let status: LiveStatusType = 'connecting';
let session: SessionType | undefined;
let resolveSession: () => void = () => {};
const sessionReady = new Promise<void>((resolve) => (resolveSession = resolve));

let connection: HubConnection = build(DIRECT);

function build(options: IHttpConnectionOptions): HubConnection {
    const next = new HubConnectionBuilder()
        .withUrl(HUB_URL, options)
        .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: (context) =>
                Math.min(1000 * 2 ** context.previousRetryCount, MAX_RETRY_DELAY),
        })
        .configureLogging(LogLevel.None)
        .build();
    for (const [method, handler] of handlers) next.on(method, handler);
    next.onreconnecting(() => setStatus('reconnecting'));
    next.onreconnected(() => void goOnline());
    next.onclose(() => {
        if (connection !== next) return;
        setStatus('reconnecting');
        void start(1000);
    });
    return next;
}

async function connect(): Promise<void> {
    connection = build(DIRECT);
    try {
        await connection.start();
    } catch {
        connection = build(NEGOTIATED);
        await connection.start();
    }
}

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
        await connect();
        await goOnline();
    } catch {
        setStatus('reconnecting');
        void start(Math.min(Math.max(delay * 2, 1000), MAX_RETRY_DELAY));
    }
}

export function startHubConnection(): void {
    void start(0);
}

export function isConnected(): boolean {
    return connection.state === HubConnectionState.Connected;
}

export function onHubEvent(method: string, handler: (...args: never[]) => void): void {
    handlers.push([method, handler as Handler]);
    connection.on(method, handler as Handler);
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

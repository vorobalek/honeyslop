export interface ClientActivityType {
    clientIp: string;
    requests: number;
    hosts: number;
    targets: number;
    firstSeenAt: string;
    lastSeenAt: string;
    lastUserAgent: string | null;
}

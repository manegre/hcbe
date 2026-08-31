# Real-time messaging

Member chat uses the authenticated SignalR hub at `/hubs/messaging`. A member must be a participant before the hub permits joining a conversation group. The REST write remains authoritative: messages are validated and committed to PostgreSQL before the hub broadcasts `MessageReceived`.

Production requires `ConnectionStrings__Redis`. The Redis backplane lets all API instances publish to clients connected to any instance. Use a TLS Redis connection string, restrict network access to the application, enable availability and eviction alerts, and do not use the backplane as message storage; PostgreSQL remains the durable record.

The browser uses automatic reconnect and retains a low-frequency REST refresh as a recovery path. Monitor active connections, reconnect rate, hub errors, Redis latency, and message delivery lag.

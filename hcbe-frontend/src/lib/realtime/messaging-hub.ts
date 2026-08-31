import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';
import { getApiBaseUrl } from '../api/base-url';

export const createMessagingHubConnection = () => new HubConnectionBuilder()
  .withUrl(`${getApiBaseUrl()}/hubs/messaging`, {
    accessTokenFactory: () => localStorage.getItem('hcbe_token') || '',
    transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
    withCredentials: true,
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
  .build();

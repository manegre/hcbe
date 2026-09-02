import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';
import { getApiBaseUrl } from '../api/base-url';

export const createCmsHubConnection = () => new HubConnectionBuilder()
  .withUrl(`${getApiBaseUrl()}/hubs/cms`, {
    transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
    withCredentials: true,
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
  .build();

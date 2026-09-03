import { bucket, defineRailway, github, postgres, preserve, project, redis, service, volume } from "railway/iac";

export default defineRailway(() => {
  const Redis = redis("Redis", { region: "us-east4-eqdc4a" });
  Redis.deploy = { startCommand: "/bin/sh -c \"rm -rf $RAILWAY_VOLUME_MOUNT_PATH/lost+found/ && exec docker-entrypoint.sh redis-server --requirepass $REDIS_PASSWORD --save 60 1 --dir $RAILWAY_VOLUME_MOUNT_PATH\"" };
  const Postgres = postgres("Postgres", { region: "us-east4-eqdc4a" });
  const postgresVolume = volume("postgres-volume", { alerts: { usage: { "100": {}, "80": {}, "95": {} } }, allowOnlineResize: true, region: "us-east4-eqdc4a", sizeMB: 500 });
  const redisVolume = volume("redis-volume", { alerts: { usage: { "100": {}, "80": {}, "95": {} } }, allowOnlineResize: true, region: "us-east4-eqdc4a", sizeMB: 500 });
  const hcbeAssets = bucket("hcbe-assets", { region: "iad" });
  const hcbeBackups = bucket("hcbe-backups", { region: "iad" });
  const postgresBackup = service("postgres-backup", {
    source: github("manegre/hcbe", { checkSuites: true }),
    build: {
      builder: "DOCKERFILE",
      dockerfilePath: "/ops/postgres-backup/Dockerfile",
      watchPatterns: ["/ops/postgres-backup/**"],
    },
    replicas: { "ams": 1 },
    deploy: {
      cronSchedule: "15 3 * * *",
      restartPolicyType: "NEVER",
    },
    env: { AWS_ACCESS_KEY_ID: preserve(), AWS_DEFAULT_REGION: preserve(), AWS_EC2_METADATA_DISABLED: preserve(), AWS_SECRET_ACCESS_KEY: preserve(), BACKUP_ENCRYPTION_KEY: preserve(), BACKUP_RETENTION_DAYS: preserve(), DATABASE_URL: preserve(), S3_BUCKET: preserve(), S3_ENDPOINT: preserve() },
  });
  const frontend = service("frontend", {
    source: github("manegre/hcbe", { checkSuites: true, rootDirectory: "/hcbe-frontend" }),
    build: { buildEnvironment: "V3", builder: "DOCKERFILE", dockerfilePath: "/hcbe-frontend/Dockerfile", watchPatterns: ["/hcbe-frontend/**"] },
    healthcheck: "/health",
    healthcheckTimeout: 60,
    replicas: { "us-east4-eqdc4a": 1 },
    domains: ["hcbe.ca"],
    env: { VITE_API_URL: preserve(), VITE_ENABLE_ADMIN_TEAM_MEMBERS: preserve(), VITE_ENABLE_MEMBER_LOGIN: preserve(), VITE_GOOGLE_CLIENT_ID: preserve() },
  });
  const api = service("api", {
    source: github("manegre/hcbe", { checkSuites: true, rootDirectory: "/hcbe-backend" }),
    build: { buildEnvironment: "V3", builder: "RAILPACK", watchPatterns: ["/hcbe-backend/**"] },
    healthcheck: "/health/ready",
    healthcheckTimeout: 300,
    replicas: { "us-east4-eqdc4a": 1 },
    deploy: { preDeployCommand: ["dotnet HcbeApi.dll MigrateDatabase"] },
    domains: ["api.hcbe.ca"],
    env: { ASPNETCORE_ENVIRONMENT: preserve(), Authentication__Google__ClientId: preserve(), Authentication__Google__Enabled: preserve(), ConnectionStrings__DefaultConnection: preserve(), ConnectionStrings__Redis: preserve(), Cors__AllowedOrigins__0: preserve(), Cors__AllowedOrigins__1: preserve(), DataProtection__KeyEncryptionKeys: preserve(), Database__ApplyMigrationsOnStartup: preserve(), Database__Provider: preserve(), Email__FromAddress: preserve(), Email__FromName: preserve(), Email__Mode: preserve(), Email__Smtp__EnableSsl: preserve(), Email__Smtp__Host: preserve(), Email__Smtp__Password: preserve(), Email__Smtp__Port: preserve(), Email__Smtp__Username: preserve(), JwtSettings__Secret: preserve(), ObjectStorage__AccessKey: preserve(), ObjectStorage__BucketName: preserve(), ObjectStorage__ForcePathStyle: preserve(), ObjectStorage__KeyPrefix: preserve(), ObjectStorage__Provider: preserve(), ObjectStorage__Region: preserve(), ObjectStorage__SecretKey: preserve(), ObjectStorage__ServiceUrl: preserve(), Operations__AlertEmail: preserve(), PublicApiUrl: preserve(), PublicAppUrl: preserve() },
  });

  return project("hcbe", {
    resources: [postgresBackup, Redis, frontend, Postgres, api, postgresVolume, redisVolume, hcbeAssets, hcbeBackups],
  });
});

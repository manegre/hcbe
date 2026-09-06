# Production object storage

Production uploads are stored in an S3-compatible bucket instead of the application filesystem. This allows multiple API instances to serve the same assets and keeps deployments stateless.

Configure these secrets in the deployment environment:

- `ObjectStorage__ServiceUrl`
- `ObjectStorage__BucketName`
- `ObjectStorage__AccessKey`
- `ObjectStorage__SecretKey`

Optional settings are `ObjectStorage__Region`, `ObjectStorage__KeyPrefix`, `ObjectStorage__ForcePathStyle`, and `ObjectStorage__PublicBaseUrl`. The committed production configuration intentionally contains no credentials and the API fails fast if any required value is absent.

The bucket should remain private. Grant the API credentials only object read/write/delete permissions for the configured prefix. By default, stored files are exposed through the API's `/api/storage/*` read-only proxy, which is compatible with private Railway buckets. A CDN or bucket custom domain may be supplied through `PublicBaseUrl` for older stored URLs, but is not required. Enable bucket versioning and a lifecycle rule for deleted-object recovery where the provider supports them.

When migrating any legacy application volume, copy existing `/app/data/uploads` objects into the bucket under the `hcbe` prefix and update stored database URLs to `/api/storage/hcbe/...`. Verify a sample of images and documents from the public site, then retain a volume snapshot through the agreed rollback window.

Uploads are checked by size, allow-listed extension, and file signature. Object names are generated server-side and immutable cache headers are applied.

#!/bin/sh
set -eu

required_variables="DATABASE_URL S3_ENDPOINT S3_BUCKET AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY AWS_DEFAULT_REGION BACKUP_ENCRYPTION_KEY"
for variable_name in $required_variables; do
  eval "variable_value=\${$variable_name:-}"
  if [ -z "$variable_value" ]; then
    echo "Required variable $variable_name is missing" >&2
    exit 1
  fi
done

stamp="$(date -u +'%Y%m%dT%H%M%SZ')"
work_directory="$(mktemp -d)"
dump_file="$work_directory/hcbe-$stamp.dump"
encrypted_file="$dump_file.gpg"
verification_file="$work_directory/hcbe-$stamp.restore-verified.txt"
postgres_directory="$work_directory/postgres"
socket_directory="$work_directory/socket"
retention_days="${BACKUP_RETENTION_DAYS:-30}"
object_prefix="${BACKUP_OBJECT_PREFIX:-postgres}"
postgres_started=0

cleanup() {
  if [ "$postgres_started" -eq 1 ]; then
    gosu postgres pg_ctl -D "$postgres_directory" -m fast -w stop >/dev/null 2>&1 || true
  fi
  rm -rf "$work_directory"
}
trap cleanup EXIT INT TERM

echo "Creating PostgreSQL logical backup"
pg_dump "$DATABASE_URL" --format=custom --compress=9 --no-owner --no-acl --file="$dump_file"

echo "Restoring backup into an isolated PostgreSQL 18 instance"
chown postgres:postgres "$work_directory"
install -d -o postgres -g postgres "$postgres_directory" "$socket_directory"
gosu postgres initdb -D "$postgres_directory" --auth=trust --no-locale >/dev/null
gosu postgres pg_ctl -D "$postgres_directory" -o "-k $socket_directory -p 55432 -c listen_addresses=''" -w start >/dev/null
postgres_started=1
createdb --host="$socket_directory" --port=55432 --username=postgres hcbe_restore_verify
pg_restore --host="$socket_directory" --port=55432 --username=postgres --dbname=hcbe_restore_verify \
  --exit-on-error --no-owner --no-acl "$dump_file"

migration_count="$(psql --host="$socket_directory" --port=55432 --username=postgres --dbname=hcbe_restore_verify \
  --tuples-only --no-align --command='SELECT COUNT(*) FROM "__EFMigrationsHistory";')"
test "$migration_count" -gt 0
schema_status="$(psql --host="$socket_directory" --port=55432 --username=postgres --dbname=hcbe_restore_verify \
  --tuples-only --no-align --command="SELECT CASE WHEN to_regclass('public.\"Users\"') IS NOT NULL AND to_regclass('public.\"Members\"') IS NOT NULL AND to_regclass('public.\"Events\"') IS NOT NULL THEN 'verified' ELSE 'invalid' END;")"
test "$schema_status" = "verified"
printf 'verified_at_utc=%s\nmigrations=%s\npostgres_major=18\n' \
  "$(date -u +'%Y-%m-%dT%H:%M:%SZ')" "$migration_count" > "$verification_file"

echo "Encrypting verified backup"
gpg --batch --yes --pinentry-mode loopback --passphrase "$BACKUP_ENCRYPTION_KEY" \
  --symmetric --cipher-algo AES256 --output "$encrypted_file" "$dump_file"
sha256sum "$encrypted_file" > "$encrypted_file.sha256"
rm -f "$dump_file"

s3_base="s3://$S3_BUCKET/$object_prefix"
aws --endpoint-url "$S3_ENDPOINT" s3 cp "$encrypted_file" "$s3_base/$(basename "$encrypted_file")" --only-show-errors
aws --endpoint-url "$S3_ENDPOINT" s3 cp "$encrypted_file.sha256" "$s3_base/$(basename "$encrypted_file.sha256")" --only-show-errors
aws --endpoint-url "$S3_ENDPOINT" s3 cp "$verification_file" "$s3_base/$(basename "$verification_file")" --only-show-errors

echo "Removing backup objects older than $retention_days days"
cutoff_epoch="$(( $(date -u +%s) - retention_days * 86400 ))"
aws --endpoint-url "$S3_ENDPOINT" s3api list-objects-v2 --bucket "$S3_BUCKET" --prefix "$object_prefix/" \
  --query 'Contents[].[LastModified,Key]' --output text | while read -r modified_at object_key; do
    [ -n "${object_key:-}" ] || continue
    object_epoch="$(python3 -c "import datetime, sys; print(int(datetime.datetime.fromisoformat(sys.argv[1].replace('Z', '+00:00')).timestamp()))" "$modified_at")"
    if [ "$object_epoch" -lt "$cutoff_epoch" ]; then
      aws --endpoint-url "$S3_ENDPOINT" s3 rm "s3://$S3_BUCKET/$object_key" --only-show-errors
    fi
  done

echo "Backup, restore verification, encryption, and upload completed at $stamp"

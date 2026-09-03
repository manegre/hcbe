FROM postgres:16-alpine

RUN apk add --no-cache aws-cli gnupg

COPY ops/postgres-backup/backup.sh /usr/local/bin/hcbe-backup
RUN chmod 0555 /usr/local/bin/hcbe-backup

ENTRYPOINT ["/usr/local/bin/hcbe-backup"]

#!/usr/bin/env bash
set -Eeuo pipefail

PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

REPO_DIR="${REPO_DIR:-/etc/Cameo-mod}"
STATE_DIR="${STATE_DIR:-/var/lib/cameo-update}"
STAGING_PARENT="${STAGING_PARENT:-/var/tmp/cameo-update}"
REMOTE_NAME="${REMOTE_NAME:-origin}"
REMOTE_BRANCH="${REMOTE_BRANCH:-master}"
TAG_GLOB="${TAG_GLOB:-playtest-*}"
TAG_REGEX="${TAG_REGEX:-^playtest-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]$}"
CAMEO_USER="${CAMEO_USER:-cameo}"
CAMEO_GROUP="${CAMEO_GROUP:-cameo}"
BUILD_HOME="${BUILD_HOME:-$STATE_DIR/build-home}"
SERVICES=(
  cameo-dedicated.service
  cameo-dedicated-2.service
  cameo-dedicated-3.service
  cameo-dedicated-4.service
)

FORCE=0
if [[ "${1:-}" == "--force" ]]; then
  FORCE=1
elif [[ $# -gt 0 ]]; then
  echo "Usage: $0 [--force]" >&2
  exit 64
fi

log() {
  printf '[%(%Y-%m-%dT%H:%M:%S%z)T] %s\n' -1 "$*"
}

run_as_cameo() {
  cd /tmp
  sudo -u "$CAMEO_USER" env \
    HOME="$BUILD_HOME" \
    DOTNET_CLI_HOME="$BUILD_HOME" \
    NUGET_PACKAGES="$BUILD_HOME/.nuget/packages" \
    TMPDIR="$BUILD_HOME/tmp" \
    "$@"
}

if [[ ${EUID} -ne 0 ]]; then
  echo "cameo-update must run as root" >&2
  exit 77
fi

mkdir -p "$STATE_DIR" "$STAGING_PARENT"
mkdir -p "$BUILD_HOME/tmp" "$BUILD_HOME/.nuget/packages"
chown -R "$CAMEO_USER:$CAMEO_GROUP" "$STAGING_PARENT" "$BUILD_HOME"

exec 9>/run/cameo-update.lock
if ! flock -n 9; then
  log "another update is already running; exiting"
  exit 0
fi

if [[ ! -d "$REPO_DIR/.git" ]]; then
  log "missing git repository at $REPO_DIR"
  exit 1
fi

REMOTE_URL="$(run_as_cameo git -C "$REPO_DIR" remote get-url "$REMOTE_NAME")"
log "fetching $REMOTE_NAME/$REMOTE_BRANCH and tags from $REMOTE_URL"
run_as_cameo git -C "$REPO_DIR" fetch --prune --tags "$REMOTE_NAME" "+refs/heads/$REMOTE_BRANCH:refs/remotes/$REMOTE_NAME/$REMOTE_BRANCH"

LATEST_TAG="$(
  run_as_cameo git -C "$REPO_DIR" for-each-ref \
    --merged "$REMOTE_NAME/$REMOTE_BRANCH" \
    --sort=-v:refname \
    --format='%(refname:short)' \
    "refs/tags/$TAG_GLOB" \
    | awk -v pattern="$TAG_REGEX" '$0 ~ pattern { print; exit }'
)"

if [[ -z "$LATEST_TAG" ]]; then
  log "no tag matching $TAG_REGEX reachable from $REMOTE_NAME/$REMOTE_BRANCH"
  exit 1
fi

CURRENT_TAG=""
if [[ -f "$STATE_DIR/current-tag" ]]; then
  CURRENT_TAG="$(cat "$STATE_DIR/current-tag")"
else
  CURRENT_TAG="$(run_as_cameo git -C "$REPO_DIR" describe --exact-match --tags HEAD 2>/dev/null || true)"
fi

if [[ "$FORCE" -eq 0 && "$CURRENT_TAG" == "$LATEST_TAG" ]]; then
  log "already on $LATEST_TAG; nothing to deploy"
  exit 0
fi

log "preparing deployment: current=${CURRENT_TAG:-unknown}, latest=$LATEST_TAG"
STAGING="$STAGING_PARENT/build-$LATEST_TAG-$$"
BACKUP="$STAGING_PARENT/live-backup-$(date +%Y%m%d%H%M%S)"

cleanup() {
  if [[ -d "$STAGING" ]]; then
    run_as_cameo git -C "$REPO_DIR" worktree remove --force "$STAGING" >/dev/null 2>&1 || rm -rf -- "$STAGING"
  fi
}
trap cleanup EXIT

rm -rf -- "$STAGING"
log "creating local staging worktree at $STAGING"
run_as_cameo git -C "$REPO_DIR" worktree prune
run_as_cameo git -C "$REPO_DIR" worktree add --detach "$STAGING" "$LATEST_TAG"

log "building $LATEST_TAG in staging"
run_as_cameo make -C "$STAGING" all
log "stamping mod version as $LATEST_TAG"
run_as_cameo make -C "$STAGING" version VERSION="$LATEST_TAG"

log "stopping Cameo dedicated services"
systemctl stop "${SERVICES[@]}" || true

log "backing up current live tree to $BACKUP"
mkdir -p "$BACKUP"
rsync -a --delete --exclude='.git' "$REPO_DIR/" "$BACKUP/"

log "syncing staged build into $REPO_DIR"
run_as_cameo git -C "$REPO_DIR" reset --hard "$LATEST_TAG"
rsync -a --delete --exclude='.git' --chown="$CAMEO_USER:$CAMEO_GROUP" "$STAGING/" "$REPO_DIR/"

log "starting Cameo dedicated services"
systemctl start "${SERVICES[@]}"
sleep 10

FAILED=0
for service in "${SERVICES[@]}"; do
  if ! systemctl is-active --quiet "$service"; then
    log "$service is not active after deployment"
    FAILED=1
  fi
done

if [[ "$FAILED" -ne 0 ]]; then
  log "deployment failed health check; restoring previous live tree"
  systemctl stop "${SERVICES[@]}" || true
  rsync -a --delete --exclude='.git' --chown="$CAMEO_USER:$CAMEO_GROUP" "$BACKUP/" "$REPO_DIR/"
  systemctl start "${SERVICES[@]}" || true
  exit 1
fi

printf '%s\n' "$LATEST_TAG" > "$STATE_DIR/current-tag"
mapfile -t OLD_BACKUPS < <(find "$STAGING_PARENT" -mindepth 1 -maxdepth 1 -type d -name 'live-backup-*' | sort -r | tail -n +3)
for old_backup in "${OLD_BACKUPS[@]}"; do
  rm -rf -- "$old_backup"
done
log "deployed $LATEST_TAG successfully"

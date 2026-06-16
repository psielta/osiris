#!/usr/bin/env bash
set -euo pipefail

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
COMPOSE=(docker compose -f "$COMPOSE_FILE")

usage() {
  cat <<'EOF'
Usage: ./deploy.sh <command>

Commands:
  all       Pull origin/main, build, migrate and start Osiris (Web + API)
  web       Pull origin/main, build/update only the Web app after migrations
  api       Pull origin/main, migrate and build/update only the mobile API
  migrate   Run EF Core migrations against the production database
  status    Show compose status and resource summary
  logs [S]  Follow logs. Optional service name S.

Run from /opt/osiris on the production VPS.
EOF
}

require_prod_files() {
  test -f .env || { echo "Missing .env"; exit 1; }
  "${COMPOSE[@]}" config >/dev/null
}

pull_main() {
  echo ">>> Updating repository"
  git fetch origin main
  git checkout main
  git reset --hard origin/main
  git log --oneline -3
}

run_migrations() {
  require_prod_files
  echo ">>> Starting database"
  "${COMPOSE[@]}" up -d osiris-db
  echo ">>> Building migration image"
  "${COMPOSE[@]}" build osiris-migrate
  echo ">>> Applying EF Core migrations"
  "${COMPOSE[@]}" run --rm osiris-migrate
}

update_web() {
  require_prod_files
  echo ">>> Building Web image"
  "${COMPOSE[@]}" build osiris-web
  echo ">>> Recreating Web"
  "${COMPOSE[@]}" up -d osiris-web
}

update_api() {
  require_prod_files
  echo ">>> Building API image"
  "${COMPOSE[@]}" build osiris-api
  echo ">>> Recreating API"
  "${COMPOSE[@]}" up -d osiris-api
}

update_all() {
  run_migrations
  update_web
  update_api
}

status() {
  require_prod_files
  echo ">>> Compose status"
  "${COMPOSE[@]}" ps
  echo
  echo ">>> Container state"
  for cid in $("${COMPOSE[@]}" ps -q); do
    docker inspect -f '{{.Name}} restart={{.RestartCount}} state={{.State.Status}} health={{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$cid"
  done
  echo
  echo ">>> Disk"
  df -h /
  echo
  echo ">>> Docker disk"
  docker system df
  echo
  echo ">>> Memory"
  free -h
}

cmd="${1:-}"
case "$cmd" in
  all)
    pull_main
    update_all
    status
    ;;
  web)
    pull_main
    run_migrations
    update_web
    status
    ;;
  api)
    pull_main
    run_migrations
    update_api
    status
    ;;
  migrate)
    pull_main
    run_migrations
    status
    ;;
  status)
    status
    ;;
  logs)
    shift || true
    "${COMPOSE[@]}" logs -f --tail=200 "$@"
    ;;
  ""|-h|--help|help)
    usage
    ;;
  *)
    echo "Unknown command: $cmd"
    usage
    exit 2
    ;;
esac

#!/usr/bin/env bash
set -e
DB=/home/ubuntu/sentsoft-comtr/sent-pazar.db
UPLOAD_ROOT=/var/www/sentpazar/wwwroot/uploads
BACKUP_DIR=/var/backups/sentpazar/uploads

echo "Scanning ListingImages for missing files..."
mapfile -t files < <(sqlite3 "$DB" "SELECT DISTINCT FilePath FROM ListingImages WHERE FilePath IS NOT NULL AND TRIM(FilePath) != '';")
missing_count=0
missing_files=()
for fp in "${files[@]}"; do
  [ -z "$fp" ] && continue
  # normalize leading slash
  norm=${fp#"/"}
  if [ -f "$UPLOAD_ROOT/$norm" ]; then
    continue
  fi
  missing_files+=("$fp")
  missing_count=$((missing_count+1))
done

echo "Total missing files: $missing_count"
if [ $missing_count -eq 0 ]; then
  echo "Nothing to restore."
  exit 0
fi

# For each missing file, search backups (newest first) and extract first match
for fp in "${missing_files[@]}"; do
  echo "Checking: $fp"
  norm=${fp#"/"}
  found=0
  for archive in $(ls -1t "$BACKUP_DIR"/*.tar.gz 2>/dev/null); do
    if tar -tzf "$archive" | grep -x "\.?/?$norm" >/dev/null 2>&1; then
      echo " Found in $archive"
      tmpd=$(mktemp -d /tmp/sentpazar-restore.XXXXXX)
      tar -xzf "$archive" -C "$tmpd" -- "./$norm" || tar -xzf "$archive" -C "$tmpd" "$norm" || true
      if [ -f "$tmpd/$norm" ]; then
        mkdir -p "$(dirname "$UPLOAD_ROOT/$norm")"
        cp -a "$tmpd/$norm" "$UPLOAD_ROOT/$norm"
        chown ubuntu:ubuntu "$UPLOAD_ROOT/$norm" || true
        echo " Restored $norm"
        found=1
        rm -rf "$tmpd"
        break
      else
        rm -rf "$tmpd"
      fi
    fi
  done
  if [ $found -eq 0 ]; then
    echo " Not found in any backup: $fp"
  fi
done

echo "Restore run complete."

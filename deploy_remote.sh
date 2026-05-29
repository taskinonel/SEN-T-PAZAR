#!/bin/bash
set -e
echo "Reading ExecStart..."
systemctl show -p ExecStart sentpazar.service
execline=$(systemctl show -p ExecStart sentpazar.service | sed 's/ExecStart=//')
echo "Exec line: $execline"
dllpath=$(echo "$execline" | sed -E "s/.* (\/.*\.dll).*/\1/")
basedir=$(dirname "$dllpath")
echo "DLL: $dllpath"
echo "Base dir: $basedir"
if [ -z "$dllpath" ]; then
  echo "ERROR: Could not determine DLL path from ExecStart" >&2
  exit 2
fi
timestamp=$(date +%Y%m%d%H%M%S)
if [ -d "$basedir" ]; then
  echo "Backing up $basedir to ${basedir}.bak.$timestamp"
  sudo cp -a "$basedir" "${basedir}.bak.$timestamp"
fi
echo "Deploying files from /tmp/sentpazar-deploy to $basedir"
sudo rsync -a --delete /tmp/sentpazar-deploy/ "$basedir/"
owner=$(stat -c "%U:%G" "$basedir" || true)
if [ -n "$owner" ]; then
  echo "Restoring owner $owner on $basedir"
  sudo chown -R $owner "$basedir"
fi
echo "Restarting service"
sudo systemctl restart sentpazar.service
sudo systemctl status sentpazar.service --no-pager -n 50

#!/usr/bin/env bash
set -euo pipefail

APPSETTINGS=/var/www/sentpazar/appsettings.Production.json

if [ -f "$APPSETTINGS" ]; then
  echo APPSETTINGS_OK
else
  echo APPSETTINGS_MISSING
fi

if [ -f "$APPSETTINGS" ] && grep -q '"Authentication"' "$APPSETTINGS"; then
  echo AUTH_SECTION_PRESENT
else
  echo AUTH_SECTION_MISSING
fi

if [ -f "$APPSETTINGS" ] && grep -q '"Google"' "$APPSETTINGS"; then
  echo GOOGLE_SECTION_PRESENT
else
  echo GOOGLE_SECTION_MISSING
fi

if systemctl show -p Environment sentpazar.service | grep -q 'Authentication__Google__ClientId='; then
  echo ENV_CLIENTID_PRESENT
else
  echo ENV_CLIENTID_MISSING
fi

if systemctl show -p Environment sentpazar.service | grep -q 'Authentication__Google__ClientSecret='; then
  echo ENV_CLIENTSECRET_PRESENT
else
  echo ENV_CLIENTSECRET_MISSING
fi

if systemctl show -p Environment sentpazar.service | grep -q 'Authentication__Google__PublicOrigin='; then
  echo ENV_PUBLICORIGIN_PRESENT
else
  echo ENV_PUBLICORIGIN_MISSING
fi

if systemctl show -p Environment sentpazar.service | grep -q 'PUBLIC_ORIGIN='; then
  echo ENV_PUBLIC_ORIGIN_PRESENT
else
  echo ENV_PUBLIC_ORIGIN_MISSING
fi

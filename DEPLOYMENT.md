Deployment checklist

1. Set environment variables (see `.env.example`).
2. Ensure uploads directory exists and is writable by the app user.
3. Configure reverse proxy (nginx/systemd) to forward to Kestrel.
4. Use a secret store for `Jwt__Key` and other secrets in production.
5. Configure backups for DB and uploads.
6. Configure monitoring to call `/health` and alert on non-200.

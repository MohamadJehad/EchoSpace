# Backup / Restore & Crypto Key Disaster Procedures

This document summarizes operational procedures for recovering from data loss or
cryptographic key issues in EchoSpace. Adapt the commands/env paths to your
deployment (Azure, AWS, on-prem, etc.).

---

## 1. Database Backup & Restore

### 1.1 Automated Backups

1. **Enable automated backups** on the database server (e.g. Azure SQL:
   `BackupRetentionDays`, `Geo-Redundant` options).
2. **Verify retention** meets business needs (default Azure = 7 days, can be
   increased to 35+).
3. **Schedule integrity checks**: once per sprint, restore a backup to a
   staging instance to validate restore point.

### 1.2 On-demand Snapshot

- Azure SQL example:
  ```powershell
  az sql db export \
    --resource-group <rg-name> \
    --server <server-name> \
    --name EchoSpaceDB \
    --storage-key-type StorageAccessKey \
    --storage-key <storage-account-key> \
    --storage-uri https://<storage>.blob.core.windows.net/backups/EchoSpaceDB-$(Get-Date -Format yyyyMMdd-HHmmss).bacpac
  ```

### 1.3 Restoration Procedure

1. **Identify restore point** (timestamp) from backup catalog.
2. **Restore to new instance** first, e.g. `EchoSpaceDB-Restore-<timestamp>`.
3. **Run smoke tests** against the restore:
   - `dotnet ef database update` (should be no-op)
   - Login via staging frontend
4. **Swap connection string** in `appsettings.Production.json` or deployment
   pipeline to point to restored DB.
5. **Archive incident notes**: document root cause + prevention plan.

### 1.4 RPO / RTO Targets

- **RPO (Recovery Point Objective)**: <= 15 minutes (requires frequent
  incremental backups or change tracking).
- **RTO (Recovery Time Objective)**: < 1 hour (depends on DB size; adjust
  targets to business SLA).

---

## 2. Application Secrets & Crypto Keys

### 2.1 Inventory

| Secret / Key                    | Location / Source              |
|---------------------------------|--------------------------------|
| JWT signing key                 | `appsettings.json` or Key Vault |
| SMTP password                   | `EmailSettings:Password`       |
| Azure Storage connection string | `AzureStorage` section         |
| Google OAuth client secret      | `Google:ClientSecret`          |

**Action**: move secrets from plaintext config to a managed store (Azure Key
Vault, AWS Secrets Manager, etc.) for production environments.

### 2.2 Rotation Plan

1. **JWT Signing Key**
   - Store in Key Vault as `EchoSpace-JWT-Key`.
   - Schedule rotation every 90 days.
   - Maintain overlapping keys via JWT `IssuerSigningKeyResolver` to allow
     active tokens to remain valid during cutover window.

2. **SMTP Password & OAuth Secrets**
   - Regenerate via provider portal (Gmail, Google Cloud Console).
   - Update Key Vault entry.
   - Redeploy app or trigger config refresh.

3. **Azure Storage Keys**
   - Use `az storage account regenerate-key`.
   - Update storage connection string wherever used (backend, CI).

Document each rotation (date, reason, operator) in an internal runbook.

### 2.3 Emergency Key Loss

1. **JWT Signing Key Compromised**
   - Immediately rotate key (invalidate old JWTs).
   - Force log-out: empty refresh token store / revoke sessions.
   - Notify users of the incident.

2. **Key Vault / Secret Manager Unavailable**
   - Maintain off-site encrypted backup of secrets (stored in company password
     manager or sealed envelope in safe).
   - Procedure:
     1. Retrieve backup secrets.
     2. Provision temporary secret store (alternate region/instance).
     3. Point applications to temporary store until primary restored.

3. **Email Provider Login Locked**
   - Use recovery channels configured on provider account.
   - Update new password in secret store once restored.

---

## 3. Disaster Recovery Playbook

1. **Incident Detection**
   - Monitoring alerts (DB unreachable, authentication failures, etc.).
   - Security triggers (unexpected key usage, login anomalies).

2. **Initial Response**
   - Notify on-call/incident Slack channel.
   - Freeze deployments until recovery complete.

3. **Recovery Steps**
   - Database: follow Section 1.3.
   - Secrets: rotate per Section 2.2.
   - Infrastructure: rebuild via IaC or documented manual steps.

4. **Post-Incident**
   - Run retrospective within 48 hours.
   - Update this document with lessons learned.
   - Automate gaps (e.g., add missing monitors, replicate backups).

---

## 4. Testing the Procedures

- **Quarterly**: Perform a game-day drill restoring DB to staging.
- **Semi-Annually**: Rotate a non-critical secret end-to-end to ensure process
  works (e.g., test environment JWT key).
- **Annually**: Full disaster recovery simulation (database + secrets + app
  redeploy).

Track completion in engineering operations calendar.

---

_Last updated: 2025-01-17_


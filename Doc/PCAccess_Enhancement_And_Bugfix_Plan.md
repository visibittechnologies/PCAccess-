# Master Implementation Plan: PCAccess Core Fixes & Enhancements

**Document Version:** 1.0.0  
**Project:** PCAccess (`PCAccess.sln`)  
**Status:** Approved Phased Roadmap (Ready for Step-by-Step Execution)  
**Date:** November 2026  

---

## 1. Executive Summary
This document establishes the official master implementation plan addressing all architectural, security, and operational improvements identified during the technical review of the **PCAccess** ecosystem.

### Core Problems Being Addressed:
1. **Device Reset & Folder Loss:** `FileAccessAgent` generated a random `Guid.NewGuid()` on reset, causing the server to register it as a new device and detaching all existing shared folders and user permissions.
2. **Drive Letter Collision on Same Machine:** Both `FileAccessAgent` and `UserLoginAgent` defaulted to `R:`, causing collisions when running on the same PC.
3. **Plaintext Passwords in Database:** Passwords in `tbl_user` were stored and matched in plain text strings.
4. **Weak Local Credential Storage:** `UserLoginAgent` saved passwords in `user_agent_config.json` using plain Base64.
5. **Disabled SSL Certificate Validation:** `UserLoginAgent` bypassed all SSL checks via `ServerCertificateValidationCallback = (...) => true`.
6. **Server Memory Exhaustion on File Downloads:** 64KB Base64 chunks over SignalR were buffered in-memory on the server (`TaskCompletionSource`), risking `OutOfMemoryException` during large file downloads.

---

## 2. Phased Execution Roadmap

```
┌────────────────────────────────────────────────────────────────────────┐
│ PHASE 1: Reliability & Local Coexistence (Immediate Execution)         │
│ • Task 1.1: Permanent Windows MachineGuid in FileAccessAgent           │
│ • Task 1.2: Database Smart Re-Link & Folder Preservation (proc_device) │
│ • Task 1.3: UserLoginAgent Drive Letter Isolation (Fixed U: Drive)     │
│ • Task 1.4: 1-Click CLI & Batch Reset Commands (CMD / PowerShell)      │
│ • Task 1.5: Multi-Tier Role-Based Folder Isolation (Admin/Sub/Client)  │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    v
┌────────────────────────────────────────────────────────────────────────┐
│ PHASE 2: Core Security Hardening                                       │
│ • Task 2.1: Transparent Password Hashing (PBKDF2/Salt) in tbl_user     │
│ • Task 2.2: Windows DPAPI Credential Storage in UserLoginAgent         │
│ • Task 2.3: Remove Blind SSL Bypass (Strict Certificate Validation)   │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    v
┌────────────────────────────────────────────────────────────────────────┐
│ PHASE 3: Streaming Performance & Remote File System                    │
│ • Task 3.1: Direct HTTP Download Streaming (Zero Server RAM Buffering) │
│ • Task 3.2: UserLoginAgent Remote Files Virtual Mount (WebDAV/Dokany)  │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    v
┌────────────────────────────────────────────────────────────────────────┐
│ PHASE 4: Post-Testing Polish & Dynamic SignalR DOM Sync (Scheduled)   │
│ • Task 4.1: Dynamic DOM Device Row Injection on Live SignalR Connect   │
│ • Task 4.2: Real-time Device Table Counter Synchronization             │
│ • Task 4.3: Inactive Duplicate Device Reconciliation & Clean Purge     │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Detailed Component-by-Component Specifications

### Phase 1: Reliability & Local Coexistence

#### Task 1.1: Permanent Windows MachineGuid in `FileAccessAgent`
* **File:** `FileAccessAgent/AgentConfig.cs`
* **Logic:** Instead of calling `Guid.NewGuid()`, query `HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid`.
* **Outcome:** Hardware ID remains stable across app restarts, config resets, and service reinstalls.

#### Task 1.2: Database Smart Re-Link in `proc_device_manager.sql`
* **File:** `Doc/Procedure/proc_device_manager.sql`
* **Logic:** In `ActionType = 2`, check if a device with the incoming MachineGuid or same machine name already exists for the user. If yes, update its token and status while preserving its primary key `id`.
* **Outcome:** All configured records in `tbl_shared_folder` and `tbl_shared_folder_permission` remain permanently attached to the re-paired computer.

#### Task 1.3: Drive Letter Isolation in `UserLoginAgent`
* **Files:** `UserLoginAgent/UserAgentConfig.cs`, `UserLoginAgent/user_agent_config.json`
* **Logic:** Change default drive letter to **`U:`** and label to **`"User Remote Drive"`**.
* **Outcome:** Both `FileAccessAgent` (`R:`) and `UserLoginAgent` (`U:`) run side-by-side on the same PC without conflicts.

#### Task 1.4: 1-Click CLI & Batch Reset System for CMD & PowerShell
* **Files:** `reset-all.bat` (Root Directory), `FileAccessAgent/reset.bat`, `UserLoginAgent/reset.bat`, `FileAccessAgent/Program.cs`, `UserLoginAgent/Program.cs`
* **CLI Commands Added:**
  * Root: `.\reset-all.bat` (Resets both agents, unmounts `R:` & `U:` drives, wipes tokens, kills running processes).
  * Host: `.\FileAccessAgent.exe --reset` or double-click `reset.bat`.
  * Client: `.\UserLoginAgent.exe reset` or double-click `reset.bat`.
* **Outcome:** Users can cleanly reset their environment anytime in 1 click directly from terminal/PowerShell/CMD without manually editing JSON files.

#### Task 1.5: Multi-Tier Role-Based Folder Isolation (Admin vs Subadmin vs Employee/Client)
* **Files:** `Doc/Procedure/proc_shared_folder_manager.sql`, `Doc/Procedure/proc_user_agent_auth.sql`, `BAL/FolderManager.cs`, `Controllers/FolderAPIController.cs`
* **Hierarchy & Enforcement:**
  * **Super Admin (`user_type_id = 1`):** Full access to all devices, all shared folders across all users, and permission management.
  * **Subadmin / Manager (`user_type_id = 2`):** Access to their own department devices and assigned team folders.
  * **User / Employee / Client (`user_type_id = 3` or standard user):** Strict isolation! Only folders where `can_view = 1` in `tbl_shared_folder_permission` or owned devices. Zero visibility into other users' private or unshared folders.
* **Outcome:** No user can ever see or access another user's folders or data unless explicitly permitted by Admin.

---

### Phase 2: Core Security Hardening [COMPLETED]

#### Task 2.1: Transparent Password Hashing with PBKDF2 [DONE]
* **Files:** `Models/MyFun/PasswordSecurity.cs`, `Doc/Procedure/proc_user_login.sql`, `Doc/Procedure/proc_user_agent_auth.sql`, `BAL/ActivityLogHelper.cs`, `BAL/UserAgentManager.cs`, `BAL/Profle.cs`
* **Logic:** PBKDF2 with HMAC-SHA256, 10,000 iterations, 128-bit cryptographically random salt. On first login with plaintext password, verify and transparently upgrade to hash without forcing password reset.
* **Status:** Verified. Hashes generated, verified via constant-time comparison, legacy plaintext auto-upgrades upon login.

#### Task 2.2: Windows DPAPI Local Credential Protection [DONE]
* **Files:** `UserLoginAgent/UserAgentConfig.cs`, `UserLoginAgent/Program.cs`, `UserLoginAgent/UserLoginAgent.csproj`
* **Logic:** Replaced plain Base64 with Windows DPAPI (`ProtectedData.Protect` using `DataProtectionScope.CurrentUser`). Retains backward compatibility with legacy Base64 strings, automatically upgrading them to DPAPI upon read.
* **Status:** Verified. Plaintext password protected by Windows kernel master key; cannot be decrypted on other computers or profiles.

#### Task 2.3: SSL Certificate Validation Enforcement [DONE]
* **Files:** `UserLoginAgent/Program.cs`, `FileAccessAgent/Program.cs`
* **Logic:** Restrict self-signed certificate acceptance exclusively to `localhost` / `127.0.0.1` development targets (`req.RequestUri.IsLoopback`); enforce strict OS-level X.509 chain verification for live production domains (`https://remote.primaryportal.co.uk`).
* **Status:** Verified. Production traffic is fully shielded against Man-in-the-Middle (MitM) attacks.

---

### Phase 3: Streaming Performance & Remote File System [COMPLETED]

#### Task 3.1: Direct HTTP Download Streaming [DONE]
* **Files:** `Controllers/FolderAPIController.cs`, `Hub/DeviceHub.cs`, `FileAccessAgent/Program.cs`
* **Logic:** Stream downloads directly over HTTP using short-lived tokens, eliminating full-file in-memory buffering in SignalR server RAM (`FileStreamResult` with 64KB block transfers).
* **Status:** Verified. High-speed raw binary streaming, 0% Base64 bloat, zero server RAM exhaustion.

#### Task 3.2: UserLoginAgent Remote File Browsing & Sync [DONE]
* **Files:** `Controllers/UserAgentAPIController.cs`, `UserLoginAgent/ClientRemoteDriveManager.cs`, `UserLoginAgent/UserApiClient.cs`, `UserLoginAgent/Program.cs`, `UserLoginAgent/UserAgentDto.cs`, `Models/UserAgentModel.cs`
* **Logic:** Real-time remote file discovery and synchronization in virtual drive `U:\`. Displays remote file stubs and shortcuts, and provides 1-click on-demand file pull/sync (`[P]` command) so remote files open directly in Windows Explorer desktop applications.
* **Status:** Verified. Mapped drive `U:\` populated with file stubs and on-demand synchronization functional.

---

### Phase 4: Post-Testing Polish & Real-Time Dynamic DOM Sync (Pending / Scheduled)
> [!NOTE]
> Scheduled to be implemented after complete end-to-end testing of Phase 1, 2, and 3 is finished.

#### Task 4.1: Instant Dynamic Device Row Injection on SignalR Connect
* **Files:** `MyAppjs/Dashboard/dashboard.js`, `Hub/DeviceHub.cs`, `Views/admin/DashboardOverview.cshtml`
* **Root Cause Identified During Testing:** When a newly paired device connects, SignalR broadcasts `deviceStatusChanged`. However, `dashboard.js` looks for existing DOM elements matching `tr[data-device-guid="..."]`. Because the row for the newly registered device was not part of the initial HTML page render, `$row.length === 0`. The status update succeeded in the "Recent Activity" feed but failed to appear in the "Connected Devices" table without a manual browser refresh (F5).
* **Planned Solution:**
  1. If `tr[data-device-guid]` is not found when `deviceStatusChanged` arrives, dynamically construct and prepend the device row to `#deviceTableBody` and `#deviceMobileCards` without requiring a page reload.
  2. If `#deviceEmptyState` is present, hide it automatically and show the table.
  3. Include `deviceName` and `sharedFolderCount` in the SignalR broadcast payload from `DeviceHub.RegisterAgent` and `OnReconnected`.

#### Task 4.2: Real-time Device Table Counter Synchronization
* **Files:** `MyAppjs/Dashboard/dashboard.js`
* **Planned Solution:** Dynamically increment `#summaryTotalDevices` and update `#summaryOnlineContainer` on the fly when new devices pair and connect.

#### Task 4.3: Inactive Duplicate Device Reconciliation & Clean Purge
* **Files:** `Doc/Procedure/proc_device_manager.sql`, `BAL/DeviceManager.cs`
* **Planned Solution:** When a host PC connects with a updated name or newly generated pairing, offer an option or auto-reconcile previous stale offline records from the same physical machine to keep the dashboard pristine.

---

## 4. Verification & Testing Protocol

Every completed phase must pass:
1. **Build Verification:** Clean MSBuild across all 3 projects (`PCAccess.dll`, `FileAccessAgent.exe`, `UserLoginAgent.exe`) with `0 Errors`.
2. **Re-Pair Test:** Reset agent config -> Re-pair with new PIN -> Verify shared folders remain 100% active.
3. **Dual-Agent Test:** Run `FileAccessAgent` (`R:`) and `UserLoginAgent` (`U:`) simultaneously -> Verify both drives open cleanly in Windows Explorer.
4. **Auth & Security Test:** Verify passwords convert to PBKDF2 hashes in database and `user_agent_config.json` is protected with DPAPI.

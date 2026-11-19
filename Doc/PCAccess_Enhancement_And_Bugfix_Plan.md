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

### Phase 2: Core Security Hardening

#### Task 2.1: Transparent Password Hashing with PBKDF2
* **Files:** `Models/MyFun/PasswordSecurity.cs`, `Doc/Procedure/proc_user_login.sql`, `Doc/Procedure/proc_user_agent_auth.sql`
* **Logic:** PBKDF2 with HMAC-SHA256, 10,000 iterations, 128-bit cryptographically random salt. On first login with plaintext password, verify and transparently upgrade to hash without forcing password reset.

#### Task 2.2: Windows DPAPI Local Credential Protection
* **File:** `UserLoginAgent/UserAgentConfig.cs`
* **Logic:** Replace Base64 with `ProtectedData.Protect` (CurrentUser scope). Stored credentials cannot be decrypted outside the user's Windows login session.

#### Task 2.3: SSL Certificate Validation Enforcement
* **Files:** `UserLoginAgent/Program.cs`, `FileAccessAgent/Program.cs`
* **Logic:** Restrict self-signed certificate acceptance exclusively to `localhost` / `127.0.0.1` development targets; enforce strict OS-level X.509 chain verification for live production domains.

---

### Phase 3: Streaming Performance & Remote File System

#### Task 3.1: Direct HTTP Download Streaming
* **Files:** `Controllers/FolderAPIController.cs`, `Hub/DeviceHub.cs`, `FileAccessAgent/Program.cs`
* **Logic:** Stream downloads directly over HTTP using short-lived tokens, eliminating full-file in-memory buffering in SignalR server RAM.

#### Task 3.2: UserLoginAgent Remote File Browsing
* **File:** `UserLoginAgent/ClientRemoteDriveManager.cs`
* **Logic:** Virtual filesystem / WebDAV bridge allowing remote files to open directly in Windows Explorer applications on client PCs.

---

## 4. Verification & Testing Protocol

Every completed phase must pass:
1. **Build Verification:** Clean MSBuild across all 3 projects (`PCAccess.dll`, `FileAccessAgent.exe`, `UserLoginAgent.exe`) with `0 Errors`.
2. **Re-Pair Test:** Reset agent config -> Re-pair with new PIN -> Verify shared folders remain 100% active.
3. **Dual-Agent Test:** Run `FileAccessAgent` (`R:`) and `UserLoginAgent` (`U:`) simultaneously -> Verify both drives open cleanly in Windows Explorer.
4. **Auth & Security Test:** Verify passwords convert to PBKDF2 hashes in database and `user_agent_config.json` is protected with DPAPI.

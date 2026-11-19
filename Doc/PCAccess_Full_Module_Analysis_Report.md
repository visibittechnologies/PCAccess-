# PCAccess — Comprehensive Technical Analysis, Security Audit & Operational Report

**Document Version:** 1.0.0  
**Project:** PCAccess (Remote PC Access & Virtual Drive Ecosystem)  
**Target Solution:** `PCAccess.sln` (.NET Framework 4.8 / ASP.NET MVC 5 / C# Desktop Agents)  
**Report Type:** Honest Technical Architecture, Security Audit & Operational Review  
**Date:** November 2026  

---

## 1. Executive Summary & Honest Reality Check

PCAccess is a distributed remote file access and management system designed to connect local on-premise Windows PCs with remote web dashboards and desktop clients without requiring public static IPs or inbound port forwarding.

### ⚠️ Honest Reality & Platform Support Verdict
* **Does it support all Operating Systems (Cross-Platform)? NO.**
  * **Central Web Portal:** Built on **ASP.NET MVC 5 (.NET Framework 4.8)**. It is tied to IIS / Windows Server and **cannot** run on Linux or macOS.
  * **FileAccessAgent (Host):** Targets **.NET Framework 4.8 AnyCPU** and relies strictly on Win32 Windows APIs (`DefineDosDevice`, `subst`, `mklink /J` NTFS directory junctions, Windows ServiceBase, Windows Registry `DriveIcons`). It **only runs on Windows (Windows 10/11/Server)**.
  * **UserLoginAgent (Client):** Targets **.NET Framework 4.8** and mounts Windows drive letters (`R:`) via Win32 kernel calls. It **only runs on Windows**.
  * **Web Browser Dashboard:** The only cross-platform component. Users on Mac, Linux, or Android/iOS can access the web portal in a browser to view and download files.
* **Is it Production-Ready? NOT YET.**
  * The architecture and networking concept (Outbound WSS/SignalR) are solid, but there are **critical security vulnerabilities** (plaintext database passwords, disabled SSL validation in agents, base64 credential storage) and **scalability bottlenecks** (RAM exhaustion during file downloads) that must be addressed before enterprise deployment.

---

## 2. High-Level Architecture & How It Works

PCAccess operates on a **3-Tier Zero-Trust Outbound Architecture**:

```
 ┌─────────────────────────────────────────────────────────────┐
 │                HOST MACHINE (Office / Home PC)              │
 │  FileAccessAgent (.NET 4.8 Console / Windows Service)       │
 │  • Physical Folders: C:\Projects, D:\Docs                   │
 │  • Local Virtual Drive R:\ (NTFS Junctions)                 │
 └──────────────────────────────┬──────────────────────────────┘
                                │ Outbound WSS / HTTPS (Port 443)
                                │ [No Port Forwarding Required]
                                v
 ┌─────────────────────────────────────────────────────────────┐
 │               CENTRAL SERVER (IIS / Windows Server)         │
 │  • SignalR DeviceHub (/signalr)                             │
 │  • REST API Controllers (DeviceAPI, FolderAPI, UserAgentAPI)│
 │  • SQL Server (PCAccess_DB - Stored Procedures)             │
 └──────────────────────────────▲──────────────────────────────┘
                                │
        ┌───────────────────────┴───────────────────────┐
        │ HTTPS Web Session                             │ REST API (JSON)
        v                                               v
 ┌──────────────────────────────┐              ┌──────────────────────────────┐
 │    WEB CLIENT (Browser)      │              │  CLIENT PC (Employee Laptop) │
 │  • Admin Dashboard Overview  │              │  UserLoginAgent (.NET 4.8)   │
 │  • Live Online/Offline pulse │              │  • Login via Username/Pass   │
 │  • Web File Browser UI       │              │  • Mounts Virtual Drive R:\  │
 └──────────────────────────────┘              └──────────────────────────────┘
```

### End-to-End Operational Workflow:
1. **Host Pairing (One-Time Handshake):**
   - The user opens the Web Portal (`/Admin/DashboardOverview`) and clicks **"Connect PC"**, which calls `/DeviceAPI/GeneratePairingCode` to generate a temporary 6-digit code (valid for 10 minutes).
   - On the host PC, the user runs `FileAccessAgent.exe` (or `run.bat`) and inputs the Server URL and 6-digit code.
   - The agent calls `/DeviceAPI/Register`. The server creates a permanent record in `tbl_device`, returns a permanent `device_guid` and secret `device_token`, which are saved locally in `agent_config.json`.
2. **Persistent Outbound Real-Time Connection:**
   - `FileAccessAgent` connects via SignalR (`Microsoft.AspNet.SignalR.Client`) to `/signalr` on the server.
   - On connect, `DeviceHub.RegisterAgent()` validates the token, sets the device status to `'online'` in SQL Server, and broadcasts `deviceStatusChanged(online)` to the owner's dashboard group.
   - Every 30 seconds, the agent sends a lightweight heartbeat (`SendHeartbeat`).
   - If the agent is terminated or loses connection, SignalR's `OnDisconnected` triggers immediately on the server, updating SQL Server to `'offline'` and pushing a real-time red status to the web browser.
3. **Shared Folder Registration & Strict Path Verification:**
   - Admin adds a folder on the web portal (e.g. `C:\Accounts`).
   - The server **does not trust** the browser input. It dispatches a `validateFolder` command through SignalR to `FileAccessAgent`.
   - The agent checks path traversal, checks `Directory.Exists()`, and tests read permissions on physical disk before sending `FolderValidationResult` back to unlock the "Save" button.
4. **File Browsing & Directory Enumeration:**
   - When a user browses files in the Web UI (`/Admin/FileBrowser`), the browser calls `/FolderAPI/GetDirectoryContents`.
   - `DeviceHub` creates an asynchronous `TaskCompletionSource` with a 10-second timeout and calls `readDirectory` on the host agent.
   - The host agent enumerates files and folders (skipping system/hidden files, resolving file icons and formatted sizes, capped at 500 items) and returns the JSON payload back to the browser.
5. **File Downloading & Uploading:**
   - **Download:** The browser calls `/FolderAPI/DownloadFile`. Host agent reads the local file in 32KB/64KB chunks and sends them via SignalR Base64 chunks to the server, which reassembles the byte array in memory and streams it to the user.
   - **Upload:** The browser posts a file to `/FolderAPI/UploadFile`. The server streams it into a temporary staging file (`App_Data/TempUploads`) and issues a one-time token. It notifies the host agent via SignalR, and the host agent directly streams the file from the server via HTTP (`/FolderAPI/GetUploadStream`) into its destination directory with conflict auto-renaming.
6. **Virtual Drive R: Mounting:**
   - **On Host PC:** `RemoteDriveManager` creates a local staging folder (`%LOCALAPPDATA%\PCAccess\RemoteDriveRoot`), generates NTFS directory junctions (`mklink /J`) for each shared folder, maps the drive letter using Win32 `subst` / `DefineDosDevice`, and broadcasts `SHChangeNotify` to Windows Explorer.

---

## 3. Core System Benefits

1. **No Port Forwarding / NAT Traversal Issues:**
   Because `FileAccessAgent` makes an **outbound** connection to the server over port 443 (HTTPS/WSS), it works behind strict corporate firewalls, NAT routers, and home broadband without public IP addresses or router reconfiguration.
2. **Zero Inbound Attack Surface on Local Machine:**
   The host computer does not open port 445 (SMB) or port 3389 (RDP) to the public internet, completely eliminating automated brute-force attacks against Windows file-sharing or Remote Desktop.
3. **Native Windows Explorer Virtual Drive (`R:`):**
   Users do not have to download files through a web browser; they can access folders as if they were a local hard drive or USB thumb drive under "This PC".
4. **Zero Server Storage Overhead (Metadata-Only Architecture):**
   The central web server does not host, duplicate, or store enterprise files. It only maintains metadata and coordinates real-time streaming, drastically reducing server hosting storage costs.
5. **Fine-Grained Role-Based Access Control (RBAC):**
   Permissions (`can_view`, `can_download`, `can_upload`, `can_delete`) are enforced per-user per-folder inside SQL Server stored procedures before any file operation is permitted.
6. **Instant Disconnection Detection:**
   Unlike traditional polling architectures, SignalR provides sub-second event notification when a workstation goes offline.

---

## 4. Deep Security Audit (Honest Vulnerability Assessment)

| Severity | Vulnerability / Issue | Impact | Location in Code | Recommended Fix |
|:---|:---|:---|:---|:---|
| 🚨 **CRITICAL** | **Plaintext Passwords in Database** | If SQL Server is compromised, all user passwords are leaked instantly. | `proc_user_login.sql`, `proc_user_agent_auth.sql` (`WHERE [password] = @password`) | Migrate to **PBKDF2 / BCrypt** with individual cryptographic salt per user. |
| 🚨 **CRITICAL** | **Disabled SSL Certificate Validation** | Makes desktop agents completely vulnerable to Man-In-The-Middle (MITM) attacks on public Wi-Fi. | `UserLoginAgent/Program.cs` line 32 (`ServerCertificateValidationCallback = (...) => true`) | Remove the bypass; enforce strict X.509 certificate validation. |
| ⚠️ **HIGH** | **In-Memory Full-File Buffering on Download** | When downloading large files (up to 500MB), the server buffers all 64KB Base64 SignalR chunks into RAM before serving HTTP response. Concurrent downloads will cause IIS `OutOfMemoryException` (DoS). | `DeviceHub.cs` (`_pendingFileTransfers`), `FolderAPIController.cs` (`DownloadFile`) | Implement direct HTTP streaming / Range chunk streaming directly from Agent to Client without full-RAM assembly. |
| ⚠️ **HIGH** | **Weak Credential Storage on Client Machine** | "Remember credentials" saves the password using plain Base64 encoding in `user_agent_config.json`. Base64 is not encryption; anyone with access to the file can decode it in milliseconds. | `UserLoginAgent/Program.cs` (`SavedPasswordBase64 = Convert.ToBase64String(...)`) | Use Windows DPAPI (`ProtectedData.Protect`) to encrypt credentials with machine/user scope. |
| ⚠️ **MEDIUM** | **Single Virtual Drive Letter Conflict (`R:`)** | If the host machine or client machine already has a physical or mapped `R:` drive, mounting will collide or fail. | `RemoteDriveManager.cs`, `ClientRemoteDriveManager.cs` | Add dynamic unused drive letter fallback detection (`Z:`, `Y:`, `X:` down to `E:`). |
| ⚠️ **MEDIUM** | **Legacy DES Encryption for Auth Cookie** | Single DES uses a 56-bit key which is cryptographically broken and obsolete. | `CommanUtilities.cs` (`DESEncrypt`) | Upgrade to AES-256-GCM or ASP.NET Identity DataProtectionProvider. |

---

## 5. What Can Be Improved? (Strategic Roadmap)

### Immediate Improvements (v1.1):
1. **Password Hashing:** Upgrade `tbl_user` to store BCrypt / PBKDF2 hashes.
2. **Windows DPAPI Integration:** Replace `SavedPasswordBase64` with `ProtectedData.Protect` in `UserLoginAgent`.
3. **Direct HTTP Download Streaming:** Refactor file downloads to use an HTTP streaming tunnel (similar to how file upload currently uses `/FolderAPI/GetUploadStream`) rather than buffering hundreds of megabytes in SignalR memory.
4. **Dynamic Drive Letter Allocation:** Automatically scan available drive letters (`GetLogicalDrives`) if `R:` is already in use.

### Medium-Term Enhancements (v1.2):
5. **True Remote File System for UserLoginAgent:**
   - *Current Limitation:* In `UserLoginAgent`, if an employee connects to a folder hosted on a *different* remote PC, the agent writes a metadata `README_Folder_Details.txt` because NTFS junctions only work for local disks.
   - *Enhancement:* Integrate a lightweight WebDAV client or Dokany/WinFsp virtual filesystem driver so that clicking any remote file in Explorer streams it on demand!
6. **Cross-Platform Host Agent (.NET 8):**
   - Port `FileAccessAgent` to .NET 8 so it can run as a systemd daemon on Linux (Ubuntu, Debian, CentOS) and launchd on macOS.
7. **End-to-End File Encryption (E2EE):**
   - Encrypt file chunks on the host machine using AES-GCM before transmission so that even server administrators cannot view file contents.

---

## 6. Comprehensive End-to-End Testing Guide

### Prerequisites
- Visual Studio 2022 with .NET Framework 4.8 runtime and IIS Express.
- SQL Server LocalDB (`(localdb)\VSDigiCRM2025` or SQL Server Express) with `PCAccess_DB` database initialized.
- Admin credentials: `admin` / `123`.

---

### Test Scenario 1: Web Dashboard & Real-Time Device Pairing
1. **Launch Web App:**
   - Run `PCAccess` in Visual Studio (`Ctrl+F5` on IIS Express: `https://localhost:44355/`).
   - Navigate to `/Account/Login`, log in as `admin` / `123`, and land on `/Admin/DashboardOverview`.
2. **Generate Pairing Code:**
   - Click **"Connect PC"** in the top navigation bar.
   - A modal appears showing a 6-digit PIN (e.g. `849-210`) with a 10-minute countdown. Click **"Copy Code"**.
3. **Launch Host Agent:**
   - Open PowerShell in `E:\Git\PCAccess-\FileAccessAgent\`.
   - Run `.\FileAccessAgent.exe` (or double-click `run.bat`).
   - Enter Server URL (`https://localhost:44355`), Device Name (e.g. `Workstation-1`), and paste the 6-digit PIN.
4. **Verify Real-Time Transition:**
   - Look at the agent console: Observe `[STATUS: ONLINE]` and `Device paired successfully`.
   - Look at the Web Dashboard **without refreshing the page**: The device row must instantly display a vibrant green **Online** status badge with an animated radar pulse (`animate-ping`).

---

### Test Scenario 2: Heartbeat & Instant Disconnect Detection
1. **Heartbeat Check:**
   - Leave `FileAccessAgent` running. Observe the console every 30 seconds: `[HEARTBEAT] Ping sent -> OK`.
2. **Simulate Crash / Shutdown:**
   - In the `FileAccessAgent` console, press `Ctrl+C`.
3. **Verify Web Reaction:**
   - Within 1–2 seconds, the Web Dashboard device badge turns red (**Offline**), and a SweetAlert toast notification appears: `⚠️ Workstation-1 is now Offline`.
   - The Recent Activity list prepends a disconnection event.

---

### Test Scenario 3: Folder Path Security & Traversal Attack Testing
1. **Valid Folder Registration:**
   - Open `/Admin/DeviceFolders?deviceGuid=<guid>`.
   - Click **"Add Shared Folder"**.
   - Enter a valid path (e.g. `C:\Users\sonuk\Documents`). Click **"Validate Path"**.
   - The Host Agent verifies disk existence, and the modal shows a green checkmark: `Folder verified and accessible`. The "Save" button is unlocked.
2. **Path Traversal Attack Test (Negative Test):**
   - Enter `C:\Projects\..\..\Windows\System32`. Click **"Validate Path"**.
   - Verify: The Agent rejects the request immediately with `Path traversal sequences ('..') are not allowed`. The Save button remains disabled.
3. **Non-Existent Path Test:**
   - Enter `C:\FolderDoesNotExist999`. Click **"Validate Path"**.
   - Verify: The Agent returns `Folder does not exist on this PC`.

---

### Test Scenario 4: Remote File Browsing & Chunked Download
1. **Open File Browser:**
   - In `/Admin/DeviceFolders`, click the green **"Browse"** button next to an active folder.
   - Land on `/Admin/FileBrowser?folderGuid=<guid>`.
2. **Navigation & Breadcrumbs:**
   - Click into any subfolder. Verify breadcrumb updates (`Dashboard / PC / Folder / Subfolder`) and browser back/forward buttons work cleanly.
3. **File Download:**
   - Click **"Download"** on a test file (e.g., a 10MB PDF or image).
   - Verify: The Host Agent logs `[FILE:REQUEST]` and streams 64KB chunks; the browser downloads the file with the exact original filename, size, and MD5 hash intact.

---

### Test Scenario 5: Remote File Upload & Duplicate Resolution
1. **Upload File:**
   - In `/Admin/FileBrowser`, click **"Upload File"**.
   - Drag and drop a test file (e.g., `test_document.pdf`).
   - Leave conflict resolution as "Auto-rename". Click **"Upload"**.
2. **Verify Stream:**
   - Web server stages file in `App_Data/TempUploads`, issues token, and agent streams it via HTTP.
   - The table refreshes, displaying the newly uploaded file.
3. **Duplicate Collision Test:**
   - Upload the exact same file again without deleting the original.
   - Verify: The agent auto-renames the new file to `test_document (1).pdf` without overwriting the original.

---

### Test Scenario 6: Virtual Drive `R:` Mounting on Host PC
1. **Inspect Host Explorer:**
   - With `FileAccessAgent` connected, open **"This PC"** in Windows Explorer.
   - Verify: Drive letter **`R:`** appears with the label **"Remote Drive"**.
   - Double-click `R:\`: Verify all configured shared folders appear as directories.
   - Double-click any folder: Verify contents open at native disk speed via NTFS junctions.
2. **Unmount Test:**
   - Press `Ctrl+C` in `FileAccessAgent`.
   - Verify: Drive `R:` disappears cleanly from "This PC" without leaving orphaned drive letters.

---

### Test Scenario 7: UserLoginAgent Desktop Client Test
1. **Launch Client Agent:**
   - Open PowerShell in `E:\Git\PCAccess-\UserLoginAgent\`.
   - Run `.\UserLoginAgent.exe` (or double-click `run.bat`).
2. **Authenticate:**
   - Enter Server URL (`https://localhost:44355`), Username (`admin`), and Password (`123`).
   - Select "Remember credentials: Y".
3. **Verify Drive & Session:**
   - Verify: The console shows Welcome banner, lists accessible shared folders, and automatically mounts drive `R:`.
   - Test CLI status: In another terminal, run `.\UserLoginAgent.exe status` -> Shows PID, mounted drive status, and server connectivity.
   - Test Clean Logout: Run `.\UserLoginAgent.exe logout` -> Unmounts `R:` and terminates background agent cleanly.

---

## 7. Summary Conclusion

PCAccess is a well-engineered proof-of-concept and prototype for **outbound remote file management**. Its core strength lies in its **firewall-friendly SignalR architecture** and **native Windows Explorer drive integration**.

However, before moving to a commercial or multi-tenant production environment:
1. **All OS support is currently Windows-only** (Server, Host Agent, Client Agent).
2. **Database passwords must be hashed (BCrypt)**.
3. **Agent SSL bypass must be removed**.
4. **File download must be refactored to HTTP streaming to prevent server RAM exhaustion**.

Once these four fixes are implemented, PCAccess will be an exceptionally robust, enterprise-grade solution.

# PCAccess — Step-by-Step Technical Guide, Rules & NuGet Documentation

**Project Name:** PCAccess (Remote PC Access & File Management System)  
**Target Framework:** ASP.NET MVC 5 (.NET Framework 4.8) + C# .NET Desktop Agent  
**Prepared For:** Team, Project Managers, Solution Architects, and Stakeholders  
**Last Updated:** September 22, 2026  

---

## 1. Executive Summary

**PCAccess** is an enterprise-grade remote PC access and file management portal. It enables authenticated users to:
1. Register and pair authorized Windows computers/laptops using a secure, temporary 6-digit pairing code (similar to TeamViewer / AnyDesk).
2. Monitor real-time online/offline connection states with automated heartbeats and instant disconnection alerts via SignalR.
3. Browse and access authorized shared directories on remote machines securely over outbound HTTPS/WSS without exposing local file-sharing or SMB/RDP ports to the public internet.

---

## 2. Core Architectural Principles & Golden Rules

Every phase of development strictly adheres to the following non-negotiable architectural rules:

1. **Strict Existing Pattern Adherence ("Zero Arbitrary / Apne Man Se Code"):**
   - Development strictly follows the project's established MVC, BAL, DAL, Stored Procedure, and JavaScript conventions.
   - No arbitrary third-party libraries, CSS frameworks, or unauthorized abstractions are introduced.
2. **Mandatory Commenting & Rationale Policy (What & Why):**
   - Every file, class, method, API action, JavaScript function, and Stored Procedure includes explicit comments detailing:
     - **WHAT** the code accomplishes.
     - **WHY (Reason)** it follows that specific architectural pattern.
3. **Continuous Step-by-Step Documentation:**
   - With every single feature or step completed, this document in `Doc/` and the rules in `CursorRule/` are updated so that any team member, lead, or manager can understand the changes immediately.
4. **Build Integrity:**
   - Every step must compile cleanly via MSBuild on .NET Framework 4.8 with `0 Errors`.

---

## 3. Complete NuGet Packages & Dependencies Inventory

Below is the complete inventory of all packages installed and used across the solution, their exact versions, purposes, and configurations:

| # | Package Name | Version | Target Project | Purpose & Role | Why This Version / Config |
|---|---|---|---|---|---|
| 1 | **Microsoft.AspNet.SignalR** | `2.4.3` | `PCAccess.csproj` (Web) | Metapackage for ASP.NET SignalR server components. | Latest stable release for .NET Framework 4.8; provides WebSocket and fallback transport capabilities. |
| 2 | **Microsoft.AspNet.SignalR.Core** | `2.4.3` | `PCAccess.csproj` (Web) | Core SignalR messaging engine, hub dispatching, and connection management. | Handles `DeviceHub`, connection mapping, group broadcasts (`User_{userId}`), and disconnect events. |
| 3 | **Microsoft.AspNet.SignalR.SystemWeb** | `2.4.3` | `PCAccess.csproj` (Web) | SignalR transport host integration for IIS / System.Web pipeline. | Integrates SignalR directly into ASP.NET MVC pipeline on IIS/IIS Express. |
| 4 | **Microsoft.AspNet.SignalR.JS** | `2.4.3` | `PCAccess.csproj` (Web) | Client-side JavaScript library (`jquery.signalR-2.4.3.min.js`). | Enables real-time browser connection to `/signalr` in `dashboard.js` without full page refresh. |
| 5 | **Microsoft.Owin** | `2.1.0` | `PCAccess.csproj` (Web) | Open Web Interface for .NET (OWIN) abstraction layer. | Required middleware pipeline host to mount SignalR onto IIS. |
| 6 | **Microsoft.Owin.Host.SystemWeb** | `2.1.0` | `PCAccess.csproj` (Web) | OWIN host adapter for ASP.NET System.Web runtime. | Discovers `[assembly: OwinStartup(typeof(PCAccess.Startup))]` and executes `app.MapSignalR()`. |
| 7 | **Microsoft.Owin.Security** | `2.1.0` | `PCAccess.csproj` (Web) | Security abstractions and helpers for OWIN pipeline. | Manages connection handshake integrity. |
| 8 | **Owin** | `1.0` | `PCAccess.csproj` (Web) | Standard interface specification for OWIN middleware. | Foundation assembly for all OWIN pipeline components. |
| 9 | **Microsoft.AspNet.SignalR.Client** | `2.4.3` | `FileAccessAgent.csproj` (Agent) | C# desktop client library for connecting to SignalR Hubs. | Provides `HubConnection`, `IHubProxy`, auto-reconnect, and heartbeat pings for the desktop agent. |
| 10 | **Newtonsoft.Json** | `13.0.3` | Both Web & Agent | Industry-standard JSON serializer and parser. | Fast, secure payload serialization for API models and `agent_config.json`. |
| 11 | **Microsoft.AspNet.Mvc** | `5.2.9` | `PCAccess.csproj` (Web) | ASP.NET MVC framework runtime. | Powers controllers (`AdminController`, `DeviceAPIController`) and Razor Views. |

---

## 4. Step-by-Step Implementation Breakdown

```
+-------------------+             +-----------------------+             +-----------------------+
|  Local Windows PC |   SignalR   | ASP.NET Server        |   SignalR   | Web Browser Dashboard |
|  FileAccessAgent  | <---------> | DeviceHub (/signalr)  | ----------> | dashboard.js          |
|  (Console App)    |   (WSS)     | + DeviceAPIController |   (WSS)     | (Live Status Badges)  |
+-------------------+             +-----------------------+             +-----------------------+
                                              |
                                              | ADO.NET (MySqlHelper)
                                              v
                                  +-----------------------+
                                  | SQL Server            |
                                  | PCAccess_DB           |
                                  | tbl_device            |
                                  | tbl_device_pairing    |
                                  | proc_device_manager   |
                                  +-----------------------+
```

### Phase 0: Solution Architecture & Foundation Setup
- **Namespace Clean Refactoring:** Migrated base template from legacy `Nkosh_blog` to `PCAccess` across BAL, Controllers, DAL, Models, Views, Content, and Scripts.
- **Database Initialization:** Created `PCAccess_DB`, executed standard table definitions, stored procedures, and seeded default administrator credentials (`admin` / `123`).
- **Build Verification:** Tested with MSBuild: `0 Errors`.

---

### Step 1: File Access System Dashboard UI Design
- **Objective:** Design the frontend user interface following the project's existing design language.
- **Key Deliverables:**
  1. **Dashboard Overview (`Views/admin/DashboardOverview.cshtml`):**
     - Header with title and descriptive context.
     - 4 Summary Cards (Connected Devices with animated live pulse, Shared Folders, Available Files, Recent Activity).
     - Connected Devices Section: Full-width table for desktop/tablet; adaptive touch cards for mobile (<640px).
     - Two-Column Bottom Split: Shared Folders (left) and Recent Activity timeline (right).
     - Clean zero-data empty states with guidance.
  2. **Navigation Menu Update (`Views/Shared/_Menu.cshtml`):**
     - Aligned sidebar with File Access System features: Dashboard, Connected Devices, Shared Folders, Activity Log, Profile.
  3. **Strict Boundaries Preserved:** Zero backend/API/SignalR logic touched in Step 1.

---

### Step 2: PC Agent + Secure Server Connection
- **Objective:** Establish outbound secure communication between the local Windows computer and the ASP.NET server.
- **Key Deliverables:**
  1. **Database Objects (`Doc/Table/` & `Doc/Procedure/`):**
     - `tbl_device.sql`: Stores permanent `device_guid`, `device_name`, `user_id`, secret `device_token`, real-time `status` (`online`/`offline`), `connection_id`, and `last_seen`.
     - `tbl_device_pairing.sql`: Stores temporary 6-digit pairing codes with a 10-minute expiry and single-use security flag (`is_used`).
     - `proc_device_manager.sql`: Centralized Stored Procedure supporting ActionTypes 1 through 7 with standard `@status` and `@msg` OUTPUT parameters.
  2. **SignalR & OWIN Infrastructure:**
     - `App_Start/Startup.cs`: OWIN pipeline mapping `/signalr`.
     - `Hub/DeviceHub.cs`: SignalR hub managing `JoinDashboardGroup`, `RegisterAgent`, 30s `SendHeartbeat`, and instant `OnDisconnected` offline detection.
  3. **Backend BAL & Controller:**
     - `BAL/DeviceManager.cs`: Orchestrates pairing, device authentication, and status transitions using `MySqlHelper.ExecuteDataTable` and `Parameters`.
     - `Controllers/DeviceAPIController.cs`: Follows `AccountAPIController` pattern, exposing endpoints:
       - `POST /DeviceAPI/GeneratePairingCode`
       - `POST /DeviceAPI/Register`
       - `POST /DeviceAPI/Verify`
       - `GET /DeviceAPI/GetDevices`
  4. **C# Desktop Agent (`FileAccessAgent/`):**
     - Added to `PCAccess.sln` as a lightweight .NET console application.
     - Interactive first-time prompt for Server URL and 6-digit Pairing Code.
     - Saves persistent machine identity to `agent_config.json`.
     - 30-second heartbeat timer and automatic reconnection with exponential backoff.
     - Graceful disconnect handling on `Ctrl+C`.
  5. **Dashboard Live Integration:**
     - `DashboardOverview.cshtml` updated to render live devices from database.
     - "Connect PC" button opens modal showing 6-digit code, countdown timer, and clipboard copy button.
     - `MyAppjs/Dashboard/dashboard.js` listens to SignalR `deviceStatusChanged` events and updates status pills (emerald pulse for Online, neutral badge for Offline) and timestamps in real time without refreshing the page.

---

## 5. Demonstration Script for Management & Team (5-Minute Walkthrough)

When presenting this system to a manager, lead, or client, use this structured demonstration:

### Step 1: Web Login & Dashboard Overview (1 minute)
- Open browser to `https://localhost:44355/Account/Login`.
- Log in with credentials (`admin` / `123`).
- Land on `Dashboard` (`/Admin/DashboardOverview`).
- **Talking Points:**
  - *"Here is our File Access System Dashboard. It uses our corporate design system with Tailwind CSS and Lucide icons."*
  - *"The summary cards show our connected endpoints, available folders, and recent activity."*
  - *"If no devices are connected yet, the system shows a clean onboarding empty state."*

### Step 2: Generating a Secure Pairing Code (1 minute)
- Click the **"Connect PC"** button in the dashboard header.
- The modal appears displaying a 6-digit code (e.g. `849-210`) and a 10-minute timer.
- Click **"Copy Code"**.
- **Talking Points:**
  - *"We do not require users to input their web password into the desktop agent."*
  - *"Instead, we use a secure short-lived pairing code (similar to AnyDesk/TeamViewer)."*
  - *"Once paired, the server issues an encrypted persistent token stored on the local PC in agent_config.json."*

### Step 3: Launching the Desktop Agent & Real-Time Connection (1.5 minutes)
- Open a terminal or launch `FileAccessAgent.exe`.
- When prompted, enter the Server URL (`https://localhost:44355`) and paste the 6-digit code.
- Observe console output:
  `[STATUS: ONLINE] Server marked <PC-NAME> as ONLINE. Dashboard updated!`
- Switch to the browser dashboard: observe the device row instantly update to 🟢 **Online** with an animated green pulse without refreshing the page!
- **Talking Points:**
  - *"The Agent makes an outbound WSS/SignalR connection to our server. It does NOT open inbound firewall ports or expose SMB/RDP."*
  - *"SignalR immediately notifies the user's dashboard group, updating the UI in real time."*

### Step 4: Heartbeat & Instant Offline Detection (1.5 minutes)
- Show the agent console sending a ping: `[HEARTBEAT] Ping sent at HH:mm:ss -> OK`.
- Press `Ctrl+C` in the Agent console to simulate shutdown.
- Switch to the browser dashboard: observe the device immediately switch to 🔴 **Offline**.
- **Talking Points:**
  - *"The Agent sends a lightweight heartbeat every 30 seconds."*
  - *"On disconnect, SignalR's OnDisconnected event triggers instantaneously in SQL Server and updates the web client in real time."*
  - *"On system reboot, the Agent starts automatically using its existing GUID from agent_config.json without asking for a pairing code again."*

---

## 6. Project Directory Map

```
E:\Git\PCAccess|-- App_Start/
|   |-- Startup.cs                     <-- OWIN SignalR Configuration
|-- BAL/
|   |-- DeviceManager.cs               <-- Business Access Layer for Devices
|-- Controllers/
|   |-- AdminController.cs             <-- Serves DashboardOverview with live devices
|   |-- DeviceAPIController.cs         <-- REST Endpoints for Pairing & Agent Auth
|-- CursorRule/
|   |-- cursor_rule.html               <-- Master AI & Cursor Rules
|   |-- PCAccess_Architecture_Documentation.md <-- In-depth technical architecture
|   |-- PCAccess_Setup_And_Login_Guide.md     <-- Developer onboarding guide
|-- Doc/
|   |-- PCAccess_Step_By_Step_Guide_And_NuGets.md <-- THIS DOCUMENT (Manager & Team Guide)
|   |-- Procedure/
|   |   |-- proc_device_manager.sql    <-- Device Stored Procedure (ActionTypes 1-7)
|   |-- Table/
|   |   |-- tbl_device.sql             <-- Registered Devices Table
|   |   |-- tbl_device_pairing.sql     <-- 6-Digit Pairing Codes Table
|-- FileAccessAgent/                   <-- C# Desktop Agent Console Application
|   |-- AgentConfig.cs                 <-- Config model & agent_config.json persistence
|   |-- FileAccessAgent.csproj         <-- Agent Project file with SignalR.Client
|   |-- Program.cs                     <-- Agent lifecycle, pairing, heartbeat, auto-reconnect
|-- Hub/
|   |-- DeviceHub.cs                   <-- SignalR Hub for real-time messaging
|-- Models/
|   |-- DeviceModel.cs                 <-- Device entity & request models
|-- MyAppjs/
|   |-- Dashboard/
|   |   |-- dashboard.js               <-- Real-time SignalR listeners & pairing modal AJAX
|-- Views/
|   |-- admin/
|   |   |-- DashboardOverview.cshtml   <-- File Access Dashboard View
|-- .cursorrules                       <-- Root level Cursor rules
|-- PCAccess.sln                       <-- Visual Studio Solution containing Web & Agent
```

---

## 7. Upcoming Phases Roadmap

- **Step 3:** Shared Folder Management (Selecting and broadcasting authorized local directories from the Agent).
- **Step 4:** Remote File Browser UI (Navigating directories, viewing files, breadcrumbs).
- **Step 5:** Secure File Transfer & Streaming (Downloading and uploading files through chunked encrypted streams).


---

## 8. Step 2.1 — UI Stability & Browser Console Optimization Fixes

### 1. Issue: Uncaught TypeError in layout.js (`addEventListener` on null)
- **Problem**: DevTools showed:
  ```text
  Uncaught TypeError: Cannot read properties of null (reading 'addEventListener') at layout.js:25:11
  ```
- **Root Cause**: `E:\Git\PCAccess\Content\js\myjs\layout.js` assumed specific multi-select and header elements (`selectBox`, `checkAll`, `selectedText`) were present on every page. On pages like `DashboardOverview`, these elements were null, causing script execution to halt abruptly.
- **Solution**:
  - Implemented safe defensive checks (`if (selectBox && dropdownMenu)`, `if (checkAll && items.length > 0)`, `sidebarEl?.classList`, etc.) in both `Content/js/myjs/layout.js` and `Content/js/layout.js`.
  - Added cache-busting query parameter `?v=@DateTime.Now.Ticks` to the script tag in `_Admin_Layout.cshtml` to ensure clients immediately receive the patched JavaScript.

### 2. Issue: Favicon 404 Error (`/Content/Home/Image/favicon.png`)
- **Problem**: DevTools showed:
  ```text
  Failed to load resource: the server responded with a status of 404 () :44355/Content/Home/Image/favicon.png:1
  ```
- **Root Cause**: Path mismatch between `_Admin_Layout.cshtml` (`~/Content/Home/Image/favicon.png`) and the disk directory (`Content/Home/images/nk-favicon.png`).
- **Solution**:
  - Mirrored the favicon to `Content/Home/Image/favicon.png` and created standard root `favicon.ico`.
  - Updated `_Admin_Layout.cshtml` `<link rel="icon">` tags to properly link both PNG and ICO formats. Verified with HTTP 200 response.


---

## 9. Step 2.2 — High-Contrast Real-Time UI/UX for Online / Offline Transitions

### 1. High-Contrast Status Badges & Action Buttons
- **Online State (Live PC):**
  - **Badge:** `bg-emerald-50 text-emerald-700 border border-emerald-300` with animated pulsing radar ping (`animate-ping`).
  - **Action Button:** Vivid primary emerald `Open` button with `external-link` icon.
  - **Device Icon:** Highlighted in soft emerald `bg-emerald-50 text-emerald-600 border border-emerald-100`.
- **Offline State (Ctrl+C / Agent Closed):**
  - **Badge:** Clear high-contrast rose pill `bg-rose-50 text-rose-700 border border-rose-200` with a solid red indicator dot (`bg-rose-500`).
  - **Action Button:** Distinct disabled `Disconnected` button (`bg-slate-100 text-slate-400 border border-slate-200 cursor-not-allowed`) with a `cloud-off` icon and hover tooltip explaining that the agent needs to be started.
  - **Device Icon:** Muted slate `bg-slate-100 text-slate-400 border border-slate-200`.

### 2. Summary Metric Counter Fix
- When 0 devices are online, the green pulsing dot is dynamically replaced with a neutral slate dot and text `0 Online now` in muted slate (eliminating the visual contradiction of a green dot with "0").

### 3. Real-Time "Recent Activity" Live Logging
- The moment the agent is terminated (`Ctrl+C`):
  - Prepend a live event card to the "Recent Activity" section:
    ```text
    [power-off] Sonu-PC disconnected (Offline) • Agent stopped / Ctrl+C • Just now
    ```
- When the agent reconnects:
  - Prepend an event card:
    ```text
    [wifi] Sonu-PC is now Online • SignalR session active • Just now
    ```

### 4. Non-Intrusive SweetAlert Toast Notifications
- Added toast notifications in the top-right corner via SweetAlert2 for instant feedback:
  - Disconnected: `⚠️ Sonu-PC is now Offline (Agent disconnected)`
  - Connected: `✅ Sonu-PC is now Online (Connected via SignalR DeviceHub)`


---

## 10. Step 3 — Shared Folder Management & Access Permissions

### 1. Architectural Overview & Scope
- **Goal**: Allow an authorized device owner/administrator to configure which local folders on a connected PC can be accessed through the Remote PC File Access System.
- **Strict Scope Boundaries**:
  - Shared folder registration, path verification via local PC Agent, folder configuration, user access permissions (`CanView`, `CanDownload`, `CanUpload`, `CanDelete`), device-to-folder relationships, and dashboard folder listing.
  - **Zero Server File Duplication**: The ASP.NET server only stores folder metadata/paths and access permissions; no physical files or folders are copied to the server.
  - **Zero Destructive Deletion**: Deactivating or removing a shared folder record never alters or deletes the physical directory on the local machine.

---

### 2. Database Design & Tables
1. **`tbl_shared_folder`**:
   - `folder_id` (BIGINT IDENTITY PK)
   - `folder_guid` (UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID())
   - `device_id` (INT NOT NULL FK -> `tbl_device.id`)
   - `folder_name` (NVARCHAR(150) NOT NULL)
   - `local_path` (NVARCHAR(500) NOT NULL)
   - `description` (NVARCHAR(500) NULL)
   - `is_active` (BIT NOT NULL DEFAULT 1)
   - `created_by` (BIGINT NOT NULL FK -> `tbl_user.user_id`)
   - `created_date`, `modified_date` (DATETIME)
   - **Filtered Unique Index**: `UQ_shared_folder_device_path` on `(device_id, local_path) WHERE is_active = 1` preventing duplicate active registrations for the same physical path on a device.

2. **`tbl_shared_folder_permission`**:
   - `permission_id` (BIGINT IDENTITY PK)
   - `folder_id` (BIGINT NOT NULL FK -> `tbl_shared_folder.folder_id`)
   - `user_id` (BIGINT NOT NULL FK -> `tbl_user.user_id`)
   - `can_view` (BIT NOT NULL DEFAULT 1)
   - `can_download` (BIT NOT NULL DEFAULT 0)
   - `can_upload` (BIT NOT NULL DEFAULT 0)
   - `can_delete` (BIT NOT NULL DEFAULT 0)
   - `created_date`, `modified_date` (DATETIME)
   - **Unique Constraint**: `UQ_folder_user_perm` on `(folder_id, user_id)` guaranteeing one permission matrix row per user per folder.

3. **Stored Procedure `proc_shared_folder_manager`**:
   - `ActionType = 1`: Get active folders for a device by `device_guid` (includes permission counts).
   - `ActionType = 2`: Get single folder details by `folder_guid`.
   - `ActionType = 3`: Insert new shared folder & automatically grant full owner permissions to creator.
   - `ActionType = 4`: Update folder name, description, and status.
   - `ActionType = 5`: Soft-delete folder (`is_active = 0`).
   - `ActionType = 6`: Get all users and their permission matrix for a folder.
   - `ActionType = 7`: Upsert user permission (`can_view`, `can_download`, `can_upload`, `can_delete`).
   - `ActionType = 8`: Seed team members (`rahul`, `amit`, `manager`) in `tbl_user` for testing.

---

### 3. Agent-Driven Path Verification Protocol
- **Security Rule**: The browser and server **never trust** client-submitted paths blindly.
- **Workflow**:
  ```text
  Dashboard User enters path (e.g., C:\Users)
        ↓
  Clicks "Validate Path"
        ↓
  Browser sends AJAX to /FolderAPI/ValidatePath
        ↓
  Server looks up active SignalR ConnectionId for Device
        ↓
  Server invokes DeviceHub.validateFolder(requestId, folderPath)
        ↓
  FileAccessAgent receives command:
    - Path traversal rejection (contains '..')
    - Invalid character checks
    - Directory.Exists(path) check
    - DirectorySecurity / Read accessibility check
        ↓
  Agent calls back to DeviceHub.FolderValidationResult(requestId, isValid, folderName, normalizedPath, errorMessage)
        ↓
  Server DeviceHub broadcasts to User group
        ↓
  Dashboard receives real-time validation result:
    - If valid: Displays green checkmark, unlocks "Save Folder" button.
    - If invalid: Displays red error message, keeps "Save Folder" disabled.
  ```

---

### 4. Controller Endpoints & Real-Time SignalR Hub
- **`Controllers/FolderAPIController.cs`**:
  - `POST /FolderAPI/ValidatePath`: Dispatches path validation to the connected agent via SignalR.
  - `POST /FolderAPI/SaveSharedFolder`: Validates session ownership and commits the new folder to SQL Server.
  - `POST /FolderAPI/UpdateSharedFolder`: Updates folder metadata or active status.
  - `POST /FolderAPI/DeleteSharedFolder`: Performs soft-deletion of folder configuration.
  - `GET /FolderAPI/GetDeviceFolders`: Returns folder list for the device with permission tallies.
  - `GET /FolderAPI/GetFolderPermissions`: Returns user permission matrix for a folder.
  - `POST /FolderAPI/UpdatePermission`: Updates `CanView`, `CanDownload`, `CanUpload`, `CanDelete` for a specific user.
- **`Hub/DeviceHub.cs`**:
  - Maintained thread-safe `_deviceConnectionMap` mapping `DeviceGuid` to active SignalR `ConnectionId`.
  - Added `ValidateFolderOnAgent` and `FolderValidationResult` hub methods.

---

### 5. Responsive UI / UX (`Views/admin/DeviceFolders.cshtml`)
- **Device Switcher & Offline Alert**: Top bar displays current machine name, status badge, and dynamic yellow alert banner when the selected device is offline.
- **Directories Table**: Lists folder name, physical path, description, status badge (`Active`/`Inactive`), access rights count, and actions.
- **Mobile Card View**: Responsive Tailwind grid collapses table to card layout on mobile devices.
- **Add/Edit Modal**: Modal with real-time "Validate Path" button, agent feedback container, and description field.
- **Permissions Modal**: Matrix modal listing all team users with custom emerald checkboxes for `View`, `Download`, `Upload`, and `Delete`.


---

## 11. Step 4 — Remote File & Folder Browser

### 1. Architectural Overview & Scope
- **Goal**: Allow an authorized logged-in user to open a configured shared folder on a connected PC and browse its directories and files remotely through the web application.
- **Strict Scope Boundaries**:
  - **Included**: Remote directory navigation, folder listing, file listing, breadcrumb navigation, file/folder metadata, in-memory instant search, column sorting, dual view mode (Table/List + Grid/Tile), loading states, empty states, and permission validation.
  - **Excluded (Deferred to Step 5+)**: Zero file downloading, uploading, deleting, renaming, copying, moving, streaming, or previews.
  - **Non-Destructive / Read-Only**: The PC Agent reads local directories in read-only mode. Files are never copied to the server or modified on the local machine.

---

### 2. Request & Response Architecture
- **Asynchronous REST API with Server-Side SignalR `TaskCompletionSource`**:
  ```text
  Browser (file-browser.js)
        ↓
  POST /FolderAPI/GetDirectoryContents { folder_guid, relative_path }
        ↓
  FolderAPIController:
    1. Authenticates session user
    2. Validates CanView permission via SQL Server (proc_shared_folder_manager ActionType 9)
    3. Checks if host PC is online
    4. Calls DeviceHub.RequestDirectoryListingAsync (10-second timeout)
        ↓
  DeviceHub (SignalR Hub):
    - Registers TaskCompletionSource<DirectoryListingResult>
    - Dispatches readDirectory(requestId, rootLocalPath, relativePath) to Agent
        ↓
  FileAccessAgent (Program.cs on host PC):
    - Security: path normalization, '..' traversal rejection, boundary verification
    - Verifies Directory.Exists(targetPath)
    - Enumerates immediate subdirectories (skips hidden/system)
    - Enumerates immediate files (skips hidden/system, calculates friendly size)
    - Sorts folders first alphabetically, then files alphabetically (capped at 500 items)
    - Calls back to DeviceHub.DirectoryContentsResult(requestId, isSuccess, errorMessage, items)
        ↓
  DeviceHub:
    - Resolves TaskCompletionSource
        ↓
  FolderAPIController:
    - Returns standard HTTP JSON response { status: "success", data: ... }
        ↓
  Browser:
    - Renders Table or Grid view, updates breadcrumb, enables instant search and sort
  ```

---

### 3. Database Layer (`Doc/Procedure/proc_shared_folder_manager.sql`)
- Added `@ActionType = 9`: **Get Folder Details & Verify View Permission**
  - Inputs: `@user_id`, `@folder_guid`.
  - Permission rule: Checks if requesting user is the device owner (`tbl_device.user_id = @user_id`) OR has explicit `can_view = 1` in `tbl_shared_folder_permission`.
  - Output: Returns folder metadata (`folder_id`, `folder_guid`, `folder_name`, `local_path`, `description`), device metadata (`device_id`, `device_guid`, `device_name`, `device_status`, `is_device_online`), and permission flags (`can_view`, `can_download`, `can_upload`, `can_delete`, `is_owner`).

---

### 4. PC Agent Layer (`FileAccessAgent/`)
- Added `FileItemDto.cs` defining `name`, `type`, `is_folder`, `size_bytes`, `size_formatted`, `extension`, `last_modified`, `last_modified_formatted`, `relative_path`, and `icon_type`.
- Added `readDirectory` SignalR listener and `HandleReadDirectoryAsync` in `Program.cs`.
- Integrated file type icon resolver (`pdf`, `word`, `excel`, `image`, `video`, `audio`, `archive`, `code`, `text`, `file`).

---

### 5. Frontend UI & UX (`Views/admin/FileBrowser.cshtml` & `MyAppjs/Dashboard/file-browser.js`)
- **Route**: `/Admin/FileBrowser?folderGuid={guid}&path={relativePath}`.
- **Deep-linking & History**: Native browser Back/Forward navigation supported via `window.history.pushState` and `popstate` events.
- **Clickable Breadcrumb**: Displays `Dashboard / [Device Name] / [Folder Name] / [Subfolders]`, allowing instant navigation back to any parent segment.
- **Action Toolbar**:
  - Up / Back directory button (`arrow-up`, disabled at root).
  - Search input with real-time clear button (`X`).
  - View mode toggle (Table View vs Grid View), saved in `localStorage`.
  - Refresh button (`rotate-cw`).
  - Item counter badge: `X folders, Y files (Z MB)`.
- **Instant Search & Sort**: Real-time in-memory filtering by name; column sorting by Name, Type, Size, or Date (folders always remain at the top).
- **Pagination**: 50 items per page with Prev/Next buttons and page numbers.
- **DeviceFolders Integration**: Added a green **"Browse"** button with `folder-open` icon on each active folder row in `/Admin/DeviceFolders`.

---

## 12. Step 7 — Native .NET Framework 4.8 Unification for Desktop Agent

### 1. Problem Statement & Architectural Rationale
- **The Problem:** The Desktop Agent (`FileAccessAgent`) was originally targeting `.NET 10.0-windows` (.NET Core). On target computers without .NET 10 x64 runtime installed, attempting to run `FileAccessAgent.exe` or manage the service via batch files resulted in:
  ```text
  You must install or update .NET to run this application.
  Framework: 'Microsoft.NETCore.App', version '10.0.0' (x64)
  No frameworks were found.
  The following frameworks for other architectures were found: x86 10.0.12
  ```
- **Architectural Rationale:**
  - The entire web application (`PCAccess.csproj`) is built on **ASP.NET MVC 5 (.NET Framework 4.8)**.
  - Converting `FileAccessAgent.csproj` to **.NET Framework 4.8 (`net48`) AnyCPU** ensures:
    1. **Zero External Prerequisites:** .NET Framework 4.8 is pre-installed out of the box on all Windows 10 and Windows 11 machines.
    2. **Zero Architecture Conflicts:** Compiling as `AnyCPU` eliminates all `x86` vs `x64` mismatch errors.
    3. **Native ServiceBase:** Uses `System.ServiceProcess.ServiceBase` directly without heavy third-party hosting packages.
    4. **Unified Solution:** Both Web and Agent projects compile cleanly together under MSBuild in Visual Studio with 0 errors.

### 2. NuGet Packages & Reference Alignment
- **FileAccessAgent Packages:**
  - `Microsoft.AspNet.SignalR.Client` (version `2.4.3`) — Native .NET Framework 4.5/4.8 client.
  - `Newtonsoft.Json` (version `13.0.3`) — High performance JSON serialization.
- **Removed Dependencies:**
  - Removed `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Hosting.WindowsServices` (.NET Core packages).
- **Framework References:**
  - `System.ServiceProcess` (for native Windows Service hosting).
  - `System.Configuration`, `System.Net.Http`.

### 3. Native Windows Service Implementation (`AgentWindowsService.cs`)
- Replaced `AgentBackgroundService` with `AgentWindowsService` inheriting from `System.ServiceProcess.ServiceBase`.
- Dispatches:
  - `OnStart(string[] args)`: Starts agent engine via background Task.
  - `OnStop()`: Cancels cancellation token and triggers graceful disconnect to notify SignalR server.
  - `OnShutdown()`: Handles clean OS shutdown.

### 4. Service Management Scripts Updated
- `check-status.bat`: Direct query via `FileAccessAgent.exe --status`.
- `start-service.bat`: Administrative elevation and service start.
- `stop-service.bat`: Administrative elevation and service stop.
- `install-service.bat`: Auto-start Windows Service registration via `sc.exe`.
- `uninstall-service.bat`: Safe service teardown.

### 5. Verification Results
- **MSBuild Build:** Succeeded with `0 Errors` across `PCAccess.sln`.
- **Status Query:** Verified execution of `FileAccessAgent.exe --status` with exit code `0`.
- **Architecture:** Fully platform-independent `AnyCPU` targeting .NET Framework 4.8.

### 6. Visual Studio NuGet TargetFramework Resolution Fix
- **Symptom:** In Visual Studio Error List:
  ```text
  Your project does not reference '.NETFramework,Version=v4.8' framework.
  Add a reference to '.NETFramework,Version=v4.8' in the 'TargetFrameworks' property of your project file and then re-run NuGet restore.
  File: Microsoft.NuGet.targets, Line 198
  ```
- **Cause:** Leftover `obj/project.assets.json` generated by .NET Core SDK CLI cached the old TFM `net10.0-windows` and collided with classic `TargetFrameworkVersion=v4.8`.
- **Permanent Solution:**
  1. Converted package dependencies to standard classic .NET Framework references:
     - `packages.config` created with `Microsoft.AspNet.SignalR.Client` (2.4.3) and `Newtonsoft.Json` (13.0.3).
     - Standard `<Reference>` tags with `<HintPath>..\packages\...</HintPath>` in `FileAccessAgent.csproj`.
  2. Added classic `App.config` specifying `<supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />`.
  3. Purged stale `obj/` build cache.
  4. Solution rebuild passes with **0 Errors**. All `.bat` commands (`start`, `stop`, `status`, `install`, `uninstall`) execute cleanly and smoothly.

---

## 14. Step 7 — Virtual "Remote Drive (R:)" in Windows Explorer ("This PC")

### 1. Architectural Overview & User Scope
- **Objective:** When the local PC Agent (`FileAccessAgent`) connects to the PCAccess server (`ONLINE`), a virtual drive (letter **`R:`**, display name **`"Remote Drive"`**) automatically mounts under **"This PC"** in Windows Explorer.
- **Inside the Drive:** Lists all active Shared Folders configured for this PC in PCAccess.
- **Direct Opening:** Double-clicking any folder opens its contents directly at full native disk speed using Windows NTFS Directory Junctions (`mklink /J`).
- **Dynamic Real-Time Sync:** Whenever a folder is added, edited, or deleted on the web portal, the server notifies the agent (`syncSharedFolders`), updating the drive in real-time.
- **Graceful Cleanup:** On disconnect, Ctrl+C shutdown, or service stop, the drive is cleanly unmounted (`subst R: /d`, `DefineDosDevice`).

### 2. Components Created & Modified
1. **`PCAccess` Server Layer:**
   - **`Models/FolderModel.cs`**: Added `AgentSharedFolderDto` contract (`folder_id`, `folder_name`, `local_path`, `description`).
   - **`Hub/DeviceHub.cs`**: Added `GetDeviceSharedFolders` (authenticates agent and returns active shared folders) and `NotifyAgentToSyncFolders(deviceGuid)`.
   - **`Controllers/FolderAPIController.cs`**: Calls `DeviceHub.NotifyAgentToSyncFolders` upon folder creation/update.
2. **`FileAccessAgent` Desktop Agent Layer:**
   - **`AgentConfig.cs` & `agent_config.json`**: Added `DriveLetter` (`R:`), `DriveLabel` (`Remote Drive`), and `EnableRemoteDrive` (`true`).
   - **`AgentSharedFolderDto.cs`**: DTO mapping for agent folder sync.
   - **`RemoteDriveManager.cs`**:
     - Manages local staging directory (`%LOCALAPPDATA%\PCAccess\RemoteDriveRoot`).
     - Constructs NTFS directory junctions (`cmd /c mklink /J`) for each shared folder.
     - Maps drive letter via `subst` and Win32 `DefineDosDevice`.
     - Registers Windows Explorer display name under `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons\R\DefaultLabel`.
     - Broadcasts shell notifications via `SHChangeNotify` (`SHCNE_DRIVEADD`, `SHCNE_DRIVEREMOVED`, `SHCNE_ASSOCCHANGED`) so Explorer updates immediately.
   - **`Program.cs`**:
     - Calls `RefreshRemoteDriveAsync()` upon successful agent registration.
     - Subscribes to SignalR `syncSharedFolders` event for live updates.
     - Calls `RemoteDriveManager.UnmountDrive()` during agent shutdown.
   - **`FileAccessAgent.csproj`**: Fixed ItemGroup syntax and registered new compile files.

### 3. Build Verification
- **`PCAccess.csproj`**: Built with MSBuild -> `0 Errors`.
- **`FileAccessAgent.csproj`**: Built with MSBuild (Release & AnyCPU) -> `0 Errors`.

---

## 15. FileAccessAgent Root-Level CLI & 1-Click Scripts

To eliminate the need to navigate into `bin\Debug\`:
- All terminal operations can be executed directly inside `E:\Git\PCAccess\FileAccessAgent`.
- Root CLI wrappers automatically detect and forward arguments to `bin\Debug\FileAccessAgent.exe` (or `bin\Release\FileAccessAgent.exe`).

### Available 1-Click Scripts (in `FileAccessAgent\` root folder):
1. **`run.bat`**: Runs the agent interactively in console mode with a single double-click.
2. **`install-service.bat`**: Installs Windows Service (`PCAccessAgent`) with Auto-Start on boot (self-elevates).
3. **`start-service.bat`**: Starts the Windows Service.
4. **`stop-service.bat`**: Stops the Windows Service.
5. **`uninstall-service.bat`**: Removes the Windows Service.
6. **`check-status.bat`**: Displays current service running status.

### Terminal Commands (from `E:\Git\PCAccess\FileAccessAgent\` directory):
- **Command Prompt (CMD)**:
  ```cmd
  FileAccessAgent                  :: Run interactively in console mode
  FileAccessAgent --install        :: Install as auto-start Windows Service
  FileAccessAgent --status         :: Check service status
  FileAccessAgent --start          :: Start service
  FileAccessAgent --stop           :: Stop service
  FileAccessAgent --uninstall      :: Uninstall service
  ```
- **PowerShell**:
  ```powershell
  .\FileAccessAgent                :: Run interactively in console mode
  .\FileAccessAgent --install      :: Install as auto-start Windows Service
  .\FileAccessAgent --status       :: Check service status
  .\FileAccessAgent --start        :: Start service
  .\FileAccessAgent --stop         :: Stop service
  .\FileAccessAgent --uninstall    :: Uninstall service
  ```

---

## 16. Step 3.2 — "Description (Optional)" Null Handling Fix

### 1. Issue Description
- **Error**: `System.NullReferenceException: Object reference not set to an instance of an object` (`value was null`).
- **Location**: `PCAccess.DAL.Parameters.GetStringParameter(string propertyName, string value)` at line 244 (`if (string.IsNullOrEmpty(value.Trim()))`).
- **Trigger**: When adding or editing a Shared Folder on `DeviceFolders.cshtml` and leaving the **"Description (Optional)"** textarea blank.
- **Root Cause**: ASP.NET MVC's default model binder converts empty strings `""` to `null`. When `FolderManager.AddSharedFolder` passed `req.description` to `Parameters.GetStringParameter`, it called `value.Trim()`, which directly threw a `NullReferenceException` because `value` was null.

### 2. Files Modified & Solution Implemented
1. **`PCAccess\DAL\Parameters.cs`**:
   - Replaced fragile `if (string.IsNullOrEmpty(value.Trim()))` with null-safe `if (string.IsNullOrWhiteSpace(value))` across `GetStringParameter`, `GetStringParameterWithNonFilter`, and `GetStringParameter_Null`.
   - When null or whitespace, immediately returns an empty parameter `new SqlParameter("@" + propertyName, "")` or `DBNull.Value` without calling `.Trim()` on null.
2. **`PCAccess\BAL\FolderManager.cs`**:
   - In `AddSharedFolder`: Added null-coalescing fallback `Parameters.GetStringParameter("description", description ?? "")`.
   - In `UpdateSharedFolder`: Added null-coalescing fallback `Parameters.GetStringParameter("description", description ?? "")`.

### 3. Build & Test Status
- `PCAccess.csproj` compiled cleanly with `0 Errors`.
- Adding and editing shared folders with an empty or omitted Description now succeeds without any exceptions.

---

## 17. Step 3.3 — Sidebar & Page Content Alignment Fix (Double Padding Elimination)

### 1. Issue Description
- **Problem**: When the sidebar was open, an excessively large blank white gap (~288px) appeared between the sidebar and the main page content (such as `DashboardOverview.cshtml`).
- **Visual Discrepancy**: The top header's hamburger button `☰` was aligned at `24px` from the sidebar, but the `<h1>Dashboard</h1>` heading and cards started `288px` further to the right.

### 2. Root Cause
- In `_Admin_Layout.cshtml`, the DOM structure is nested:
  ```html
  <div id="mainContent" class="...">
      <div id="pageContent" class="...">
          @RenderBody()
      </div>
  </div>
  ```
- In `Content/js/myjs/layout.js`, `openSidebar()` was adding `md:pl-64` (`256px` padding-left) to `mainContentEl` **AND** adding `md:pl-64` (`256px` padding-left) to `pageContentEl`.
- This resulted in double-padding (`256px + 256px = 512px`), pushing all page content far away from the sidebar.

### 3. Solution Implemented
- Removed the duplicate `pageContentEl.classList.add('md:pl-64')` and `remove('md:pl-64')` in both `Content/js/myjs/layout.js` and `Content/js/layout.js`.
- Now, only parent `mainContentEl` receives `md:pl-64`, allowing page content inside `@RenderBody()` to align naturally under the header at standard responsive margins (`px-4 sm:px-6 lg:px-8`).

---

## 18. Step 3.4 — 100% Dynamic Conversion of `/Admin/DashboardOverview`

### 1. Enhancements Overview
All previously static or mocked sections of the Dashboard have been converted to 100% database-driven and real-time operational feeds:

1. **Top Summary Cards**:
   - **Connected Devices**: Real count (`@devices.Count`) and live SignalR online status (`@onlineCount`).
   - **Shared Folders**: Real total active shared folders queried from `tbl_shared_folder` via `FolderManager.GetAllUserSharedFolders`.
   - **Remote Drive**: Displays mounted status (`R: Virtual Drive`) corresponding to the desktop `FileAccessAgent` remote mount.
   - **Recent Activity Count**: Dynamic event count based on real sessions and updates.

2. **Connected Devices Table**:
   - **Shared Folders Column**: Replaced hardcoded text (`"3 folders"`) with `@device.shared_folder_count` (exact folder count per device mapped from database).
   - Applied to both Desktop Table and Mobile Card views.

3. **Shared Folders Section (Bottom-Left)**:
   - Replaced mock folders (`Projects 412 files`, `Documents 872 files`) with real records from `tbl_shared_folder`.
   - Displays real folder name, host workstation name, and physical directory path.
   - Wired active **Browse** button linking directly to `/Admin/FileBrowser?folderGuid={guid}`.
   - Added clean empty state when no folders are yet configured, linking to `/Admin/DeviceFolders`.

4. **Recent Activity Feed (Bottom-Right)**:
   - Dynamically generated from live online device telemetry and recently registered shared folders.
   - Clickable links directly navigate to the respective device or folder browser.

### 2. Code Changes
- **`Models/DeviceModel.cs`**: Added `shared_folder_count`.
- **`Models/AdminDashboardViewModel.cs`**: Added `SharedFolders`, `TotalSharedFolders`, `OnlineDevicesCount`, and `RecentActivities` list.
- **`BAL/FolderManager.cs`**: Implemented `GetAllUserSharedFolders(long userId)` querying `tbl_shared_folder` joined with `tbl_device`.
- **`Controllers/AdminController.cs`**: Wired `DashboardOverview()` to populate all dynamic models and activity events.
- **`Views/Admin/DashboardOverview.cshtml`**: Replaced all hardcoded HTML/strings with dynamic Razor expressions.

---

## 19. Step 3.5 — Shared Folders Architecture Fix via Stored Procedure (ActionType 10)

### 1. Issue Description & Resolution
- **Problem**: On `/Admin/DashboardOverview`, the Connected Devices table showed `0 folders` and the Shared Folders section displayed `No shared folders configured yet`, even though `/Admin/DeviceFolders` showed 3 configured folders.
- **Root Cause & Architectural Decision**: The initial implementation attempted to use raw SQL inside `FolderManager.GetAllUserSharedFolders`. In accordance with the project's strict core principle ("Zero Arbitrary / Apne Man Se Code — All DB access must strictly use Stored Procedures with `@ActionType`, `@status`, and `@msg`"), raw SQL was completely replaced.
- **Solution Implemented**:
  1. **Stored Procedure (`Doc/Procedure/proc_shared_folder_manager.sql`)**:
     - Added **`ActionType = 10`** (`GetAllUserFolders`).
     - Queries all active shared folders for a user across all active devices (`WHERE d.user_id = @user_id AND f.is_active = 1 AND d.is_active = 1`).
     - Returns folder details including `device_name`, `device_guid`, `access_level`, `created_at`, etc.
  2. **BAL Layer (`BAL/FolderManager.cs`)**:
     - Updated `GetAllUserSharedFolders(long userId)` to call `proc_shared_folder_manager` with `ActionType = 10` and output parameters, adhering to the standard project pattern.
  3. **Verification**:
     - Compiled with `0 Errors` via MSBuild.
     - Confirmed all 3 folders (`Company Files`, `tesring folder`, `D drive`) load dynamically.

---

## 20. Step 3.6 — Database Operations & Security Policy (Strict Manual Execution)

### 1. Policy Directive
- **CRITICAL RULE**: Under NO circumstances should any AI assistant or automated CLI tool execute scripts, migrations, or DDL/DML directly against the **Live/Production Database** or the **Local Database**.
- All database modifications (Tables, Stored Procedures, Views, Seeds) must be authored exclusively into `.sql` files:
  - Tables: `Doc/Table/tbl_<entity>.sql`
  - Procedures: `Doc/Procedure/proc_<entity>_<action>.sql`
- The user will **MANUALLY** inspect and execute all `.sql` scripts using Visual Studio or SQL Server Management Studio (SSMS) on Local DB and Live DB.
- Recorded and enforced in `.cursorrules` and `.agents/rules/database-rules.md`.

---

## 21. Step 3.7 — UserLoginAgent: Direct User/Employee Desktop Agent & Remote Drive Architecture

### 1. Overview & Business Rationale
- **Objective**: Provide users, employees, and administrators the ability to access their permitted shared folders directly from their Windows desktop **without needing to open a web browser**.
- **User Experience**:
  - User launches `UserLoginAgent.exe` (or `run.bat`).
  - Prompts for:
    1. **Website Server URL** (e.g. `http://localhost:44355` or live production URL)
    2. **Username or Email**
    3. **Password** (masked input)
  - Features session persistence (`user_agent_config.json`) with "Remember credentials", allowing instant 1-click login on subsequent runs.
  - Upon authentication, automatically mounts virtual **`R:` "Remote Drive"** in Windows Explorer under "This PC" and opens `R:\` directly.
  - Interactive terminal commands while running:
    - `[S]` - Status: View session details, user role, drive mount status, and connection health.
    - `[O]` - Open: Directly open Remote Drive (`R:\`) in Windows Explorer.
    - `[R]` - Refresh: Re-fetch shared folders list dynamically from server.
    - `[C]` - Switch User: Clear credentials and switch login account without exiting console.
    - `[L]` or `[Q]` or `[X]` - Logout: Unmount drive cleanly and exit.
  - Command Line & Batch Scripts:
    - `run.bat` - Launch desktop agent.
    - `status.bat` (`UserLoginAgent.exe status`) - Check agent and drive status non-intrusively.
    - `logout.bat` / `stop.bat` (`UserLoginAgent.exe logout`) - Unmount drive and stop agent safely.
  - Detailed User Guide: [`Doc/UserLoginAgent_User_Guide.md`](file:///e:/Git/PCAccess/Doc/UserLoginAgent_User_Guide.md)

### 2. Architecture & File Inventory
| Layer | File Created / Modified | Purpose |
|---|---|---|
| **Database** | `Doc/Procedure/proc_user_agent_auth.sql` | Authenticates user by username OR email, verifies active status, and returns accessible folders (Admins receive all active folders; Employees receive folders where owner or `can_view = 1` in `tbl_shared_folder_permission`). **Manual execution by user in SSMS/VS**. |
| **Model** | `Models/UserAgentModel.cs` | DTOs for `UserAgentLoginRequest`, `UserAgentLoginResponse`, and `UserAgentFolderDto`. |
| **BAL** | `BAL/UserAgentManager.cs` | Executes `proc_user_agent_auth` via `MySqlHelper.ExecuteDataSet` with output parameters `@status` and `@msg`. |
| **API** | `Controllers/UserAgentAPIController.cs` | REST endpoints: `POST /UserAgentAPI/Login` and `GET /UserAgentAPI/Ping`. |
| **Client App** | `UserLoginAgent/UserLoginAgent.csproj` | .NET Framework 4.8 console desktop application. |
| **Client App** | `UserLoginAgent/Program.cs` | Main interactive login loop, CLI arguments (status, stop, logout), and session lifecycle. |
| **Client App** | `UserLoginAgent/ClientRemoteDriveManager.cs` | Win32 `subst` and `DefineDosDevice` drive mounter, staging junction manager, and shell refresher. |
| **Client App** | `UserLoginAgent/UserApiClient.cs` | Native `HttpClient` wrapper for login and ping requests. |
| **Client App** | `UserLoginAgent/UserAgentConfig.cs` | JSON configuration manager for saving credentials and server URL. |
| **Client App** | `UserLoginAgent/UserAgentLogger.cs` | Structured console and file logger with timestamps and color tags. |
| **Client App** | `UserLoginAgent/run.bat` | 1-click launcher for the user agent. |
| **Client App** | `UserLoginAgent/status.bat` | Batch script to check agent process and drive status. |
| **Client App** | `UserLoginAgent/logout.bat` | Batch script to unmount drive and log out running agent. |
| **Documentation** | `Doc/UserLoginAgent_User_Guide.md` | Dedicated step-by-step user manual with commands and action descriptions. |
| **Solution** | `PCAccess.sln` | Integrated `UserLoginAgent` project. |

### 3. Verification & Safety
- **Compilation**: Clean MSBuild for all 3 projects (`PCAccess.dll`, `FileAccessAgent.exe`, `UserLoginAgent.exe`) with `0 Errors`.
- **Existing System Integrity**: Existing web portal controllers, layouts, SignalR hub (`DeviceHub`), and host `FileAccessAgent` remain 100% untouched.










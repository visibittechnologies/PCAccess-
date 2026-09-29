# PCAccess - Project Architecture & Technical Documentation

## 1. Project Overview
- **Project Name:** PCAccess
- **Technology Stack:** ASP.NET MVC 5 (.NET Framework 4.8), C#, Razor, Vanilla JS, jQuery 3.7.0, SQL Server
- **Solution Type:** Flat Visual Studio Solution (`PCAccess.sln` and `PCAccess.csproj` in root folder `E:\Git\PCAccess\`)
- **Primary Purpose:** Remote PC Access, monitoring, and administration platform with an integrated Admin Dashboard and role-based authentication.

---

## 2. Solution Directory Structure

```text
E:\Git\PCAccess\
├── PCAccess.sln                          # Visual Studio 2022 Solution file
├── PCAccess.csproj                       # .NET 4.8 Web Application Project file
├── Web.config                            # Main configuration (connectionStrings, routing, compilation)
├── packages.config                       # NuGet packages (MVC 5.2.9, Razor, Newtonsoft.Json)
├── Global.asax / Global.asax.cs          # Application startup & bundle/route registration
├── App_Start\
│   ├── RouteConfig.cs                   # Default Route mapping (defaults to Account/Login)
│   ├── FilterConfig.cs                  # Global MVC Filters
│   └── BundleConfig.cs                  # CSS & JS Bundles
├── BAL\                                  # Business Access Layer (Domain logic & orchestrators)
│   ├── Account.cs                       # Account business logic
│   ├── ActivityLogHelper.cs             # User verification & activity tracking
│   ├── Company.cs                       # Company profiles, categories, settings
│   ├── DashboardManager.cs              # Dashboard metrics & analytics fetcher
│   ├── ExceptioLoggerDB.cs              # Database error logging service
│   ├── MasterDataTabel.cs               # Dynamic master table helper
│   └── Profle.cs                        # User profile management
├── DAL\                                  # Data Access Layer (ADO.NET execution engine)
│   ├── MySqlHelper.cs                   # Core SQL Server command executor using "constr"
│   ├── Parameters.cs                    # SqlParameter generation utilities
│   ├── AiAuditDAL.cs                    # Audit logging DAL
│   └── ConversationDAL.cs               # Conversation repository
├── Controllers\                          # MVC & Web API Controllers
│   ├── AccountController.cs             # Serves /Account/Login view
│   ├── AccountAPIController.cs          # API endpoints for Login POST, Logout
│   ├── AdminController.cs               # Serves /Admin/DashboardOverview, Profile, etc.
│   ├── AdminAPIController.cs            # Admin management APIs
│   ├── CommonController.cs              # Shared dropdown & utility APIs
│   └── HomeController.cs                # Default home/public actions
├── Models\                               # Data models, ViewModels & Helpers
│   ├── LoginModel.cs                    # Login request model (UserName, PassWord, RememberMe)
│   ├── AdminDashboardViewModel.cs       # Admin dashboard stats model
│   ├── BaseModel.cs                     # Base entity model
│   ├── CompanyProfile.cs                # Company profile model
│   ├── SeoMetaModel.cs                  # SEO & Meta tags model
│   ├── TablePagging.cs                  # JqDataTable paging model
│   └── MyFun\                          # Core Security & Utility Functions
│       ├── CommanUtilities.cs           # User session provider, cookie encrypt/decrypt, WebHelper
│       ├── Net.cs                       # IP address, browser detection
│       └── myFunc.cs                    # Formatting, string & utility helpers
├── MyAppjs\                              # Modular Client-Side JavaScript
│   ├── Common\                          # master_dataTable.js, drawer.js, modal.js, Tracker.js
│   └── profile\                         # profile.js
├── Views\                                # Razor View templates
│   ├── account\Login.cshtml            # Primary Login view
│   ├── admin\DashboardOverview.cshtml  # Admin dashboard view
│   ├── admin\Profile.cshtml            # Admin profile view
│   ├── Shared\                         # _Admin_Layout.cshtml, _Header.cshtml, _Menu.cshtml
│   └── Web.config                       # Razor namespace registration
├── Content\                              # CSS, Tailwind/Bootstrap, Images, Vendor libraries
├── Scripts\                              # jQuery 3.7.0, Validation, Bootstrap scripts
├── Doc\                                  # Database Schema, Tables, Stored Procedures, Seed Data
│   ├── Table\                          # Table definitions (tbl_user, tbl_user_type, etc.)
│   ├── Procedure\                      # Stored procedures (proc_user_login, etc.)
│   ├── Function\                       # User-defined functions
│   └── Seed_Data_Admin_User.sql         # Default admin credentials seed script
└── CursorRule\                           # AI & Developer Rules and Project Documentation
    ├── cursor_rule.html                 # Core developer rules and standards
    ├── PCAccess_Architecture_Documentation.md
    └── PCAccess_Setup_And_Login_Guide.md
```

---

## 3. Database Architecture & Conventions

### Connection String
- Configured in `Web.config`:
```xml
<connectionStrings>
  <add name="constr" 
       connectionString="Data Source=(localdb)\VSDigiCRM2025;Initial Catalog=PCAccess_DB;Integrated Security=True;MultipleActiveResultSets=True" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```
- Accessed through `System.Configuration.ConfigurationManager.ConnectionStrings["constr"].ConnectionString`.

### Stored Procedure Execution Pattern
All queries execute via `PCAccess.DAL.MySqlHelper`:
- `ExecuteDataTable(sp_name, parameters)`
- `ExecuteNonQuery(sp_name, parameters)`
- Output parameters (`@status`, `@msg`) are standard across all procedures.

---

## 4. Authentication & Session Management

1. **User Submits Credentials:** POST `/AccountAPI/Login` receives `LoginModel` (`UserName`, `PassWord`, `RememberMe`).
2. **Verification:** `ActivityLogHelper.VerifyUser` calls Stored Procedure `proc_user_login`.
3. **Identity Mapping:** Returns user details (`user_id`, `name`, `email`, `role_name`, `module_url`).
4. **Session / Encrypted Cookie Creation:**
   - Provider stores user state using `CommanUtilities.Provider.AddCurrent(op)`.
   - Encrypted with `DESEncrypt`.
   - Auth cookie named `PCAccessAuth` is attached to `Response.Cookies`.
5. **Client Redirection:** Controller returns `{ status: "success", url: op.GetUrl }`, and JavaScript navigates to `data.url` (e.g., `/Admin/DashboardOverview`).

---

## 5. Coding, Pattern Enforcement & Maintenance Rules

### 5.1 The Core Golden Rule: Strict Existing Pattern Adherence
> **"Mera existing pattern se hi development hoga. Kuchh bhi apne man se write nahi karni hai."**
- Absolutely **NO arbitrary or self-invented code**.
- All development must strictly mirror existing reference files in this project.
- Before writing ANY code, inspect existing implementations across each layer and match their exact structure, naming, parameters, and response patterns.

### 5.2 Mandatory Code Commenting & Rationale Policy
- **Every class, method, function, API endpoint, and stored procedure MUST include descriptive comments explaining:**
  1. **WHAT** the code does.
  2. **REASON / WHY:** The business reason and how it adheres to the existing project pattern.
- Uncommented code or code lacking rationale is strictly prohibited.

### 5.3 Layer-by-Layer Pattern Enforcement

#### 1. JavaScript (`MyAppjs/`)
- **Zero Inline Scripts:** Never write `<script>` logic directly inside `.cshtml` views.
- **Dedicated Feature Modules:** All scripts belong in `MyAppjs/[Feature]/` (e.g., `MyAppjs/Device/device.js`, `MyAppjs/Folder/folder.js`).
- **AJAX & Response Contract:**
  - Must strictly follow the existing AJAX response contract:
    ```javascript
    // REASON: Aligns with existing AJAX response contract across all MyAppjs modules
    $.ajax({
        url: '/ControllerName/ActionOrAPI',
        type: 'POST',
        data: JSON.stringify(postData),
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        success: function (res) {
            if (res.status === 'success' || res.status === 1) {
                // Success feedback via toastr / sweetalert
            } else {
                // Error message display via res.msg
            }
        }
    });
    ```
- **Reuse Common Modules:** Always check and reuse `MyAppjs/Common/master_dataTable.js`, `Common.js`, `modal.js`, and `drawer.js`.

#### 2. Views & Razor (`Views/`)
- Must inherit from `~/Views/Shared/_Admin_Layout.cshtml`.
- Strictly adhere to existing Tailwind CSS utility classes, primary brand color (`#1f9251`), `Manrope` font, and Lucide icons (`data-lucide="..."`).
- Maintain full responsiveness across Mobile (`<640px`), Tablet (`640px-1024px`), and Desktop (`>=1024px`).
- Include Razor comments (`@* REASON: ... *@`) explaining layout and component decisions.

#### 3. Models & ViewModels (`Models/`)
- Reside in `PCAccess.Models` or feature sub-namespaces.
- Property naming must strictly match database columns and stored procedure parameters.
- For all tabular lists, grids, and paginated searches, strictly use existing `JqDataTableModel` and `SearchModel` from `TablePagging.cs`.
- Include XML documentation comments (`/// <summary>`) on all classes and properties.

#### 4. Controllers & APIs (`Controllers/`)
- **Thin Controller Architecture:** Controllers only handle request routing, model validation, session authentication, and returning responses.
- **Business Logic in BAL:** All orchestration and logic belongs in `PCAccess.BAL/`, never inside the controller.
- **Standardized API & Action Response:**
  - Every API endpoint or AJAX action must return:
    ```csharp
    // REASON: Adheres to standardized project JSON envelope pattern
    return Json(new { 
        status = "success", // or "error"
        msg = "Operation message", 
        data = resultData 
    }, JsonRequestBehavior.AllowGet);
    ```
- **Authentication:** Must strictly use `CommanUtilities.Provider.GetCurrent()`.
- **Exception Logging:** Must catch unhandled exceptions and log using `ExceptioLoggerDB`.

#### 5. Stored Procedures (`Doc/Procedure/`)
- Stored in individual `.sql` files under `Doc/Procedure/` with `USE [PCAccess_DB] GO`.
- Standard naming: `proc_<entity>_<action>`.
- Mandatory parameters:
  - `@ActionType INT = 1`
  - `@status VARCHAR(50) = NULL OUTPUT`
  - `@msg NVARCHAR(255) = NULL OUTPUT`
- Standard structured `BEGIN TRY ... BEGIN TRANSACTION ... COMMIT ... END TRY BEGIN CATCH ... ROLLBACK ... END CATCH` error handling.
- Header comments must document purpose, input/output parameters, action types, and calling BAL methods.

#### 6. Database Tables (`Doc/Table/`)
- Stored in individual `.sql` files under `Doc/Table/` with `USE [PCAccess_DB] GO`.
- Standard naming: `tbl_<entity>.sql`.
- Standard audit fields: `id INT IDENTITY(1,1) PRIMARY KEY`, `created_at DATETIME DEFAULT GETDATE()`, `created_by INT`, `is_active BIT DEFAULT 1`, `status VARCHAR(50)`.


### 5.5 Mandatory Continuous Documentation in Doc/ (For Team & Management)
- **Every step MUST maintain and update `Doc/PCAccess_Step_By_Step_Guide_And_NuGets.md`.**
- Must document:
  - All NuGet packages installed/modified with versions, role, and configuration.
  - Step breakdown with business and architectural reasons.
  - 5-minute management and team demonstration script.
  - Continuous synchronization between `Doc/` and `CursorRule/`.

### 5.4 Pre-Implementation Verification Checklist
Before writing or implementing any new code:
1. Identify the existing reference file in that layer.
2. Verify exact signature, naming, and pattern compatibility.
3. Verify that no arbitrary or self-invented pattern is introduced.
4. Add clear comments explaining WHAT and WHY (Reason).
5. Verify build with MSBuild on .NET Framework 4.8 (`0 Errors`).
6. Update documentation in `CursorRule/`.

---

## 6. Step 1: File Access System Dashboard UI Design

### 6.1 Purpose and Architectural Scope
The File Access System Dashboard serves as the central control surface where authenticated users manage and navigate remote workstations, laptops, and shared directories.
In accordance with Step 1 specifications:
- **Zero Backend/API/Database/SignalR changes:** Pure presentation layer utilizing mock visual placeholders designed for seamless future binding with SignalR hubs and REST endpoints.
- **Strict Design System Reuse:** Built exclusively using the project's existing Tailwind CSS configuration, `Manrope` typography, Lucide icons, and primary emerald brand palette (`#1f9251`).

### 6.2 Component Hierarchy & Views
- **Target View:** `Views/admin/DashboardOverview.cshtml` (Rendered via `_Admin_Layout.cshtml`).
- **Sidebar Navigation:** `Views/Shared/_Menu.cshtml` (Updated with File Access navigation links: Dashboard, Connected Devices, Shared Folders, Recent Activity, Profile).

#### 1. Header Canvas
- Page title (`Dashboard`) with contextual micro-description: *"Manage and access your connected devices and shared files."*
- **Interactive State Preview Switcher:** A non-intrusive toolbar button (`btnToggleState`) enabling reviewers and developers to toggle seamlessly between populated mock data and clean empty states without code modifications.

#### 2. Summary Metrics (4-Column Adaptive Grid)
- **Connected Devices:** Total authorized devices count with a live animated green ping indicator showing active online instances.
- **Shared Folders:** Total accessible network folders aggregated across all authorized endpoints.
- **Available Files:** Total indexed files ready for remote browsing.
- **Recent Activity:** Quick count of operational events in the last 24 hours.

#### 3. Connected Devices Section
- **Desktop/Tablet Layout (`hidden sm:block`):** Full-width professional table showing:
  - Device Name & OS/Machine Type with hardware icons (`monitor`, `laptop`).
  - Real-time Status Badge (`Online` with green indicator, `Offline` with neutral slate badge).
  - Shared Folders counter badge.
  - Last seen timestamp.
  - Primary Action Button: Emerald `Open` for active devices, slate `View` for offline endpoints.
- **Mobile Card Layout (`block sm:hidden`):** Touch-optimized card list showing device metadata, status badge, and full-width touch actions without horizontal scrolling or squashed text.

#### 4. Shared Folders Section (Left Column)
- Clean folder list displaying:
  - Folder Name with emerald folder icon.
  - Host PC / Device origin tag.
  - File count and last accessed time.
  - Action trigger (`Browse`).

#### 5. Recent Activity Feed (Right Column)
- Compact timeline feed showing:
  - Specific action icons (download, folder access, device connection, upload).
  - Target resource and host device context tags.
  - Relative timestamps (`10m ago`, `1h ago`, etc.).
  - Status indicator pills (`Completed`, `Active`).

#### 6. Empty State Design
- Clean, uncluttered zero-data fallback states:
  - *No Connected Devices:* Guidance prompt to connect an agent or configure a PC.
  - *No Shared Folders:* Clear instructions to share directories from connected devices.
  - Actionable buttons to add devices or refresh connections.

### 6.3 Responsive Breakpoint Matrix
| Breakpoint | Summary Metrics | Connected Devices | Bottom Section (Folders & Activity) |
|---|---|---|---|
| **Mobile (`< 640px`)** | 1 Column | Touch Cards (No horizontal overflow) | Single Column Stack |
| **Tablet (`640px - 1024px`)** | 2 Columns | Full-Width Table (Optimized padding) | Stacked or Adaptive Grid |
| **Laptop / Desktop (`>= 1024px`)** | 4 Columns | Full-Width Table | 2-Column Split (50% / 50%) |

---

## 7. Step 2: PC Agent + Secure Server Connection

### 7.1 Purpose & Architectural Scope
Step 2 establishes the end-to-end communication foundation between the local Windows computer running the C# File Access Agent and the ASP.NET MVC / SignalR server.
- **Zero File Operations:** No file browsing, upload, download, or streaming is implemented in this step.
- **Focus:** Device identity, 6-digit pairing flow, SignalR real-time messaging, 30-second heartbeat, offline detection, and dashboard real-time DOM reflection.

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

### 7.2 Database Layer
- **Tables:**
  - `Doc/Table/tbl_device.sql`: Stores permanent `device_guid`, `device_name`, `user_id`, `device_token`, `status` ('online'/'offline'), `connection_id`, `last_seen`.
  - `Doc/Table/tbl_device_pairing.sql`: Stores temporary 6-digit pairing codes with a 10-minute expiry (`expires_at`) and one-time use flag (`is_used`).
- **Stored Procedure:**
  - `Doc/Procedure/proc_device_manager.sql`:
    - `@ActionType = 1`: Generate & save 6-digit pairing code for authenticated user.
    - `@ActionType = 2`: Validate pairing code & register new device (issues `device_token`).
    - `@ActionType = 3`: Authenticate existing device using `device_guid` + `device_token`.
    - `@ActionType = 4`: Update online/offline status and SignalR `connection_id`.
    - `@ActionType = 5`: Record 30-second heartbeat (`last_seen`).
    - `@ActionType = 6`: Get user devices list for dashboard.
    - `@ActionType = 7`: Mark stale devices offline (> 90 seconds without heartbeat).

### 7.3 Backend & SignalR Server Architecture
- **OWIN Pipeline (`App_Start/Startup.cs`):**
  - Mapped via `[assembly: OwinStartup(typeof(PCAccess.Startup))]` and `app.MapSignalR()`.
- **Hub (`Hub/DeviceHub.cs`):**
  - Inherits from `Microsoft.AspNet.SignalR.Hub`.
  - Maintains concurrent connection mapping for instant disconnect detection.
  - `JoinDashboardGroup(userId)`: Adds browser client to private `User_{userId}` group.
  - `RegisterAgent(deviceGuid, token)`: Marks device `online` in database and broadcasts `deviceStatusChanged(deviceGuid, "online", "Just now")` to dashboard.
  - `SendHeartbeat(deviceGuid)`: Updates `last_seen` in database.
  - `OnDisconnected(stopCalled)`: Automatically marks device `offline` in database and broadcasts status change to dashboard.
- **Controller & BAL (`Controllers/DeviceAPIController.cs` & `BAL/DeviceManager.cs`):**
  - Follows existing `AccountAPIController` pattern returning standardized JSON envelopes.
  - Endpoints:
    - `POST /DeviceAPI/GeneratePairingCode` (Authenticated web session)
    - `POST /DeviceAPI/Register` (Agent first-time pairing)
    - `POST /DeviceAPI/Verify` (Agent credential verification)
    - `GET /DeviceAPI/GetDevices` (User device list)

### 7.4 C# File Access Agent (`FileAccessAgent/`)
- Included in `PCAccess.sln` as a lightweight .NET console project.
- **Interactive First-Run Pairing:**
  - Prompts for Server URL and 6-digit Pairing Code generated from Dashboard.
  - Generates permanent `DeviceGuid` and retrieves secret `DeviceToken`.
  - Persists settings locally in `agent_config.json`.
- **Subsequent Runs:**
  - Automatically loads `agent_config.json` without re-prompting.
  - Connects to SignalR Hub `/signalr`.
  - Registers device and receives `ONLINE` status confirmation.
- **Heartbeat & Reconnection:**
  - Sends ping every 30 seconds via `SendHeartbeat`.
  - Automatic reconnection with exponential retry backoff if network or server drops.
  - Graceful shutdown on `Ctrl+C` immediately notifying the server.

### 7.5 Dashboard Real-Time Integration (`dashboard.js`)
- Replaces mock device data with real database records rendered dynamically in `DashboardOverview.cshtml`.
- Connects to `DeviceHub` and listens for `deviceStatusChanged(data)`.
- Instantly updates status badges (emerald ping pulse for `Online`, neutral badge for `Offline`) and Last Seen text without requiring a page refresh.
- Provides interactive "Connect PC" modal with 6-digit pairing code and countdown timer.

### 7.6 Native .NET Framework 4.8 Unification & Windows Service Architecture
- **Complete .NET Framework 4.8 Alignment:**
  - `FileAccessAgent.csproj` targets `.NET Framework 4.8 (AnyCPU)` matching the ASP.NET MVC web portal.
  - Zero .NET Core / .NET 10 dependencies: Eliminates runtime missing errors, architecture mismatch (`x86` vs `x64`), `hostpolicy.dll`, and `runtimeconfig.json` requirements.
  - Natively supported out-of-the-box on every Windows 10 and Windows 11 installation without requiring user runtime installations.
- **Native ServiceBase Implementation (`AgentWindowsService.cs`):**
  - Inherits from `System.ServiceProcess.ServiceBase` without external hosting packages.
  - Dispatches `OnStart`, `OnStop`, and `OnShutdown` cleanly with cancellation token coordination.
- **Classic NuGet & Assembly Reference Pattern:**
  - Uses solution-level `packages.config` and explicit `<Reference Include="..."><HintPath>..\packages\...</HintPath></Reference>` matching `PCAccess.csproj`.
  - Avoids SDK-style NuGet target conflicts (`Microsoft.NuGet.targets` TFM mismatch errors).
- **Service Control Scripts:**
  - `start-service.bat`: Starts Windows Service under Administrator elevation.
  - `stop-service.bat`: Stops Windows Service under Administrator elevation.
  - `check-status.bat`: Queries service status (`FileAccessAgent.exe --status`) and outputs latest logs.
  - `install-service.bat`: Registers Windows Service with automatic boot startup.
  - `uninstall-service.bat`: Safely unregisters and removes the Windows Service.



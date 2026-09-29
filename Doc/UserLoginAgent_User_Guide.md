# PCAccess — UserLoginAgent Complete User & Command Guide

**Target Application:** `UserLoginAgent.exe` (Desktop Remote Drive Client)  
**Location:** `e:\Git\PCAccess\UserLoginAgent\` & `e:\Git\PCAccess\UserLoginAgent\bin\Debug\`  
**Target Audience:** Users, Employees, Administrators & Technical Team  

---

## 1. Overview (Agent Kya Hai Aur Kyun Hai?)

**`UserLoginAgent`** ek lightweight Windows desktop console application hai jo Admins aur Employees ko bina browser open kiye direct unke permitted shared folders tak access deti hai.

Jab user login karta hai:
1. Ye system me virtual drive **`R:` ("Remote Drive")** ko automatically mount kar deta hai.
2. Windows Explorer ("This PC") me **`R:` Drive** dikhne lagti hai.
3. User ke assigned sabhi shared folders `R:` drive ke andar shortcuts/folders ke roop me appear hote hain.
4. User terminal se hi Status dekh sakta hai, Refresh kar sakta hai, aur jab chahe Logout/Stop kar sakta hai.

---

## 2. Interactive Terminal Commands (Console Chalte Samay Kya Dabayein?)

Jab `UserLoginAgent` console window chal rahi ho, tab aap keyboard se directly single key press karke actions perform kar sakte hain:

| Key | Command Name | Meaning (Matlab) | Exact Action (Kya Dabane Pe Kya Hoga) |
|:---:|:---|:---|:---|
| **`S`** | **Session Status** | Current Session & Health Check | **User Details & Drive Status Dikhayega:**<br>• Logged-in Username & Email<br>• Active Role (`Admin` ya `User`)<br>• Remote Drive (`R:`) Mounted hai ya nahi<br>• Accessible Folders ki count<br>• Server URL aur connection status terminal me print karega. |
| **`O`** | **Open Drive** | Open in Windows Explorer | **Explorer me `R:\` Drive Khol Dega:**<br>• User ko "This PC" me manually jaane ki zaroorat nahi padegi, direct Windows File Explorer popup hoga aur `R:\` khul jayega. |
| **`R`** | **Refresh Folders** | Re-fetch Shared Folders | **Naye Folders Sync Karega:**<br>• Agar Admin ne web portal se user ko koi naya folder assign kiya hai, to bina restart kiye server se latest folder list fetch karega aur `R:` drive me add kar dega. |
| **`C`** | **Switch User** | Clear Credentials & Change Account | **Account Switch Karega:**<br>• Current `R:` drive unmount karega.<br>• Saved password aur username memory/file se clear karega.<br>• Console band kiye bina dobara Login prompt dega taaki doosra user (e.g. Admin ya Rahul) login kar sake. |
| **`L`** ya **`Q`** ya **`X`** | **Logout & Stop** | Clean Disconnect & Exit | **Drive Hataega aur Exit Karega:**<br>• Virtual Drive `R:` ko Windows se cleanly unmount karega.<br>• Explorer shell ko notify karega taaki stale drive icon na bache.<br>• Console window safely close ho jayegi. |
| **`Ctrl + C`** | **Force Terminate** | Graceful Exit | **Safe Cleanup Trigger Karega:**<br>• Process exit hone se pehle `R:` drive unmount karega aur safely band ho jayega. |

---

## 3. Batch Scripts & CLI Commands (Terminal / Background Controls)

Agar console window minimize ho ya background me chal rahi ho, ya aap scripts se control karna chahein, to folder me ye batch files available hain:

| File / Command | CLI Syntax | Meaning (Matlab) | Kya Action Hoga |
|:---|:---|:---|:---|
| **`run.bat`** | `UserLoginAgent.exe` | **Start Agent** | UserLoginAgent ko launch karta hai, login prompt deta hai aur credentials verify hone par `R:` drive mount karta hai. |
| **`status.bat`** | `UserLoginAgent.exe status` | **Check Status** | Bina running agent ko disturb kiye status check karta hai:<br>1. Kya agent process running hai?<br>2. Kya Drive `R:` mounted hai?<br>3. Server live hai ya offline?<br>4. Last logged-in user kaun tha? |
| **`logout.bat`** | `UserLoginAgent.exe logout` | **Logout & Unmount** | 1. Virtual Drive `R:` ko turant unmount/remove karta hai.<br>2. Running `UserLoginAgent` process ko terminate karke user ko cleanly log out kar deta hai. |
| **`stop.bat`** | `UserLoginAgent.exe stop` | **Stop Service/Agent** | `logout.bat` ka alternate alias — drive unmount karke agent ko stop karta hai. |

---

## 4. Console Screen Samples (Output Kaisa Dikhega?)

### A. Login Success Output
```text
========================================================================
 [SUCCESS] Welcome, Rahul Sharma! Logged in as: User
========================================================================
User ID  : 2
Email    : rahul@pcaccess.com
Role     : User
Folders  : 1 accessible shared folder(s)

---------------------------------------------------------------------------------------------------
#   | Folder Name          | Host Device      | Status     | Permissions              
---------------------------------------------------------------------------------------------------
1   | Company Files        | Sonu-PC          | OFFLINE    | View=✓ Download=✓ Upload=✗
---------------------------------------------------------------------------------------------------

[REMOTE DRIVE] Mounting virtual drive R: ("Remote Drive")...
[SUCCESS] Drive R: successfully mounted and active in Windows Explorer!

------------------------------------------------------------------------
 [STATUS] User Session Active & Remote Drive (R:) Mounted.
 Commands:
   [S] - Status: View session, drive, and connection health
   [O] - Open: Browse Remote Drive (R:) in Windows Explorer
   [R] - Refresh: Re-fetch shared folders from server
   [C] - Switch User: Clear credentials and log in as another user
   [L] or [Q] - Logout: Stop agent, unmount drive, and Exit
------------------------------------------------------------------------
```

### B. `[S]` Key dabane par Inline Status Output
```text
------------------------------------------------------------------------
                     CURRENT SESSION STATUS                             
------------------------------------------------------------------------
 User Name     : Rahul Sharma
 Email / ID    : rahul@pcaccess.com (ID: 2)
 Active Role   : User
 Remote Drive  : R: [MOUNTED & READY]
 Folders Count : 1 accessible folder(s)
 Server URL    : https://localhost:44355/
------------------------------------------------------------------------
```

### C. `[L]` ya `[Q]` dabane par Logout Output
```text
[INFO] Logout & Exit requested by user.
[INFO] Unmounting Remote Drive...
[REMOTE DRIVE] Drive R: unmounted cleanly.

[SUCCESS] Logged out successfully. Remote Drive unmounted.
Exiting program in 2 seconds...
```

### D. `status.bat` chalane par Output
```text
========================================================================
                 PCAccess User Login Agent - Status Check
========================================================================
 Agent Process     : [RUNNING] (PID: 14220)
 Remote Drive (R:) : [MOUNTED & ACTIVE]
 Configured Server : https://localhost:44355/
 Last User Login   : rahul
 Last Login Date   : 2026-09-28 18:35:02
 Drive Label       : Remote Drive
 Server Reachable  : [ONLINE]
========================================================================
```

### E. `logout.bat` chalane par Output
```text
[ACTION] Stopping UserLoginAgent and Unmounting Remote Drive...
[REMOTE DRIVE] Drive R: unmounted cleanly.
========================================================================
 [SUCCESS] Remote Drive (R:) unmounted cleanly.
 [SUCCESS] 1 running UserLoginAgent process(es) terminated.
 [STATUS] User session logged out successfully.
========================================================================
```

---

## 5. Roles & Access Rules

1. **Admin (`user_type_id = 1`):**
   - System me active sabhi shared folders access kar sakta hai.
   - Display role: `Admin`.
2. **User / Employee (`user_type_id = 2`):**
   - Sirf wahi folders access kar sakta hai jisme `can_view = 1` ki permission Admin ne di ho.
   - Display role: `User`.

---

## 6. Files Location & Distribution

- **Executable:** `e:\Git\PCAccess\UserLoginAgent\bin\Debug\UserLoginAgent.exe`
- **Config File:** `e:\Git\PCAccess\UserLoginAgent\bin\Debug\user_agent_config.json`
- **Batch Scripts:**
  - `run.bat`
  - `status.bat`
  - `logout.bat`
  - `stop.bat`
- **Virtual Drive Mount Point:** `R:\` (Label: `Remote Drive`)

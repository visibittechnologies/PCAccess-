# PCAccess - Setup & Login Execution Guide

Follow these simple steps to run the database scripts, start the application in Visual Studio, and log in to the Dashboard.

---

## Step 1: Database Setup in SQL Server

1. Open **SQL Server Management Studio (SSMS)**.
2. Connect to your local SQL Server instance:
   - Server Name: `(localdb)\VSDigiCRM2025` (or your configured SQL Server)
3. Create the database:
   ```sql
   CREATE DATABASE [PCAccess_DB];
   GO
   ```
4. Run the database scripts located in `E:\Git\PCAccess\Doc\`:
   - First, run all scripts in `E:\Git\PCAccess\Doc\Table\` to create tables (`tbl_user`, `tbl_user_type`, `tbl_user_loginfo`, etc.).
   - Next, run all scripts in `E:\Git\PCAccess\Doc\Procedure\` to create stored procedures (`proc_user_login.sql`, `ua_user_loginfo_iud.sql`, etc.).
   - Next, run scripts in `E:\Git\PCAccess\Doc\Function\`.
5. Run the Seed Data script:
   - Execute `E:\Git\PCAccess\Doc\Seed_Data_Admin_User.sql`.
   - This sets up:
     - User Type `Admin` with `module_url = '/Admin/DashboardOverview'`
     - User `admin` with Password `123` and status `Active`

---

## Step 2: Open and Run in Visual Studio

1. Open **Visual Studio 2022**.
2. Go to **File -> Open -> Project/Solution** and select:
   ```text
   E:\Git\PCAccess\PCAccess.sln
   ```
3. Build the solution:
   - Press **Ctrl + Shift + B** (or **Build -> Rebuild Solution**).
   - Expected Output: `Rebuild All: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped`.
4. Run the application:
   - Press **F5** (Debug) or **Ctrl + F5** (Run without debugging).
   - IIS Express launches automatically at `https://localhost:44355/`.
   - The default route immediately opens the Login page:
     ```text
     https://localhost:44355/Account/Login
     ```

---

## Step 3: Login Verification

1. On the Login screen:
   - **Username:** `admin`
   - **Password:** `123`
   - Check **Remember Me** (optional)
2. Click **Log in**.
3. A sweetalert success message will display and you will be redirected straight to:
   ```text
   https://localhost:44355/Admin/DashboardOverview
   ```

---

## Troubleshooting Quick Reference

| Issue | Cause | Fix |
|---|---|---|
| `Cannot open database "PCAccess_DB"` | Database not created in SQL Server | Run `CREATE DATABASE [PCAccess_DB];` in SSMS |
| `Invalid User` or `Invalid password` | Admin user not seeded | Execute `Doc\Seed_Data_Admin_User.sql` in SSMS |
| `Uncaught ReferenceError: $ is not defined` | Missing jQuery | `Scripts/jquery-3.7.0.min.js` is already copied to `Scripts/` |
| `Configuration Error / CodeDom` | Missing Roslyn compiler | Already fixed; `system.codedom` removed from `Web.config` |

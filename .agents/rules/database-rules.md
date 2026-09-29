# Database Operations Policy - PCAccess

## STRICT DIRECTIVE: NO AUTOMATED DATABASE EXECUTION
Under NO circumstances should the AI agent execute any SQL scripts, queries, migrations, or commands directly against any database (neither Live/Production DB nor Local DB).

### Mandatory Rules:
1. **NEVER run automated DB commands:**
   - Do NOT run `sqlcmd`, PowerShell SQL commands, connection scripts, or any automated tools to execute SQL on Local or Live DB.
   - The user will execute all database scripts **MANUALLY** in Visual Studio or SQL Server Management Studio (SSMS).

2. **File-Only Database Changes:**
   - All table definitions must be written to `Doc/Table/tbl_<entity>.sql`.
   - All stored procedures must be written to `Doc/Procedure/proc_<entity>_<action>.sql`.
   - Every script must start with `USE [PCAccess_DB]` and `GO`.
   - Always follow the existing SP contract:
     - `@ActionType INT = 1`
     - `@status VARCHAR(50) = NULL OUTPUT`
     - `@msg NVARCHAR(255) = NULL OUTPUT`
     - Transactional `BEGIN TRY ... BEGIN TRANSACTION ... COMMIT ... END TRY BEGIN CATCH ... ROLLBACK ... END CATCH`.

3. **User Notification & Delivery:**
   - When a database change is needed, the agent creates or updates the `.sql` file in `Doc/Table/` or `Doc/Procedure/`.
   - The agent provides the exact file path and clear manual execution instructions to the user.
   - The user handles running it on Local DB and deploying to Live DB.

# UserGuide Synchronization Policy - PCAccess

## CRITICAL DIRECTIVE: MANDATORY USERGUIDE UPDATES
Whenever ANY change, enhancement, bugfix, or new feature is made in the PCAccess solution, the corresponding documentation in the `UserGuide/` folder **MUST** be updated immediately.

### Rules & Workflow:
1. **Always Update Documentation on Change:**
   - Any modification to `UserLoginAgent` (commands, keybindings, authentication flow, drive management) ➔ update `UserGuide/1_UserLoginAgent_User_Guide.html` & `.pdf`.
   - Any modification to `FileAccessAgent` (pairing, service commands, background daemon, SignalR) ➔ update `UserGuide/2_FileAccessAgent_And_Comparison_Guide.html` & `.pdf`.
   - Any architectural, technological, or systemic change (APIs, Database, UI, Security, Workflow) ➔ update `UserGuide/3_PCAccess_Full_Project_Presentation.html` & `.pdf`.

2. **Both HTML and PDF Formats:**
   - Always maintain the source HTML and re-generate the PDF using Chrome/Edge headless (`--headless=new --print-to-pdf`).
   - Ensure PDF files remain clean, printable, professional, and accessible directly from `UserGuide/`.

3. **Clarity on Commands & Actions:**
   - Every command or keybinding must clearly explain:
     - **Key / Command:** Exactly what is typed or pressed.
     - **Meaning:** What the command represents.
     - **Exact Action:** "Kya dabane pe kya action hoga" in clear, unambiguous Hindi/English.

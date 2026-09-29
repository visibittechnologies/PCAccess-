USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
================================================================================
TABLE: [dbo].[tbl_device_pairing]
AUTHOR: Antigravity / Senior .NET Developer
DATE: 2026-09-22
PURPOSE:
  Stores temporary 6-digit pairing codes generated from the web dashboard.
  Enables secure, user-friendly pairing between the Windows PC Agent and the user's account.

REASON & ARCHITECTURAL PATTERN:
  - Follows standard temporary security token pattern (like AnyDesk / TeamViewer pairing).
  - Pairing codes have a 10-minute expiry and can only be used once (is_used = 1).
  - Eliminates the need to send raw user passwords to the desktop agent.
================================================================================
*/

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_device_pairing]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_device_pairing](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[pairing_code] [varchar](10) NOT NULL,
	[user_id] [bigint] NOT NULL,
	[expires_at] [datetime] NOT NULL,
	[is_used] [bit] NOT NULL CONSTRAINT [DF_tbl_device_pairing_is_used] DEFAULT ((0)),
	[created_at] [datetime] NOT NULL CONSTRAINT [DF_tbl_device_pairing_created_at] DEFAULT (getdate()),
 CONSTRAINT [PK_tbl_device_pairing] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO

-- Index for rapid pairing code verification
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_device_pairing_code' AND object_id = OBJECT_ID('tbl_device_pairing'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_tbl_device_pairing_code] ON [dbo].[tbl_device_pairing] ([pairing_code], [is_used], [expires_at]);
END
GO

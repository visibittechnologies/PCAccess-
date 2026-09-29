USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
================================================================================
TABLE: [dbo].[tbl_device]
AUTHOR: Antigravity / Senior .NET Developer
DATE: 2026-09-22
PURPOSE:
  Stores registered remote PCs/laptops associated with users in the File Access System.
  Maintains permanent DeviceGuid, friendly DeviceName, secure authentication token,
  real-time online/offline status, SignalR ConnectionId, and LastSeen timestamp.

REASON & ARCHITECTURAL PATTERN:
  - Adheres strictly to the project's existing table naming pattern (tbl_<entity>).
  - Follows SQL Server table conventions with IDENTITY(1,1) primary key.
  - DeviceGuid is persistent and unique per agent installation.
  - Status ('online' / 'offline') and LastSeen provide real-time connection state.
================================================================================
*/

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_device]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_device](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[device_guid] [uniqueidentifier] NOT NULL,
	[device_name] [nvarchar](150) NOT NULL,
	[user_id] [bigint] NOT NULL,
	[device_token] [nvarchar](256) NOT NULL,
	[status] [varchar](20) NOT NULL CONSTRAINT [DF_tbl_device_status] DEFAULT ('offline'),
	[connection_id] [nvarchar](100) NULL,
	[last_seen] [datetime] NOT NULL CONSTRAINT [DF_tbl_device_last_seen] DEFAULT (getdate()),
	[created_at] [datetime] NOT NULL CONSTRAINT [DF_tbl_device_created_at] DEFAULT (getdate()),
	[updated_at] [datetime] NULL,
	[is_active] [bit] NOT NULL CONSTRAINT [DF_tbl_device_is_active] DEFAULT ((1)),
 CONSTRAINT [PK_tbl_device] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
 CONSTRAINT [UQ_tbl_device_guid] UNIQUE NONCLUSTERED 
(
	[device_guid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END
GO

-- Index for fast user device lookups on dashboard
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_device_user_id' AND object_id = OBJECT_ID('tbl_device'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_tbl_device_user_id] ON [dbo].[tbl_device] ([user_id], [is_active]);
END
GO

-- Index for status filtering and heartbeat checks
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_device_status' AND object_id = OBJECT_ID('tbl_device'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_tbl_device_status] ON [dbo].[tbl_device] ([status], [last_seen]);
END
GO

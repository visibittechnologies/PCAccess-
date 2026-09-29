USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_user_role]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_user_role](
	[user_role_id] [int] NOT NULL,
	[user_role_name] [varchar](50) NULL,
	[is_system] [bit] NULL,
	[created_by] [bigint] NULL,
	[created_on] [datetime] NULL,
	[modified_by] [bigint] NULL,
	[modified_on] [datetime] NULL,
 CONSTRAINT [PK_tbl_user_role] PRIMARY KEY CLUSTERED 
(
	[user_role_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[tbl_user_role] ADD  CONSTRAINT [DF_tbl_user_role_is_system]  DEFAULT ((1)) FOR [is_system]
ALTER TABLE [dbo].[tbl_user_role] ADD  CONSTRAINT [DF_tbl_user_role_created_on]  DEFAULT (getdate()) FOR [created_on]
ALTER TABLE [dbo].[tbl_user_role] ADD  CONSTRAINT [DF_tbl_user_role_modified_on]  DEFAULT (getdate()) FOR [modified_on]
END
GO


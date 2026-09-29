USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_user_type]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_user_type](
	[user_type_id] [int] NOT NULL,
	[user_type] [varchar](50) NULL,
	[user_level] [int] NOT NULL,
	[module_url] [nvarchar](250) NULL,
 CONSTRAINT [PK_tbl_user_type] PRIMARY KEY CLUSTERED 
(
	[user_type_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO


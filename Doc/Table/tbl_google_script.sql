USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_google_script]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_google_script](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ScriptContent] [nvarchar](max) NULL,
	[is_deleted] [bit] NULL,
	[created_by] [bigint] NULL,
	[created_date] [datetime] NULL,
	[modified_by] [bigint] NULL,
	[updated_date] [datetime] NULL,
 CONSTRAINT [PK_tbl_google_script] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END
GO


USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_user_loginfo]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_user_loginfo](
	[user_loginfo_id] [numeric](18, 0) NOT NULL,
	[user_id] [numeric](18, 0) NULL,
	[ip_address] [varchar](50) NULL,
	[login_date] [datetime] NULL,
	[login_token] [nvarchar](50) NULL,
	[log_type] [int] NULL,
	[module_url] [nvarchar](250) NULL,
	[module_name] [nvarchar](150) NULL,
	[description] [nvarchar](500) NULL,
	[browser] [nvarchar](500) NULL,
	[city] [nvarchar](500) NULL,
	[country] [nvarchar](500) NULL,
 CONSTRAINT [PK_tbl_user_loginfo] PRIMARY KEY CLUSTERED 
(
	[user_loginfo_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO


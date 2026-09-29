USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_page_seo_meta_tags]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_page_seo_meta_tags](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[page_name] [nvarchar](350) NULL,
	[page_url] [nvarchar](max) NULL,
	[meta_title] [nvarchar](max) NULL,
	[meta_description] [nvarchar](max) NULL,
	[meta_keywords] [nvarchar](max) NULL,
	[is_deleted] [bit] NULL,
	[created_by] [bigint] NULL,
	[created_date] [datetime] NULL,
	[modified_by] [bigint] NULL,
	[updated_date] [datetime] NULL,
 CONSTRAINT [PK_tbl_page_seo_meta_tags] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END
GO


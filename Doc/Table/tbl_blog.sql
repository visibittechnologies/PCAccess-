USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_blog]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_blog](
	[blog_id] [bigint] IDENTITY(1,1) NOT NULL,
	[user_id] [bigint] NULL,
	[title] [varchar](max) NULL,
	[blog_type] [nvarchar](max) NULL,
	[blog_img] [varchar](max) NULL,
	[short_description] [varchar](max) NULL,
	[long_description] [varchar](max) NULL,
	[meta_title] [varchar](max) NULL,
	[meta_description] [varchar](max) NULL,
	[meta_keywords] [varchar](max) NULL,
	[is_deleted] [bit] NULL,
	[created_by] [bigint] NULL,
	[created_date] [datetime] NULL,
	[modified_by] [bigint] NULL,
	[updated_date] [datetime] NULL,
	[category] [nvarchar](max) NULL,
	[author] [nvarchar](max) NULL,
	[status] [nvarchar](max) NULL,
	[scheduled_date] [nvarchar](max) NULL,
	[crop_name] [nvarchar](255) NULL,
	[mandi_name] [nvarchar](255) NULL,
	[price] [nvarchar](100) NULL,
	[price_change] [nvarchar](100) NULL,
	[location] [nvarchar](255) NULL,
	[view_count] [bigint] NOT NULL,
 CONSTRAINT [PK_tbl_blog] PRIMARY KEY CLUSTERED 
(
	[blog_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [dbo].[tbl_blog] ADD  DEFAULT ((0)) FOR [view_count]
END
GO


USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_company_profile]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_company_profile](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[user_id] [bigint] NULL,
	[company_name] [nvarchar](350) NULL,
	[email] [nvarchar](350) NULL,
	[phone_no] [nvarchar](100) NULL,
	[whats_app_number] [nvarchar](100) NULL,
	[address] [nvarchar](max) NULL,
	[coprate_address] [nvarchar](max) NULL,
	[title] [nvarchar](max) NULL,
	[google_map_url] [nvarchar](max) NULL,
	[facebook_url] [nvarchar](max) NULL,
	[twitter_url] [nvarchar](max) NULL,
	[linkedin_url] [nvarchar](max) NULL,
	[instagram_url] [nvarchar](max) NULL,
	[youtube_url] [nvarchar](max) NULL,
	[contact_person] [nvarchar](200) NULL,
	[secondary_phone] [nvarchar](50) NULL,
	[fax_no] [nvarchar](50) NULL,
	[pinterest_url] [nvarchar](max) NULL,
	[reddit_url] [nvarchar](max) NULL,
	[tumblr_url] [nvarchar](max) NULL,
	[meta_keywords] [nvarchar](max) NULL,
	[show_in_common] [bit] NULL,
	[enable_public_links] [bit] NULL,
	[require_manual_review] [bit] NULL,
	[company_logo] [nvarchar](max) NULL,
	[privacy_policy_file] [nvarchar](max) NULL,
	[terms_conditions_file] [nvarchar](max) NULL,
	[is_deleted] [bit] NULL,
	[created_by] [bigint] NULL,
	[created_date] [datetime] NULL,
	[modified_by] [bigint] NULL,
	[updated_date] [datetime] NULL,
 CONSTRAINT [PK_tbl_company_profile] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END
GO


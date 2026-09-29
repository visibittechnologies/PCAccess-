USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_user]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_user](
	[user_id] [bigint] IDENTITY(1,1) NOT NULL,
	[user_type_id] [int] NULL,
	[user_role_id] [int] NULL,
	[user_name] [varchar](max) NULL,
	[password] [nvarchar](max) NULL,
	[name] [nvarchar](350) NULL,
	[father_name] [nvarchar](350) NULL,
	[email] [nvarchar](350) NULL,
	[phone_no] [nvarchar](350) NULL,
	[alternate_phone_no] [nvarchar](350) NULL,
	[date_of_birth] [datetime] NULL,
	[gender_id] [int] NULL,
	[marital_status_id] [int] NULL,
	[status] [varchar](100) NULL,
	[is_advisor] [bit] NULL,
	[relation_type] [nvarchar](20) NULL,
	[is_deleted] [bit] NULL,
	[IsEmailVarified] [int] NULL,
	[IsProfileComplete] [int] NULL,
	[IsTempPasswordUpdate] [int] NULL,
	[created_by] [bigint] NULL,
	[created_date] [datetime] NOT NULL,
	[modified_by] [bigint] NULL,
	[modified_date] [datetime] NULL,
	[LastIpAddress] [varchar](150) NULL,
	[LastLoginDate] [datetime] NULL,
	[LastActivityDate] [datetime] NULL,
	[RequireReLogin] [bit] NULL,
	[profile_photo] [nvarchar](max) NULL,
 CONSTRAINT [PK_tbl_user_1] PRIMARY KEY CLUSTERED 
(
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [dbo].[tbl_user] ADD  CONSTRAINT [DF__tbl_user__is_adv__41EDCAC5]  DEFAULT ((0)) FOR [is_advisor]
END
GO


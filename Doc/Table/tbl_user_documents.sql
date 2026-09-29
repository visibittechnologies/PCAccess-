USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_user_documents]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_user_documents](
	[document_id] [bigint] IDENTITY(1,1) NOT NULL,
	[user_id] [bigint] NULL,
	[aadhaar_number] [nvarchar](20) NULL,
	[aadhaar_front] [nvarchar](max) NULL,
	[aadhaar_back] [nvarchar](max) NULL,
	[pan_number] [nvarchar](20) NULL,
	[pan_image] [nvarchar](max) NULL,
	[profile_photo] [nvarchar](max) NULL,
	[contact_form] [nvarchar](max) NULL,
	[offer_letter] [nvarchar](max) NULL,
	[appointment_letter] [nvarchar](max) NULL,
	[relieving_letter] [nvarchar](max) NULL,
	[is_deleted] [bit] NULL,
	[created_by] [bigint] NULL,
	[created_date] [datetime] NULL,
	[modified_by] [bigint] NULL,
	[modified_date] [datetime] NULL,
	[voter_id] [nvarchar](50) NULL,
	[voter_image] [nvarchar](max) NULL,
	[electricity_number] [nvarchar](50) NULL,
	[electricity_bill] [nvarchar](max) NULL,
	[bank_account] [nvarchar](50) NULL,
	[bank_passbook] [nvarchar](max) NULL,
	[account_holder_name] [nvarchar](150) NULL,
	[bank_name] [nvarchar](150) NULL,
	[bank_ifsc_code] [nvarchar](20) NULL,
	[bank_branch_name] [nvarchar](150) NULL,
	[resume] [nvarchar](500) NULL,
	[prev_company_name] [nvarchar](500) NULL,
	[prev_designation] [nvarchar](500) NULL,
	[experience_letter] [nvarchar](500) NULL,
	[salary_slip] [nvarchar](500) NULL,
	[noc_relieving_letter] [nvarchar](500) NULL,
	[other_documents] [nvarchar](500) NULL,
 CONSTRAINT [PK__tbl_user__9666E8AC3D5ACA75] PRIMARY KEY CLUSTERED 
(
	[document_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [dbo].[tbl_user_documents] ADD  CONSTRAINT [DF__tbl_user___is_de__6FE99F9F]  DEFAULT ((0)) FOR [is_deleted]
ALTER TABLE [dbo].[tbl_user_documents] ADD  CONSTRAINT [DF__tbl_user___creat__70DDC3D8]  DEFAULT (getdate()) FOR [created_date]
END
GO


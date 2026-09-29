USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_referral_request]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_referral_request](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](200) NULL,
	[Email] [nvarchar](200) NULL,
	[PhoneNo] [nvarchar](20) NULL,
	[ReferralType] [nvarchar](300) NULL,
	[Message] [nvarchar](max) NULL,
	[IsDeleted] [bit] NULL,
	[CreatedDate] [datetime] NULL,
 CONSTRAINT [PK_tbl_referral_request] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [dbo].[tbl_referral_request] ADD  DEFAULT ((0)) FOR [IsDeleted]
ALTER TABLE [dbo].[tbl_referral_request] ADD  DEFAULT (getdate()) FOR [CreatedDate]
END
GO


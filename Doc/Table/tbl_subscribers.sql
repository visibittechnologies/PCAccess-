USE [PCAccess_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_subscribers]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_subscribers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Email] [nvarchar](255) NOT NULL,
	[IsActive] [bit] NULL,
	[IsVerified] [bit] NULL,
	[SubscribeDate] [datetime] NULL,
	[UnsubscribeDate] [datetime] NULL,
	[CreatedBy] [nvarchar](50) NULL,
	[IPAddress] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[tbl_subscribers] ADD  DEFAULT ((1)) FOR [IsActive]
ALTER TABLE [dbo].[tbl_subscribers] ADD  DEFAULT ((0)) FOR [IsVerified]
ALTER TABLE [dbo].[tbl_subscribers] ADD  DEFAULT (getdate()) FOR [SubscribeDate]
END
GO


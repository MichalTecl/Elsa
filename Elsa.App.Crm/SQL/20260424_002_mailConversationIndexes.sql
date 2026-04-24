IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MessageParticipantAddress_Email')
BEGIN
    CREATE NONCLUSTERED INDEX IX_MessageParticipantAddress_Email
        ON dbo.MessageParticipantAddress (Email)
        INCLUDE (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MailMessageReferenceParticipant_ParticipantAddressId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_MailMessageReferenceParticipant_ParticipantAddressId
        ON dbo.MailMessageReferenceParticipant (ParticipantAddressId)
        INCLUDE (MailMessageReferenceId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MailMessageReferenceParticipant_MailMessageReferenceId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_MailMessageReferenceParticipant_MailMessageReferenceId
        ON dbo.MailMessageReferenceParticipant (MailMessageReferenceId)
        INCLUDE (ParticipantAddressId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MailMessageReference_ConversationId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_MailMessageReference_ConversationId
        ON dbo.MailMessageReference (ConversationId)
        INCLUDE (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MailConversation_SummaryId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_MailConversation_SummaryId
        ON dbo.MailConversation (SummaryId)
        INCLUDE (Id, ConversationEndDt, Hint);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customer_IsDistributor_Id')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Customer_IsDistributor_Id
        ON dbo.Customer (IsDistributor, Id)
        INCLUDE (Email, ErpUid);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerContactPerson_CustomerId_PersonId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_CustomerContactPerson_CustomerId_PersonId
        ON dbo.CustomerContactPerson (CustomerId, PersonId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Person_Email')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Person_Email
        ON dbo.Person (Email)
        INCLUDE (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerEmailChange_ErpUid_NewEmail')
BEGIN
    CREATE NONCLUSTERED INDEX IX_CustomerEmailChange_ErpUid_NewEmail
        ON dbo.CustomerEmailChange (ErpUid, NewEmail);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerEmailChange_ErpUid_OldEmail')
BEGIN
    CREATE NONCLUSTERED INDEX IX_CustomerEmailChange_ErpUid_OldEmail
        ON dbo.CustomerEmailChange (ErpUid, OldEmail);
END;
GO

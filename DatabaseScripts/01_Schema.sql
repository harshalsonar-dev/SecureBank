-- =========================================================================
-- Project: SecureBank Enterprise Banking System
-- File: 01_Schema.sql
-- Description: Normalized SQL Server Schema with Strict Financial Constraints,
--              ACID Support, Optimistic Concurrency Tokens, and Immutable Audit Trail.
-- =========================================================================

-- 1. Create Database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SecureBankDB')
BEGIN
    CREATE DATABASE SecureBankDB;
END
GO

USE SecureBankDB;
GO

-- 2. Users Table
-- Stores user credentials, role-based access, and account lockout tracking
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL 
    DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
    Email NVARCHAR(100) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL, -- BCrypt hashed password
    FullName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_Role DEFAULT 'Customer'
        CONSTRAINT CK_Users_Role CHECK (Role IN ('Customer', 'BankTeller', 'Admin')),
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    FailedLoginAttempts INT NOT NULL CONSTRAINT DF_Users_FailedLoginAttempts DEFAULT 0,
    LockedUntil DATETIME NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME()
);
GO

-- 3. Accounts Table
-- Stores financial balances with hard non-negative check constraints and RowVersion for optimistic locking
IF OBJECT_ID('dbo.Accounts', 'U') IS NOT NULL 
    DROP TABLE dbo.Accounts;
GO

CREATE TABLE dbo.Accounts (
    AccountId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    AccountNumber VARCHAR(20) NOT NULL CONSTRAINT UQ_Accounts_AccountNumber UNIQUE,
    AccountType VARCHAR(20) NOT NULL CONSTRAINT CK_Accounts_AccountType CHECK (AccountType IN ('Checking', 'Savings')),
    Balance DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Accounts_Balance DEFAULT 0.00,
    Currency CHAR(3) NOT NULL CONSTRAINT DF_Accounts_Currency DEFAULT 'USD',
    IsActive BIT NOT NULL CONSTRAINT DF_Accounts_IsActive DEFAULT 1,
    RowVersion ROWVERSION NOT NULL, -- Automatic 8-byte timestamp token for optimistic concurrency checks
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Accounts_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Accounts_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Accounts_Balance_NonNegative CHECK (Balance >= 0.00) -- Invariant: Balance cannot go negative
);
GO

-- 4. Transactions Table
-- Immutable double-entry ledger record for deposits, withdrawals, and transfers
IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL 
    DROP TABLE dbo.Transactions;
GO

CREATE TABLE dbo.Transactions (
    TransactionId BIGINT IDENTITY(1,1) PRIMARY KEY,
    TransactionReference UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Transactions_Ref DEFAULT NEWID() CONSTRAINT UQ_Transactions_Ref UNIQUE,
    FromAccountId INT NULL, -- NULL for Cash/External Deposits
    ToAccountId INT NULL,   -- NULL for Cash/External Withdrawals
    TransactionType VARCHAR(20) NOT NULL CONSTRAINT CK_Transactions_Type CHECK (TransactionType IN ('Deposit', 'Withdrawal', 'Transfer')),
    Amount DECIMAL(18, 2) NOT NULL CONSTRAINT CK_Transactions_PositiveAmount CHECK (Amount > 0.00),
    Status VARCHAR(20) NOT NULL CONSTRAINT CK_Transactions_Status CHECK (Status IN ('Pending', 'Completed', 'Failed')),
    Remarks NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Transactions_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Transactions_FromAccount FOREIGN KEY (FromAccountId) REFERENCES dbo.Accounts(AccountId),
    CONSTRAINT FK_Transactions_ToAccount FOREIGN KEY (ToAccountId) REFERENCES dbo.Accounts(AccountId),
    CONSTRAINT CK_Transactions_Parties CHECK (
        (TransactionType = 'Deposit' AND FromAccountId IS NULL AND ToAccountId IS NOT NULL) OR
        (TransactionType = 'Withdrawal' AND FromAccountId IS NOT NULL AND ToAccountId IS NULL) OR
        (TransactionType = 'Transfer' AND FromAccountId IS NOT NULL AND ToAccountId IS NOT NULL AND FromAccountId <> ToAccountId)
    )
);
GO

-- 5. AuditLogs Table (Append-Only Immutable Ledger)
-- Retains audit trail of changes across financial records and critical security events
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL 
    DROP TABLE dbo.AuditLogs;
GO

CREATE TABLE dbo.AuditLogs (
    AuditId BIGINT IDENTITY(1,1) PRIMARY KEY,
    TableName VARCHAR(50) NOT NULL,
    RecordId INT NOT NULL,
    ActionType VARCHAR(20) NOT NULL CONSTRAINT CK_AuditLogs_Action CHECK (ActionType IN ('INSERT', 'UPDATE', 'DELETE', 'SECURITY')),
    OldValue NVARCHAR(MAX) NULL,
    NewValue NVARCHAR(MAX) NULL,
    PerformedBy NVARCHAR(50) NOT NULL,
    IPAddress VARCHAR(45) NULL,
    Timestamp DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_Timestamp DEFAULT SYSUTCDATETIME()
);
GO

-- 6. High-Performance Indexes for OLTP Queries
CREATE NONCLUSTERED INDEX IX_Accounts_UserId ON dbo.Accounts(UserId);
CREATE NONCLUSTERED INDEX IX_Transactions_FromAccountId ON dbo.Transactions(FromAccountId);
CREATE NONCLUSTERED INDEX IX_Transactions_ToAccountId ON dbo.Transactions(ToAccountId);
CREATE NONCLUSTERED INDEX IX_Transactions_CreatedAt ON dbo.Transactions(CreatedAt DESC);
CREATE NONCLUSTERED INDEX IX_AuditLogs_RecordId_TableName ON dbo.AuditLogs(TableName, RecordId);
GO

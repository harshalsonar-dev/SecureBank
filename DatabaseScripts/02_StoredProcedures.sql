-- =========================================================================
-- Project: SecureBank Enterprise Banking System
-- File: 02_StoredProcedures.sql
-- Description: Mission-critical Stored Procedures enforcing ACID transactions,
--              deadlock elimination via lock-ordering, race-condition mitigation
--              via (UPDLOCK, ROWLOCK, HOLDLOCK), and automated audit trails.
-- =========================================================================

USE SecureBankDB;
GO

-- =========================================================================
-- 1. Stored Procedure: dbo.sp_TransferFunds
-- Description:
--   Executes a financial transfer between two accounts with strict ACID guarantees.
--   Prevents deadlocks across concurrent reverse transfers (A->B and B->A)
--   by locking accounts in ascending numerical order of AccountId.
--   Prevents double-spending and lost updates using UPDLOCK + ROWLOCK + HOLDLOCK.
-- =========================================================================
CREATE OR ALTER PROCEDURE dbo.sp_TransferFunds
    @FromAccountId INT,
    @ToAccountId INT,
    @Amount DECIMAL(18, 2),
    @Remarks NVARCHAR(255),
    @PerformedBy NVARCHAR(50),
    @IPAddress VARCHAR(45),
    @TransactionReference UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON; -- Guarantees immediate rollback on fatal engine errors

    -- 1. Input Sanitization & Domain Validation
    IF @Amount IS NULL OR @Amount <= 0.00
    BEGIN
        RAISERROR('Transfer amount must be strictly greater than zero.', 16, 1);
        RETURN;
    END

    IF @FromAccountId IS NULL OR @ToAccountId IS NULL
    BEGIN
        RAISERROR('Both source and destination accounts must be specified.', 16, 1);
        RETURN;
    END

    IF @FromAccountId = @ToAccountId
    BEGIN
        RAISERROR('Source and Destination accounts cannot be identical.', 16, 1);
        RETURN;
    END

    -- Generate tracking reference
    SET @TransactionReference = NEWID();

    -- Determine deterministic lock acquisition order to prevent deadlocks
    DECLARE @FirstAccountId INT, @SecondAccountId INT;
    IF @FromAccountId < @ToAccountId
    BEGIN
        SET @FirstAccountId = @FromAccountId;
        SET @SecondAccountId = @ToAccountId;
    END
    ELSE
    BEGIN
        SET @FirstAccountId = @ToAccountId;
        SET @SecondAccountId = @FromAccountId;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 2. Lock First Account (Acquires Update Lock held until COMMIT/ROLLBACK)
        DECLARE @FirstBal DECIMAL(18, 2), @FirstActive BIT;
        SELECT @FirstBal = Balance, @FirstActive = IsActive
        FROM dbo.Accounts WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
        WHERE AccountId = @FirstAccountId;

        IF @FirstBal IS NULL OR @FirstActive = 0
        BEGIN
            RAISERROR('Account ID %d does not exist or is inactive.', 16, 1, @FirstAccountId);
        END

        -- 3. Lock Second Account
        DECLARE @SecondBal DECIMAL(18, 2), @SecondActive BIT;
        SELECT @SecondBal = Balance, @SecondActive = IsActive
        FROM dbo.Accounts WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
        WHERE AccountId = @SecondAccountId;

        IF @SecondBal IS NULL OR @SecondActive = 0
        BEGIN
            RAISERROR('Account ID %d does not exist or is inactive.', 16, 1, @SecondAccountId);
        END

        -- 4. Check Available Balance on Source Account
        DECLARE @SourceBalance DECIMAL(18, 2);
        DECLARE @DestBalance DECIMAL(18, 2);

        IF @FromAccountId = @FirstAccountId
        BEGIN
            SET @SourceBalance = @FirstBal;
            SET @DestBalance = @SecondBal;
        END
        ELSE
        BEGIN
            SET @SourceBalance = @SecondBal;
            SET @DestBalance = @FirstBal;
        END

        IF @SourceBalance < @Amount
        BEGIN
            DECLARE @ErrBalanceTransfer NVARCHAR(500) = CONCAT('Insufficient funds. Current balance: ', @SourceBalance, ', Requested transfer: ', @Amount);
            RAISERROR(@ErrBalanceTransfer, 16, 1);
        END

        -- 5. Atomic Balance Updates
        UPDATE dbo.Accounts
        SET Balance = Balance - @Amount
        WHERE AccountId = @FromAccountId;

        UPDATE dbo.Accounts
        SET Balance = Balance + @Amount
        WHERE AccountId = @ToAccountId;

        -- 6. Insert Financial Ledger Record
        INSERT INTO dbo.Transactions (
            TransactionReference,
            FromAccountId,
            ToAccountId,
            TransactionType,
            Amount,
            Status,
            Remarks,
            CreatedAt
        )
        VALUES (
            @TransactionReference,
            @FromAccountId,
            @ToAccountId,
            'Transfer',
            @Amount,
            'Completed',
            @Remarks,
            SYSUTCDATETIME()
        );

        -- 7. Audit Log for Source Account
        INSERT INTO dbo.AuditLogs (
            TableName,
            RecordId,
            ActionType,
            OldValue,
            NewValue,
            PerformedBy,
            IPAddress,
            Timestamp
        )
        VALUES (
            'Accounts',
            @FromAccountId,
            'UPDATE',
            CONCAT('Balance: ', @SourceBalance),
            CONCAT('Balance: ', @SourceBalance - @Amount, '; TransferredTo: ', @ToAccountId, '; Ref: ', @TransactionReference),
            @PerformedBy,
            @IPAddress,
            SYSUTCDATETIME()
        );

        -- 8. Audit Log for Destination Account
        INSERT INTO dbo.AuditLogs (
            TableName,
            RecordId,
            ActionType,
            OldValue,
            NewValue,
            PerformedBy,
            IPAddress,
            Timestamp
        )
        VALUES (
            'Accounts',
            @ToAccountId,
            'UPDATE',
            CONCAT('Balance: ', @DestBalance),
            CONCAT('Balance: ', @DestBalance + @Amount, '; ReceivedFrom: ', @FromAccountId, '; Ref: ', @TransactionReference),
            @PerformedBy,
            @IPAddress,
            SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

-- =========================================================================
-- 2. Stored Procedure: dbo.sp_DepositFunds
-- Description:
--   Executes a deposit operation into a target account under an explicit transaction.
--   Uses (UPDLOCK, ROWLOCK, HOLDLOCK) to ensure serial access during balance change.
-- =========================================================================
CREATE OR ALTER PROCEDURE dbo.sp_DepositFunds
    @AccountId INT,
    @Amount DECIMAL(18, 2),
    @Remarks NVARCHAR(255),
    @PerformedBy NVARCHAR(50),
    @IPAddress VARCHAR(45),
    @TransactionReference UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Amount IS NULL OR @Amount <= 0.00
    BEGIN
        RAISERROR('Deposit amount must be strictly greater than zero.', 16, 1);
        RETURN;
    END

    IF @AccountId IS NULL
    BEGIN
        RAISERROR('Target account must be specified.', 16, 1);
        RETURN;
    END

    SET @TransactionReference = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Acquire exclusive update lock on the account
        DECLARE @CurrentBalance DECIMAL(18, 2), @IsActive BIT;
        SELECT @CurrentBalance = Balance, @IsActive = IsActive
        FROM dbo.Accounts WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
        WHERE AccountId = @AccountId;

        IF @CurrentBalance IS NULL OR @IsActive = 0
        BEGIN
            RAISERROR('Account ID %d is invalid or inactive.', 16, 1, @AccountId);
        END

        -- Credit Balance
        UPDATE dbo.Accounts
        SET Balance = Balance + @Amount
        WHERE AccountId = @AccountId;

        -- Record Deposit in Ledger
        INSERT INTO dbo.Transactions (
            TransactionReference,
            FromAccountId,
            ToAccountId,
            TransactionType,
            Amount,
            Status,
            Remarks,
            CreatedAt
        )
        VALUES (
            @TransactionReference,
            NULL,
            @AccountId,
            'Deposit',
            @Amount,
            'Completed',
            @Remarks,
            SYSUTCDATETIME()
        );

        -- Audit Log Record
        INSERT INTO dbo.AuditLogs (
            TableName,
            RecordId,
            ActionType,
            OldValue,
            NewValue,
            PerformedBy,
            IPAddress,
            Timestamp
        )
        VALUES (
            'Accounts',
            @AccountId,
            'UPDATE',
            CONCAT('Balance: ', @CurrentBalance),
            CONCAT('Balance: ', @CurrentBalance + @Amount, '; Ref: ', @TransactionReference),
            @PerformedBy,
            @IPAddress,
            SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

-- =========================================================================
-- 3. Stored Procedure: dbo.sp_WithdrawFunds
-- Description:
--   Executes a withdrawal operation under an explicit transaction with locking.
--   Guarantees no overdraft can occur via pessimistic lock and table check constraints.
-- =========================================================================
CREATE OR ALTER PROCEDURE dbo.sp_WithdrawFunds
    @AccountId INT,
    @Amount DECIMAL(18, 2),
    @Remarks NVARCHAR(255),
    @PerformedBy NVARCHAR(50),
    @IPAddress VARCHAR(45),
    @TransactionReference UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Amount IS NULL OR @Amount <= 0.00
    BEGIN
        RAISERROR('Withdrawal amount must be strictly greater than zero.', 16, 1);
        RETURN;
    END

    IF @AccountId IS NULL
    BEGIN
        RAISERROR('Account must be specified.', 16, 1);
        RETURN;
    END

    SET @TransactionReference = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Acquire exclusive update lock on the account
        DECLARE @CurrentBalance DECIMAL(18, 2), @IsActive BIT;
        SELECT @CurrentBalance = Balance, @IsActive = IsActive
        FROM dbo.Accounts WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
        WHERE AccountId = @AccountId;

        IF @CurrentBalance IS NULL OR @IsActive = 0
        BEGIN
            RAISERROR('Account ID %d is invalid or inactive.', 16, 1, @AccountId);
        END

        IF @CurrentBalance < @Amount
        BEGIN
            DECLARE @ErrBalanceWithdraw NVARCHAR(500) = CONCAT('Insufficient funds. Current balance: ', @CurrentBalance, ', Requested withdrawal: ', @Amount);
            RAISERROR(@ErrBalanceWithdraw, 16, 1);
        END

        -- Debit Balance
        UPDATE dbo.Accounts
        SET Balance = Balance - @Amount
        WHERE AccountId = @AccountId;

        -- Record Withdrawal in Ledger
        INSERT INTO dbo.Transactions (
            TransactionReference,
            FromAccountId,
            ToAccountId,
            TransactionType,
            Amount,
            Status,
            Remarks,
            CreatedAt
        )
        VALUES (
            @TransactionReference,
            @AccountId,
            NULL,
            'Withdrawal',
            @Amount,
            'Completed',
            @Remarks,
            SYSUTCDATETIME()
        );

        -- Audit Log Record
        INSERT INTO dbo.AuditLogs (
            TableName,
            RecordId,
            ActionType,
            OldValue,
            NewValue,
            PerformedBy,
            IPAddress,
            Timestamp
        )
        VALUES (
            'Accounts',
            @AccountId,
            'UPDATE',
            CONCAT('Balance: ', @CurrentBalance),
            CONCAT('Balance: ', @CurrentBalance - @Amount, '; Ref: ', @TransactionReference),
            @PerformedBy,
            @IPAddress,
            SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO


-- =========================================================================
-- 4. Stored Procedure: dbo.sp_RegisterUser
-- Description:
--   Atomically registers a new customer, sets their BCrypt hash, and provisions
--   their default "Checking" account with an optional opening balance.
-- =========================================================================
CREATE OR ALTER PROCEDURE dbo.sp_RegisterUser
    @Username NVARCHAR(50),
    @Email NVARCHAR(100),
    @PasswordHash NVARCHAR(255),
    @FullName NVARCHAR(100),
    @InitialDeposit DECIMAL(18, 2) = 0.00,
    @UserId INT OUTPUT,
    @AccountId INT OUTPUT,
    @AccountNumber VARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Basic domain validations
    IF @Username IS NULL OR LEN(LTRIM(RTRIM(@Username))) < 3
    BEGIN
        RAISERROR('Username must be at least 3 characters long.', 16, 1);
        RETURN;
    END

    IF @Email IS NULL OR CHARINDEX('@', @Email) = 0
    BEGIN
        RAISERROR('A valid email address is required.', 16, 1);
        RETURN;
    END

    IF @PasswordHash IS NULL OR LEN(@PasswordHash) < 10
    BEGIN
        RAISERROR('Valid password hash is required.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Uniqueness checks
        IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
        BEGIN
            RAISERROR('Username is already taken.', 16, 1);
        END

        IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email)
        BEGIN
            RAISERROR('Email address is already registered.', 16, 1);
        END

        -- 1. Insert User
        INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, Role, IsActive, CreatedAt)
        VALUES (@Username, @Email, @PasswordHash, @FullName, 'Customer', 1, SYSUTCDATETIME());

        SET @UserId = SCOPE_IDENTITY();

        -- 2. Generate unique Account Number (Format: SB-XXXXXXXXXX)
        SET @AccountNumber = 'SB-' + RIGHT('0000000000' + CAST(ABS(CHECKSUM(NEWID())) % 10000000000 AS VARCHAR(10)), 10);

        -- Ensure uniqueness in unlikely collision
        WHILE EXISTS (SELECT 1 FROM dbo.Accounts WHERE AccountNumber = @AccountNumber)
        BEGIN
            SET @AccountNumber = 'SB-' + RIGHT('0000000000' + CAST(ABS(CHECKSUM(NEWID())) % 10000000000 AS VARCHAR(10)), 10);
        END

        -- 3. Provision Default Checking Account
        INSERT INTO dbo.Accounts (UserId, AccountNumber, AccountType, Balance, Currency, IsActive, CreatedAt)
        VALUES (@UserId, @AccountNumber, 'Checking', @InitialDeposit, 'USD', 1, SYSUTCDATETIME());

        SET @AccountId = SCOPE_IDENTITY();

        -- 4. Record Initial Deposit transaction if applicable
        IF @InitialDeposit > 0.00
        BEGIN
            DECLARE @TxRef UNIQUEIDENTIFIER = NEWID();

            INSERT INTO dbo.Transactions (
                TransactionReference,
                FromAccountId,
                ToAccountId,
                TransactionType,
                Amount,
                Status,
                Remarks,
                CreatedAt
            )
            VALUES (
                @TxRef,
                NULL,
                @AccountId,
                'Deposit',
                @InitialDeposit,
                'Completed',
                'Account Opening Complimentary Deposit',
                SYSUTCDATETIME()
            );

            INSERT INTO dbo.AuditLogs (TableName, RecordId, ActionType, OldValue, NewValue, PerformedBy, IPAddress, Timestamp)
            VALUES ('Accounts', @AccountId, 'INSERT', NULL, CONCAT('Initial Balance: ', @InitialDeposit), @Username, 'System', SYSUTCDATETIME());
        END

        -- 5. Audit Log for user registration
        INSERT INTO dbo.AuditLogs (TableName, RecordId, ActionType, OldValue, NewValue, PerformedBy, IPAddress, Timestamp)
        VALUES ('Users', @UserId, 'INSERT', NULL, CONCAT('Registered: ', @Username, '; Account: ', @AccountNumber), @Username, 'System', SYSUTCDATETIME());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrSev INT = ERROR_SEVERITY();
        DECLARE @ErrState INT = ERROR_STATE();
        RAISERROR(@ErrMsg, @ErrSev, @ErrState);
    END CATCH
END;
GO

-- =========================================================================
-- 5. Stored Procedure: dbo.sp_GetUserAccounts
-- Description:
--   Fetches all active accounts associated with a specific user.
-- =========================================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetUserAccounts
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        AccountId,
        UserId,
        AccountNumber,
        AccountType,
        Balance,
        Currency,
        IsActive,
        CreatedAt
    FROM dbo.Accounts
    WHERE UserId = @UserId AND IsActive = 1
    ORDER BY AccountId ASC;
END;
GO

-- =========================================================================
-- 6. Stored Procedure: dbo.sp_GetRecentTransactions
-- Description:
--   Fetches the top N recent transactions for all accounts owned by a user.
-- =========================================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetRecentTransactions
    @UserId INT,
    @Limit INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limit)
        t.TransactionId,
        t.TransactionReference,
        t.FromAccountId,
        t.ToAccountId,
        t.TransactionType,
        t.Amount,
        t.Status,
        t.Remarks,
        t.CreatedAt
    FROM dbo.Transactions t
    WHERE t.FromAccountId IN (SELECT AccountId FROM dbo.Accounts WHERE UserId = @UserId)
       OR t.ToAccountId IN (SELECT AccountId FROM dbo.Accounts WHERE UserId = @UserId)
    ORDER BY t.CreatedAt DESC;
END;
GO

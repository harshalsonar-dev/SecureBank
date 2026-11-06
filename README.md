# SecureBank Enterprise Banking Web Application

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![Pure ADO.NET](https://img.shields.io/badge/Data%20Access-Pure%20ADO.NET-green.svg)](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/)
[![MS SQL Server](https://img.shields.io/badge/Database-MS%20SQL%20Server-red.svg)](https://www.microsoft.com/sql-server/)
[![BCrypt Security](https://img.shields.io/badge/Security-BCrypt-orange.svg)](https://github.com/BcryptNet/bcrypt.net)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

An enterprise-grade, high-performance retail banking web application built with **C#**, **ASP.NET Core MVC**, **Pure ADO.NET**, and **MS SQL Server**. Designed according to strict real-world financial software specifications: zero data loss, deterministic concurrency control, and immutable audit trailing.

---

## Architecture Overview

SecureBank enforces a strict **N-Tier Architecture**:

```
[ Presentation Layer: ASP.NET Core MVC, Razor, Bootstrap 5, FontAwesome ]
                              |
                              v
[ Security & Filter Layer: SessionAuthorize, Anti-Forgery Tokens ]
                              |
                              v
[ Business Logic Layer (BLL): Domain Services, BCrypt, Financial Invariants ]
                              |
                              v
[ Data Access Layer (DAL): Pure ADO.NET (Microsoft.Data.SqlClient), Repositories ]
                              |
                              v
[ Database Engine: MS SQL Server (ACID Transactions, UPDLOCK/HOLDLOCK, Audit Logs) ]
```

---

## Core Enterprise Banking Features

* **Strict ACID Financial Transactions:** Deposits, withdrawals, and inter-account transfers are committed atomically inside SQL Server transactions (`BEGIN TRAN`, `COMMIT`, `ROLLBACK`) with `SET XACT_ABORT ON`.
* **Deterministic Deadlock Elimination:** Simultaneous reverse transfers between accounts (e.g. Account A &rarr; Account B while Account B &rarr; Account A) automatically order row locks by `MIN(AccountId)` followed by `MAX(AccountId)`.
* **Double-Spending Prevention:** Balance queries acquire exclusive update locks via `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)`.
* **Hard Invariant Defense:** Database-level check constraint `CHECK (Balance >= 0.00)` ensures balances cannot go negative even under unforeseen edge conditions.
* **Immutable Audit Trail:** Balances cannot change without synchronously inserting corresponding ledger rows into `Transactions` and before/after snapshots into `AuditLogs`.
* **Zero-Catch DAL:** Low-level `SqlException` instances bubble directly to the BLL, where domain logic translates them into domain exceptions (`InsufficientFundsException`, `AccountNotFoundException`).
* **Cryptographic Security:** Passwords hashed with `BCrypt.Net-Next` (`workFactor: 12`).
* **Session Hardening:** 20-minute idle expiration, `HttpOnly = true`, `SameSite = Strict`, and anti-forgery token verification (`[ValidateAntiForgeryToken]`).

---

## Database Schema

* `dbo.Users`: User accounts, BCrypt password hashes, role-based checks, lockout counters.
* `dbo.Accounts`: Checking and savings accounts with `RowVersion` optimistic concurrency tokens and non-negative constraints.
* `dbo.Transactions`: Immutable double-entry financial ledger recording party relationships and UUID tracking references.
* `dbo.AuditLogs`: Append-only security and balance audit trail recording client IP, acting user, and before/after deltas.

---

## Getting Started

### Prerequisites
* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* [Microsoft SQL Server](https://www.microsoft.com/sql-server/) or SQL Server LocalDB

### 1. Database Setup
Execute the scripts located in `DatabaseScripts/` on your SQL Server instance:
```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i "DatabaseScripts/01_Schema.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i "DatabaseScripts/02_StoredProcedures.sql"
```

### 2. Configure Connection String
Update `appsettings.json` if using a remote or dedicated SQL Server instance:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SecureBankDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### 3. Run the Application
```powershell
dotnet run
```
Navigate to `http://localhost:5055` (or the port specified in terminal output).

---

## Docker Deployment

Build and run using the production multi-stage `Dockerfile`:
```bash
docker build -t securebank:latest .
docker run -p 8080:8080 -e ConnectionStrings__DefaultConnection="<YOUR_SQL_CONNECTION_STRING>" securebank:latest
```

---

## License
This project is licensed under the MIT License.

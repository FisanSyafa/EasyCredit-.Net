using System.Data;
using Dapper;
using Dapper.Oracle;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure.Oracle;

public sealed class OracleCreditApplicationRepository : IOracleCreditApplicationRepository
{
    private const string Table = "EASYCREDIT.TRN_CREDIT_APPLICATION";
    private const string InstallmentTable = "EASYCREDIT.TRN_CREDIT_INSTALLMENT";
    private const string AuditTable = "EASYCREDIT.TRN_APPROVAL_AUDIT_LOG";
    private const string Schema = "EASYCREDIT";

    private readonly OracleConnectionFactory _factory;

    public OracleCreditApplicationRepository(OracleConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<(IReadOnlyList<CreditApplication> Items, int Total)> GetPagedAsync(
        string? status, int page, int size)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        await using var conn = _factory.Create();

        var whereClause = "";
        var countParams = new DynamicParameters();
        var queryParams = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereClause = "WHERE STATUS = :Status";
            countParams.Add("Status", status.Trim().ToUpperInvariant(), DbType.String);
            queryParams.Add("Status", status.Trim().ToUpperInvariant(), DbType.String);
        }

        queryParams.Add("Offset", (page - 1) * size, DbType.Int32);
        queryParams.Add("Limit", size, DbType.Int32);

        var countSql = $"SELECT COUNT(1) FROM {Table} {whereClause}";
        var total = await conn.ExecuteScalarAsync<int>(countSql, countParams);

        var dataSql = $"""
            SELECT 
                APPLICATION_ID AS ApplicationId,
                CUSTOMER_ID AS CustomerId,
                LOAN_AMOUNT AS LoanAmount,
                TENOR_MONTHS AS TenorMonths,
                INTEREST_RATE AS InterestRate,
                CREDIT_SCORE AS CreditScore,
                STATUS AS Status,
                APPLICATION_DATE AS ApplicationDate,
                APPROVED_BY AS ApprovedBy,
                APPROVED_AT AS ApprovedAt
            FROM {Table}
            {whereClause}
            ORDER BY APPLICATION_ID DESC
            OFFSET :Offset ROWS FETCH NEXT :Limit ROWS ONLY
            """;

        var items = await conn.QueryAsync<CreditApplication>(dataSql, queryParams);
        return (items.ToList(), total);
    }

    public async Task<CreditApplication?> GetByIdAsync(int applicationId)
    {
        await using var conn = _factory.Create();

        const string sql = $"""
            SELECT 
                APPLICATION_ID AS ApplicationId,
                CUSTOMER_ID AS CustomerId,
                LOAN_AMOUNT AS LoanAmount,
                TENOR_MONTHS AS TenorMonths,
                INTEREST_RATE AS InterestRate,
                CREDIT_SCORE AS CreditScore,
                STATUS AS Status,
                APPLICATION_DATE AS ApplicationDate,
                APPROVED_BY AS ApprovedBy,
                APPROVED_AT AS ApprovedAt
            FROM {Table}
            WHERE APPLICATION_ID = :Id
            """;

        return await conn.QuerySingleOrDefaultAsync<CreditApplication>(sql, new { Id = applicationId });
    }

    public async Task<int> CreateWithInstallmentsAsync(
        CreditApplication application,
        IEnumerable<CreditInstallment> installments)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            const string insertAppSql = $"""
                INSERT INTO {Table} 
                    (CUSTOMER_ID, LOAN_AMOUNT, TENOR_MONTHS, INTEREST_RATE, CREDIT_SCORE, STATUS, APPLICATION_DATE)
                VALUES 
                    (:CustomerId, :LoanAmount, :TenorMonths, :InterestRate, :CreditScore, :Status, :AppDate)
                RETURNING APPLICATION_ID INTO :NewId
                """;

            var p = new DynamicParameters();
            p.Add("CustomerId", application.CustomerId, DbType.Int32);
            p.Add("LoanAmount", application.LoanAmount, DbType.Decimal);
            p.Add("TenorMonths", application.TenorMonths, DbType.Int32);
            p.Add("InterestRate", application.InterestRate, DbType.Decimal);
            p.Add("CreditScore", application.CreditScore, DbType.Int32);
            p.Add("Status", application.Status, DbType.String);
            p.Add("AppDate", application.ApplicationDate, DbType.Date);
            p.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(insertAppSql, p, tx);

            var newAppId = p.Get<int>("NewId");
            application.ApplicationId = newAppId;

            const string insertInstSql = $"""
                INSERT INTO {InstallmentTable}
                    (APPLICATION_ID, INSTALLMENT_NO, DUE_DATE, INSTALLMENT_AMOUNT, STATUS)
                VALUES 
                    (:AppId, :InstNo, :DueDate, :Amount, :Status)
                """;

            foreach (var inst in installments)
            {
                await conn.ExecuteAsync(insertInstSql, new
                {
                    AppId = newAppId,
                    InstNo = inst.InstallmentNo,
                    DueDate = inst.DueDate,
                    Amount = inst.InstallmentAmount,
                    Status = inst.Status
                }, tx);
            }

            // Insert initial Audit Log
            try
            {
                const string insertAuditSql = $"""
                    INSERT INTO {AuditTable}
                        (APPLICATION_ID, ACTION, ACTION_BY, ACTION_AT, PREVIOUS_STATUS, NEW_STATUS, NOTES)
                    VALUES
                        (:AppId, 'SUBMIT', 'SYSTEM', :ActionAt, NULL, 'PENDING', 'Pengajuan kredit baru dibuat')
                    """;
                await conn.ExecuteAsync(insertAuditSql, new
                {
                    AppId = newAppId,
                    ActionAt = DateTime.UtcNow
                }, tx);
            }
            catch
            {
                // Non-critical jika tabel audit belum terbuat
            }

            tx.Commit();
            return newAppId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<(string Status, string Message)> ApproveOrRejectAsync(int applicationId, string action, string approver)
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_app_id", applicationId, OracleMappingType.Int32, ParameterDirection.Input);
        p.Add("p_action", action, OracleMappingType.Varchar2, ParameterDirection.Input, 20);
        p.Add("p_approver", approver, OracleMappingType.Varchar2, ParameterDirection.Input, 100);
        p.Add("out_stat", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 50);
        p.Add("out_mess", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 500);

        await conn.ExecuteAsync(
            $"{Schema}.sp_approve_application",
            p,
            commandType: CommandType.StoredProcedure);

        var status = p.Get<string>("out_stat") ?? "ERROR";
        var message = p.Get<string>("out_mess") ?? string.Empty;

        return (status, message);
    }

    public async Task<(IReadOnlyList<CreditInstallment> Installments, string Status, string Message)> CalculateInstallmentSpAsync(
        decimal loanAmount, int tenorMonths)
    {
        await using var conn = _factory.Create();

        var p = new OracleDynamicParameters();
        p.Add("p_loan_amount", loanAmount, OracleMappingType.Decimal, ParameterDirection.Input);
        p.Add("p_tenor_months", tenorMonths, OracleMappingType.Int32, ParameterDirection.Input);
        p.Add("p_cursor", dbType: OracleMappingType.RefCursor, direction: ParameterDirection.Output);
        p.Add("out_stat", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 50);
        p.Add("out_mess", dbType: OracleMappingType.Varchar2, direction: ParameterDirection.Output, size: 500);

        var items = await conn.QueryAsync<CreditInstallment>(
            $"{Schema}.sp_calculate_installment",
            p,
            commandType: CommandType.StoredProcedure);

        var status = p.Get<string>("out_stat") ?? "ERROR";
        var message = p.Get<string>("out_mess") ?? string.Empty;

        return (items.ToList(), status, message);
    }

    public async Task<IReadOnlyList<ApprovalAuditLog>> GetAuditLogsByApplicationIdAsync(int applicationId)
    {
        await using var conn = _factory.Create();
        try
        {
            const string sql = $"""
                SELECT 
                    AUDIT_ID AS AuditId,
                    APPLICATION_ID AS ApplicationId,
                    ACTION AS Action,
                    ACTION_BY AS ActionBy,
                    ACTION_AT AS ActionAt,
                    PREVIOUS_STATUS AS PreviousStatus,
                    NEW_STATUS AS NewStatus,
                    NOTES AS Notes
                FROM {AuditTable}
                WHERE APPLICATION_ID = :ApplicationId
                ORDER BY AUDIT_ID ASC
                """;

            var logs = await conn.QueryAsync<ApprovalAuditLog>(sql, new { ApplicationId = applicationId });
            return logs.ToList();
        }
        catch
        {
            return Array.Empty<ApprovalAuditLog>();
        }
    }
}

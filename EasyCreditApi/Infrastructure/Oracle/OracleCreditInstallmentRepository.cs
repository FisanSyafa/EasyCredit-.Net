using System.Data;
using Dapper;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure.Oracle;

public sealed class OracleCreditInstallmentRepository : IOracleCreditInstallmentRepository
{
    private const string Table = "EASYCREDIT.TRN_CREDIT_INSTALLMENT";
    private readonly OracleConnectionFactory _factory;

    public OracleCreditInstallmentRepository(OracleConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<CreditInstallment>> GetByApplicationIdAsync(int applicationId)
    {
        await using var conn = _factory.Create();

        const string sql = $"""
            SELECT 
                INSTALLMENT_ID AS InstallmentId,
                APPLICATION_ID AS ApplicationId,
                INSTALLMENT_NO AS InstallmentNo,
                DUE_DATE AS DueDate,
                INSTALLMENT_AMOUNT AS InstallmentAmount,
                STATUS AS Status
            FROM {Table}
            WHERE APPLICATION_ID = :ApplicationId
            ORDER BY INSTALLMENT_NO ASC
            """;

        var result = await conn.QueryAsync<CreditInstallment>(sql, new { ApplicationId = applicationId });
        return result.ToList();
    }
}


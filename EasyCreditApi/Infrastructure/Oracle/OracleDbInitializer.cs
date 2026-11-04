using Dapper;
using Oracle.ManagedDataAccess.Client;
using Serilog;

namespace EasyCreditApi.Infrastructure.Oracle;

public static class OracleDbInitializer
{
    public static async Task InitializeAsync(OracleConnectionFactory factory)
    {
        try
        {
            await using var conn = factory.Create();
            await conn.OpenAsync();

            // 1. Create Table TRN_CREDIT_APPLICATION
            try
            {
                await conn.ExecuteAsync("""
                    CREATE TABLE EASYCREDIT.TRN_CREDIT_APPLICATION (
                        APPLICATION_ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                        CUSTOMER_ID NUMBER NOT NULL,
                        LOAN_AMOUNT NUMBER NOT NULL,
                        TENOR_MONTHS NUMBER NOT NULL,
                        INTEREST_RATE NUMBER,
                        CREDIT_SCORE NUMBER DEFAULT 700,
                        STATUS VARCHAR2(20) DEFAULT 'PENDING',
                        APPLICATION_DATE DATE NOT NULL,
                        APPROVED_BY VARCHAR2(100),
                        APPROVED_AT DATE
                    )
                    """);
                Log.Information("Tabel EASYCREDIT.TRN_CREDIT_APPLICATION berhasil dibuat.");
            }
            catch (OracleException ex) when (ex.Number == 955)
            {
                // ORA-00955: name is already used by an existing object
            }

            // 2. Create Table TRN_CREDIT_INSTALLMENT
            try
            {
                await conn.ExecuteAsync("""
                    CREATE TABLE EASYCREDIT.TRN_CREDIT_INSTALLMENT (
                        INSTALLMENT_ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                        APPLICATION_ID NUMBER NOT NULL,
                        INSTALLMENT_NO NUMBER NOT NULL,
                        DUE_DATE DATE NOT NULL,
                        INSTALLMENT_AMOUNT NUMBER NOT NULL,
                        STATUS VARCHAR2(20) DEFAULT 'UNPAID',
                        CONSTRAINT FK_TRN_INST_APP FOREIGN KEY (APPLICATION_ID)
                            REFERENCES EASYCREDIT.TRN_CREDIT_APPLICATION (APPLICATION_ID)
                            ON DELETE CASCADE
                    )
                    """);
                Log.Information("Tabel EASYCREDIT.TRN_CREDIT_INSTALLMENT berhasil dibuat.");
            }
            catch (OracleException ex) when (ex.Number == 955)
            {
                // ORA-00955: already exists
            }

            // 3. Create Table TRN_APPROVAL_AUDIT_LOG
            try
            {
                await conn.ExecuteAsync("""
                    CREATE TABLE EASYCREDIT.TRN_APPROVAL_AUDIT_LOG (
                        AUDIT_ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                        APPLICATION_ID NUMBER NOT NULL,
                        ACTION VARCHAR2(20) NOT NULL,
                        ACTION_BY VARCHAR2(100) NOT NULL,
                        ACTION_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
                        PREVIOUS_STATUS VARCHAR2(20),
                        NEW_STATUS VARCHAR2(20),
                        NOTES VARCHAR2(255)
                    )
                    """);
                Log.Information("Tabel EASYCREDIT.TRN_APPROVAL_AUDIT_LOG berhasil dibuat.");
            }
            catch (OracleException ex) when (ex.Number == 955)
            {
                // ORA-00955: already exists
            }

            // 4. Create or Replace Procedure sp_calculate_installment
            const string spCalc = """
                CREATE OR REPLACE PROCEDURE EASYCREDIT.sp_calculate_installment(
                    p_loan_amount   IN NUMBER,
                    p_tenor_months  IN NUMBER,
                    p_cursor        OUT SYS_REFCURSOR,
                    out_stat        OUT VARCHAR2,
                    out_mess        OUT VARCHAR2
                ) IS
                    v_interest_rate NUMBER := 0.12;
                    v_total_interest NUMBER;
                    v_total_amount   NUMBER;
                    v_monthly_amount NUMBER;
                BEGIN
                    out_stat := 'ERROR';
                    out_mess := NULL;

                    IF p_loan_amount < 1000000 OR p_loan_amount > 50000000 THEN
                        out_stat := 'ERROR';
                        out_mess := 'Jumlah pinjaman harus antara Rp 1.000.000 dan Rp 50.000.000';
                        OPEN p_cursor FOR
                            SELECT 0 AS INSTALLMENT_NO, SYSDATE AS DUE_DATE, 0 AS INSTALLMENT_AMOUNT FROM DUAL WHERE 1 = 0;
                        RETURN;
                    END IF;

                    IF p_tenor_months NOT IN (6, 12, 24, 36) THEN
                        out_stat := 'ERROR';
                        out_mess := 'Tenor harus 6, 12, 24, atau 36 bulan';
                        OPEN p_cursor FOR
                            SELECT 0 AS INSTALLMENT_NO, SYSDATE AS DUE_DATE, 0 AS INSTALLMENT_AMOUNT FROM DUAL WHERE 1 = 0;
                        RETURN;
                    END IF;

                    v_total_interest := p_loan_amount * v_interest_rate * (p_tenor_months / 12);
                    v_total_amount   := p_loan_amount + v_total_interest;
                    v_monthly_amount := ROUND(v_total_amount / p_tenor_months);

                    OPEN p_cursor FOR
                        SELECT 
                            LEVEL AS "InstallmentNo",
                            ADD_MONTHS(TRUNC(SYSDATE), LEVEL) AS "DueDate",
                            v_monthly_amount AS "InstallmentAmount"
                        FROM DUAL
                        CONNECT BY LEVEL <= p_tenor_months;

                    out_stat := 'SUCCESS';
                    out_mess := 'Simulasi cicilan berhasil dihitung';
                EXCEPTION
                    WHEN OTHERS THEN
                        out_stat := 'ERROR';
                        out_mess := SQLERRM;
                        OPEN p_cursor FOR
                            SELECT 0 AS INSTALLMENT_NO, SYSDATE AS DUE_DATE, 0 AS INSTALLMENT_AMOUNT FROM DUAL WHERE 1 = 0;
                END sp_calculate_installment;
                """;

            await conn.ExecuteAsync(spCalc);

            // 5. Create or Replace Procedure sp_approve_application with Audit Log
            const string spApprove = """
                CREATE OR REPLACE PROCEDURE EASYCREDIT.sp_approve_application(
                    p_app_id   IN NUMBER,
                    p_action   IN VARCHAR2,
                    p_approver IN VARCHAR2,
                    out_stat   OUT VARCHAR2,
                    out_mess   OUT VARCHAR2
                ) IS
                    v_current_status VARCHAR2(20);
                    v_target_status  VARCHAR2(20);
                BEGIN
                    out_stat := 'ERROR';
                    out_mess := NULL;

                    BEGIN
                        SELECT STATUS INTO v_current_status
                        FROM EASYCREDIT.TRN_CREDIT_APPLICATION
                        WHERE APPLICATION_ID = p_app_id;
                    EXCEPTION
                        WHEN NO_DATA_FOUND THEN
                            out_stat := 'NOT_FOUND';
                            out_mess := 'Pengajuan kredit #' || p_app_id || ' tidak ditemukan';
                            RETURN;
                    END;

                    IF UPPER(p_action) = 'APPROVE' THEN
                        v_target_status := 'APPROVED';
                    ELSIF UPPER(p_action) = 'REJECT' THEN
                        v_target_status := 'REJECTED';
                    ELSE
                        out_stat := 'ERROR';
                        out_mess := 'Aksi tidak valid (hanya APPROVE atau REJECT)';
                        RETURN;
                    END IF;

                    IF v_current_status NOT IN ('SUBMITTED', 'PENDING') THEN
                        out_stat := 'INVALID_STATUS';
                        out_mess := 'Pengajuan dengan status ' || v_current_status || ' tidak dapat di-' || LOWER(p_action) || ' lagi';
                        RETURN;
                    END IF;

                    UPDATE EASYCREDIT.TRN_CREDIT_APPLICATION
                       SET STATUS      = v_target_status,
                           APPROVED_BY = p_approver,
                           APPROVED_AT = SYSDATE
                     WHERE APPLICATION_ID = p_app_id
                       AND STATUS IN ('SUBMITTED', 'PENDING');

                    IF SQL%ROWCOUNT = 0 THEN
                        out_stat := 'INVALID_STATUS';
                        out_mess := 'Status pengajuan sudah berubah oleh transaksi lain';
                        ROLLBACK;
                        RETURN;
                    END IF;

                    -- Insert ke tabel Audit Log Approval
                    BEGIN
                        INSERT INTO EASYCREDIT.TRN_APPROVAL_AUDIT_LOG
                            (APPLICATION_ID, ACTION, ACTION_BY, ACTION_AT, PREVIOUS_STATUS, NEW_STATUS, NOTES)
                        VALUES
                            (p_app_id, UPPER(p_action), p_approver, SYSTIMESTAMP, v_current_status, v_target_status, 'Audit: Pengajuan di-' || LOWER(p_action) || ' oleh ' || p_approver);
                    EXCEPTION
                        WHEN OTHERS THEN
                            NULL; -- Cegah kegagalan audit membatalkan approval
                    END;

                    COMMIT;
                    out_stat := 'SUCCESS';
                    out_mess := 'Pengajuan kredit #' || p_app_id || ' berhasil di-' || LOWER(p_action);
                EXCEPTION
                    WHEN OTHERS THEN
                        ROLLBACK;
                        out_stat := 'ERROR';
                        out_mess := SQLERRM;
                END sp_approve_application;
                """;

            await conn.ExecuteAsync(spApprove);
            Log.Information("Stored procedures Oracle EasyCredit berhasil diinisialisasi.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Inisialisasi schema Oracle dilewati atau gagal: {Message}", ex.Message);
        }
    }
}

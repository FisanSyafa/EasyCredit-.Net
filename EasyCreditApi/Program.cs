using System.Text;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Infrastructure.Security;
using EasyCreditApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using BCrypt;
using EasyCreditApi.Application.Services;
using EasyCreditApi.Infrastructure.Oracle;
using Dapper;
using EasyCreditApi.Application.DTO;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using EasyCreditApi.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Dapper: petakan snake_case (kolom Oracle/Postgres) ke PascalCase C# properties
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Npgsql: izinkan DateTime tanpa konversi paksa Utc/Local
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Secret lokal (connection string, API key, JWT key) — file ini di-gitignore.
// Alternatif: environment variable (ConnectionStrings__Oracle, ApiKey, Jwt__Key).
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var postgresConnectionString = builder.Configuration.GetConnectionString("postgres") 
?? throw new InvalidOperationException("Connection string 'postgres' tidak ditemukan.");


// Serilog: sink & level diatur di section "Serilog" pada appsettings.json.
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/// JWT
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Konfigurasi 'Jwt' tidak ditemukan");

// Repo latihan tidak menyimpan secret. Di Development, pakai key lokal agar app
// bisa dijalankan tanpa appsettings.Local.json. Di luar Development, key wajib diisi.
if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Konfigurasi 'Jwt:Key' wajib diisi.");

    jwtSettings.Key = "easycredit-dev-only-jwt-key-change-me-32chars";
    builder.Configuration["Jwt:Key"] = jwtSettings.Key;
}

// API key middleware juga butuh nilai lokal saat latihan.
// Frontend mengirim header X-Api-Key dengan nilai yang sama.
if (string.IsNullOrWhiteSpace(builder.Configuration["ApiKey"]))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Konfigurasi 'ApiKey' wajib diisi.");

    builder.Configuration["ApiKey"] = "easycredit-secret-api-key-2026";
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            RoleClaimType = "role",
            NameClaimType = "name",
            ClockSkew = TimeSpan.Zero
        };

        // 401/403 dari JWT juga memakai format ApiResponse.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Unauthorized: token tidak valid atau tidak ada."));
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Forbidden: akses ditolak."));
            }
        };
    });

builder.Services.AddAuthorization();

// ── DEPENDENCY INJECTION ──
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<OracleConnectionFactory>();
builder.Services.AddScoped<OracleTransactionDemo>();
builder.Services.AddScoped<ICustomerSpRepository, OracleCustomerSpRepository>();
builder.Services.AddScoped<ICustomerSpService, CustomerSpService>();
builder.Services.AddScoped<ICreditApplicationSpRepository, OracleCreditApplicationSpRepository>();
builder.Services.AddScoped<ICreditApplicationSpService, CreditApplicationSpService>();
builder.Services.AddScoped<IOracleCreditApplicationRepository, OracleCreditApplicationRepository>();
builder.Services.AddScoped<IOracleCreditInstallmentRepository, OracleCreditInstallmentRepository>();
builder.Services.AddScoped<ICreditApplicationService, CreditApplicationService>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(postgresConnectionString);
});

// Project API hanya melayani endpoint JSON di /api.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Error validasi model -> 400 dengan format ApiResponse.
        options.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

            return new BadRequestObjectResult(
                ApiResponse<Dictionary<string, string[]>>.Fail("Validasi gagal.", errors));
        };
    });
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    // Endpoint lab: hanya aktif di Development.
    app.MapGet("/_dbping", async (OracleConnectionFactory factory) =>
    {
        await using var conn = factory.Create();
        var ok = await conn.ExecuteScalarAsync<int>("SELECT 1 FROM dual");

        return ok == 1
            ? Results.Ok(ApiResponse<object>.Success(new { database = "oracle", schema = "EASYCREDIT", ok = true }))
            : Results.Json(ApiResponse<object>.Fail("Oracle tidak merespons dengan benar."), statusCode: 500);
    });

    app.MapPost("/_tx-demo", async (OracleTransactionDemo demo, bool fail = true) =>
    {
        try
        {
            var id = await demo.InsertTwoAsync(fail);
            return Results.Ok(ApiResponse<object>.Success(new { committed = true, firstId = id }));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<object>.Fail(ex.Message), statusCode: 500);
        }
    });

    // Helper lab Day 6: kembalikan application ke SUBMITTED agar smoke test bisa diulang.
    app.MapPost("/_reset-app/{id:int}", async (int id, OracleConnectionFactory factory) =>
    {
        await using var conn = factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        var rows = await conn.ExecuteAsync(
            "UPDATE EASYCREDIT.CREDIT_APPLICATIONS SET STATUS = 'SUBMITTED', APPROVED_BY = NULL, APPROVED_AT = NULL WHERE ID = :Id",
            new { Id = id }, tx);

        tx.Commit();
        return Results.Ok(new { reset = rows });
    });
}

// Urutan: Serilog request log (paling luar) -> correlation/request log -> exception -> API key.
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        if (http.Items.TryGetValue(RequestLoggingMiddleware.ItemKey, out var id))
            diag.Set(RequestLoggingMiddleware.ItemKey, id);
    };
});

app.UseRequestLogging();

app.UseCustomExceptionHandler();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

// Semua endpoint API memakai attribute routing (/api/...).
app.MapControllers();

// Seed PostgreSQL default admin & staff jika database terhubung
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    if (dbContext.Database.CanConnect())
    {
        if (!dbContext.Users.Any())
        {
            dbContext.Users.AddRange(
                new EasyCreditApi.Domain.Entities.User
                {
                    Name = "Admin EasyCredit",
                    Email = "admin@easycredit.test",
                    PasswordHash = hasher.Hash("admin123"),
                    Role = "ADMIN",
                    CreatedAt = DateTime.UtcNow
                },
                new EasyCreditApi.Domain.Entities.User
                {
                    Name = "Staff EasyCredit",
                    Email = "staff@easycredit.test",
                    PasswordHash = hasher.Hash("staff123"),
                    Role = "STAFF",
                    CreatedAt = DateTime.UtcNow
                }
            );
            dbContext.SaveChanges();
        }

        if (!dbContext.Customers.Any())
        {
            dbContext.Customers.AddRange(
                new EasyCreditApi.Domain.Entities.Customer
                {
                    Name = "Budi Santoso",
                    Nik = "3171012345670001",
                    Phone = "081234567890",
                    Email = "budi.santoso@example.com",
                    Address = "Jl. Sudirman No. 12, Jakarta",
                    CreatedAt = DateTime.UtcNow
                },
                new EasyCreditApi.Domain.Entities.Customer
                {
                    Name = "Siti Aminah",
                    Nik = "3171012345670002",
                    Phone = "081234567891",
                    Email = "siti.aminah@example.com",
                    Address = "Jl. Thamrin No. 45, Jakarta",
                    CreatedAt = DateTime.UtcNow
                }
            );
            dbContext.SaveChanges();
        }
    }
    // Inisialisasi schema & stored procedure Oracle jika belum ada
    var oracleFactory = scope.ServiceProvider.GetRequiredService<OracleConnectionFactory>();
    await OracleDbInitializer.InitializeAsync(oracleFactory);
}
catch (Exception ex)
{
    Log.Warning(ex, "Gagal menginisialisasi seed data PostgreSQL atau Oracle pada startup.");
}

app.Run();

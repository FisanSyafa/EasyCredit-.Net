# EasyCredit — Sistem Pengajuan Kredit Online

Sistem pengajuan kredit online sederhana dengan arsitektur berlapis, memadukan **ASP.NET Core Web API**, **Dual Database (PostgreSQL + Oracle)**, dan **ASP.NET Core MVC Frontend** (Razor + jQuery AJAX).

---

## 1. Arsitektur Sistem

EasyCredit dirancang dengan pemisahan tanggung jawab yang tegas antar database dan antar layer aplikasi:

```text
  [ Browser Pengguna ]
           │
           │ HTTPS / AJAX
           ▼
┌─────────────────────────────────────────────────────────┐
│ EasyCreditFrontend (ASP.NET Core MVC · Razor · jQuery)   │
│  - Session server-side untuk JWT                        │
│  - ApiClient (X-Api-Key + Authorization Bearer)         │
└──────────────────────────┬──────────────────────────────┘
                           │
                           │ HttpClient (JSON REST)
                           ▼
┌─────────────────────────────────────────────────────────┐
│ EasyCreditApi (ASP.NET Core Web API)                    │
│  - ApiKeyMiddleware & ExceptionMiddleware               │
│  - JWT Bearer Authentication & Role Authorization       │
│  - Service layer (validasi relasi & aturan bisnis)       │
└──────────────┬───────────────────────────┬──────────────┘
               │                           │
               ▼                           ▼
┌──────────────────────────────┐ ┌──────────────────────────────┐
│ PostgreSQL (EF Core + Npgsql)│ │ Oracle (Dapper + SP Legacy)  │
│  - "Sistem Baru"             │ │  - "Sistem Legacy"           │
│  - MST_USER                  │ │  - TRN_CREDIT_APPLICATION    │
│  - MST_CUSTOMER              │ │  - TRN_CREDIT_INSTALLMENT    │
└──────────────────────────────┘ └──────────────────────────────┘
```

### 1.1 Dual Database Responsibility
* **PostgreSQL (Sistem Baru — EF Core + Npgsql):**
  * `MST_USER`: Menyimpan data kredensial login (Email, Password BCrypt Hash, Role: `ADMIN` / `STAFF`).
  * `MST_CUSTOMER`: Menyimpan master data nasabah (Name, NIK Unique, Phone, Email, Address).
* **Oracle (Sistem Legacy — Dapper + Stored Procedure):**
  * `TRN_CREDIT_APPLICATION`: Header pengajuan pinjaman (Loan Amount, Tenor, Status, Approved By/At).
  * `TRN_CREDIT_INSTALLMENT`: Rincian jadwal angsuran berkala per pengajuan pinjaman.
  * *Stored Procedures*:
    * `sp_calculate_installment`: Menghitung skema angsuran dari nominal dan tenor pinjaman (RefCursor + out_stat, out_mess).
    * `sp_approve_application`: Memproses persetujuan/penolakan transaksional dengan kontrol konkurensi status.

> **Penting:** Tidak ada Foreign Key fisik lintas database. Integritas data dan validasi `CUSTOMER_ID` sepenuhnya diverifikasi pada Service Layer sebelum transaksi disimpan ke Oracle.

---

## 2. Struktur Repositori

```text
easy-credit/
├── EasyCreditApi/
│   ├── Controllers/               ← AuthController, CustomersController, CreditApplicationsController
│   ├── Application/
│   │   ├── DTO/                   ← ApiResponse<T>, DTO Auth, Customer, Credit
│   │   ├── Interfaces/            ← IAuthService, ICustomerService, ICreditApplicationService
│   │   └── Services/              ← Business logic, validasi lintas DB
│   ├── Domain/
│   │   └── Entities/              ← User, Customer, CreditApplication, CreditInstallment
│   ├── Infrastructure/
│   │   ├── Postgres/              ← AppDbContext, EfUserRepository, EfCustomerRepository
│   │   ├── Oracle/                ← OracleConnectionFactory, Dapper Repositories
│   │   └── Security/              ← BCryptPasswordHasher, JwtTokenGenerator
│   ├── Middleware/                ← ApiKeyMiddleware, ExceptionMiddleware, RequestLogging
│   └── Database/                  ← Skrip DDL PostgreSQL & Oracle + Stored Procedures
├── EasyCreditFrontend/
│   ├── Controllers/               ← AuthController, BaseController, CustomersController, CreditApplicationsController
│   ├── Models/
│   │   ├── Dtos/                  ← Kontrak DTO response dari API
│   │   └── ViewModels/            ← ViewModel halaman + DataAnnotation
│   ├── Services/                  ← ApiClient (HttpClient wrapper) & SessionKeys
│   ├── Views/                     ← Razor Views (Auth, Customers, CreditApplications, Dashboard)
│   └── wwwroot/                   ← Bootstrap, jQuery, CSS, dan custom JavaScript
├── docker-compose.yml             ← Container PostgreSQL 16 & Oracle Free
└── README.md
```

---

## 3. Akun Demo

| Email | Password | Role | Hak Akses |
|---|---|---|---|
| `admin@easycredit.test` | `admin123` | **ADMIN** | Pengelolaan Customer, Pembuatan Pengajuan, **Approve & Reject Pengajuan** |
| `staff@easycredit.test` | `staff123` | **STAFF** | Pengelolaan Customer, Pembuatan Pengajuan & Simulasi Cicilan |

---

## 4. Panduan Menjalankan Aplikasi

### Langkah 1 — Jalankan Database via Docker Compose
Jalankan PostgreSQL dan Oracle secara otomatis:

```bash
docker-compose up -d
```

Skrip inisialisasi pada folder `EasyCreditApi/Database/` akan dieksekusi secara otomatis:
* `01_postgres_schema_and_seed.sql`: Membuat tabel `mst_user`, `mst_customer` dan menyuntikkan user demo.
* `02_oracle_schema_and_sp.sql`: Membuat tabel transaksi `TRN_CREDIT_APPLICATION`, `TRN_CREDIT_INSTALLMENT` dan Stored Procedures `sp_calculate_installment` & `sp_approve_application`.

### Langkah 2 — Jalankan Backend API
Buka Terminal 1:

```bash
dotnet run --project EasyCreditApi
```
* **URL:** `http://localhost:5052`
* **Swagger UI:** `http://localhost:5052/swagger`

### Langkah 3 — Jalankan Frontend MVC
Buka Terminal 2:

```bash
dotnet run --project EasyCreditFrontend
```
* **URL:** `http://localhost:5200`

---

## 5. Kontrak API (EasyCreditApi)

Semua endpoint `/api/*` mewajibkan header `X-Api-Key: easycredit-secret-api-key-2026`.
Semua response dibungkus dalam format standar `ApiResponse<T>`:

```json
{
  "status": true,
  "message": "Success",
  "data": { ... }
}
```

| Method | Endpoint | Autentikasi | Keterangan |
|---|---|---|---|
| `POST` | `/api/auth/login` | X-Api-Key | Login user, menghasilkan token JWT |
| `POST` | `/api/auth/register` | X-Api-Key | Registrasi user baru |
| `GET` | `/api/customers` | X-Api-Key + JWT | Daftar customer (paging & keyword search) |
| `GET` | `/api/customers/{id}` | X-Api-Key + JWT | Detail customer berdasarkan ID |
| `POST` | `/api/customers` | X-Api-Key + JWT | Tambah customer baru (validasi NIK unik) |
| `PUT` | `/api/customers/{id}` | X-Api-Key + JWT | Ubah data customer |
| `DELETE` | `/api/customers/{id}` | X-Api-Key + JWT | Hapus customer |
| `GET` | `/api/credit-applications` | X-Api-Key + JWT | Daftar pengajuan (filter status, keyword, paging) |
| `GET` | `/api/credit-applications/{id}` | X-Api-Key + JWT | Detail pengajuan (header + customer + jadwal cicilan) |
| `POST` | `/api/credit-applications/preview-installment` | X-Api-Key + JWT | Simulasi cicilan via SP Oracle tanpa simpan |
| `POST` | `/api/credit-applications` | X-Api-Key + JWT | Buat pengajuan baru (transaksi Oracle header + cicilan) |
| `PUT` | `/api/credit-applications/{id}/approve` | X-Api-Key + ADMIN | Setujui pengajuan kredit via SP Oracle |
| `PUT` | `/api/credit-applications/{id}/reject` | X-Api-Key + ADMIN | Tolak pengajuan kredit via SP Oracle |

---

## 6. Ringkasan Implementasi Tugas (T1 – T7)

1. **T1 (Login & Auth):**
   * Endpoint login memverifikasi password dengan hashing BCrypt dari tabel `mst_user` PostgreSQL.
   * Token JWT mencakup claim wajib `sub`, `email`, dan `role`.
   * Frontend menyimpan JWT di session server-side (`SessionKeys.Token`), tidak disimpan di `localStorage` peramban.
   * `ApiKeyMiddleware` memvalidasi header `X-Api-Key` secara ketat.
2. **T2 (Customer CRUD):**
   * Query menggunakan EF Core LINQ async dengan `EF.Functions.ILike` untuk pencarian fleksibel pada PostgreSQL.
   * NIK dijamin unik (menghasilkan status HTTP 409 Conflict jika duplikat).
   * AJAX search, pagination, create, edit, dan delete konfirmasi pada frontend.
3. **T3 (Pengajuan Kredit & Preview Cicilan):**
   * Form pengajuan dilengkapi tombol "Hitung Cicilan" via AJAX ke Stored Procedure Oracle `sp_calculate_installment`.
   * Preview menampilkan jadwal cicilan secara interaktif tanpa reload halaman.
   * Submit pengajuan memvalidasi keberadaan nasabah di PostgreSQL, lalu menyimpan header `TRN_CREDIT_APPLICATION` dan seluruh baris `TRN_CREDIT_INSTALLMENT` dalam satu transaksi Oracle (ACID).
4. **T4 (List & Detail Pengajuan):**
   * Daftar pengajuan mengambil data transaksi dari Oracle dan melakukan resolusi nama nasabah dari PostgreSQL secara efisien di service layer (tanpa SQL JOIN lintas DB).
   * Halaman detail menampilkan informasi lengkap: nasabah, parameter pinjaman, dan rincian angsuran.
5. **T5 (Approve & Reject):**
   * Dilindungi otorisasi `[Authorize(Roles = "ADMIN")]`. User ber-role `STAFF` ditolak dengan HTTP 403 Forbidden.
   * Memanggil Stored Procedure `sp_approve_application`. Status hanya boleh diubah dari `PENDING` atau `SUBMITTED`.
   * Approve/Reject kedua otomatis ditolak dengan pesan error validasi.
6. **T6 (Validasi & Error Handling):**
   * Validasi ganda: DataAnnotation di ViewModel MVC dan validasi ulang di Application Service API.
   * `ExceptionMiddleware` memetakan exception ke status HTTP: 400 (Validation), 401 (Unauthorized), 403 (Forbidden), 404 (Not Found), 409 (Conflict), 500 (Internal Server Error).
7. **T7 (UI & AJAX):**
   * Layout Razor modern dan konsisten, tombol ter-disable saat request berlangsung, notifikasi error/sukses ramah via toast.

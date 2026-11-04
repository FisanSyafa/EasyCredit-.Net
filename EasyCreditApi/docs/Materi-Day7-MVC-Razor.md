# Materi Day 7 — ASP.NET Core MVC, Razor, Data Annotation & jQuery AJAX

> **Root repository:** `EasyCredit/`  
> **Project API:** `EasyCreditApi/` (.NET 10) — JSON di `/api`  
> **Project Frontend:** `EasyCreditFrontend/` (.NET 10 MVC) — Razor + jQuery AJAX  
> **Database:** Oracle `localhost:1521/FREEPDB1` (Day 5–6)  
> **Prasyarat:** Day 5 (Dapper inline) dan Day 6 (stored procedure) sudah selesai  
> **Fokus:** MVC, Controller, Razor View Engine, Model/DTO/ViewModel, Data Annotation, integrasi template, jQuery AJAX

---

## 0. Hasil akhir Day 7

Setelah materi ini selesai, repository punya **dua project sibling** di bawah satu folder root:

```text
EasyCredit/                        ← root repository
├── EasyCredit.slnx                ← solution dua project
├── README.md
├── EasyCreditApi/                 ← Web API (JSON)
│   ├── EasyCreditApi.csproj
│   ├── Program.cs
│   ├── Controllers/               ← API controllers
│   ├── Application/
│   ├── Domain/
│   ├── Infrastructure/
│   ├── Middleware/
│   ├── docs/
│   └── scripts/
└── EasyCreditFrontend/            ← project MVC terpisah
    ├── EasyCreditFrontend.csproj
    ├── Program.cs
    ├── Controllers/
    ├── Models/
    │   ├── Dtos/
    │   └── ViewModels/
    ├── Services/
    ├── Views/
    └── wwwroot/
```

Keduanya **tidak saling bersarang**. `EasyCreditFrontend` bukan subfolder dari
`EasyCreditApi`, sehingga tidak perlu exclude `Compile Remove` di csproj API.

### 0.1 Arsitektur

```text
Browser
  │  HTML + jQuery AJAX
  ▼
EasyCreditFrontend (ASP.NET Core MVC)
  │  ApiClient (HttpClient)
  │  Header: X-Api-Key + Authorization: Bearer <JWT>
  ▼
EasyCreditApi (ASP.NET Core Web API)
  ├── PostgreSQL / in-memory user  → auth
  └── Oracle + Dapper / SP         → customer & credit application
```

Pemisahan tanggung jawab:

| Project | Peran | Teknologi |
|---|---|---|
| `EasyCreditApi` | Logika bisnis, validasi, akses database, auth | Web API, Dapper/Oracle, JWT, API key |
| `EasyCreditFrontend` | Tampilan web, Razor, consume API | ASP.NET Core MVC, Razor, jQuery AJAX, `HttpClient` |

**Frontend tidak mengakses database secara langsung.** Semua data diambil lewat API.

### 0.2 Halaman frontend

| URL | Action | Tipe response | Keterangan |
|---|---|---|---|
| `/Auth/Login` | `Auth.Login` GET/POST | View | Login + validasi |
| `/` | `Home.Index` | View | Dashboard |
| `/Customers` | `Customers.Index` | View | Daftar customer |
| `/Customers/Rows` | `Customers.Rows` | PartialView | AJAX search/paging |
| `/Customers/Create` | `Customers.Create` GET/POST | View | Form + validasi |
| `/Customers/Edit/{id}` | `Customers.Edit` GET/POST | View | Form edit |
| `/Customers/Delete` | `Customers.Delete` POST | JSON | AJAX hapus |
| `/CreditApplications` | `CreditApplications.Index` | View | Daftar pengajuan |
| `/CreditApplications/Approve` | `CreditApplications.Approve` POST | JSON | AJAX approve |
| `/Home/Error` | `Home.Error` | View | 404/500 |

### 0.3 File utama yang ditambahkan

**Frontend:**

```text
EasyCreditFrontend/EasyCreditFrontend.csproj
EasyCreditFrontend/Program.cs
EasyCreditFrontend/appsettings.json
EasyCreditFrontend/Properties/launchSettings.json
EasyCreditFrontend/Services/ApiClient.cs
EasyCreditFrontend/Services/SessionKeys.cs
EasyCreditFrontend/Models/ApiResponse.cs
EasyCreditFrontend/Models/Dtos/CustomerDto.cs
EasyCreditFrontend/Models/Dtos/LoginDataDto.cs
EasyCreditFrontend/Models/Dtos/ApplicationListItemDto.cs
EasyCreditFrontend/Models/ViewModels/*.cs
EasyCreditFrontend/Controllers/BaseController.cs
EasyCreditFrontend/Controllers/AuthController.cs
EasyCreditFrontend/Controllers/HomeController.cs
EasyCreditFrontend/Controllers/CustomersController.cs
EasyCreditFrontend/Controllers/CreditApplicationsController.cs
EasyCreditFrontend/Views/**
EasyCreditFrontend/wwwroot/**
```

**Root repository:**

```text
EasyCredit.slnx                  (solution dua project sibling)
README.md
```

**API:**

```text
EasyCreditApi/wwwroot/.gitkeep   (folder statis kosong; API tidak memakai static files)
```

### 0.4 Timeline sesi

| Waktu | Durasi | Sesi | Bagian dokumen |
|---|---|---|---|
| 13.00–13.15 | 15 min | Recap Day 5–6 + arsitektur dua project | §0–§1 |
| 13.15–13.45 | 30 min | Konsep MVC + Razor + routing | §2 |
| 13.45–14.15 | 30 min | Model, DTO, ViewModel, Data Annotation | §3 |
| 14.15–14.45 | 30 min | Integrasi layout + static assets | §4 |
| 14.45–15.15 | 30 min | Live coding halaman Customer + ApiClient | §5–§6 |
| 15.15–15.30 | 15 min | Break | — |
| 15.30–16.00 | 30 min | jQuery AJAX: search, delete, approve | §5, §6 |
| 16.00–16.30 | 30 min | Workshop: login + session + proteksi halaman | §6, §9 |
| 16.30–16.50 | 20 min | Verifikasi & troubleshooting | §7–§8 |
| 16.50–17.00 | 10 min | Checklist & bridge Day 8 | §10 |

---

## 1. Urutan pengerjaan

Ikuti urutan ini. Jangan membuat view sebelum controller dan kontrak API jelas.

1. Pahami alur dua project: browser → MVC → API → database.
2. Pastikan API hanya melayani `/api`.
3. Buat project `EasyCreditFrontend` + solution.
4. Konfigurasi `ApiClient`, session, dan `ApiSettings`.
5. Buat DTO response API + ViewModel halaman.
6. Buat `BaseController` + `AuthController` (login/session).
7. Buat controller MVC: Home, Customers, CreditApplications.
8. Buat layout Razor + halaman login + partial view.
9. Pasang jQuery AJAX untuk search, delete, dan approve.
10. Jalankan dua app, login, dan verifikasi.

---

## 2. Konsep MVC dan Pemisahan Frontend/API

### 2.1 Tiga peran utama

| Komponen | Tugas | Di project frontend |
|---|---|---|
| **Model** | Bentuk data dan aturan validasi | `Models/Dtos`, `Models/ViewModels` |
| **View** | Tampilan HTML | `Views/**/*.cshtml` |
| **Controller** | Menerima request, memanggil `ApiClient`, memilih view | `Controllers/*.cs` |

Alur request halaman:

```text
Browser
  │ GET /Customers?keyword=andi
  ▼
CustomersController.Index(keyword, page)
  ▼
ApiClient.GetAsync<PagedData<CustomerDto>>("/api/customers?...")
  ▼
EasyCreditApi → Oracle
  ▼
Customers/Index.cshtml (Razor)
  ▼
Browser menerima HTML lengkap
```

Alur request AJAX:

```text
jQuery $.ajax
  │ GET /Customers/Rows?keyword=andi&page=1
  ▼
CustomersController.Rows(...)
  ▼
ApiClient → API
  ▼
PartialView "_CustomerList"
  ▼
jQuery mengganti isi <div id="customerList">
```

### 2.2 Kenapa frontend dipisah?

| Alasan | Penjelasan |
|---|---|
| Tanggung jawab | API fokus JSON; frontend fokus HTML/UX |
| Keamanan | Token JWT disimpan di session server-side frontend |
| Skalabilitas | Frontend dan API bisa di-deploy terpisah |
| Konsistensi | API bisa dipakai Swagger, mobile, atau aplikasi lain |
| Struktur final project | Mengikuti pola `EasyCreditFrontend` + API |

Frontend **tidak** memanggil service/repository API secara langsung. Ia memanggil endpoint HTTP.

### 2.3 Login dan session

```text
1. User submit form login di frontend
2. AuthController → ApiClient.PostAsync<LoginDataDto>("/api/auth/login")
3. API memvalidasi password dan membuat JWT
4. Frontend menyimpan token di session server-side:
      SessionKeys.Token, SessionKeys.Role, SessionKeys.Name
5. Request berikutnya:
      ApiClient menambahkan Authorization: Bearer <token>
6. BaseController menolak halaman jika session token kosong
```

Session bukan `localStorage`. Token tidak pernah dikirim ke JavaScript browser.

### 2.4 Controller MVC vs Controller API

| Aspek | Controller API | Controller MVC |
|---|---|---|
| Project | `EasyCreditApi` | `EasyCreditFrontend` |
| Base class | `ControllerBase` | `Controller` |
| Atribut | `[ApiController]` | tidak wajib |
| Routing | atribut `[Route("api/...")]` | conventional route |
| Return | `Ok`, `NotFound`, `CreatedAtAction` | `View`, `PartialView`, `RedirectToAction`, `Json` |
| Response | JSON `ApiResponse<T>` | HTML / JSON kecil |
| Autentikasi | JWT + API key | session frontend |

Karena berada di project berbeda, kedua project boleh memiliki nama controller yang sama:

```text
EasyCreditApi/Controllers/CustomersController.cs          → /api/customers
EasyCreditFrontend/Controllers/CustomersController.cs     → /Customers
```

### 2.5 Razor View Engine

Razor menggabungkan HTML dan C#:

```cshtml
@model CustomerIndexViewModel

<h2>@Model.Total customer</h2>

@foreach (var item in Model.Items)
{
    <tr>
        <td>@item.Name</td>
        <td>@item.Nik</td>
    </tr>
}
```

File penting:

| File | Fungsi |
|---|---|
| `Views/_ViewStart.cshtml` | Layout default (`Layout = "_Layout"`) |
| `Views/_ViewImports.cshtml` | `@using` + `@addTagHelper` |
| `Views/Shared/_Layout.cshtml` | Kerangka HTML + sidebar/topbar |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | jQuery validation |
| `Views/Customers/_CustomerList.cshtml` | Partial tabel customer |
| `Views/Customers/_CustomerForm.cshtml` | Partial field form |

`_ViewImports.cshtml`:

```cshtml
@using EasyCreditFrontend
@using EasyCreditFrontend.Models
@using EasyCreditFrontend.Models.Dtos
@using EasyCreditFrontend.Models.ViewModels
@using EasyCreditFrontend.Services
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

Tanpa `@addTagHelper`, `asp-for`, `asp-action`, dan `asp-validation-for` tidak bekerja.

### 2.6 Tag Helper yang dipakai

| Tag Helper | Contoh | Hasil |
|---|---|---|
| `asp-for` | `<input asp-for="Name" />` | name, id, value, atribut validasi |
| `asp-validation-for` | `<span asp-validation-for="Name"></span>` | pesan error per field |
| `asp-validation-summary` | `<div asp-validation-summary="ModelOnly"></div>` | error global |
| `asp-action` | `<form asp-action="Create">` | URL action otomatis |
| `asp-controller` | `<a asp-controller="Customers">` | URL controller |
| `asp-route-id` | `<a asp-route-id="@item.CustomerId">` | parameter route |
| `asp-append-version` | `<link asp-append-version="true" />` | cache busting asset |

### 2.7 Routing

Frontend memakai conventional routing:

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

| URL | Controller | Action | Parameter |
|---|---|---|---|
| `/` | `Home` | `Index` | — |
| `/Customers` | `Customers` | `Index` | — |
| `/Customers/Create` | `Customers` | `Create` | — |
| `/Customers/Edit/5` | `Customers` | `Edit` | `id = 5` |
| `/CreditApplications` | `CreditApplications` | `Index` | — |

API memakai attribute routing:

```csharp
app.MapControllers(); // /api/customers, /api/credit-applications, /api/auth/login
```

### 2.8 Program.cs frontend

```csharp
using EasyCreditFrontend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]
        ?? "http://localhost:5052");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

Urutan penting:

- `UseStaticFiles()` sebelum routing agar CSS/JS tersedia.
- `UseSession()` sebelum controller dijalankan.
- `UseStatusCodePagesWithReExecute` agar 404/500 tampil sebagai halaman Razor.

### 2.9 Program.cs API

API tetap fokus JSON:

```csharp
builder.Services.AddControllers();
```

Pipeline API:

```csharp
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

API tidak memakai `UseStaticFiles`, `UseSession`, atau `MapControllerRoute`.

---

## 3. Model, DTO, dan Data Annotation Validation

### 3.1 Tiga jenis model

| Istilah | Lokasi | Tujuan | Contoh |
|---|---|---|---|
| **Entity** | API `Domain/Entities` | Bentuk data database | `Customer`, `CreditApplication` |
| **DTO** | API `Application/DTO`, frontend `Models/Dtos` | Kontrak request/response API | `CustomerDto`, `LoginDataDto` |
| **ViewModel** | Frontend `Models/ViewModels` | Kebutuhan satu halaman MVC | `CustomerFormViewModel`, `CustomerIndexViewModel` |

Kenapa dipisah?

- Entity bisa berisi kolom internal yang tidak boleh dikirim ke browser.
- DTO menjaga kontrak API tetap stabil.
- ViewModel boleh berisi paging, keyword, dropdown, dan pesan validasi khusus halaman.

### 3.2 DTO response API di frontend

API membungkus semua response:

```json
{
  "status": true,
  "message": "Success",
  "data": { }
}
```

Model frontend:

```csharp
public class ApiResponse<T>
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public bool IsSuccess => Status;
}
```

Bentuk data customer:

```csharp
public class PagedData<T>
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}
```

Bentuk data pengajuan:

```csharp
public class TotalItemsData<T>
{
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}
```

DTO:

```csharp
public class CustomerDto
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Nik { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
```

```csharp
public class LoginDataDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
```

### 3.3 ViewModel + Data Annotation

`Models/ViewModels/CustomerFormViewModel.cs`:

```csharp
public class CustomerFormViewModel
{
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Nama wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter.")]
    [Display(Name = "Nama lengkap")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "NIK wajib diisi.")]
    [RegularExpression(@"^\d{16,20}$", ErrorMessage = "NIK harus 16-20 digit angka.")]
    [Display(Name = "NIK")]
    public string Nik { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter.")]
    [Phone(ErrorMessage = "Format nomor telepon tidak valid.")]
    [Display(Name = "No. telepon")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    [StringLength(100, ErrorMessage = "Email maksimal 100 karakter.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(100, ErrorMessage = "Alamat maksimal 100 karakter.")]
    [Display(Name = "Alamat")]
    public string? Address { get; set; }
}
```

Atribut yang sering dipakai:

| Atribut | Fungsi |
|---|---|
| `[Required]` | Field wajib |
| `[StringLength(max)]` | Batas panjang string |
| `[RegularExpression]` | Pola tertentu, mis. NIK |
| `[EmailAddress]` | Format email |
| `[Phone]` | Format telepon |
| `[Range]` | Batas nilai angka |
| `[Display(Name = "...")]` | Label otomatis di form |

### 3.4 Alur validasi

```text
User submit form
   │
   ├─ 1. Client-side: jQuery Validation Unobtrusive
   │     membaca atribut data-val-* dari Data Annotation
   │
   ├─ 2. Server: model binding mengisi CustomerFormViewModel
   │
   ├─ 3. Server: ModelState.IsValid
   │     false → return View(model) + pesan error
   │     true  → ApiClient POST/PUT ke API
   │
   └─ 4. API: validasi bisnis
         mis. NIK duplikat → 409 Conflict → pesan ke ModelState
```

Controller frontend:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CustomerFormViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var response = await _apiClient.PostAsync<CustomerDto>("/api/customers", new
    {
        name = model.Name,
        nik = model.Nik,
        phone = model.Phone,
        email = model.Email,
        address = model.Address
    });

    if (response is null || !response.IsSuccess || response.Data is null)
    {
        ModelState.AddModelError(
            string.Empty,
            response?.Message ?? "Gagal menyimpan customer.");

        return View(model);
    }

    TempData["Success"] = response.Message;
    return RedirectToAction(nameof(Index));
}
```

View:

```cshtml
@model CustomerFormViewModel

<form asp-action="Create" method="post">
    <div asp-validation-summary="ModelOnly" class="validation-summary"></div>

    <label asp-for="Name" class="form-label"></label>
    <input asp-for="Name" class="form-control" />
    <span asp-validation-for="Name" class="field-error"></span>

    <button type="submit" class="btn btn-accent">Simpan customer</button>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

### 3.5 Validasi client-side

`Views/Shared/_ValidationScriptsPartial.cshtml`:

```cshtml
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"></script>
```

Atribut seperti `[Required]` dan `[EmailAddress]` otomatis menjadi validasi browser. Validasi server tetap wajib karena client-side bisa dimatikan.

### 3.6 Kesalahan umum

| Kesalahan | Dampak | Perbaikan |
|---|---|---|
| Memakai entity API langsung di form | Overposting / field internal bocor | Pakai ViewModel |
| Hanya validasi client-side | Bisa dilewati | Selalu cek `ModelState.IsValid` |
| Lupa `[ValidateAntiForgeryToken]` | Form rentan CSRF | Tambahkan atribut + token |
| Lupa `asp-validation-for` | Pesan error tidak muncul | Tambahkan span per field |
| Validasi bisnis diduplikasi di frontend | Aturan tidak konsisten | Validasi utama tetap di API service |

---

## 4. Integrasi Template HTML/CSS/JS ke Razor Layout

### 4.1 Struktur `wwwroot` frontend

```text
EasyCreditFrontend/wwwroot/
├── css/site.css
├── js/site.js
└── lib/
    ├── bootstrap/dist/css/bootstrap.min.css
    ├── bootstrap/dist/js/bootstrap.bundle.min.js
    ├── jquery/dist/jquery.min.js
    ├── jquery-validation/dist/jquery.validate.min.js
    └── jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js
```

Aktifkan static files di `EasyCreditFrontend/Program.cs`:

```csharp
app.UseStaticFiles();
```

Referensikan asset dari Razor:

```cshtml
<link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />

<script src="~/lib/jquery/dist/jquery.min.js"></script>
<script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
<script src="~/js/site.js" asp-append-version="true"></script>
```

`asp-append-version="true"` menambahkan query `?v=...` agar browser tidak memakai cache lama setelah file diubah.

### 4.2 Anatomi `_Layout.cshtml`

```cshtml
@using EasyCreditFrontend.Services
@{
    var isLoggedIn = !string.IsNullOrEmpty(Context.Session.GetString(SessionKeys.Token));
    var userName = Context.Session.GetString(SessionKeys.Name) ?? "User";
    var role = Context.Session.GetString(SessionKeys.Role) ?? "STAFF";
}
<!DOCTYPE html>
<html lang="id">
<head>
    <title>@ViewData["Title"] · EasyCredit</title>
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body class="@(isLoggedIn ? string.Empty : "is-auth")">
    <div class="app-shell">
        @if (isLoggedIn)
        {
            <aside class="sidebar">...</aside>
        }

        <div class="app-main">
            @if (isLoggedIn)
            {
                <header class="topbar">
                    <h1>@ViewData["Title"]</h1>
                    <span>@userName · @role</span>
                </header>
            }

            <main class="page-content @(isLoggedIn ? "" : "auth-page")">
                @RenderBody()
            </main>
        </div>
    </div>

    @Html.AntiForgeryToken()

    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

Poin penting:

- `@RenderBody()` adalah tempat view halaman disisipkan.
- `@RenderSectionAsync("Scripts", required: false)` memungkinkan view menambahkan script sendiri.
- `isLoggedIn` menentukan sidebar/topbar/auth page.
- `@Html.AntiForgeryToken()` menyediakan token global untuk AJAX.
- Layout tidak boleh berisi logika bisnis.

### 4.3 Menu aktif

```cshtml
@{
    var controller = (string?)ViewContext.RouteData.Values["controller"] ?? string.Empty;

    string NavClass(string name) =>
        string.Equals(controller, name, StringComparison.OrdinalIgnoreCase)
            ? "nav-link is-active"
            : "nav-link";
}

<a class="@NavClass("Customers")" asp-controller="Customers" asp-action="Index">
    Customer
</a>
```

### 4.4 TempData untuk notifikasi

Setelah redirect, gunakan `TempData`:

```csharp
TempData["Success"] = "Customer berhasil dibuat.";
return RedirectToAction(nameof(Index));
```

Layout menampilkannya sekali:

```cshtml
@if (TempData["Success"] is string success)
{
    <div class="alert alert-success" data-alert>@success</div>
}
```

`site.js` menutup alert otomatis setelah 6 detik.

### 4.5 Partial view

`Views/Customers/_CustomerList.cshtml`:

```cshtml
@model CustomerIndexViewModel

@if (!Model.Items.Any())
{
    <div class="panel">
        <div class="empty-state">
            <h4>Belum ada customer</h4>
        </div>
    </div>
}
else
{
    <div class="panel">
        <div class="table-wrap">
            <table class="data-table">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Nama</th>
                        <th>NIK</th>
                        <th>Aksi</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in Model.Items)
                    {
                        <tr>
                            <td class="mono">#@item.CustomerId</td>
                            <td>@item.Name</td>
                            <td class="mono">@item.Nik</td>
                            <td>
                                <button type="button"
                                        class="btn btn-danger-soft btn-sm js-delete"
                                        data-id="@item.CustomerId"
                                        data-name="@item.Name">
                                    Hapus
                                </button>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>

        <div class="panel-foot">
            <button type="button"
                    class="btn btn-light btn-sm js-page"
                    data-page="@(Model.Page - 1)"
                    disabled="@(!Model.HasPreviousPage)">
                Sebelumnya
            </button>
            <span>@Model.Page / @Model.TotalPages</span>
            <button type="button"
                    class="btn btn-light btn-sm js-page"
                    data-page="@(Model.Page + 1)"
                    disabled="@(!Model.HasNextPage)">
                Berikutnya
            </button>
        </div>
    </div>
}
```

Render dari halaman:

```cshtml
<div id="customerList">
    <partial name="_CustomerList" model="Model" />
</div>
```

Render dari controller (AJAX):

```csharp
[HttpGet]
public async Task<IActionResult> Rows(string? keyword, int page = 1, int size = 10)
{
    var model = await BuildIndexAsync(keyword, page, size);
    return PartialView("_CustomerList", model);
}
```

### 4.6 Section Scripts

```cshtml
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

Atau script AJAX:

```cshtml
@section Scripts {
    <script>
        $(function () {
            // kode AJAX halaman ini
        });
    </script>
}
```

### 4.7 File `site.js`

`wwwroot/js/site.js` menyediakan helper global:

| Helper | Fungsi |
|---|---|
| `EC.token()` | Mengambil token antiforgery |
| `EC.toast(type, message, title)` | Notifikasi kanan atas |
| `EC.confirm(options)` | Modal konfirmasi berbasis Promise |
| `EC.formatRupiah(value)` | Format angka rupiah |

Contoh:

```javascript
EC.toast('success', 'Customer berhasil dihapus.', 'Berhasil');

var ok = await EC.confirm({
    title: 'Hapus customer?',
    message: 'Data akan dihapus permanen.',
    confirmText: 'Ya, hapus',
    danger: true
});
```

### 4.8 Prinsip integrasi template

- Simpan CSS/JS di `wwwroot` frontend, bukan di folder `Views`.
- Gunakan layout untuk sidebar, topbar, footer, dan script global.
- Gunakan partial untuk potongan yang dirender ulang AJAX.
- Jangan menaruh connection string atau secret di JavaScript.
- Gunakan `asp-append-version="true"` untuk asset yang sering berubah.

---

## 5. Setup jQuery AJAX untuk Consume Endpoint Controller MVC

### 5.1 Dua pola response

| Pola | Response | Cocok untuk | Contoh |
|---|---|---|---|
| **PartialView** | Potongan HTML | Tabel, list, pagination | `GET /Customers/Rows` |
| **JSON** | Objek kecil | Aksi delete/approve, update status | `POST /Customers/Delete` |

Untuk tabel kompleks, PartialView lebih aman karena HTML di-encode Razor. Untuk aksi, JSON lebih ringkas.

### 5.2 Anti-forgery token

Semua POST MVC memakai `[ValidateAntiForgeryToken]`. Token dikirim dengan dua cara:

1. Field form: `__RequestVerificationToken`
2. Header: `RequestVerificationToken`

`site.js` mengatur header global:

```javascript
var currentToken = EC.token();
if (currentToken) {
    $.ajaxSetup({
        headers: { 'RequestVerificationToken': currentToken }
    });
}
```

`EC.token()` membaca:

```javascript
$('input[name="__RequestVerificationToken"]').first().val()
```

Tanpa token, server mengembalikan **400 Bad Request**.

### 5.3 AJAX pencarian + pagination (PartialView)

View `Customers/Index.cshtml`:

```cshtml
<form id="customerSearchForm" asp-action="Index" method="get" class="toolbar">
    <input id="customerKeyword"
           name="keyword"
           value="@Model.Keyword"
           class="form-control"
           placeholder="Cari nama, NIK, atau email…"
           autocomplete="off" />
    <button type="submit" class="btn btn-dark">Cari</button>
</form>

<div id="customerList">
    <partial name="_CustomerList" model="Model" />
</div>
```

JavaScript:

```javascript
$(function () {
    var searchUrl = '@Url.Action("Rows", "Customers")';
    var defaultPageSize = @Model.Size;
    var timer = null;

    function currentPageSize() {
        // Ambil nilai dropdown saat ini, bukan nilai awal dari model.
        return $('#customerSearchForm select[name="size"]').val() || defaultPageSize;
    }

    function loadCustomers(page) {
        $.ajax({
            url: searchUrl,
            type: 'GET',
            data: {
                keyword: $('#customerKeyword').val(),
                page: page,
                size: currentPageSize()
            },
            success: function (html) {
                $('#customerList').html(html);
            },
            error: function () {
                EC.toast('error', 'Gagal memuat daftar customer.', 'Gagal');
            }
        });
    }

    $('#customerKeyword').on('input', function () {
        clearTimeout(timer);
        timer = setTimeout(function () {
            loadCustomers(1);
        }, 350);
    });

    $('#customerSearchForm').on('submit', function (event) {
        event.preventDefault();
        loadCustomers(1);
    });

    // Ganti jumlah baris langsung memuat ulang tabel dari halaman 1.
    $('#customerSearchForm select[name="size"]').on('change', function () {
        loadCustomers(1);
    });

    $('#customerList').on('click', '.js-page', function () {
        loadCustomers($(this).data('page'));
    });
});
```

> **Sering salah:** memakai `size: @Model.Size` di dalam AJAX. Nilai itu adalah
> nilai saat halaman pertama kali dirender, bukan nilai dropdown yang sedang
> dipilih user. Akibatnya user memilih "5 baris" tetapi request tetap memakai
> `size=10`. Selalu baca nilai dropdown saat request dikirim.

Kenapa `event.preventDefault()`?

- Form tetap berfungsi tanpa JavaScript (server-side search).
- Dengan JavaScript, submit dicegat agar tidak reload halaman.

Kenapa event delegation?

- Tombol pagination baru tetap memiliki handler setelah partial diganti.
- `$('.js-page').on(...)` langsung tidak mengikat tombol baru.

### 5.4 AJAX delete (JSON)

Controller frontend:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id)
{
    var response = await _apiClient.DeleteAsync($"/api/customers/{id}");

    return Json(new
    {
        status = response?.IsSuccess ?? false,
        message = response?.Message ?? "Gagal menghapus customer."
    });
}
```

JavaScript:

```javascript
$('#customerList').on('click', '.js-delete', async function () {
    var button = $(this);
    var id = button.data('id');
    var name = button.data('name');

    var ok = await EC.confirm({
        title: 'Hapus customer?',
        message: 'Customer "' + name + '" akan dihapus permanen.',
        confirmText: 'Ya, hapus',
        danger: true
    });

    if (!ok) return;

    button.prop('disabled', true);

    $.ajax({
        url: '@Url.Action("Delete", "Customers")',
        type: 'POST',
        data: {
            id: id,
            __RequestVerificationToken: EC.token()
        },
        success: function (response) {
            if (response.status) {
                EC.toast('success', response.message, 'Berhasil');
                loadCustomers(@Model.Page);
            } else {
                button.prop('disabled', false);
                EC.toast('error', response.message, 'Gagal');
            }
        },
        error: function () {
            button.prop('disabled', false);
            EC.toast('error', 'Tidak dapat menghubungi server.', 'Gagal');
        }
    });
});
```

### 5.5 AJAX approve (JSON + stored procedure)

Controller frontend:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Approve(ApproveApplicationViewModel model)
{
    if (!ModelState.IsValid)
    {
        var message = string.Join(" ", ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage));

        return Json(new { status = false, message });
    }

    var response = await _apiClient.PostAsync<object>(
        $"/api/credit-applications/{model.ApplicationId}/approve",
        new { approver = model.Approver });

    if (response is null || !response.IsSuccess)
    {
        return Json(new
        {
            status = false,
            message = response?.Message ?? "Approve gagal."
        });
    }

    return Json(new
    {
        status = true,
        message = response.Message,
        applicationId = model.ApplicationId,
        newStatus = "APPROVED",
        approver = model.Approver
    });
}
```

ViewModel:

```csharp
public class ApproveApplicationViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Application ID tidak valid.")]
    public int ApplicationId { get; set; }

    [Required(ErrorMessage = "Nama approver wajib diisi.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nama approver 2-100 karakter.")]
    public string Approver { get; set; } = string.Empty;
}
```

JavaScript:

```javascript
$('#approveSubmit').on('click', function () {
    var submit = $(this);
    var approver = $('#approverName').val().trim();

    if (!approver) {
        $('#approveError').text('Nama approver wajib diisi.');
        return;
    }

    submit.prop('disabled', true);

    $.ajax({
        url: '@Url.Action("Approve", "CreditApplications")',
        type: 'POST',
        data: {
            ApplicationId: currentId,
            Approver: approver,
            __RequestVerificationToken: EC.token()
        },
        success: function (response) {
            submit.prop('disabled', false);

            if (!response.status) {
                $('#approveError').text(response.message || 'Approve gagal.');
                return;
            }

            var row = $('tr[data-id="' + response.applicationId + '"]');
            row.find('.js-status')
               .removeClass('is-pending is-approved is-rejected')
               .addClass('is-approved')
               .text(response.newStatus);

            row.find('.js-approver').text(response.approver);
            row.find('.js-approve')
               .prop('disabled', true)
               .text('Selesai');

            modal.hide();
            EC.toast('success', response.message, 'Berhasil');
        },
        error: function () {
            submit.prop('disabled', false);
            $('#approveError').text('Tidak dapat menghubungi server.');
        }
    });
});
```

### 5.6 Checklist AJAX

- [ ] URL dibuat dengan `@Url.Action(...)`, bukan hardcode string.
- [ ] POST mengirim `__RequestVerificationToken`.
- [ ] Controller memakai `[ValidateAntiForgeryToken]`.
- [ ] Server tetap memvalidasi semua input.
- [ ] Response JSON memakai `status` boolean dan `message`.
- [ ] Event handler memakai delegation jika DOM diganti AJAX.
- [ ] Tombol di-disable saat request berjalan.
- [ ] Error response ditampilkan sebagai toast/pesan.
- [ ] Tidak ada secret/connection string di JavaScript.
- [ ] HTML PartialView di-encode Razor, bukan string concatenation.

---

## 6. Step-by-step patch code

### Step 1 — Siapkan solution dua project

Struktur akhir:

```text
EasyCredit/
├── EasyCredit.slnx
├── EasyCreditApi/
│   └── EasyCreditApi.csproj
└── EasyCreditFrontend/
    └── EasyCreditFrontend.csproj
```

Dari folder root `EasyCredit/`:

```bash
dotnet new sln -n EasyCredit
dotnet sln EasyCredit.slnx add EasyCreditApi/EasyCreditApi.csproj
dotnet sln EasyCredit.slnx add EasyCreditFrontend/EasyCreditFrontend.csproj
```

Karena `EasyCreditFrontend` berada **di luar** folder `EasyCreditApi`, tidak ada
konfigurasi exclude di `EasyCreditApi.csproj`. Masing-masing project hanya
mengompilasi file di foldernya sendiri.

> Kalau sebelumnya frontend sempat berada di dalam folder API, hapus blok
> `Compile Remove="EasyCreditFrontend/**"` setelah folder dipindah ke sibling.

### Step 2 — Konfigurasi API

API tetap API-only:

```csharp
builder.Services.AddControllers();
```

Tambahkan fallback Development untuk API key dan JWT:

```csharp
if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Konfigurasi 'Jwt:Key' wajib diisi.");

    jwtSettings.Key = "easycredit-dev-only-jwt-key-change-me-32chars";
    builder.Configuration["Jwt:Key"] = jwtSettings.Key;
}

if (string.IsNullOrWhiteSpace(builder.Configuration["ApiKey"]))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Konfigurasi 'ApiKey' wajib diisi.");

    builder.Configuration["ApiKey"] = "easycredit-dev-api-key";
}
```

API pipeline:

```csharp
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

### Step 3 — Konfigurasi frontend

`EasyCreditFrontend/appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5052",
    "ApiKey": "easycredit-dev-api-key"
  }
}
```

`Properties/launchSettings.json`:

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5200",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Step 4 — `ApiClient`

`Services/ApiClient.cs`:

```csharp
public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _apiKey;

    public ApiClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _apiKey = configuration["ApiSettings:ApiKey"] ?? string.Empty;
    }

    public Task<ApiResponse<T>?> GetAsync<T>(string url)
        => SendAsync<T>(HttpMethod.Get, url, null);

    public Task<ApiResponse<T>?> PostAsync<T>(string url, object? body = null)
        => SendAsync<T>(HttpMethod.Post, url, body);

    public Task<ApiResponse<T>?> PutAsync<T>(string url, object? body = null)
        => SendAsync<T>(HttpMethod.Put, url, body);

    public Task<ApiResponse<object>?> DeleteAsync(string url)
        => SendAsync<object>(HttpMethod.Delete, url, null);

    private async Task<ApiResponse<T>?> SendAsync<T>(
        HttpMethod method,
        string url,
        object? body)
    {
        using var request = new HttpRequestMessage(method, url);

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.TryAddWithoutValidation("X-Api-Key", _apiKey);
        }

        var token = _httpContextAccessor.HttpContext?.Session.GetString(SessionKeys.Token);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using var response = await _httpClient.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);

        return result ?? new ApiResponse<T>
        {
            Status = false,
            Message = "Response tidak valid"
        };
    }
}
```

Tambahkan penanganan `HttpRequestException`, timeout, dan `JsonException` seperti di file final.

### Step 5 — Session + BaseController + AuthController

`Services/SessionKeys.cs`:

```csharp
public static class SessionKeys
{
    public const string Token = "easycredit.token";
    public const string Role = "easycredit.role";
    public const string Name = "easycredit.name";
}
```

`BaseController`:

```csharp
public abstract class BaseController : Controller
{
    protected string? Token => HttpContext.Session.GetString(SessionKeys.Token);
    protected string Role => HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;
    protected string UserName => HttpContext.Session.GetString(SessionKeys.Name) ?? string.Empty;
    protected bool IsAdmin => string.Equals(Role, "ADMIN", StringComparison.OrdinalIgnoreCase);

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (string.Equals(
                context.ActionDescriptor.RouteValues["action"],
                "Error",
                StringComparison.OrdinalIgnoreCase))
        {
            base.OnActionExecuting(context);
            return;
        }

        if (string.IsNullOrEmpty(Token))
        {
            context.Result = RedirectToAction("Login", "Auth");
            return;
        }

        ViewBag.UserName = UserName;
        ViewBag.Role = Role;
        ViewBag.IsAdmin = IsAdmin;

        base.OnActionExecuting(context);
    }
}
```

`AuthController.Login`:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var response = await _apiClient.PostAsync<LoginDataDto>("/api/auth/login", new
    {
        email = model.Email,
        password = model.Password
    });

    if (response is null || !response.IsSuccess || response.Data is null)
    {
        ModelState.AddModelError(
            string.Empty,
            response?.Message ?? "Login gagal. Periksa email dan password.");

        return View(model);
    }

    HttpContext.Session.SetString(SessionKeys.Token, response.Data.AccessToken);
    HttpContext.Session.SetString(SessionKeys.Role, response.Data.Role);
    HttpContext.Session.SetString(SessionKeys.Name, response.Data.Name);

    return RedirectToAction("Index", "Home");
}
```

Akun demo API:

```text
admin@mail.com / Admin123!
```

### Step 6 — Controller MVC

| Controller | Endpoint API yang dipanggil |
|---|---|
| `HomeController` | `/api/customers`, `/api/credit-applications` |
| `CustomersController` | `/api/customers`, `/api/customers/{id}` |
| `CreditApplicationsController` | `/api/credit-applications`, `/api/credit-applications/{id}/approve` |

Customers index:

```csharp
private async Task<CustomerIndexViewModel> BuildIndexAsync(
    string? keyword,
    int page,
    int size)
{
    page = Math.Max(page, 1);
    size = Math.Clamp(size, 1, 100);

    var url = $"/api/customers?keyword={Uri.EscapeDataString(keyword ?? string.Empty)}" +
              $"&page={page}&size={size}";

    var response = await _apiClient.GetAsync<PagedData<CustomerDto>>(url);
    var data = response?.Data;

    var total = data?.Total ?? 0;
    var currentPage = data?.Page ?? page;
    var currentSize = data?.Size ?? size;
    var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)currentSize);

    return new CustomerIndexViewModel
    {
        Items = data?.Items ?? new List<CustomerDto>(),
        Keyword = keyword,
        Page = currentPage,
        Size = currentSize,
        Total = total,
        TotalPages = totalPages
    };
}
```

Credit applications index:

```csharp
var response = await _apiClient.GetAsync<TotalItemsData<ApplicationListItemDto>>(
    "/api/credit-applications");

var names = await LoadCustomerNamesAsync();

var rows = (response?.Data?.Items ?? new List<ApplicationListItemDto>())
    .Select(application => ToRow(application, names))
    .OrderByDescending(row => row.ApplicationId)
    .AsEnumerable();
```

Nama customer diambil lewat lookup terpisah:

```csharp
private async Task<Dictionary<int, string>> LoadCustomerNamesAsync()
{
    var response = await _apiClient.GetAsync<PagedData<CustomerDto>>(
        "/api/customers?page=1&size=100");

    return (response?.Data?.Items ?? new List<CustomerDto>())
        .ToDictionary(customer => customer.CustomerId, customer => customer.Name);
}
```

### Step 7 — Views

Urutan pembuatan:

1. `Views/_ViewImports.cshtml`
2. `Views/_ViewStart.cshtml`
3. `Views/Shared/_Layout.cshtml`
4. `Views/Shared/_ValidationScriptsPartial.cshtml`
5. `Views/Shared/Error.cshtml`
6. `Views/Auth/Login.cshtml`
7. `Views/Home/Index.cshtml`
8. `Views/Customers/Index.cshtml`
9. `Views/Customers/_CustomerList.cshtml`
10. `Views/Customers/_CustomerForm.cshtml`
11. `Views/Customers/Create.cshtml`
12. `Views/Customers/Edit.cshtml`
13. `Views/CreditApplications/Index.cshtml`

### Step 8 — Asset frontend

```text
EasyCreditFrontend/wwwroot/css/site.css
EasyCreditFrontend/wwwroot/js/site.js
EasyCreditFrontend/wwwroot/lib/...
```

Gunakan library lokal agar latihan tidak bergantung CDN.

### Step 9 — Build dan jalankan

Dari folder root `EasyCredit/`, build dua project:

```bash
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet build EasyCredit.slnx
```

Terminal 1 — API:

```bash
dotnet run --project EasyCreditApi
```

Terminal 2 — frontend:

```bash
dotnet run --project EasyCreditFrontend
```

Buka:

```text
http://localhost:5200/Auth/Login
```

Login:

```text
admin@mail.com / Admin123!
```

---

## 7. Verifikasi

### 7.1 Build

Dari folder root `EasyCredit/`:

```bash
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet build EasyCredit.slnx --nologo -v:minimal
```

Expected:

```text
EasyCreditApi net10.0 succeeded
EasyCreditFrontend net10.0 succeeded
Build succeeded
0 Error(s)
```

### 7.2 Dua app berjalan

| App | Port | Cek |
|---|---|---|
| API | `5052` | `http://localhost:5052/swagger` |
| Frontend | `5200` | `http://localhost:5200/Auth/Login` |

### 7.3 Proteksi halaman

Tanpa session:

```text
GET /              → 302 /Auth/Login
GET /Customers     → 302 /Auth/Login
GET /CreditApplications → 302 /Auth/Login
```

Dengan session setelah login:

```text
GET /              → 200 dashboard
GET /Customers     → 200 daftar customer
GET /Customers/Rows → 200 partial HTML
```

### 7.4 Login

```text
POST /Auth/Login
Email    = admin@mail.com
Password = Admin123!
```

Expected:

- `302` redirect ke `/`
- Session berisi token, role, name
- Dashboard menampilkan nama user dan tombol **Keluar**

### 7.5 API key dan JWT

Tanpa API key:

```bash
curl -i http://localhost:5052/api/customers
```

Expected: `401` + `{"status":false,"message":"Invalid or missing API key"}`.

Dengan API key tetapi tanpa token:

```bash
curl -i http://localhost:5052/api/customers \
  -H "X-Api-Key: easycredit-dev-api-key"
```

Expected: `401` + pesan token tidak valid.

### 7.6 AJAX

| Aksi | Network tab | Expected |
|---|---|---|
| Ketik keyword | `GET /Customers/Rows` | Tabel berubah tanpa reload |
| Klik pagination | `GET /Customers/Rows?page=2` | Halaman tabel berubah |
| Klik Hapus | `POST /Customers/Delete` | Toast sukses, baris hilang |
| Klik Approve | `POST /CreditApplications/Approve` | Status berubah `APPROVED` |
| Submit form kosong | tidak ada POST | Pesan validasi muncul |
| Matikan JavaScript | form submit biasa | Validasi server tetap muncul |

### 7.7 Static asset frontend

```bash
curl -I http://localhost:5200/css/site.css
curl -I http://localhost:5200/js/site.js
```

Expected: `200 OK`.

### 7.8 Swagger API

Buka:

```text
http://localhost:5052/swagger
```

Hanya endpoint `/api/...` yang muncul. Action MVC tidak boleh ada di Swagger karena berada di project frontend.

---

## 8. Troubleshooting

### 8.1 `Connection refused (localhost:5052)`

**Penyebab:** API belum berjalan atau port berbeda.

**Solusi:**

```bash
dotnet run --project EasyCreditApi
```

Cek `ApiSettings:BaseUrl` di `EasyCreditFrontend/appsettings.json`.

### 8.2 `Invalid or missing API key`

**Penyebab:** frontend tidak mengirim `X-Api-Key` atau nilainya berbeda.

**Solusi:**

- Frontend: `ApiSettings:ApiKey = easycredit-dev-api-key`
- API Development fallback: `ApiKey = easycredit-dev-api-key`
- Jika production, samakan nilai dari environment variable.

### 8.3 Login berhasil di API tetapi frontend tetap redirect ke login

**Penyebab:** session tidak aktif atau cookie tidak tersimpan.

**Solusi:**

- Pastikan `builder.Services.AddSession(...)`.
- Pastikan `app.UseSession()` dipanggil sebelum controller.
- Pastikan browser mengizinkan cookie.
- Cek `SessionKeys.Token` benar.

### 8.4 `IDX10703: key length is zero`

**Penyebab:** `Jwt:Key` kosong.

**Solusi:** API sudah punya fallback Development. Untuk production:

```bash
export Jwt__Key="secret-production-yang-panjang"
```

### 8.5 `ORA-50029: OracleConnection.ConnectionString is invalid`

**Penyebab:** `ConnectionStrings:Oracle` kosong.

**Solusi:** isi `appsettings.Local.json` API:

```json
{
  "ConnectionStrings": {
    "Oracle": "User Id=EASYCREDIT;Password=...;Data Source=localhost:1521/FREEPDB1"
  }
}
```

### 8.5a `ORA-12514: service EASYCREDIT is not registered with the listener`

**Penyebab:** connection string memakai `SERVICE_NAME=EASYCREDIT`.
`EASYCREDIT` adalah **schema/user**, bukan nama service Oracle.

**Solusi:** ganti service name menjadi PDB yang benar:

```text
Data Source=localhost:1521/FREEPDB1
```

atau bentuk TNS:

```text
Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=FREEPDB1)))
```

Untuk image Oracle XE, service name biasanya `XEPDB1`.

Cek service yang terdaftar dari dalam container:

```bash
docker exec -it easycredit-oracle bash -lc "lsnrctl status"
```

Atau tanpa sqlplus, dari host dengan Python `oracledb`:

```python
import oracledb
for svc in ["FREEPDB1", "XEPDB1", "FREE"]:
    try:
        oracledb.connect(user="...", password="...", dsn=f"localhost:1521/{svc}")
        print(svc, "OK")
    except Exception as e:
        print(svc, str(e).splitlines()[0])
```

### 8.5b `ORA-01017: invalid credential or not authorized; logon denied`

**Penyebab:** service name sudah benar, tetapi `User Id`/`Password` tidak cocok
dengan user Oracle di PDB tersebut.

**Solusi:**

1. Cek environment container Oracle:

   ```bash
   docker inspect easycredit-oracle \
     --format '{{range .Config.Env}}{{println .}}{{end}}' \
     | grep -E 'ORACLE_PASSWORD|APP_USER'
   ```

2. Atau reset password dari dalam container:

   ```bash
   docker exec -it easycredit-oracle bash
   sqlplus / as sysdba
   ```

   ```sql
   ALTER USER SYSTEM IDENTIFIED BY "PasswordBaru";
   -- atau user aplikasi:
   ALTER USER EASYCREDIT IDENTIFIED BY "PasswordBaru";
   ```

3. Update `ConnectionStrings:Oracle` di API, lalu restart API.

> Login API tetap `200` walau Oracle mati karena autentikasi memakai
> `InMemoryUserRepository`, bukan tabel Oracle. Error Oracle baru muncul saat
> endpoint customer/pengajuan dipanggil.

### 8.6 API 500 saat dashboard, tetapi frontend tetap tampil

Dashboard frontend menangani response API yang gagal:

```csharp
ApiAvailable = applications is not null && applications.IsSuccess
```

Jika Oracle belum siap, dashboard menampilkan angka 0 + alert error. Ini perilaku yang benar. Perbaiki koneksi Oracle agar data muncul.

### 8.7 CSS/JS 404 di frontend

Cek:

1. File ada di `EasyCreditFrontend/wwwroot`.
2. `app.UseStaticFiles()` ada di `EasyCreditFrontend/Program.cs`.
3. Path diawali `~/`.
4. Nama file sama persis (case-sensitive di Linux).

### 8.8 Build error karena salah folder/root

**Penyebab:** perintah build dijalankan dari folder project, bukan dari root
solution, atau path project di `EasyCredit.slnx` masih menunjuk struktur lama.

**Solusi:** jalankan dari folder root `EasyCredit/`:

```bash
dotnet build EasyCredit.slnx
```

Pastikan isi `EasyCredit.slnx`:

```xml
<Solution>
  <Project Path="EasyCreditApi/EasyCreditApi.csproj" />
  <Project Path="EasyCreditFrontend/EasyCreditFrontend.csproj" />
</Solution>
```

Karena frontend sekarang **sibling** dengan API, tidak perlu
`Compile Remove="EasyCreditFrontend/**"` di `EasyCreditApi.csproj`.

### 8.9 `400 Bad Request` saat AJAX POST

**Penyebab:** token anti-forgery tidak dikirim atau kedaluwarsa.

**Solusi:**

- Pastikan layout memuat `@Html.AntiForgeryToken()`.
- Pastikan AJAX mengirim `__RequestVerificationToken`.
- Refresh halaman setelah restart aplikasi.

### 8.10 Port bentrok

```text
Failed to bind to address http://localhost:5052: address already in use
```

Cari proses lama:

```bash
lsof -i :5052
```

Hentikan proses atau jalankan app di port lain dengan mengubah `launchSettings.json`/`--urls`.

### 8.11 `Application is not in SUBMITTED/PENDING status`

**Penyebab:** pengajuan yang di-approve sudah berstatus `APPROVED`/`REJECTED`,
atau data lama masih berstatus `PENDING` tetapi SP versi lama hanya menerima
`SUBMITTED`. Ini bukan error koneksi; stored procedure sengaja menolak approve
kedua agar status tidak berubah dua kali.

Logika SP yang dipakai project ini:

```sql
UPDATE EASYCREDIT.CREDIT_APPLICATIONS
   SET STATUS = 'APPROVED', ...
 WHERE ID = p_app_id
   AND STATUS IN ('SUBMITTED', 'PENDING');

IF SQL%ROWCOUNT = 0 THEN
    out_stat := 'INVALID_STATUS';
    out_mess := 'Application is not in SUBMITTED/PENDING status';
END IF;
```

**Solusi:**

- Pastikan `scripts/oracle/day6_stored_procedures.sql` versi terbaru sudah
  dijalankan ulang di Oracle. Script memakai `CREATE OR REPLACE`, jadi aman
  dijalankan berulang.
- Refresh halaman pengajuan; status dan tombol akan mengikuti data terbaru.
- Frontend sudah otomatis reload setelah menerima pesan ini agar tombol
  `Approve` yang stale berubah menjadi `Selesai`.
- Untuk mengulang demo approval dari status `APPROVED`, reset ke `SUBMITTED`:

  ```bash
  curl -X POST http://localhost:5052/_reset-app/8
  ```

  atau lewat SQL:

  ```sql
  UPDATE EASYCREDIT.CREDIT_APPLICATIONS
     SET STATUS = 'SUBMITTED',
         APPROVED_BY = NULL,
         APPROVED_AT = NULL
   WHERE ID = 8;
  COMMIT;
  ```

  Endpoint `/_reset-app/{id}` hanya aktif di environment **Development** dan
  sengaja disediakan untuk latihan Day 6.

> Status `DRAFT`, `APPROVED`, dan `REJECTED` tetap tidak bisa di-approve.
> Hanya `SUBMITTED` dan `PENDING` yang dianggap menunggu keputusan.

---

## 9. Latihan

1. **Customer detail**
   Tambahkan action `Detail(int id)` + view `Customers/Detail.cshtml`.
   Tampilkan data customer dan riwayat pengajuan miliknya.

2. **AJAX edit inline**
   Tambahkan tombol "Edit cepat" yang membuka modal, lalu kirim `POST /Customers/UpdateQuick` via AJAX.

3. **Filter pengajuan via AJAX**
   Ubah filter status/keyword menjadi AJAX dengan PartialView `_ApplicationList`.

4. **Role-based UI**
   Sembunyikan tombol Approve jika `ViewBag.IsAdmin == false`.
   API tetap harus menolak request non-admin.

5. **Validasi kondisional**
   Tambahkan aturan: jika `Email` diisi, `Phone` wajib diisi.
   Implementasikan dengan `IValidatableObject` di ViewModel.

6. **Refresh token / logout otomatis**
   Jika API mengembalikan 401, hapus session dan redirect ke login.

---

## 10. Checklist akhir

### Konsep MVC & arsitektur

- [ ] Bisa menjelaskan peran Model, View, Controller.
- [ ] Bisa menjelaskan alur browser → frontend MVC → API → database.
- [ ] Bisa membedakan controller API dan controller MVC.
- [ ] Bisa menjelaskan fungsi `ApiClient`, session, dan JWT.
- [ ] Bisa menjelaskan conventional route vs attribute route.

### Model & validasi

- [ ] Bisa membedakan Entity, DTO, dan ViewModel.
- [ ] Form MVC memakai ViewModel, bukan entity API.
- [ ] Data Annotation terpasang di ViewModel.
- [ ] `ModelState.IsValid` dicek di setiap POST.
- [ ] `asp-validation-for` dan `asp-validation-summary` terpasang.
- [ ] `[ValidateAntiForgeryToken]` terpasang di POST.
- [ ] Validasi bisnis tetap ada di API service.

### Template & Razor

- [ ] `EasyCreditFrontend/wwwroot` berisi CSS/JS.
- [ ] `UseStaticFiles()` aktif di frontend.
- [ ] Layout memuat Bootstrap, jQuery, dan `site.js`.
- [ ] Asset memakai `asp-append-version="true"`.
- [ ] Menu aktif mengikuti controller.
- [ ] TempData sukses/error tampil di layout.
- [ ] Partial view dipakai untuk tabel/form.
- [ ] Section `Scripts` dirender setelah jQuery.
- [ ] Login page memakai layout auth (`is-auth`).

### jQuery AJAX

- [ ] Search memakai AJAX + PartialView.
- [ ] Pagination memakai AJAX + event delegation.
- [ ] Delete memakai AJAX + JSON.
- [ ] Approve memakai AJAX + JSON + stored procedure.
- [ ] Token anti-forgery dikirim.
- [ ] Tombol di-disable saat request.
- [ ] Error ditampilkan sebagai toast/pesan.
- [ ] Tidak ada secret di JavaScript.

### Verifikasi

- [ ] `dotnet build EasyCredit.slnx` sukses.
- [ ] API berjalan di `5052`.
- [ ] Frontend berjalan di `5200`.
- [ ] Login `admin@mail.com / Admin123!` berhasil.
- [ ] Halaman tanpa login redirect ke `/Auth/Login`.
- [ ] Dashboard menampilkan data setelah Oracle aktif.
- [ ] `/Customers/Create` merender form.
- [ ] Validasi client-side muncul.
- [ ] Validasi server-side tetap muncul saat JavaScript dimatikan.
- [ ] `/Customers` menampilkan data dari Oracle.
- [ ] `/CreditApplications` menampilkan data + nama customer.
- [ ] Approve mengubah status dan approver.
- [ ] Static CSS/JS `200 OK`.
- [ ] Swagger hanya menampilkan endpoint API.

---

## Bridge ke Day 8

Day 7 sudah mencakup:

- dua project terpisah (API + MVC frontend),
- Razor layout + partial + static assets,
- ViewModel + Data Annotation,
- jQuery AJAX ke controller MVC,
- login + session + JWT + `ApiClient`.

Arah natural Day 8:

1. **Detail pengajuan + skema cicilan** — halaman detail dan tabel cicilan.
2. **Create pengajuan** — form + preview cicilan AJAX.
3. **Role-based authorization** — tombol/aksi admin vs staff.
4. **Refresh token / handling 401** — logout otomatis saat token kedaluwarsa.
5. **Pagination & filter server-side di API** — agar frontend tidak memfilter di memori.
6. **Integrasi external credit score API** — menampilkan skor kredit di halaman detail.

---

## Lampiran A — Peta file frontend

```text
EasyCreditFrontend/
├── EasyCreditFrontend.csproj
├── Program.cs
├── appsettings.json
├── Properties/launchSettings.json
├── Services/
│   ├── ApiClient.cs
│   └── SessionKeys.cs
├── Models/
│   ├── ApiResponse.cs
│   ├── Dtos/
│   │   ├── ApplicationListItemDto.cs
│   │   ├── CustomerDto.cs
│   │   └── LoginDataDto.cs
│   └── ViewModels/
│       ├── ApproveApplicationViewModel.cs
│       ├── ApplicationIndexViewModel.cs
│       ├── ApplicationRowViewModel.cs
│       ├── CustomerFormViewModel.cs
│       ├── CustomerIndexViewModel.cs
│       ├── DashboardViewModel.cs
│       ├── ErrorViewModel.cs
│       └── LoginViewModel.cs
├── Controllers/
│   ├── AuthController.cs
│   ├── BaseController.cs
│   ├── CreditApplicationsController.cs
│   ├── CustomersController.cs
│   └── HomeController.cs
├── Views/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Auth/Login.cshtml
│   ├── Home/Index.cshtml
│   ├── Customers/
│   │   ├── Index.cshtml
│   │   ├── _CustomerList.cshtml
│   │   ├── _CustomerForm.cshtml
│   │   ├── Create.cshtml
│   │   └── Edit.cshtml
│   ├── CreditApplications/Index.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       ├── _ValidationScriptsPartial.cshtml
│       └── Error.cshtml
└── wwwroot/
    ├── css/site.css
    ├── js/site.js
    └── lib/...
```

---

## Lampiran B — Ringkasan route

### Frontend MVC

| Method | URL | Handler | Response |
|---|---|---|---|
| GET | `/Auth/Login` | `Auth.Login` | View login |
| POST | `/Auth/Login` | `Auth.Login` | Redirect/View |
| POST | `/Auth/Logout` | `Auth.Logout` | Redirect |
| GET | `/` | `Home.Index` | View dashboard |
| GET | `/Customers` | `Customers.Index` | View daftar |
| GET | `/Customers/Rows` | `Customers.Rows` | PartialView |
| GET | `/Customers/Create` | `Customers.Create` | View form |
| POST | `/Customers/Create` | `Customers.Create` | Redirect/View |
| GET | `/Customers/Edit/{id}` | `Customers.Edit` | View form |
| POST | `/Customers/Edit/{id}` | `Customers.Edit` | Redirect/View |
| POST | `/Customers/Delete` | `Customers.Delete` | JSON |
| GET | `/CreditApplications` | `CreditApplications.Index` | View daftar |
| POST | `/CreditApplications/Approve` | `CreditApplications.Approve` | JSON |
| GET | `/Home/Error` | `Home.Error` | View error |

### API

| Method | URL | Auth |
|---|---|---|
| POST | `/api/auth/login` | API key |
| GET | `/api/customers` | API key + JWT |
| GET | `/api/customers/{id}` | API key + JWT |
| POST | `/api/customers` | API key + JWT |
| PUT | `/api/customers/{id}` | API key + JWT |
| DELETE | `/api/customers/{id}` | API key + JWT |
| GET | `/api/credit-applications` | API key + JWT |
| POST | `/api/credit-applications/{id}/approve` | API key + JWT |
| GET | `/api/customers/sp` | API key + JWT |
| GET | `/api/customers/sp/{id}` | API key + JWT |
# ?? Bat Framework

> ?ò ›—?„Ê—ò ‘Œ’? Ê ﬁœ— „‰œ »—«?  Ê”⁄Â ”—?⁄ «Å·?ò?‘‰ùÂ«? .NET

[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-All%20Rights%20Reserved-red)](LICENSE)
[![Author](https://img.shields.io/badge/Author-Mehran%20Norouzi-blue)](https://github.com/mehrannoruzi)

---

## ?? ›Â—”  „ÿ«·»

- [œ—»«—Â Å—ÊéÂ](#-œ—»«—Â-Å—ÊéÂ)
- [„⁄„«—?](#-„⁄„«—?)
- [Åò?ÃùÂ«](#-Åò?ÃÂ«)
- [‰’» Ê —«Âù«‰œ«“?](#-‰’»-Ê-—«Â«‰œ«“?)
- [„” ‰œ« ](#-„” ‰œ« )
- [‰„Ê‰Â «” ›«œÂ](#-‰„Ê‰Â-«” ›«œÂ)
- [Ê?éê?ùÂ«? ò·?œ?](#-Ê?éê?Â«?-ò·?œ?)
- [Å‘ ?»«‰?](#-Å‘ ?»«‰?)

---

## ?? œ—»«—Â Å—ÊéÂ

**Bat Framework** ?ò „Ã„Ê⁄Â ò«„· «“ ò «»Œ«‰ÂùÂ«? .NET «”  òÂ ÿ? ç‰œ?‰ ”«·  Ê”⁄Â Ê »Â»Êœ ?«› Â Ê »—«?  ”—?⁄ Ê  ”Â?·  Ê”⁄Â «Å·?ò?‘‰ùÂ«? enterprise-grade ÿ—«Õ? ‘œÂ «” . «?‰ ›—?„Ê—ò ‘«„· «»“«—Â«? ò«—»—œ?° «·êÊÂ«? ÿ—«Õ? «” «‰œ«—œ Ê ﬁ«»·? ùÂ«? „Ê—œ ‰?«“ »—«? Å—ÊéÂùÂ«? Ê«ﬁ⁄? «” .

### ç—« Bat Frameworkø

? ** Ê”⁄Â ”—?⁄**: ò«Â‘ “„«‰  Ê”⁄Â »« «»“«—Â«? «“ Å?‘ ¬„«œÂ  
? **Best Practices**: Å?«œÂù”«“? «·êÊÂ«? «” «‰œ«—œ „«‰‰œ Repository° UnitOfWork  
? **„” ‰œ”«“? ò«„·**: Â— Åò?Ã œ«—«? „” ‰œ«  Ê „À«·ùÂ«? ⁄„·?  
? **»Âù—Ê“**: Å‘ ?»«‰? «“ .NET 10 Ê ¬Œ—?‰ «” «‰œ«—œÂ«  
? ** Ê·?œ ‘Œ’?**: »«  Ã—»Â Ê«ﬁ⁄? Ê ‰?«“Â«? ⁄„·?  Ê”⁄Â ?«› Â  

---

## ??? „⁄„«—?

›—?„Ê—ò Bat œ— çÂ«— ·«?Â «’·? ”«“„«‰œÂ? ‘œÂ «” :

```
Bat/
??? src/
?   ??? Core/                    # Â” Â «’·? ›—?„Ê—ò
?   ?   ??? Bat.Core/           # «»“«—Â«? Å«?Â Ê Extension Methods
?   ?   ??? Bat.Tools/          # «»“«—Â«? ò„ò? «÷«›?
?   ?
?   ??? AspNetCore/             # ·«?Â Ê» Ê API
?   ?   ??? Bat.AspNetCore/    # ò«„ÅÊ‰‰ ùÂ«? ASP.NET Core
?   ?   ??? Bat.Http/          # «»“«—Â«? HTTP
?   ?   ??? Bat.Di/            # Dependency Injection
?   ?
?   ??? DataAccess/            # ·«?Â œ” —”? »Â œ«œÂ
?   ?   ??? Bat.EntityFrameworkCore/       # Repository Pattern »« EF Core
?   ?   ??? Bat.EntityFrameworkCore.Tools/ # «»“«—Â«? Å?‘—› Â EF Core
?   ?   ??? Bat.Dapper/                    # Dapper Integration
?   ?
?   ??? Cache/                 # „œ?—?  ò‘
?   ?   ??? Bat.Cache/        # InterfaceùÂ«? ò‘
?   ?   ??? Bat.Cache.Redis/  # Å?«œÂù”«“? Redis Ê Hybrid Cache
?   ?
?   ??? Queue/                 # „œ?—?  ’›
?       ??? Bat.Queue/        # Queue Management
```

---

## ?? Åò?ÃùÂ«

### ?? Core Layer

#### **Bat.Core** ![NuGet](https://img.shields.io/badge/v10.0.0-green)
Â” Â «’·? ›—?„Ê—ò ‘«„·:
- **Extensions**: String° DateTime° Enum° Number° Object Ê...
- **Security**: AES° RSA° Hash Generation° Encryption
- **Validation**: Attributes ”›«—‘? (Email° Mobile° NationalCode° IP° URL Ê...)
- **DateTime**: PersianDateTime°  »œ?·«   «—?Œ
- **Serialization**: JSON Converters
- **File Operations**: „œ?—?  ›«?·
- **Logging**: FileLogger
- **Pagination**: Å?«œÂù”«“? Paging
- **Math**: Randomizer Ê  Ê«»⁄ —?«÷?

#### **Bat.Tools**
«»“«—Â«? ò„ò? «÷«›?:
- Excel Operations
- Public Extensions

---

### ?? AspNetCore Layer

#### **Bat.AspNetCore** ![NuGet](https://img.shields.io/badge/v10.0.0-green)
ò«„ÅÊ‰‰ ùÂ«? ASP.NET Core:
- **Authentication**: JWT Service Ê Å?ò—»‰œ?
- **Middleware**: 
  - `BatJwtParserMiddleware`
  - `BatExceptionHandlingMiddleware`
  - `BatEnableRequestBufferingMiddleware`
- **Filters**:
  - Exception Handling Filter
  - Model Validation Filter
  - Swagger Authorization Filters
- **TagHelpers**: CustomInput° CustomSelect° FileUploader Ê...
- **File Upload**: „œ?—?  ¬Å·Êœ ›«?·ùÂ«? òÊçò Ê »“—ê
- **Extensions**: Swagger° CORS° MVC Extensions

#### **Bat.Http**
«»“«—Â«? HTTP:
- Client Info Detection
- Request Details
- Device Logging
- HTTP Extensions

#### **Bat.Di**
Dependency Injection Utilities

---

### ?? DataAccess Layer

#### **Bat.EntityFrameworkCore** ![NuGet](https://img.shields.io/badge/v10.0.0-green)
Å?«œÂù”«“? ò«„· Repository Ê UnitOfWork Pattern:

**Features:**
- ? Generic Repository »« ﬁ«»·? ùÂ«? Å?‘—› Â
- ? UnitOfWork Pattern
- ? Automatic Tracking Changes
- ? Persian/English Character Normalization
- ? Validation Handling
- ? Exception Handling »« SaveChangeResult
- ? Soft Delete Support
- ? Audit Log Properties
- ? Pagination Extensions
- ? Dynamic OrderBy
- ? Stored Procedure Execution

#### **Bat.EntityFrameworkCore.Tools**
«»“«—Â«? Å?‘—› Â EF Core

#### **Bat.Dapper**
Dapper Integration & Extensions

---

### ?? Cache Layer

#### **Bat.Cache**
InterfaceùÂ«? Cache „” ﬁ· «“ Å?«œÂù”«“?

#### **Bat.Cache.Redis** ![NuGet](https://img.shields.io/badge/v10.0.0-green)
Å?«œÂù”«“? Cache »« Redis:
- ? Redis Cache Provider
- ? Hybrid Cache (Memory + Redis)
- ? Sentinel Support
- ? SSL Support
- ? Connection Retry Logic
- ? Distributed Cache

---

### ?? Queue Layer

#### **Bat.Queue**
„œ?—?  ’›ùÂ«? Å?«„

---

## ?? ‰’» Ê —«Âù«‰œ«“?

### ‰’» «“ ÿ—?ﬁ NuGet Package Manager:

```powershell
# Core Package
Install-Package Bat.Core -Version 10.0.0

# Entity Framework Core
Install-Package Bat.EntityFrameworkCore -Version 10.0.0

# ASP.NET Core
Install-Package Bat.AspNetCore -Version 10.0.0

# Redis Cache
Install-Package Bat.Cache.Redis -Version 10.0.0
```

### ‰’» «“ ÿ—?ﬁ .NET CLI:

```bash
dotnet add package Bat.Core --version 10.0.0
dotnet add package Bat.EntityFrameworkCore --version 10.0.0
dotnet add package Bat.AspNetCore --version 10.0.0
dotnet add package Bat.Cache.Redis --version 10.0.0
```

---

## ?? „” ‰œ« 

Â— Åò?Ã œ«—«? ›«?· `readme.md` „Ã“« »«  Ê÷?Õ«  ò«„· Ê „À«·ùÂ«? ò«—»—œ? «” :

- [?? Bat.Core Documentation](src/Core/Bat.Core/readme.md)
- [?? Bat.EntityFrameworkCore Documentation](src/DataAccess/Bat.EntityFrameworkCore/readme.md)
- [?? Bat.AspNetCore Documentation](src/AspNetCore/Bat.AspNetCore/readme.md)
- [?? Bat.Cache.Redis Documentation](src/Cache/Bat.Cache.Redis/readme.md)

---

## ?? ‰„Ê‰Â «” ›«œÂ

### ?? «” ›«œÂ «“ Bat.Core

```csharp
using Bat.Core;

public class UserService
{
    public void ProcessUser()
    {
        // Date & Time
        string persianDate = DateTime.Now.ToPersianDate();
        DateTime miladiDate = "1402/12/15".ToDateTime();
        
        // Validation
        bool isValid = "09123456789".IsMobile();
        bool isNationalCode = "0080799999".IsNationalCode();
        
        // Encryption
        string encrypted = AesEncryption.Encrypt("SecretData", "MyKey");
        string decrypted = AesEncryption.Decrypt(encrypted, "MyKey");
        
        // Logging
        FileLoger.Info("User processed successfully");
        
        // Randomizer
        string uniqueKey = Randomizer.GetUniqueKey(10);
        int randomCode = Randomizer.GetRandomInteger(6);
    }
}
```

### ?? «” ›«œÂ «“ Bat.EntityFrameworkCore

```csharp
// 1. Define DbContext
public class AppDbContext : BatDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
}

// 2. Define UnitOfWork
public class AppUnitOfWork : IBatUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    
    public AppUnitOfWork(AppDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }
    
    public IEFGenericRepo<User> UserRepo => 
        _serviceProvider.GetRequiredService<IEFGenericRepo<User>>();
    
    public IEFGenericRepo<Product> ProductRepo => 
        _serviceProvider.GetRequiredService<IEFGenericRepo<Product>>();
    
    public Task<SaveChangeResult> SaveAsync() => 
        _context.BatSaveChangesAsync();
}

// 3. Register Services
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IEFGenericRepo<User>, EFGenericRepo<User>>();
builder.Services.AddScoped<IEFGenericRepo<Product>, EFGenericRepo<Product>>();
builder.Services.AddScoped<AppUnitOfWork>();

// 4. Use in Service
public class UserService
{
    private readonly AppUnitOfWork _uow;
    
    public UserService(AppUnitOfWork uow) => _uow = uow;
    
    public async Task<List<User>> GetActiveUsers()
    {
        return await _uow.UserRepo
            .Where(x => x.IsActive)
            .Include(x => x.Orders)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();
    }
    
    public async Task<PagingListDetails<User>> GetUsersPaged(PagingParameter paging)
    {
        return await _uow.UserRepo
            .Where(x => x.IsActive)
            .OrderBy(x => x.FullName)
            .ToPagingListDetailsAsync(paging);
    }
    
    public async Task<SaveChangeResult> CreateUser(User user)
    {
        await _uow.UserRepo.AddAsync(user);
        return await _uow.SaveAsync();
    }
}
```

### ?? «” ›«œÂ «“ Bat.AspNetCore

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.AddBatJwtConfiguration(jwtSettings);
builder.Services.AddTransient<IJwtService, JwtService>();

// Swagger
var swaggerSettings = builder.Configuration.GetSection("SwaggerSettings").Get<SwaggerSetting>();
builder.Services.AddBatSwagger(swaggerSettings);

var app = builder.Build();

// Middleware
app.UseMiddleware<BatJwtParserMiddleware>();
app.UseMiddleware<BatEnableRequestBufferingMiddleware>();
app.UseMiddleware<BatExceptionHandlingMiddleware>();

app.UseBatJwtConfiguration();
app.Run();
```

### ?? «” ›«œÂ «“ Bat.Cache.Redis

```csharp
// appsettings.json
{
  "RedisSettings": {
    "Server1": "127.0.0.1",
    "Port1": 6379,
    "Password": "YourPassword",
    "ClientName": "MyApp",
    "DefaultDatabaseIndex": 0
  }
}

// Program.cs
builder.Services.Configure<RedisSettings>(
    builder.Configuration.GetSection("RedisSettings"));
builder.Services.AddSingleton<IRedisCacheProvider, RedisCacheProvider>();
builder.Services.AddSingleton<IHybridCacheProvider, HybridCacheProvider>();

// Service
public class ProductService
{
    private readonly IRedisCacheProvider _cache;
    
    public ProductService(IRedisCacheProvider cache) => _cache = cache;
    
    public List<Product> GetProducts()
    {
        const string cacheKey = "products_list";
        
        // Try get from cache
        var products = _cache.Get<List<Product>>(cacheKey);
        if (products != null) return products;
        
        // Load from database
        products = _dbContext.Products.ToList();
        
        // Store in cache
        _cache.Set(cacheKey, products, TimeSpan.FromHours(24));
        
        return products;
    }
}
```

---

## ? Ê?éê?ùÂ«? ò·?œ?

### ?? Bat.Core

| Ê?éê? |  Ê÷?Õ«  |
|-------|---------|
| ?? Persian DateTime |  »œ?·«  ò«„·  «—?Œ ‘„”? Ê „?·«œ? |
| ?? Security | AES, RSA, Hash Generation |
| ? Validation | 15+ Validation Attribute |
| ?? Pagination | Å?«œÂù”«“? ò«„· ’›ÕÂù»‰œ? |
| ?? Logging | File-based Logger |
| ?? Randomizer |  Ê·?œ òœ Ê ò·?œ ?ò « |

### ?? Bat.EntityFrameworkCore

| Ê?éê? |  Ê÷?Õ«  |
|-------|---------|
| ??? Repository Pattern | Å?«œÂù”«“? ò«„· Generic Repository |
| ?? UnitOfWork | „œ?—?   —«ò‰‘ùÂ« |
| ? Auto Tracking | À»  ŒÊœò«—  «—?Œ «?Ã«œ/Ê?—«?‘ |
| ?? Normalization | ‰—„«·ù”«“? ò«—«ò —Â«? ›«—”? |
| ??? Exception Handling | „œ?—?  ò«„· Œÿ«Â« |
| ?? Pagination | ’›ÕÂù»‰œ? »« ⁄„·ò—œ »«·« |

### ?? Bat.AspNetCore

| Ê?éê? |  Ê÷?Õ«  |
|-------|---------|
| ?? JWT | Å?«œÂù”«“? ò«„· Authentication |
| ??? Middleware | Exception Handling, Request Buffering |
| ?? Filters | Validation, Authorization |
| ?? File Upload | ¬Å·Êœ ›«?·ùÂ«? òÊçò Ê »“—ê |
| ?? Swagger | Å?ò—»‰œ? Swagger »« Authentication |

### ?? Bat.Cache.Redis

| Ê?éê? |  Ê÷?Õ«  |
|-------|---------|
| ? Redis Cache | ò‘  Ê“?⁄ù‘œÂ »« Redis |
| ?? Hybrid Cache |  —ò?» Memory + Redis |
| ?? SSL Support | «— »«ÿ «„‰ |
| ?? Retry Logic |  ·«‘ „Ãœœ ŒÊœò«— |
| ?? Sentinel | Å‘ ?»«‰? «“ Redis Sentinel |

---

## ???  ò‰Ê·Êé?ùÂ«

- **.NET 10.0**
- **Entity Framework Core**
- **ASP.NET Core**
- **Redis (StackExchange.Redis)**
- **Dapper**
- **System.Text.Json**
- **JWT (System.IdentityModel.Tokens.Jwt)**

---

## ?? Ê÷⁄?  Å—ÊéÂ

«?‰ ›—?„Ê—ò »ÂùÿÊ— ›⁄«·  Ê”⁄Â „?ù?«»œ Ê »Âù—Ê“—”«‰?ùÂ«? „‰Ÿ„ œ—?«›  „?ùò‰œ:

- ? **Stable**:  „«„ Åò?ÃùÂ«? Core
- ?? **Active Development**: »Âù—Ê“—”«‰? „” „—
- ?? **Versioning**: Semantic Versioning (10.0.0)
- ?? **.NET Support**: .NET 10

---

## ?? „‘«—ò 

«?‰ Å—ÊéÂ ?ò ›—?„Ê—ò ‘Œ’? «”  Ê œ— Õ«· Õ«÷— »—«? „‘«—ò  ⁄„Ê„? »«“ ‰?” ° «„« Å?‘‰Â«œ«  Ê »«“ŒÊ—œÂ« Â„?‘Â ŒÊ‘ù¬„œ Â” ‰œ.

---

## ?? Å‘ ?»«‰?

»—«? ”Ê«·« ° Å?‘‰Â«œ«  ?« ê“«—‘ „‘ò·« :

?? **Email**: mehrannoruzi@gmail.com  
?? **Tel**: +989301919109  
?? **GitHub**: [@mehrannoruzi](https://github.com/mehrannoruzi)  
?? **Repository**: [https://github.com/mehrannoruzi/Bat](https://github.com/mehrannoruzi/Bat)

---

## ?? „ÃÊ“

©  „«„? ÕﬁÊﬁ „Õ›ÊŸ «”  - Mehran Norouzi

---

## ?? ” «—ÂùœÂ?

«ê— «“ «?‰ ›—?„Ê—ò «” ›«œÂ „?ùò‰?œ Ê »—«? ‘„« „›?œ »Êœ° ·ÿ›« »« œ«œ‰ ” «—Â »Â «?‰ Å—ÊéÂ «“ ¬‰ Õ„«?  ò‰?œ! ?

---

<div align="center">

**”«Œ Â ‘œÂ »« ??  Ê”ÿ Mehran Norouzi**

</div>

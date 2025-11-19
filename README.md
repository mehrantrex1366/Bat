# 🦇 Bat Framework

<div dir="rtl">

> یک فریمورک شخصی و قدرتمند برای توسعه سریع اپلیکیشن‌های .NET

</div>

[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-All%20Rights%20Reserved-red)](LICENSE)
[![Author](https://img.shields.io/badge/Author-Mehran%20Norouzi-blue)](https://github.com/mehrannoruzi)

---

## 📋 فهرست مطالب

- [درباره پروژه](#-درباره-پروژه)
- [معماری](#-معماری)
- [پکیج‌ها](#-پکیجها)
- [نصب و راه‌اندازی](#-نصب-و-راهاندازی)
- [مستندات](#-مستندات)
- [نمونه استفاده](#-نمونه-استفاده)
- [ویژگی‌های کلیدی](#-ویژگیهای-کلیدی)
- [پشتیبانی](#-پشتیبانی)

---

<div dir="rtl">

## 🎯 درباره پروژه

**Bat Framework** یک مجموعه کامل از کتابخانه‌های .NET است که طی چندین سال توسعه و بهبود یافته و برای تسریع و تسهیل توسعه اپلیکیشن‌های enterprise-grade طراحی شده است. این فریمورک شامل ابزارهای کاربردی، الگوهای طراحی استاندارد و قابلیت‌های مورد نیاز برای پروژه‌های واقعی است.

### چرا Bat Framework؟

✅ **توسعه سریع**: کاهش زمان توسعه با ابزارهای از پیش آماده  
✅ **Best Practices**: پیاده‌سازی الگوهای استاندارد مانند Repository، UnitOfWork  
✅ **مستندسازی کامل**: هر پکیج دارای مستندات و مثال‌های عملی  
✅ **به‌روز**: پشتیبانی از .NET 10 و آخرین استانداردها  
✅ **تولید شخصی**: با تجربه واقعی و نیازهای عملی توسعه یافته  

---

## 🏗️ معماری

فریمورک Bat در شش لایه اصلی سازماندهی شده است:

</div>

```
Bat/
├── src/
│   ├── Core/                           # هسته اصلی فریمورک
│   │   ├── Bat.Core/                  # ابزارهای پایه و Extension Methods
│   │   └── Bat.Tools/                 # ابزارهای کمکی اضافی (Excel و...)
│   │
│   ├── AspNetCore/                    # لایه وب و API
│   │   ├── Bat.AspNetCore/           # کامپوننت‌های ASP.NET Core
│   │   ├── Bat.Http/                 # ابزارهای HTTP و Client Detection
│   │   └── Bat.Di/                   # Dependency Injection Utilities
│   │
│   ├── DataAccess/                   # لایه دسترسی به داده
│   │   ├── Bat.EntityFrameworkCore/       # Repository Pattern با EF Core
│   │   ├── Bat.EntityFrameworkCore.Tools/ # ابزارهای پیشرفته EF Core
│   │   └── Bat.Dapper/                    # Dapper Integration
│   │
│   ├── Cache/                        # مدیریت کش
│   │   ├── Bat.Cache/               # Interface‌های کش
│   │   └── Bat.Cache.Redis/         # پیاده‌سازی Redis و Hybrid Cache
│   │
│   ├── Queue/                        # مدیریت صف‌های پیام
│   │   └── Bat.Queue/               # MSMQ و RabbitMQ Integration
│   │
│   ├── Test/                         # ابزارهای تست
│   │   └── Bat.Test/                # Test Utilities و Mock Builders
│   │
│   └── Sql/                          # SQL Server CLR
│       └── Bat.SqlClrAssembly/      # SQL CLR Functions و Stored Procedures
```

<div dir="rtl">

---

## 📦 پکیج‌ها

### 🔷 Core Layer

#### **Bat.Core** ![NuGet](https://img.shields.io/badge/v10.0.0-green)

هسته اصلی فریمورک شامل:

- **Extensions**: String، DateTime، Enum، Number، Object و...
- **Security**: AES، RSA، Hash Generation، Encryption
- **Validation**: Attributes سفارشی (Email، Mobile، NationalCode، IP، URL و...)
- **DateTime**: PersianDateTime، تبدیلات تاریخ
- **Serialization**: JSON Converters
- **File Operations**: مدیریت فایل
- **Logging**: FileLogger
- **Pagination**: پیاده‌سازی Paging
- **Math**: Randomizer و توابع ریاضی

#### **Bat.Tools**

ابزارهای کمکی اضافی:

- Excel Operations
- Public Extensions

---

### 🔷 AspNetCore Layer

#### **Bat.AspNetCore** ![NuGet](https://img.shields.io/badge/v10.0.0-green)

کامپوننت‌های ASP.NET Core:

- **Authentication**: JWT Service و پیکربندی
- **Middleware**: 
  - `BatJwtParserMiddleware`
  - `BatExceptionHandlingMiddleware`
  - `BatEnableRequestBufferingMiddleware`
- **Filters**:
  - Exception Handling Filter
  - Model Validation Filter
  - Swagger Authorization Filters
- **TagHelpers**: CustomInput، CustomSelect، FileUploader و...
- **File Upload**: مدیریت آپلود فایل‌های کوچک و بزرگ
- **Extensions**: Swagger، CORS، MVC Extensions

#### **Bat.Http**

ابزارهای HTTP:

- Client Info Detection
- Request Details
- Device Logging
- HTTP Extensions

#### **Bat.Di**

Dependency Injection Utilities

---

### 🔷 DataAccess Layer

#### **Bat.EntityFrameworkCore** ![NuGet](https://img.shields.io/badge/v10.0.0-green)

پیاده‌سازی کامل Repository و UnitOfWork Pattern:

**Features:**

- ✅ Generic Repository با قابلیت‌های پیشرفته
- ✅ UnitOfWork Pattern
- ✅ Automatic Tracking Changes
- ✅ Persian/English Character Normalization
- ✅ Validation Handling
- ✅ Exception Handling با SaveChangeResult
- ✅ Soft Delete Support
- ✅ Audit Log Properties
- ✅ Pagination Extensions
- ✅ Dynamic OrderBy
- ✅ Stored Procedure Execution

#### **Bat.EntityFrameworkCore.Tools**

ابزارهای پیشرفته EF Core

#### **Bat.Dapper**

Dapper Integration & Extensions

---

### 🔷 Cache Layer

#### **Bat.Cache**

Interface‌های Cache مستقل از پیاده‌سازی

#### **Bat.Cache.Redis** ![NuGet](https://img.shields.io/badge/v10.0.0-green)

پیاده‌سازی Cache با Redis:

- ✅ Redis Cache Provider
- ✅ Hybrid Cache (Memory + Redis)
- ✅ Sentinel Support
- ✅ SSL Support
- ✅ Connection Retry Logic
- ✅ Distributed Cache

---

### 🔷 Queue Layer

#### **Bat.Queue** ![NuGet](https://img.shields.io/badge/v10.0.0-green)

مدیریت صف‌های پیام و Message Queue:

**MSMQ Integration:**

- ✅ Microsoft Message Queue Support
- ✅ Send/Receive Messages
- ✅ Queue Management

**RabbitMQ Integration:**

- ✅ RabbitMQ Producer
- ✅ RabbitMQ Consumer
- ✅ Exchange Types (Direct، Fanout، Topic، Headers)
- ✅ Queue Configuration
- ✅ Connection Management
- ✅ Publish/Subscribe Pattern

---

### 🔷 Test Layer

#### **Bat.Test**

ابزارهای جامع برای تست یونیت و Integration:

**Mock Builders:**

- ✅ `MockBuilder`: ساخت Mock با Moq
- ✅ `NSubstituteBuilder`: ساخت Mock با NSubstitute
- ✅ `MockRepoBuilder`: Mock کردن Repository
- ✅ `MockUowBuilder`: Mock کردن UnitOfWork
- ✅ `MockDbContextBuilder`: Mock کردن DbContext

**Data Faker:**

- ✅ `BogusBuilder<T>`: ساخت داده‌های تصادفی با AutoBogus
- ✅ پشتیبانی از Nested Objects
- ✅ قابلیت Set کردن مقادیر خاص
- ✅ ساخت لیست‌های تصادفی

**Service Mocking:**

- ✅ `EasyServiceMocker`: Mock سریع سرویس‌ها
- ✅ `ServiceBuilder`: ساخت سرویس با Dependency‌های Mock شده

**Test Tools:**

- ✅ Extension Methods برای تست
- ✅ Static Values Builder
- ✅ Integration با MockQueryable

---

### 🔷 SQL Layer

#### **Bat.SqlClrAssembly**

SQL Server CLR Integration:

- ✅ Custom SQL CLR Functions
- ✅ SQL CLR Stored Procedures
- ✅ توابع سفارشی برای SQL Server
- ✅ Integration با .NET 10

---

## 🚀 نصب و راه‌اندازی

### نصب از طریق NuGet Package Manager:

</div>

```powershell
# Core Package
Install-Package Bat.Core -Version 10.0.0

# Entity Framework Core
Install-Package Bat.EntityFrameworkCore -Version 10.0.0

# ASP.NET Core
Install-Package Bat.AspNetCore -Version 10.0.0

# Redis Cache
Install-Package Bat.Cache.Redis -Version 10.0.0

# Queue Management
Install-Package Bat.Queue -Version 10.0.0
```

<div dir="rtl">

### نصب از طریق .NET CLI:

</div>

```bash
dotnet add package Bat.Core --version 10.0.0
dotnet add package Bat.EntityFrameworkCore --version 10.0.0
dotnet add package Bat.AspNetCore --version 10.0.0
dotnet add package Bat.Cache.Redis --version 10.0.0
dotnet add package Bat.Queue --version 10.0.0
```

<div dir="rtl">

---

## 📚 مستندات

هر پکیج دارای فایل `readme.md` مجزا با توضیحات کامل و مثال‌های کاربردی است:

</div>

- [📖 Bat.Core Documentation](src/Core/Bat.Core/readme.md)
- [📖 Bat.EntityFrameworkCore Documentation](src/DataAccess/Bat.EntityFrameworkCore/readme.md)
- [📖 Bat.AspNetCore Documentation](src/AspNetCore/Bat.AspNetCore/readme.md)
- [📖 Bat.Cache.Redis Documentation](src/Cache/Bat.Cache.Redis/readme.md)

<div dir="rtl">

---

## 💡 نمونه استفاده

### 🔹 استفاده از Bat.Core

</div>

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

<div dir="rtl">

### 🔹 استفاده از Bat.EntityFrameworkCore

</div>

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
        _serviceProvider.GetRequiredService<IEFGenericRepo<User>>>();
    
    public IEFGenericRepo<Product> ProductRepo => 
        _serviceProvider.GetRequiredService<IEFGenericRepo<Product>>>();
    
    public Task<SaveChangeResult> SaveAsync() => 
        _context.BatSaveChangesAsync();
}

// 3. Register Services
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IEFGenericRepo<User>, EFGenericRepo<User>>>();
builder.Services.AddScoped<IEFGenericRepo<Product>, EFGenericRepo<Product>>>();
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

<div dir="rtl">

### 🔹 استفاده از Bat.AspNetCore

</div>

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

<div dir="rtl">

### 🔹 استفاده از Bat.Cache.Redis

</div>

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

<div dir="rtl">

### 🔹 استفاده از Bat.Queue (RabbitMQ)

</div>

```csharp
// appsettings.json
{
  "RabbitConfiguration": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}

// Program.cs
builder.Services.Configure<RabbitConfiguration>(
    builder.Configuration.GetSection("RabbitConfiguration"));
builder.Services.AddSingleton<IRabbitProducer, RabbitProducer>();
builder.Services.AddSingleton<IRabbitConsumer, RabbitConsumer>();

// Producer
public class OrderService
{
    private readonly IRabbitProducer _producer;
    
    public OrderService(IRabbitProducer producer) => _producer = producer;
    
    public void CreateOrder(Order order)
    {
        // Send message to queue
        _producer.Publish("orders_queue", order.SerializeToJson());
    }
}

// Consumer
public class OrderProcessor
{
    private readonly IRabbitConsumer _consumer;
    
    public OrderProcessor(IRabbitConsumer consumer) => _consumer = consumer;
    
    public void StartProcessing()
    {
        _consumer.Subscribe("orders_queue", message =>
        {
            var order = message.DeSerializeJson<Order>();
            // Process order...
        });
    }
}
```

<div dir="rtl">

### 🔹 استفاده از Bat.Test (BogusBuilder)

</div>

```csharp
using Bat.Test;

public class UserServiceTests
{
    [Fact]
    public void CreateUser_ShouldReturnSuccess()
    {
        // Arrange
        var fakeUser = new BogusBuilder<User>()
            .WithNaturalInt()
            .Set(x => x.Email, "test@example.com")
            .Set(x => x.Age, f => f.Random.Int(18, 65))
            .SetString(x => x.FirstName, 5, 10)
            .Generate();
        
        // Act & Assert
        Assert.NotNull(fakeUser);
        Assert.Equal("test@example.com", fakeUser.Email);
    }
    
    [Fact]
    public void GetUsers_WithMockedRepository()
    {
        // Arrange
        var fakeUsers = new BogusBuilder<User>()
            .WithNaturalInt()
            .Generate(10);
        
        var mockRepo = new MockBuilder()
            .MockRepo<User>()
            .SetupGetAsync(fakeUsers)
            .Build();
        
        var service = new UserService(mockRepo.Object);
        
        // Act
        var result = await service.GetAllUsers();
        
        // Assert
        Assert.Equal(10, result.Count);
    }
}
```

<div dir="rtl">

---

## ⚡ ویژگی‌های کلیدی

### 🎨 Bat.Core

</div>

| ویژگی | توضیحات |
|-------|---------|
| 📅 Persian DateTime | تبدیلات کامل تاریخ شمسی و میلادی |
| 🔐 Security | AES, RSA, Hash Generation |
| ✅ Validation | 15+ Validation Attribute |
| 📄 Pagination | پیاده‌سازی کامل صفحه‌بندی |
| 📝 Logging | File-based Logger |
| 🎲 Randomizer | تولید کد و کلید یکتا |

<div dir="rtl">

### 💾 Bat.EntityFrameworkCore

</div>

| ویژگی | توضیحات |
|-------|---------|
| 🗂️ Repository Pattern | پیاده‌سازی کامل Generic Repository |
| 🔄 UnitOfWork | مدیریت تراکنش‌ها |
| ✨ Auto Tracking | ثبت خودکار تاریخ ایجاد/ویرایش |
| 🌍 Normalization | نرمال‌سازی کاراکترهای فارسی |
| 🛡️ Exception Handling | مدیریت کامل خطاها |
| 📑 Pagination | صفحه‌بندی با عملکرد بالا |

<div dir="rtl">

### 🌐 Bat.AspNetCore

</div>

| ویژگی | توضیحات |
|-------|---------|
| 🔑 JWT | پیاده‌سازی کامل Authentication |
| 🛡️ Middleware | Exception Handling, Request Buffering |
| 📋 Filters | Validation, Authorization |
| 📤 File Upload | آپلود فایل‌های کوچک و بزرگ |
| 📖 Swagger | پیکربندی Swagger با Authentication |

<div dir="rtl">

### 💨 Bat.Cache.Redis

</div>

| ویژگی | توضیحات |
|-------|---------|
| ⚡ Redis Cache | کش توزیع‌شده با Redis |
| 🔀 Hybrid Cache | ترکیب Memory + Redis |
| 🔐 SSL Support | ارتباط امن |
| 🔄 Retry Logic | تلاش مجدد خودکار |
| 🎯 Sentinel | پشتیبانی از Redis Sentinel |

<div dir="rtl">

### 📨 Bat.Queue

</div>

| ویژگی | توضیحات |
|-------|---------|
| 📮 MSMQ | Microsoft Message Queue Integration |
| 🐰 RabbitMQ | RabbitMQ Producer/Consumer |
| 🔀 Exchange Types | Direct, Fanout, Topic, Headers |
| 📡 Pub/Sub | Publish/Subscribe Pattern |
| ⚙️ Configuration | Queue و Connection Management |

<div dir="rtl">

### 🧪 Bat.Test

</div>

| ویژگی | توضیحات |
|-------|---------|
| 🎭 Mock Builders | Moq و NSubstitute Support |
| 🎲 Data Faker | BogusBuilder با AutoBogus |
| 🗂️ Repository Mock | Mock کردن Repository و UnitOfWork |
| 🔧 Service Mocker | Mock سریع Dependencies |
| 📊 Test Extensions | Extension Methods برای تست |

<div dir="rtl">

---

## 🛠️ تکنولوژی‌ها

</div>

- **.NET 10.0**
- **Entity Framework Core**
- **ASP.NET Core**
- **Redis (StackExchange.Redis)**
- **RabbitMQ (RabbitMQ.Client)**
- **MSMQ (Experimental.System.Messaging)**
- **Dapper**
- **Moq & NSubstitute**
- **AutoBogus & AutoFixture**
- **System.Text.Json**
- **JWT (System.IdentityModel.Tokens.Jwt)**

<div dir="rtl">

---

## 📊 وضعیت پروژه

این فریمورک به‌طور فعال توسعه می‌یابد و به‌روزرسانی‌های منظم دریافت می‌کند:

</div>

- ✅ **Stable**: تمام پکیج‌های Core
- 🔄 **Active Development**: به‌روزرسانی مستمر
- 📦 **Versioning**: Semantic Versioning (10.0.0)
- 🎯 **.NET Support**: .NET 10

<div dir="rtl">

---

## 🤝 مشارکت

این پروژه یک فریمورک شخصی است و در حال حاضر برای مشارکت عمومی باز نیست، اما پیشنهادات و بازخوردها همیشه خوش‌آمد هستند.

---

## 📞 پشتیبانی

برای سوالات، پیشنهادات یا گزارش مشکلات:

</div>

📧 **Email**: mehrannoruzi@gmail.com  
📱 **Tel**: +989301919109  
🐙 **GitHub**: [@mehrannoruzi](https://github.com/mehrannoruzi)  
🔗 **Repository**: [https://github.com/mehrannoruzi/Bat](https://github.com/mehrannoruzi/Bat)

<div dir="rtl">

---

## 📄 مجوز

© تمامی حقوق محفوظ است - Mehran Norouzi
---

## 🌟 ستاره‌دهی

اگر از این فریمورک استفاده می‌کنید و برای شما مفید بود، لطفاً با دادن ستاره به این پروژه از آن حمایت کنید! ⭐

</div>

---

<div align="center">

**ساخته شده با ❤️ توسط Mehran Norouzi**

</div>

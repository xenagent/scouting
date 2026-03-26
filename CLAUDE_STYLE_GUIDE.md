# Project Style Guide

Bu dosya, yeni bir projeye başlarken Claude'a verilecek kodlama stil rehberidir.
Projeye özel içerik içermez; yalnızca mimari kararları, naming convention'ları ve kod yazım kalıplarını açıklar.

---

## Tech Stack

- **.NET 9**, C# 13
- **PostgreSQL** + EF Core 9
- **FluentValidation** (istek validasyonu)
- **Custom Mediator** (CQRS altyapısı — MediatR değil, özel implementasyon)
- Her feature kendi içinde kapalı (Vertical Slice Architecture)

---

## Genel Proje Dizin Yapısı

```
/services/{service-name}/
├── /Controllers/               # Slim controller'lar, sadece mediator çağrısı
├── /Resources/                 # Feature dosyaları (Commands + Queries)
│   ├── /FeatureGroup/
│   │   ├── /Commands/
│   │   │   └── CreateFoo.cs   # Static class: Request + Response + Validator + Handler
│   │   └── /Queries/
│   │       └── GetFoo.cs
├── /Domains/                   # Entity sınıfları (EF Core models)
│   └── /FooEntity/
│       ├── FooEntity.cs
│       └── FooEntityConfiguration.cs
├── /Services/                  # Business logic servisleri (interface + impl)
└── /Infrastructure/            # DI kayıtları, mediator config, db context

/commons/{common-lib}/          # Paylaşılan kütüphane
├── /Mediators/                 # ICommand, IQuery, ICommandHandler, IQueryHandler, IMediator
├── /Results/                   # FeatureObjectResultModel, FeatureResultModel, MessageItem
├── /Domains/                   # BaseModel, BaseUserTrackModel
└── /DbSettings/                # BaseConfiguration<T>
```

---

## CQRS — Temel Arayüzler

### Command

```csharp
// Sonuç döndürmeyen command
public abstract class Command : ICommand
{
    public Guid TraceId { get; protected set; } = Guid.NewGuid();
    public bool BeginTransactionOnCommand { get; set; } = true;
    public Func<CancellationToken, Task>? CallBroadcastFunction { get; set; }
}

// Sonuç döndüren command
public abstract class Command<TResult> : Command, ICommand<TResult> { }
```

### Query

```csharp
public abstract class Query<TResult> : IQuery<TResult>
{
    public IQueryCacheOptions? QueryCacheOptions { get; set; }
    public bool IgnoreTranslate { get; set; } = false;
    public bool IgnoreCache { get; set; } = false;
    public Guid TraceId { get; protected set; } = Guid.NewGuid();
}
```

### Handler Arayüzleri

```csharp
public interface ICommandHandler<in TCommand, TResult> where TCommand : class, ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand cmd, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : class, IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}
```

### Mediator Kullanımı (Controller'dan)

```csharp
// Command göndermek için
await _mediator.SendAsync(command, ct);

// Query göndermek için
await _mediator.FetchAsync(query, ct);
```

---

## Feature Dosyası Yapısı (Vertical Slice)

Her feature **tek bir `.cs` dosyasında**, **tek bir `static class`** içinde tanımlanır.
Sınıf şu 4 nested class'ı içerir: `Request`, `Response`, `Validator`, `Handler`.

### Command Feature Örneği

```csharp
// Dosya: Resources/Foos/Commands/CreateFoo.cs
namespace MyApp.Resources.Foos.Commands;

public static class CreateFoo
{
    public class Request : Command<FeatureObjectResultModel<Response>>
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class Response
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .MaximumLength(128)
                .WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR);
        }
    }

    public class Handler : ICommandHandler<Request, FeatureObjectResultModel<Response>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FeatureObjectResultModel<Response>> HandleAsync(
            Request request,
            CancellationToken ct)
        {
            var result = Foo.Create(request.Name, request.Description);

            if (!result.IsSuccess)
                return FeatureObjectResultModel<Response>.Error(result.Messages);

            await _context.Set<Foo>().AddAsync(result.Data!, ct);
            await _context.SaveChangesAsync(ct);

            return FeatureObjectResultModel<Response>.Ok(new Response
            {
                Id = result.Data!.Id,
                Name = result.Data.Name!
            });
        }
    }
}
```

### Query Feature Örneği

```csharp
// Dosya: Resources/Foos/Queries/GetFoo.cs
namespace MyApp.Resources.Foos.Queries;

public static class GetFoo
{
    public class Request : Query<FeatureObjectResultModel<Response>>
    {
        public Guid Id { get; set; }
    }

    public class Response
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY);
        }
    }

    public class Handler : IQueryHandler<Request, FeatureObjectResultModel<Response>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FeatureObjectResultModel<Response>> HandleAsync(
            Request request,
            CancellationToken ct)
        {
            var foo = await _context.Set<Foo>()
                .AsNoTracking()
                .Where(f => f.Id == request.Id)
                .Select(f => new Response
                {
                    Id = f.Id,
                    Name = f.Name!,
                    Description = f.Description
                })
                .FirstOrDefaultAsync(ct);

            if (foo is null)
                return FeatureObjectResultModel<Response>.NotFound();

            return FeatureObjectResultModel<Response>.Ok(foo);
        }
    }
}
```

---

## Validasyon

### FluentValidation — Standart Kullanım

```csharp
public class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
            .EmailAddress()
            .WithErrorCode(ErrorCodes.COMMON_MESSAGE_INVALID_VALUE);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithErrorCode(ErrorCodes.COMMON_MESSAGE_INVALID_VALUE)
            .When(x => x.Amount.HasValue);
    }
}
```

- Her kural için **mutlaka** `.WithErrorCode(...)` kullanılır.
- Şartlı kurallar için `.When(...)` kullanılır.
- Human-readable mesaj yerine error code tercih edilir; mesajları client tarafı handle eder.

### Custom IValidationHandler (Karmaşık iş kuralları için)

FluentValidation yeterli olmadığında (DB sorgusu gerektiren kontroller vb.) `IValidationHandler<T>` implementasyonu eklenir:

```csharp
public class Validator : IValidationHandler<Request>
{
    private readonly AppDbContext _context;

    public Validator(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IResultModel> ValidateAsync(Request request, CancellationToken ct)
    {
        var exists = await _context.Set<Foo>()
            .AnyAsync(f => f.Name == request.Name, ct);

        if (exists)
            return FeatureResultModel.Error(new MessageItem
            {
                Code = ErrorCodes.COMMON_MESSAGE_ALREADY_EXISTS,
                Property = nameof(request.Name),
                Table = nameof(Foo)
            });

        return FeatureResultModel.Ok();
    }
}
```

Validasyon pipeline sırası:
1. FluentValidation çalışır
2. `IValidationHandler` çalışır
3. İkisi de başarılıysa handler çalışır

---

## Result Modelleri

### Kullanım Amaçları

| Tip | Ne zaman kullanılır |
|-----|---------------------|
| `FeatureResultModel` | Veri dönmeyen işlemler (Create, Delete, Update) |
| `FeatureObjectResultModel<T>` | Tek nesne dönen işlemler |
| `FeatureListResultModel<T>` | Liste dönen işlemler |
| `FeaturePagedResultModel<T>` | Sayfalı liste dönen işlemler |
| `ResultDomain<T>` | Domain entity factory methodları |

### FeatureObjectResultModel

```csharp
// Başarılı sonuç
FeatureObjectResultModel<Response>.Ok(response);

// Hata
FeatureObjectResultModel<Response>.Error(new MessageItem
{
    Code = ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND,
    Property = nameof(request.Id),
    Table = nameof(Foo)
});

// Çoklu hata
FeatureObjectResultModel<Response>.Error(result.Messages);

// Bulunamadı
FeatureObjectResultModel<Response>.NotFound();
```

### MessageItem

```csharp
new MessageItem
{
    Code = ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND,  // zorunlu
    Property = nameof(request.Id),                       // hangi field
    Table = nameof(Foo),                                 // hangi entity
    // Message = "...",                                  // opsiyonel, nadiren kullanılır
}
```

---

## Domain Entity Yapısı

```csharp
public class Foo : BaseUserTrackModel
{
    private Foo() { }  // EF Core için private constructor

    public string? Name { get; private set; }
    public string? Description { get; private set; }

    // Tüm property'ler private set

    // Factory method — domain validasyon burada
    public static ResultDomain<Foo> Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ResultDomain<Foo>.Error(new MessageItem
            {
                Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY,
                Property = nameof(name),
                Table = nameof(Foo)
            });

        if (name.Length > 128)
            return ResultDomain<Foo>.Error(new MessageItem
            {
                Code = ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR,
                Property = nameof(name),
                Table = nameof(Foo)
            });

        return ResultDomain<Foo>.Ok(new Foo
        {
            Name = name,
            Description = description
        });
    }

    // Güncelleme metodları
    public ResultDomain Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ResultDomain.Error(new MessageItem
            {
                Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY,
                Property = nameof(name),
                Table = nameof(Foo)
            });

        Name = name;
        return ResultDomain.Ok();
    }
}
```

### EF Core Configuration

```csharp
public class FooConfiguration : BaseUserTrackConfiguration<Foo>
{
    public override void Map(EntityTypeBuilder<Foo> model)
    {
        model.Property(e => e.Name).HasMaxLength(128).IsRequired();
        model.Property(e => e.Description).HasMaxLength(512);

        // Enum string conversion
        model.Property(e => e.Status)
            .HasMaxLength(32)
            .HasConversion(
                e => e.ToString(),
                e => Enumeration.FromDisplayName<FooStatus>(e)!)
            .IsRequired();

        base.Map(model);  // Key, ToTable, Schema set eder
    }

    public override string GetSchemaName() => DbSchemaNames.Foo;
    public override string GetTableName() => nameof(Foo);
}
```

---

## BaseModel

```csharp
// Her entity'nin sahip olduğu alanlar (otomatik, elle set edilmez)
public abstract class BaseModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int OrderNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
}

// User tracking ekler (kim oluşturdu, kim güncelledi)
public abstract class BaseUserTrackModel : BaseModel
{
    public Guid? CreatedUserId { get; set; }
    public Guid? UpdatedUserId { get; set; }
    public Guid? DeletedUserId { get; set; }
}
```

- `IsDeleted`, `DeletedAt`, `DeletedUserId` soft-delete için global query filter ile otomatik filtrelenir.
- `CreatedAt`, `UpdatedAt` DbContext interceptor ile otomatik set edilir; handler'da elle set edilmez.

---

## Controller Yapısı

Controller'lar **sadece mediator çağrısı** yapar, iş mantığı bulunmaz:

```csharp
[Route("foos")]
public class FoosController : BaseApiController
{
    private readonly IMediator _mediator;

    public FoosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    public async Task<FeatureObjectResultModel<GetFoo.Response>> Get(Guid id, CancellationToken ct)
        => await _mediator.FetchAsync(new GetFoo.Request { Id = id }, ct);

    [HttpPost]
    public async Task<FeatureObjectResultModel<CreateFoo.Response>> Create(
        CreateFoo.Request request,
        CancellationToken ct)
        => await _mediator.SendAsync(request, ct);
}
```

---

## EF Core Sorgu Kuralları

```csharp
// Query handler'larda HER ZAMAN AsNoTracking kullan
var item = await _context.Set<Foo>()
    .AsNoTracking()
    .Where(f => f.Id == request.Id)
    .Select(f => new Response { ... })
    .FirstOrDefaultAsync(ct);

// Birden fazla Include varsa AsSplitQuery kullan
var items = await _context.Set<Foo>()
    .AsNoTracking()
    .AsSplitQuery()
    .Include(f => f.Bars)
    .Include(f => f.Bazs)
    .ToListAsync(ct);

// IsDeleted filtresini elle yazma — global filter halleder
// ❌ .Where(f => !f.IsDeleted)   // gereksiz
// ✅ .Where(f => f.Name == ...)  // sadece iş mantığı filtresi
```

---

## Naming Conventions

| Konu | Kural | Örnek |
|------|-------|-------|
| Feature dosyası | PascalCase fiil+isim | `CreateFoo.cs`, `GetFooList.cs` |
| Feature nested class'ları | Sabit isimler | `Request`, `Response`, `Validator`, `Handler` |
| Error code'lar | UPPER_SNAKE_CASE | `COMMON_MESSAGE_VALUE_EMPTY` |
| Entity property'leri | private set | `public string? Name { get; private set; }` |
| Namespace | dizin yapısıyla birebir | `MyApp.Resources.Foos.Commands` |
| DbSchema sabitleri | Ayrı static class | `DbSchemaNames.Foo` |

---

## Error Code Standartları

```csharp
// Ortak kodlar (commons'tan gelir)
COMMON_MESSAGE_VALUE_EMPTY          // boş/null değer
COMMON_MESSAGE_RECORD_NOT_FOUND     // kayıt bulunamadı
COMMON_MESSAGE_INVALID_VALUE        // geçersiz değer
COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR // max uzunluk aşıldı
COMMON_MESSAGE_ALREADY_EXISTS       // zaten mevcut

// Domain-specific kodlar UPPER_SNAKE_CASE
FOO_CANNOT_BE_DELETED_WHEN_ACTIVE
BAR_LIMIT_EXCEEDED
```

"Bulunamadı" durumunda her zaman genel `COMMON_MESSAGE_RECORD_NOT_FOUND` kullanılır,
`Property` ve `Table` alanları hangi kayıt olduğunu belirtir. Asla `FOO_NOT_FOUND` gibi domain-specific not-found kodu üretilmez.

---

## Özet — Yeni Feature Yazarken Kontrol Listesi

1. `Resources/{Group}/Commands/` veya `Resources/{Group}/Queries/` altında tek dosya oluştur
2. `public static class FeatureName` yap
3. İçine sırayla `Request`, `Response`, `Validator`, `Handler` nested class'larını yaz
4. `Request` → `Command<FeatureObjectResultModel<Response>>` veya `Query<...>` miras al
5. `Validator` → FluentValidation, her kural için `WithErrorCode` ekle
6. `Handler` → constructor injection, iş mantığı, `FeatureObjectResultModel<Response>.Ok/Error/NotFound` döndür
7. Domain entity değiştiriliyorsa değişikliği entity'nin kendi methoduna koy, handler'da direkt property set etme
8. Query handler'larda `AsNoTracking()` unutma
9. Controller'a sadece mediator çağrısı ekle

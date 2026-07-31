# Cấu trúc dự án Flash Sale — Clean Architecture (4 Layer) + CQRS lai (Hybrid)

Dựa trên tài liệu mô tả hệ thống (7 module, 12 bảng) và yêu cầu kiến trúc mới:
đây là mô hình **CQRS lai** — chỉ 3 module "nóng" (nhiều traffic, nhiều nghiệp vụ concurrency) dùng CQRS + MediatR,
các module còn lại dùng **Service/Repository chuẩn** để giảm overhead, dễ đọc, dễ maintain.

## 0. Bảng phân loại kiến trúc theo module (đọc trước khi xem cây thư mục)

| # | Module | Kiến trúc | Lý do |
|---|---|---|---|
| 1 | **Flash Sale & Đặt hàng** (Reservation/Stock/Order) | **CQRS — Command** | Nghiệp vụ phức tạp, nhiều bước (Redis DECR → tạo Order → publish RabbitMQ), cần tách rõ input/side-effect, cần Pipeline Behavior (validate, log, retry) |
| 2 | **Thanh toán** (Payment webhook) | **CQRS — Command** | Nhận webhook ngoài (VNPay/MoMo), cần xử lý idempotent, phát domain event để tách việc gửi Email/Notification khỏi luồng chính |
| 3 | **Xem & Tìm kiếm Sản phẩm** (Catalog Query) | **CQRS — Query only** | Tách hẳn luồng đọc traffic cao khỏi luồng ghi, đọc thẳng từ Redis Cache, không qua Command/MediatR pipeline nặng |
| 4 | Auth & Identity | **Service/Repository** | CRUD nghiệp vụ đơn giản, không có nhiều side-effect phức tạp, JWT + Identity đã đủ chuẩn hoá |
| 5 | Quản lý Admin (Category, Product, Banner) | **Service/Repository** | CRUD thuần, không cần tách Command/Query vì lượng truy cập thấp, thao tác bởi Admin |
| 6 | Thông tin người dùng (Địa chỉ, Hồ sơ) | **Service/Repository** | CRUD thuần, dữ liệu cá nhân, không có nghiệp vụ concurrency |

**Nguyên tắc chọn:** module nào có (a) nghiệp vụ concurrency/side-effect phức tạp, hoặc (b) traffic đọc/ghi cực lớn cần tối ưu riêng → CQRS. Module còn lại (CRUD hành chính, ít traffic) → Service/Repository để tránh over-engineering.

```
FlashSale.sln
│
├── src/
│   ├── FlashSale.Domain/
│   ├── FlashSale.Application/
│   ├── FlashSale.Infrastructure/
│   └── FlashSale.WebApi/
│
└── tests/
    ├── FlashSale.Domain.Tests/
    ├── FlashSale.Application.Tests/
    └── FlashSale.WebApi.Tests/
```

---

## 1. FlashSale.Domain

Không đổi nhiều so với thiết kế DDD gốc — Domain không quan tâm tầng trên dùng CQRS hay Service, chỉ quan tâm nghiệp vụ.
Bổ sung 2 entity mới: `Address` (hồ sơ người dùng) và `Banner` (theo yêu cầu module Admin).

```
FlashSale.Domain/
│
├── SeedWork/
│   ├── Entity.cs
│   ├── AggregateRoot.cs
│   ├── ValueObject.cs
│   ├── IDomainEvent.cs
│   ├── IRepository.cs                  # Generic marker interface (dùng chung cho cả CQRS lẫn Service)
│   └── DomainException.cs
│
├── Users/
│   ├── User.cs                         # Aggregate Root
│   ├── UserRole.cs                     # Enum: Admin, Customer
│   ├── RefreshToken.cs
│   ├── Address.cs                      # MỚI — Entity con: FullName, Phone, Province, District, Ward, Detail, IsDefault
│   ├── ValueObjects/
│   │   ├── Email.cs
│   │   └── PhoneNumber.cs
│   ├── Events/
│   │   ├── UserRegisteredEvent.cs
│   │   └── PasswordChangedEvent.cs
│   ├── Exceptions/
│   │   └── InvalidCredentialsException.cs
│   └── IUserRepository.cs              # Dùng bởi AuthService & UserProfileService (không qua MediatR)
│
├── Catalog/
│   ├── Category.cs
│   ├── Product.cs                      # Aggregate Root
│   ├── Banner.cs                       # MỚI — Title, ImageUrl, LinkUrl, DisplayOrder, IsActive, StartDate, EndDate
│   ├── ValueObjects/
│   │   └── Money.cs
│   ├── Events/
│   │   └── ProductStockAdjustedEvent.cs
│   ├── Exceptions/
│   │   └── InsufficientStockException.cs
│   ├── ICategoryRepository.cs          # Dùng bởi Admin CategoryService
│   ├── IProductRepository.cs           # Dùng bởi Admin ProductService (ghi) — KHÔNG dùng cho đọc public
│   └── IBannerRepository.cs            # Dùng bởi Admin BannerService
│
├── FlashSales/
│   ├── FlashSale.cs                    # Aggregate Root
│   ├── FlashSaleItem.cs                # Entity con — SoldCount, RowVersion
│   ├── FlashSaleStatus.cs
│   ├── Specifications/
│   │   ├── FlashSaleCanBeActivatedSpec.cs
│   │   └── FlashSaleItemHasStockSpec.cs
│   ├── Events/
│   │   ├── FlashSaleActivatedEvent.cs
│   │   ├── FlashSaleEndedEvent.cs
│   │   ├── FlashSaleItemStockDecrementedEvent.cs
│   │   └── FlashSaleItemSoldOutEvent.cs
│   ├── Exceptions/
│   │   ├── FlashSaleNotActiveException.cs
│   │   └── FlashSaleStockExceededException.cs
│   └── IFlashSaleRepository.cs
│
├── Reservations/
│   ├── Reservation.cs                  # Aggregate Root
│   ├── ReservationStatus.cs
│   ├── Events/
│   │   ├── ReservationCreatedEvent.cs
│   │   ├── ReservationExpiredEvent.cs
│   │   └── ReservationConvertedEvent.cs
│   ├── Exceptions/
│   │   └── ReservationAlreadyExpiredException.cs
│   └── IReservationRepository.cs
│
├── Orders/
│   ├── Order.cs
│   ├── OrderItem.cs
│   ├── OrderType.cs
│   ├── OrderStatus.cs
│   ├── Events/
│   │   ├── OrderCreatedEvent.cs
│   │   ├── OrderConfirmedEvent.cs
│   │   └── OrderCancelledEvent.cs
│   ├── Exceptions/
│   │   └── OrderCannotBeCancelledException.cs
│   └── IOrderRepository.cs
│
├── Payments/
│   ├── Payment.cs
│   ├── PaymentProvider.cs
│   ├── PaymentStatus.cs
│   ├── Events/
│   │   ├── PaymentSucceededEvent.cs
│   │   └── PaymentFailedEvent.cs
│   └── IPaymentRepository.cs
│
├── Carts/
│   ├── Cart.cs
│   ├── CartItem.cs
│   └── ICartRepository.cs
│
├── Notifications/
│   ├── Notification.cs
│   ├── NotificationType.cs
│   └── INotificationRepository.cs
│
└── Common/
    └── Guards/
        └── Guard.cs
```

---

## 2. FlashSale.Application

Đây là tầng thể hiện rõ nhất sự phân tách **CQRS (3 module)** vs **Service/Repository (3 module còn lại)**.
Chia thành 2 thư mục gốc: `CQRS/` và `Services/` để một người mới đọc code biết ngay module nào theo pattern nào.

```
FlashSale.Application/
│
├── Common/
│   ├── Interfaces/
│   │   ├── IApplicationDbContext.cs
│   │   ├── ICurrentUserService.cs
│   │   ├── IDateTimeProvider.cs
│   │   ├── IEmailService.cs
│   │   ├── IJwtTokenGenerator.cs
│   │   ├── IPasswordHasher.cs
│   │   ├── IFlashSaleStockCache.cs        # Redis DECR — dùng bởi Module 1
│   │   ├── IProductCatalogCache.cs        # Redis cache read-model — dùng bởi Module 3
│   │   ├── IReservationQueue.cs           # RabbitMQ/Channel — dùng bởi Module 1
│   │   ├── IPaymentGateway.cs             # VNPay/MoMo sandbox — dùng bởi Module 2
│   │   └── INotificationHub.cs            # SignalR — dùng bởi Module 1 & 2
│   ├── Models/
│   │   ├── PagedResult.cs
│   │   └── Result.cs
│   └── Exceptions/
│       └── ApplicationException.cs
│
├── CQRS/                                   # ====== 3 MODULE DÙNG MEDIATR ======
│   │
│   ├── FlashSaleOrders/                     # MODULE 1: Flash Sale & Đặt hàng (Command)
│   │   ├── Commands/
│   │   │   ├── PlaceFlashSaleOrder/         # Command chính: "giật deal"
│   │   │   │   ├── PlaceFlashSaleOrderCommand.cs        # FlashSaleItemId, UserId, Quantity
│   │   │   │   ├── PlaceFlashSaleOrderCommandHandler.cs # 1) Redis DECR atomic (Lua script)
│   │   │   │   │                                        # 2) Tạo Reservation (Holding) + Order (Pending)
│   │   │   │   │                                        # 3) Publish message vào RabbitMQ (OrderPlacedIntegrationEvent)
│   │   │   │   │                                        # 4) Nếu Redis DECR thất bại (hết hàng) → trả lỗi ngay, không chạm DB
│   │   │   │   └── PlaceFlashSaleOrderCommandValidator.cs
│   │   │   ├── ExpireReservations/          # Batch command — Hangfire job gọi mỗi 30-60s
│   │   │   │   ├── ExpireReservationsCommand.cs
│   │   │   │   └── ExpireReservationsCommandHandler.cs  # Hoàn Redis stock + đánh dấu Reservation Expired
│   │   │   ├── CancelExpiredOrders/         # Hangfire job quét PaymentDeadline
│   │   │   │   ├── CancelExpiredOrdersCommand.cs
│   │   │   │   └── CancelExpiredOrdersCommandHandler.cs
│   │   │   ├── ActivateFlashSale/           # Hangfire job: Upcoming → Active
│   │   │   │   ├── ActivateFlashSaleCommand.cs
│   │   │   │   └── ActivateFlashSaleCommandHandler.cs   # Warm Redis stock từ SaleStock khi Active
│   │   │   ├── EndFlashSale/                # Hangfire job: Active → Ended
│   │   │   │   ├── EndFlashSaleCommand.cs
│   │   │   │   └── EndFlashSaleCommandHandler.cs
│   │   │   └── SyncFlashSaleStockToDb/      # Write-behind: Redis → SQL SoldCount
│   │   │       ├── SyncFlashSaleStockToDbCommand.cs
│   │   │       └── SyncFlashSaleStockToDbCommandHandler.cs
│   │   ├── Queries/
│   │   │   └── GetReservationStatus/        # Query nhẹ, dùng chung MediatR pipeline cho tiện polling
│   │   │       ├── GetReservationStatusQuery.cs
│   │   │       └── GetReservationStatusQueryHandler.cs
│   │   └── EventHandlers/
│   │       ├── ReleaseStockOnReservationExpiredHandler.cs   # Handle ReservationExpiredEvent
│   │       ├── NotifyFlashSaleItemSoldOutHandler.cs         # Push SignalR "hết hàng"
│   │       └── PublishOrderPlacedIntegrationEventHandler.cs # Convert Domain Event → RabbitMQ message
│   │
│   ├── Payments/                            # MODULE 2: Thanh toán (Command)
│   │   ├── Commands/
│   │   │   ├── InitiatePayment/             # Tạo URL thanh toán VNPay/MoMo sandbox
│   │   │   │   ├── InitiatePaymentCommand.cs
│   │   │   │   └── InitiatePaymentCommandHandler.cs
│   │   │   └── ConfirmPaymentCallback/      # Nhận webhook/callback từ cổng thanh toán
│   │   │       ├── ConfirmPaymentCallbackCommand.cs       # TransactionCode, Amount, Signature, Status
│   │   │       ├── ConfirmPaymentCallbackCommandHandler.cs # 1) Verify chữ ký/signature
│   │   │       │                                          # 2) Idempotency check (TransactionCode đã xử lý chưa)
│   │   │       │                                          # 3) Update Payment.Status + Order.Status
│   │   │       │                                          # 4) Publish PaymentSucceededEvent/PaymentFailedEvent
│   │   │       └── ConfirmPaymentCallbackCommandValidator.cs
│   │   └── EventHandlers/
│   │       ├── ConfirmOrderOnPaymentSucceededHandler.cs    # Order.Confirm()
│   │       ├── SendOrderConfirmationEmailHandler.cs        # Phát Email khi thanh toán thành công
│   │       ├── SendPaymentFailedNotificationHandler.cs     # Phát Notification khi thanh toán thất bại
│   │       └── NotifyPaymentResultRealtimeHandler.cs       # Push SignalR kết quả thanh toán
│   │
│   └── CatalogQuery/                        # MODULE 3: Xem & Tìm kiếm Sản phẩm (Query ONLY — không Command)
│       ├── Queries/
│       │   ├── GetProducts/                 # Danh sách + filter + search + phân trang
│       │   │   ├── GetProductsQuery.cs
│       │   │   ├── GetProductsQueryHandler.cs   # Đọc thẳng từ IProductCatalogCache (Redis), fallback DB nếu cache miss
│       │   │   └── ProductDto.cs
│       │   ├── GetProductById/
│       │   │   ├── GetProductByIdQuery.cs
│       │   │   └── GetProductByIdQueryHandler.cs
│       │   ├── GetActiveFlashSales/         # Danh sách flash sale đang chạy — cache nóng
│       │   │   ├── GetActiveFlashSalesQuery.cs
│       │   │   └── FlashSaleDto.cs
│       │   └── GetFlashSaleItemStock/       # Số lượng còn lại real-time — đọc trực tiếp Redis key
│       │       ├── GetFlashSaleItemStockQuery.cs
│       │       └── GetFlashSaleItemStockQueryHandler.cs
│       └── CacheWarming/
│           └── ProductCatalogCacheWarmer.cs # Được gọi khi Admin CRUD Product/Category (từ Services/Admin) để invalidate/refresh cache
│
└── Services/                                # ====== CÁC MODULE CÒN LẠI — SERVICE/REPOSITORY CHUẨN, KHÔNG DÙNG MEDIATR ======
    │
    ├── Auth/                                # Auth & Identity
    │   ├── IAuthService.cs
    │   ├── AuthService.cs                   # Đăng ký, đăng nhập, refresh token, revoke, đổi/quên mật khẩu
    │   │                                     # Gọi trực tiếp IUserRepository + IJwtTokenGenerator + IPasswordHasher
    │   └── DTOs/
    │       ├── RegisterRequestDto.cs
    │       ├── LoginRequestDto.cs
    │       ├── AuthResultDto.cs             # AccessToken + RefreshToken
    │       └── ChangePasswordRequestDto.cs
    │
    ├── Admin/                                # Quản lý Admin: Category, Product, Banner
    │   ├── ICategoryService.cs
    │   ├── CategoryService.cs                # CRUD Category, gọi ICategoryRepository trực tiếp
    │   ├── IProductAdminService.cs
    │   ├── ProductAdminService.cs             # CRUD Product (ghi) — sau khi Save() gọi ProductCatalogCacheWarmer
    │   │                                      # để đồng bộ Redis cache cho luồng đọc (Module 3)
    │   ├── IBannerService.cs
    │   ├── BannerService.cs                   # CRUD Banner
    │   └── DTOs/
    │       ├── CategoryDto.cs
    │       ├── ProductAdminDto.cs
    │       └── BannerDto.cs
    │
    └── UserProfile/                          # Thông tin người dùng: Hồ sơ, Địa chỉ giao hàng
        ├── IUserProfileService.cs
        ├── UserProfileService.cs             # Xem/cập nhật hồ sơ cá nhân
        ├── IAddressService.cs
        ├── AddressService.cs                 # CRUD địa chỉ giao hàng, đặt địa chỉ mặc định
        └── DTOs/
            ├── UserProfileDto.cs
            └── AddressDto.cs
```

**Lưu ý ranh giới quan trọng:**
- `ProductAdminService` (ghi — Service) và `GetProducts` Query (đọc — CQRS) **cùng thao tác trên `Product`** nhưng đi 2 đường khác nhau: ghi qua Repository → SQL Server; đọc qua `IProductCatalogCache` → Redis. Khi Admin sửa sản phẩm, `ProductAdminService` phải chủ động gọi `ProductCatalogCacheWarmer` để tránh lệch cache (không dùng Domain Event ở đây vì đây là module Service, không phải CQRS — giữ đơn giản, gọi trực tiếp).
- `Auth`, `Admin`, `UserProfile` **không có** `Commands/`, `Queries/`, không phụ thuộc MediatR — Controller gọi thẳng Service qua interface (constructor injection thông thường).
- 3 module CQRS vẫn dùng chung `Common/Behaviors` (Validation, Logging, UnhandledException) qua MediatR Pipeline — đây là điểm khác biệt lớn nhất so với Service (Service tự try/catch + validate thủ công trong hàm).

---

## 3. FlashSale.Infrastructure

```
FlashSale.Infrastructure/
│
├── Persistence/
│   ├── ApplicationDbContext.cs             # Implement IApplicationDbContext, dispatch Domain Events khi SaveChanges
│   ├── ApplicationDbContextInitializer.cs  # Seed Admin mặc định, Category/Banner mẫu
│   ├── Configurations/
│   │   ├── UserConfiguration.cs
│   │   ├── RefreshTokenConfiguration.cs
│   │   ├── AddressConfiguration.cs         # MỚI
│   │   ├── CategoryConfiguration.cs
│   │   ├── ProductConfiguration.cs
│   │   ├── BannerConfiguration.cs          # MỚI
│   │   ├── FlashSaleConfiguration.cs
│   │   ├── FlashSaleItemConfiguration.cs   # RowVersion .IsRowVersion(), composite index
│   │   ├── ReservationConfiguration.cs
│   │   ├── OrderConfiguration.cs
│   │   ├── OrderItemConfiguration.cs
│   │   ├── PaymentConfiguration.cs
│   │   ├── CartItemConfiguration.cs
│   │   └── NotificationConfiguration.cs
│   ├── Repositories/
│   │   ├── UserRepository.cs               # Dùng bởi Services/Auth + Services/UserProfile
│   │   ├── AddressRepository.cs            # MỚI — dùng bởi Services/UserProfile
│   │   ├── CategoryRepository.cs           # Dùng bởi Services/Admin
│   │   ├── ProductRepository.cs            # Dùng bởi Services/Admin (ghi)
│   │   ├── BannerRepository.cs             # MỚI — dùng bởi Services/Admin
│   │   ├── FlashSaleRepository.cs          # Dùng bởi CQRS/FlashSaleOrders
│   │   ├── ReservationRepository.cs        # Dùng bởi CQRS/FlashSaleOrders
│   │   ├── OrderRepository.cs              # Dùng bởi CQRS/FlashSaleOrders & Payments
│   │   ├── CartRepository.cs
│   │   └── NotificationRepository.cs
│   ├── Interceptors/
│   │   └── DomainEventDispatchInterceptor.cs
│   └── Migrations/
│
├── Caching/
│   ├── RedisFlashSaleStockCache.cs          # Implement IFlashSaleStockCache — DECR atomic, Lua script chống oversell (Module 1)
│   ├── RedisProductCatalogCache.cs          # Implement IProductCatalogCache — cache Product/Category/FlashSale list (Module 3)
│   ├── ProductCatalogCacheWarmer.cs         # Implement warmer được gọi từ ProductAdminService khi CRUD
│   └── RedisConnectionFactory.cs
│
├── Messaging/
│   ├── RabbitMq/
│   │   ├── RabbitMqEventBus.cs              # Publish OrderPlacedIntegrationEvent (Module 1)
│   │   ├── RabbitMqConnectionFactory.cs
│   │   └── Consumers/
│   │       └── FlashSaleOrderPlacedConsumer.cs
│   └── InternalQueue/
│       └── ReservationChannelQueue.cs       # Phương án thay thế RabbitMQ bằng Channel nội bộ nếu cần
│
├── BackgroundJobs/                          # Hangfire — gọi Command của Module 1 & 2
│   ├── FlashSaleStatusJob.cs                # Gọi ActivateFlashSaleCommand / EndFlashSaleCommand
│   ├── ReservationExpirationJob.cs          # Gọi ExpireReservationsCommand
│   ├── OrderPaymentDeadlineJob.cs           # Gọi CancelExpiredOrdersCommand
│   └── FlashSaleStockSyncJob.cs             # Gọi SyncFlashSaleStockToDbCommand
│
├── Realtime/
│   ├── NotificationHub.cs                   # SignalR Hub
│   └── SignalRNotificationService.cs        # Implement INotificationHub
│
├── Identity/
│   ├── JwtTokenGenerator.cs                 # Implement IJwtTokenGenerator (dùng ASP.NET Core Identity + JWT)
│   ├── CurrentUserService.cs
│   └── IdentityPasswordHasher.cs            # Wrap ASP.NET Core Identity PasswordHasher<User>
│
├── Payments/
│   ├── VnPaySandboxGateway.cs               # Implement IPaymentGateway
│   ├── MomoSandboxGateway.cs
│   └── PaymentGatewayFactory.cs
│
├── Email/
│   ├── SmtpEmailService.cs
│   └── Templates/
│       ├── OrderConfirmationTemplate.html
│       ├── OrderCancelledTemplate.html
│       ├── PaymentFailedTemplate.html
│       └── WelcomeEmailTemplate.html
│
├── DateTime/
│   └── DateTimeProvider.cs
│
└── DependencyInjection.cs                   # AddInfrastructure(...) — đăng ký cả CQRS handler lẫn Service
```

---

## 4. FlashSale.WebApi

Controller phân 2 nhóm rõ ràng: **gọi `IMediator`** (3 module CQRS) và **gọi thẳng Service qua interface** (3 module còn lại).

```
FlashSale.WebApi/
│
├── Controllers/
│   │
│   ├── # ---- Nhóm CQRS: Controller inject IMediator ----
│   ├── FlashSaleOrdersController.cs         # POST /flash-sale-orders → PlaceFlashSaleOrderCommand (endpoint "nóng" nhất, cần rate limit)
│   ├── PaymentsController.cs                # POST /payments/webhook/vnpay|momo → ConfirmPaymentCallbackCommand
│   ├── ProductsController.cs                # GET /products, /products/{id} → GetProductsQuery, GetProductByIdQuery (public, cache Redis)
│   ├── FlashSalesController.cs              # GET /flash-sales/active → GetActiveFlashSalesQuery
│   │
│   ├── # ---- Nhóm Service/Repository: Controller inject Service interface trực tiếp ----
│   ├── AuthController.cs                    # inject IAuthService — Register, Login, Refresh, Revoke, ChangePassword
│   ├── AdminCategoryController.cs           # inject ICategoryService
│   ├── AdminProductController.cs            # inject IProductAdminService (CRUD — khác ProductsController public)
│   ├── AdminBannerController.cs             # inject IBannerService
│   ├── UserProfileController.cs             # inject IUserProfileService
│   ├── AddressController.cs                 # inject IAddressService
│   │
│   ├── CartController.cs                    # Service/Repository chuẩn (không nêu trong 3 module CQRS)
│   ├── NotificationsController.cs
│   └── AdminDashboardController.cs
│
├── Hubs/
│   └── (tham chiếu NotificationHub trong Infrastructure.Realtime)
│
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs       # Map DomainException / Service exception → RFC 7807 ProblemDetails
│   └── RequestLoggingMiddleware.cs
│
├── Filters/
│   └── PaymentWebhookSignatureFilter.cs     # Verify signature trước khi vào PaymentsController
│
├── Extensions/
│   ├── ServiceCollectionExtensions.cs       # AddApplicationCqrs() + AddApplicationServices() riêng biệt
│   └── RateLimiterExtensions.cs             # Bắt buộc cho FlashSaleOrdersController
│
├── BackgroundJobsRegistration/
│   └── HangfireJobsRegistration.cs
│
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── FlashSale.WebApi.http
```

**`Program.cs` / DI đăng ký tách riêng để dễ nhìn:**
```csharp
// CQRS — chỉ 3 module dùng MediatR
services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<PlaceFlashSaleOrderCommand>());
services.AddValidatorsFromAssemblyContaining<PlaceFlashSaleOrderCommandValidator>();

// Service/Repository — 3 module còn lại, đăng ký thủ công theo interface
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<ICategoryService, CategoryService>();
services.AddScoped<IProductAdminService, ProductAdminService>();
services.AddScoped<IBannerService, BannerService>();
services.AddScoped<IUserProfileService, UserProfileService>();
services.AddScoped<IAddressService, AddressService>();

// JWT + Identity
services.AddIdentity<User, IdentityRole<Guid>>(...)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* TokenValidationParameters ... */ });
```

---

## 5. Bổ sung Database Schema (so với tài liệu gốc)

### 5.1. Bảng `Addresses` (MỚI — phục vụ module Thông tin người dùng)
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| UserId | uniqueidentifier (FK → Users) | |
| RecipientName | nvarchar(100) | |
| PhoneNumber | varchar(20) | |
| Province | nvarchar(100) | |
| District | nvarchar(100) | |
| Ward | nvarchar(100) | |
| DetailAddress | nvarchar(300) | |
| IsDefault | bit | |

### 5.2. Bảng `Banners` (MỚI — phục vụ module Admin)
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| Title | nvarchar(200) | |
| ImageUrl | nvarchar(500) | |
| LinkUrl | nvarchar(500) | |
| DisplayOrder | int | |
| IsActive | bit | |
| StartDate | datetime2 | Nullable |
| EndDate | datetime2 | Nullable |

---

## 6. Ánh xạ Module → Layer → Kiến trúc (tổng hợp)

| Module | Kiến trúc | Domain | Application | Infrastructure | WebApi |
|---|---|---|---|---|---|
| Flash Sale & Đặt hàng | **CQRS** | `FlashSales/`, `Reservations/`, `Orders/` | `CQRS/FlashSaleOrders/` | `Caching/RedisFlashSaleStockCache`, `Messaging/RabbitMq`, `BackgroundJobs/*` | `FlashSaleOrdersController` |
| Thanh toán | **CQRS** | `Payments/` | `CQRS/Payments/` | `Payments/`, `Realtime/`, `Email/` | `PaymentsController` |
| Xem & Tìm kiếm Sản phẩm | **CQRS (Query only)** | `Catalog/` (đọc) | `CQRS/CatalogQuery/` | `Caching/RedisProductCatalogCache` | `ProductsController`, `FlashSalesController` |
| Auth & Identity | **Service/Repository** | `Users/` | `Services/Auth/` | `Identity/`, `Persistence/Repositories/UserRepository` | `AuthController` |
| Quản lý Admin | **Service/Repository** | `Catalog/` (ghi) | `Services/Admin/` | `Persistence/Repositories/{Category,Product,Banner}Repository` | `AdminCategoryController`, `AdminProductController`, `AdminBannerController` |
| Thông tin người dùng | **Service/Repository** | `Users/Address.cs` | `Services/UserProfile/` | `Persistence/Repositories/AddressRepository` | `UserProfileController`, `AddressController` |
| Cart, Notification, Dashboard | Service/Repository (giữ nguyên, không đổi) | `Carts/`, `Notifications/` | tương ứng module | tương ứng | `CartController`, `NotificationsController`, `AdminDashboardController` |

---

## 7. Điểm nhấn kỹ thuật khi viết README/CV

1. **CQRS lai (Selective CQRS)**: chỉ áp dụng MediatR cho 3 module thật sự cần — tránh over-engineering ở các module CRUD hành chính (Auth, Admin, Profile), thể hiện tư duy chọn đúng công cụ cho đúng bài toán thay vì áp CQRS toàn bộ dự án.
2. **Tách Command/Query vật lý cho Catalog**: ghi (Admin CRUD) đi qua `ProductRepository` → SQL Server; đọc (public, traffic cao) đi qua `IProductCatalogCache` → Redis. Đây là ví dụ CQRS ở mức hạ tầng (không chỉ ở mức code pattern).
3. **Chống oversell**: Redis atomic Lua script (chặn đầu) + `RowVersion` Optimistic Concurrency (chặn cuối khi write-behind về SQL).
4. **Idempotency ở Payment webhook**: kiểm tra `TransactionCode` đã xử lý trước khi update, tránh xử lý trùng khi cổng thanh toán gọi lại webhook.
5. **Domain Event chỉ dùng trong 2 module CQRS** để tách side-effect (Email, SignalR, RabbitMQ) khỏi Command Handler chính; module Service không dùng Domain Event, xử lý side-effect trực tiếp trong Service method cho đơn giản.

---

Bạn muốn mình viết chi tiết code thật cho **`PlaceFlashSaleOrderCommandHandler`** (Redis DECR → tạo Reservation + Order → publish RabbitMQ) để có ví dụ implement đầy đủ cho module trọng tâm nhất không?

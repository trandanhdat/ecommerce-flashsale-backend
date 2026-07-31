# Tổng hợp các lỗi và vấn đề kỹ thuật trong dự án FlashSale

Tài liệu này ghi lại toàn bộ các lỗi, cảnh báo và vấn đề thiết kế (Code Smell) đã gặp phải từ đầu quá trình xây dựng dự án FlashSale (Clean Architecture) cùng cách khắc phục.

---

## 1. Lỗi Build ở giai đoạn khởi tạo (Lỗi biên dịch & Lỗi Domain Event)
- **Hiện tượng:** Khi chạy `dotnet build` ở những bước đầu tiên (lúc khởi tạo ApplicationDbContext và Domain), dự án liên tục báo lỗi biên dịch (như bạn đã nhắc "bij lỗi rồi xem log dotnet build đi", "lỗi tiếp").
- **Nguyên nhân:**
  - Lỗi cú pháp và thiếu thư viện khi thiết lập các Entity ban đầu.
  - Vấn đề trùng lặp mã: Trong file AggregateRoot, hệ thống tự sinh ra các hàm `AddDomainEvent`, `RemoveDomainEvent`, `ClearDomainEvents` trong khi những hàm này **đã tồn tại sẵn** trong class cha `Entity`.
- **Cách fix:** 
  - Khắc phục các lỗi cú pháp để project build thành công.
  - Xoá bỏ các đoạn code xử lý DomainEvent thừa thãi trong AggregateRoot vì class `Entity` ( SeedWork) đã đảm nhiệm tốt vai trò này.

---

## 2. Vấn đề thiết kế: Gom chung tất cả Repository vào 1 file (Code Smell)
- **Hiện tượng:** File `Repositories.cs` ở tầng Infrastructure bị phình to (chứa toàn bộ 12 class Repositories như UserRepository, AddressRepository, OrderRepository... chung một chỗ).
- **Nguyên nhân:** Lỗi tổ chức mã nguồn (Code Organization) không tuân thủ nguyên tắc Single Responsibility Principle (SRP) của SOLID.
- **Cách fix:** Đã tiến hành "Tách đi" theo yêu cầu của bạn, chia nhỏ `Repositories.cs` thành 12 file `.cs` riêng biệt đặt trong thư mục `Infrastructure/Persistence/Repositories/` (Ví dụ: `CategoryRepository.cs`, `AddressRepository.cs`...).

---

## 3. Lỗi xác thực 401 Unauthorized (Lỗi "Bearer Bearer")
- **Hiện tượng:** Khi gọi API yêu cầu xác thực (ví dụ: `POST /api/Auth/change-password`), Server trả về lỗi `401 Unauthorized` mặc dù Token truyền vào có vẻ hợp lệ.
- **Nguyên nhân:** Swagger đã được cấu hình tự động thêm tiền tố `Bearer ` vào Header `Authorization`. Tuy nhiên, khi test, bạn dán chuỗi token kèm luôn chữ `Bearer <chuỗi_token>`. Kết quả Server nhận được Header có dạng `Bearer Bearer <chuỗi_token>`.
- **Cách fix:** Cập nhật lại mô tả Swagger để nhắc người dùng **chỉ dán chuỗi Token**.

---

## 4. Lỗi đăng nhập sai thông tin (InvalidCredentialsException)
- **Hiện tượng:** Bắn ra lỗi `FlashSale.Domain.Users.Exceptions.InvalidCredentialsException: Invalid credentials for user dat@gmail.com.` khi gọi API Login.
- **Nguyên nhân:** Logic Hash mật khẩu lưu trong DB (khi chạy Database Seeder) và logic Verify mật khẩu lúc Login không khớp nhau. 
- **Cách fix:** Cập nhật lại Database Seeder để tạo ra chuỗi Hash hợp lệ bằng chính `IdentityPasswordHasher` chuẩn của ASP.NET Core trước khi lưu xuống DB.

---

## 5. Lỗi Generic Constraint của Repository (CS0311 & CS0535)
- **Hiện tượng:** Lỗi đỏ tại `AddressRepository.cs` báo `Address` không thể được ép kiểu sang `IAggregateRoot`.
- **Nguyên nhân:** Theo chuẩn DDD, `Address` là một Child Entity của `User` (chỉ kế thừa `Entity`). Nhưng `RepositoryBase<T>` lại bắt buộc `T : IAggregateRoot`.
- **Cách fix:** Cho `AddressRepository` triển khai thẳng `IAddressRepository` (bỏ kế thừa `RepositoryBase`), đồng thời fix lại các property bị đổi tên trong cấu hình EF Core (`AddressConfiguration`).

---

## 6. Lỗi đăng ký AutoMapper Dependency Injection (CS1503)
- **Hiện tượng:** Lỗi build `error CS1503: Argument 2: cannot convert from 'System.Type' to 'System.Action<AutoMapper.IMapperConfigurationExpression>'` tại file `Program.cs`.
- **Nguyên nhân:** Cú pháp đăng ký AutoMapper (phiên bản v13+) đã thay đổi, không cho phép truyền trực tiếp Type/Assembly vào hàm `AddAutoMapper` như các bản cũ.
- **Cách fix:** Đổi sang cú pháp Action Delegate chuẩn của bản mới:
  ```csharp
  builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(CategoryMappingProfile).Assembly));
  ```

---

## 7. Lỗi Runtime khi khởi động do thiếu EF Core Migration
- **Hiện tượng:** Khi chạy ứng dụng (`dotnet run`), ứng dụng crash ngay lúc khởi động (trong quá trình `DatabaseSeeder.SeedAsync` gọi `MigrateAsync`) với lỗi:
  ```
  Unhandled exception. System.InvalidOperationException: An error was generated for warning 'Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning': The model for context 'ApplicationDbContext' has pending changes. Add a new migration before updating the database.
  ```
- **Nguyên nhân:** Do EF Core 9 mặc định sẽ ném exception nếu phát hiện ra Database Schema (như bảng `Addresses` bị đổi tên cột, `Category` được thêm cột `Slug`) đã thay đổi trong source code nhưng chưa được tạo Migration để đồng bộ.
- **Cách fix:** Mở terminal và chạy lệnh Add Migration để EF Core sinh ra bản ghi cập nhật:
  ```bash
  dotnet ef migrations add UpdateCategoryAndAddress -p ../ecommerce-flashsale-backend.Infrastructure -s .
  ```

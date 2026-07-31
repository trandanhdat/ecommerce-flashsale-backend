@# TÀI LIỆU MÔ TẢ HỆ THỐNG
## Hệ thống Flash Sale - Săn hàng giới hạn theo thời gian thực

---

## 1. TỔNG QUAN HỆ THỐNG

Hệ thống thương mại điện tử quy mô nhỏ, tập trung vào tính năng cốt lõi **Flash Sale**: cho phép nhiều người dùng cùng tranh mua một sản phẩm có số lượng giới hạn trong một khung thời gian xác định, đảm bảo không xảy ra bán vượt tồn kho (overselling) dù có hàng nghìn request đồng thời.

Hệ thống gồm 3 nhóm tác nhân:
- **Khách hàng (User)**: duyệt sản phẩm, tham gia flash sale, đặt mua, thanh toán, theo dõi đơn hàng.
- **Quản trị viên (Admin)**: quản lý sản phẩm, tạo/điều phối chương trình flash sale, theo dõi thống kê.
- **Hệ thống nền (System)**: tự động mở/đóng flash sale, huỷ đơn quá hạn, gửi thông báo.

---

## 2. DANH SÁCH MODULE

### 2.1. Module Auth & Identity
Quản lý đăng ký, đăng nhập, phân quyền (Admin / User) bằng JWT + Refresh Token, sử dụng ASP.NET Core Identity.

**Chức năng chính:**
- Đăng ký, đăng nhập, đăng xuất
- Refresh token, revoke token
- Phân quyền theo Role (Admin, Customer)
- Quên mật khẩu / đổi mật khẩu

### 2.2. Module Catalog (Danh mục & Sản phẩm)
Quản lý dữ liệu sản phẩm nền tảng — không liên quan trực tiếp đến flash sale nhưng là dữ liệu gốc để tạo chương trình sale.

**Chức năng chính:**
- CRUD Danh mục (Category)
- CRUD Sản phẩm (Product): tên, mô tả, ảnh, giá gốc, tồn kho tổng
- Tìm kiếm, lọc, phân trang sản phẩm

### 2.3. Module Flash Sale Engine (Module trọng tâm)
Module xử lý nghiệp vụ phức tạp nhất hệ thống — nơi thể hiện kỹ thuật concurrency và tối ưu hiệu năng.

**Chức năng chính:**
- Admin tạo chương trình Flash Sale: chọn sản phẩm, số lượng giới hạn (`SaleStock`), giá sale, thời gian bắt đầu/kết thúc
- Job nền (Hangfire) tự động chuyển trạng thái sale: `Upcoming → Active → Ended`
- Đếm ngược thời gian, cập nhật số lượng còn lại real-time qua SignalR
- Xử lý "giữ chỗ" (reservation) khi user bấm mua:
  - Dùng Redis `DECR` atomic để trừ tồn kho tạm thời, tránh race condition
  - Giữ chỗ trong X phút (ví dụ 5 phút) để user thanh toán, hết hạn tự hoàn lại số lượng
- Hàng đợi xử lý (Queue/Channel nội bộ hoặc RabbitMQ) khi lượng request tại thời điểm mở sale quá lớn, tránh nghẽn DB

### 2.4. Module Order & Payment
Quản lý vòng đời đơn hàng, từ lúc giữ chỗ thành công đến khi thanh toán hoặc huỷ.

**Chức năng chính:**
- Tạo đơn hàng ở trạng thái `Pending` khi giữ chỗ thành công
- Tích hợp thanh toán giả lập (VNPay/MoMo sandbox)
- Xác nhận thanh toán → chuyển đơn sang `Confirmed`
- Huỷ đơn tự động nếu quá hạn thanh toán (Hangfire job) → hoàn lại `SaleStock`
- Lịch sử đơn hàng của user

### 2.5. Module Cart (Giỏ hàng thường)
Giỏ hàng cho luồng mua sắm thông thường (ngoài flash sale), giúp hệ thống có đủ chức năng e-commerce cơ bản.

**Chức năng chính:**
- Thêm/xoá/cập nhật số lượng sản phẩm trong giỏ
- Checkout giỏ hàng thành đơn hàng thường (không qua Flash Sale Engine)

### 2.6. Module Notification
Gửi thông báo cho người dùng qua email và real-time.

**Chức năng chính:**
- Email xác nhận đơn hàng, email huỷ đơn
- Thông báo real-time (SignalR) khi: sale sắp mở, sale hết hàng, đặt chỗ thành công/thất bại

### 2.7. Module Admin Dashboard
Thống kê và giám sát vận hành cho Admin.

**Chức năng chính:**
- Thống kê lượt xem, lượt tham gia, tỉ lệ chuyển đổi từng chương trình flash sale
- Biểu đồ doanh thu theo thời gian
- Danh sách đơn hàng, lọc theo trạng thái, duyệt/huỷ thủ công

---

## 3. CƠ SỞ DỮ LIỆU (DATABASE SCHEMA)

### 3.1. Bảng `Users`
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| FullName | nvarchar(100) | |
| Email | nvarchar(150) | Unique |
| PasswordHash | nvarchar(max) | |
| PhoneNumber | varchar(20) | |
| Role | varchar(20) | Admin / Customer |
| CreatedAt | datetime2 | |
| IsActive | bit | |

### 3.2. Bảng `RefreshTokens`
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| UserId | uniqueidentifier (FK → Users) | |
| Token | nvarchar(max) | |
| ExpiresAt | datetime2 | |
| IsRevoked | bit | |

### 3.3. Bảng `Categories`
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | int (PK) | |
| Name | nvarchar(100) | |
| Slug | varchar(150) | Unique |

### 3.4. Bảng `Products`
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| CategoryId | int (FK → Categories) | |
| Name | nvarchar(200) | |
| Description | nvarchar(max) | |
| ImageUrl | nvarchar(500) | |
| BasePrice | decimal(18,2) | Giá gốc |
| StockQuantity | int | Tồn kho tổng (bán thường) |
| CreatedAt | datetime2 | |

**Index đề xuất:** `IX_Products_CategoryId`, full-text hoặc index trên `Name` nếu cần tìm kiếm nhanh.

### 3.5. Bảng `FlashSales`
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| Name | nvarchar(200) | Tên chương trình |
| StartTime | datetime2 | |
| EndTime | datetime2 | |
| Status | varchar(20) | Upcoming / Active / Ended |
| CreatedBy | uniqueidentifier (FK → Users) | Admin tạo |

**Index đề xuất:** `IX_FlashSales_Status_StartTime` (composite) — phục vụ job quét sale cần mở/đóng.

### 3.6. Bảng `FlashSaleItems`
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| FlashSaleId | uniqueidentifier (FK → FlashSales) | |
| ProductId | uniqueidentifier (FK → Products) | |
| SalePrice | decimal(18,2) | Giá sale |
| SaleStock | int | Số lượng giới hạn cho sale |
| SoldCount | int | Số lượng đã bán (cập nhật bằng transaction) |
| RowVersion | rowversion | Dùng cho Optimistic Concurrency |

**Index đề xuất:** composite index `IX_FlashSaleItems_FlashSaleId_ProductId` (unique). Đây là bảng "nóng" nhất hệ thống — số lượng tồn thực tế trong lúc sale nên đồng bộ với Redis key `flashsale:{id}:stock`, DB chỉ là nguồn dữ liệu cuối cùng (source of truth) để đối soát.

### 3.7. Bảng `Reservations` (Giữ chỗ tạm thời)
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| FlashSaleItemId | uniqueidentifier (FK → FlashSaleItems) | |
| UserId | uniqueidentifier (FK → Users) | |
| Quantity | int | |
| Status | varchar(20) | Holding / Expired / Converted |
| ExpiresAt | datetime2 | Thời điểm hết hạn giữ chỗ |
| CreatedAt | datetime2 | |

**Index đề xuất:** `IX_Reservations_Status_ExpiresAt` — phục vụ job quét reservation hết hạn để hoàn kho.

### 3.8. Bảng `Orders`
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| UserId | uniqueidentifier (FK → Users) | |
| OrderType | varchar(20) | Normal / FlashSale |
| ReservationId | uniqueidentifier (FK → Reservations, nullable) | Chỉ có nếu đến từ Flash Sale |
| TotalAmount | decimal(18,2) | |
| Status | varchar(20) | Pending / Confirmed / Cancelled / Completed |
| CreatedAt | datetime2 | |
| PaymentDeadline | datetime2 | |

### 3.9. Bảng `OrderItems`
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| OrderId | uniqueidentifier (FK → Orders) | |
| ProductId | uniqueidentifier (FK → Products) | |
| UnitPrice | decimal(18,2) | |
| Quantity | int | |

### 3.10. Bảng `Payments`
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| OrderId | uniqueidentifier (FK → Orders) | |
| Provider | varchar(20) | VNPay / MoMo (sandbox) |
| TransactionCode | varchar(100) | |
| Amount | decimal(18,2) | |
| Status | varchar(20) | Pending / Success / Failed |
| PaidAt | datetime2 | Nullable |

### 3.11. Bảng `CartItems`
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| UserId | uniqueidentifier (FK → Users) | |
| ProductId | uniqueidentifier (FK → Products) | |
| Quantity | int | |

### 3.12. Bảng `Notifications`
| Cột | Kiểu dữ liệu | Ghi chú |
|---|---|---|
| Id | uniqueidentifier (PK) | |
| UserId | uniqueidentifier (FK → Users) | |
| Type | varchar(30) | Email / Realtime |
| Content | nvarchar(500) | |
| IsRead | bit | |
| CreatedAt | datetime2 | |

---

## 4. QUAN HỆ GIỮA CÁC BẢNG (TÓM TẮT)

```
Users 1─N RefreshTokens
Users 1─N Orders
Users 1─N Reservations
Users 1─N CartItems
Users 1─N Notifications

Categories 1─N Products
Products 1─N FlashSaleItems
Products 1─N OrderItems
Products 1─N CartItems

FlashSales 1─N FlashSaleItems
FlashSaleItems 1─N Reservations

Reservations 1─1 Orders (nullable, chỉ khi OrderType = FlashSale)
Orders 1─N OrderItems
Orders 1─1 Payments
```

---

## 5. GHI CHÚ THIẾT KẾ QUAN TRỌNG

- **Nguồn sự thật (source of truth) về tồn kho flash sale**: Redis giữ số liệu real-time để xử lý nhanh, nhưng mọi thay đổi cuối cùng đều phải đồng bộ ghi xuống `FlashSaleItems.SoldCount` trong SQL Server (qua background worker hoặc write-behind) để đảm bảo tính nhất quán khi cần đối soát.
- **RowVersion** trên `FlashSaleItems` dùng cho Optimistic Concurrency ở tầng DB, là lớp bảo vệ thứ hai phòng khi Redis lock thất bại.
- **Reservation hết hạn** nên được quét bằng Hangfire job chạy mỗi 30-60 giây, không nên dựa hoàn toàn vào TTL của Redis để tránh mất đồng bộ với DB.
- **Composite index** trên các bảng `FlashSaleItems`, `Reservations`, `FlashSales` là bắt buộc vì đây là các bảng bị truy vấn/ghi với tần suất cao nhất trong toàn hệ thống.

---

*Tài liệu này là bản mô tả kiến trúc module và schema DB ở mức thiết kế, dùng làm cơ sở để triển khai chi tiết migration EF Core và các API endpoint tương ứng.*

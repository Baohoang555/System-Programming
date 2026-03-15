# Inventory KPI Calculation System

Hệ thống tính toán KPI tồn kho theo thời gian thực từ dữ liệu hóa đơn (Invoice) và đơn nhập hàng (Purchase Order) định dạng JSON.

---

## Yêu cầu hệ thống

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows / Linux / macOS
- Dung lượng ổ đĩa trống tối thiểu: 500 MB

---

## Cấu trúc thư mục

```
InventoryKPISystem/
├── InventoryKpiSystem.Core/          # Models, Interfaces, Services (KpiCalculator, IncrementalKpiUpdater)
├── InventoryKpiSystem.Infrastructure/ # Logging, Persistence (FileTracker, KpiRegistry), Monitoring
├── InventoryKpiSystem.ConsoleApp/     # Giao diện console, Configuration, Coordinators
└── InventoryKPISystem.Tests/          # Unit tests & Integration tests
    ├── UnitTests/
    └── IntegrationTests/
```

---

## Hướng dẫn chạy chương trình

### Bước 1 — Clone hoặc tải source code

```powershell
git clone https://github.com/Baohoang555/System-Programming.git
cd InventoryKPISystem
```

### Bước 2 — Build toàn bộ solution

```powershell
cd InventoryKpiSystem.ConsoleApp
dotnet build
```

Kết quả mong đợi:
```
Build succeeded with 2 warning(s)
```

> Warning NU1510 về `System.Text.Json` là không ảnh hưởng, có thể bỏ qua.

### Bước 3 — Chạy ứng dụng

```powershell
dotnet run
```

Màn hình khởi động sẽ hiển thị:

```
╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║      INVENTORY KPI CALCULATION SYSTEM                     ║
║      Real-Time Processing & Analytics                     ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝
```


### Bước 4 — Sử dụng menu

```
┌─────────────────────────────────────────┐
│          MAIN MENU                      │
├─────────────────────────────────────────┤
│  1. View Current KPIs                   │
│  2. View Product KPIs                   │
│  3. View System Status                  │
│  4. Generate Report                     │
│  5. Configuration                       │
│  Q. Quit                                │
└─────────────────────────────────────────┘
```

| Phím | Chức năng |
|------|-----------|
| `1`  | Xem KPI tổng quan (Total SKUs, Stock Value, Out-of-Stock, Daily Sales, Inventory Age) |
| `2`  | Xem KPI theo từng sản phẩm |
| `3`  | Xem trạng thái hệ thống (uptime, memory, files processed) |
| `4`  | Xuất báo cáo JSON và CSV vào `data/reports/` |
| `5`  | Xem cấu hình hiện tại |
| `Q`  | Thoát chương trình (lưu báo cáo cuối trước khi thoát) |

---

## Định dạng file dữ liệu đầu vào

### Invoice (đặt vào `data/invoices/`)

```json
{
  "invoices": [
    {
      "invoiceId": "INV-001",
      "productId": "P001",
      "quantity": 80,
      "unitPrice": 70000,
      "date": "2025-02-01T00:00:00"
    }
  ]
}
```

### Purchase Order (đặt vào `data/purchase-orders/`)

```json
{
  "items": [
    {
      "orderId": "PO-001",
      "productId": "P001",
      "quantity": 100,
      "unitCost": 50000,
      "date": "2025-01-01T00:00:00"
    }
  ]
}
```

---

## Cấu hình (appsettings.json)

```json
{
  "invoiceDirectory": "data/invoices",
  "purchaseOrderDirectory": "data/purchase-orders",
  "reportsDirectory": "data/reports",
  "processedFilesDirectory": "data/processed-files",
  "logDirectory": "logs",
  "maxConcurrentFiles": 5,
  "retryAttempts": 3,
  "retryDelaySeconds": 2,
  "enableDetailedLogging": true,
  "enableConsoleOutput": true,
  "enableFileOutput": true,
  "reportCleanupDays": 30,
  "logCleanupDays": 30,
  "autoGenerateReports": true,
  "reportGenerationIntervalMinutes": 60
}
```

---

## Chạy Tests

```powershell
cd InventoryKPISystem.Tests
dotnet test
```

Kết quả mong đợi:

```
Test summary: total: 39, failed: 0, succeeded: 39, skipped: 0
Build succeeded with 2 warning(s)
```

### Danh sách test

| Test Class | Framework | Mô tả |
|---|---|---|
| `ValidationTests` | xUnit | Kiểm tra validate Invoice, PurchaseOrder |
| `ModelTests` | xUnit | Kiểm tra DataValidator với các edge case |
| `DataAccessTests` | xUnit | Kiểm tra JsonDataReader với file không tồn tại |
| `KpiLogicTests` | MSTest | Kiểm tra KpiCalculator, IncrementalKpiUpdater (15 test cases) |
| `FileMonitoringTests` | MSTest | Kiểm tra InventoryWatcher detect file JSON mới |
| `ConcurrentProcessingTests` | MSTest | Kiểm tra thread-safety của FileTracker, KpiRegistry |
| `PerformanceTests` | MSTest | Kiểm tra hiệu năng Logging, FileTracker, KpiRegistry |

---

## KPIs được tính toán

| KPI | Mô tả | Công thức |
|-----|-------|-----------|
| **Total SKUs** | Tổng số sản phẩm phân biệt | Số lượng ProductId duy nhất |
| **Stock Value** | Giá trị hàng tồn kho | `(Tổng mua - Tổng bán) × Giá vốn bình quân` |
| **Out-of-Stock** | Số sản phẩm hết hàng | Số SKU có `CurrentStock ≤ 0` |
| **Avg Daily Sales** | Doanh số bán bình quân ngày | `Tổng bán / Số ngày có giao dịch` |
| **Inventory Age** | Tuổi tồn kho bình quân (ngày) | `Hôm nay - Ngày mua sớm nhất` |

---

## Tắt chương trình

Nhấn `Q` từ menu chính để thoát gracefully — hệ thống sẽ tự động lưu báo cáo KPI cuối cùng trước khi thoát.

Hoặc nhấn `Ctrl+C` để thoát ngay lập tức.

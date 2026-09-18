# Kiến trúc LiBot

Dự án này được tổ chức theo mô hình Clean Architecture / Layered Architecture với flow:

Domain -> Application -> Infrastructure -> API

Mỗi tầng có trách nhiệm riêng rõ ràng, giúp tách business logic khỏi API, database và hệ thống giao tiếp robot.

## 1. LiBot.Domain

Tầng này chứa các khái niệm cốt lõi của nghiệp vụ:

- Entity
- AggregateRoot
- DomainEvent
- Enum
- Model nghiệp vụ như Robot, Mission, Workflow, Inventory, Library, Identity

Mục tiêu:
- Không phụ thuộc vào EF Core, ASP.NET Core, HTTP, SignalR hay database.
- Chứa logic nghiệp vụ cứng nhất của hệ thống.
- Định nghĩa các sự kiện domain và các aggregate root.

Ví dụ:
- `Robot` chứa trạng thái robot, vị trí, trạng thái hoạt động.
- `Mission` chứa workflow, mục tiêu, deadline, trạng thái.
- `InventoryItem` chứa quantity, status, metadata.

### Mẫu code gợi ý

```csharp
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

## 2. LiBot.Application

Tầng này chứa use cases / business flows của ứng dụng.

Thư mục gợi ý:

- `Identity/Commands`
- `Identity/Queries`
- `Library/Commands`
- `Library/Queries`
- `Robot/Commands`
- `Robot/Queries`
- `Mission/Commands`
- `Mission/Queries`
- `Mission/Events`
- `Workflow/Commands`
- `Workflow/Queries`
- `Workflow/Engine`
- `Inventory/Commands`
- `Inventory/Queries`
- `Inventory/Events`

Mỗi use case thường có 3 phần:

- Command / Query: đầu vào của nghiệp vụ
- Handler: xử lý nghiệp vụ
- Result / DTO: đầu ra

Ví dụ:
- `CreateMissionCommand`
- `CreateMissionCommandHandler`
- `GetRobotByIdQuery`
- `GetRobotByIdQueryHandler`

Trách nhiệm:
- Orchestrate workflow
- Validate business rules
- Gọi repository hoặc service
- Trả về dữ liệu dạng DTO / result

Không nên:
- Không chứa logic database trực tiếp
- Không gọi HTTP/SignalR trực tiếp
- Không chứa code cụ thể cho EF Core hoặc MQTT

## 3. LiBot.Contracts

Tầng này định nghĩa contract dữ liệu chia sẻ giữa các layer.

Nó bao gồm:
- Request DTOs
- Response DTOs
- Command / Query contracts
- Event payloads

Thường dùng cho:
- API nhận dữ liệu từ client
- ứng dụng trả dữ liệu cho API
- giao tiếp giữa các module hoặc service

Ví dụ:
- `CreateMissionRequest`
- `MissionResponse`
- `RobotStatusDto`
- `WorkflowStepResult`

## 4. LiBot.Infrastructure

Tầng này triển khai chi tiết kỹ thuật của hệ thống.

Phân công:

- `Persistence/`: DbContext, migrations, entity configs
- `Repositories/`: repository implementation
- `Messaging/Mqtt/`: MQTT client, subscribers, publishers
- `Messaging/Events/`: event handlers hoặc schema cho message broker
- `Robot/`: service giao tiếp với robot hoặc thiết bị
- `BackgroundJobs/`: worker chạy định kỳ, watchdog, monitor job

Ví dụ:
- `RobotCommunicationService`: gửi lệnh đến robot, theo dõi trạng thái
- `MissionDispatcher`: phân công nhiệm vụ cho robot
- `RobotHeartbeatMonitor`: kiểm tra robot còn sống hay không
- `MissionTimeoutMonitor`: phát hiện mission quá hạn

Trách nhiệm:
- Kết nối dữ liệu / storage
- Kết nối message bus / MQTT
- Implement repository
- Chạy background task

## 5. LiBot.Api

Tầng API là entry point của hệ thống.

Thư mục gợi ý:

- `Controllers/`: REST API endpoints
- `Hubs/`: realtime hub cho SignalR
- `Middleware/`: exception handling, logging, auth filters, tracing
- `Program.cs`: đăng ký services và cấu hình pipeline

Ví dụ:
- `MissionController`
- `RobotController`
- `InventoryController`
- `RobotHub`
- `ExceptionMiddleware`

Đây là nơi:
- nhận HTTP request
- validate request
- map request -> command/query
- gọi Application layer
- trả response JSON hoặc SignalR event

## 6. Luồng xử lý điển hình

Ví dụ khi client tạo nhiệm vụ cho robot:

1. Client gọi `POST /api/missions`
2. `MissionController` nhận request
3. Controller map sang `CreateMissionCommand`
4. `CreateMissionCommandHandler` validate nghiệp vụ
5. Handler gọi repository hoặc service từ Infrastructure
6. Domain model tạo `Mission` và `DomainEvent`
7. Hệ thống lưu dữ liệu và trả về response

## 7. Quy tắc thiết kế nên tuân theo

- Domain không phụ thuộc vào API
- Application không phụ thuộc vào database trực tiếp
- Infrastructure implement interface của Application
- API chỉ gọi Application và trả response
- Contracts dùng để định nghĩa DTO rõ ràng
- Domain event dùng để phản ứng khi business state thay đổi

## 8. Gợi ý phát triển từng module

### Identity
- đăng nhập, đăng ký, phân quyền, refresh token

### Library
- quản lý tài liệu, sách, mẫu workflow, template

### Robot
- trạng thái robot, command execution, telemetry, ping/heartbeat

### Mission
- lifecycle của nhiệm vụ: created -> assigned -> running -> completed -> failed

### Workflow
- engine thực thi các bước nhiệm vụ, điều kiện, retry, timeout

### Inventory
- tracking vật tư, phụ tùng, nguyên liệu, hàng tồn kho

## 9. Khuyến nghị code style

- Dùng namespace theo tên project: `LiBot.Domain.*`, `LiBot.Application.*`, `LiBot.Infrastructure.*`, `LiBot.Api.*`
- Dùng `record` cho DTO / request / result nếu cần
- Dùng `interface` cho repository / service contract
- Dùng `Mediator` hoặc `MediatR` nếu muốn tách handler rõ hơn
- Tách service thành interface + implementation để dễ mock test

## 10. Bắt đầu code theo từng tầng

- `LiBot.Domain`: định nghĩa aggregate và business rules căn bản
- `LiBot.Application`: thêm command/query handlers cho từng module
- `LiBot.Infrastructure`: triển khai repository, MQTT, storage
- `LiBot.Contracts`: định nghĩa DTOs và response models
- `LiBot.Api`: map các endpoint và SignalR hub

Đây là nền tảng tốt để mở rộng theo các module robot, mission, workflow và inventory trong tương lai.

# Контракты взаимодействия


# 9. Основные DTO проекта

Для передачи данных между слоями и модулями создаются DTO.

## 9.1. ClassroomDto

```csharp
public record ClassroomDto(
    Guid Id,
    string Name,
    string Building,
    int Capacity,
    string Type,
    string Status
);
```

---

## 9.2. CreateBookingRequest

```csharp
public record CreateBookingRequest(
    Guid UserId,
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime,
    string Purpose
);
```

---

## 9.3. BookingDto

```csharp
public record BookingDto(
    Guid Id,
    Guid UserId,
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime,
    string Purpose,
    string Status
);
```

---

## 9.4. CheckAvailabilityRequest

```csharp
public record CheckAvailabilityRequest(
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime
);
```

---

## 9.5. CheckAvailabilityResult

```csharp
public record CheckAvailabilityResult(
    bool IsAvailable,
    string? ErrorCode
);
```

---

## 9.6. ApproveBookingRequest

```csharp
public record ApproveBookingRequest(
    Guid BookingId,
    Guid AdminId
);
```

---

## 9.7. ReserveSlotRequest

```csharp
public record ReserveSlotRequest(
    Guid BookingId,
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime
);
```

---

## 9.8. ScheduleSlotDto

```csharp
public record ScheduleSlotDto(
    Guid Id,
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime,
    string Status
);
```

---

## 9.9. UserDto

```csharp
public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role
);
```

## 9.10. ReleaseSlotRequest

```csharp
public record ReleaseSlotRequest(
    Guid BookingId,
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime
);
```

## 9.11. ReleaseSlotResult

```csharp
public record ReleaseSlotResult(
    bool Success,
    string? ErrorCode
);
```

---

## 10. Описание API

Клиентское приложение взаимодействует с системой через HTTP API.

Предполагаются следующие основные точки входа.

| Метод | Адрес | Назначение |
|---|---|---|
| `GET` | `/api/classrooms` | Получение списка аудиторий |
| `GET` | `/api/classrooms/{id}` | Получение информации об аудитории |
| `GET` | `/api/classrooms/{id}/schedule` | Получение расписания аудитории |
| `POST` | `/api/bookings` | Создание заявки на бронирование |
| `GET` | `/api/bookings/{id}` | Получение заявки |
| `GET` | `/api/bookings/user/{userId}` | Получение заявок пользователя |
| `POST` | `/api/bookings/{id}/approve` | Подтверждение заявки (админ) |
| `POST` | `/api/bookings/{id}/reject` | Отклонение заявки (админ) |
| `POST` | `/api/bookings/{id}/cancel` | Отмена бронирования |
| `GET` | `/api/users/{id}` | Получение данных пользователя |

Контракт API на последующих этапах может быть дополнительно описан средствами OpenAPI.

# 11. Протокол взаимодействия № 1  
## Проверка доступности аудитории и предотвращение конфликтов расписания

### 11.1. Инициатор и получатель

**Инициатор:**

```text
BookingModule
```

**Получатель:**

```text
ScheduleModule
```

---

### 11.2. Назначение

Протокол используется при создании заявки на бронирование. Перед сохранением заявки необходимо убедиться, что выбранная аудитория свободна на указанный временной интервал и конфликт расписания отсутствует.

---

### 11.3. Механизм вызова

Асинхронный вызов через интерфейс C#:

```csharp
IScheduleConflictService
```

Зависимость передается через механизм Dependency Injection.

---

### 11.4. Точка входа

```csharp
Task<CheckAvailabilityResult> CheckAvailabilityAsync(
    CheckAvailabilityRequest request,
    CancellationToken cancellationToken);
```

---

### 11.5. Формат запроса

```json
{
  "classroomId": "a3f1c8e2-5b7d-4e9a-8f2c-1d6e4a9b0c3f",
  "startTime": "2026-10-01T09:00:00",
  "endTime": "2026-10-01T10:30:00"
}
```

---

### 11.6. Успешный ответ

```json
{
  "isAvailable": true,
  "errorCode": null
}
```

---

### 11.7. Ошибки

Возможные ошибки:

| Код | Описание |
|---|---|
| `CLASSROOM_NOT_FOUND` | Аудитория не существует |
| `SCHEDULE_CONFLICT` | Аудитория занята на выбранный интервал |
| `INVALID_TIME_INTERVAL` | Некорректный временной интервал (конец раньше начала) |
| `CLASSROOM_UNAVAILABLE` | Аудитория недоступна (ремонт, обслуживание) |

Пример:

```json
{
  "isAvailable": false,
  "errorCode": "SCHEDULE_CONFLICT"
}
```

---

### 11.8. Таймаут

Операция должна завершиться не более чем за:

```text
3 секунды
```

При отмене запроса используется `CancellationToken`.

---

### 11.9. Повторная отправка

Автоматический повтор операции проверки не выполняется.
Причина: повторный вызов может вернуть иной результат, если за время между вызовами расписание изменилось. 

Повтор выполняется только при явной необходимости с повторной проверкой результата.

---

### 11.10. Аутентификация

Дополнительная аутентификация между модулями не выполняется, поскольку они работают внутри одного серверного приложения.

Проверка прав пользователя выполняется на уровне API.

---

### 11.11. Версия контракта

```text
ScheduleConflictContract v1
```

---

### 11.12. Пример обмена

```text
BookingModule
       │
       │ CheckAvailabilityAsync(...)
       ▼
 ScheduleModule
       │
       │ Проверка существования аудитории
       │
       │ Проверка пересечения интервалов
       │
       │ Проверка состояния аудитории
       ▼
 CheckAvailabilityResult
       │
       ▼
 BookingModule
```

При отсутствии конфликта `BookingModule` продолжает создание заявки.

При ошибке `SCHEDULE_CONFLICT` заявка не создаётся, пользователю возвращается информация о конфликте.

---

# 12. Протокол взаимодействия № 2  
## Подтверждение заявки на бронирование администратором

### 12.1. Инициатор и получатель

**Инициатор:**

```text
BookingModule
```

**Получатель:**

```text
ScheduleModule
```

---

### 12.2. Назначение

После того как администратор подтверждает заявку на бронирование, необходимо зарезервировать временной слот в расписании аудитории, чтобы предотвратить бронирование этого же интервала другими пользователями.

---

### 12.3. Механизм вызова

Асинхронный вызов через интерфейс:

```csharp
IScheduleReservationService
```

---

### 12.4. Точка входа

```csharp
Task<ScheduleSlotDto> ReserveSlotAsync(
    ReserveSlotRequest request,
    CancellationToken cancellationToken);
```

---

### 12.5. Формат запроса

```json
{
  "bookingId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "classroomId": "a3f1c8e2-5b7d-4e9a-8f2c-1d6e4a9b0c3f",
  "startTime": "2026-10-01T09:00:00",
  "endTime": "2026-10-01T10:30:00"
}
```

---

### 12.6. Успешный ответ

```json
{
  "id": "d290f1ee-6c54-4b01-90e6-d701748f0851",
  "classroomId": "a3f1c8e2-5b7d-4e9a-8f2c-1d6e4a9b0c3f",
  "startTime": "2026-10-01T09:00:00",
  "endTime": "2026-10-01T10:30:00",
  "status": "Booked"
}
```

---

### 12.7. Ошибки

| Код | Описание |
|---|---|
| `BOOKING_NOT_FOUND` | Заявка не существует |
| `SLOT_ALREADY_RESERVED` | Слот уже зарезервирован другой заявкой |
| `INVALID_TIME_INTERVAL` | Некорректный временной интервал |
| `CLASSROOM_NOT_FOUND` | Аудитория не найдена |

Пример ошибки:

```json
{
  "errorCode": "SLOT_ALREADY_RESERVED"
}
```

---

### 12.8. Таймаут

Максимальное ожидаемое время выполнения:

```text
3 секунды
```

---

### 12.9. Повторная отправка

Повторный запрос допускается только после проверки того, что слот для данной заявки ещё не зарезервирован. 

`BookingId` используется для предотвращения создания нескольких резервирований по одной заявке.

---

### 12.10. Аутентификация

Внутренняя аутентификация между модулями не требуется. 

Доступ к операции подтверждения заявки предварительно проверяется на уровне API (проверка роли Admin).

---

### 12.11. Версия контракта

```text
ScheduleReservationContract v1
```

---

### 12.12. Пример обмена

```text
BookingModule
      │
      │ ReserveSlotAsync(...)
      ▼
ScheduleModule
      │
      │ Проверка данных
      │
      │ Проверка доступности слота
      │
      │ Создание записи в расписании
      ▼
ScheduleSlotDto
      │
      ▼
BookingModule
```

---

## 13. Протокол взаимодействия № 3
Освобождение временного слота при отмене бронирования

### 13.1. Инициатор и получатель

Инициатор:

BookingModule

Получатель:

ScheduleModule

### 13.2. Назначение

Протокол используется при отмене заявки пользователем до подтверждения и при отмене подтверждённого бронирования администратором. Необходимо освободить временной слот аудитории, чтобы интервал снова стал доступен для бронирования.

### 13.3. Механизм вызова

Асинхронный вызов через интерфейс C#:

`IScheduleReleaseService`

Зависимость передаётся через механизм Dependency Injection.

### 13.4. Точка входа

```csharp
Task<ReleaseSlotResult> ReleaseSlotAsync(
    ReleaseSlotRequest request,
    CancellationToken cancellationToken);
```

### 13.5. Формат запроса

```json
{
  "bookingId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "classroomId": "a3f1c8e2-5b7d-4e9a-8f2c-1d6e4a9b0c3f",
  "startTime": "2026-10-01T09:00:00",
  "endTime": "2026-10-01T10:30:00"
}
```

### 13.6. Успешный ответ

```json
{
  "success": true,
  "errorCode": null
}
```

### 13.7. Ошибки

| Код | Описание |
| --- | --- |
| BOOKING_NOT_FOUND | Заявка не существует |
| SLOT_NOT_RESERVED | Слот не зарезервирован для данной заявки |
| INVALID_TIME_INTERVAL | Некорректный временной интервал |
| CLASSROOM_NOT_FOUND | Аудитория не найдена |

Пример:

```json
{
  "success": false,
  "errorCode": "SLOT_NOT_RESERVED"
}
```

### 13.8. Таймаут

Операция должна завершиться не более чем за:

3 секунды

При отмене запроса используется `CancellationToken`.

### 13.9. Повторная отправка

Операция идемпотентна по `BookingId`.

Повторный вызов после успешного освобождения слота возвращает:

```json
{
  "success": true,
  "errorCode": null
}
```

Автоматический повтор не требуется при ошибке `SLOT_NOT_RESERVED`, если заявка уже отменена ранее.

### 13.10. Аутентификация

Дополнительная аутентификация между модулями не выполняется, поскольку они работают внутри одного серверного приложения.

Проверка прав пользователя выполняется на уровне API:
- владелец заявки может отменить свою заявку до подтверждения;
- администратор может отменить подтверждённое бронирование.

### 13.11. Версия контракта

ScheduleReleaseContract v1

### 13.12. Пример обмена

```text
BookingModule
       │
       │ ReleaseSlotAsync(...)
       ▼
 ScheduleModule
       │
       │ Проверка существования заявки
       │
       │ Проверка зарезервированного слота
       │
       │ Освобождение временного интервала
       ▼
 ReleaseSlotResult
       │
       ▼
 BookingModule
```

При успешном освобождении слота `BookingModule` устанавливает статус заявки `Cancelled`.

При ошибке `SLOT_NOT_RESERVED` модуль проверяет, не была ли заявка уже отменена ранее. Если слот не принадлежит данной заявке, операция завершается ошибкой.

## 14. Интерфейсы служб контрактов

### 14.1. Контракт проверки доступности расписания

```csharp
public interface IScheduleConflictService
{
    Task<CheckAvailabilityResult> CheckAvailabilityAsync(
        CheckAvailabilityRequest request,
        CancellationToken cancellationToken = default);
}
```

### 14.2. Контракт резервирования временного слота

```csharp
public interface IScheduleReservationService
{
    Task<ScheduleSlotDto> ReserveSlotAsync(
        ReserveSlotRequest request,
        CancellationToken cancellationToken = default);
}
```

### 14.3. Контракт освобождения временного слота

```csharp
public interface IScheduleReleaseService
{
    Task<ReleaseSlotResult> ReleaseSlotAsync(
        ReleaseSlotRequest request,
        CancellationToken cancellationToken = default);
}
```

### 14.4. Контракт модуля бронирования

```csharp
public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<BookingDto> ApproveBookingAsync(
        ApproveBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<BookingDto> CancelBookingAsync(
        Guid bookingId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
```

### 14.5. Исключение контракта

```csharp
public sealed class ContractOperationException : Exception
{
    public string ErrorCode { get; }

    public ContractOperationException(string errorCode)
        : base(errorCode)
    {
        ErrorCode = errorCode;
    }

    public ContractOperationException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```
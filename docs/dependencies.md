# Перечень зависимостей

## Внутренние зависимости

```mermaid
graph TD
    Api[ClassroomBooking.Api]
    Bookings[ClassroomBooking.Bookings]
    Classrooms[ClassroomBooking.Classrooms]
    Schedule[ClassroomBooking.Schedule]
    Users[ClassroomBooking.Users]
    Contracts[ClassroomBooking.Contracts]

    Api --> Bookings
    Api --> Classrooms
    Api --> Schedule
    Api --> Users
    Api --> Contracts

    Bookings --> Contracts
    Classrooms --> Contracts
    Schedule --> Contracts
    Users --> Contracts
```

Бизнес-модули не обращаются напрямую к внутренним классам друг друга.  
Взаимодействие выполняется через публичные контракты.

Например:

```text
BookingModule → IScheduleConflictService → ScheduleModule
```

Это позволяет уменьшить связанность модулей.

## Внешние зависимости

| Зависимость | Назначение |
|-------------|------------|
| .NET 9 | Платформа выполнения |
| ASP.NET Core | Разработка серверного API |
| Entity Framework Core | Доступ к данным |
| SQLite | Хранение данных в учебной версии проекта |
| xUnit | Модульное и интеграционное тестирование |
| Git | Контроль версий |

Конкретные версии библиотек фиксируются в файлах проектов `.csproj`.
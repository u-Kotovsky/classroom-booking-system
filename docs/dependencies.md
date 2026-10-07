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

    Bookings --> Contracts
    Classrooms --> Contracts
    Schedule --> Contracts
    Users --> Contracts
```

## Внешние зависимости

| Зависимость | Назначение |
| --- | --- |
| .NET 9 | Платформа выполнения |
| ASP.NET Core | Разработка серверного API |
| Entity Framework Core | Доступ к данным |
| SQLite | Хранение данных в учебной версии проекта |
| xUnit | Модульное тестирование |
| Git | Контроль версий |
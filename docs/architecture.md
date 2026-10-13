# Архитектурная схема

Для проекта выбрана модульная архитектура.

```mermaid
flowchart TD
    Client["Клиент (браузер / UI)"]

    subgraph Server["Серверное приложение (ASP.NET Core)"]
        ApiHost["ApiHost — HTTP API"]

        subgraph Modules["Функциональные модули"]
            Booking["BookingModule"]
            Classroom["ClassroomModule"]
            Schedule["ScheduleModule"]
            User["UserModule"]
        end
    end

    Client -->|HTTP| ApiHost
    ApiHost --> Booking
    ApiHost --> Classroom
    ApiHost --> User

    Booking -->|GetClassroomAsync| Classroom
    Booking -->|CheckAvailabilityAsync /<br/>ReserveSlotAsync /<br/>ReleaseSlotAsync| Schedule
    Booking -->|GetUserAsync| User
```

Центральным модулем бизнес-процесса является `BookingModule`.

При создании заявки он:

1. получает данные пользователя;
2. проверяет существование и состояние аудитории;
3. проверяет конфликт расписания;
4. создаёт заявку;
5. передаёт заявку администратору на принятие.
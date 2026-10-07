# Бронирование учебных аудиторий

Учебный курсовой проект по разработке информационной системы для бронирования учебных аудиторий.

Проект выполнен с использованием стека **C# / .NET 9** и системы контроля версий **Git**.

---

## Технологии

- .NET 9 SDK
- C#
- Git
- Markdown
- dotnet CLI

---

### Требования

Для работы с проектом необходимо установить следующее программное обеспечение:

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet)
- [Visual Studio](https://visualstudio.microsoft.com/downloads/) (версии 2022 или новее)
- Git

---

### Клонирование репозитория

После установки всех требований необходимо клонировать локальную копию репозитория:

```bash
git clone https://github.com/u-Kotovsky/classroom-booking-system.git
```

### Сборка

Для восстановления зависимостей и сборки проекта выполните команды:

```bash
dotnet restore
dotnet build
```

Успешная сборка означает, что все проекты решения корректно скомпилированы и зависимости восстановлены.

---

### Тесты

Для запуска тестов выполните:

```bash
dotnet restore
dotnet test
```

Тесты позволяют проверить корректность работы модулей проекта и основные сценарии использования системы.

### Запуск

Для запуска приложения выполните команду:

```bash
dotnet restore
dotnet run --project src/ClassroomBooking.Api
```

### Структура репозитория

```
classroom-booking-system/
├── src/
│   ├── ClassroomBooking.Api/
│   ├── ClassroomBooking.Classrooms/
│   ├── ClassroomBooking.Bookings/
│   ├── ClassroomBooking.Schedule/
│   ├── ClassroomBooking.Users/
│   └── ClassroomBooking.Contracts/
├── tests/
│   ├── ClassroomBooking.UnitTests/
│   └── ClassroomBooking.IntegrationTests/
├── docs/
│   ├── project-scope.md
│   ├── modules.md
│   ├── architecture.md
│   ├── dependencies.md
│   ├── contracts.md
│   ├── testing.md
│   ├── version-control.md
│   └── decisions/
│       ├── ADR-001-modular-architecture.md
│       └── ADR-002-module-contracts.md
├── .gitignore
├── .editorconfig
├── README.md
└── ClassroomBooking.sln
```
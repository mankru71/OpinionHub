# OpinionHub (MVP, .NET 8)

OpinionHub — веб-приложение для создания и проведения опросов с авторизацией, экспортом результатов и автоматическим жизненным циклом (завершение/архивация).

## Что реализовано в MVP
- Регистрация/вход через ASP.NET Core Identity.
- Подтверждение email (через `ConsoleEmailSender`, ссылка логируется в консоль).
- Создание опроса авторизованным пользователем.
- Режимы: `Draft`, `Active`, `Completed`, `Archived`.
- Типы: одиночный/множественный выбор.
- Видимость: публичная/анонимная (в анонимном режиме `UserId` в голосе не сохраняется).
- Один голос на аккаунт на опрос + опциональное изменение голоса.
- Лента опросов с приоритетом активных.
- Реальное время: обновление страницы деталей через SignalR при новом голосе.
- Экспорт результатов автора в CSV и XLSX.
- Автозавершение по дедлайну и автоархивация через фоновый hosted service.
- Аудит-логи ключевых действий.

## Технологии
- ASP.NET Core MVC (.NET 8)
- Entity Framework Core
- PostgreSQL (Npgsql)
- SignalR
- Chart.js
- ClosedXML

## Быстрый запуск
1. Установите .NET SDK 8 и PostgreSQL 14+.
2. Создайте БД, например `opinionhub`.
3. Измените строку подключения в `OpinionHub.Web/appsettings.json`.
4. Примените миграции:
   ```bash
   dotnet ef database update --project OpinionHub.Web
   ```
5. Запустите приложение:
   ```bash
   dotnet run --project OpinionHub.Web
   ```
6. Откройте URL из `launchSettings.json`.

## Деплой
- Подходит для VPS/контейнеров/Cloud Run/Azure App Service.
- Для production:
  - используйте внешнюю БД PostgreSQL,
  - настройте реальный SMTP-провайдер вместо `ConsoleEmailSender`,
  - включите HTTPS и переменные окружения для секретов.

## Тесты
```bash
dotnet test OpinionHub.Tests
```

## Ограничения текущего MVP
- Нет полноценной панели администратора.
- График динамики голосов по времени и email-уведомления о скором завершении пока не реализованы.
- Требуется добавить EF migrations (`dotnet ef migrations add InitialCreate`) в среде с установленным SDK.

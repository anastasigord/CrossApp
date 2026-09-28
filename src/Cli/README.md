# CrossApp

Наскрізний проєкт з крос-платформного програмування.

## Предметна область

Замовлення.

Сутності:
- Customer — клієнт
- Product — товар
- Order — замовлення
- OrderLine — рядок замовлення

Призначення: оформлення замовлень і підрахунок сум.

## Додаткове завдання
Порівняння розмірів self-contained публікації:
win-x64: 76,66 MB
linux-x64: 78,79 MB

## Структура solution

```
CrossApp/
├── CrossApp.slnx
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   └── EnvironmentInfo.cs
    └── Cli/
        ├── Cli.csproj      (ProjectReference на Core)
        └── Program.cs
```

Залежність одностороння: Cli → Core.

Домовленість про каталоги в Core на семестр:
- `Core/Dto/` — record-типи формату даних (тиждень 3)
- `Core/Domain/` — сутності з поведінкою та інваріантами (тиждень 4)
- `Core/Storage/` — реалізації сховищ (тиждень 5)

## Команди

Build:
```
dotnet build
dotnet build src/Core/Core.csproj
```

Run:
```
dotnet run --project src/Cli
```

Publish:
```
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r win-x64 --self-contained false
```

## Порівняння режимів публікації

| RID | Режим | Розмір publish | Потрібен runtime |
|---|---|---|---|
| win-x64 | self-contained | 76,68 МБ | ні |
| win-x64 | framework-dependent | 0,19 МБ | так (.NET 10) |

## Self-contained vs framework-dependent

Self-contained публікація містить код застосунку, залежності та копію .NET Runtime для конкретного RID, тому працює без встановленого .NET, але займає значно більше місця. Framework-dependent публікація містить лише код і залежності, тому невелика, але на машині користувача має бути встановлений сумісний .NET 10.
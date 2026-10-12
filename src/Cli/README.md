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

## Лабораторна робота 4: доменна модель Order / OrderLine

Сутності `Order` і `OrderLine` розміщені в `src/Core/Domain`. Record-типи з лаб. 3
(`src/Core/Dto`) збережені як DTO: вони лише переносять дані, а бізнес-правила
захищають сутності. Зв'язок між ними: `ToDto()` і `FromDto()`.

### Інваріанти замовлення

1. Ідентифікатор замовлення не порожній: `ArgumentException` (`Order.Create`).
2. Ідентифікатор клієнта не порожній: `ArgumentException` (`Order.Create`).
3. Ідентифікатор товару і назва в рядку не порожні: `ArgumentException` (`OrderLine.Create`).
4. Кількість у рядку більша за нуль: `ArgumentOutOfRangeException` (`OrderLine.Create`).
5. Ціна не від'ємна: `ArgumentOutOfRangeException` (`OrderLine.Create`).
6. До підтвердженого замовлення не можна додавати рядки: `InvalidOperationException` (`Order.AddLine`).
7. Порожнє замовлення не можна підтвердити: `InvalidOperationException` (`Order.Confirm`).
8. Підтверджене замовлення не можна підтвердити повторно: `InvalidOperationException` (`Order.Confirm`).
9. Загальна сума обчислюється з рядків і не може бути змінена ззовні (`Order.Total`).
10. Колекція рядків назовні доступна лише для читання (`IReadOnlyList<OrderLine>`).
11. `FromDto` відновлює сутність через `Create`, `AddLine` і `Confirm`, тобто з тими самими перевірками.
12. Товар з імпорту, що порушує інваріанти, не потрапляє в замовлення, а додається до переліку помилок (`OrderImportMapper.ToOrder`).

### Запуск

```
dotnet run --project src/Cli
```

Програма показує три сценарії: успішне створення й підтвердження замовлення, порушення
інваріантів (з виводом `Message` без stack trace) та імпорт товарів у замовлення.


## Лабораторна робота 6: доменні події, делегати, лямбди

`OrderService` піднімає подію `Changed` (`EventHandler<OrderChangedEventArgs>`) після успішних
`CreateOrder`, `AddLine`, `ConfirmOrder`. Подію піднімає один приватний метод `OnChanged`
(`Changed?.Invoke(...)`) лише після `Add`/`Update` у сховищі: якщо інваріант кинув виняток,
події немає. Дані події: `OrderId`, `Kind` (`OrderChangeKind`), `LinesCount`, `Total`, `At`.

Core не містить `Console.WriteLine` і не працює з файлом логу. У Cli два підписники:
локальний метод `OnOrderChanged` (консоль + `logs/app.log`, його можна зняти через `-=`)
і лямбда-лічильник `(_, _) => changes++` із замиканням. Лог:
`Path.Combine(AppContext.BaseDirectory, "logs", "app.log")`, каталог створюється кодом.
`OrderService.FindAll(Func<Order, bool>)` повертає замовлення за умовою.

Запуск:
```
dotnet run --project src/Cli
dotnet run --project src/Cli -- --file
```
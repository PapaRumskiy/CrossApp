# CrossApp

Наскрізний навчальний проєкт із кросплатформного програмування, розроблений мовою C# на платформі .NET.

**Предметна область:** Замовлення.

**Основні сутності:** Customer, Product, Order, OrderLine.

**Призначення:** представлення та імпорт даних про товари, створення замовлень, керування їхнім станом, перевірка бізнес-правил і підрахунок загальної вартості.

Проєкт побудований із розділенням доменної логіки та консольного інтерфейсу.

## Технології

- C#
- .NET 8 / .NET 10
- Visual Studio Code
- System.Text.Json
- CSV / JSON
- Git
- Docker

## Структура проєкту

```text
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
├── data/
│   ├── sample.csv
│   ├── sample.json
│   └── sample-mixed.csv
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   ├── EnvironmentInfo.cs
    │   ├── Dto/
    │   │   ├── ProductDto.cs
    │   │   ├── WarehouseDto.cs
    │   │   ├── ImportResult.cs
    │   │   ├── OrderDto.cs
    │   │   └── OrderLineDto.cs
    │   ├── Domain/
    │   │   ├── Order.cs
    │   │   ├── OrderLine.cs
    │   │   └── OrderStatus.cs
    │   └── Import/
    │       ├── ProductCsvImporter.cs
    │       ├── ProductJsonImporter.cs
    │       └── MixedCsvImporter.cs
    └── Cli/
        ├── Cli.csproj
        ├── Program.cs
        └── Lab04Demo.cs
```

### Core

Бібліотека, яка містить основну логіку програми:

- DTO для передавання даних.
- Доменні сутності та бізнес-правила.
- Імпортери CSV/JSON.
- Перевірку коректності даних.
- Інформацію про середовище виконання.

### Cli

Консольний застосунок, який забезпечує:

- Отримання аргументів командного рядка.
- Вибір формату імпорту.
- Виведення завантажених даних.
- Виведення помилок і статистики.
- Демонстрацію роботи доменної моделі.

Залежність між проєктами:

```text
Cli → Core
```

Для підключення Core використовується `ProjectReference`.

## Multi-targeting

Бібліотека Core підтримує дві версії .NET:

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

Консольний застосунок Cli використовує .NET 10:

```xml
<TargetFramework>net10.0</TargetFramework>
```

Перевірка:

```powershell
dotnet build
dotnet build -c Release
```

## Представлення даних

Для опису даних використовуються record-типи.

### ProductDto

Представляє товар:

- `Id` — ідентифікатор.
- `Name` — назва.
- `Price` — ціна.

### WarehouseDto

Представляє склад:

- `Id` — ідентифікатор.
- `Name` — назва.

### ImportResult

Узагальнений тип `ImportResult<T>`, що містить:

- `Items` — успішно імпортовані записи.
- `Errors` — повідомлення про некоректні записи.

Це дозволяє продовжувати обробку файлу навіть за наявності пошкоджених рядків.

## Імпорт CSV та JSON

Реалізовано імпорт даних із файлів CSV та JSON.

### CSV

Використовується роздільник `;`.

Приклад:

```csv
id;name;price
P-001;Laptop;35000.50
P-002;Keyboard;2500.00
P-003;Mouse;800.99
```

Для аналізу рядків застосовуються:

- Pattern matching.
- Switch expressions.
- Property patterns.
- List patterns.
- Guard conditions (`when`).
- `decimal.TryParse`.
- `CultureInfo.InvariantCulture`.

Некоректні записи пропускаються із зазначенням номера рядка та причини помилки.

### JSON

Імпорт реалізований за допомогою `System.Text.Json`.

Для десеріалізації використовується `JsonSerializer.Deserialize`.

Параметр `PropertyNameCaseInsensitive` дозволяє зіставляти назви JSON-властивостей незалежно від регістру.

### Змішаний CSV

Підтримується імпорт різних типів записів:

- `P` — Product.
- `W` — Warehouse.

Приклад:

```csv
type;id;name;price
P;P-201;Laptop;35000.50
W;W-001;Main Warehouse;
P;P-202;Keyboard;2500.00
```

Тип запису визначається за допомогою pattern matching.

### Статистика імпорту

Після завантаження виводиться:

- Загальна кількість записів.
- Кількість прийнятих записів.
- Кількість пропущених записів.
- Відсоток помилок.
- Причини пропуску некоректних рядків.

## Доменна модель

Для реалізації бізнес-правил створено каталог `Core/Domain`.

### Order

Головна сутність, яка представляє замовлення.

Властивості:

- `Id` — ідентифікатор замовлення.
- `CustomerId` — ідентифікатор клієнта.
- `Status` — поточний стан.
- `Lines` — список позицій.
- `Total` — загальна вартість.

Основні методи:

- `Create()` — створення замовлення.
- `AddLine()` — додавання товару.
- `Confirm()` — підтвердження замовлення.
- `Cancel()` — скасування замовлення.
- `ToDto()` — перетворення в DTO.
- `FromDto()` — відновлення з DTO.

### OrderLine

Представляє окрему позицію замовлення.

Властивості:

- `ProductId` — ідентифікатор товару.
- `Name` — назва.
- `Price` — ціна за одиницю.
- `Quantity` — кількість.
- `Total` — загальна вартість позиції.

## Інваріанти

Доменна модель забезпечує дотримання бізнес-правил протягом усього життєвого циклу об'єктів.

| Інваріант | Виняток |
|---|---|
| Id замовлення обов'язковий | ArgumentException |
| CustomerId обов'язковий | ArgumentException |
| ProductId обов'язковий | ArgumentException |
| Назва товару обов'язкова | ArgumentException |
| Ціна товару не може бути від'ємною | ArgumentOutOfRangeException |
| Кількість товару повинна бути більшою за нуль | ArgumentOutOfRangeException |
| Порожнє замовлення не можна підтвердити | InvalidOperationException |
| Не можна додавати позиції до підтвердженого замовлення | InvalidOperationException |
| Не можна повторно підтверджувати замовлення | InvalidOperationException |
| Підтверджене замовлення не можна скасувати | InvalidOperationException |
| Повторне скасування заборонене | InvalidOperationException |

Усі перевірки виконуються перед зміною стану об'єкта.

Якщо операція порушує інваріант, генерується відповідний виняток зі зрозумілим повідомленням.

## Інкапсуляція

У доменній моделі використовується інкапсуляція:

- Критичні властивості не мають публічних сеттерів.
- Конструктори сутностей приватні.
- Створення виконується через фабричні методи `Create()`.
- Внутрішня колекція позицій захищена від зовнішніх змін.
- Стан замовлення змінюється лише через бізнес-методи.

Це запобігає створенню та зміні об'єктів із порушенням бізнес-правил.

## Статуси замовлення

Використовується перелічення `OrderStatus`:

```csharp
public enum OrderStatus
{
    Draft,
    Confirmed,
    Cancelled
}
```

Можливі переходи:

```text
Draft → Confirmed
Draft → Cancelled
```

Підтверджене або скасоване замовлення не можна повторно змінити через реалізовані методи переходів стану.

Для перевірки переходів використовується `switch expression`.

## DTO та доменні сутності

DTO використовуються для передавання й зберігання даних.

Доменні сутності відповідають за бізнес-логіку, інкапсуляцію та контроль стану.

Метод `ToDto()` перетворює сутність у DTO.

Метод `FromDto()` відновлює сутність, повторно перевіряючи інваріанти.

Цей механізм підготовлений для подальшої реалізації файлового сховища.

## Запуск

Усі команди виконуються з кореневого каталогу проєкту.

### Компіляція

```powershell
dotnet build
```

### Імпорт CSV

```powershell
dotnet run --project src/Cli
```

### Імпорт JSON

```powershell
dotnet run --project src/Cli -- data/sample.json
```

### Імпорт змішаного CSV

```powershell
dotnet run --project src/Cli -- data/sample-mixed.csv
```

### Демонстрація доменної моделі

```powershell
dotnet run --project src/Cli -- --lab04
```

Демонстрація містить:

1. Створення замовлення.
2. Додавання товарів.
3. Підрахунок загальної вартості.
4. Підтвердження замовлення.
5. Перетворення в DTO та відновлення.
6. Перевірку порушення інваріантів.
7. Перевірку незмінності стану після помилок.
8. Скасування замовлення.

### Release

```powershell
dotnet build -c Release
dotnet run --project src/Cli -c Release -- --lab04
```

### Неіснуючий файл

```powershell
dotnet run --project src/Cli -- data/notfound.csv
```

Застосунок виводить зрозуміле повідомлення про відсутність файлу.

## Publish

### Self-contained

Self-contained версія містить .NET Runtime і не потребує його окремого встановлення на цільовій системі.

```powershell
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -o publish/win-x64-self-contained
```

### Framework-dependent

Framework-dependent версія потребує встановленого .NET Runtime відповідної версії.

```powershell
dotnet publish src/Cli -c Release -r win-x64 --self-contained false -o publish/win-x64-framework-dependent
```

Self-contained публікація зазвичай займає більше дискового простору, оскільки містить компоненти середовища виконання.

## Перевірка опублікованих застосунків

Self-contained:

```powershell
cd publish/win-x64-self-contained
.\Cli.exe --lab04
```

Framework-dependent:

```powershell
cd publish/win-x64-framework-dependent
dotnet Cli.dll --lab04
```

Параметр `--lab04` запускає демонстрацію доменної моделі без потреби у вхідних CSV-файлах.

## Docker

Для запуску в Docker із .NET SDK 10 використовується команда:

```powershell
docker run --rm -v ${PWD}:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project src/Cli -- --lab04
```

Контейнер забезпечує ізольоване середовище для збірки та запуску застосунку.

## Результати

У межах проєкту реалізовано:

- Багатопроєктну архітектуру Core + Cli.
- Multi-targeting .NET 8 / .NET 10.
- Представлення даних за допомогою record.
- Імпорт CSV та JSON.
- Pattern matching та switch expressions.
- Обробку некоректних записів.
- Статистику імпорту.
- Доменну модель Order та OrderLine.
- Фабричні методи та інкапсуляцію.
- Бізнес-інваріанти.
- Керування статусами замовлень.
- Мапінг між DTO і доменними сутностями.
- Демонстрацію успішних і помилкових операцій.

Проєкт організовано так, щоб надалі можна було додавати файлове сховище, сервіси, тести та інші інтерфейси без перенесення бізнес-логіки в консольний застосунок.
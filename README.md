# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Замовлення.

Сутності: Customer, Product, Order, OrderLine.

Призначення: оформлення замовлень і підрахунок їх загальної вартості.

## Запуск

```bash
dotnet build

dotnet run --project src/Cli
```

## Структура проєкту

```text
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   └── EnvironmentInfo.cs
    └── Cli/
        ├── Cli.csproj
        └── Program.cs
```

`Core` містить спільну логіку збору інформації про середовище виконання.

`Cli` є консольним застосунком і використовує бібліотеку `Core`.

Залежність між проєктами:

```text
Cli → Core
```

Для підключення `Core` використовується `ProjectReference`.

## Multi-targeting

Бібліотека `Core` підтримує дві версії .NET:

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

Консольний застосунок `Cli` працює на .NET 10:

```xml
<TargetFramework>net10.0</TargetFramework>
```

Перевірка збірки:

```bash
dotnet build
```

## Publish

### Self-contained

Self-contained версія містить .NET runtime і не потребує окремого встановлення .NET на цільовому комп'ютері.

```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -o publish/win-x64-self-contained
```

### Framework-dependent

Framework-dependent версія не містить .NET runtime та потребує встановленого .NET 10 на цільовому комп'ютері.

```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained false -o publish/win-x64-framework-dependent
```

### Результати

| RID     | Режим               |       Розмір | Потрібен .NET Runtime |
| ------- | ------------------- | -----------: | --------------------- |
| win-x64 | Self-contained      | [вказати] MB | Ні                    |
| win-x64 | Framework-dependent | [вказати] MB | Так                   |

Self-contained версія має більший розмір, оскільки містить .NET runtime.

## Перевірка запуску

Self-contained:

```powershell
cd publish/win-x64-self-contained
.\Cli.exe
```

Framework-dependent:

```powershell
cd publish/win-x64-framework-dependent
dotnet Cli.dll
```

Обидва варіанти успішно запускають застосунок та виводять інформацію про середовище виконання.

## Docker

Роботу застосунку також перевірено в Docker-контейнері з .NET SDK 10:

```powershell
docker run --rm -v ${PWD}:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project src/Cli
```

У контейнері застосунок визначає Linux як операційну систему та використовує архітектуру X64.

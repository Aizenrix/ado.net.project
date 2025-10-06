@echo off
chcp 65001 >nul
echo 🛫 Система продажи авиабилетов
echo ================================
echo.
echo Выберите режим запуска:
echo 1^) 🎮 Интерактивная версия (полное меню^)
echo 2^) 🎯 Демонстрационная версия (автоматический показ^)
echo 3^) 🚪 Выход
echo.

set /p choice="Введите номер (1-3): "

if "%choice%"=="1" (
    echo 🚀 Запуск интерактивной версии...
    dotnet run --project AirlineTicketSystem.csproj
) else if "%choice%"=="2" (
    echo 🎯 Запуск демонстрационной версии...
    dotnet run --project Demo/AirlineTicketDemo.csproj
) else if "%choice%"=="3" (
    echo 👋 До свидания!
    exit /b 0
) else (
    echo ❌ Неверный выбор. Попробуйте снова.
)

pause

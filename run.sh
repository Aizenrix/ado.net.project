#!/bin/bash

echo "🛫 Система продажи авиабилетов"
echo "================================"
echo ""
echo "Выберите режим запуска:"
echo "1) 🎮 Интерактивная версия (полное меню)"
echo "2) 🎯 Демонстрационная версия (автоматический показ)"
echo "3) 🚪 Выход"
echo ""

read -p "Введите номер (1-3): " choice

case $choice in
    1)
        echo "🚀 Запуск интерактивной версии..."
        dotnet run --project AirlineTicketSystem.csproj
        ;;
    2)
        echo "🎯 Запуск демонстрационной версии..."
        dotnet run --project Demo/AirlineTicketDemo.csproj
        ;;
    3)
        echo "👋 До свидания!"
        exit 0
        ;;
    *)
        echo "❌ Неверный выбор. Попробуйте снова."
        ;;
esac

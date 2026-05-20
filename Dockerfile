# Этап 1: Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /App

# Копируем файл проекта и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем весь исходный код и собираем приложение
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Этап 2: Запуск приложения
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Порт, на котором будет работать приложение внутри контейнера
EXPOSE 8080

# Команда запуска
ENTRYPOINT ["dotnet", "LibraryWebSystem.dll"]
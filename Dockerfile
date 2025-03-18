# Используем официальный образ .NET SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
COPY ["HealthCheck.csproj", "./"]
RUN dotnet restore

# Копируем весь исходный код
COPY . .

# Собираем и публикуем приложение
RUN dotnet build "HealthCheck.csproj" -c Release -o /app/build
RUN dotnet publish "HealthCheck.csproj" -c Release -o /app/publish

# Используем официальный образ .NET Runtime для запуска
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Указываем порт, который будет использовать приложение
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Запускаем приложение
ENTRYPOINT ["dotnet", "HealthCheck.dll"]
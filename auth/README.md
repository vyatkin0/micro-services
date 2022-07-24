# Introduction 
Пакет предназначен для подключения аутентификации и авторизации пользователей с использованием сервиса identity в других сервисах Micro.

# Getting Started
Для включения поддержки аутентификации и авторизации пользователей в сервисе необходимо добавить следующие строки:
1.	Необходимо добавить инструкцию using MicroAuth; 
2.  В ConfigureServices(IServiceCollection services) необходимо добавить вызов services.AddMicroAuth();
3.	В Configure(IApplicationBuilder app) необходимо добавить вызов app.UseWapiAuth() перед app.MapGrpcService(...);
4.	В файлах сервисов или контроллеров добавить атрибут [Authorize] для всего класса или для отдельных методов;
5.	Внутри методов имеющих атрибут [Authorize] допускается делать вызов HttpContext Context = context.GetHttpContext(),
    где context имеет тип ServerCallContext, и затем использовать объект Context.User для проверки пользователя метода.

# Build and Test
1. Для сборки пакета необходимо установить номер версии в элементе VersionPrefix файла MicroAuth.csproj;
2. Выполнить команду dotnet build --configuration Release

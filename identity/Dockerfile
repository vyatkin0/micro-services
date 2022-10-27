# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
COPY bin/Release/net6.0/publish app/
WORKDIR /app
ENTRYPOINT ["dotnet", "MicroIdentity.dll"]
# MicroIdentity
Example of an Identity gRPC service with ASP.NET Core and Microsoft identity platform.

MSSQL Server used as a data storage.

These are generic installation instructions.

Edit database connection string in file appsettings.json;

Download and install ASP.NET Core SDK 5.0;

In the solution's folder execute following commands:

`dotnet restore`

`dotnet tool install --global dotnet-ef`

`dotnet ef migrations add Initial --project MicroIdentity.csproj`

`dotnet ef database update --project MicroIdentity.csproj`

`dotnet build`

`dotnet test`


In the solution's folder execute `dotnet run` command.

The service is running now on a local grpc-server with url http://localhost:5102

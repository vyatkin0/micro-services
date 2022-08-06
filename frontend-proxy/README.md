# MicroFrontendProxy
Example of an ASP.NET Core REST-gRPC proxy service that is used to provide access for web applications to the system infrastructure.

These are generic installation instructions.

Download and install ASP.NET Core SDK 6.0;

In the solution's folder execute following commands:

`dotnet restore`

`dotnet build`

`dotnet test`


In the solution's folder execute `dotnet run --project MicroFrontendProxy` command.

The service is running now on a local grpc-server with url http://localhost:5000

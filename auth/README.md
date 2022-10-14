# Introduction 
This package is for simplifying authentication and authorization provided by micro-identity service with other ASP .NET services.

# Getting Started
Following lines of code should be added to enable using authorization and authentication to an ASP.NET service:
1.	Add instruction using MicroAuth; 
2.  In ConfigureServices(IServiceCollection services) method add call services.AddMicroAuth();
3.	In Configure(IApplicationBuilder app) add call app.UseMicroAuth() before app.MapGrpcService(...);
4.	Services and controllers or some methods that are required authentication or authorization have to be annotated with attribute [Authorize];
5.	Methods annotated by [Authorize] attribute can use HttpContext Context = context.GetHttpContext(), where context has type ServerCallContext, and then use object Context.User for validating user credentials.

# Build and Test
1. To build the package set version in VersionPrefix element in file MicroAuth.csproj;
2. Run command dotnet build --configuration Release

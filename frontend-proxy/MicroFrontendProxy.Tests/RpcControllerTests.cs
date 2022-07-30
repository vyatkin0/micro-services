using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MicroFrontendProxy.Controllers;
using MicroFrontendProxy.Models;
using MicroIdentity.Protos;
using System.Text.Json;
using System.Collections.Generic;

namespace MicroFrontendProxy.Tests
{
    [TestClass]
    public class RpcControllerTests
    {
        internal readonly IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json").Build();

        internal JsonSerializerOptions options = new JsonSerializerOptions{
            PropertyNamingPolicy=JsonNamingPolicy.CamelCase
        };

        [TestMethod]
        public void CorrectParameters()
        {
            RpcController controller = new RpcController(config);

            const string firstName = "Test";
            string lastName = DateTime.UtcNow.Ticks.ToString();
            RpcRequest request = new RpcRequest()
            {
                Interface = "Accounts",
                Method = "Register",
                Service = "Identity",
                Message = JsonSerializer.SerializeToElement(new
                {
                    company = "Test",
                    email = lastName + "@test.ru",
                    firstName = firstName,
                    lastName = lastName,
                    name = firstName + lastName,
                    password = "Pa$$w0rd"
                }, options),
            };

            JsonResult result = controller.Call(request) as JsonResult;
            Assert.IsNotNull(result);

            LoginInfo info = result.Value as LoginInfo;

            RpcRequest logoutRequest = new RpcRequest()
            {
                Interface = "Accounts",
                Method = "Logout",
                Service = "Identity",
                Message = JsonSerializer.SerializeToElement(new { }),
                Headers = new Dictionary<string, string>
                {
                    ["authorization"] = info.AccessToken,
                },
            };

            JsonResult logoutResult = controller.Call(logoutRequest) as JsonResult;
            Assert.IsNotNull(logoutResult);

            RpcRequest loginRequest = new RpcRequest()
            {
                Interface = "accounts",
                Method = "login",
                Service = "Identity",
                Message = JsonSerializer.SerializeToElement(new
                {
                    name = firstName + lastName,
                    password = "Pa$$w0rd"
                }, options),
            };

            JsonResult loginResult = controller.Call(loginRequest) as JsonResult;
            Assert.IsNotNull(loginResult);

            info = loginResult.Value as LoginInfo;

            logoutRequest.Headers["authorization"] = info.AccessToken;

            JsonResult logoutResult2 = controller.Call(logoutRequest) as JsonResult;
            Assert.IsNotNull(logoutResult2);
        }

        [TestMethod]
        public void WrongService()
        {
            RpcController controller = new RpcController(config);
            RegisterRequest registerRequest = new RegisterRequest()
            {
                Company = "Test",
                Email = "test@test.ru",
                FirstName = "Test",
                LastName = "Test",
                Name = "Test",
                Password = "123"
            };

            RpcRequest request = new RpcRequest()
            {
                Interface = "Account",
                Method = "Register",
                Service = "Identite",
                Message = JsonSerializer.SerializeToElement(registerRequest, options),
            };

            BadRequestObjectResult result = controller.Call(request) as BadRequestObjectResult;

            Assert.AreEqual("Wrong provider", result?.Value);
        }

        [TestMethod]
        public void WrongInterface()
        {
            RpcController controller = new RpcController(config);
            RegisterRequest registerRequest = new RegisterRequest()
            {
                Company = "Test",
                Email = "test@test.ru",
                FirstName = "Test",
                LastName = "Test",
                Name = "Test",
                Password = "123"
            };
            RpcRequest request = new RpcRequest()
            {
                Interface = "Accound",
                Method = "Register",
                Service = "identity",
                Message = JsonSerializer.SerializeToElement(registerRequest, options),
            };

            BadRequestObjectResult result = controller.Call(request) as BadRequestObjectResult;

            Assert.AreEqual("Wrong client", result?.Value);
        }

        [TestMethod]
        public void WrongMethod()
        {
            RpcController controller = new RpcController(config);
            RegisterRequest registerRequest = new RegisterRequest()
            {
                Company = "Test",
                Email = "test@test.ru",
                FirstName = "Test",
                LastName = "Test",
                Name = "Test",
                Password = "123"
            };

            RpcRequest request = new RpcRequest()
            {
                Interface = "Accounts",
                Method = "Registet",
                Service = "Identity",
                Message = JsonSerializer.SerializeToElement(registerRequest, options),
            };

            BadRequestObjectResult result = controller.Call(request) as BadRequestObjectResult;

            Assert.AreEqual("Wrong method", result?.Value);
        }

        [TestMethod]
        public void WrongMessage()
        {
            RpcController controller = new RpcController(config);
            var loginRequest = new
            {
                extra = "",
                name = "Test",
                password = "123"
            };

            RpcRequest request = new RpcRequest()
            {
                Interface = "Accounts",
                Method = "Register",
                Service = "Identity",
                Message = JsonSerializer.SerializeToElement(loginRequest, options),
            };

            BadRequestObjectResult result = controller.Call(request) as BadRequestObjectResult;

            Assert.AreEqual("Wrong message", result?.Value);
        }
    }
}

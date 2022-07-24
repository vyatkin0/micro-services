using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MicroFrontendProxy.Controllers;
using MicroFrontendProxy.Extensions;
using MicroFrontendProxy.Models;
using MicroIdentity.Protos;

namespace MicroFrontendProxy.Tests
{
    [TestClass]
    public class RpcControllerTests
    {
        [TestMethod]
        public void Call_CorrectParameters_Success()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();
            var controller = new RpcController(config);
            var testRequest = new
            {
                Name = "Test",
                Password = "Pa$$w0rd"
            };

            var request = new RpcRequest()
            {
                Interface = "accounts",
                Method = "login",
                Service = "identity",
                Message = JsonExtensions.JsonElementFromObject(testRequest)
            };
            var result = controller.Call(request);

            Assert.AreEqual(typeof(JsonResult), result.GetType());
        }

        [TestMethod]
        public void Call_IncorrectService_Error()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();
            var controller = new RpcController(config);
            var registerRequest = new RegisterRequest()
            {
                Company = "Test",
                Email = "test@test.ru",
                FirstName = "Test",
                LastName = "Test",
                Name = "Test",
                Password = "123"
            };
            var request = new RpcRequest()
            {
                Interface = "Account",
                Method = "Register",
                Service = "Identite",
                Message = JsonExtensions.JsonElementFromObject(registerRequest)
            };
            var result = controller.Call(request);

            Assert.AreEqual("Can't find provider", ((BadRequestObjectResult)result).Value.ToString());
        }

        [TestMethod]
        public void Call_IncorrectInterface_Error()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();
            var controller = new RpcController(config);
            var registerRequest = new RegisterRequest()
            {
                Company = "Test",
                Email = "test@test.ru",
                FirstName = "Test",
                LastName = "Test",
                Name = "Test",
                Password = "123"
            };
            var request = new RpcRequest()
            {
                Interface = "Accound",
                Method = "Register",
                Service = "Identity",
                Message = JsonExtensions.JsonElementFromObject(registerRequest)
            };
            var result = controller.Call(request);

            Assert.AreEqual("Can't find client", ((BadRequestObjectResult)result).Value.ToString());
        }

        [TestMethod]
        public void Call_IncorrectMethod_Error()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();
            var controller = new RpcController(config);
            var registerRequest = new RegisterRequest()
            {
                Company = "Test",
                Email = "test@test.ru",
                FirstName = "Test",
                LastName = "Test",
                Name = "Test",
                Password = "123"
            };
            var request = new RpcRequest()
            {
                Interface = "Account",
                Method = "Registet",
                Service = "Identity",
                Message = JsonExtensions.JsonElementFromObject(registerRequest)
            };
            var result = controller.Call(request);

            Assert.AreEqual("Couldn't find method", ((BadRequestObjectResult)result).Value.ToString());
        }

        [TestMethod]
        public void Call_IncorrectMessage_Error()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();
            var controller = new RpcController(config);
            var loginRequest = new LoginRequest()
            {
                Name = "Test",
                Password = "123"
            };
            var request = new RpcRequest()
            {
                Interface = "Account",
                Method = "Register",
                Service = "Identity",
                Message = JsonExtensions.JsonElementFromObject(loginRequest)
            };
            var result = controller.Call(request);

            Assert.AreEqual("Incorrect message", ((BadRequestObjectResult)result).Value.ToString());
        }
    }
}

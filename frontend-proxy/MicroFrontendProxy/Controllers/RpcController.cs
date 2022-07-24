using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using MicroFrontendProxy.Models;

namespace MicroFrontendProxy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RpcController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RpcController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("")]
        public IActionResult Call(RpcRequest request)
        {
            string serviceName = char.IsUpper(request.Service[0])
                ? request.Service
                : char.ToUpper(request.Service[0], CultureInfo.InvariantCulture) + request.Service.Substring(1);

            string interfaceName = char.IsUpper(request.Interface[0])
                ? request.Interface
                : char.ToUpper(request.Interface[0], CultureInfo.InvariantCulture) + request.Interface.Substring(1);

            string providerName = string.Format("MicroFrontendProxy.Providers.{0}Provider", serviceName),
                clientName = string.Format("Micro{0}.Protos.{1}+{1}Client", serviceName, interfaceName);

            Type providerType = Type.GetType(providerName);

            Type clientType = Type.GetType(clientName);

            if (providerType == null)
                return BadRequest("Wrong provider");
            if (clientType == null)
                return BadRequest("Wrong client");

            try
            {
                MethodInfo clientMethod = null;
                ParameterInfo defaultParameter = null;
                try
                {
                    clientMethod = clientType.GetMethods().FirstOrDefault(m => m.Name == request.Method);
                    if (clientMethod == null)
                    {
                        if (!char.IsUpper(request.Interface[0]))
                        {
                            string requestMethod = char.ToUpper(request.Method[0], CultureInfo.InvariantCulture) + request.Method.Substring(1);
                            clientMethod = clientType.GetMethods().FirstOrDefault(m => m.Name == requestMethod);
                        }

                        if (clientMethod == null)
                        {
                            return BadRequest("Wrong method");
                        }
                    }

                    defaultParameter = clientMethod.GetParameters().First();
                }
                catch { return BadRequest("Wrong method"); }

                Google.Protobuf.IMessage message = null;
                try
                {
                    string mesText = request.Message.GetRawText();

                    if (!string.IsNullOrEmpty(mesText) && defaultParameter.ParameterType != typeof(Google.Protobuf.WellKnownTypes.Empty))
                    {
                        PropertyInfo pi = defaultParameter.ParameterType.GetProperty("Parser");
                        object parser = pi.GetValue(defaultParameter, null);

                        MethodInfo mi = parser.GetType().GetMethod("ParseJson");

                        message = mi.Invoke(parser, new object[] { mesText }) as Google.Protobuf.IMessage;
                    }
                    else
                        message = new Google.Protobuf.WellKnownTypes.Empty();
                }
                catch
                {
                    return BadRequest("Wrong message");
                }

                ConstructorInfo providerConstructor = providerType.GetConstructors().First();
                object providerObject = providerConstructor.Invoke(new object[] { _configuration });

                var channel = providerObject.GetType().GetProperty("Channel").GetValue(providerObject, null);

                ConstructorInfo clientConstructor = clientType.GetConstructor(new Type[] { channel.GetType() });
                object clientObject = clientConstructor.Invoke(new object[] { channel });

                Metadata headers = null;
                if (request.Headers != null && request.Headers.Any())
                {
                    headers = new Metadata();
                    foreach (var hdr in request.Headers)
                    {
                        headers.Add(hdr.Key, (hdr.Key == "authorization" ? "Bearer " : "") + hdr.Value);
                    }
                }

                try
                {
                    object result = clientMethod.Invoke(clientObject, new object[] { message, headers, null, null });

                    return new JsonResult(result);
                }
                catch (TargetInvocationException e)
                {
                    RpcException rpcException = e.InnerException as RpcException;
                    if (null != rpcException)
                    {
                        return BadRequest(rpcException.Status);
                    }

                    throw;
                }

            }
            catch (Exception e)
            {
                return BadRequest($"Internal error. {e.Message}");
            }
        }
    }
}

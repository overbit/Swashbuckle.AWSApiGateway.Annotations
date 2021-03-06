﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Swashbuckle.AWSApiGateway.Annotations.Extensions;
using Swashbuckle.AWSApiGateway.Annotations.Options;

namespace Swashbuckle.AWSApiGateway.Annotations
{
    internal class OpenApiOperationFactory
    {
        private static OpenApiOperation BuildCorsOptionOperation(XAmazonApiGatewayCORSOptions options, OpenApiPathItem path)
        {
            var response = new OpenApiResponse
            {
                Description = "Success",
                // AWS needs an empty content dictionary.  Unfortunately the OpenApiResponse serializer
                // considers the Content collection optional and will not serialize and empty dictionary.
                // Hence, an empty content dictionary is added below as an extension.
                // Content = new Dictionary<string, OpenApiMediaType>(),
                Extensions = new Dictionary<string, IOpenApiExtension>() { { "content", new OpenApiObject() } },
                Headers = new Dictionary<string, OpenApiHeader>()
                    .ConditionalAdd
                    (
                        () => options?.AllowOrigins != null && options.AllowOrigins.Any(),
                        HeaderNames.AccessControlAllowOrigin,
                        ()=>new OpenApiHeader { Schema = new OpenApiSchema{ Type = "string" } }
                    )
                    .ConditionalAdd
                    (
                        () => options?.AllowMethods != null && options.AllowMethods.Any(),
                        HeaderNames.AccessControlAllowMethods,
                        () =>new OpenApiHeader { Schema = new OpenApiSchema { Type = "string" } }
                    )
                    .ConditionalAdd
                    (
                        () => options?.AllowHeaders != null && options.AllowHeaders.Any(),
                        HeaderNames.AccessControlAllowHeaders,
                        () => new OpenApiHeader { Schema = new OpenApiSchema { Type = "string" } }
                    )
                    .ConditionalAdd
                    (
                        () => options?.ExposeHeaders != null && options.ExposeHeaders.Any(),
                        HeaderNames.AccessControlExposeHeaders,
                        () => new OpenApiHeader { Schema = new OpenApiSchema { Type = "string" } }
                    )
                    .ConditionalAdd
                    (
                        () => options.MaxAge.HasValue,
                        HeaderNames.AccessControlMaxAge,
                        () => new OpenApiHeader { Schema = new OpenApiSchema { Type = "string" } }
                    )
                    .ConditionalAdd
                    (
                        () => options.AllowCredentials.HasValue && options.AllowCredentials.Value,
                        HeaderNames.AccessControlAllowCredentials,
                        () => new OpenApiHeader { Schema = new OpenApiSchema { Type = "string" } }
                    )
            };

            return new OpenApiOperation
            {
                Responses = new OpenApiResponses { { "200", response } },
                Parameters = path.Operations.Select(x => x.Value).FirstOrDefault()?.Parameters
            };
        }

        internal static OpenApiOperation FromCorsOptions(XAmazonApiGatewayCORSOptions options, OpenApiPathItem path)
        {
            var corsOptionOperation = BuildCorsOptionOperation(options,path);

            var integrationOptions = new XAmazonApiGatewayIntegrationOptions
            {
                Type = IntegrationType.mock,
                PassthroughBehavior = PassthroughBehavior.WHEN_NO_MATCH,
                Responses = new Dictionary<string, IntegrationResponse>
                {
                    {
                        "200",
                        new IntegrationResponse
                        {
                            StatusCode = "200",
                            ResponseParameters = new Dictionary<string, string>()
                                .ConditionalAdd
                                (
                                    () => options?.AllowMethods != null && options.AllowMethods.Any(),
                                    $"method.response.header.{HeaderNames.AccessControlAllowMethods}",
                                    () => $"'{string.Join(",", options.AllowMethods)}'"
                                )
                                .ConditionalAdd
                                (
                                    () => options?.AllowHeaders != null && options.AllowHeaders.Any(),
                                    $"method.response.header.{HeaderNames.AccessControlAllowHeaders}",
                                    () => $"'{string.Join(",", options.AllowHeaders)}'"
                                )
                                .ConditionalAdd
                                (
                                    () => options?.AllowOrigins != null && options.AllowOrigins.Any(),
                                    $"method.response.header.{HeaderNames.AccessControlAllowOrigin}",
                                    () => $"'{string.Join(",", options.AllowOrigins)}'"
                                )
                                .ConditionalAdd
                                (
                                    () => options?.ExposeHeaders != null && options.ExposeHeaders.Any(),
                                    $"method.response.header.{HeaderNames.AccessControlExposeHeaders}",
                                    () => $"'{string.Join(",", options.ExposeHeaders)}'"
                                )
                                .ConditionalAdd
                                (
                                    () => options.MaxAge.HasValue,
                                    HeaderNames.AccessControlMaxAge,
                                    () => options.MaxAge.Value.ToString()
                                )
                                .ConditionalAdd
                                (
                                    () => options.AllowCredentials.HasValue && options.AllowCredentials.Value,
                                    HeaderNames.AccessControlAllowCredentials ,
                                    () => bool.TrueString.ToLower()
                                )
                        }
                    }
                },
                RequestTemplates = new Dictionary<string, string>
                {
                    {"application/json", "{\"statusCode\": 200}"}
                }
            };

            foreach (var item in integrationOptions.ToDictionary())
            {
                corsOptionOperation.Extensions.Add(item.Key, item.Value);
            }

            return corsOptionOperation;
        }
    }
}

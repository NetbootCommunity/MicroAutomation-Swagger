using MicroAutomation.Swagger.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MicroAutomation.Swagger.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    /// Add Swagger Generator.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        // Get swagger configurations.
        var swaggerConfiguration = new SwaggerOption();
        configuration.GetSection(nameof(SwaggerOption)).Bind(swaggerConfiguration);

        if (swaggerConfiguration.Documents != null)
        {
            services.AddSwaggerGen(c =>
            {
                #region Documents

                foreach (var docConfiguration in swaggerConfiguration.Documents)
                {
                    c.SwaggerDoc(docConfiguration.Version, new OpenApiInfo
                    {
                        Version = docConfiguration.Version,
                        Title = docConfiguration.Title,
                        Description = docConfiguration.Description,
                        Contact = new OpenApiContact
                        {
                            Name = docConfiguration.Contact.Name,
                            Email = docConfiguration.Contact.Email,
                            Url = docConfiguration.Contact.Url
                        }
                    });
                }

                #endregion Documents

                #region Oauth2

                if (swaggerConfiguration.Features.OauthAuthentication)
                {
                    c.AddSecurityDefinition("Oauth2", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Description = "Standard authorisation using the Oauth2 scheme.",
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Scheme = "Bearer",
                        BearerFormat = "JWT",

                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = swaggerConfiguration.Authentication.AuthorizationUrl,
                                TokenUrl = swaggerConfiguration.Authentication.TokenUrl,
                                Scopes = swaggerConfiguration.Authentication.Scopes,
                            }
                        }
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {{
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Oauth2"
                                }
                            },
                            new string[] {}
                        }});
                }

                #endregion Oauth2

                #region Bearer

                if (swaggerConfiguration.Features.BearerAuthentication)
                {
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "Standard authorisation using the Bearer scheme. Example: \"bearer {token}\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {{
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}
                        }});
                }

                #endregion Bearer

                // Describe all parameters, regardless of how they appear in code, in camelCase.
                c.DescribeAllParametersInCamelCase();

                // To avoid errors on several application media types.
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                // Add customization for controller names
                // https://rimdev.io/swagger-grouping-with-controller-name-fallback-using-swashbuckle-aspnetcore/
                c.EnableAnnotations();
                c.TagActionsBy(api =>
                {
                    if (api.GroupName != null)
                    {
                        return new[] { api.GroupName };
                    }

                    if (api.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                    {
                        return new[] { controllerActionDescriptor.ControllerName };
                    }

                    throw new InvalidOperationException("Unable to determine tag for endpoint.");
                });
                c.DocInclusionPredicate((_, __) => true);

                // Load all custom implementations of swagger filters.
                c.AddDocumentFilters();
                c.AddOperationFilters();

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    c.IncludeXmlComments(xmlPath, true);
            });
        }
    }

    /// <summary>
    /// Register the Swagger middleware.
    /// </summary>
    /// <param name="app"></param>
    public static void UseSwaggerUI(this IApplicationBuilder app, IConfiguration configuration)
    {
        // Get swagger configurations.
        var swaggerConfiguration = new SwaggerOption();
        configuration.GetSection(nameof(SwaggerOption)).Bind(swaggerConfiguration);

        // Enable middleware to serve generated Swagger as a JSON endpoint.
        app.UseSwagger(c =>
        {
            c.RouteTemplate = swaggerConfiguration.RoutePrefix + "/{documentname}/swagger.json";
        });

        // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
        // specifying the Swagger JSON endpoint.
        app.UseSwaggerUI(c =>
        {
            // Global configuration.
            c.EnableFilter();
            c.DocExpansion(DocExpansion.None);
            c.DisplayRequestDuration();
            c.RoutePrefix = swaggerConfiguration.RoutePrefix;

            // Set OAuth configuration.
            if (swaggerConfiguration.Features.OauthAuthentication)
            {
                c.OAuthClientId(swaggerConfiguration.Authentication.ClientId);
                c.OAuthClientSecret(swaggerConfiguration.Authentication.ClientSecret);
                c.OAuthScopes(swaggerConfiguration.Authentication.Scopes?.Keys.ToArray());
                c.OAuthUsePkce();
            }

            // Add all swagger endpoints.
            foreach (var docConfiguration in swaggerConfiguration.Documents)
                c.SwaggerEndpoint($"{swaggerConfiguration.BasePath}{swaggerConfiguration.RoutePrefix}/{docConfiguration.Version}/swagger.json", $"{docConfiguration.Title} {docConfiguration.Version}");
        });
    }

    /// <summary>
    ///  Extend the Swagger Generator with "filters" that can modify SwaggerDocuments after they're initially generated
    /// </summary>
    /// <param name="swaggerGen"></param>
    /// <returns></returns>
    private static SwaggerGenOptions AddDocumentFilters(this SwaggerGenOptions swaggerGen)
    {
        var parameters = Array.Empty<object>();
        foreach (var implementation in GetTypesWithInterface<ICustomDocumentFilter>())
        {
            swaggerGen.DocumentFilterDescriptors.Add(new FilterDescriptor
            {
                Type = implementation,
                Arguments = parameters
            });
        }
        return swaggerGen;
    }

    /// <summary>
    /// Extend the Swagger Generator with "filters" that can modify Operations after they're initially generated.
    /// </summary>
    /// <param name="swaggerGen"></param>
    /// <returns></returns>
    private static SwaggerGenOptions AddOperationFilters(this SwaggerGenOptions swaggerGen)
    {
        var parameters = Array.Empty<object>();
        foreach (var implementation in GetTypesWithInterface<ICustomOperationFilter>())
        {
            swaggerGen.OperationFilterDescriptors.Add(new FilterDescriptor
            {
                Type = implementation,
                Arguments = parameters
            });
        }
        return swaggerGen;
    }

    /// <summary>
    /// Retrieve all implementations of the selected type.
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    /// <returns></returns>
    private static List<Type> GetTypesWithInterface<TType>()
    {
        var type = typeof(TType);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);
        return types.ToList();
    }
}
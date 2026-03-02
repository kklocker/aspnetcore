#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task PropertyDisplayName_WithNameOnly()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/property-display-name", (PropertyDisplayNameType model) => Results.Ok("Passed"!));

app.Run();

public class PropertyDisplayNameType
{
    [Range(10, 100), Display(Name = "My Custom Name")]
    public int Value { get; set; } = 10;
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/property-display-name", async (endpoint, serviceProvider) =>
        {
            var payload = """
            {
                "Value": 5
            }
            """;
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            Assert.Collection(problemDetails.Errors, kvp =>
            {
                Assert.Equal("Value", kvp.Key);
                Assert.Equal("The field My Custom Name must be between 10 and 100.", kvp.Value.Single());
            });
        });
    }

    [Fact]
    public async Task PropertyDisplayName_WithResourceType()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/property-resource-display-name", (PropertyResourceDisplayNameType model) => Results.Ok("Passed"!));

app.Run();

public class PropertyResourceDisplayNameType
{
    [Range(10, 100), Display(Name = "ValueDisplayName", ResourceType = typeof(TestResources))]
    public int Value { get; set; } = 10;
}

public class TestResources
{
    public static string ValueDisplayName => "Localized Value Name";
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/property-resource-display-name", async (endpoint, serviceProvider) =>
        {
            var payload = """
            {
                "Value": 5
            }
            """;
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            Assert.Collection(problemDetails.Errors, kvp =>
            {
                Assert.Equal("Value", kvp.Key);
                Assert.Equal("The field Localized Value Name must be between 10 and 100.", kvp.Value.Single());
            });
        });
    }

    [Fact]
    public async Task ParameterDisplayName_WithNameOnly()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/param-display-name", (
    [Range(10, 100), Display(Name = "Parameter Label")] int value) => "OK");

app.Run();
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/param-display-name", async (endpoint, serviceProvider) =>
        {
            var context = CreateHttpContext(serviceProvider);
            context.Request.QueryString = new QueryString("?value=5");
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            Assert.Collection(problemDetails.Errors, kvp =>
            {
                Assert.Equal("value", kvp.Key);
                Assert.Equal("The field Parameter Label must be between 10 and 100.", kvp.Value.Single());
            });
        });
    }

    [Fact]
    public async Task ParameterDisplayName_WithResourceType()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/param-resource-display-name", (
    [Range(10, 100), Display(Name = "ParamDisplayName", ResourceType = typeof(ParamResources))] int value) => "OK");

app.Run();

public class ParamResources
{
    public static string ParamDisplayName => "Localized Parameter Name";
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/param-resource-display-name", async (endpoint, serviceProvider) =>
        {
            var context = CreateHttpContext(serviceProvider);
            context.Request.QueryString = new QueryString("?value=5");
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            Assert.Collection(problemDetails.Errors, kvp =>
            {
                Assert.Equal("value", kvp.Key);
                Assert.Equal("The field Localized Parameter Name must be between 10 and 100.", kvp.Value.Single());
            });
        });
    }

    [Fact]
    public async Task TypeDisplayName_WithNameOnly()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.Run();

[ValidatableType]
[Display(Name = "My Model")]
[EvenSumValidation]
public class TypeWithDisplayName : IValidatableObject
{
    [Range(0, 100)]
    public int X { get; set; } = 10;

    [Range(0, 100)]
    public int Y { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // The DisplayName should be "My Model" from the Display attribute
        if (validationContext.DisplayName != "My Model")
        {
            yield return new ValidationResult(
                $"Expected display name 'My Model' but got '{validationContext.DisplayName}'");
        }
    }
}

public class EvenSumValidationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is TypeWithDisplayName model && (model.X + model.Y) % 2 != 0)
        {
            return new ValidationResult("Sum must be even");
        }
        return ValidationResult.Success;
    }
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "TypeWithDisplayName", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            var instance = Activator.CreateInstance(type);
            var context = new ValidateContext
            {
                ValidationOptions = validationOptions,
                ValidationContext = new ValidationContext(instance)
            };

            await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

            // Should produce no errors - the IValidatableObject.Validate checks
            // that DisplayName is "My Model" and reports an error if not
            Assert.Null(context.ValidationErrors);
        });
    }

    [Fact]
    public async Task TypeDisplayName_WithResourceType()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.Run();

[ValidatableType]
[Display(Name = "TypeDisplayName", ResourceType = typeof(TypeResources))]
public class TypeWithResourceDisplayName : IValidatableObject
{
    [Range(0, 100)]
    public int X { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // The DisplayName should be the localized value from the resource type
        if (validationContext.DisplayName != "Localized Type Name")
        {
            yield return new ValidationResult(
                $"Expected display name 'Localized Type Name' but got '{validationContext.DisplayName}'");
        }
    }
}

public class TypeResources
{
    public static string TypeDisplayName => "Localized Type Name";
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "TypeWithResourceDisplayName", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            var instance = Activator.CreateInstance(type);
            var context = new ValidateContext
            {
                ValidationOptions = validationOptions,
                ValidationContext = new ValidationContext(instance)
            };

            await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

            // Should produce no errors - the IValidatableObject.Validate checks
            // that DisplayName is "Localized Type Name" and reports an error if not
            Assert.Null(context.ValidationErrors);
        });
    }

    [Fact]
    public async Task PropertyDisplayName_WithoutDisplayAttribute_UsesPropertyName()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/no-display-attr", (NoDisplayAttributeType model) => Results.Ok("Passed"!));

app.Run();

public class NoDisplayAttributeType
{
    [Range(10, 100)]
    public int MyProperty { get; set; } = 10;
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/no-display-attr", async (endpoint, serviceProvider) =>
        {
            var payload = """
            {
                "MyProperty": 5
            }
            """;
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            Assert.Collection(problemDetails.Errors, kvp =>
            {
                Assert.Equal("MyProperty", kvp.Key);
                Assert.Equal("The field MyProperty must be between 10 and 100.", kvp.Value.Single());
            });
        });
    }

    [Fact]
    public async Task RecordPropertyDisplayName_WithNameOnly()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/record-display-name", (RecordWithDisplayName model) => Results.Ok("Passed"!));

app.Run();

public record RecordWithDisplayName(
    [Range(10, 100), Display(Name = "Record Field Label")] int Value);
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/record-display-name", async (endpoint, serviceProvider) =>
        {
            var payload = """
            {
                "Value": 5
            }
            """;
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            Assert.Collection(problemDetails.Errors, kvp =>
            {
                Assert.Equal("Value", kvp.Key);
                Assert.Equal("The field Record Field Label must be between 10 and 100.", kvp.Value.Single());
            });
        });
    }

    [Fact]
    public async Task RecordPropertyDisplayName_WithResourceType()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/record-resource-display-name", (RecordWithResourceDisplayName model) => Results.Ok("Passed"!));

app.Run();

public record RecordWithResourceDisplayName(
    [Range(10, 100), Display(Name = "RecordValueDisplayName", ResourceType = typeof(RecordResources))] int Value);

public class RecordResources
{
    public static string RecordValueDisplayName => "Localized Record Value";
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/record-resource-display-name", async (endpoint, serviceProvider) =>
        {
            var payload = """
            {
                "Value": 5
            }
            """;
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            Assert.Collection(problemDetails.Errors, kvp =>
            {
                Assert.Equal("Value", kvp.Key);
                Assert.Equal("The field Localized Record Value must be between 10 and 100.", kvp.Value.Single());
            });
        });
    }
}

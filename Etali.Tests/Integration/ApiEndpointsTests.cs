using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// DEPRECATED: This file contains all tests in one place and should not be used for new tests.
/// Use the separate test files instead:
/// - HandEndpointTests.cs
/// - HandEvaluateEndpointTests.cs
/// - DecklistEndpointTests.cs
/// - DecklistsEndpointTests.cs
/// - HandsEvaluatedEndpointTests.cs
/// - CorsTests.cs
/// - SwaggerTests.cs
/// - MoxfieldIntegrationTests.cs
/// - WorkflowTests.cs
/// 
/// This file is kept temporarily for backward compatibility.
/// </summary>
[Obsolete("Use separate endpoint-specific test files instead")]
public class ApiEndpointsTests
{
    // Tests have been moved to separate files by endpoint
    // This class is kept for compatibility but should not be used
}

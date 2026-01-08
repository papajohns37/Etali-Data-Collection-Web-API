using Etali_Data_Collection_Web_API.Models.Auth;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Etali_Data_Collection_Web_API.Endpoints;

/// <summary>
/// Authentication endpoints for user login and registration.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth").WithTags("Authentication");

        // POST /auth/login - Public endpoint for user authentication
        group.MapPost("/login", Login)
            .WithName("Login")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Authenticate user and receive JWT token",
                Description = @"Authenticates a user with username and password, returning a JWT bearer token.

The token should be included in subsequent requests using the Authorization header:
`Authorization: Bearer {token}`

Token is valid for the configured expiration period (default: 7 days).

Returns:
- 200 OK: Successful authentication with JWT token
- 401 Unauthorized: Invalid credentials"
            })
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        // POST /auth/register - Admin-only endpoint for creating new users
        group.MapPost("/register", Register)
            .WithName("Register")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Register a new user (Admin only)",
                Description = @"Creates a new user account with the specified username and password.

Only administrators can register new users. This endpoint requires authentication
with a valid JWT token that includes the 'Admin' role.

Request body:
- username: Unique username (3-50 characters, letters, numbers, hyphens, periods, underscores)
- password: Strong password (min 6 characters, requires uppercase, lowercase, digit, special character)
- confirmPassword: Must match password
- isAdmin: true to assign Admin role, false for Contributor role (default: false)

Roles:
- **Admin**: Can create users, add/edit data
- **Contributor**: Can add/edit data only

Returns:
- 200 OK: User created successfully
- 400 Bad Request: Validation errors or registration failed
- 401 Unauthorized: No authentication token provided
- 403 Forbidden: User is not an administrator"
            })
            .Produces(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .RequireAuthorization("AdminOnly");
    }

    private static async Task<Results<Ok<LoginResponse>, UnauthorizedHttpResult>> Login(
        LoginRequest request,
        IAuthService authService)
    {
        var response = await authService.LoginAsync(request.Username, request.Password);

        if (response == null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<object>, BadRequest<ProblemDetails>>> Register(
        RegisterRequest request,
        IAuthService authService)
    {
        // Validate model annotations
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(request);
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            var validationErrors = validationResults.Select(vr => vr.ErrorMessage ?? "Validation error").ToList();
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = string.Join("; ", validationErrors)
            });
        }

        // Validate password confirmation matches
        if (request.Password != request.ConfirmPassword)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "Password and confirmation password do not match."
            });
        }

        var (success, errors) = await authService.RegisterUserAsync(
            request.Username,
            request.Password,
            request.IsAdmin);

        if (!success)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Registration failed",
                Detail = string.Join("; ", errors)
            });
        }

        return TypedResults.Ok<object>(new
        {
            message = "User registered successfully",
            username = request.Username,
            role = request.IsAdmin ? "Admin" : "Contributor"
        });
    }
}

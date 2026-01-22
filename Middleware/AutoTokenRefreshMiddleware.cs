using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IPOClient.Models.Entities;
using IPOClient.Repositories.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace IPOClient.Middleware
{
    public class AutoTokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AutoTokenRefreshMiddleware> _logger;

        public AutoTokenRefreshMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<AutoTokenRefreshMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
        {
            try
            {
                // Only process authenticated requests
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var tokenType = context.User.FindFirst("type")?.Value;

                    // Only check access tokens (not refresh tokens)
                    if (tokenType == "access")
                    {
                        var expClaim = context.User.FindFirst("exp")?.Value;
                        if (expClaim != null && long.TryParse(expClaim, out var exp))
                        {
                            var expiryDate = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                            var timeUntilExpiry = expiryDate - DateTime.UtcNow;

                            // If token expires in less than 5 minutes, auto-refresh
                            if (timeUntilExpiry.TotalMinutes < 5 && timeUntilExpiry.TotalMinutes > 0)
                            {
                                var userIdClaim = context.User.FindFirst("sub")?.Value;
                                if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
                                {
                                    // Fetch user from database
                                    var user = await userRepository.GetByIdAsync(userId);
                                    if (user != null)
                                    {
                                        // Check if account is still valid
                                        if (!user.ExpiryDate.HasValue || user.ExpiryDate >= DateTime.UtcNow)
                                        {
                                            // Generate new tokens
                                            var newAccessToken = GenerateAccessToken(user);
                                            var newRefreshToken = GenerateRefreshToken(user);

                                            // Add new tokens to response headers
                                            context.Response.Headers["X-New-Access-Token"] = newAccessToken;
                                            context.Response.Headers["X-New-Refresh-Token"] = newRefreshToken;
                                            context.Response.Headers["X-Token-Refreshed"] = "true";

                                            _logger.LogInformation($"Auto-refreshed tokens for user {userId}. Token was expiring in {timeUntilExpiry.TotalMinutes:F2} minutes.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoTokenRefreshMiddleware");
                // Don't block the request if token refresh fails
            }

            await _next(context);
        }

        private string GenerateAccessToken(IPO_UserMaster user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");

            var key = Encoding.ASCII.GetBytes(secretKey ?? "default-secret-key");
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", user.Id.ToString()), 
                    new Claim("role", user.IsAdmin ? "Admin" : "User"),
                    new Claim("cid", user.CreatedBy.ToString()),
                    new Claim("type", "access")
                }),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken(IPO_UserMaster user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "7");

            var key = Encoding.ASCII.GetBytes(secretKey ?? "default-secret-key");
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", user.Id.ToString()),
                    new Claim("type", "refresh")
                }),
                Expires = DateTime.UtcNow.AddDays(expirationDays),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}

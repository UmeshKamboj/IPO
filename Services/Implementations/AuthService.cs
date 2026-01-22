using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IPOClient.Models.Entities;
using IPOClient.Models.Requests;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;
using IPOClient.Utilities;
using Microsoft.IdentityModel.Tokens;

namespace IPOClient.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IUserRepository userRepository, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ReturnData<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                    return ReturnData<LoginResponse>.ErrorResponse("Email and password are required", 400);

                // Find user by email
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                    return ReturnData<LoginResponse>.ErrorResponse("Invalid email or password", 401);

                // Verify password
                if (!PasswordHelper.VerifyPassword(request.Password, user.Password ?? ""))
                    return ReturnData<LoginResponse>.ErrorResponse("Invalid email or password", 401);

                // Check if account is expired
                if (user.ExpiryDate.HasValue && user.ExpiryDate < DateTime.UtcNow)
                    return ReturnData<LoginResponse>.ErrorResponse("Your account has expired", 401);

                // Generate access token (15 min) and refresh token (7 days)
                var accessToken = GenerateAccessToken(user);
                var refreshToken = GenerateRefreshToken(user);

                var response = new LoginResponse
                {
                    Token = accessToken,
                    RefreshToken = refreshToken ,
                    User = new UserResponse
                    {
                        Id = user.Id,
                        FName = user.FName,
                        LName = user.LName,
                        Email = user.Email,
                        Phone = user.Phone,
                        IsAdmin = user.IsAdmin,
                        CreatedBy = user.CreatedBy,
                        CreatedDate = user.CreatedDate,
                        ExpiryDate = user.ExpiryDate
                    }
                };

                return ReturnData<LoginResponse>.SuccessResponse(response, "Login successful", 200, user.Id);
            }
            catch (Exception ex)
            {
                return ReturnData<LoginResponse>.ErrorResponse($"Login error: {ex.Message}", 500);
            }
        }

        public async Task LogoutAsync()
        {
            _httpContextAccessor.HttpContext?.Session.Clear();
        }

        public async Task<ReturnData<LoginResponse>> RefreshTokenAsync()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                    return ReturnData<LoginResponse>.ErrorResponse("Invalid request context", 400);

                // Get user ID from current refresh token claims
                var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
                var tokenType = httpContext.User.FindFirst("type")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    return ReturnData<LoginResponse>.ErrorResponse("Invalid token", 401);

                // Verify this is a refresh token, not an access token
                if (tokenType != "refresh")
                    return ReturnData<LoginResponse>.ErrorResponse("Invalid token type. Use refresh token.", 401);

                // Fetch user from database
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return ReturnData<LoginResponse>.ErrorResponse("User not found", 401);

                // Check if account is expired
                if (user.ExpiryDate.HasValue && user.ExpiryDate < DateTime.UtcNow)
                    return ReturnData<LoginResponse>.ErrorResponse("Your account has expired", 401);

                // Generate new access token and refresh token (rotation)
                var newAccessToken = GenerateAccessToken(user);
                var newRefreshToken = GenerateRefreshToken(user);

                var response = new LoginResponse
                {
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken,
                    User = new UserResponse
                    {
                        Id = user.Id,
                        FName = user.FName,
                        LName = user.LName,
                        Email = user.Email,
                        Phone = user.Phone,
                        IsAdmin = user.IsAdmin,
                        CreatedBy = user.CreatedBy,
                        CreatedDate = user.CreatedDate,
                        ExpiryDate = user.ExpiryDate
                    }
                };

                return ReturnData<LoginResponse>.SuccessResponse(response, "Token refreshed successfully", 200, user.Id);
            }
            catch (Exception ex)
            {
                return ReturnData<LoginResponse>.ErrorResponse($"Token refresh error: {ex.Message}", 500);
            }
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
                    new Claim("type", "access") // Token type identifier
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
                    new Claim("type", "refresh") // Token type identifier
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

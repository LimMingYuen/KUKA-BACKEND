using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace QES_KUKA_AMR_API_Simulator.Auth
{
    public class SimulatorJwtOptions
    {
        private readonly IConfiguration _configuration;

        public SimulatorJwtOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string SigningKey => _configuration["Jwt:SigningKey"] ?? "QES-KUKA-AMR-LoginSimulator-SecretKey";

        public TimeSpan AccessTokenLifetime => TimeSpan.FromHours(
            _configuration.GetValue<int>("Jwt:AccessTokenLifetimeHours", 1));

        public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(
            _configuration.GetValue<int>("Jwt:RefreshTokenLifetimeDays", 7));

        public TimeSpan ClockSkew => TimeSpan.FromMinutes(
            _configuration.GetValue<int>("Jwt:ClockSkewMinutes", 5));

        public SymmetricSecurityKey GetSigningKey() =>
            new(Encoding.UTF8.GetBytes(SigningKey));

        // Static helper for backward compatibility during Program.cs startup
        public static SymmetricSecurityKey GetStaticSigningKey(IConfiguration configuration)
        {
            var key = configuration["Jwt:SigningKey"] ?? "QES-KUKA-AMR-LoginSimulator-SecretKey";
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        }
    }
}

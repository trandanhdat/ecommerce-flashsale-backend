using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.Services.Auth.DTOs;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.Users;
using FlashSale.Domain.Users.Events;
using FlashSale.Domain.Users.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace FlashSale.Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly UserManager<User> _userManager;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            UserManager<User> userManager)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _userManager = userManager;
        }

        public async Task<AuthResultDto> RegisterAsync(RegisterRequestDto dto)
        {
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
            {
                throw new ValidationException("Email is already registered.");
            }

            var user = new User(dto.Email, dto.Email, dto.FullName);
            // In a real Identity setup bypassing UserManager, we manually hash
            user.PasswordHash = _passwordHasher.Hash(dto.Password);
            user.PhoneNumber = dto.PhoneNumber;
            user.SecurityStamp = Guid.NewGuid().ToString(); // Fix: User security stamp cannot be null.

            await _userRepository.AddAsync(user);

            var defaultRole = UserRole.Customer.ToString();
            await _userManager.AddToRoleAsync(user, defaultRole);

            var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user, defaultRole);
            var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

            var refreshToken = new RefreshToken(user.Id, refreshTokenString, DateTime.UtcNow.AddDays(7));
            await _refreshTokenRepository.AddAsync(refreshToken);

            user.AddDomainEvent(new UserRegisteredEvent(user.Id));

            return new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                AccessTokenExpiresAt = expiresAt,
                UserId = user.Id,
                FullName = user.FullName,
                Role = UserRole.Customer.ToString()
            };
        }

        public async Task<AuthResultDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
            {
                throw new InvalidCredentialsException(dto.Email);
            }

            if (!_passwordHasher.Verify(user.PasswordHash, dto.Password))
            {
                throw new InvalidCredentialsException(dto.Email);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = System.Linq.Enumerable.FirstOrDefault(roles) ?? UserRole.Customer.ToString();

            var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user, role);
            var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

            var refreshToken = new RefreshToken(user.Id, refreshTokenString, DateTime.UtcNow.AddDays(7));
            await _refreshTokenRepository.AddAsync(refreshToken);

            return new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                AccessTokenExpiresAt = expiresAt,
                UserId = user.Id,
                FullName = user.FullName,
                Role = role
            };
        }

        public async Task<AuthResultDto> RefreshTokenAsync(string tokenString)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(tokenString);
            
            if (refreshToken == null || !refreshToken.IsActive)
            {
                throw new ValidationException("Invalid or expired refresh token.");
            }

            var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
            if (user == null || !user.IsActive)
            {
                throw new InvalidCredentialsException(user?.Email ?? "Unknown");
            }

            // Revoke old token (rotation)
            refreshToken.Revoke();
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            var roles = await _userManager.GetRolesAsync(user);
            var role = System.Linq.Enumerable.FirstOrDefault(roles) ?? UserRole.Customer.ToString();
            var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user, role);
            var newRefreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

            var newRefreshToken = new RefreshToken(user.Id, newRefreshTokenString, DateTime.UtcNow.AddDays(7));
            await _refreshTokenRepository.AddAsync(newRefreshToken);

            return new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshTokenString,
                AccessTokenExpiresAt = expiresAt,
                UserId = user.Id,
                FullName = user.FullName,
                Role = role
            };
        }

        public async Task RevokeTokenAsync(string tokenString)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(tokenString);
            if (refreshToken != null && refreshToken.Revoked == null)
            {
                refreshToken.Revoke();
                await _refreshTokenRepository.UpdateAsync(refreshToken);
            }
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new ValidationException("User not found.");

            if (!_passwordHasher.Verify(user.PasswordHash, dto.CurrentPassword))
            {
                throw new ValidationException("Invalid current password.");
            }

            user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
            await _userRepository.UpdateAsync(user);

            user.AddDomainEvent(new PasswordChangedEvent(user.Id));
        }
    }
}

﻿using EventsWebApp.Application.Interfaces;
using EventsWebApp.Domain.Interfaces.Repositories;
using EventsWebApp.Application.Interfaces.Services;
using EventsWebApp.Application.Validators;
using EventsWebApp.Domain.Enums;
using EventsWebApp.Domain.Exceptions;
using EventsWebApp.Domain.Models;
using System.Security.Claims;
using System.Text;
using System.Threading;

namespace EventsWebApp.Application.Services
{
    public class UserService : IDisposable, IUserService
    {
        private readonly IAppUnitOfWork _appUnitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtProvider _jwtProvider;
        private readonly UserValidator _validator;

        public UserService(IAppUnitOfWork appUnitOfWork, IPasswordHasher passwordHasher, IJwtProvider jwtProvider, UserValidator validator)
        {
            _appUnitOfWork = appUnitOfWork;
            _passwordHasher = passwordHasher;
            _jwtProvider = jwtProvider;
            _validator = validator;
        }

        public async Task<List<User>> GetAllUsers(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var users = await _appUnitOfWork.UserRepository.GetAll(cancellationToken);
            return users;
        }

        public async Task<User> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            User user = await _appUnitOfWork.UserRepository.GetById(id, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new UserException("No such user found");
            }
            return user;
        }

        public async Task<User> GetUserByEmail(string email, CancellationToken cancellationToken)
        {
            User user = await _appUnitOfWork.UserRepository.GetByEmail(email, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new UserException("No such user found");
            }
            return user;
        }
        public async Task<(string, string)> Register(string email, string password, string username, CancellationToken cancellationToken)
        {
            var candidate = await _appUnitOfWork.UserRepository.GetByEmail(email, cancellationToken);
            if (candidate != null)
            {
                throw new UserException("User already exists");
            }
            string hashedPassword = _passwordHasher.Generate(password);

            User user = new User(email, hashedPassword, username, E_Role.User);
            ValidateUser(user);

            var addedUserId = await _appUnitOfWork.UserRepository.Add(user, cancellationToken);
            user.Id = addedUserId;

            cancellationToken.ThrowIfCancellationRequested();
            var (accessToken, refreshToken) = _jwtProvider.CreateTokens(user);
            _appUnitOfWork.Save();

            return (accessToken, refreshToken);
        }

        public async Task<(string, string)> Login(string email, string password, CancellationToken cancellationToken)
        {
            User candidate = await _appUnitOfWork.UserRepository.GetByEmail(email, cancellationToken);

            if (candidate == null || !_passwordHasher.Verify(password, candidate.PasswordHash))
            {
                throw new UserException("No candidate found");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var (accessToken, refreshToken) = _jwtProvider.CreateTokens(candidate);

            _appUnitOfWork.Save();
            return (accessToken, refreshToken);
        }

        public async Task<Guid> UpdateUser(User user, CancellationToken cancellationToken)
        {
            ValidateUser(user);

            var userId = await _appUnitOfWork.UserRepository.Update(user, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            _appUnitOfWork.Save();
            return userId;
        }

        public async Task<Guid> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            int rowsDeleted = await _appUnitOfWork.UserRepository.Delete(id, cancellationToken);

            if (rowsDeleted == 0)
            {
                throw new SocialEventException("User wasn't deleted");
            }

            cancellationToken.ThrowIfCancellationRequested();
            _appUnitOfWork.Save();
            return id;
        }

        public string? GetRoleByToken(string accessToken, CancellationToken cancellationToken)
        {
            var principal = _jwtProvider.GetPrincipalFromExpiredToken(accessToken);

            cancellationToken.ThrowIfCancellationRequested();
            return principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        }

        public async Task<string> RefreshToken(string accessToken, string refreshToken, CancellationToken cancellationToken)
        {
            var principal = _jwtProvider.GetPrincipalFromExpiredToken(accessToken);

            var userId = principal.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;

            if(userId == null)
            {
                throw new UserException("No user id found");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var user = await _appUnitOfWork.UserRepository.GetById(Guid.Parse(userId), cancellationToken);

            if (user == null || user.RefreshToken != refreshToken || user.ExpiresRefreshToken <= DateTime.UtcNow)
            {
                throw new TokenException("Invalid token");
            }

            cancellationToken.ThrowIfCancellationRequested();
            accessToken = _jwtProvider.GenerateAccessToken(user);
            _appUnitOfWork.Save();
            return accessToken;
        }

        private void ValidateUser(User user)
        {
            var result = _validator.Validate(user);
            if (!result.IsValid)
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (var error in result.Errors)
                {
                    stringBuilder.Append(error.ErrorMessage);
                }
                throw new UserException(stringBuilder.ToString());
            }
        }
        public void Dispose()
        {
            _appUnitOfWork.Dispose();
        }
    }
}

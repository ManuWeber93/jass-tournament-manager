using JassTournamentManager.Application.Common;
using JassTournamentManager.Application.Users;
using JassTournamentManager.Contracts.Users;

namespace JassTournamentManager.Api.Tests.Users
{
    internal sealed class FakeUserService : IUserService
    {
        public Result<UserResponse>? CreateAsyncResult { get; set; }

        public Result<UserResponse>? GetByIdAsyncResult { get; set; }

        public Result<IEnumerable<ClaimableUserResponse>>? GetClaimableUsersResult { get; set; }

        public Result<bool>? ResetPasswordResult { get; set; }

        public CreateUserRequest? ReceivedCreateAsyncRequest { get; private set; }

        public Guid? ReceivedGetByIdAsyncRequest { get; private set; }

        public Guid? ReceivedResetPasswordUserId { get; private set; }

        public ResetPasswordRequest? ReceivedResetPasswordRequest { get; private set; }

        public int CreateClaimableAsyncCallCount { get; private set; }

        public int GetClaimableUsersCallCount { get; private set; }

        public int ResetPasswordCallCount { get; private set; }

        public Task<Result<UserResponse>> CreateClaimableAsync(CreateUserRequest request, CancellationToken cancellationToken)
        {
            ReceivedCreateAsyncRequest = request;
            CreateClaimableAsyncCallCount++;
            return Task.FromResult(CreateAsyncResult!);
        }

        public Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            ReceivedGetByIdAsyncRequest = id;
            return Task.FromResult(GetByIdAsyncResult!);
        }

        public Task<Result<IEnumerable<ClaimableUserResponse>>> GetClaimableUsers(CancellationToken cancellationToken)
        {
            GetClaimableUsersCallCount++;
            return Task.FromResult(GetClaimableUsersResult!);
        }

        public Task<Result<bool>> ResetPassword(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            ReceivedResetPasswordUserId = userId;
            ReceivedResetPasswordRequest = request;
            ResetPasswordCallCount++;
            return Task.FromResult(ResetPasswordResult!);
        }
    }
}


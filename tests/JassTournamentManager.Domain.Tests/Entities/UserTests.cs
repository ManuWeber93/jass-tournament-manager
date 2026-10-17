using FluentAssertions;
using JassTournamentManager.Domain.Entities;
using JassTournamentManager.Domain.Enums;
using JassTournamentManager.Domain.Tests.TestHelpers;

namespace JassTournamentManager.Domain.Tests.Entities
{
    public class UserTests
    {
        [Fact]
        public void Constructor_WithBlankFirstName_ThrowsArgumentException()
        {
            var blankFirstName = " ";

            Action act = () => new User(
                UserTestData.CreateEmail(),
                UserTestData.CreatePasswordHash(),
                blankFirstName,
                UserTestData.CreateLastName(),
                UserTestData.CreateSourceType());

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithBlankLastName_ThrowsArgumentException()
        {
            var blankLastName = " ";

            Action act = () => new User(
                UserTestData.CreateEmail(),
                UserTestData.CreatePasswordHash(),
                UserTestData.CreateFirstName(),
                blankLastName,
                UserTestData.CreateSourceType());

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithTooLongEmail_ThrowsArgumentOutOfRangeException()
        {
            var tooLongEmail = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" +
                                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" +
                                "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc" +
                                "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd" +
                                "@example.com";

            Action act = () => new User(
                tooLongEmail,
                UserTestData.CreatePasswordHash(),
                UserTestData.CreateFirstName(),
                UserTestData.CreateLastName(),
                UserTestData.CreateSourceType());

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_WithTooLongFirstName_ThrowsArgumentOutOfRangeException()
        {
            var tooLongFirstName = "MaximilianAlexanderSebastianChristopherJonathanTheodore";

            Action act = () => new User(
                UserTestData.CreateEmail(),
                UserTestData.CreatePasswordHash(),
                tooLongFirstName,
                UserTestData.CreateLastName(),
                UserTestData.CreateSourceType());

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_WithTooLongLastName_ThrowsArgumentOutOfRangeException()
        {
            var tooLongLastName = "VonUndZuHinteroberunterwasserschlossensbergerMeierHuber";

            Action act = () => new User(
                UserTestData.CreateEmail(),
                UserTestData.CreatePasswordHash(),
                UserTestData.CreateFirstName(),
                tooLongLastName,
                UserTestData.CreateSourceType());

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_WithSelfRegisteredUserAndBlankEmail_ThrowsArgumentException()
        {
            var blankEmail = " ";

            Action act = () => new User(
                blankEmail,
                UserTestData.CreatePasswordHash(),
                UserTestData.CreateFirstName(),
                UserTestData.CreateLastName(),
                UserSourceType.SelfRegistered);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithSelfRegisteredUserAndBlankPasswordHash_ThrowsArgumentException()
        {
            var blankPasswordHash = " ";

            Action act = () => new User(
                UserTestData.CreateEmail(),
                blankPasswordHash,
                UserTestData.CreateFirstName(),
                UserTestData.CreateLastName(),
                UserSourceType.SelfRegistered);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_DefaultIsActive_IsTrue()
        {
            var user = UserTestData.CreateUser();

            user.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Constructor_DefaultIsSysAdmin_IsFalse()
        {
            var user = UserTestData.CreateUser();

            user.IsSysAdmin.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithValidValues_CreatesUser()
        {
            var email = UserTestData.CreateEmail();
            var passwordHash = UserTestData.CreatePasswordHash();
            var firstName = UserTestData.CreateFirstName();
            var lastName = UserTestData.CreateLastName();
            var sourceType = UserTestData.CreateSourceType();
            var isActive = false;
            var isSysAdmin = true;

            var user = new User(
                email,
                passwordHash,
                firstName,
                lastName,
                sourceType,
                isActive,
                isSysAdmin);

            user.Email.Should().Be(email);
            user.PasswordHash.Should().Be(passwordHash);
            user.FirstName.Should().Be(firstName);
            user.LastName.Should().Be(lastName);
            user.SourceType.Should().Be(sourceType);
            user.IsActive.Should().Be(isActive);
            user.IsSysAdmin.Should().Be(isSysAdmin);
        }

        [Fact]
        public void Constructor_WithPaddedValues_NormalizesUser()
        {
            var email = UserTestData.CreateEmail();
            var passwordHash = UserTestData.CreatePasswordHash();
            var firstName = UserTestData.CreateFirstName();
            var lastName = UserTestData.CreateLastName();

            var user = new User(
                " " + email.ToUpperInvariant() + " ",
                " " + passwordHash + " ",
                " " + firstName + " ",
                " " + lastName + " ",
                UserTestData.CreateSourceType());

            user.Email.Should().Be(email);
            user.PasswordHash.Should().Be(passwordHash);
            user.FirstName.Should().Be(firstName);
            user.LastName.Should().Be(lastName);
        }

        [Fact]
        public void Constructor_WithManualUserSourceTypeAndMissingCredentials_CreatesUser()
        {
            var user = new User(
                null,
                null,
                UserTestData.CreateFirstName(),
                UserTestData.CreateLastName(),
                UserSourceType.Manual);

            user.Email.Should().BeNull();
            user.PasswordHash.Should().BeNull();
            user.SourceType.Should().Be(UserSourceType.Manual);
        }

        [Fact]
        public void Update_WithEmptyEmail_ThrowsArgumentException()
        {
            var user = UserTestData.CreateUser();
            var emptyEmail = " ";

            Action act = () => user.Update(
                emptyEmail,
                UserTestData.CreatePasswordHash(),
                UserTestData.CreateFirstName(),
                UserTestData.CreateLastName(),
                true);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_WithEmptyPasswordHash_ThrowsArgumentException()
        {
            var user = UserTestData.CreateUser();
            var emptyPasswordHash = " ";

            Action act = () => user.Update(
                UserTestData.CreateEmail(),
                emptyPasswordHash,
                UserTestData.CreateFirstName(),
                UserTestData.CreateLastName(),
                true);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_WithEmptyFirstName_ThrowsArgumentException()
        {
            var user = UserTestData.CreateUser();
            var emptyFirstName = " ";

            Action act = () => user.Update(
                UserTestData.CreateEmail(),
                UserTestData.CreatePasswordHash(),
                emptyFirstName,
                UserTestData.CreateLastName(),
                true);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_WithEmptyLastName_ThrowsArgumentException()
        {
            var user = UserTestData.CreateUser();
            var emptyLastName = " ";

            Action act = () => user.Update(
                UserTestData.CreateEmail(),
                UserTestData.CreatePasswordHash(),
                UserTestData.CreateFirstName(),
                emptyLastName,
                true);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetIsSysAdmin_UpdatesIsSysAdmin()
        {
            var user = UserTestData.CreateUser();
            var isSysAdmin = true;

            user.SetIsSysAdmin(isSysAdmin);

            user.IsSysAdmin.Should().Be(isSysAdmin);
        }

        [Fact]
        public void SetPasswordHash_WithEmptyPasswordHash_ThrowsArgumentException()
        {
            var user = UserTestData.CreateUser();
            var emptyPasswordHash = " ";

            var act = () => user.SetPasswordHash(emptyPasswordHash);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetPasswordHash_WithValidPasswordHash_UpdatesPasswordHash()
        {
            var user = UserTestData.CreateUser();
            var newPasswordHash = UserTestData.CreatePasswordHash() + "_updated";

            user.SetPasswordHash(newPasswordHash);

            user.PasswordHash.Should().Be(newPasswordHash);
        }

        [Fact]
        public void CanLogin_WithInactiveUser_ReturnsFalse()
        {
            var user = UserTestData.CreateUser();
            user.Update(
                user.Email!,
                user.PasswordHash!,
                user.FirstName,
                user.LastName,
                isActive: false);

            bool canLogin = user.CanLogin();

            canLogin.Should().BeFalse();
        }

        [Fact]
        public void CanLogin_WithoutEmail_ReturnsFalse()
        {
            var user = UserTestData.CreateUserWithoutEmail();

            bool canLogin = user.CanLogin();

            canLogin.Should().BeFalse();
        }

        [Fact]
        public void CanLogin_WithoutPasswordHash_ReturnsFalse()
        {
            var user = UserTestData.CreateUserWithoutPasswordHash();

            bool canLogin = user.CanLogin();

            canLogin.Should().BeFalse();
        }

        [Fact]
        public void CanLogin_WithMergedUser_ReturnsFalse()
        {
            var user = UserTestData.CreateUser();
            user.MergeIntoDifferentUser(UserTestData.CreateUserId(), UserTestData.CreateUserId());

            bool canLogin = user.CanLogin();

            canLogin.Should().BeFalse();
        }

        [Fact]
        public void CanLogin_WithActiveUserAndCredentials_ReturnsTrue()
        {
            var user = UserTestData.CreateUser();

            bool canLogin = user.CanLogin();

            canLogin.Should().BeTrue();
        }

        [Fact]
        public void CanBeClaimed_WithInactiveUser_ReturnsFalse()
        {
            var user = UserTestData.CreateImportedUser(isActive: false);

            bool canBeClaimed = user.CanBeClaimed();

            canBeClaimed.Should().BeFalse();
        }

        [Fact]
        public void CanBeClaimed_WithEmailAddress_ReturnsFalse()
        {
            var user = UserTestData.CreateUserWithoutPasswordHash();

            bool canBeClaimed = user.CanBeClaimed();

            canBeClaimed.Should().BeFalse();
        }

        [Fact]
        public void CanBeClaimed_WithPasswordHash_ReturnsFalse()
        {
            var user = UserTestData.CreateUserWithoutEmail();

            bool canBeClaimed = user.CanBeClaimed();

            canBeClaimed.Should().BeFalse();
        }

        [Fact]
        public void CanBeClaimed_WithMergedUser_ReturnsFalse()
        {
            var user = UserTestData.CreateImportedUser();
            user.MergeIntoDifferentUser(UserTestData.CreateUserId(), UserTestData.CreateUserId());

            bool canBeClaimed = user.CanBeClaimed();

            canBeClaimed.Should().BeFalse();
        }

        [Fact]
        public void CanBeClaimed_WithActiveUserAndWithoutCredentials_ReturnsTrue()
        {
            var user = UserTestData.CreateImportedUser();

            bool canBeClaimed = user.CanBeClaimed();

            canBeClaimed.Should().BeTrue();
        }

        [Fact]
        public void Claim_WithEmailAndPasswordHash_ThrowsInvalidOperationException()
        {
            var user = UserTestData.CreateUser();

            Action act = () => user.Claim(
                UserTestData.CreateEmail(),
                UserTestData.CreatePasswordHash(),
                UserTestData.CreateFirstName(),
                UserTestData.CreateLastName());

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Claim_WithClaimableUser_SetsEmailAndPasswordHash()
        {
            var user = UserTestData.CreateImportedUser();

            user.Claim(
                UserTestData.CreateEmail(),
                UserTestData.CreatePasswordHash(),
                UserTestData.CreateFirstName(),
                UserTestData.CreateLastName());

            user.HasEmail.Should().BeTrue();
            user.HasPassword.Should().BeTrue();
        }

        [Fact]
        public void MergeIntoDifferentUser_WithEmptyMergeTargetUserId_ThrowsArgumentException()
        {
            var user = UserTestData.CreateUser();
            var emptyMergeTargetUserId = Guid.Empty;

            Action act = () => user.MergeIntoDifferentUser(emptyMergeTargetUserId, Guid.NewGuid());

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void MergeIntoDifferentUser_WithEmptyMergedByUserId_ThrowsArgumentException()
        {
            var user = UserTestData.CreateUser();
            var emptyMergedByUserId = Guid.Empty;

            Action act = () => user.MergeIntoDifferentUser(Guid.NewGuid(), emptyMergedByUserId);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void MergeIntoDifferentUser_WithValidValues_MarksUserAsMerged()
        {
            var user = UserTestData.CreateUser();
            var mergeTargetUserId = Guid.NewGuid();
            var mergedByUserId = Guid.NewGuid();

            user.MergeIntoDifferentUser(mergeTargetUserId, mergedByUserId);

            user.MergeTargetUserId.Should().Be(mergeTargetUserId);
            user.MergedBy.Should().Be(mergedByUserId);
            user.MergedAt.Should().NotBeNull();
        }

        [Fact]
        public void MergeIntoDifferentUser_WithOwnUserId_ThrowsInvalidOperationException()
        {
            var user = UserTestData.CreateUser();

            Action act = () => user.MergeIntoDifferentUser(user.Id, Guid.NewGuid());

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void MergeIntoDifferentUser_WhenUserAlreadyMerged_ThrowsInvalidOperationException()
        {
            var user = UserTestData.CreateUser();
            user.MergeIntoDifferentUser(Guid.NewGuid(), Guid.NewGuid());

            Action act = () => user.MergeIntoDifferentUser(Guid.NewGuid(), Guid.NewGuid());

            act.Should().Throw<InvalidOperationException>();
        }
    }
}

using FluentAssertions;
using JassTournamentManager.App.Features.Authentication.Models;
using Xunit;

namespace JassTournamentManager.App.Tests.Features.Authentication.Models
{
    public class ClaimableUserListItemTests
    {
        [Fact]
        public void Constructor_WithProvidedValues_StoresValues()
        {
            var id = Guid.NewGuid();
            const string firstName = "Alex";
            const string lastName = "River";

            var user = new ClaimableUserListItem(id, firstName, lastName);

            user.Id.Should().Be(id);
            user.FirstName.Should().Be(firstName);
            user.LastName.Should().Be(lastName);
        }

        [Theory]
        [InlineData("Alex", "River")]
        [InlineData("Anna Maria", "von Bergen")]
        [InlineData("Élodie", "Müller")]
        public void DisplayName_WithProvidedNames_JoinsNamesWithSpace(string firstName, string lastName)
        {
            var user = new ClaimableUserListItem(Guid.NewGuid(), firstName, lastName);

            user.DisplayName.Should().Be($"{firstName} {lastName}");
        }
    }
}
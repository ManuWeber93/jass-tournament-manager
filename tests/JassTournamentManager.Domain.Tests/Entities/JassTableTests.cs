using FluentAssertions;
using JassTournamentManager.Domain.Entities;
using JassTournamentManager.Domain.Tests.TestHelpers;

namespace JassTournamentManager.Domain.Tests.Entities
{
    public class JassTableTests
    {
        [Fact]
        public void Constructor_WithEmptyOrganizerId_ThrowsArgumentException()
        {
            var emptyOrganizerId = Guid.Empty;

            Action act = () => new JassTable(
                emptyOrganizerId,
                JassTableTestData.CreateJassTableName(),
                JassTableTestData.CreateDisplayOrder());

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithEmptyName_ThrowsArgumentException()
        {
            var emptyName = "  ";

            Action act = () => new JassTable(
                UserTestData.CreateUserId(),
                emptyName,
                JassTableTestData.CreateDisplayOrder());

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithTooLongName_ThrowsArgumentOutOfRangeException()
        {
            var tooLongJassTableName = "Jasstisch im grossen Saal, direkt neben dem Fenster mit Blick auf die Bühne und reserviert für Finalrunden";

            Action act = () => new JassTable(
                UserTestData.CreateUserId(),
                tooLongJassTableName,
                JassTableTestData.CreateDisplayOrder());

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_WithNegativeDisplayOrder_ThrowsArgumentOutOfRangeException()
        {
            var negativeDisplayOrder = -1;

            Action act = () => new JassTable(
                UserTestData.CreateUserId(),
                JassTableTestData.CreateJassTableName(),
                negativeDisplayOrder);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_WithValidValues_CreatesJassTable()
        {
            var organizerId = UserTestData.CreateUserId();
            var name = JassTableTestData.CreateJassTableName();
            var displayOrder = JassTableTestData.CreateDisplayOrder();
            var isActive = false;

            var jassTable = new JassTable(organizerId, name, displayOrder, isActive);

            jassTable.OrganizerId.Should().Be(organizerId);
            jassTable.Name.Should().Be(name);
            jassTable.DisplayOrder.Should().Be(displayOrder);
            jassTable.IsActive.Should().Be(isActive);
        }

        [Fact]
        public void Constructor_DefaultIsActive_ShouldBeTrue()
        {
            var jassTable = JassTableTestData.CreateJassTable();

            jassTable.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Rename_WithEmptyName_ThrowsArgumentException()
        {
            var jassTable = JassTableTestData.CreateJassTable();
            var emptyName = "  ";

            Action act = () => jassTable.Rename(emptyName);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Rename_WithNonEmptyName_ShouldRenameJassTable()
        {
            var jassTable = JassTableTestData.CreateJassTable();
            var newName = "new Name";
            jassTable.Rename(newName);

            jassTable.Name.Should().Be(newName);
        }

        [Fact]
        public void ChangeDisplayOrder_WithNegativeDisplayOrder_ThrowsArgumentOutOfRangeException()
        {
            var jassTable = JassTableTestData.CreateJassTable();
            var negativeDisplayOrder = -1;

            Action act = () => jassTable.ChangeDisplayOrder(negativeDisplayOrder);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ChangeDisplayOrder_WithValidDisplayOrder_ShouldChangeDisplayOrder()
        {
            var jassTable = JassTableTestData.CreateJassTable();
            var newDisplayOrder = 2;
            jassTable.ChangeDisplayOrder(newDisplayOrder);

            jassTable.DisplayOrder.Should().Be(newDisplayOrder);
        }

        [Fact]
        public void SetIsActive_ShouldChangeIsActive()
        {
            var jassTable = JassTableTestData.CreateJassTable();
            var newIsActive = false;
            jassTable.SetIsActive(newIsActive);

            jassTable.IsActive.Should().Be(newIsActive);
        }
    }
}

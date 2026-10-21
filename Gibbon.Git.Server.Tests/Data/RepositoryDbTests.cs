using System.Collections.Generic;
using System.Linq;

using Gibbon.Git.Server.Data.Entities;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Tests.TestHelper;

using Microsoft.EntityFrameworkCore;

namespace Gibbon.Git.Server.Tests.Data;

[TestClass]
[TestCategory(TestCategories.Database)]
public class SqliteRepositoryDbTests : GibbonDbContextTestsBase<SqliteConnectionFactory>
{
    [TestMethod]
    public void CanAddRepository()
    {
        // Arrange
        var repository = new Repository {  Name = "TestRepo" };

        // Act
        Context.Repositories.Add(repository);
        Context.SaveChanges();

        // Assert
        var savedRepo = Context.Repositories.FirstOrDefault(r => r.Name == "TestRepo");
        Assert.IsNotNull(savedRepo);
        Assert.AreEqual("TestRepo", savedRepo.Name);
    }

    [TestMethod]
    public void RepositoryNameMustBeUnique()
    {
        // Arrange
        var repo1 = new Repository {  Name = "UniqueRepo" };
        var repo2 = new Repository {  Name = "UniqueRepo" };

        // Act
        Context.Repositories.Add(repo1);
        Context.SaveChanges();

        // Assert
        Context.Repositories.Add(repo2);
        Assert.ThrowsExactly<DbUpdateException>(() => Context.SaveChanges());
    }

    [TestMethod]
    public void CanAddUserWithRole()
    {
        // Arrange
        var role = new Role {  Name = "Admin" };
        var user = new User
        {
            
            Username = "TestUser",
            GivenName = "Test",
            Surname = "User",
            Email = "example@example.org",
            Password = "TestPassword",
            PasswordSalt = "TestSalt",
            Roles = [role]
        };

        // Act
        Context.Users.Add(user);
        Context.SaveChanges();

        // Assert
        var savedUser = Context.Users.Include(u => u.Roles).FirstOrDefault(u => u.Username == "TestUser");
        Assert.IsNotNull(savedUser);
        Assert.IsTrue(savedUser.Roles.Any(r => r.Name == "Admin"));
    }

    [TestMethod]
    public void CanDeleteRepository()
    {
        // Arrange
        var repository = new Repository {  Name = "TestRepo" };
        Context.Repositories.Add(repository);
        Context.SaveChanges();

        // Act
        Context.Repositories.Remove(repository);
        Context.SaveChanges();

        // Assert
        var deletedRepo = Context.Repositories.FirstOrDefault(r => r.Name == "TestRepo");
        Assert.IsNull(deletedRepo);
    }

    [TestMethod]
    public void CanAddTeamWithRepository()
    {
        // Arrange
        var repository = new Repository {  Name = "TestRepo" };
        var team = new Team
        {
            
            Name = "Dev Team",
            Repositories = [repository]
        };

        // Act
        Context.Teams.Add(team);
        Context.SaveChanges();

        // Assert
        var savedTeam = Context.Teams.Include(t => t.Repositories).FirstOrDefault(t => t.Name == "Dev Team");
        Assert.IsNotNull(savedTeam);
        Assert.IsTrue(savedTeam.Repositories.Any(r => r.Name == "TestRepo"));
    }

    [TestMethod]
    public void CanAssignAdministratorToRepository()
    {
        // Arrange
        var repository = new Repository {  Name = "AdminRepo" };
        var admin = new User
        {
            
            Username = "AdminUser",
            GivenName = "Admin",
            Surname = "User",
            Email = "admin@example.org",
            Password = "AdminPassword",
            PasswordSalt = "AdminSalt"
        };

        // Act
        repository.Users.Add(admin);
        Context.Repositories.Add(repository);
        Context.SaveChanges();
        repository.UserPermissions.Single().AccessLevel = RepositoryAccessLevel.Administer;
        Context.SaveChanges();

        // Assert
        var savedRepo = Context.Repositories.Include(r => r.UserPermissions).FirstOrDefault(r => r.Name == "AdminRepo");
        Assert.IsNotNull(savedRepo);
        Assert.IsTrue(savedRepo.UserPermissions.Any(p => p.UserId == admin.Id && p.AccessLevel == RepositoryAccessLevel.Administer));
    }

    [TestMethod]
    public void CanAddMultipleUsersToRepository()
    {
        // Arrange
        var repository = new Repository {  Name = "TestRepo" };
        var user1 = new User
        {
            
            Username = "User1",
            GivenName = "Test1",
            Surname = "User1",
            Email = "user1@example.org",
            Password = "Password1",
            PasswordSalt = "Salt1"
        };
        var user2 = new User
        {
            
            Username = "User2",
            GivenName = "Test2",
            Surname = "User2",
            Email = "user2@example.org",
            Password = "Password2",
            PasswordSalt = "Salt2"
        };

        // Act
        repository.Users.Add(user1);
        repository.Users.Add(user2);
        Context.Repositories.Add(repository);
        Context.SaveChanges();

        // Assert
        var savedRepo = Context.Repositories.Include(r => r.Users).FirstOrDefault(r => r.Name == "TestRepo");
        Assert.IsNotNull(savedRepo);
        Assert.IsTrue(savedRepo.Users.Any(u => u.Username == "User1"));
        Assert.IsTrue(savedRepo.Users.Any(u => u.Username == "User2"));
    }
    [TestMethod]
    public void TransactionRollbackOnRepositoryFailure()
    {
        // Arrange
        using var transaction = Context.Database.BeginTransaction();

        var repository = new Repository {  Name = "RollbackRepo" };
        var team = new Team {  Name = "RollbackTeam" };

        Context.Repositories.Add(repository);
        Context.SaveChanges();

        // Act
        try
        {
            team.Repositories.Add(repository);
            Context.Teams.Add(team);

            repository.Name = null; 

            Context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            transaction.Rollback();
        }

        // Assert
        var savedRepo = Context.Repositories.FirstOrDefault(r => r.Name == "RollbackRepo");
        Assert.IsNull(savedRepo);
    }
    [TestMethod]
    public void DeleteRepositoryRemovesOnlyRelationsNotUsersOrTeams()
    {
        // Arrange
        var repository = new Repository {  Name = "RelationTestRepo" };
        var admin = new User {  Username = "AdminUser", GivenName = "Admin", Surname = "User", Email = "admin@example.com", Password = "password", PasswordSalt = "salt" };
        var team = new Team {  Name = "RelationTestTeam" };

        repository.Users.Add(admin);
        repository.Teams.Add(team);

        Context.Repositories.Add(repository);
        Context.SaveChanges();

        // Act
        Context.Repositories.Remove(repository);
        Context.SaveChanges();

        // Assert
        var deletedRepo = Context.Repositories.FirstOrDefault(r => r.Name == "RelationTestRepo");
        Assert.IsNull(deletedRepo);

        var existingAdmin = Context.Users.FirstOrDefault(u => u.Username == "AdminUser");
        var existingTeam = Context.Teams.FirstOrDefault(t => t.Name == "RelationTestTeam");
        Assert.IsNotNull(existingAdmin);
        Assert.IsNotNull(existingTeam);

        var remainingAdminRelation = Context.Set<RepositoryUserPermission>("UserRepository_Permission")
            .FirstOrDefault(relation => relation.UserId == admin.Id);
        var remainingTeamRelation = Context.Set<RepositoryTeamPermission>("TeamRepository_Permission")
            .FirstOrDefault(relation => relation.TeamId == team.Id);

        Assert.IsNull(remainingAdminRelation);
        Assert.IsNull(remainingTeamRelation);
    }
    [TestMethod]
    public void DeleteUserOrTeamDoesNotDeleteRepository()
    {
        // Arrange
        var repository = new Repository {  Name = "TestRepo" };
        var user = new User {  Username = "TestUser", GivenName = "Test", Surname = "User", Email = "testuser@example.com", Password = "password", PasswordSalt = "salt" };
        var team = new Team {  Name = "TestTeam" };

        repository.Users.Add(user);
        repository.Teams.Add(team);

        Context.Repositories.Add(repository);
        Context.Users.Add(user);
        Context.Teams.Add(team);
        Context.SaveChanges();

        // Act
        Context.Users.Remove(user);
        Context.Teams.Remove(team);
        Context.SaveChanges();

        // Assert
        var remainingRepo = Context.Repositories.FirstOrDefault(r => r.Name == "TestRepo");
        Assert.IsNotNull(remainingRepo);

        var remainingUserRelation = Context.Set<RepositoryUserPermission>("UserRepository_Permission")
            .FirstOrDefault(relation => relation.UserId == user.Id);
        var remainingTeamRelation = Context.Set<RepositoryTeamPermission>("TeamRepository_Permission")
            .FirstOrDefault(relation => relation.TeamId == team.Id);

        Assert.IsNull(remainingUserRelation);
        Assert.IsNull(remainingTeamRelation);
    }

    [TestMethod]
    public void RepositoryDataIntegrityWithUsersTeamsAndAdmins()
    {
        // Arrange
        var repository = new Repository {  Name = "DataIntegrityRepo" };
        var user = new User {  Username = "DataUser", GivenName = "User", Surname = "Test", Email = "user@example.com", Password = "password", PasswordSalt = "salt" };
        var team = new Team {  Name = "DataTeam" };
        var admin = new User {  Username = "AdminUser", GivenName = "Admin", Surname = "Test", Email = "admin@example.com", Password = "password", PasswordSalt = "salt" };

        repository.Users.Add(user);
        repository.Teams.Add(team);
        repository.Users.Add(admin);

        Context.Repositories.Add(repository);
        Context.SaveChanges();
        repository.UserPermissions.Single(p => p.UserId == admin.Id).AccessLevel = RepositoryAccessLevel.Administer;
        Context.SaveChanges();

        // Act
        var savedRepo = Context.Repositories
            .Include(r => r.Users)
            .Include(r => r.Teams)
            .Include(r => r.UserPermissions)
            .FirstOrDefault(r => r.Name == "DataIntegrityRepo");

        // Assert
        Assert.IsNotNull(savedRepo);
        Assert.AreEqual(2, savedRepo.Users.Count);
        Assert.AreEqual(1, savedRepo.Teams.Count);
        Assert.IsTrue(savedRepo.Users.Any(u => u.Username == "DataUser"));
        Assert.IsTrue(savedRepo.Teams.Any(t => t.Name == "DataTeam"));
        Assert.AreEqual(RepositoryAccessLevel.Push, savedRepo.UserPermissions.Single(p => p.UserId == user.Id).AccessLevel);
        Assert.AreEqual(RepositoryAccessLevel.Administer, savedRepo.UserPermissions.Single(p => p.UserId == admin.Id).AccessLevel);
    }
    [TestMethod]
    public void CompleteRepositoryLifecycleTest()
    {
        // Arrange 
        var repository = new Repository {  Name = "LifecycleRepo" };
        var admin = new User {  Username = "AdminLifecycle", GivenName = "Admin", Surname = "Test", Email = "admin@example.com", Password = "password", PasswordSalt = "salt" };
        var user = new User {  Username = "UserLifecycle", GivenName = "User", Surname = "Test", Email = "user@example.com", Password = "password", PasswordSalt = "salt" };
        var team = new Team {  Name = "LifecycleTeam" };

        // Act: Create
        repository.Users.Add(admin);
        repository.Users.Add(user);
        repository.Teams.Add(team);

        Context.Repositories.Add(repository);
        Context.SaveChanges();

        // Assert
        var savedRepo = Context.Repositories.Include(r => r.Users).Include(r => r.Teams).FirstOrDefault(r => r.Name == "LifecycleRepo");
        Assert.IsNotNull(savedRepo);
        Assert.IsTrue(savedRepo.Users.Any(u => u.Username == "UserLifecycle"));
        Assert.IsTrue(savedRepo.Users.Any(u => u.Username == "AdminLifecycle"));
        Assert.IsTrue(savedRepo.Teams.Any(t => t.Name == "LifecycleTeam"));

        // Act: Update
        savedRepo.Name = "UpdatedRepo";
        Context.Repositories.Update(savedRepo);
        Context.SaveChanges();

        // Assert
        var updatedRepo = Context.Repositories.FirstOrDefault(r => r.Name == "UpdatedRepo");
        Assert.IsNotNull(updatedRepo);

        // Act: Delete
        Context.Repositories.Remove(updatedRepo);
        Context.SaveChanges();

        // Assert
        var deletedRepo = Context.Repositories.FirstOrDefault(r => r.Name == "UpdatedRepo");
        Assert.IsNull(deletedRepo);
    }
    [TestMethod]
    public void RepositoryDeleteFailsRollbackUsersAndTeams()
    {
        // Arrange
        var repository = new Repository {  Name = "FailRepo" };
        var user = new User {  Username = "FailUser", GivenName = "User", Surname = "Fail", Email = "fail@example.com", Password = "password", PasswordSalt = "salt" };
        var team = new Team {  Name = "FailTeam" };

        repository.Users.Add(user);
        repository.Teams.Add(team);

        Context.Repositories.Add(repository);
        Context.SaveChanges();

        using var transaction = Context.Database.BeginTransaction();
        // Act
        repository.Users.Clear();
        repository.Teams.Clear();

        Context.Repositories.Remove(repository);
        Context.SaveChanges();

        transaction.Rollback();

        // Assert
        var remainingUser = Context.Users.FirstOrDefault(u => u.Username == "FailUser");
        var remainingTeam = Context.Teams.FirstOrDefault(t => t.Name == "FailTeam");
        Assert.IsNotNull(remainingUser);
        Assert.IsNotNull(remainingTeam);
    }
    [TestMethod]
    public void CanAddAndRemoveUserFromRepository()
    {
        // Arrange
        var repository = new Repository {  Name = "TestRepo" };
        var user = new User
        {
            
            Username = "TestUser",
            GivenName = "Test",
            Surname = "User",
            Email = "testuser@example.com",
            Password = "password",
            PasswordSalt = "salt"
        };

        Context.Repositories.Add(repository);
        Context.Users.Add(user);
        Context.SaveChanges();

        // Act
        repository.Users.Add(user);
        Context.SaveChanges();

        // Assert
        var savedRepo = Context.Repositories.Include(r => r.Users).FirstOrDefault(r => r.Name == "TestRepo");
        Assert.IsNotNull(savedRepo);
        Assert.IsTrue(savedRepo.Users.Any(u => u.Username == "TestUser"));

        // Act
        savedRepo.Users.Remove(user);
        Context.SaveChanges();

        // Assert
        var updatedRepo = Context.Repositories.Include(r => r.Users).FirstOrDefault(r => r.Name == "TestRepo");
        Assert.IsNotNull(updatedRepo);
        Assert.IsFalse(updatedRepo.Users.Any(u => u.Username == "TestUser"));
    }
    [TestMethod]
    public void CanAddTwoTeamsAndRemoveFirstFromRepository()
    {
        // Arrange
        var repository = new Repository {  Name = "TestRepo" };
        var team1 = new Team {  Name = "FirstTeam" };
        var team2 = new Team {  Name = "SecondTeam" };

        Context.Repositories.Add(repository);
        Context.Teams.Add(team1);
        Context.Teams.Add(team2);
        Context.SaveChanges();

        // Act
        repository.Teams.Add(team1);
        Context.SaveChanges();

        // Act
        repository.Teams.Add(team2);
        Context.SaveChanges();

        // Assert
        var savedRepo = Context.Repositories.Include(r => r.Teams).FirstOrDefault(r => r.Name == "TestRepo");
        Assert.IsNotNull(savedRepo);
        Assert.IsTrue(savedRepo.Teams.Any(t => t.Name == "FirstTeam"));
        Assert.IsTrue(savedRepo.Teams.Any(t => t.Name == "SecondTeam"));

        // Act
        savedRepo.Teams.Remove(team1);
        Context.SaveChanges();

        // Assert
        var updatedRepo = Context.Repositories.Include(r => r.Teams).FirstOrDefault(r => r.Name == "TestRepo");
        Assert.IsNotNull(updatedRepo);
        Assert.IsFalse(updatedRepo.Teams.Any(t => t.Name == "FirstTeam"));
        Assert.IsTrue(updatedRepo.Teams.Any(t => t.Name == "SecondTeam"));
    }
}

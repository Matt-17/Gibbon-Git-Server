using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Extensions;
using Gibbon.Git.Server.Git.GitService;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Services;

using Microsoft.Extensions.Options;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.Git;

[TestClass]
public class GitServiceExecutorTests
{
    private IPathResolver _pathResolver = null!;
    private ITeamService _teamService = null!;
    private IRoleProvider _roleProvider = null!;
    private IUserService _userService = null!;
    private GitService _executor = null!;
    private GitSettings _gitSettings = null!;
    private IProcessService _processService = null!;

    [TestInitialize]
    public void Setup()
    {
        _pathResolver = Substitute.For<IPathResolver>();
        _teamService = Substitute.For<ITeamService>();
        _roleProvider = Substitute.For<IRoleProvider>();
        _userService = Substitute.For<IUserService>();
        _processService = Substitute.For<IProcessService>();

        _gitSettings = new GitSettings
        {
            BinaryPath = "fakeGitPath",
            HomePath = "/fakeHomePath",
        };

        var options = Options.Create(_gitSettings);

        _executor = new GitService(options, _processService, _pathResolver, _teamService, _roleProvider, _userService);
    }

    [TestMethod]
    [DataRow("upload-pack", true)]
    [DataRow("receive-pack", true)]
    [DataRow("invalid-service", false)]
    public async Task ExecuteServiceByName_ServiceNameValidation_ThrowsOrSucceeds(string serviceName, bool isValid)
    {
        // Arrange
        var repositoryName = "repo";
        var inStream = new MemoryStream();
        var outStream = new MemoryStream();
        var userName = "testUser";
        var options = new ExecutionOptions(true, true);
        var userId = 1;

        SetupInfo("TeamA", userId, userName);
        _pathResolver.Resolve(Arg.Any<string>(), Arg.Any<string>()).Returns("/fakeGitPath");
        _pathResolver.GetRepositoryPath(Arg.Any<string>()).Returns("/fakeRepoDir/repo");

        // Act & Assert
        if (isValid)
        {
            await _executor.ExecuteServiceByName(repositoryName, serviceName, options, inStream, outStream, userId);
            await _processService.Received(1).StartProcessWithStreamAsync(Arg.Any<ProcessStartInfo>(), inStream, outStream, true);
        }
        else
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                _executor.ExecuteServiceByName(repositoryName, serviceName, options, inStream, outStream, userId)
            );
        }
    }

    [TestMethod]
    [DataRow(true, true, " --advertise-refs")]
    [DataRow(true, false, " --advertise-refs")]
    [DataRow(false, true, "")]
    [DataRow(false, false, "")]
    public async Task ExecuteServiceByName_CommandLineArgs_ValidatesAdvertiseRefsArgument(bool advertiseRefs, bool endStreamWithClose, string expectedArgs)
    {
        // Arrange
        var serviceName = "upload-pack";
        var repositoryName = "repo";
        var inStream = new MemoryStream();
        var outStream = new MemoryStream();
        var userName = "testUser";
        var options = new ExecutionOptions(advertiseRefs, endStreamWithClose);
        var userId = 1;

        SetupInfo("TeamA", userId, userName);
        _pathResolver.Resolve(Arg.Any<string>(), Arg.Any<string>()).Returns("/fakeGitPath");
        _pathResolver.GetRepositoryPath(Arg.Any<string>()).Returns("/fakeRepoDir/repo");

        // Act
        await _executor.ExecuteServiceByName(repositoryName, serviceName, options, inStream, outStream, userId);

        // Assert
        await _processService.Received(1).StartProcessWithStreamAsync(Arg.Is<ProcessStartInfo>(p =>
            p.Arguments.Contains(expectedArgs)
        ), inStream, outStream, endStreamWithClose);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ExecuteServiceByName_EndStreamWithClose_ClosesOrContinues(bool endStreamWithClose)
    {
        // Arrange
        var serviceName = "upload-pack";
        var repositoryName = "repo";
        var inStream = new MemoryStream();
        var outStream = new MemoryStream();
        var userName = "testUser";
        var options = new ExecutionOptions(true, endStreamWithClose);
        var userId = 1;

        SetupInfo("TeamA", userId, userName);
        _pathResolver.Resolve(Arg.Any<string>(), Arg.Any<string>()).Returns("/fakeGitPath");
        _pathResolver.GetRepositoryPath(Arg.Any<string>()).Returns("/fakeRepoDir/repo");

        // Act
        await _executor.ExecuteServiceByName(repositoryName, serviceName, options, inStream, outStream, userId);

        // Assert
        await _processService.Received(1).StartProcessWithStreamAsync(Arg.Any<ProcessStartInfo>(), inStream, outStream, endStreamWithClose);
    }

    [TestMethod]
    [DataRow("TeamA", "Role1", "User, Test")]
    [DataRow("TeamB", "Role2", "User, Test")]
    public async Task ExecuteServiceByName_WithUserTeamsAndRoles_SetsCorrectEnvironmentVariables(string teamName, string roleName, string displayName)
    {
        // Arrange
        var serviceName = "upload-pack";
        var repositoryName = "repo";
        var inStream = new MemoryStream();
        var outStream = new MemoryStream();
        var userName = "testUser";
        var options = new ExecutionOptions(true, true);
        var userId = 1;

        SetupInfo(teamName, userId, userName, roleName);
        _pathResolver.Resolve(Arg.Any<string>(), Arg.Any<string>()).Returns("/fakeGitPath");
        _pathResolver.GetRepositoryPath(Arg.Any<string>()).Returns("/fakeRepoDir/repo");

        // Act
        await _executor.ExecuteServiceByName(repositoryName, serviceName, options, inStream, outStream, userId);

        // Assert
        _teamService.Received(1).GetTeamsForUser(userId);
        _roleProvider.Received(1).GetRolesForUser(userId);
        _userService.Received(1).GetUserModel(userId);

        var expectedTeamsStr = new[] { teamName }.StringlistToEscapedStringForEnvVar();
        var expectedRolesStr = new[] { roleName }.StringlistToEscapedStringForEnvVar();
        await _processService.Received(1).StartProcessWithStreamAsync(Arg.Is<ProcessStartInfo>(p =>
            p.EnvironmentVariables["AUTH_USER"] == userName &&
            p.EnvironmentVariables["AUTH_USER_TEAMS"] == expectedTeamsStr &&
            p.EnvironmentVariables["AUTH_USER_ROLES"] == expectedRolesStr &&
            p.EnvironmentVariables["AUTH_USER_DISPLAYNAME"] == displayName
        ), inStream, outStream, options.EndStreamWithClose);
    }

    [TestMethod]
    public async Task ExecuteServiceByName_StartsConfiguredExecutableWithResolvedHome()
    {
        _gitSettings.Executable = "/opt/git/bin/git";
        _executor = new GitService(Options.Create(_gitSettings), _processService, _pathResolver, _teamService, _roleProvider, _userService);
        _pathResolver.ResolveExecutable("/opt/git/bin/git").Returns("/opt/git/bin/git");
        _pathResolver.ResolveRoot("/fakeHomePath").Returns("/resolved/home");
        _pathResolver.GetRepositoryPath(Arg.Any<string>()).Returns("/fakeRepoDir/repo");

        await _executor.ExecuteServiceByName("repo", "upload-pack", new ExecutionOptions(true, true), new MemoryStream(), new MemoryStream(), 0);

        await _processService.Received(1).StartProcessWithStreamAsync(Arg.Is<ProcessStartInfo>(p =>
            p.FileName == "/opt/git/bin/git" &&
            p.EnvironmentVariables["HOME"] == "/resolved/home"
        ), Arg.Any<Stream>(), Arg.Any<Stream>(), true);
    }

    [TestMethod]
    public void GitSettings_DefaultExecutable_IsGitOnPath()
    {
        Assert.AreEqual("git", new GitSettings().Executable);
    }

    private void SetupInfo(string teamName, int userId, string userName, params string[] roleName)
    {
        List<TeamModel> teams = [
            new() { Name = teamName }
        ];
        _roleProvider.GetRolesForUser(userId).Returns(roleName);
        _teamService.GetTeamsForUser(userId).Returns(teams);
        _userService.GetUserModel(userId).Returns(new UserModel
        {
            Id = userId,
            GivenName = "Test",
            Surname = "User",
            Username = userName,
        });
    }
}

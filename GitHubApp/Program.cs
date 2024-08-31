using Microsoft.Extensions.Configuration;
using Octokit;

var builder = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json", true, true)
    .AddUserSecrets<Program>();
var configuration = builder.Build();
var settings = configuration.GetSection("GitHubApp").Get<GitHubAppSettings>();

// Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
var generator = new GitHubJwt.GitHubJwtFactory(
    new GitHubJwt.StringPrivateKeySource(settings?.PrivateKey),
    new GitHubJwt.GitHubJwtFactoryOptions
    {
        AppIntegrationId = settings?.AppId ?? -1, // The GitHub App Id
        ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
    }
);

var jwtToken = generator.CreateEncodedJwtToken();

// Pass the JWT as a Bearer token to Octokit.net
var appClient = new GitHubClient(new ProductHeaderValue(settings?.AppName))
{
    Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
};

// Get a list of installations for the authenticated GitHubApp
var installations = await appClient.GitHubApps.GetAllInstallationsForCurrent();

var installation = installations.First(i => i.Account.Login == settings?.InstallationAccount);

// Create an Installation token for Installation Id 123
var response = await appClient.GitHubApps.CreateInstallationToken(installation.Id);

// Create a new GitHubClient using the installation token as authentication
var installationClient = new GitHubClient(new ProductHeaderValue(settings?.AppName))
{
    Credentials = new Credentials(response.Token)
};

var commits = installationClient.Repository.Commit.GetAll(
    settings?.InstallationAccount,
    settings?.RepositoryName
);
var latestCommit = commits.Result.First();

Console.WriteLine($"Latest commit SHA: {latestCommit.Sha}");

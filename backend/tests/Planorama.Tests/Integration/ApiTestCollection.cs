using Xunit;

namespace Planorama.Tests.Integration;

/// <summary>
/// Forces every test class sharing this collection onto one <see cref="PlanoramaWebApplicationFactory"/>
/// instance instead of one-per-class — xUnit runs different collections in parallel by default, and two
/// independently-built hosts race on Serilog's static reloadable logger ("The logger is already frozen").
/// </summary>
[CollectionDefinition("Api")]
public class ApiTestCollection : ICollectionFixture<PlanoramaWebApplicationFactory>;

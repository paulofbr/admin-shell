using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AdminShell.Repository.Tests;

[CollectionDefinition("RepositoryTests")]
public class RepositoryTestCollection : ICollectionFixture<DatabaseFixture>
{
}
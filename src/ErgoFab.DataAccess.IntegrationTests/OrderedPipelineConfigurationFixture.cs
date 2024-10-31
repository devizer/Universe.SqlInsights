using ErgoFab.DataAccess.IntegrationTests.Shared;
using Universe.NUnitPipeline;

[assembly: NUnitPipelineAction]

namespace ErgoFab.DataAccess.IntegrationTests;

// One time setup fixture per each test project
[SetUpFixture]
public class OrderedPipelineConfigurationFixture
{
    [OneTimeSetUp]
    public void Configure()
    {
        OrderedPipelineConfiguration.Configure();
    }
}
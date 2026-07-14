using CloudCostEngine.Infrastructure;
using Xunit;

namespace CloudCostEngine.Tests;

public class JsonPlanRepositoryTests : IDisposable
{
    private readonly string _tempDir = Directory.CreateTempSubdirectory("cce-tests-").FullName;

    [Fact]
    public void MissingFile_ReturnsFatalErrorWithoutThrowing()
    {
        var repo = new JsonPlanRepository();

        var result = repo.Load(Path.Combine(_tempDir, "does-not-exist.json"));

        Assert.False(result.Success);
        Assert.Contains(result.FatalErrors, e => e.Contains("not found"));
    }

    [Fact]
    public void MalformedJson_ReturnsFatalErrorWithoutThrowing()
    {
        var path = WriteFile("plans.json", "{ this is not valid json ][");
        var repo = new JsonPlanRepository();

        var result = repo.Load(path);

        Assert.False(result.Success);
        Assert.NotEmpty(result.FatalErrors);
    }

    [Fact]
    public void ValidFile_LoadsAllPlans()
    {
        var path = WriteFile("plans.json", SpecExampleJson);
        var repo = new JsonPlanRepository();

        var result = repo.Load(path);

        Assert.True(result.Success);
        Assert.Single(result.Plans);
        Assert.Equal("NimbusCloud", result.Plans[0].ProviderName);
    }

    [Fact]
    public void OneInvalidPlanAmongValidOnes_SkipsOnlyTheInvalidOne()
    {
        var json = $$"""
        [
            {{SpecExampleJson.Trim('[', ']')}},
            {
                "provider_name": "BrokenCloud",
                "plan_name": "BadTiers",
                "base_fee": 500,
                "storage_prices": [ { "rate": 4.0 }, { "rate": 2.0 } ],
                "compute_prices": [ { "rate": 10.0 } ]
            }
        ]
        """;
        var path = WriteFile("mixed.json", json);
        var repo = new JsonPlanRepository();

        var result = repo.Load(path);

        Assert.True(result.Success); // file-level parse succeeded
        Assert.Single(result.Plans); // only the good plan survived
        Assert.NotEmpty(result.PlanWarnings); // the bad one was reported, not silently dropped
    }

    private const string SpecExampleJson = """
    [
        {
            "provider_name": "NimbusCloud",
            "plan_name": "EnterpriseHybrid",
            "base_fee": 1000,
            "storage_prices": [ { "rate": 4.0, "threshold": 500 }, { "rate": 2.0 } ],
            "compute_prices": [ { "rate": 15.0, "threshold": 50 }, { "rate": 10.0 } ]
        }
    ]
    """;

    private string WriteFile(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}

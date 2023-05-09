namespace IFramework.Configuration.ProtectedJson.Test;

public class ConfigurationTest
{
    const string PROTECTEDJSON_SECRET_KEY = "Ym40HQndCh6ZKJQJ";

    const string secretKeyEnvironmentVariableName = "SECRET_KEY";
    const string cipherPrefix = "cipher:";

    IConfiguration Configuration { get; set; }

    public ConfigurationTest()
    {
        Environment.SetEnvironmentVariable(secretKeyEnvironmentVariableName, PROTECTEDJSON_SECRET_KEY);

        //IConfigurationBuilder builder = new ConfigurationBuilder();
        //builder.AddProtectedJsonFile("appsettings.json", true, true);

        //Configuration = builder.Build();

        IHostBuilder hostBuilder = new HostBuilder();
        hostBuilder.ConfigureAppConfiguration((hostingContext, configBuilder) =>
        {
            configBuilder.AddProtectedJsonFile(hostingContext, true, secretKeyEnvironmentVariableName, cipherPrefix);
        });

        var host = hostBuilder.Build();
        Configuration = host.Services.GetService<IConfiguration>();
    }

    [Fact]
    public void GetConfigurationValueFormAppsettingsTest()
    {
        var option1 = Configuration.GetValue<string>("Option1");
        Assert.Equal("123456", option1);
    }

    [Fact]
    public void GetConfigurationValueFormProductionTest()
    {
        var option2 = Configuration.GetValue<string>("Option2");
        Assert.Equal("123456", option2);
    }


    [Fact]
    public void TestWithConfigurationSection()
    {
        var sub1 = Configuration.GetSection("Option3").GetValue<string>("Sub1");
        Assert.Equal("123456", sub1);

        var sub2 = Configuration.GetSection("Option3").GetValue<string>("Sub2");
        Assert.Equal("123456", sub2);
    }
}
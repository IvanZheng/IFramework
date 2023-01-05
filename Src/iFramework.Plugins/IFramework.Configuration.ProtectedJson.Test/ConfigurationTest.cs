namespace IFramework.Configuration.ProtectedJson.Test;

public class ConfigurationTest
{
    const string PROTECTEDJSON_SECRET_KEY = "Ym40HQndCh6ZKJQJ";

    IConfiguration Configuration { get; set; }

    public ConfigurationTest()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder();
        builder.AddProtectedJsonFile("appsettings.json", true, true);
        Environment.SetEnvironmentVariable("PROTECTEDJSON_SECRET_KEY", PROTECTEDJSON_SECRET_KEY);
        //Environment.SetEnvironmentVariable("PROTECTEDJSON_CIPHER_PREFIX", "cipherText2:");
        Configuration = builder.Build();
    }

    [Fact]
    public void Test1()
    {
        var option1 = Configuration.GetValue<string>("Option1");
        Assert.Equal("123456", option1);

        var option2 = Configuration.GetValue<string>("Option2");
        Assert.Equal("123456", option2);
    }

    [Fact]
    public void Test2()
    {
        var sub1 = Configuration.GetSection("Option3").GetValue<string>("Sub1");
        Assert.Equal("123456", sub1);

        var sub2 = Configuration.GetSection("Option3").GetValue<string>("Sub2");
        Assert.Equal("123456", sub2);
    }
}
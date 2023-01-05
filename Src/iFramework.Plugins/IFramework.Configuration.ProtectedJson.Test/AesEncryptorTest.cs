namespace IFramework.Configuration.ProtectedJson.Test;

public class AesEncryptorTest
{
    const string PROTECTEDJSON_SECRET_KEY = "Ym40HQndCh6ZKJQJ";

    private readonly ITestOutputHelper _output;

    public AesEncryptorTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void EncryptTest()
    {
        var cipherText = AesEncryptor.Encrypt("123456", PROTECTEDJSON_SECRET_KEY);

        _output.WriteLine("cipherText:" + cipherText);
    }
}
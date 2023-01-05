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
        string plainText = "123456";
        var cipherText = AesEncryptor.Encrypt("123456", PROTECTEDJSON_SECRET_KEY);
        var decryptedText = AesEncryptor.Decrypt(cipherText, PROTECTEDJSON_SECRET_KEY);

        _output.WriteLine("cipherText:" + cipherText);

        Assert.Equal(decryptedText, plainText);
    }
}
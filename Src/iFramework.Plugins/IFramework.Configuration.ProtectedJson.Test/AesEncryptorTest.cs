using System.Security.Cryptography;
using System.Text;

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

    [Fact]
    public void DecryptTest()
    {
        var cipherText = "9m5racwnjt9m6/jttanONYwylmg5HAsNDewlKVp9eQyjgItkj/QLktkPak7LMq+7fBqabt8AtJ43bMCvLItsc0EgTnKYJwJ0tyPhzCSDcIULbnFIxh53LYxGfT1afPezEEILfeGtLK6WNLImScw+l3JACG+TTzr6kbkrHLbv0hO71uiD00UnyVM7EURzweoBOHC+YFhoO9nF7uH16zKd6aTc2+UscESH188RHHl9udYUEpWMcpOe+TdV81tHCXP/D+OfZh7eFC2BNPqJsJA61EEmdN1CnsyaN1fpgHiOCALw79MvA+4HZZ8b+c3SedbSDS5H+TnsormWf5VyhWXavA==";
        var decryptedText = AesEncryptor.Decrypt(cipherText, PROTECTEDJSON_SECRET_KEY);

        _output.WriteLine("decryptedText:" + decryptedText);

    }
}
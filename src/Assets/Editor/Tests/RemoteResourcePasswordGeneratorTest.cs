using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Remote;

public class RemoteResourcePasswordGeneratorTest
{
    [Test]
    public void Generate_ReturnsValidPassword()
    {
        var generator = new RemoteResourcePasswordGenerator();

        var password = generator.Generate("abcd1234", 1);

        Assert.AreEqual("\x08\x07\x18\x24YWJjZDEyMzQx", password);
    }
}
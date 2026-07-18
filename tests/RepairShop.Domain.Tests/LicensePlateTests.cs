using RepairShop.Domain.Common;
using RepairShop.Domain.ValueObjects;

namespace RepairShop.Domain.Tests;

[TestFixture]
public class LicensePlateTests
{
    [TestCase("M-AB 1234")]
    [TestCase("B-X 1")]
    [TestCase("FS-MY 90")]
    [TestCase("M-AB 1234E")]
    [TestCase("HH-AB 12H")]
    public void Create_ValidPlate_IsAccepted(string input)
    {
        Assert.That(LicensePlate.Create(input).Value, Is.EqualTo(input));
    }

    [TestCase("m-ab 1234", "M-AB 1234")]
    [TestCase("  M-AB 1234  ", "M-AB 1234")]
    [TestCase("M-AB    1234", "M-AB 1234")]
    public void Create_Normalises(string input, string expected)
    {
        Assert.That(LicensePlate.Create(input).Value, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    [TestCase("1234")]
    [TestCase("M AB 1234")]
    [TestCase("MMMM-AB 1234")]
    [TestCase("M-ABC 1234")]
    [TestCase("M-AB 12345")]
    public void Create_InvalidPlate_Throws(string? input)
    {
        Assert.Throws<DomainException>(() => LicensePlate.Create(input));
    }

    [Test]
    public void Equality_IsByValue_NotByReference()
    {
        var a = LicensePlate.Create("M-AB 1234");
        var b = LicensePlate.Create("m-ab 1234");

        Assert.Multiple(() =>
        {
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        });
    }
}

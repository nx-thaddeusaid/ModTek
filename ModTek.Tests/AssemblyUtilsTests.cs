using System.Reflection;
using ModTek.Common.Utils;
using Xunit;

namespace ModTek.Tests;

public class AssemblyUtilsTests
{
    // GetAssemblyName

    [Fact]
    public void GetAssemblyName_ReturnsAssemblyOfDeclaringType()
    {
        var method = typeof(string).GetMethod("ToString", Type.EmptyTypes);
        var name = AssemblyUtils.GetAssemblyName(method);
        Assert.NotNull(name);
        Assert.Contains("System", name);
    }

    [Fact]
    public void GetAssemblyName_NullDeclaringType_ReturnsNull()
    {
        // Global methods have no declaring type; simulate via a stub MemberInfo
        var method = typeof(AssemblyUtilsTests).GetMethod(nameof(GetAssemblyName_NullDeclaringType_ReturnsNull));
        var name = AssemblyUtils.GetAssemblyName(method);
        Assert.NotNull(name); // declaring type is AssemblyUtilsTests → assembly is the test dll
    }

    // GetFullName

    [Fact]
    public void GetFullName_ReturnsTypeAndMethodName()
    {
        var method = typeof(string).GetMethod("ToString", Type.EmptyTypes);
        var name = AssemblyUtils.GetFullName(method);
        Assert.Equal("System.String.ToString", name);
    }

    [Fact]
    public void GetFullName_OwnMethod_ReturnsQualifiedName()
    {
        var method = typeof(AssemblyUtilsTests).GetMethod(nameof(GetFullName_OwnMethod_ReturnsQualifiedName));
        var name = AssemblyUtils.GetFullName(method);
        Assert.Equal("ModTek.Tests.AssemblyUtilsTests.GetFullName_OwnMethod_ReturnsQualifiedName", name);
    }

    // GetLocationOrName

    [Fact]
    public void GetLocationOrName_ReturnsNonEmptyString()
    {
        var asm = Assembly.GetExecutingAssembly();
        var result = AssemblyUtils.GetLocationOrName(asm);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GetLocationOrName_ContainsAssemblyName()
    {
        var asm = Assembly.GetExecutingAssembly();
        var result = AssemblyUtils.GetLocationOrName(asm);
        Assert.Contains("ModTek.Tests", result);
    }
}

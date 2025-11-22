using CppPointerVisualizer.Models;
using FluentAssertions;

namespace CppPointerVisualizer.Tests.Models;

/// <summary>
/// Unit tests for MemoryObject model.
/// Tests business logic and display methods.
/// </summary>
public class MemoryObjectTests
{
    #region GetTypeDescription Tests

    [Fact]
    public void GetTypeDescription_SimpleVariable_ShouldReturnBasicType()
    {
        // Arrange
        var obj = new MemoryObject
        {
            Type = "int",
            ObjectType = MemoryObjectType.Variable,
            IsConst = false
        };

        // Act
        var result = obj.GetTypeDescription();

        // Assert
        result.Should().Be("int");
    }

    [Fact]
    public void GetTypeDescription_ConstVariable_ShouldIncludeConst()
    {
        // Arrange
        var obj = new MemoryObject
        {
            Type = "int",
            ObjectType = MemoryObjectType.Variable,
            IsConst = true
        };

        // Act
        var result = obj.GetTypeDescription();

        // Assert
        result.Should().Be("const int");
    }

    [Fact]
    public void GetTypeDescription_SimplePointer_ShouldIncludeAsterisk()
    {
        // Arrange
        var obj = new MemoryObject
        {
            Type = "int",
            ObjectType = MemoryObjectType.Pointer,
            PointerLevel = 1,
            IsConst = false,
            IsPointerConst = false
        };

        // Act
        var result = obj.GetTypeDescription();

        // Assert
        result.Should().Be("int*");
    }

    [Fact]
    public void GetTypeDescription_DoublePointer_ShouldIncludeTwoAsterisks()
    {
        // Arrange
        var obj = new MemoryObject
        {
            Type = "int",
            ObjectType = MemoryObjectType.Pointer,
            PointerLevel = 2
        };

        // Act
        var result = obj.GetTypeDescription();

        // Assert
        result.Should().Be("int**");
    }

    [Fact]
    public void GetTypeDescription_TriplePointer_ShouldIncludeThreeAsterisks()
    {
        // Arrange
        var obj = new MemoryObject
        {
            Type = "int",
            ObjectType = MemoryObjectType.Pointer,
            PointerLevel = 3
        };

        // Act
        var result = obj.GetTypeDescription();

        // Assert
        result.Should().Be("int***");
    }

    [Fact]
    public void GetTypeDescription_ConstPointerToInt_ShouldShowCorrectly()
    {
        // Arrange
        var obj = new MemoryObject
        {
            Type = "int",
            ObjectType = MemoryObjectType.Pointer,
            PointerLevel = 1,
            IsConst = true,
            IsPointerConst = false
        };

        // Act
        var result = obj.GetTypeDescription();

        // Assert
        result.Should().Be("const int*");
    }

    [Fact]
    public void GetTypeDescription_PointerConst_ShouldShowConstAfterAsterisk()
    {
        // Arrange
        var obj = new MemoryObject
        {
            Type = "int",
            ObjectType = MemoryObjectType.Pointer,
            PointerLevel = 1,
            IsConst = false,
            IsPointerConst = true
        };

        // Act
        var result = obj.GetTypeDescription();

        // Assert
        result.Should().Be("int* const");
    }

    [Fact]
    public void GetTypeDescription_ConstPointerToConst_ShouldShowBothConsts()
    {
        // Arrange
        var obj = new MemoryObject
        {
            Type = "int",
            ObjectType = MemoryObjectType.Pointer,
            PointerLevel = 1,
            IsConst = true,
            IsPointerConst = true
        };

        // Act
        var result = obj.GetTypeDescription();

        // Assert
        result.Should().Be("const int* const");
    }

    [Fact]
    public void GetTypeDescription_SimpleReference_ShouldIncludeAmpersand()
    {
        // Arrange
        var obj = new MemoryObject
        {
            Type = "int",
            ObjectType = MemoryObjectType.Reference,
            IsConst = false
        };

        // Act
        var result = obj.GetTypeDescription();

        // Assert
        result.Should().Be("int &");
    }

    [Fact]
    public void GetTypeDescription_ConstReference_ShouldIncludeConst()
    {
        // Arrange
        var obj = new MemoryObject
        {
            Type = "int",
            ObjectType = MemoryObjectType.Reference,
            IsConst = true
        };

        // Act
        var result = obj.GetTypeDescription();

        // Assert
        result.Should().Be("const int &");
    }

    #endregion

    #region GetModifiabilityInfo Tests

    [Fact]
    public void GetModifiabilityInfo_MutableVariable_ShouldIndicateModifiable()
    {
        // Arrange
        var obj = new MemoryObject
        {
            ObjectType = MemoryObjectType.Variable,
            IsConst = false
        };

        // Act
        var result = obj.GetModifiabilityInfo();

        // Assert
        result.Should().Contain("значение изменяемо");
    }

    [Fact]
    public void GetModifiabilityInfo_ConstVariable_ShouldIndicateNotModifiable()
    {
        // Arrange
        var obj = new MemoryObject
        {
            ObjectType = MemoryObjectType.Variable,
            IsConst = true
        };

        // Act
        var result = obj.GetModifiabilityInfo();

        // Assert
        result.Should().Contain("значение НЕ изменяемо");
    }

    [Fact]
    public void GetModifiabilityInfo_MutablePointer_ShouldShowBothMutable()
    {
        // Arrange
        var obj = new MemoryObject
        {
            ObjectType = MemoryObjectType.Pointer,
            IsConst = false,
            IsPointerConst = false
        };

        // Act
        var result = obj.GetModifiabilityInfo();

        // Assert
        result.Should().Contain("*p изменяемо");
        result.Should().Contain("p изменяемо");
    }

    [Fact]
    public void GetModifiabilityInfo_ConstPointerToInt_ShouldShowValueNotModifiable()
    {
        // Arrange
        var obj = new MemoryObject
        {
            ObjectType = MemoryObjectType.Pointer,
            IsConst = true,
            IsPointerConst = false
        };

        // Act
        var result = obj.GetModifiabilityInfo();

        // Assert
        result.Should().Contain("*p НЕ изменяемо");
        result.Should().Contain("p изменяемо");
    }

    [Fact]
    public void GetModifiabilityInfo_PointerConst_ShouldShowPointerNotModifiable()
    {
        // Arrange
        var obj = new MemoryObject
        {
            ObjectType = MemoryObjectType.Pointer,
            IsConst = false,
            IsPointerConst = true
        };

        // Act
        var result = obj.GetModifiabilityInfo();

        // Assert
        result.Should().Contain("*p изменяемо");
        result.Should().Contain("p НЕ изменяемо");
    }

    [Fact]
    public void GetModifiabilityInfo_FullyConstPointer_ShouldShowBothNotModifiable()
    {
        // Arrange
        var obj = new MemoryObject
        {
            ObjectType = MemoryObjectType.Pointer,
            IsConst = true,
            IsPointerConst = true
        };

        // Act
        var result = obj.GetModifiabilityInfo();

        // Assert
        result.Should().Contain("*p НЕ изменяемо");
        result.Should().Contain("p НЕ изменяемо");
    }

    [Fact]
    public void GetModifiabilityInfo_Reference_ShouldAlwaysShowRefNotModifiable()
    {
        // Arrange
        var obj = new MemoryObject
        {
            ObjectType = MemoryObjectType.Reference,
            IsConst = false
        };

        // Act
        var result = obj.GetModifiabilityInfo();

        // Assert
        result.Should().Contain("ref НЕ изменяемо (всегда)");
        result.Should().Contain("*ref изменяемо");
    }

    [Fact]
    public void GetModifiabilityInfo_ConstReference_ShouldShowValueNotModifiable()
    {
        // Arrange
        var obj = new MemoryObject
        {
            ObjectType = MemoryObjectType.Reference,
            IsConst = true
        };

        // Act
        var result = obj.GetModifiabilityInfo();

        // Assert
        result.Should().Contain("ref НЕ изменяемо (всегда)");
        result.Should().Contain("*ref НЕ изменяемо");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void MemoryObject_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var obj = new MemoryObject();

        // Assert
        obj.Name.Should().Be("");
        obj.Type.Should().Be("");
        obj.Address.Should().Be("");
        obj.Value.Should().BeNull();
        obj.PointsTo.Should().BeNull();
        obj.IsConst.Should().BeFalse();
        obj.IsPointerConst.Should().BeFalse();
        obj.PointerLevel.Should().Be(0);
    }

    [Fact]
    public void MemoryObject_AllProperties_ShouldBeSettable()
    {
        // Arrange & Act
        var obj = new MemoryObject
        {
            Name = "testVar",
            Type = "int",
            Value = 42,
            ObjectType = MemoryObjectType.Pointer,
            Address = "0x1000",
            PointsTo = "0x2000",
            IsConst = true,
            IsPointerConst = true,
            PointerLevel = 2
        };

        // Assert
        obj.Name.Should().Be("testVar");
        obj.Type.Should().Be("int");
        obj.Value.Should().Be(42);
        obj.ObjectType.Should().Be(MemoryObjectType.Pointer);
        obj.Address.Should().Be("0x1000");
        obj.PointsTo.Should().Be("0x2000");
        obj.IsConst.Should().BeTrue();
        obj.IsPointerConst.Should().BeTrue();
        obj.PointerLevel.Should().Be(2);
    }

    #endregion
}

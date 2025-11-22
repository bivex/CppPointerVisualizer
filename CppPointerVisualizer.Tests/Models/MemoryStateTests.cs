using CppPointerVisualizer.Models;
using FluentAssertions;

namespace CppPointerVisualizer.Tests.Models;

/// <summary>
/// Unit tests for MemoryState model.
/// Tests collection management and lookup methods.
/// </summary>
public class MemoryStateTests
{
    [Fact]
    public void MemoryState_DefaultConstructor_ShouldInitializeCollections()
    {
        // Arrange & Act
        var state = new MemoryState();

        // Assert
        state.Objects.Should().NotBeNull();
        state.Objects.Should().BeEmpty();
        state.AddressMap.Should().NotBeNull();
        state.AddressMap.Should().BeEmpty();
    }

    [Fact]
    public void GetObjectByName_WithExistingName_ShouldReturnObject()
    {
        // Arrange
        var state = new MemoryState();
        var obj = new MemoryObject { Name = "testVar", Address = "0x1000" };
        state.Objects.Add(obj);

        // Act
        var result = state.GetObjectByName("testVar");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(obj);
    }

    [Fact]
    public void GetObjectByName_WithNonExistingName_ShouldReturnNull()
    {
        // Arrange
        var state = new MemoryState();
        var obj = new MemoryObject { Name = "existingVar", Address = "0x1000" };
        state.Objects.Add(obj);

        // Act
        var result = state.GetObjectByName("nonExistingVar");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetObjectByName_WithEmptyName_ShouldReturnNull()
    {
        // Arrange
        var state = new MemoryState();

        // Act
        var result = state.GetObjectByName("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetObjectByName_WithMultipleObjects_ShouldReturnFirst()
    {
        // Arrange
        var state = new MemoryState();
        var obj1 = new MemoryObject { Name = "duplicate", Address = "0x1000" };
        var obj2 = new MemoryObject { Name = "duplicate", Address = "0x2000" };
        state.Objects.Add(obj1);
        state.Objects.Add(obj2);

        // Act
        var result = state.GetObjectByName("duplicate");

        // Assert
        result.Should().BeSameAs(obj1, "should return the first matching object");
    }

    [Fact]
    public void GetObjectByAddress_WithExistingAddress_ShouldReturnObject()
    {
        // Arrange
        var state = new MemoryState();
        var obj = new MemoryObject { Name = "testVar", Address = "0x1000" };
        state.Objects.Add(obj);

        // Act
        var result = state.GetObjectByAddress("0x1000");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(obj);
    }

    [Fact]
    public void GetObjectByAddress_WithNonExistingAddress_ShouldReturnNull()
    {
        // Arrange
        var state = new MemoryState();
        var obj = new MemoryObject { Name = "testVar", Address = "0x1000" };
        state.Objects.Add(obj);

        // Act
        var result = state.GetObjectByAddress("0x9999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetObjectByAddress_WithEmptyAddress_ShouldReturnNull()
    {
        // Arrange
        var state = new MemoryState();

        // Act
        var result = state.GetObjectByAddress("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Objects_AddMultiple_ShouldMaintainOrder()
    {
        // Arrange
        var state = new MemoryState();
        var obj1 = new MemoryObject { Name = "first", Address = "0x1000" };
        var obj2 = new MemoryObject { Name = "second", Address = "0x2000" };
        var obj3 = new MemoryObject { Name = "third", Address = "0x3000" };

        // Act
        state.Objects.Add(obj1);
        state.Objects.Add(obj2);
        state.Objects.Add(obj3);

        // Assert
        state.Objects.Should().HaveCount(3);
        state.Objects[0].Should().BeSameAs(obj1);
        state.Objects[1].Should().BeSameAs(obj2);
        state.Objects[2].Should().BeSameAs(obj3);
    }

    [Fact]
    public void Objects_Clear_ShouldRemoveAll()
    {
        // Arrange
        var state = new MemoryState();
        state.Objects.Add(new MemoryObject { Name = "obj1", Address = "0x1000" });
        state.Objects.Add(new MemoryObject { Name = "obj2", Address = "0x2000" });

        // Act
        state.Objects.Clear();

        // Assert
        state.Objects.Should().BeEmpty();
    }

    [Theory]
    [InlineData("var1")]
    [InlineData("pointer_p")]
    [InlineData("ref_r")]
    [InlineData("UPPERCASE")]
    [InlineData("_underscore")]
    public void GetObjectByName_CaseSensitive_ShouldMatchExactly(string name)
    {
        // Arrange
        var state = new MemoryState();
        var obj = new MemoryObject { Name = name, Address = "0x1000" };
        state.Objects.Add(obj);

        // Act
        var result = state.GetObjectByName(name);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(name);
    }

    [Fact]
    public void GetObjectByName_DifferentCase_ShouldNotMatch()
    {
        // Arrange
        var state = new MemoryState();
        var obj = new MemoryObject { Name = "Variable", Address = "0x1000" };
        state.Objects.Add(obj);

        // Act
        var result = state.GetObjectByName("variable");

        // Assert
        result.Should().BeNull("names are case-sensitive");
    }
}

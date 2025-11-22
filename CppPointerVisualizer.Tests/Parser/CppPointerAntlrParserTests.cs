using CppPointerVisualizer.Models;
using CppPointerVisualizer.Parser;
using FluentAssertions;

namespace CppPointerVisualizer.Tests.Parser;

/// <summary>
/// Unit tests for the ANTLR-based C++ pointer parser.
/// Following AAA pattern (Arrange, Act, Assert) and FIRST principles.
/// </summary>
public class CppPointerAntlrParserTests
{
    private readonly CppPointerAntlrParser _sut; // System Under Test

    public CppPointerAntlrParserTests()
    {
        _sut = new CppPointerAntlrParser();
    }

    #region Variable Declaration Tests

    [Fact]
    public void Parse_SimpleVariableDeclaration_ShouldCreateVariableObject()
    {
        // Arrange
        const string code = "int a = 42;";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Should().NotBeNull();
        result.Objects.Should().HaveCount(1);

        var obj = result.Objects[0];
        obj.Name.Should().Be("a");
        obj.Type.Should().Be("int");
        obj.Value.Should().Be(42);
        obj.ObjectType.Should().Be(MemoryObjectType.Variable);
        obj.IsConst.Should().BeFalse();
        obj.Address.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Parse_ConstVariableDeclaration_ShouldSetConstFlag()
    {
        // Arrange
        const string code = "const int x = 100;";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(1);
        result.Objects[0].IsConst.Should().BeTrue();
        result.Objects[0].Name.Should().Be("x");
        result.Objects[0].Value.Should().Be(100);
    }

    [Theory]
    [InlineData("int a = 42;", 42)]
    [InlineData("int c = 999;", 999)]
    public void Parse_VariableWithDifferentValues_ShouldParseCorrectly(string code, object expectedValue)
    {
        // Arrange & Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(1);
        result.Objects[0].Value.Should().Be(expectedValue);
    }

    [Fact]
    public void Parse_ZeroValue_ShouldParseAsStringZero()
    {
        // Arrange
        const string code = "int b = 0;";

        // Act
        var result = _sut.Parse(code);

        // Assert - Zero is parsed as ZeroExpr context, returns string "0"
        result.Objects.Should().HaveCount(1);
        result.Objects[0].Value.Should().Be("0");
    }

    [Fact]
    public void Parse_DoubleVariable_ShouldParseAsDouble()
    {
        // Arrange
        const string code = "double pi = 3.14;";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects[0].Type.Should().Be("double");
        result.Objects[0].Value.Should().Be(3.14);
    }

    [Fact]
    public void Parse_StringVariable_ShouldRemoveQuotes()
    {
        // Arrange
        const string code = "const char *str = \"Hello\";";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects[0].Type.Should().Be("char");
        result.Objects[0].ObjectType.Should().Be(MemoryObjectType.Pointer);
    }

    #endregion

    #region Pointer Declaration Tests

    [Fact]
    public void Parse_SimplePointerDeclaration_ShouldCreatePointerObject()
    {
        // Arrange
        const string code = @"
            int a = 42;
            int *p = &a;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(2);

        var variable = result.Objects[0];
        var pointer = result.Objects[1];

        pointer.Name.Should().Be("p");
        pointer.Type.Should().Be("int");
        pointer.ObjectType.Should().Be(MemoryObjectType.Pointer);
        pointer.PointerLevel.Should().Be(1);
        pointer.PointsTo.Should().Be(variable.Address);
    }

    [Fact]
    public void Parse_NullptrPointer_ShouldPointToNullptr()
    {
        // Arrange
        const string code = "int *p = nullptr;";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(1);
        result.Objects[0].PointsTo.Should().Be("nullptr");
    }

    [Theory]
    [InlineData("int *p = NULL;")]
    [InlineData("int *p = 0;")]
    [InlineData("int *p = nullptr;")]
    public void Parse_NullPointerVariants_ShouldAllPointToNullptr(string code)
    {
        // Arrange & Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects[0].PointsTo.Should().Be("nullptr");
    }

    [Fact]
    public void Parse_DoublePointer_ShouldCreatePointerToPointer()
    {
        // Arrange
        const string code = @"
            int a = 42;
            int *p = &a;
            int **pp = &p;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(3);

        var variable = result.Objects[0];
        var pointer = result.Objects[1];
        var doublePointer = result.Objects[2];

        doublePointer.Name.Should().Be("pp");
        doublePointer.PointerLevel.Should().Be(2);
        doublePointer.PointsTo.Should().Be(pointer.Address);
        pointer.PointsTo.Should().Be(variable.Address);
    }

    [Fact]
    public void Parse_TriplePointer_ShouldHandleThreeLevelsOfIndirection()
    {
        // Arrange
        const string code = @"
            int value = 100;
            int *p1 = &value;
            int **p2 = &p1;
            int ***p3 = &p2;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(4);
        result.Objects[3].PointerLevel.Should().Be(3);
        result.Objects[3].Name.Should().Be("p3");
    }

    [Fact]
    public void Parse_ConstPointerToInt_ShouldSetConstFlagCorrectly()
    {
        // Arrange
        const string code = @"
            int a = 42;
            const int *p = &a;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        var pointer = result.Objects[1];
        pointer.IsConst.Should().BeTrue("pointer points to const int");
        pointer.IsPointerConst.Should().BeFalse("pointer itself is not const");
    }

    [Fact]
    public void Parse_ConstPointer_ShouldSetPointerConstFlag()
    {
        // Arrange
        const string code = @"
            int a = 42;
            int* const p = &a;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        var pointer = result.Objects[1];
        pointer.IsPointerConst.Should().BeTrue("pointer itself is const");
    }

    [Fact]
    public void Parse_ConstPointerToConst_ShouldSetBothConstFlags()
    {
        // Arrange
        const string code = @"
            int a = 42;
            const int* const p = &a;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        var pointer = result.Objects[1];
        pointer.IsConst.Should().BeTrue();
        pointer.IsPointerConst.Should().BeTrue();
    }

    #endregion

    #region Reference Declaration Tests

    [Fact]
    public void Parse_SimpleReference_ShouldCreateReferenceObject()
    {
        // Arrange
        const string code = @"
            int a = 42;
            int &ref = a;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(2);

        var variable = result.Objects[0];
        var reference = result.Objects[1];

        reference.Name.Should().Be("ref");
        reference.Type.Should().Be("int");
        reference.ObjectType.Should().Be(MemoryObjectType.Reference);
        reference.PointsTo.Should().Be(variable.Address);
        reference.Value.Should().Be(42);
    }

    [Fact]
    public void Parse_ConstReference_ShouldSetConstFlag()
    {
        // Arrange
        const string code = @"
            int a = 42;
            const int &ref = a;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects[1].IsConst.Should().BeTrue();
    }

    [Fact]
    public void Parse_ReferenceToPointer_ShouldCreateProperLink()
    {
        // Arrange
        const string code = @"
            int num = 777;
            int *ptr = &num;
            int* &refPtr = ptr;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(3);

        var pointer = result.Objects[1];
        var reference = result.Objects[2];

        reference.Name.Should().Be("refPtr");
        reference.PointsTo.Should().Be(pointer.Address);
    }

    [Fact]
    public void Parse_ReferenceToDereferencedPointer_ShouldPointToOriginalVariable()
    {
        // Arrange
        const string code = @"
            int data = 88;
            int *p = &data;
            int &rp = *p;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(3);

        var variable = result.Objects[0];
        var reference = result.Objects[2];

        reference.PointsTo.Should().Be(variable.Address, "reference should point to what the pointer points to");
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void Parse_MultipleVariablesAndPointers_ShouldParseAll()
    {
        // Arrange
        const string code = @"
            int a = 10;
            int b = 20;
            int c = 30;
            int *p1 = &a;
            int *p2 = &b;
            int *p3 = &c;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(6);
        result.Objects.Count(o => o.ObjectType == MemoryObjectType.Variable).Should().Be(3);
        result.Objects.Count(o => o.ObjectType == MemoryObjectType.Pointer).Should().Be(3);
    }

    [Fact]
    public void Parse_MixedTypesAndConstQualifiers_ShouldHandleCorrectly()
    {
        // Arrange
        const string code = @"
            int val = 256;
            int *p = &val;
            const int *cp = &val;
            int* const pc = &val;
            const int* const cpc = &val;
            int &r = val;
            const int &cr = val;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(7);

        // Verify const qualifiers
        result.Objects[2].IsConst.Should().BeTrue(); // const int *cp
        result.Objects[3].IsPointerConst.Should().BeTrue(); // int* const pc
        result.Objects[4].IsConst.Should().BeTrue(); // const int* const cpc
        result.Objects[4].IsPointerConst.Should().BeTrue();
        result.Objects[6].IsConst.Should().BeTrue(); // const int &cr
    }

    [Fact]
    public void Parse_ComplexPointerChain_ShouldMaintainRelationships()
    {
        // Arrange
        const string code = @"
            int a = 11;
            int b = 22;
            int c = 33;
            int *p1 = &a;
            int *p2 = &b;
            int **pp1 = &p1;
            int **pp2 = &p2;
            int ***ppp = &pp1;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(8);

        // Verify the chain
        var ppp = result.Objects.First(o => o.Name == "ppp");
        var pp1 = result.Objects.First(o => o.Name == "pp1");
        var p1 = result.Objects.First(o => o.Name == "p1");
        var a = result.Objects.First(o => o.Name == "a");

        ppp.PointsTo.Should().Be(pp1.Address);
        pp1.PointsTo.Should().Be(p1.Address);
        p1.PointsTo.Should().Be(a.Address);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void Parse_EmptyString_ShouldReturnEmptyState()
    {
        // Arrange
        const string code = "";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Should().NotBeNull();
        result.Objects.Should().BeEmpty();
    }

    [Fact]
    public void Parse_OnlyComments_ShouldReturnEmptyState()
    {
        // Arrange
        const string code = @"
            // This is a comment
            // Another comment
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().BeEmpty();
    }

    [Fact]
    public void Parse_OnlyWhitespace_ShouldReturnEmptyState()
    {
        // Arrange
        const string code = "   \n\t\r\n   ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().BeEmpty();
    }

    [Fact]
    public void Parse_CodeWithCommentsAndWhitespace_ShouldIgnoreThem()
    {
        // Arrange
        const string code = @"
            // Variable declaration
            int a = 42;

            // Pointer to a
            int *p = &a;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_MemoryAddressesAreUnique_ShouldHaveDifferentAddresses()
    {
        // Arrange
        const string code = @"
            int a = 1;
            int b = 2;
            int c = 3;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        var addresses = result.Objects.Select(o => o.Address).ToList();
        addresses.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Parse_SequentialAddresses_ShouldIncrementCorrectly()
    {
        // Arrange
        const string code = @"
            int a = 1;
            int b = 2;
            int c = 3;
        ";

        // Act
        var result = _sut.Parse(code);

        // Assert
        // Addresses should increment (e.g., 0x1000, 0x1004, 0x1008)
        result.Objects[0].Address.Should().Be("0x1000");
        result.Objects[1].Address.Should().Be("0x1004");
        result.Objects[2].Address.Should().Be("0x1008");
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void GetObjectByName_ExistingObject_ShouldReturnCorrectObject()
    {
        // Arrange
        const string code = @"
            int a = 42;
            int b = 100;
        ";
        var result = _sut.Parse(code);

        // Act
        var obj = result.GetObjectByName("b");

        // Assert
        obj.Should().NotBeNull();
        obj!.Name.Should().Be("b");
        obj.Value.Should().Be(100);
    }

    [Fact]
    public void GetObjectByName_NonExistingObject_ShouldReturnNull()
    {
        // Arrange
        const string code = "int a = 42;";
        var result = _sut.Parse(code);

        // Act
        var obj = result.GetObjectByName("nonexistent");

        // Assert
        obj.Should().BeNull();
    }

    [Fact]
    public void GetObjectByAddress_ExistingAddress_ShouldReturnCorrectObject()
    {
        // Arrange
        const string code = "int a = 42;";
        var result = _sut.Parse(code);
        var expectedAddress = result.Objects[0].Address;

        // Act
        var obj = result.GetObjectByAddress(expectedAddress);

        // Assert
        obj.Should().NotBeNull();
        obj!.Address.Should().Be(expectedAddress);
    }

    #endregion
}

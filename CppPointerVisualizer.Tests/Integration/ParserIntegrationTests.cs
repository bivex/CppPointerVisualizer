using CppPointerVisualizer.Models;
using CppPointerVisualizer.Parser;
using FluentAssertions;

namespace CppPointerVisualizer.Tests.Integration;

/// <summary>
/// Integration tests for the complete parsing pipeline.
/// Tests realistic scenarios with complex C++ code snippets.
/// </summary>
public class ParserIntegrationTests
{
    private readonly CppPointerAntlrParser _parser;

    public ParserIntegrationTests()
    {
        _parser = new CppPointerAntlrParser();
    }

    [Fact]
    public void Parse_CompleteExample1_BasicPointerAndReference()
    {
        // Arrange
        const string code = @"
            int a = 42;
            int *p = &a;
            int &ref = a;
        ";

        // Act
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(3);

        var variable = result.Objects[0];
        var pointer = result.Objects[1];
        var reference = result.Objects[2];

        // Verify variable
        variable.Name.Should().Be("a");
        variable.Value.Should().Be(42);
        variable.ObjectType.Should().Be(MemoryObjectType.Variable);

        // Verify pointer
        pointer.Name.Should().Be("p");
        pointer.PointsTo.Should().Be(variable.Address);
        pointer.ObjectType.Should().Be(MemoryObjectType.Pointer);

        // Verify reference
        reference.Name.Should().Be("ref");
        reference.PointsTo.Should().Be(variable.Address);
        reference.ObjectType.Should().Be(MemoryObjectType.Reference);
    }

    [Fact]
    public void Parse_CompleteExample2_TriplePointer()
    {
        // Arrange - Deep pointer indirection
        const string code = @"
            int value = 100;
            int *p1 = &value;
            int **p2 = &p1;
            int ***p3 = &p2;
        ";

        // Act
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(4);

        var value = result.GetObjectByName("value");
        var p1 = result.GetObjectByName("p1");
        var p2 = result.GetObjectByName("p2");
        var p3 = result.GetObjectByName("p3");

        // Verify chain
        value.Should().NotBeNull();
        p1!.PointsTo.Should().Be(value!.Address);
        p2!.PointsTo.Should().Be(p1.Address);
        p3!.PointsTo.Should().Be(p2.Address);

        // Verify pointer levels
        p1.PointerLevel.Should().Be(1);
        p2.PointerLevel.Should().Be(2);
        p3.PointerLevel.Should().Be(3);
    }

    [Fact]
    public void Parse_CompleteExample3_ConstQualifierVariations()
    {
        // Arrange - All const variations
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
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(7);

        var p = result.GetObjectByName("p");
        var cp = result.GetObjectByName("cp");
        var pc = result.GetObjectByName("pc");
        var cpc = result.GetObjectByName("cpc");
        var r = result.GetObjectByName("r");
        var cr = result.GetObjectByName("cr");

        // Verify const qualifiers
        p!.IsConst.Should().BeFalse();
        p.IsPointerConst.Should().BeFalse();

        cp!.IsConst.Should().BeTrue();
        cp.IsPointerConst.Should().BeFalse();

        pc!.IsConst.Should().BeFalse();
        pc.IsPointerConst.Should().BeTrue();

        cpc!.IsConst.Should().BeTrue();
        cpc.IsPointerConst.Should().BeTrue();

        r!.IsConst.Should().BeFalse();
        cr!.IsConst.Should().BeTrue();
    }

    [Fact]
    public void Parse_CompleteExample4_MultiplePointerChains()
    {
        // Arrange - Complex interconnected structure
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
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(8);

        // Verify all objects exist
        var a = result.GetObjectByName("a");
        var b = result.GetObjectByName("b");
        var c = result.GetObjectByName("c");
        var p1 = result.GetObjectByName("p1");
        var p2 = result.GetObjectByName("p2");
        var pp1 = result.GetObjectByName("pp1");
        var pp2 = result.GetObjectByName("pp2");
        var ppp = result.GetObjectByName("ppp");

        a.Should().NotBeNull();
        b.Should().NotBeNull();
        c.Should().NotBeNull();

        // Verify first chain: ppp -> pp1 -> p1 -> a
        ppp!.PointsTo.Should().Be(pp1!.Address);
        pp1.PointsTo.Should().Be(p1!.Address);
        p1.PointsTo.Should().Be(a!.Address);

        // Verify second chain: pp2 -> p2 -> b
        pp2!.PointsTo.Should().Be(p2!.Address);
        p2.PointsTo.Should().Be(b!.Address);

        // Verify standalone variable
        c!.PointsTo.Should().BeNull();
    }

    [Fact]
    public void Parse_CompleteExample5_NullptrHandling()
    {
        // Arrange - Mix of valid pointers and nullptr
        const string code = @"
            int a = 42;
            int *p1 = &a;
            int *p2 = nullptr;
            int **pp = &p1;
        ";

        // Act
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(4);

        var p1 = result.GetObjectByName("p1");
        var p2 = result.GetObjectByName("p2");
        var pp = result.GetObjectByName("pp");

        p1!.PointsTo.Should().NotBe("nullptr");
        p2!.PointsTo.Should().Be("nullptr");
        pp!.PointsTo.Should().Be(p1.Address);
    }

    [Fact]
    public void Parse_CompleteExample6_ReferenceToPointer()
    {
        // Arrange
        const string code = @"
            int num = 777;
            int *ptr = &num;
            int* &refPtr = ptr;
        ";

        // Act
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(3);

        var num = result.GetObjectByName("num");
        var ptr = result.GetObjectByName("ptr");
        var refPtr = result.GetObjectByName("refPtr");

        // Verify relationships
        ptr!.PointsTo.Should().Be(num!.Address);
        refPtr!.PointsTo.Should().Be(ptr.Address);
        refPtr.ObjectType.Should().Be(MemoryObjectType.Reference);
    }

    [Fact]
    public void Parse_CompleteExample7_ReferenceToDereferencedPointer()
    {
        // Arrange
        const string code = @"
            int data = 88;
            int *p = &data;
            int &rp = *p;
        ";

        // Act
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(3);

        var data = result.GetObjectByName("data");
        var p = result.GetObjectByName("p");
        var rp = result.GetObjectByName("rp");

        // Reference should point to what pointer points to
        p!.PointsTo.Should().Be(data!.Address);
        rp!.PointsTo.Should().Be(data.Address);
    }

    [Fact]
    public void Parse_CompleteExample8_MixedTypes()
    {
        // Arrange - Different data types
        const string code = @"
            int i = 42;
            double d = 3.14;
            int *pi = &i;
            double *pd = &d;
            int &ri = i;
            double &rd = d;
        ";

        // Act
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(6);

        // Verify types are preserved
        var i = result.GetObjectByName("i");
        var d = result.GetObjectByName("d");
        var pi = result.GetObjectByName("pi");
        var pd = result.GetObjectByName("pd");

        i!.Type.Should().Be("int");
        d!.Type.Should().Be("double");
        pi!.Type.Should().Be("int");
        pd!.Type.Should().Be("double");

        // Verify values
        i.Value.Should().Be(42);
        d.Value.Should().Be(3.14);
    }

    [Fact]
    public void Parse_RealWorldScenario_SmartPointerSimulation()
    {
        // Arrange - Simulating smart pointer pattern
        const string code = @"
            int resource = 1024;
            int *rawPtr = &resource;
            int **controlBlock = &rawPtr;
            int &ref1 = resource;
            int &ref2 = resource;
        ";

        // Act
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(5);

        var resource = result.GetObjectByName("resource");
        var rawPtr = result.GetObjectByName("rawPtr");
        var controlBlock = result.GetObjectByName("controlBlock");
        var ref1 = result.GetObjectByName("ref1");
        var ref2 = result.GetObjectByName("ref2");

        // Verify all point to resource directly or indirectly
        rawPtr!.PointsTo.Should().Be(resource!.Address);
        controlBlock!.PointsTo.Should().Be(rawPtr.Address);
        ref1!.PointsTo.Should().Be(resource.Address);
        ref2!.PointsTo.Should().Be(resource.Address);
    }

    [Fact]
    public void Parse_EdgeCase_EmptyCodeWithCommentsOnly()
    {
        // Arrange
        const string code = @"
            // This is just a comment
            // Another comment
            // No actual code
        ";

        // Act
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().BeEmpty();
    }

    [Fact]
    public void Parse_EdgeCase_MixedCodeAndComments()
    {
        // Arrange
        const string code = @"
            // Variable declaration
            int x = 10;
            // Pointer to x
            int *px = &x;
            // Reference to x
            int &rx = x;
        ";

        // Act
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(3, "comments should be ignored");
    }

    [Fact]
    public void Parse_PerformanceTest_ManyVariables()
    {
        // Arrange - Stress test with many variables
        const string code = @"
            int v1 = 1;
            int v2 = 2;
            int v3 = 3;
            int v4 = 4;
            int v5 = 5;
            int *p1 = &v1;
            int *p2 = &v2;
            int *p3 = &v3;
            int *p4 = &v4;
            int *p5 = &v5;
            int **pp1 = &p1;
            int **pp2 = &p2;
            int **pp3 = &p3;
        ";

        // Act
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().HaveCount(13);

        // Verify all addresses are unique
        var addresses = result.Objects.Select(o => o.Address).ToList();
        addresses.Should().OnlyHaveUniqueItems();

        // Verify relationships are maintained
        var p1 = result.GetObjectByName("p1");
        var pp1 = result.GetObjectByName("pp1");
        pp1!.PointsTo.Should().Be(p1!.Address);
    }

    [Fact]
    public void Parse_DataIntegrity_AllObjectsHaveValidAddresses()
    {
        // Arrange
        const string code = @"
            int a = 1;
            int b = 2;
            int *p1 = &a;
            int *p2 = &b;
            int **pp = &p1;
        ";

        // Act
        var result = _parser.Parse(code);

        // Assert
        result.Objects.Should().AllSatisfy(obj =>
        {
            obj.Address.Should().NotBeNullOrEmpty();
            obj.Address.Should().StartWith("0x");
            obj.Name.Should().NotBeNullOrEmpty();
            obj.Type.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void Parse_Consistency_MultipleParsesShouldProduceConsistentResults()
    {
        // Arrange
        const string code = @"
            int x = 42;
            int *p = &x;
        ";

        // Act
        var result1 = _parser.Parse(code);
        var result2 = new CppPointerAntlrParser().Parse(code);

        // Assert - Structure should be identical (though addresses may differ due to counter)
        result1.Objects.Should().HaveCount(result2.Objects.Count);

        for (int i = 0; i < result1.Objects.Count; i++)
        {
            result1.Objects[i].Name.Should().Be(result2.Objects[i].Name);
            result1.Objects[i].Type.Should().Be(result2.Objects[i].Type);
            result1.Objects[i].ObjectType.Should().Be(result2.Objects[i].ObjectType);
            result1.Objects[i].Value.Should().Be(result2.Objects[i].Value);
        }
    }
}

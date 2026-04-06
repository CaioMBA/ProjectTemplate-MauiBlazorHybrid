using Domain.Extensions;
using System.Text;

namespace UnitTest;

public class ObjectExtensionTests
{
    private sealed class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    [Fact]
    public void ToBytes_And_ToObject_String_ShouldRoundTrip()
    {
        const string value = "hello";

        var bytes = value.ToBytes();
        var result = bytes.ToObject<string>();

        Assert.Equal(value, result);
    }

    [Fact]
    public void ToObject_Int_ShouldConvertFromUtf8Data()
    {
        var bytes = Encoding.UTF8.GetBytes("123");

        var result = bytes.ToObject<int>();

        Assert.Equal(123, result);
    }

    [Fact]
    public void ToObject_ComplexType_ShouldDeserializeJson()
    {
        var person = new Person { Name = "Ana", Age = 30 };
        var bytes = person.ToBytes();

        var result = bytes.ToObject<Person>();

        Assert.NotNull(result);
        Assert.Equal("Ana", result!.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void ToCsv_Generic_ShouldBuildHeaderAndRows()
    {
        var csv = new[] { new Person { Name = "John", Age = 20 } }.ToCSV(',');

        Assert.Contains("Name,Age", csv);
        Assert.Contains("John,20", csv);
    }

    [Fact]
    public void ToLambdaFilter_ShouldFilterMatchingValues()
    {
        var data = new[]
        {
            new Person { Name = "A", Age = 10 },
            new Person { Name = "B", Age = 20 }
        };

        var filter = new Dictionary<string, object?> { ["Age"] = 20 }.ToLambdaFilter<Person>().Compile();
        var result = data.Where(filter).ToList();

        Assert.Single(result);
        Assert.Equal("B", result[0].Name);
    }

    [Fact]
    public void ToQueryString_ShouldConvertDictionary()
    {
        var parameters = new Dictionary<string, object?>
        {
            ["name"] = "john",
            ["age"] = 30,
            ["nullValue"] = null
        };

        var result = parameters.ToQueryString();

        Assert.Contains("name=john", result);
        Assert.Contains("age=30", result);
        Assert.DoesNotContain("nullValue", result);
    }

    [Fact]
    public void ToCsv_Dictionary_ShouldBuildExpectedRows()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                ["Name"] = "Jane",
                ["Age"] = 28
            }
        };

        var csv = data.Cast<IDictionary<string, object>>().ToCSV(',');

        Assert.Contains("Name,Age", csv);
        Assert.Contains("Jane,28", csv);
    }

    [Fact]
    public void ToLambdaFilter_NoValidFilters_ShouldThrowArgumentException()
    {
        var filters = new Dictionary<string, object?>
        {
            ["UnknownProperty"] = "value"
        };

        Assert.Throws<ArgumentException>(() => filters.ToLambdaFilter<Person>());
    }
}

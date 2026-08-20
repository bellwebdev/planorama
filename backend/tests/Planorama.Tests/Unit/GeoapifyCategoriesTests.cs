using Planorama.Api.Places;
using Planorama.Core.Places;
using Xunit;

namespace Planorama.Tests.Unit;

public class GeoapifyCategoriesTests
{
    [Fact]
    public void Every_category_maps_to_a_provider_identifier()
    {
        foreach (PlaceCategory category in Enum.GetValues<PlaceCategory>())
        {
            string queryValue = GeoapifyCategories.ToQueryValue(category);

            Assert.NotEmpty(queryValue);
            // A typo'd identifier would silently return zero results rather than fail, so assert
            // the shape the provider documents: lowercase, dot-separated, comma-joined.
            Assert.All(queryValue.Split(','), identifier =>
                Assert.Matches("^[a-z]+(\\.[a-z_]+)*$", identifier));
        }
    }

    [Theory]
    [InlineData("entertainment.museum", PlaceCategory.Museum)]
    [InlineData("leisure.playground", PlaceCategory.Playground)]
    [InlineData("beach", PlaceCategory.Beach)]
    public void Maps_provider_categories_back_onto_the_enum(string providerCategory, PlaceCategory expected) =>
        Assert.Equal(expected, GeoapifyCategories.FromProvider([providerCategory], PlaceCategory.Attraction));

    [Fact]
    public void Falls_back_to_the_requested_category_when_nothing_matches() =>
        Assert.Equal(
            PlaceCategory.Park,
            GeoapifyCategories.FromProvider(["building.residential", "man_made"], PlaceCategory.Park));
}

using Lib9c.GraphQL.Enums;
using Mimir.Models.Product;
using Mimir.Repositories;

namespace Mimir.GraphQL.Queries;

public class ProductQuery
{
    [UseOffsetPaging]
    [UseProjection]
    [UseSorting]
    [UseFiltering]
    public IExecutable<Product> GetProducts(
        [Service] ProductsRepository productsRepository,
        PlanetName planetName
    )
    {
        return productsRepository.GetProducts(planetName);
    }
}

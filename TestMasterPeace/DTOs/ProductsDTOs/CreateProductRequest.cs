using TestMasterPeace.Models;

namespace TestMasterPeace.DTOs.ProductsDTOs;

public class CreateProductRequest
{
    public required  string Name { get; set; }
    public required string? Description { get; set; }
    public required decimal Price { get; set; }
    public required long? CategoryId { get; set; }
    public required  string? Img { get; set; }


}

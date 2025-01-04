using Microsoft.AspNetCore.Mvc;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }
    [HttpGet("Categories/{category}")]
    public async Task<ActionResult<IEnumerable<Product>>> GetItemsByCategory(string category)
    {
        var products = await _productService.GetAllAsync();

        List<Product> categoryProducts = new List<Product>();
        foreach (var item in products)
        {
            List<string> categories = new List<string>();
            categories = item.category.ToList();
            if (categories.Find(i=>i==category )== category)
            {
                categoryProducts.Add(item);
            }
        }
        return Ok(categoryProducts);
    }


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _productService.GetAllAsync();
        List<Product> finalProducts = new List<Product>();
        finalProducts = products.ToList();
        return Ok(finalProducts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Add(Product product)
    {

        await _productService.AddAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = product.product_id }, product);
    }

    [HttpPut]
    public async Task<IActionResult> Update(Product product)
    {
        await _productService.UpdateAsync(product);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteAsync(id);
        return NoContent();
    }
}

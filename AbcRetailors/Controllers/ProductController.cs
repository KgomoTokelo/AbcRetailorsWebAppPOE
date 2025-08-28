using Microsoft.AspNetCore.Mvc;
using AbcRetailors.Models;
using AbcRetailors.Services;

namespace AbcRetailors.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController( IAzureStorageService azureStorageService, ILogger<ProductController> logger)
        {
            _logger = logger;
            _azureStorageService = azureStorageService;
        }

        public async Task<IActionResult> Index()
        {
            
         var products = await _azureStorageService.GetAllEntitiesAsync<Product>();
            return View(products);
            
        }

        public IActionResult Create() {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (Request.Form.TryGetValue("Price", out var priceFormValue))
            {
                _logger.LogInformation("Raw price from form: {PriceFormValue}", priceFormValue.ToString());
                if (double.TryParse(priceFormValue, out var parsedPrice))
                {
                    product.Price = parsedPrice;
                    _logger.LogInformation("Successfully Parsed price {parsedPrice}", parsedPrice);
                }
                else
                {
                    _logger.LogWarning("Failed to parse price : {PriceFormValue}", priceFormValue.ToString());
                }
            }
            _logger.LogInformation("Final product price :{Price}",product.Price);

            if (ModelState.IsValid)
            {
                try
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greate 0.00$");
                        return View(product);
                    }
                    if(imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _azureStorageService.UploadImageAsync(imageFile, "product-images");
                        product.ImageUrl = imageUrl;

                    }
                    await _azureStorageService.AddEntityAsync(product);
                    TempData["Success"] = $"Product {product.ProductName} created successfully with price{product.Price:C}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
    
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
             }
        }

            return View(product);
        }
        public async Task<IActionResult> Edit(string id)
        {
            if ( string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var product = await _azureStorageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit( Product product, IFormFile? imageFile)
        {
         
            if (Request.Form.TryGetValue( "Price", out var priceFormVaLlue))
            {
                if (double.TryParse(priceFormVaLlue, out var parsedPrice))
                {
                    product.Price = parsedPrice;
                    _logger.LogInformation("Edit: Successfully parsed Price: {Price}",parsedPrice);
                }
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var originalProduct = await _azureStorageService.GetEntityAsync<Product>("Product", product.RowKey);
                    if (originalProduct == null)
                    {
                        return NotFound();
                    }

                    originalProduct.ProductName = product.ProductName;
                    originalProduct.Description = product.Description;
                    originalProduct.Price = product.Price;
                    originalProduct.StockAvaliable = product.StockAvaliable;

                    if (imageFile != null && imageFile.Length > 0 )
                    {
                        
                       var imageUrl = await _azureStorageService.UploadImageAsync(imageFile,"product-images");
                        originalProduct.ImageUrl = imageUrl;
                    }
                   
                        await _azureStorageService.UpdateEntityAsync(product);
                        TempData["Success"] = "Product updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating product: {Message}",ex.Message);
                        ModelState.AddModelError("", $"Error updating product:  {ex.Message}");
                        
                    }
                }
            return View(product);
            }

        [HttpPost]
       public async Task<IActionResult> Delete(string id)
        {
           try
            {
                await _azureStorageService.DeleteEntityAsync<Product>("Product", id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch(Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    }





    


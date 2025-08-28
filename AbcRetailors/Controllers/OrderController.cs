using Microsoft.AspNetCore.Mvc;
using AbcRetailors.Models;
using AbcRetailors.Models.ViewModels;
using AbcRetailors.Services;
using System.Text.Json;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Shared;

namespace AbcRetailors.Controllers
{

    public class OrderController : Controller
    {

        private readonly IAzureStorageService _storageService;

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }


        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
            return View(orders);
        }

        public async Task<IActionResult> Create()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();

            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products,
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var customers = await _storageService.GetEntityAsync<Customer>("Customer", model.CustomerId);
                    var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductId);

                    if (customers == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected");
                        await PopulateDropdowns(model);
                        return View(model);

                    }

                    if (product.StockAvaliable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Avaible : {product.StockAvaliable}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    var order = new Order
                    {
                        CustomerId = model.CustomerId,
                        Username = customers.Username,
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc),
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * model.Quantity,
                        Status = "Submitted"
                    };

                    await _storageService.AddEntityAsync(order);

                    product.StockAvaliable -= model.Quantity;
                    await _storageService.UpdateEntityAsync(product);

                    var OrderMessage = new
                    {
                        OrderId = order.OrderId,
                        CustomerId = order.CustomerId,
                        CustomerName = customers.Name + " " + customers.Surname,
                        ProductName = product.ProductName,
                        Quantity = order.Quantity,
                        TotalPrice = order.TotalPrice,
                        OrderDate = order.OrderDate,
                        Status = order.Status
                    };

                    await _storageService.SendMessageAsync("orders-notifications", JsonSerializer.Serialize(OrderMessage));

                    var stockMessage = new
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        PreviousStock = product.StockAvaliable + model.Quantity,
                        NewsStock = product.StockAvaliable,
                        UpdatedBy = "Order System",
                        UpdateDate = DateTime.UtcNow
                    };

                    await _storageService.SendMessageAsync("stock-updates", JsonSerializer.Serialize(stockMessage));

                    TempData["Succes"] = "Order created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                }
            }
            await PopulateDropdowns(model);
            return View(model);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var orders = await _storageService.GetEntityAsync<Order>("Order", id);
            if (orders == null)
            {
                return NotFound();
            }
            return View(orders);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var orders = await _storageService.GetEntityAsync<Order>("Order", id);
            if(orders == null)
            {
                return NotFound();
            }
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    await _storageService.UpdateEntityAsync(order);
                    TempData["Success"] = "Order updated successfully";
                    return RedirectToAction(nameof(Index));

                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);

        }
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Order>("Order", id);
                TempData["Success"] = "Order deleted Successfully";


            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> GetProductPrice(string productId)
        {
            try
            {
                var products = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (products == null)
                {
                    return Json(new
                    {
                        success = true,
                        price = products.Price,
                        stock = products.StockAvaliable,
                        productName = products.ProductName
                    });

                }
                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                var orders = await _storageService.GetEntityAsync<Order>("Order", id);
                if (orders == null)
                {
                    return Json(new { success = false, message = "Order not Found" });

                }
                var previousStatus = orders.Status;
                orders.Status = newStatus;
                await _storageService.UpdateEntityAsync(orders);

                var statusMessage = new
                {
                    OrderId = orders.OrderId,
                    CustomerId = orders.CustomerId,
                    CustomerName = orders.Username,
                    ProductName = orders.ProductName,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    UpdatedDate = DateTime.UtcNow,
                    Updateby = "System"
                };
                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));
                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = true, message = ex.Message });
            }
        }
        public async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }
    }

}

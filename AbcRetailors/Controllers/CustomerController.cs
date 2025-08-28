using Microsoft.AspNetCore.Mvc;
using AbcRetailors.Models;
using AbcRetailors.Services;

namespace AbcRetailors.Controllers
{
    public class CustomerController : Controller

    {
        private readonly IAzureStorageService _storageService;

        public CustomerController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            return View(customers);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _storageService.AddEntityAsync(customer);
                    TempData["Succes"] = "Customer created Successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating Customer: {ex.Message} ");
                }
            }
            return View(customer);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var customer = await _storageService.GetEntityAsync<Customer>("Customer", id);
            if (customer == null) {
                return NotFound();
            }
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {

            if (ModelState.IsValid)
            {
                try
                {
                  var OriginalCustomer =  await _storageService.GetEntityAsync<Customer>("Customer",customer.RowKey);
                    if (OriginalCustomer == null)
                    {
                        return NotFound();
                    }

                    //updating fields
                    OriginalCustomer.Name= customer.Name;
                    OriginalCustomer.Email= customer.Email;
                    OriginalCustomer.Surname= customer.Surname;
                    OriginalCustomer.Username= customer.Username;
                    OriginalCustomer.ShippingAddress= customer.ShippingAddress;

                    //update Azure table
                    await _storageService.UpdateEntityAsync(OriginalCustomer);
                    TempData["Success"] = "Customer Updated Successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex) {
                    ModelState.AddModelError("", $"fAILED UPDATING customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(String id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Customer>("Customers", id);
                TempData["Success"] = "Customer deleted SuccessFully";

            }
            catch (Exception ex) {
                TempData["Error"] = $"Error Deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));

        }
    }
}




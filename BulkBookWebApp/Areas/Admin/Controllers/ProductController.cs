using Microsoft.AspNetCore.Mvc;
using BulkBook.DataAccessLayer;
using BulkBook.DataAccessLayer.Infrastructure.IRepository;
using BulkBook.Models;
using BulkBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Win32.SafeHandles;
using BulkBook.CommonHelper;
using Microsoft.AspNetCore.Authorization;

namespace BulkBookWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = WebSiteRole.Role_Admin)]
    public class ProductController : Controller
    {
        private IUnitOfWork _unitofwork;
        private IWebHostEnvironment _hostingEnvironment;

		public ProductController(IUnitOfWork unitofwork, IWebHostEnvironment hostingEnvironment)
		{
			_unitofwork = unitofwork;
			_hostingEnvironment = hostingEnvironment;
		}
        #region APICALL
        public IActionResult AllProducts()
        { 
            var products=_unitofwork.Product.GetAll(includeProperties:"Category");
            return Json(new {data=products});
        }
        #endregion
			public IActionResult Index()
        {
   //         ProductVM productVM = new ProductVM();
			//productVM.Products = _unitofwork.Product.GetAll();
            return View();
        }

        [HttpGet]
        public IActionResult CreateUpdate(int? id)
        {
            ProductVM vm = new ProductVM()
            {
                product = new(),
                Categories = _unitofwork.Category.GetAll().Select(x => new SelectListItem()
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                })
            };
            if (id == 0 || id == null)
            {
                return View(vm);
            }
            else
            {
                vm.product = _unitofwork.Product.GetT(x => x.Id == id);
                if (vm.product == null)
                {
                    return NotFound();
                }
                else
                {
					return View(vm);
				}				
			}
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUpdate(ProductVM vm, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string fileName = String.Empty;
                if(file != null)
                {
                    string uploadDir = Path.Combine(_hostingEnvironment.WebRootPath, "ProductImage");
                    fileName=Guid.NewGuid().ToString()+"-"+file.FileName;
                    string filePath=Path.Combine(uploadDir, fileName);

                    if (vm.product.ImageUrl!=null)
                    {
                        var oldImagePath = Path.Combine(_hostingEnvironment.WebRootPath, vm.product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }                    

                    using(var fileStream=new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    vm.product.ImageUrl = @"\ProductImage\" + fileName;
                }
                if (vm.product.Id == 0)
                {
					_unitofwork.Product.Add(vm.product);
                    TempData["success"] = "Product created successfully!";
				}
                else
                {
                    _unitofwork.Product.Update(vm.product);
                    TempData["success"] = "Product updated successfully!";
                }
                
                _unitofwork.Save();
                
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        //[HttpGet]
        //public IActionResult Create()
        //{
        //    return View();
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create(Category category)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _unitofwork.Category.Add(category);
        //        _unitofwork.Save();
        //        TempData["success"] = "Category created successfully!";
        //        return RedirectToAction("Index");
        //    }
        //    return View(category);
        //}

        //[HttpGet]
        //public IActionResult Delete(int? id)
        //{
        //    if (id == 0 || id == null)
        //    {
        //        return NotFound();
        //    }
        //    var category = _unitofwork.Category.GetT(x => x.Id == id);
        //    if (category == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(category);
        //}

        #region DeleteAPICall
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var product = _unitofwork.Product.GetT(x => x.Id == id);
            if (product == null)
            {
                return Json(new {success=false,message="Error in fetching data"});
            }
            else
            {
                var oldImagePath = Path.Combine(_hostingEnvironment.WebRootPath, product.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
                _unitofwork.Product.Delete(product);
                _unitofwork.Save();
                return Json(new { success = true, message = "Data deleted successfully" });
            }
           
        }
        #endregion
    }
}

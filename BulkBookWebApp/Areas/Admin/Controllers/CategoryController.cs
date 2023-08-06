using Microsoft.AspNetCore.Mvc;
using BulkBook.DataAccessLayer;
using BulkBook.DataAccessLayer.Infrastructure.IRepository;
using BulkBook.Models;
using BulkBook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using BulkBook.CommonHelper;

namespace BulkBookWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =WebSiteRole.Role_Admin)]
    public class CategoryController : Controller
    {
        private IUnitOfWork _unitofwork;

        public CategoryController(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }

        public IActionResult Index()
        {
            CategoryVM categoryVM = new CategoryVM();
            categoryVM.Categories = _unitofwork.Category.GetAll();
            return View(categoryVM);
        }

        [HttpGet]
        public IActionResult CreateUpdate(int? id)
        {
            CategoryVM vm = new CategoryVM();
            if (id == 0 || id == null)
            {
                return View(vm);
            }
            else
            {
                vm.Category = _unitofwork.Category.GetT(x => x.Id == id);
                if (vm.Category == null)
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
        public IActionResult CreateUpdate(CategoryVM vm)
        {
            if (ModelState.IsValid)
            {
                if(vm.Category.Id == 0) 
                {
					_unitofwork.Category.Add(vm.Category);
					TempData["success"] = "Category created successfully!";
				}
                else
                {
					_unitofwork.Category.Update(vm.Category);
					TempData["success"] = "Category Updated successfully!";
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

        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == 0 || id == null)
            {
                return NotFound();
            }
            var category = _unitofwork.Category.GetT(x => x.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteData(int? id)
        {
            var category = _unitofwork.Category.GetT(x => x.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            _unitofwork.Category.Delete(category);
            _unitofwork.Save();
            TempData["success"] = "Category Deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}

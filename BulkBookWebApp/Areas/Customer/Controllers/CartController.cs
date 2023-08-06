using BulkBook.CommonHelper;
using BulkBook.DataAccessLayer.Infrastructure.IRepository;
using BulkBook.Models;
using BulkBook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkBookWebApp.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private IUnitOfWork _unitOfWork;
        public CartVM itemList { get; set; }
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            itemList = new CartVM()
            {
                ListOfCart = _unitOfWork.Cart.GetAll(x => x.ApplicationUserId == claims.Value, includeProperties: "Product"),
                OrderHeader=new BulkBook.Models.OrderHeader()
            };
            
            foreach (var item in itemList.ListOfCart)
            {
                itemList.OrderHeader.OrderTotal += (item.Count * item.Product.Price);
            }
            return View(itemList);
        }
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            itemList = new CartVM()
            {
                ListOfCart = _unitOfWork.Cart.GetAll(x => x.ApplicationUserId == claims.Value, includeProperties: "Product"),
                OrderHeader = new BulkBook.Models.OrderHeader()
            };
            itemList.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetT(x => x.Id == claims.Value);
            itemList.OrderHeader.Name = itemList.OrderHeader.ApplicationUser.Name;
            itemList.OrderHeader.Phone = itemList.OrderHeader.ApplicationUser.PhoneNumber;
            itemList.OrderHeader.Address = itemList.OrderHeader.ApplicationUser.Address;
            itemList.OrderHeader.City = itemList.OrderHeader.ApplicationUser.City;
            itemList.OrderHeader.State = itemList.OrderHeader.ApplicationUser.State;
            itemList.OrderHeader.PostalCode = itemList.OrderHeader.ApplicationUser.PinCode;

            foreach (var item in itemList.ListOfCart)
            {
                itemList.OrderHeader.OrderTotal += (item.Count * item.Product.Price);
            }
            return View(itemList);
        }

        [HttpPost]        
        public IActionResult Summary(CartVM vm)
        {
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            vm.ListOfCart = _unitOfWork.Cart.GetAll(x => x.ApplicationUserId == claims.Value, includeProperties: "Product");
            vm.OrderHeader.OrderStatus = OrderStatus.StatusPending;
            vm.OrderHeader.PaymentStatus=PaymentStatus.StatusPending;
            vm.OrderHeader.OrderDate = DateTime.Now;
            vm.OrderHeader.ApplicationUserId = claims.Value;
			foreach (var item in vm.ListOfCart)
			{
				vm.OrderHeader.OrderTotal += (item.Count * item.Product.Price);
			}
            _unitOfWork.OrderHeader.Add(vm.OrderHeader);
            _unitOfWork.Save();
            foreach(var item in vm.ListOfCart)
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    ProductId = item.ProductId,
                    OrderHeaderId = vm.OrderHeader.Id,
                    Count = item.Count,
                    Price = item.Product.Price
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }
            var domain = "https://localhost:7138/";
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>(),
                
                Mode = "payment",
                SuccessUrl = domain+$"customer/cart/ordersuccess?id={vm.OrderHeader.Id}",
                CancelUrl = domain+$"customer/cart/Index"
            };

            foreach (var item in vm.ListOfCart)
            {
                var lineItemsOptions = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Product.Price*100),
                        Currency = "INR",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name =item.Product.Name,
                        },
                    },
                    Quantity = item.Count,
                };
                options.LineItems.Add(lineItemsOptions);
            }

            var service = new SessionService();
            Session session=service.Create(options);

            _unitOfWork.OrderHeader.PaymentStatus(vm.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();

			_unitOfWork.Cart.DeleteRange(vm.ListOfCart);
			_unitOfWork.Save();

			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
			return RedirectToAction("Index", "Home");
		}

        public IActionResult ordersuccess(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.GetT(x => x.Id == id);
			var service = new SessionService();
			Session session = service.Get(orderHeader.SessionId);
            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeader.UpdateStatus(id,OrderStatus.StatusApproved,PaymentStatus.StatusApproved);
                _unitOfWork.OrderHeader.PaymentStatus(id, session.Id, session.PaymentIntentId);
            }
            List<Cart> cart = _unitOfWork.Cart.GetAll(x=>x.ApplicationUserId==orderHeader.ApplicationUserId).ToList();
            _unitOfWork.Cart.DeleteRange(cart);
            _unitOfWork.Save();
            return View(id);
		}

        public IActionResult plus(int id)
        {
            var cart=_unitOfWork.Cart.GetT(x=>x.Id == id);
            _unitOfWork.Cart.IncrementCartItem(cart, 1);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult minus(int id)
        {
            var cart = _unitOfWork.Cart.GetT(x => x.Id == id);
            if (cart.Count <= 1)
            {
                _unitOfWork.Cart.Delete(cart);
                var count = _unitOfWork.Cart.GetAll(x => x.ApplicationUserId == cart.ApplicationUserId).ToList().Count-1;
                HttpContext.Session.SetInt32("SessionCart", count);
            }
            else
            {
                _unitOfWork.Cart.DecrementCartItem(cart, 1);
            }
            
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult delete(int id)
        {
            var cart = _unitOfWork.Cart.GetT(x => x.Id == id);
            _unitOfWork.Cart.Delete(cart);
            _unitOfWork.Save();
            var count=_unitOfWork.Cart.GetAll(x=>x.ApplicationUserId==cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32("SessionCart", count);
            return RedirectToAction(nameof(Index));
        }
    }
}

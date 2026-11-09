using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using TiemBaDuong.Models;
using TiemBaDuong.Models.Services;

namespace TiemBaDuong.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private const string CheckoutTokenKey = "CheckoutToken";
        private const int OrdersPageSize = 10; // Chưa có quy định trong PPT, tạm thời (IMPLEMENTATION_NOTES)

        private readonly mystoreEntities db = new mystoreEntities();

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        private Customer CurrentCustomer()
        {
            string name = User.Identity.Name;
            return db.Customers.FirstOrDefault(c => c.Username == name);
        }

        // GET: /Order/Checkout
        [HttpGet]
        public ActionResult Checkout()
        {
            var customer = CurrentCustomer();
            if (customer == null)
            {
                TempData["Error"] = "Tài khoản này chưa có hồ sơ khách hàng để đặt hàng.";
                return RedirectToAction("Index", "Cart");
            }

            var cart = new CartService(Session, db);
            cart.Refresh();
            if (cart.SelectedItems.Count == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm trong giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }

            return View(BuildCheckoutVM(customer, cart, NewCheckoutToken()));
        }

        // POST: /Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutVM model)
        {
            var customer = CurrentCustomer();
            if (customer == null)
            {
                TempData["Error"] = "Tài khoản này chưa có hồ sơ khách hàng để đặt hàng.";
                return RedirectToAction("Index", "Cart");
            }

            // Token một lần: chống gửi trùng (double submit).
            string expected = Session[CheckoutTokenKey] as string;
            Session[CheckoutTokenKey] = null;
            if (string.IsNullOrEmpty(expected) || model.CheckoutToken != expected)
            {
                TempData["Error"] = "Phiên đặt hàng đã hết hạn hoặc đã được gửi. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }

            var cart = new CartService(Session, db);
            cart.Refresh();

            // Địa chỉ cuối cùng do server kiểm tra (spec mục 4.8)
            string address = !string.IsNullOrWhiteSpace(model.NewShippingAddress)
                ? model.NewShippingAddress.Trim()
                : (customer.CustomerAddress ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(address))
            {
                ModelState.AddModelError("NewShippingAddress", "Vui lòng nhập địa chỉ giao hàng.");
            }
            if (!CheckoutOptions.DeliveryMethods.Contains(model.DeliveryMethod))
            {
                ModelState.AddModelError("DeliveryMethod", "Phương thức giao hàng không hợp lệ.");
            }
            if (!CheckoutOptions.PaymentMethods.Contains(model.PaymentMethod))
            {
                ModelState.AddModelError("PaymentMethod", "Phương thức thanh toán không hợp lệ.");
            }
            if (cart.SelectedItems.Count == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm trong giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }
            if (!ModelState.IsValid)
            {
                return View(BuildCheckoutVM(customer, cart, NewCheckoutToken()));
            }

            try
            {
                int orderId = new OrderService(db).PlaceOrder(
                    customer.CustomerID,
                    cart.SelectedItems,
                    address,
                    model.DeliveryMethod,
                    model.PaymentMethod);

                // Chỉ xóa các item đã đặt; item không chọn giữ lại (giả định A8).
                cart.Items.RemoveAll(i => i.Selected);
                cart.SetSelected(null);

                return RedirectToAction("OrderSuccess", new { id = orderId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Cart");
            }
        }

        // GET: /Order/OrderSuccess/{id}
        public ActionResult OrderSuccess(int id)
        {
            string name = User.Identity.Name;
            var order = db.Orders
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .Include(o => o.Customer)
                .FirstOrDefault(o => o.OrderID == id && o.Customer.Username == name);

            // Không tìm thấy hoặc không phải đơn của người này: 404, không tiết lộ đơn tồn tại.
            if (order == null) return HttpNotFound();
            return View(order);
        }

        // GET: /Order/MyOrder?status=&q=&page=
        public ActionResult MyOrder(string status, string q, int? page)
        {
            string name = User.Identity.Name;
            int current = page.HasValue && page.Value > 0 ? page.Value : 1;

            var query = db.Orders
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .Where(o => o.Customer.Username == name);

            if (!string.IsNullOrEmpty(status))
            {
                if (!OrderStatuses.IsValid(status)) return HttpNotFound();
                query = query.Where(o => o.OrderStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                string term = q.Trim();
                int orderId;
                bool isId = int.TryParse(term, out orderId);
                query = query.Where(o => (isId && o.OrderID == orderId)
                                      || o.OrderDetails.Any(d => d.Product.ProductName.Contains(term)));
            }

            var ordered = query.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderID);
            var paged = PagedResult<Order>.FromQuery(ordered, current, OrdersPageSize);

            var vm = new OrderHistoryVM
            {
                Status = status,
                Q = q,
                Orders = paged,
                Pager = new PagerVM
                {
                    CurrentPage = paged.PageNumber,
                    TotalPages = paged.TotalPages,
                    Action = "MyOrder",
                    Controller = "Order",
                    Values = new RouteValueDictionary { { "status", status }, { "q", q } }
                }
            };
            return View(vm);
        }

        // POST: /Order/BuyAgain/{id}: thêm lại sản phẩm còn bán, giá lấy mới từ DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BuyAgain(int id)
        {
            string name = User.Identity.Name;
            var order = db.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.OrderID == id && o.Customer.Username == name);
            if (order == null) return HttpNotFound();

            var cart = new CartService(Session, db);
            var skipped = new List<string>();
            foreach (var detail in order.OrderDetails.ToList())
            {
                string error = cart.Add(detail.ProductID, Math.Min(detail.Quantity, CartService.MaxQuantityPerItem));
                if (error != null)
                {
                    skipped.Add(detail.Product != null ? detail.Product.ProductName : ("Sản phẩm #" + detail.ProductID));
                }
            }

            if (skipped.Count > 0)
            {
                TempData["Error"] = "Không thêm được: " + string.Join(", ", skipped) + " (đã ngừng bán hoặc không còn hợp lệ).";
            }
            else
            {
                TempData["Success"] = "Đã thêm lại các sản phẩm của đơn hàng vào giỏ.";
            }
            return RedirectToAction("Index", "Cart");
        }

        private string NewCheckoutToken()
        {
            string token = Guid.NewGuid().ToString("N");
            Session[CheckoutTokenKey] = token;
            return token;
        }

        private CheckoutVM BuildCheckoutVM(Customer customer, CartService cart, string token)
        {
            return new CheckoutVM
            {
                DefaultAddress = customer.CustomerAddress,
                CheckoutToken = token,
                Items = cart.SelectedItems,
                Total = cart.SelectedTotal + CheckoutOptions.ShippingFee,
                DeliveryOptions = CheckoutOptions.DeliveryMethods,
                PaymentOptions = CheckoutOptions.PaymentMethods
            };
        }
    }
}

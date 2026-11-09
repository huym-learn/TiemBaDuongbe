using System.Linq;
using System.Web.Mvc;
using TiemBaDuong.Models;
using TiemBaDuong.Security;

namespace TiemBaDuong.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class DashboardController : Controller
    {
        private readonly mystoreEntities db = new mystoreEntities();

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        public ActionResult Index()
        {
            // Doanh thu: không tính đơn đã hủy. Đơn cũ (OrderStatus NULL) vẫn được tính.
            var revenue = db.Orders
                .Where(o => o.OrderStatus == null || o.OrderStatus != "Cancelled")
                .Sum(o => (decimal?)o.TotalAmount) ?? 0m;

            var vm = new DashboardVM
            {
                ProductCount = db.Products.Count(),
                CategoryCount = db.Categories.Count(),
                CustomerCount = db.Customers.Count(),
                OrderCount = db.Orders.Count(),
                Revenue = revenue
            };
            return View(vm);
        }
    }
}

using System.Linq;
using System.Web.Mvc;
using TiemBaDuong.Models;
using TiemBaDuong.Models.Services;

namespace TiemBaDuong.Controllers
{
    public class CartController : Controller
    {
        private readonly mystoreEntities db = new mystoreEntities();

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        private CartService Cart()
        {
            return new CartService(Session, db);
        }

        private ActionResult BackToCart()
        {
            return RedirectToAction("Index");
        }

        // GET: /Cart
        public ActionResult Index()
        {
            var cart = Cart();
            var messages = cart.Refresh();
            var items = cart.Items.ToList();

            var catalog = new CatalogService(db);
            var categoryIds = items.Select(i => i.CategoryID).Distinct().ToList();
            var cartProductIds = items.Select(i => i.ProductID).ToList();

            var vm = new CartIndexVM
            {
                Items = items,
                SelectedTotal = cart.SelectedTotal,
                SelectedCount = cart.SelectedItems.Count,
                SimilarProducts = catalog.GetSimilar(categoryIds, cartProductIds, 8),
                Messages = messages
            };
            return View(vm);
        }

        // POST: /Cart/AddItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddItem(int productId, int quantity = 1)
        {
            string error = Cart().Add(productId, quantity);
            if (error != null)
            {
                TempData["Error"] = error;
            }
            else
            {
                TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";
            }
            return BackToCart();
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            string error = Cart().UpdateQuantity(productId, quantity);
            if (error != null) TempData["Error"] = error;
            else TempData["Success"] = "Đã cập nhật số lượng.";
            return BackToCart();
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int productId)
        {
            Cart().Remove(productId);
            TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ.";
            return BackToCart();
        }

        // POST: /Cart/ClearCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            Cart().Clear();
            TempData["Success"] = "Đã xóa toàn bộ giỏ hàng.";
            return BackToCart();
        }

        // POST: /Cart/SaveSelection — lưu các item được tick để mua
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveSelection(int[] selectedIds)
        {
            Cart().SetSelected(selectedIds);
            return BackToCart();
        }
    }
}

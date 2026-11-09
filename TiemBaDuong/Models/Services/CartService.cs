using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TiemBaDuong.Models.Services
{
    /// <summary>
    /// Giỏ hàng lưu trong Session (key "Cart"). Giá, tên, ảnh, danh mục luôn lấy từ DB khi thêm
    /// và khi đồng bộ (Refresh). Không nhận giá từ client.
    /// </summary>
    public class CartService
    {
        public const int MaxQuantityPerItem = 99;
        private const string SessionKey = "Cart";

        private readonly HttpSessionStateBase session;
        private readonly mystoreEntities db;
        private List<CartItem> items;

        public CartService(HttpSessionStateBase session, mystoreEntities db)
        {
            this.session = session;
            this.db = db;
        }

        public List<CartItem> Items
        {
            get
            {
                if (items == null)
                {
                    items = session[SessionKey] as List<CartItem> ?? new List<CartItem>();
                }
                return items;
            }
        }

        public int TotalQuantity
        {
            get { return Items.Sum(i => i.Quantity); }
        }

        public decimal SelectedTotal
        {
            get { return Items.Where(i => i.Selected).Sum(i => i.Subtotal); }
        }

        public IList<CartItem> SelectedItems
        {
            get { return Items.Where(i => i.Selected).ToList(); }
        }

        /// <summary>Thêm sản phẩm. Trùng thì cộng dồn (Q16). Trả về thông báo lỗi, hoặc null nếu thành công.</summary>
        public string Add(int productId, int quantity)
        {
            if (quantity < 1 || quantity > MaxQuantityPerItem)
            {
                return "Số lượng phải từ 1 đến " + MaxQuantityPerItem + ".";
            }

            var info = LoadLiveProduct(productId);
            if (info == null)
            {
                return "Sản phẩm không tồn tại hoặc đã ngừng bán.";
            }

            var item = Items.FirstOrDefault(i => i.ProductID == productId);
            if (item == null)
            {
                item = new CartItem { Selected = true };
                Items.Add(item);
            }
            else if (item.Quantity + quantity > MaxQuantityPerItem)
            {
                return "Tổng số lượng sản phẩm này tối đa là " + MaxQuantityPerItem + ".";
            }
            else
            {
                quantity = item.Quantity + quantity;
            }

            ApplyLive(item, info);
            item.Quantity = quantity;
            Save();
            return null;
        }

        /// <summary>Cập nhật số lượng. Quantity 0 là lỗi validation, không tự xóa (Q16).</summary>
        public string UpdateQuantity(int productId, int quantity)
        {
            if (quantity < 1 || quantity > MaxQuantityPerItem)
            {
                return "Số lượng phải từ 1 đến " + MaxQuantityPerItem + ".";
            }
            var item = Items.FirstOrDefault(i => i.ProductID == productId);
            if (item == null) return "Sản phẩm không có trong giỏ.";
            item.Quantity = quantity;
            Save();
            return null;
        }

        public void Remove(int productId)
        {
            Items.RemoveAll(i => i.ProductID == productId);
            Save();
        }

        public void Clear()
        {
            Items.Clear();
            Save();
        }

        public void SetSelected(IEnumerable<int> selectedIds)
        {
            var set = new HashSet<int>(selectedIds ?? new int[0]);
            foreach (var item in Items)
            {
                item.Selected = set.Contains(item.ProductID);
            }
            Save();
        }

        /// <summary>Đồng bộ giá/tên từ DB, loại sản phẩm đã ngừng bán. Trả về danh sách thông báo.</summary>
        public IList<string> Refresh()
        {
            var messages = new List<string>();
            if (Items.Count == 0) return messages;

            var ids = Items.Select(i => i.ProductID).ToList();
            var live = db.Products.AsNoTracking()
                .Where(p => ids.Contains(p.ProductID) && p.IsActive)
                .Select(p => new LiveProduct
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    ProductImage = p.ProductImage,
                    ProductPrice = p.ProductPrice,
                    CategoryID = p.CategoryID,
                    CategoryName = p.Category.CategoryName
                })
                .ToList();

            foreach (var item in Items.ToList())
            {
                var info = live.FirstOrDefault(x => x.ProductID == item.ProductID);
                if (info == null)
                {
                    messages.Add(item.ProductName + " đã ngừng bán và được xóa khỏi giỏ.");
                    Items.Remove(item);
                }
                else
                {
                    ApplyLive(item, info);
                }
            }
            Save();
            return messages;
        }

        private LiveProduct LoadLiveProduct(int productId)
        {
            return db.Products.AsNoTracking()
                .Where(p => p.ProductID == productId && p.IsActive)
                .Select(p => new LiveProduct
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    ProductImage = p.ProductImage,
                    ProductPrice = p.ProductPrice,
                    CategoryID = p.CategoryID,
                    CategoryName = p.Category.CategoryName
                })
                .FirstOrDefault();
        }

        private static void ApplyLive(CartItem item, LiveProduct info)
        {
            item.ProductID = info.ProductID;
            item.ProductName = info.ProductName;
            item.ProductImage = info.ProductImage;
            item.UnitPrice = info.ProductPrice;
            item.CategoryID = info.CategoryID;
            item.CategoryName = info.CategoryName;
        }

        private void Save()
        {
            session[SessionKey] = Items;
        }

        private class LiveProduct
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public string ProductImage { get; set; }
            public decimal ProductPrice { get; set; }
            public int CategoryID { get; set; }
            public string CategoryName { get; set; }
        }
    }
}

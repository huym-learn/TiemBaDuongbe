using System.Collections.Generic;

namespace TiemBaDuong.Models
{
    /// <summary>Một dòng trong giỏ (lưu trong Session). Giá luôn được đồng bộ lại từ DB.</summary>
    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public bool Selected { get; set; }

        public decimal Subtotal
        {
            get { return UnitPrice * Quantity; }
        }
    }

    public class CartIndexVM
    {
        public IList<CartItem> Items { get; set; }
        public decimal SelectedTotal { get; set; }
        public int SelectedCount { get; set; }
        public IList<ProductCardVM> SimilarProducts { get; set; }
        public IList<string> Messages { get; set; }
    }
}

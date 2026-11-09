using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TiemBaDuong.Models.Services
{
    /// <summary>
    /// Truy vấn sản phẩm, danh mục, bán chạy. Mọi lọc/sắp xếp/phân trang nằm ở database.
    /// "Đơn hợp lệ" = đơn không bị hủy (OrderStatus <> Cancelled hoặc NULL với đơn cũ) — Q3.
    /// </summary>
    public class CatalogService
    {
        public const int FeaturedTake = 10;
        public const int NewProductsTake = 20;
        public const int HomeNewPageSize = 6;
        public const int SearchPageSize = 6;
        public const int CategoryPageSize = 6;
        public const int RelatedTake = 8;
        public const int TopDealsPageSize = 3;

        private readonly mystoreEntities db;

        public CatalogService(mystoreEntities db)
        {
            this.db = db;
        }

        public IList<Category> GetCategories()
        {
            return db.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToList();
        }

        public Category GetCategory(int id)
        {
            return db.Categories.AsNoTracking().FirstOrDefault(c => c.CategoryID == id);
        }

        // ---- Nền truy vấn ----

        // Phải là Expression (không phải phương thức C#) để EF6 dịch được thành SQL khi dùng trong subquery.
        private static readonly Expression<Func<OrderDetail, bool>> ValidSale =
            d => d.Order.OrderStatus == null || d.Order.OrderStatus != OrderStatuses.Cancelled;

        private IQueryable<OrderDetail> ValidSales()
        {
            return db.OrderDetails.Where(ValidSale);
        }

        private IQueryable<Product> ActiveProducts(string term)
        {
            var q = db.Products.AsNoTracking().Where(p => p.IsActive);
            if (!string.IsNullOrWhiteSpace(term))
            {
                string t = term.Trim();
                q = q.Where(p => p.ProductName.Contains(t)
                              || p.ProductDecription.Contains(t)
                              || p.Category.CategoryName.Contains(t));
            }
            return q;
        }

        private IQueryable<ProductCardVM> ToCards(IQueryable<Product> products)
        {
            return products.Select(p => new ProductCardVM
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                ProductPrice = p.ProductPrice,
                ProductDecription = p.ProductDecription,
                ProductImage = p.ProductImage,
                AudioDemoUrl = p.AudioDemoUrl,
                CategoryID = p.CategoryID,
                CategoryName = p.Category.CategoryName,
                Sold = db.OrderDetails.Where(ValidSale).Where(d => d.ProductID == p.ProductID).Sum(d => (int?)d.Quantity) ?? 0
            });
        }

        // ---- Trang chủ ----

        /// <summary>Top 10 sản phẩm bán chạy nhất (theo tổng Quantity đơn hợp lệ), áp dụng tìm kiếm.</summary>
        public IList<ProductCardVM> GetFeatured(string term)
        {
            var products = ActiveProducts(term);
            var productIds = products.Select(p => p.ProductID);

            var topIds = ValidSales()
                .Where(d => productIds.Contains(d.ProductID))
                .GroupBy(d => d.ProductID)
                .Select(g => new { ProductID = g.Key, Sold = g.Sum(x => x.Quantity) })
                .Where(x => x.Sold > 0)
                .OrderByDescending(x => x.Sold)
                .ThenBy(x => x.ProductID)
                .Take(FeaturedTake)
                .Select(x => x.ProductID)
                .ToList();

            if (topIds.Count == 0)
            {
                // Không có dữ liệu bán: không bịa số liệu, để trống (giả định A6).
                return new List<ProductCardVM>();
            }

            var cards = ToCards(products.Where(p => topIds.Contains(p.ProductID))).ToList();
            return cards.OrderBy(c => topIds.IndexOf(c.ProductID)).ToList();
        }

        /// <summary>Tối đa 20 sản phẩm mới nhất (theo CreatedDate, rồi ProductID). Phân trang ở controller.</summary>
        public IList<ProductCardVM> GetNewProductsPool(string term)
        {
            return ToCards(ActiveProducts(term)
                .OrderByDescending(p => p.CreatedDate)
                .ThenByDescending(p => p.ProductID)
                .Take(NewProductsTake)).ToList();
        }

        // ---- Danh mục ----

        public PagedResult<ProductCardVM> GetByCategory(int categoryId, int page)
        {
            var q = ActiveProducts(null)
                .Where(p => p.CategoryID == categoryId)
                .OrderByDescending(p => p.CreatedDate)
                .ThenByDescending(p => p.ProductID);
            return PagedResult<ProductCardVM>.FromQuery(ToCards(q), page, CategoryPageSize);
        }

        // ---- Tìm kiếm nâng cao ----

        public PagedResult<ProductCardVM> Search(ProductSearchVM vm, int page)
        {
            var q = ActiveProducts(vm.SearchTerm);
            if (vm.MinPrice.HasValue) q = q.Where(p => p.ProductPrice >= vm.MinPrice.Value);
            if (vm.MaxPrice.HasValue) q = q.Where(p => p.ProductPrice <= vm.MaxPrice.Value);

            IOrderedQueryable<Product> sorted;
            switch (vm.SortOrder)
            {
                case "name_asc":
                    sorted = q.OrderBy(p => p.ProductName).ThenBy(p => p.ProductID);
                    break;
                case "name_desc":
                    sorted = q.OrderByDescending(p => p.ProductName).ThenByDescending(p => p.ProductID);
                    break;
                case "price_asc":
                    sorted = q.OrderBy(p => p.ProductPrice).ThenBy(p => p.ProductID);
                    break;
                case "price_desc":
                    sorted = q.OrderByDescending(p => p.ProductPrice).ThenByDescending(p => p.ProductID);
                    break;
                default:
                    sorted = q.OrderByDescending(p => p.ProductID);
                    break;
            }
            return PagedResult<ProductCardVM>.FromQuery(ToCards(sorted), page, SearchPageSize);
        }

        // ---- Chi tiết ----

        public ProductCardVM GetDetail(int productId)
        {
            return ToCards(db.Products.AsNoTracking()
                .Where(p => p.ProductID == productId && p.IsActive)).FirstOrDefault();
        }

        public IList<ProductCardVM> GetRelated(int categoryId, int excludeProductId)
        {
            return ToCards(ActiveProducts(null)
                .Where(p => p.CategoryID == categoryId && p.ProductID != excludeProductId)
                .OrderByDescending(p => p.ProductID)
                .Take(RelatedTake)).ToList();
        }

        /// <summary>Top deals: cùng danh mục, sắp theo số đã bán giảm dần, phân trang 3/trang.</summary>
        public PagedResult<ProductCardVM> GetTopDeals(int categoryId, int excludeProductId, int page)
        {
            var q = ActiveProducts(null)
                .Where(p => p.CategoryID == categoryId && p.ProductID != excludeProductId)
                .OrderByDescending(p => db.OrderDetails.Where(ValidSale).Where(d => d.ProductID == p.ProductID).Sum(d => (int?)d.Quantity) ?? 0)
                .ThenBy(p => p.ProductID);
            return PagedResult<ProductCardVM>.FromQuery(ToCards(q), page, TopDealsPageSize);
        }

        /// <summary>Sản phẩm tương tự cho giỏ hàng: cùng danh mục, loại trừ sản phẩm đã có trong giỏ.</summary>
        public IList<ProductCardVM> GetSimilar(IList<int> categoryIds, IList<int> excludeProductIds, int take)
        {
            if (categoryIds == null || categoryIds.Count == 0) return new List<ProductCardVM>();
            var exclude = excludeProductIds ?? new List<int>();
            return ToCards(ActiveProducts(null)
                .Where(p => categoryIds.Contains(p.CategoryID) && !exclude.Contains(p.ProductID))
                .OrderByDescending(p => p.ProductID)
                .Take(take)).ToList();
        }
    }
}

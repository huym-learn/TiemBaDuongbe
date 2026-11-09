using System.Web.Mvc;
using System.Web.Routing;
using TiemBaDuong.Models;
using TiemBaDuong.Models.Services;

namespace TiemBaDuong.Controllers
{
    public class HomeController : Controller
    {
        private readonly mystoreEntities db = new mystoreEntities();
        private readonly CatalogService catalog;

        public HomeController()
        {
            catalog = new CatalogService(db);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        // GET: /Home/Index?searchTerm=&page=
        public ActionResult Index(string searchTerm, int? page)
        {
            int current = page.HasValue && page.Value > 0 ? page.Value : 1;

            var pool = catalog.GetNewProductsPool(searchTerm);
            var newPaged = PagedResult<ProductCardVM>.FromList(pool, current, CatalogService.HomeNewPageSize);

            var vm = new HomeProductVM
            {
                SearchTerm = searchTerm,
                Featured = catalog.GetFeatured(searchTerm),
                NewProducts = newPaged,
                NewPager = new PagerVM
                {
                    CurrentPage = newPaged.PageNumber,
                    TotalPages = newPaged.TotalPages,
                    Action = "Index",
                    Controller = "Home",
                    Values = new RouteValueDictionary { { "searchTerm", searchTerm } }
                }
            };
            return View(vm);
        }

        // GET: /Home/Search?searchTerm=&minPrice=&maxPrice=&sortOrder=&page=
        public ActionResult Search(ProductSearchVM query, int? page)
        {
            query.Page = page.HasValue && page.Value > 0 ? page.Value : 1;

            if (query.MinPrice.HasValue && query.MaxPrice.HasValue && query.MinPrice > query.MaxPrice)
            {
                ModelState.AddModelError("MinPrice", "Giá tối thiểu không được lớn hơn giá tối đa.");
            }
            if ((query.MinPrice.HasValue && query.MinPrice < 0) || (query.MaxPrice.HasValue && query.MaxPrice < 0))
            {
                ModelState.AddModelError("MinPrice", "Giá không được âm.");
            }

            if (ModelState.IsValid)
            {
                query.Products = catalog.Search(query, query.Page);
            }
            else
            {
                query.Products = new PagedResult<ProductCardVM>
                {
                    Items = new System.Collections.Generic.List<ProductCardVM>(),
                    PageNumber = 1,
                    PageSize = CatalogService.SearchPageSize,
                    TotalCount = 0
                };
            }

            query.Pager = new PagerVM
            {
                CurrentPage = query.Products.PageNumber,
                TotalPages = query.Products.TotalPages,
                Action = "Search",
                Controller = "Home",
                Values = new RouteValueDictionary
                {
                    { "searchTerm", query.SearchTerm },
                    { "minPrice", query.MinPrice },
                    { "maxPrice", query.MaxPrice },
                    { "sortOrder", query.SortOrder }
                }
            };
            return View(query);
        }

        // GET: /Home/ProductList/{categoryId}?page=
        public ActionResult ProductList(int categoryId, int? page)
        {
            var category = catalog.GetCategory(categoryId);
            if (category == null) return HttpNotFound();

            int current = page.HasValue && page.Value > 0 ? page.Value : 1;
            var products = catalog.GetByCategory(categoryId, current);

            var vm = new ProductListVM
            {
                CategoryID = category.CategoryID,
                CategoryName = category.CategoryName,
                Products = products,
                Pager = new PagerVM
                {
                    CurrentPage = products.PageNumber,
                    TotalPages = products.TotalPages,
                    Action = "ProductList",
                    Controller = "Home",
                    Values = new RouteValueDictionary { { "categoryId", categoryId } }
                }
            };
            return View(vm);
        }

        // GET: /Home/ProductDetail/{id}?topPage=
        public ActionResult ProductDetail(int id, int? topPage)
        {
            var product = catalog.GetDetail(id);
            if (product == null) return HttpNotFound();

            int current = topPage.HasValue && topPage.Value > 0 ? topPage.Value : 1;
            var topDeals = catalog.GetTopDeals(product.CategoryID, product.ProductID, current);

            var vm = new ProductDetailVM
            {
                Product = product,
                RelatedProducts = catalog.GetRelated(product.CategoryID, product.ProductID),
                TopDeals = topDeals,
                TopDealsPager = new PagerVM
                {
                    CurrentPage = topDeals.PageNumber,
                    TotalPages = topDeals.TotalPages,
                    Action = "ProductDetail",
                    Controller = "Home",
                    Values = new RouteValueDictionary { { "id", id } }
                }
            };
            return View(vm);
        }

        [ChildActionOnly]
        public ActionResult Categories()
        {
            return PartialView("_CategoriesPV", catalog.GetCategories());
        }
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Routing;

namespace TiemBaDuong.Models
{
    /// <summary>Thông tin sản phẩm để hiển thị. Không trả entity EF trực tiếp cho View.</summary>
    public class ProductCardVM
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public string ProductDecription { get; set; }
        public string ProductImage { get; set; }
        public string AudioDemoUrl { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public int Sold { get; set; }
    }

    /// <summary>Dữ liệu cho nút phân trang. Values giữ các tham số tìm kiếm khi chuyển trang.</summary>
    public class PagerVM
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public RouteValueDictionary Values { get; set; }

        public PagerVM()
        {
            Values = new RouteValueDictionary();
        }
    }

    public class HomeProductVM
    {
        public string SearchTerm { get; set; }
        public IList<ProductCardVM> Featured { get; set; }
        public PagedResult<ProductCardVM> NewProducts { get; set; }
        public PagerVM NewPager { get; set; }
    }

    public class ProductSearchVM
    {
        public string SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortOrder { get; set; }
        public int Page { get; set; }
        public PagedResult<ProductCardVM> Products { get; set; }
        public PagerVM Pager { get; set; }
    }

    public class ProductListVM
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public PagedResult<ProductCardVM> Products { get; set; }
        public PagerVM Pager { get; set; }
    }

    public class ProductDetailVM
    {
        public ProductCardVM Product { get; set; }
        public IList<ProductCardVM> RelatedProducts { get; set; }
        public PagedResult<ProductCardVM> TopDeals { get; set; }
        public PagerVM TopDealsPager { get; set; }
    }

    public class PersonalMenuVM
    {
        public bool IsLoggedIn { get; set; }
        public string Username { get; set; }
        public int CartCount { get; set; }
    }

    public class RegisterVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự.")]
        [RegularExpression(@"^[A-Za-z0-9_.]+$", ErrorMessage = "Chỉ dùng chữ không dấu, số, dấu gạch dưới hoặc dấu chấm.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^[0-9]{9,15}$", ErrorMessage = "Số điện thoại chỉ gồm 9 đến 15 chữ số.")]
        public string CustomerPhone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(200)]
        public string CustomerEmail { get; set; }

        [StringLength(500)]
        public string CustomerAddress { get; set; }
    }

    public class LoginVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ProfileVM
    {
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^[0-9]{9,15}$", ErrorMessage = "Số điện thoại chỉ gồm 9 đến 15 chữ số.")]
        public string CustomerPhone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(200)]
        public string CustomerEmail { get; set; }

        [StringLength(500)]
        public string CustomerAddress { get; set; }
    }

    public class ChangePasswordVM
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới tối thiểu 8 ký tự.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; }
    }

    public class CheckoutVM
    {
        public string DefaultAddress { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự.")]
        public string NewShippingAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức giao hàng.")]
        public string DeliveryMethod { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string PaymentMethod { get; set; }

        [Required]
        public string CheckoutToken { get; set; }

        public IList<CartItem> Items { get; set; }
        public decimal Total { get; set; }
        public IList<string> DeliveryOptions { get; set; }
        public IList<string> PaymentOptions { get; set; }
    }

    public class OrderHistoryVM
    {
        public string Status { get; set; }
        public string Q { get; set; }
        public PagedResult<Order> Orders { get; set; }
        public PagerVM Pager { get; set; }
    }

    public class AdminLoginVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class DashboardVM
    {
        public int ProductCount { get; set; }
        public int CategoryCount { get; set; }
        public int CustomerCount { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }
}

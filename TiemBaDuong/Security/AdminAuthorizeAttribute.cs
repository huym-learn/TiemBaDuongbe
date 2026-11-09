using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using TiemBaDuong.Models;

namespace TiemBaDuong.Security
{
    /// <summary>
    /// Chỉ cho phép tài khoản có UserRole = 'A' trong DB. Kiểm tra trên server ở mọi request,
    /// không dựa vào cookie hay việc ẩn link trên View (spec mục 5.1, 8).
    /// Chưa đăng nhập: chuyển tới trang đăng nhập Admin. Đã đăng nhập nhưng không phải Admin: 403.
    /// </summary>
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null || !httpContext.User.Identity.IsAuthenticated) return false;

            string name = httpContext.User.Identity.Name;
            using (var db = new mystoreEntities())
            {
                return db.Users.Any(u => u.Username == name && u.UserRole == "A");
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new HttpStatusCodeResult(403);
            }
            else
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    area = "Admin",
                    controller = "Account",
                    action = "Login"
                }));
            }
        }
    }
}

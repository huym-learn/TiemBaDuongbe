using System.Web.Mvc;
using System.Web.Routing;

namespace TiemBaDuong
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Area Admin đã được đăng ký trước qua AreaRegistration.RegisterAllAreas().
            // Giới hạn namespace để không lẫn với controller cùng tên trong Area.
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "TiemBaDuong.Controllers" }
            );
        }
    }
}

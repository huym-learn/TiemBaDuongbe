using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using TiemBaDuong.Models;
using TiemBaDuong.Models.Services;

namespace TiemBaDuong.Areas.Admin.Controllers
{
    public class AccountController : Controller
    {
        private readonly mystoreEntities db = new mystoreEntities();

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new AdminLoginVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(AdminLoginVM model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            var user = db.Users.FirstOrDefault(u => u.Username == model.Username && u.UserRole == "A");
            if (user == null || !PasswordService.Verify(model.Password, user.Password))
            {
                ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu.");
                return View(model);
            }

            FormsAuthentication.SetAuthCookie(user.Username, false);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }
}

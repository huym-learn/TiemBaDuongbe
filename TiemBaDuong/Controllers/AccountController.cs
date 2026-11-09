using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using TiemBaDuong.Models;
using TiemBaDuong.Models.Services;
using UserAccount = TiemBaDuong.User;

namespace TiemBaDuong.Controllers
{
    public class AccountController : Controller
    {
        private readonly mystoreEntities db = new mystoreEntities();

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        // ---- Menu cá nhân (child action trong layout) ----

        [ChildActionOnly]
        public ActionResult PersonalMenu()
        {
            bool loggedIn = User.Identity.IsAuthenticated;
            var cart = new CartService(Session, db);
            var vm = new PersonalMenuVM
            {
                IsLoggedIn = loggedIn,
                Username = loggedIn ? User.Identity.Name : null,
                CartCount = cart.TotalQuantity
            };
            return PartialView("_PersonalMenuPV", vm);
        }

        // ---- Đăng ký ----

        [HttpGet]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View(new RegisterVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVM model)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            if (!ModelState.IsValid) return View(model);

            if (db.Users.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            // User + Customer trong một transaction (spec mục 4.6)
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    db.Users.Add(new UserAccount
                    {
                        Username = model.Username,
                        Password = PasswordService.Hash(model.Password),
                        UserRole = "C"
                    });
                    db.Customers.Add(new Customer
                    {
                        Username = model.Username,
                        CustomerName = model.CustomerName.Trim(),
                        CustomerPhone = model.CustomerPhone.Trim(),
                        CustomerEmail = string.IsNullOrWhiteSpace(model.CustomerEmail) ? null : model.CustomerEmail.Trim(),
                        CustomerAddress = string.IsNullOrWhiteSpace(model.CustomerAddress) ? null : model.CustomerAddress.Trim()
                    });
                    db.SaveChanges();
                    tx.Commit();
                }
                catch (DbUpdateException)
                {
                    tx.Rollback();
                    ModelState.AddModelError("Username", "Không thể tạo tài khoản. Tên đăng nhập có thể đã tồn tại.");
                    return View(model);
                }
            }

            FormsAuthentication.SetAuthCookie(model.Username, false);
            TempData["Success"] = "Đăng ký thành công. Chào mừng bạn đến với Tiệm Bá Đường!";
            return RedirectToAction("Index", "Home");
        }

        // ---- Đăng nhập / đăng xuất ----

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginVM model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            // Tài khoản Admin (UserRole = 'A') đăng nhập qua khu Admin, không qua trang khách.
            var user = db.Users.FirstOrDefault(u => u.Username == model.Username);
            if (user == null || user.UserRole == "A" || !PasswordService.Verify(model.Password, user.Password))
            {
                ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            FormsAuthentication.SetAuthCookie(user.Username, model.RememberMe);
            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            // Chỉ xóa Forms Authentication cookie. Giỏ hàng (Session) giữ nguyên.
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        // ---- Hồ sơ ----

        [Authorize]
        [HttpGet]
        public ActionResult ProfileInfo()
        {
            var customer = CurrentCustomer();
            if (customer == null)
            {
                TempData["Error"] = "Tài khoản này chưa có hồ sơ khách hàng.";
                return RedirectToAction("Index", "Home");
            }
            return View(ToProfileVM(customer));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(ProfileVM model)
        {
            var customer = CurrentCustomer();
            if (customer == null) return HttpNotFound();

            // Username lấy từ người đăng nhập, không lấy từ form.
            model.Username = customer.Username;
            if (!ModelState.IsValid) return View("ProfileInfo", model);

            customer.CustomerName = model.CustomerName.Trim();
            customer.CustomerPhone = model.CustomerPhone.Trim();
            customer.CustomerEmail = string.IsNullOrWhiteSpace(model.CustomerEmail) ? null : model.CustomerEmail.Trim();
            customer.CustomerAddress = string.IsNullOrWhiteSpace(model.CustomerAddress) ? null : model.CustomerAddress.Trim();
            db.SaveChanges();

            TempData["Success"] = "Đã cập nhật thông tin cá nhân.";
            return RedirectToAction("ProfileInfo");
        }

        [Authorize]
        [HttpGet]
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordVM());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = db.Users.FirstOrDefault(u => u.Username == User.Identity.Name);
            if (user == null || !PasswordService.Verify(model.CurrentPassword, user.Password))
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            user.Password = PasswordService.Hash(model.NewPassword);
            db.SaveChanges();

            TempData["Success"] = "Đã đổi mật khẩu.";
            return RedirectToAction("ProfileInfo");
        }

        // ---- Helper ----

        internal Customer CurrentCustomer()
        {
            string name = User.Identity.Name;
            return db.Customers.FirstOrDefault(c => c.Username == name);
        }

        private static ProfileVM ToProfileVM(Customer c)
        {
            return new ProfileVM
            {
                Username = c.Username,
                CustomerName = c.CustomerName,
                CustomerPhone = c.CustomerPhone,
                CustomerEmail = c.CustomerEmail,
                CustomerAddress = c.CustomerAddress
            };
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SEN_T_PAZAR.Models;

namespace SEN_T_PAZAR.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Models.LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
                return View(model);
            }
            var userName = user.UserName ?? user.Email ?? string.Empty;
            var result = await _signInManager.PasswordSignInAsync(userName, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim("FullName", user.FullName ?? user.UserName ?? user.Email ?? "Kullanıcı")
                };
                await _signInManager.SignInWithClaimsAsync(user, model.RememberMe, claims);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Models.RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // İsterseniz otomatik giriş de yapılabilir
                // await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Login");
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        [Authorize]
        [HttpGet]
        public IActionResult Profile() => RedirectToAction(nameof(Dashboard));

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Dashboard(string tab = "overview")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            ViewData["ActiveTab"] = tab;
            return View(await BuildDashboardViewModel(user));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileUpdateViewModel form)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                ViewData["ActiveTab"] = "profile";
                var vm = await BuildDashboardViewModel(user);
                vm.ProfileForm = form;
                return View("Dashboard", vm);
            }

            user.FullName = form.FullName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(form.PhoneNumber) ? null : form.PhoneNumber.Trim();
            user.AddressLine = string.IsNullOrWhiteSpace(form.AddressLine) ? null : form.AddressLine.Trim();
            user.City = string.IsNullOrWhiteSpace(form.City) ? null : form.City.Trim();
            user.AvatarUrl = string.IsNullOrWhiteSpace(form.AvatarUrl) ? null : form.AvatarUrl.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewData["ActiveTab"] = "profile";
                var vm = await BuildDashboardViewModel(user);
                vm.ProfileForm = form;
                return View("Dashboard", vm);
            }

            TempData["DashboardSuccess"] = "Profil bilgileriniz güncellendi.";
            return RedirectToAction(nameof(Dashboard), new { tab = "profile" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel form)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                ViewData["ActiveTab"] = "security";
                var vm = await BuildDashboardViewModel(user);
                vm.PasswordForm = form;
                return View("Dashboard", vm);
            }

            var result = await _userManager.ChangePasswordAsync(user, form.CurrentPassword, form.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewData["ActiveTab"] = "security";
                var vm = await BuildDashboardViewModel(user);
                vm.PasswordForm = form;
                return View("Dashboard", vm);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["DashboardSuccess"] = "Şifreniz başarıyla güncellendi.";
            return RedirectToAction(nameof(Dashboard), new { tab = "security" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotificationSettings(NotificationSettingsViewModel form)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            user.EmailNotifications = form.EmailNotifications;
            user.SmsNotifications = form.SmsNotifications;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewData["ActiveTab"] = "notifications";
                var vm = await BuildDashboardViewModel(user);
                vm.NotificationForm = form;
                return View("Dashboard", vm);
            }

            TempData["DashboardSuccess"] = "Bildirim tercihleriniz güncellendi.";
            return RedirectToAction(nameof(Dashboard), new { tab = "notifications" });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyListings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            return RedirectToAction(nameof(Dashboard), new { tab = "listings" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private async Task<AccountDashboardViewModel> BuildDashboardViewModel(ApplicationUser user)
        {
            var myListings = await _db.Listings
                .Where(x => x.UserId == user.Id || x.Phone == user.PhoneNumber || x.FullName == user.FullName)
                .OrderByDescending(x => x.CreatedAt)
                .Take(50)
                .Select(x => new MyListingCardViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    City = x.City,
                    PriceAmount = x.PriceAmount,
                    PriceCurrency = x.PriceCurrency,
                    IsApproved = x.IsApproved,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            var stats = new DashboardStatsViewModel
            {
                TotalListings = myListings.Count,
                ApprovedListings = myListings.Count(x => x.IsApproved),
                PendingListings = myListings.Count(x => !x.IsApproved),
                FavoritesCount = 0
            };

            return new AccountDashboardViewModel
            {
                User = user,
                ProfileForm = new ProfileUpdateViewModel
                {
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    AddressLine = user.AddressLine,
                    City = user.City,
                    AvatarUrl = user.AvatarUrl
                },
                PasswordForm = new ChangePasswordViewModel(),
                NotificationForm = new NotificationSettingsViewModel
                {
                    EmailNotifications = user.EmailNotifications,
                    SmsNotifications = user.SmsNotifications
                },
                MyListings = myListings,
                Favorites = [],
                BillingItems =
                [
                    new BillingItemViewModel
                    {
                        InvoiceNo = "INV-2026-001",
                        Date = DateTime.UtcNow.AddDays(-10),
                        Amount = 0,
                        Currency = "TL",
                        Status = "Ücretsiz Plan"
                    }
                ],
                Stats = stats
            };
        }
    }
}

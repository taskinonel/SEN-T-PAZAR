using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using SEN_T_PAZAR.Models;
using SEN_T_PAZAR.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace SEN_T_PAZAR.Controllers
{
    public class AccountController : Controller
    {
        private const int MaxUploadImageDimension = 1600;
        private const int UploadJpegQuality = 78;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;
        private readonly EmailSender _emailSender;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IUploadStorageService _uploadStorage;
        private readonly IListingCatalogService _catalog;
        private readonly SiteLocalizer _localizer;
        private readonly IUserMessageAutomationService _userMessageAutomationService;
        private readonly ILogger<AccountController> _logger;
        private readonly IHubContext<SEN_T_PAZAR.Hubs.ChatHub> _chatHubContext;


        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext db,
            EmailSender emailSender,
            IWebHostEnvironment webHostEnvironment,
            IUploadStorageService uploadStorage,
            IListingCatalogService catalog,
            SiteLocalizer localizer,
            IUserMessageAutomationService userMessageAutomationService,
            ILogger<AccountController> logger,
            IHubContext<SEN_T_PAZAR.Hubs.ChatHub> chatHubContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _emailSender = emailSender;
            _webHostEnvironment = webHostEnvironment;
            _uploadStorage = uploadStorage;
            _catalog = catalog;
            _localizer = localizer;
            _userMessageAutomationService = userMessageAutomationService;
            _logger = logger;
            _chatHubContext = chatHubContext;
        }

        private string T(string tr, string en, string ru, string ar, string? fa = null) => _localizer.CultureCode switch
        {
            "en" => en,
            "ru" => ru,
            "ar" => ar,
            "fa" => fa ?? en ?? tr,
            _ => tr
        };

        private string GetCorporatePlanLabel(string? planId)
        {
            var normalized = (planId ?? "free").Trim().ToLowerInvariant();
            return normalized switch
            {
                "free" or "corporate free" or "ücretsiz kurumsal" => T("Ücretsiz Kurumsal", "Corporate Free", "Бесплатный корпоративный", "الخطة المؤسسية المجانية", "شرکتی رایگان"),
                "basic" or "başlangıç vitrin" => T("Başlangıç Vitrin", "Starter Showcase", "Стартовая витрина", "واجهة البداية", "ویترین پایه"),
                "pro" or "profesyonel öne çıkan" => T("Profesyonel Öne Çıkan", "Professional Featured", "Профессиональный премиум", "الباقة الاحترافية المميزة", "ویژه حرفه‌ای"),
                "enterprise" or "mega kombinasyon" => T("Mega Kombinasyon", "Mega Combination", "Мега-комбинация", "الباقة العملاقة", "ترکیب مگا"),
                _ => string.IsNullOrWhiteSpace(planId)
                    ? T("Ücretsiz Kurumsal", "Corporate Free", "Бесплатный корпоративный", "الخطة المؤسسية المجانية", "شرکتی رایگان")
                    : planId
            };
        }

        private string GetCorporateMembershipStatus(ApplicationUser user)
        {
            return user.IsCorporateMember
                ? $"{T("Kurumsal Üye", "Corporate Member", "Корпоративный участник", "عضو مؤسسي", "عضو شرکتی")} ({GetCorporatePlanLabel(user.SubscriptionPlan)})"
                : T("Ücretsiz Plan", "Free Plan", "Бесплатный план", "الخطة المجانية", "پلن رایگان");
        }

        [HttpGet]
        public IActionResult Login(string? externalError = null)
        {
            if (!string.IsNullOrWhiteSpace(externalError))
            {
                TempData["ExternalLoginError"] = externalError;
            }

            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return RedirectToAction(nameof(Login));
            }

            var externalSchemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            var providerAvailable = externalSchemes.Any(s =>
                s.Name.Equals(provider, StringComparison.OrdinalIgnoreCase) ||
                (s.DisplayName?.Equals(provider, StringComparison.OrdinalIgnoreCase) ?? false));

            if (!providerAvailable)
            {
                TempData["ExternalLoginError"] = T(
                    "Google girisi su anda kullanilamiyor. Lutfen daha sonra tekrar deneyin.",
                    "Google sign-in is currently unavailable. Please try again later.",
                    "Вход через Google сейчас недоступен. Пожалуйста, попробуйте позже.",
                    "تسجيل الدخول عبر Google غير متاح حاليًا. يرجى المحاولة لاحقًا.",
                    "ورود با Google در حال حاضر در دسترس نیست. لطفاً بعداً دوباره تلاش کنید.");
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                TempData["ExternalLoginError"] = T("Harici giriş sırasında bir hata oluştu.", "An error occurred during external login.", "Произошла ошибка при внешнем входе.", "حدث خطأ أثناء تسجيل الدخول الخارجي.", "در هنگام ورود خارجی خطایی رخ داد.") + $" ({remoteError})";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ExternalLoginError"] = T("Harici giriş bilgisi alınamadı.", "External login information could not be retrieved.", "Не удалось получить данные внешнего входа.", "تعذر جلب معلومات تسجيل الدخول الخارجي.", "اطلاعات ورود خارجی دریافت نشد.");
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (signInResult.Succeeded)
            {
                var linkedUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (linkedUser != null)
                {
                    var fullName = linkedUser.FullName ?? linkedUser.UserName ?? linkedUser.Email ?? "User";
                    await _signInManager.SignInWithClaimsAsync(linkedUser, isPersistent: false, new[]
                    {
                        new Claim("FullName", fullName)
                    });
                }
                return LocalRedirect(returnUrl);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ExternalLoginError"] = T("Google hesabınızda e-posta bilgisi bulunamadı.", "No email information was found in your Google account.", "В аккаунте Google не найден адрес электронной почты.", "لم يتم العثور على بريد إلكتروني في حساب Google الخاص بك.", "در حساب Google شما اطلاعات ایمیل یافت نشد.");
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var user = await _userManager.FindByEmailAsync(email);
            var isNewUser = false;
            if (user == null)
            {
                var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;
                var userName = await GenerateUniqueUserNameAsync(email);

                user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    TempData["ExternalLoginError"] = T("Google hesabı ile kayıt oluşturulamadı.", "Unable to create an account with Google.", "Не удалось создать учетную запись через Google.", "تعذر إنشاء حساب عبر Google.", "ایجاد حساب با Google ممکن نشد.");
                    return RedirectToAction(nameof(Login), new { returnUrl });
                }

                isNewUser = true;
            }
            else if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                var confirmResult = await _userManager.UpdateAsync(user);
                if (!confirmResult.Succeeded)
                {
                    TempData["ExternalLoginError"] = T("Google hesabı doğrulanamadı.", "Google account could not be marked as verified.", "Аккаунт Google не удалось отметить как подтвержденный.", "تعذر اعتبار حساب Google موثقًا.", "نشانه‌گذاری حساب Google به‌عنوان تأییدشده ممکن نشد.");
                    return RedirectToAction(nameof(Login), new { returnUrl });
                }
            }

            var hasLogin = (await _userManager.GetLoginsAsync(user)).Any(x => x.LoginProvider == info.LoginProvider && x.ProviderKey == info.ProviderKey);
            if (!hasLogin)
            {
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded)
                {
                    TempData["ExternalLoginError"] = T("Google hesabı bağlanamadı.", "Google account could not be linked.", "Не удалось привязать аккаунт Google.", "تعذر ربط حساب Google.", "اتصال حساب Google ممکن نشد.");
                    return RedirectToAction(nameof(Login), new { returnUrl });
                }
            }

            if (isNewUser)
            {
                await _userMessageAutomationService.SendWelcomeAsync(user);
            }


            // 2FA kaldırıldı: Doğrudan giriş yapılır.

            await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, new[]
            {
                new Claim("FullName", user.FullName ?? user.UserName ?? user.Email ?? "User")
            });

            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-post")]
        public async Task<IActionResult> Login(Models.LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var identifier = model.Email.Trim();
            ApplicationUser? user;

            if (identifier.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(identifier);
                // Case-insensitive fallback
                if (user == null)
                    user = await _userManager.FindByEmailAsync(identifier.ToLowerInvariant());
            }
            else
            {
                user = await _userManager.FindByNameAsync(identifier);
                if (user == null)
                    user = await _userManager.FindByNameAsync(identifier.ToLowerInvariant());
            }

            // Special case: allow username-based email lookup
            if (user == null && !identifier.Contains('@') && identifier.Equals("taskinonel", StringComparison.OrdinalIgnoreCase))
            {
                user = await _userManager.FindByEmailAsync($"{identifier}@gmail.com");
            }

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, T(
                    "E-posta veya kullanıcı adı bulunamadı. E-posta/kullanıcı adınızı ve şifrenizi kontrol edin.",
                    "Email or username not found. Check your credentials.",
                    "Адрес эл. почты или имя пользователя не найдены. Проверьте данные.",
                    "لم يتم العثور على البريد الإلكتروني أو اسم المستخدم. تحقق من بياناتك.",
                    "ایمیل یا نام کاربری یافت نشد. اعتبارنامه‌ها را بررسی کنید."));
                return View(model);
            }

            // Check if user has a local password (external login users may not have one)
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, T(
                    "Bu hesap harici bir sağlayıcı ile oluşturulmuş olabilir; lütfen Google ile giriş yapın veya şifrenizi sıfırlayın.",
                    "This account may have been created via an external provider; please sign in with Google or reset your password.",
                    "Эта учетная запись, возможно, создана через внешний поставщик; войдите через Google или сбросьте пароль.",
                    "قد تم إنشاء هذا الحساب عبر مزود خارجي؛ يرجى تسجيل الدخول عبر Google أو إعادة تعيين كلمة المرور.",
                    "این حساب ممکن است از طریق ارائه‌دهنده خارجی ایجاد شده باشد؛ لطفاً با Google وارد شوید یا رمز عبور را بازنشانی کنید."));
                return View(model);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, T(
                        "Hesabınız geçici olarak kilitlenmiş. Bir süre sonra tekrar deneyin veya şifre sıfırlama isteğinde bulunun.",
                        "Your account is temporarily locked. Try again later or request a password reset.",
                        "Ваша учетная запись временно заблокирована. Повторите попытку позже или сбросьте пароль.",
                        "تم قفل حسابك مؤقتًا. حاول مرة أخرى لاحقًا أو اطلب إعادة تعيين كلمة المرور.",
                        "حساب شما موقتاً قفل شده است. بعداً دوباره تلاش کنید یا بازنشانی رمز عبور را درخواست کنید."));
                }
                else if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, T(
                        "Giriş yetkisi yok. E-posta adresinizi doğrulamamış olabilirsiniz.",
                        "Sign-in not allowed. You may need to verify your email address.",
                        "Вход запрещен. Возможно, вам нужно подтвердить адрес электронной почты.",
                        "تسجيل الدخول غير مسموح. قد تحتاج إلى التحقق من بريدك الإلكتروني.",
                        "ورود مجاز نیست. ممکن است نیاز به تأیید ایمیل داشته باشید."));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, T(
                        "E-posta veya şifre hatalı. Lütfen bilgilerinizi kontrol edin.",
                        "Email or password is incorrect. Please check your credentials.",
                        "Неверный адрес эл. почты или пароль. Проверьте данные.",
                        "البريد الإلكتروني أو كلمة المرور غير صحيحة. تحقق من بياناتك.",
                        "ایمیل یا رمز عبور اشتباه است. لطفاً اعتبارنامه‌ها را بررسی کنید."));
                }

                return View(model);
            }

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("FullName", user.FullName ?? user.UserName ?? user.Email ?? "Kullanıcı")
            };
            await _userManager.ResetAccessFailedCountAsync(user);
            await _signInManager.SignInWithClaimsAsync(user, model.RememberMe, claims);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth-post")]
        public async Task<IActionResult> Register(Models.RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.UserName = (model.UserName ?? string.Empty).Trim();
            model.FullName = (model.FullName ?? string.Empty).Trim();
            model.Email = (model.Email ?? string.Empty).Trim();

            var normalizedEmail = _userManager.NormalizeEmail(model.Email);
            var emailExists = await _userManager.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), T("Bu e-posta adresi zaten kullanılıyor.", "This email address is already in use.", "Этот адрес электронной почты уже используется.", "عنوان البريد الإلكتروني هذا مستخدم بالفعل.", "این آدرس ایمیل قبلاً استفاده شده است."));
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = false
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                try
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
                    var confirmationUrl = Url.Action(
                        nameof(ConfirmEmail),
                        "Account",
                        new { userId = user.Id, code = encodedToken },
                        protocol: Request.Scheme,
                        host: Request.Host.Value) ?? string.Empty;

                    var subject = T(
                        "SEN-T PAZAR E-posta Doğrulama",
                        "SEN-T PAZAR Email Confirmation",
                        "Подтверждение электронной почты SEN-T PAZAR",
                        "تأكيد البريد الإلكتروني SEN-T PAZAR",
                        "تأیید ایمیل SEN-T PAZAR");
                    var body = $@"<div style='font-family:Arial,sans-serif;line-height:1.6;color:#1f2937'>
                        <h2 style='margin:0 0 16px'>SEN-T PAZAR</h2>
                        <p>{T("Kaydınız başarıyla oluşturuldu. Hesabınızı etkinleştirmek için aşağıdaki butona tıklayın.", "Your account has been created successfully. Click the button below to activate it.", "Ваша учетная запись успешно создана. Нажмите кнопку ниже, чтобы активировать ее.", "تم إنشاء حسابك بنجاح. انقر الزر أدناه لتفعيل الحساب.", "حساب شما با موفقیت ایجاد شد. برای فعال‌سازی روی دکمه زیر کلیک کنید.")}</p>
                        <p style='margin:24px 0'>
                            <a href='{confirmationUrl}' style='display:inline-block;background:#0d6efd;color:#fff;text-decoration:none;padding:12px 20px;border-radius:8px;font-weight:700'>
                                {T("E-postayı Doğrula", "Confirm Email", "Подтвердить e-mail", "تأكيد البريد الإلكتروني", "تأیید ایمیل")}
                            </a>
                        </p>
                        <p style='font-size:14px;color:#6b7280'>{T("Buton çalışmazsa şu bağlantıyı kopyalayın:", "If the button does not work, copy this link:", "Если кнопка не работает, скопируйте эту ссылку:", "إذا لم يعمل الزر، انسخ هذا الرابط:", "اگر دکمه کار نکرد، این لینک را کپی کنید:")}<br /><a href='{confirmationUrl}'>{confirmationUrl}</a></p>
                    </div>";

                    await _emailSender.SendAsync(model.Email, subject, body);
                }
                catch (Exception ex)
                {
                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError(string.Empty, T(
                        "Doğrulama e-postası gönderilemedi. Lütfen daha sonra tekrar deneyin.",
                        "The verification email could not be sent. Please try again later.",
                        "Не удалось отправить письмо для подтверждения. Пожалуйста, попробуйте позже.",
                        "تعذر إرسال رسالة التحقق. يرجى المحاولة لاحقًا.",
                        "ایمیل تأیید ارسال نشد. لطفاً بعداً دوباره تلاش کنید."));
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(model);
                }

                await _userMessageAutomationService.SendWelcomeAsync(user);

                TempData["EmailConfirmationInfo"] = T(
                    "Kaydınız oluşturuldu. E-posta adresinize gönderilen doğrulama bağlantısına tıklayın, ardından giriş yapın.",
                    "Your account has been created. Click the verification link sent to your email, then sign in.",
                    "Аккаунт создан. Нажмите ссылку подтверждения, отправленную на вашу почту, затем войдите.",
                    "تم إنشاء حسابك. انقر على رابط التحقق المرسل إلى بريدك الإلكتروني ثم سجّل الدخول.",
                    "حساب شما ایجاد شد. روی لینک تأیید ارسال‌شده به ایمیلتان کلیک کنید، سپس وارد شوید.");
                return RedirectToAction(nameof(Login));
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                TempData["EmailConfirmationWarning"] = T(
                    "Doğrulama bağlantısı geçersiz.",
                    "The confirmation link is invalid.",
                    "Ссылка подтверждения недействительна.",
                    "رابط التأكيد غير صالح.",
                    "لینک تأیید نامعتبر است.");
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["EmailConfirmationWarning"] = T(
                    "Kullanıcı bulunamadı.",
                    "User not found.",
                    "Пользователь не найден.",
                    "المستخدم غير موجود.",
                    "کاربر یافت نشد.");
                return RedirectToAction(nameof(Login));
            }

            if (user.EmailConfirmed)
            {
                TempData["EmailConfirmationSuccess"] = T(
                    "E-posta adresiniz zaten doğrulanmış.",
                    "Your email address is already confirmed.",
                    "Ваш адрес электронной почты уже подтвержден.",
                    "تم تأكيد بريدك الإلكتروني بالفعل.",
                    "ایمیل شما قبلاً تأیید شده است.");
                return RedirectToAction(nameof(Login));
            }

            string token;
            try
            {
                token = System.Text.Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch
            {
                TempData["EmailConfirmationWarning"] = T(
                    "Doğrulama bağlantısı bozuk.",
                    "The confirmation link is malformed.",
                    "Ссылка подтверждения повреждена.",
                    "رابط التأكيد غير صالح.",
                    "لینک تأیید خراب است.");
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                TempData["EmailConfirmationWarning"] = T(
                    "E-posta doğrulanamadı. Bağlantı süresi dolmuş olabilir.",
                    "Email could not be confirmed. The link may have expired.",
                    "Не удалось подтвердить e-mail. Срок действия ссылки мог истечь.",
                    "تعذر تأكيد البريد الإلكتروني. قد يكون الرابط منتهي الصلاحية.",
                    "تأیید ایمیل انجام نشد. ممکن است لینک منقضی شده باشد.");
                return RedirectToAction(nameof(Login));
            }

            TempData["EmailConfirmationSuccess"] = T(
                "E-posta adresiniz doğrulandı. Artık giriş yapabilirsiniz.",
                "Your email address has been confirmed. You can now sign in.",
                "Ваш адрес электронной почты подтвержден. Теперь вы можете войти.",
                "تم تأكيد بريدك الإلكتروني. يمكنك تسجيل الدخول الآن.",
                "ایمیل شما تأیید شد. اکنون می‌توانید وارد شوید.");
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        // 2FA kaldırıldıktan sonra kalan boş satır ve fazla kapama süslü parantezi temizlendi.

        [Authorize]
        [HttpGet]
        public IActionResult Profile() => RedirectToAction(nameof(Dashboard));

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetTwoFactor(bool enabled)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (!enabled && isAdmin)
            {
                TempData["DashboardSuccess"] = T(
                    "Yonetici hesaplarinda iki adimli dogrulama kapatilamaz.",
                    "Two-factor authentication cannot be disabled for admin accounts.",
                    "Для учетных записей администратора нельзя отключить двухфакторную аутентификацию.",
                    "لا يمكن تعطيل المصادقة الثنائية لحسابات المسؤول.",
                    "برای حساب‌های مدیر نمی‌توان تأیید دومرحله‌ای را غیرفعال کرد.");
                return RedirectToAction(nameof(Dashboard), new { tab = "security" });
            }

            if (enabled && (string.IsNullOrWhiteSpace(user.Email) || !user.EmailConfirmed))
            {
                TempData["DashboardSuccess"] = T(
                    "Iki adimli dogrulama acmak icin once e-posta adresinizi dogrulayin.",
                    "Please verify your email address before enabling two-factor authentication.",
                    "Подтвердите адрес электронной почты перед включением двухфакторной аутентификации.",
                    "يرجى التحقق من عنوان بريدك الإلكتروني قبل تفعيل المصادقة الثنائية.",
                    "برای فعال‌سازی تأیید دومرحله‌ای ابتدا ایمیل خود را تأیید کنید.");
                return RedirectToAction(nameof(Dashboard), new { tab = "security" });
            }

            await _userManager.SetTwoFactorEnabledAsync(user, enabled);
            await _signInManager.RefreshSignInAsync(user);

            TempData["DashboardSuccess"] = enabled
                ? T("Iki adimli dogrulama aktif edildi.", "Two-factor authentication enabled.", "Двухфакторная аутентификация включена.", "تم تفعيل المصادقة الثنائية.", "تأیید دومرحله‌ای فعال شد.")
                : T("Iki adimli dogrulama devre disi birakildi.", "Two-factor authentication disabled.", "Двухфакторная аутентификация отключена.", "تم تعطيل المصادقة الثنائية.", "تأیید دومرحله‌ای غیرفعال شد.");

            return RedirectToAction(nameof(Dashboard), new { tab = "security" });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Dashboard(string tab = "overview", int? threadId = null, string filter = "all")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (string.Equals(tab, "favorites", StringComparison.OrdinalIgnoreCase))
            {
                return LocalRedirect("/Account/Favorites");
            }

            // Admin accounts must stay protected with step-up verification.
            // If the flag is still false but email is verified, auto-enable to keep UI and policy in sync.
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin && !user.TwoFactorEnabled && user.EmailConfirmed && !string.IsNullOrWhiteSpace(user.Email))
            {
                await _userManager.SetTwoFactorEnabledAsync(user, true);
                await _signInManager.RefreshSignInAsync(user);

                var refreshedUser = await _userManager.FindByIdAsync(user.Id);
                if (refreshedUser != null)
                {
                    user = refreshedUser;
                }
            }

            ViewData["ActiveTab"] = tab;
            ViewData["MsgFilter"] = filter;
            var model = await BuildDashboardViewModel(user);
            model.Filter = filter;
            if (filter == "unread")
            {
                model.VisitorMessages = model.VisitorMessages.Where(t => !t.IsRead).ToList();
            }
            // Auto-mark thread messages as read when viewing
            if (threadId.HasValue)
            {
                model.SelectedThreadId = threadId.Value;
                
                // Mark all messages in this thread as read
                var selectedThread = model.VisitorMessages.FirstOrDefault(t => t.Id == threadId);
                var conversationId = selectedThread?.ConversationId ?? $"legacy-{threadId}";
                var messagesToUpdate = await _db.VisitorMessages
                    .Where(x => x.ConversationId == conversationId)
                    .ToListAsync();
                
                foreach (var msg in messagesToUpdate.Where(m => !m.IsRead))
                {
                    msg.IsRead = true;
                }
                if (messagesToUpdate.Any())
                {
                    await _db.SaveChangesAsync();
                }
            }
            
            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CorporateAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!user.IsCorporateMember)
            {
                return RedirectToAction(nameof(CorporateMembership));
            }

            return View("CorporateDashboard", await BuildDashboardViewModel(user));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([Bind(Prefix = "ProfileForm")] ProfileUpdateViewModel form)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (form.AvatarFile is { Length: > 0 } && !IsValidAvatarFile(form.AvatarFile))
            {
                ModelState.AddModelError(nameof(form.AvatarFile), T(
                    "Profil fotoğrafı JPG, PNG veya WEBP olmalı ve 5 MB'ı geçmemelidir.",
                    "Profile photo must be JPG, PNG or WEBP and must not exceed 5 MB.",
                    "Фото профиля должно быть JPG, PNG или WEBP и не должно превышать 5 МБ.",
                    "يجب أن تكون صورة الملف الشخصي بصيغة JPG أو PNG أو WEBP وألا تتجاوز 5 ميغابايت.",
                    "عکس پروفایل باید JPG، PNG یا WEBP باشد و نباید از ۵ مگابایت بیشتر شود."));
            }

            if (!ModelState.IsValid)
            {
                ViewData["ActiveTab"] = "profile";
                var vm = await BuildDashboardViewModel(user);
                vm.ProfileForm = form;
                return View("Dashboard", vm);
            }

            var firstName = (form.FirstName ?? string.Empty).Trim();
            var lastName = (form.LastName ?? string.Empty).Trim();
            user.FullName = string.Join(' ', new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            user.PhoneNumber = string.IsNullOrWhiteSpace(form.PhoneNumber) ? null : form.PhoneNumber.Trim();
            user.AddressLine = string.IsNullOrWhiteSpace(form.AddressLine) ? null : form.AddressLine.Trim();
            user.City = string.IsNullOrWhiteSpace(form.City) ? null : form.City.Trim();

            if (form.AvatarFile is { Length: > 0 })
            {
                var uploadsFolder = _uploadStorage.EnsureDirectory("avatars");

                var savedAvatarPath = await SaveOptimizedImageAsync(form.AvatarFile, uploadsFolder, _uploadStorage.GetPublicDirectory("avatars"));
                if (string.IsNullOrWhiteSpace(savedAvatarPath))
                {
                    ModelState.AddModelError(nameof(form.AvatarFile), T(
                        "Profil fotoğrafı kaydedilemedi. Lütfen farklı bir dosya deneyin.",
                        "Profile photo could not be saved. Please try another file.",
                        "Не удалось сохранить фото профиля. Пожалуйста, выберите другой файл.",
                        "تعذّر حفظ صورة الملف الشخصي. يرجى تجربة ملف آخر.",
                        "ذخیره عکس پروفایل انجام نشد. لطفاً فایل دیگری را امتحان کنید."));

                    ViewData["ActiveTab"] = "profile";
                    var vm = await BuildDashboardViewModel(user);
                    vm.ProfileForm = form;
                    return View("Dashboard", vm);
                }

                user.AvatarUrl = savedAvatarPath;
            }

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

            TempData["DashboardSuccess"] = T("Profil bilgileriniz güncellendi.", "Your profile information has been updated.", "Информация профиля обновлена.", "تم تحديث معلومات ملفك الشخصي.", "اطلاعات پروفایل شما به‌روزرسانی شد.");
            return RedirectToAction(nameof(Dashboard), new { tab = "profile" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "PasswordForm")] ChangePasswordViewModel form)
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
            TempData["DashboardSuccess"] = T("Şifreniz başarıyla güncellendi.", "Your password has been updated successfully.", "Ваш пароль успешно обновлен.", "تم تحديث كلمة المرور بنجاح.", "رمز عبور شما با موفقیت به‌روزرسانی شد.");
            return RedirectToAction(nameof(Dashboard), new { tab = "security" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotificationSettings([Bind(Prefix = "NotificationForm")] NotificationSettingsViewModel form)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            user.EmailNotifications = form.EmailNotifications;

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

            TempData["DashboardSuccess"] = T("Bildirim tercihleriniz güncellendi.", "Your notification preferences have been updated.", "Ваши настройки уведомлений обновлены.", "تم تحديث تفضيلات الإشعارات الخاصة بك.", "تنظیمات اعلان شما به‌روزرسانی شد.");
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

        [Authorize]
        [HttpGet]
        [SEN_T_PAZAR.Filters.ValidateListingOwner]
        public async Task<IActionResult> EditListing(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            ViewData["Cities"] = _catalog.Cities
                .Where(x => !string.Equals(x, "all", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var listing = await _db.Listings
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (listing == null || (listing.UserId != user.Id && listing.Phone != user.PhoneNumber && listing.FullName != user.FullName))
            {
                return NotFound();
            }

var model = new ListingEditViewModel
              {
                  Id = listing.Id,
                  Title = listing.Title,
                  Description = listing.Description,
                  Category = listing.Category,
                  Type = listing.Type,
                  City = listing.City,
                  District = listing.District,
                  Neighborhood = listing.Neighborhood,
                  HouseNumber = listing.HouseNumber,
                  ApartmentNumber = listing.ApartmentNumber,
                  Address = listing.Address,
                  PriceAmount = listing.PriceAmount,
                  PriceCurrency = listing.PriceCurrency,
                  FullName = listing.FullName,
                  Phone = listing.Phone,
                  ExistingImages = listing.Images
                      .OrderBy(x => x.Id)
                      .Select((x, index) => new ListingEditImageItemViewModel
                      {
                          Id = x.Id,
                          FilePath = x.FilePath,
                          IsCover = index == listing.CoverImageIndex
                      })
                      .ToList(),
                  CoverImageId = listing.Images
                      .OrderBy(x => x.Id)
                      .Select((x, index) => new { x.Id, Index = index })
.FirstOrDefault(x => x.Index == listing.CoverImageIndex)?.Id
              };
            
            ViewData["Districts"] = _db.Listings
                .AsNoTracking()
                .Where(x => !string.IsNullOrWhiteSpace(x.District))
                .Select(x => x.District!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SEN_T_PAZAR.Filters.ValidateListingOwner]
        public async Task<IActionResult> EditListing(ListingEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            ViewData["Cities"] = _catalog.Cities
                .Where(x => !string.Equals(x, "all", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!ModelState.IsValid)
            {
                var listingForValidation = await _db.Listings
                    .Include(x => x.Images)
                    .FirstOrDefaultAsync(x => x.Id == model.Id);
                if (listingForValidation != null)
                {
                    model.ExistingImages = listingForValidation.Images
                        .OrderBy(x => x.Id)
                        .Select((x, index) => new ListingEditImageItemViewModel
                        {
                            Id = x.Id,
                            FilePath = x.FilePath,
                            IsCover = index == listingForValidation.CoverImageIndex
                        })
                        .ToList();
                }
                return View(model);
            }

            if (decimal.Truncate(model.PriceAmount) != model.PriceAmount)
            {
                ModelState.AddModelError(nameof(model.PriceAmount), T(
                    "Fiyat tam sayi olmalidir.",
                    "Price must be a whole number.",
                    "Цена должна быть целым числом.",
                    "يجب أن يكون السعر رقمًا صحيحًا.",
                    "قیمت باید عدد صحیح باشد."));
                var listingForValidation = await _db.Listings
                    .Include(x => x.Images)
                    .FirstOrDefaultAsync(x => x.Id == model.Id);
                if (listingForValidation != null)
                {
                    model.ExistingImages = listingForValidation.Images
                        .OrderBy(x => x.Id)
                        .Select((x, index) => new ListingEditImageItemViewModel
                        {
                            Id = x.Id,
                            FilePath = x.FilePath,
                            IsCover = index == listingForValidation.CoverImageIndex
                        })
                        .ToList();
                }
                return View(model);
            }

            var listing = await _db.Listings
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == model.Id);
            if (listing == null || (listing.UserId != user.Id && listing.Phone != user.PhoneNumber && listing.FullName != user.FullName))
            {
                return NotFound();
            }

            listing.Title = model.Title.Trim();
            listing.Description = model.Description.Trim();
            listing.Category = model.Category.Trim();
            listing.Type = model.Type.Trim();
            listing.City = model.City.Trim();
            listing.District = model.District?.Trim() ?? string.Empty;
            listing.Neighborhood = string.IsNullOrWhiteSpace(model.Neighborhood) ? null : model.Neighborhood.Trim();
            listing.HouseNumber = string.IsNullOrWhiteSpace(model.HouseNumber) ? null : model.HouseNumber.Trim();
            listing.ApartmentNumber = string.IsNullOrWhiteSpace(model.ApartmentNumber) ? null : model.ApartmentNumber.Trim();
            listing.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            listing.PriceAmount = model.PriceAmount;
            // Para birimi boş veya null ise TL olarak ayarla
            listing.PriceCurrency = string.IsNullOrWhiteSpace(model.PriceCurrency) ? "TL" : model.PriceCurrency.Trim();
            
            listing.FullName = model.FullName.Trim();
            listing.Phone = model.Phone.Trim();
            
            if (user.FullName != model.FullName.Trim())
            {
                user.FullName = model.FullName.Trim();
            }
            if (user.PhoneNumber != model.Phone.Trim())
            {
                user.PhoneNumber = model.Phone.Trim();
            }

            var deleteIds = (model.DeleteImageIds ?? new List<int>())
                .Distinct()
                .ToHashSet();

            if (deleteIds.Count > 0)
            {
                var imagesToDelete = listing.Images
                    .Where(x => deleteIds.Contains(x.Id))
                    .ToList();
                foreach (var image in imagesToDelete)
                {
                    if (!string.IsNullOrWhiteSpace(image.FilePath))
                    {
                        var physicalPath = _uploadStorage.TryGetPhysicalPath(image.FilePath);
                        if (!string.IsNullOrWhiteSpace(physicalPath) && System.IO.File.Exists(physicalPath))
                        {
                            try
                            {
                                System.IO.File.Delete(physicalPath);
                            }
                            catch
                            {
                                // Ignore file deletion errors and continue DB update.
                            }
                        }
                    }
                }

                _db.ListingImages.RemoveRange(imagesToDelete);
            }

            if (model.NewImageFiles != null && model.NewImageFiles.Count > 0)
            {
                var uploadsFolder = _uploadStorage.EnsureDirectory();

                foreach (var file in model.NewImageFiles)
                {
                    if (file.Length <= 0)
                    {
                        continue;
                    }

                    var savedPath = await SaveOptimizedImageAsync(file, uploadsFolder, _uploadStorage.GetPublicDirectory());
                    if (!string.IsNullOrWhiteSpace(savedPath))
                    {
                        listing.Images.Add(new ListingImage { FilePath = savedPath, UserId = user.Id });
                    }
                }
            }

            var orderedImages = listing.Images.OrderBy(x => x.Id).ToList();
            if (model.CoverImageId.HasValue)
            {
                var coverIndex = orderedImages.FindIndex(x => x.Id == model.CoverImageId.Value);
                listing.CoverImageIndex = coverIndex >= 0 ? coverIndex : 0;
            }
            else if (orderedImages.Count == 0)
            {
                listing.CoverImageIndex = 0;
            }
            else if (listing.CoverImageIndex >= orderedImages.Count)
            {
                listing.CoverImageIndex = 0;
            }

await _db.SaveChangesAsync();
             await _userManager.UpdateAsync(user);
             TempData["DashboardSuccess"] = T("İlan başarıyla güncellendi.", "Listing updated successfully.", "Объявление успешно обновлено.", "تم تحديث الإعلان بنجاح.", "آگهی با موفقیت به‌روزرسانی شد.");
             return RedirectToAction(nameof(Dashboard), new { tab = "listings" });
        }

        private static async Task<string?> SaveOptimizedImageAsync(IFormFile file, string uploadsFolder, string webRelativeDirectory = "/uploads")
        {
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return null;
            }

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            var outputName = $"{Guid.NewGuid():N}.jpg";
            var outputPath = Path.Combine(uploadsFolder, outputName);

            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input);

            if (image.Width > MaxUploadImageDimension || image.Height > MaxUploadImageDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(MaxUploadImageDimension, MaxUploadImageDimension),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                }));
            }

            var encoder = new JpegEncoder
            {
                Quality = UploadJpegQuality
            };

            await image.SaveAsJpegAsync(outputPath, encoder);
            var normalizedDirectory = string.IsNullOrWhiteSpace(webRelativeDirectory)
                ? "/uploads"
                : webRelativeDirectory.TrimEnd('/');
            return normalizedDirectory + "/" + outputName;
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteListing(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == id);
            if (listing == null || (listing.UserId != user.Id && listing.Phone != user.PhoneNumber && listing.FullName != user.FullName))
            {
                return NotFound();
            }

            var images = await _db.ListingImages.Where(x => x.ListingId == id).ToListAsync();
            _db.ListingImages.RemoveRange(images);
            _db.Listings.Remove(listing);
            await _db.SaveChangesAsync();

            TempData["DashboardSuccess"] = T("İlan silindi.", "Listing deleted.", "Объявление удалено.", "تم حذف الإعلان.", "آگهی حذف شد.");
            return RedirectToAction(nameof(Dashboard), new { tab = "listings" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateListingState(int id, string state)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == id);
            if (listing == null || (listing.UserId != user.Id && listing.Phone != user.PhoneNumber && listing.FullName != user.FullName))
            {
                return NotFound();
            }

            switch ((state ?? string.Empty).ToLowerInvariant())
            {
                case "sold":
                    listing.DealStatus = "sold";
                    listing.IsClosed = true;
                    break;
                case "rented":
                    listing.DealStatus = "rented";
                    listing.IsClosed = true;
                    break;
                case "closed":
                    listing.DealStatus = "closed";
                    listing.IsClosed = true;
                    break;
                default:
                    listing.DealStatus = "open";
                    listing.IsClosed = false;
                    break;
            }

            await _db.SaveChangesAsync();
            TempData["DashboardSuccess"] = T("İlan durumu güncellendi.", "Listing status updated.", "Статус объявления обновлен.", "تم تحديث حالة الإعلان.", "وضعیت آگهی به‌روزرسانی شد.");
            return RedirectToAction(nameof(Dashboard), new { tab = "listings" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkMessageAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var message = await _db.VisitorMessages.FirstOrDefaultAsync(x => x.Id == id);
            if (message == null)
            {
                return RedirectToAction(nameof(Dashboard), new { tab = "messages" });
            }

            var canAccess = (!string.IsNullOrWhiteSpace(message.RecipientUserId) && message.RecipientUserId == user.Id)
                || (!string.IsNullOrWhiteSpace(message.RecipientPhone) && message.RecipientPhone == user.PhoneNumber)
                || (!string.IsNullOrWhiteSpace(message.RecipientEmail) && message.RecipientEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(message.SenderUserId) && message.SenderUserId == user.Id);
            if (!canAccess)
            {
                return Forbid();
            }

            if (!message.IsRead)
            {
                message.IsRead = true;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Dashboard), new { tab = "messages" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveMessage(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var message = await _db.VisitorMessages.FirstOrDefaultAsync(x => x.Id == id);
            if (message == null) return RedirectToAction(nameof(Dashboard), new { tab = "messages" });

            var canAccess = (!string.IsNullOrWhiteSpace(message.RecipientUserId) && message.RecipientUserId == user.Id)
                || (!string.IsNullOrWhiteSpace(message.RecipientPhone) && message.RecipientPhone == user.PhoneNumber)
                || (!string.IsNullOrWhiteSpace(message.RecipientEmail) && message.RecipientEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(message.SenderUserId) && message.SenderUserId == user.Id);
            if (!canAccess) return Forbid();

            message.IsArchived = true;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard), new { tab = "messages" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var message = await _db.VisitorMessages.FirstOrDefaultAsync(x => x.Id == id);
            if (message == null) return RedirectToAction(nameof(Dashboard), new { tab = "messages" });

            var canAccess = (!string.IsNullOrWhiteSpace(message.RecipientUserId) && message.RecipientUserId == user.Id)
                || (!string.IsNullOrWhiteSpace(message.RecipientPhone) && message.RecipientPhone == user.PhoneNumber)
                || (!string.IsNullOrWhiteSpace(message.RecipientEmail) && message.RecipientEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(message.SenderUserId) && message.SenderUserId == user.Id);
            if (!canAccess) return Forbid();

            _db.VisitorMessages.Remove(message);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard), new { tab = "messages" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyMessage(int id, string replyText)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            replyText = (replyText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(replyText))
            {
                TempData["DashboardSuccess"] = T(
                    "Cevap metni boş olamaz.",
                    "Reply text cannot be empty.",
                    "Текст ответа не может быть пустым.",
                    "لا يمكن أن يكون نص الرد فارغًا.",
                    "متن پاسخ نمی‌تواند خالی باشد.");
                return RedirectToAction(nameof(Dashboard), new { tab = "messages" });
            }

            var message = await _db.VisitorMessages.FirstOrDefaultAsync(x => x.Id == id);
            if (message == null)
            {
                return RedirectToAction(nameof(Dashboard), new { tab = "messages" });
            }

            var canAccess = (!string.IsNullOrWhiteSpace(message.RecipientUserId) && message.RecipientUserId == user.Id)
                || (!string.IsNullOrWhiteSpace(message.RecipientPhone) && message.RecipientPhone == user.PhoneNumber)
                || (!string.IsNullOrWhiteSpace(message.RecipientEmail) && message.RecipientEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(message.SenderUserId) && message.SenderUserId == user.Id);
            if (!canAccess)
            {
                return Forbid();
            }

            var conversationId = string.IsNullOrWhiteSpace(message.ConversationId) ? $"legacy-{message.Id}" : message.ConversationId;
            var reply = new VisitorMessage
            {
                ListingId = message.ListingId,
                ConversationId = conversationId,
                RecipientUserId = message.SenderUserId,
                RecipientPhone = message.SenderPhone,
                RecipientEmail = message.SenderEmail,
                SenderUserId = user.Id,
                SenderName = user.FullName ?? user.UserName ?? "Kullanıcı",
                SenderEmail = user.Email ?? string.Empty,
                SenderPhone = user.PhoneNumber,
                SenderRole = "owner",
                Subject = message.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? message.Subject : $"Re: {message.Subject}",
                Message = replyText,
                CreatedAtUtc = DateTime.UtcNow,
                IsRead = false
            };

            _db.VisitorMessages.Add(reply);
            message.IsRead = true;
            await _db.SaveChangesAsync();

            // Real-time broadcast via SignalR
            try
            {
                await _chatHubContext.Clients
                    .Group($"conversation-{conversationId}")
                    .SendAsync("ReceiveMessage", new
                    {
                        senderName = reply.SenderName,
                        message = reply.Message,
                        timestamp = reply.CreatedAtUtc
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR broadcast failed for conversation {ConversationId}", conversationId);
            }

            // AJAX isteği için JSON döndür (sayfa yenilenmesin)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new 
                { 
                    success = true, 
                    message = "Cevabınız gönderildi." 
                });
            }

            TempData["DashboardSuccess"] = T(
                "Cevabınız gönderildi.",
                "Your reply has been sent.",
                "Ваш ответ отправлен.",
                "تم إرسال ردك.",
                "پاسخ شما ارسال شد.");
            return RedirectToAction(nameof(Dashboard), new { tab = "messages" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllMessagesAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var unreadThreads = await _db.VisitorMessages
                .Where(m => m.RecipientEmail == user.Email && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unreadThreads)
            {
                msg.IsRead = true;
            }
            await _db.SaveChangesAsync();

            TempData["DashboardSuccess"] = T(
                "Tüm mesajlar okundu olarak işaretlendi.",
                "All messages marked as read.",
                "Все сообщения отмечены как прочитанные.",
                "تم تعليم جميع الرسائل كمقروءة.",
                "همه پیام‌ها به‌عنوان خوانده‌شده علامت‌گذاری شدند.");
            return RedirectToAction(nameof(Dashboard), new { tab = "messages" });
        }

        private async Task<string> GenerateUniqueUserNameAsync(string email)
        {
            var baseName = email.Split('@')[0].Trim();
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "user";
            }

            baseName = string.Concat(baseName.Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '.'));
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "user";
            }

            var candidate = baseName;
            var index = 1;
            while (await _userManager.FindByNameAsync(candidate) != null)
            {
                candidate = $"{baseName}{index}";
                index++;
            }

            return candidate;
        }

        private async Task<AccountDashboardViewModel> BuildDashboardViewModel(ApplicationUser user)
        {
            if (!string.IsNullOrWhiteSpace(user.AvatarUrl) &&
                user.AvatarUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) &&
                !user.AvatarUrl.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(user.AvatarUrl);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    var candidatePhysical = Path.Combine(_uploadStorage.EnsureDirectory("avatars"), fileName);
                    if (System.IO.File.Exists(candidatePhysical))
                    {
                        user.AvatarUrl = _uploadStorage.GetPublicDirectory("avatars") + "/" + fileName;
                        await _userManager.UpdateAsync(user);
                    }
                }
            }

            var myListings = await _db.Listings
                .Where(x => x.UserId == user.Id || x.Phone == user.PhoneNumber || x.FullName == user.FullName)
                .OrderByDescending(x => x.CreatedAt)
                .Take(50)
                .Select(x => new MyListingCardViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Category = x.Category,
                    Type = x.Type,
                    City = x.City,
                    PriceAmount = x.PriceAmount,
                    PriceCurrency = x.PriceCurrency,
                    IsApproved = x.IsApproved,
                    ViewCount = x.ViewCount,
                    FavoritesCount = _db.UserFavorites.Count(f => f.ListingId == x.Id),
                    IsClosed = x.IsClosed,
                    DealStatus = x.DealStatus,
                    CreatedAt = x.CreatedAt,
                    IsFeatured = x.IsFeatured,
                    IsVitrin = x.IsVitrin,
                    FeaturedExpiryDate = x.FeaturedExpiryDate,
                    VitrinExpiryDate = x.VitrinExpiryDate,
                    CoverImageUrl = _db.ListingImages
                        .Where(i => i.ListingId == x.Id)
                        .OrderBy(i => i.Id)
                        .Select(i => i.FilePath)
                        .FirstOrDefault(),
                    RawFilePath = _db.ListingImages
                        .Where(i => i.ListingId == x.Id)
                        .OrderBy(i => i.Id)
                        .Select(i => i.FilePath)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // CoverImageUrl'leri absolute URL'ye çevir
            foreach (var listing in myListings)
            {
                var fp = listing.RawFilePath;
                if (!string.IsNullOrWhiteSpace(fp))
                {
                    if (!Uri.TryCreate(fp, UriKind.Absolute, out _))
                    {
                        var normalized = fp.StartsWith("/") ? fp : "/" + fp;
                        listing.CoverImageUrl = $"{Request.Scheme}://{Request.Host}{normalized}";
                    }
                    else
                    {
                        listing.CoverImageUrl = fp;
                    }
                }
                listing.RawFilePath = null;
            }

            var stats = new DashboardStatsViewModel
            {
                TotalListings = myListings.Count,
                ApprovedListings = myListings.Count(x => x.IsApproved),
                PendingListings = myListings.Count(x => !x.IsApproved),
                TotalViews = myListings.Sum(x => Math.Max(0, x.ViewCount)),
                FeaturedListings = myListings.Count(x => x.IsFeatured && (x.FeaturedExpiryDate == null || x.FeaturedExpiryDate > DateTime.UtcNow)),
                VitrinListings = myListings.Count(x => x.IsVitrin && (x.VitrinExpiryDate == null || x.VitrinExpiryDate > DateTime.UtcNow))
            };

            // Kullanıcı paketlerini al
            var userPackages = await _db.UserPackages
                .Where(p => p.UserId == user.Id && p.IsActive && (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow))
                .Include(p => p.Package)
                .ToListAsync();

            var activePackages = userPackages.Select(p => new UserPackageInfo
            {
                PackageName = p.Package?.Name ?? "Bilinmiyor",
                PackageType = p.Package?.PackageType ?? "",
                RemainingUses = p.RemainingCount,
                ExpiryDate = p.ExpiryDate
            }).ToList();

            var visitorMessages = await _db.VisitorMessages
                .AsNoTracking()
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(x.RecipientUserId) && x.RecipientUserId == user.Id) ||
                    (string.IsNullOrWhiteSpace(x.RecipientUserId) && !string.IsNullOrWhiteSpace(user.PhoneNumber) && x.RecipientPhone == user.PhoneNumber) ||
                    (!string.IsNullOrWhiteSpace(x.RecipientEmail) && x.RecipientEmail == user.Email) ||
                    (!string.IsNullOrWhiteSpace(x.SenderUserId) && x.SenderUserId == user.Id))
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new
                {
                    x.Id,
                    x.ListingId,
                    x.ConversationId,
                    ListingTitle = _db.Listings.Where(l => l.Id == x.ListingId).Select(l => l.Title).FirstOrDefault(),
                    x.SenderName,
                    x.SenderEmail,
                    x.SenderPhone,
                    x.Subject,
                    x.Message,
                    x.CreatedAtUtc,
                    x.IsRead,
                    x.SenderRole,
                    x.RecipientEmail,
                    x.RecipientUserId,
                    x.RecipientPhone,
                    x.SenderUserId
                })
                .ToListAsync();

            var visitorMessageThreads = visitorMessages
                .GroupBy(x => string.IsNullOrWhiteSpace(x.ConversationId) ? $"legacy-{x.Id}" : x.ConversationId)
                .Select(group =>
                {
                    var ordered = group.OrderBy(x => x.CreatedAtUtc).ToList();
                    var root = ordered.First();
                    return new VisitorMessageThreadViewModel
                    {
                        ConversationId = group.Key,
                        RootMessageId = root.Id,
                        Id = root.Id,
                        ListingId = root.ListingId,
                        ListingTitle = root.ListingId == 0 ? "Sistem mesajı" : root.ListingTitle ?? $"Ilan #{root.ListingId}",
                        RecipientEmail = root.RecipientEmail ?? string.Empty,
                        SenderName = root.SenderName,
                        SenderEmail = root.SenderEmail,
                        SenderPhone = root.SenderPhone,
                        Subject = root.Subject,
                        IsRead = ordered.All(x => x.IsRead),
                        CreatedAtUtc = ordered.Last().CreatedAtUtc,
                        SenderRole = ordered.Last().SenderRole,
                        Messages = ordered.Select(x => new VisitorMessageEntryViewModel
                        {
                            Id = x.Id,
                            SenderName = x.SenderName,
                            SenderRole = x.SenderRole,
                            Message = x.Message,
                            CreatedAtUtc = x.CreatedAtUtc,
                            IsRead = x.IsRead
                        }).ToList()
                    };
                })
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToList();

            var unreadMessageCount = visitorMessages.Count(x => !x.IsRead);

            // Gelişmiş analitik hesaplamaları
            var totalFavorites = myListings.Sum(x => x.FavoritesCount);
            var totalMessagesReceived = visitorMessages.Count;

            stats.TotalFavorites = totalFavorites;
            stats.TotalMessages = totalMessagesReceived;
            stats.AverageViewsPerListing = myListings.Any() 
                ? Math.Round(myListings.Average(x => Math.Max(0, x.ViewCount)), 1) 
                : 0;

            return new AccountDashboardViewModel
            {
                User = user,
                ProfileForm = new ProfileUpdateViewModel
                {
                    FirstName = SplitName(user.FullName).FirstName,
                    LastName = SplitName(user.FullName).LastName,
                    PhoneNumber = user.PhoneNumber,
                    AddressLine = user.AddressLine,
                    City = user.City
                },
                PasswordForm = new ChangePasswordViewModel(),
                NotificationForm = new NotificationSettingsViewModel
                {
                    EmailNotifications = user.EmailNotifications
                },
                EmailVerificationForm = new EmailVerificationViewModel
                {
                    NewEmail = user.Email ?? string.Empty
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
                        Status = GetCorporateMembershipStatus(user)
                    }
                ],
                VisitorMessages = visitorMessageThreads,
                Stats = stats,
                ActivePackages = activePackages,
                NotificationCount = unreadMessageCount
            };
        }

        private static (string FirstName, string LastName) SplitName(string? fullName)
        {
            var trimmed = (fullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return (string.Empty, string.Empty);
            }

            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return (parts[0], string.Empty);
            }

            return (parts[0], string.Join(' ', parts.Skip(1)));
        }

        private static bool IsValidAvatarFile(IFormFile file)
        {
            if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
            {
                return false;
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png" or ".webp";
        }


        #region Kurumsal Üyelik

        [HttpGet]
        public IActionResult CorporateMembership()
        {
            PopulateCorporatePlans();
            return View(new CorporateMembershipViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyCorporate(CorporateMembershipViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.CompanyTaxOffice))
            {
                model.CompanyTaxOffice = model.CompanyTaxOffice.Trim();
            }

            if (!string.IsNullOrWhiteSpace(model.CompanyMersisNumber))
            {
                model.CompanyMersisNumber = model.CompanyMersisNumber.Trim();
            }

            if (model.CompanyLogoFile is { Length: > 0 } && !IsValidCompanyLogo(model.CompanyLogoFile))
            {
                ModelState.AddModelError(nameof(model.CompanyLogoFile), "Logo dosyası JPG, PNG veya WEBP olmalıdır ve 5 MB boyutunu aşmamalıdır.");
            }

            if (!ModelState.IsValid)
            {
                PopulateCorporatePlans();
                return View("CorporateMembership", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (model.CompanyLogoFile is { Length: > 0 })
            {
                model.CompanyLogoUrl = await SaveCompanyLogoAsync(model.CompanyLogoFile);
            }

            // Kurumsal üyelik bilgilerini güncelle
            user.IsCorporateMember = true;
            user.CompanyName = model.CompanyName;
            user.CompanyTaxNumber = model.CompanyTaxNumber;
            user.CompanyTaxOffice = model.CompanyTaxOffice;
            user.CompanyMersisNumber = model.CompanyMersisNumber;
            user.CompanyPhone = model.CompanyPhone;
            user.CompanyAddress = model.CompanyAddress;
            user.CompanyWebSite = model.CompanyWebSite;
            user.CompanyLogoUrl = model.CompanyLogoUrl;
            user.FullName = model.ContactPersonName;
            user.PhoneNumber = model.ContactPersonPhone;
            user.SubscriptionPlan = model.SelectedPlan;
            user.SubscriptionStartDate = DateTime.UtcNow;
            user.SubscriptionEndDate = DateTime.UtcNow.AddYears(1);
            user.IsSubscriptionActive = false; // Onay bekliyor
            user.IsCorporateApproved = false;
            user.CorporateApprovalDate = null;
            user.CorporateNote = "pending";

            // Plan limitlerini ayarla
            var planConfig = GetPlanConfig(model.SelectedPlan);
            user.TotalListingsAllowed = planConfig.ListingsPerMonth;
            user.ListingsRemainingThisMonth = planConfig.ListingsPerMonth == -1 ? 999999 : planConfig.ListingsPerMonth;
            user.FeaturedListingsIncluded = planConfig.FeaturedListings == -1 ? 999999 : planConfig.FeaturedListings;
            user.VitrinListingsIncluded = planConfig.VitrinListings == -1 ? 999999 : planConfig.VitrinListings;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                PopulateCorporatePlans();
                return View("CorporateMembership", model);
            }

            TempData["CorporateSuccess"] = T(
                "Kurumsal üyelik başvurunuz alınmıştır. İnceleme süreci 1-3 iş günü sürmektedir.",
                "Your corporate membership application has been received. Review usually takes 1-3 business days.",
                "Ваша заявка на корпоративное членство принята. Проверка обычно занимает 1-3 рабочих дня.",
                "تم استلام طلب العضوية المؤسسية. تستغرق المراجعة عادة من يوم إلى ثلاثة أيام عمل.",
                "درخواست عضویت شرکتی شما دریافت شد. بررسی معمولاً ۱ تا ۳ روز کاری زمان می‌برد.");
            return RedirectToAction(nameof(Dashboard), new { tab = "corporate" });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UpdateCorporate()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsCorporateMember)
            {
                return RedirectToAction(nameof(CorporateMembership));
            }

            var model = new CorporateMembershipViewModel
            {
                CompanyName = user.CompanyName ?? "",
                CompanyTaxNumber = user.CompanyTaxNumber ?? "",
                CompanyTaxOffice = user.CompanyTaxOffice ?? "",
                CompanyMersisNumber = user.CompanyMersisNumber,
                CompanyPhone = user.CompanyPhone ?? "",
                CompanyAddress = user.CompanyAddress ?? "",
                CompanyWebSite = user.CompanyWebSite,
                CompanyLogoUrl = user.CompanyLogoUrl,
                ContactPersonName = user.FullName,
                ContactPersonPhone = user.PhoneNumber ?? string.Empty,
                ContactPersonEmail = user.Email,
                SelectedPlan = user.SubscriptionPlan ?? "free"
            };

            PopulateCorporatePlans();

            return View("CorporateMembership", model);
        }

        private void PopulateCorporatePlans()
        {
            ViewBag.Plans = new List<CorporatePlanViewModel>
            {
                new CorporatePlanViewModel { PlanId = "free", PlanName = GetCorporatePlanLabel("free"), MonthlyPrice = 0, YearlyPrice = 0, ListingsPerMonth = -1, FeaturedListings = 0, VitrinListings = 0, HasCustomLogo = true, HasVerifiedBadge = true },
                new CorporatePlanViewModel { PlanId = "basic", PlanName = GetCorporatePlanLabel("basic"), MonthlyPrice = 299, YearlyPrice = 2870, ListingsPerMonth = -1, FeaturedListings = 5, VitrinListings = 2, HasCustomLogo = true, HasVerifiedBadge = true },
                new CorporatePlanViewModel { PlanId = "pro", PlanName = GetCorporatePlanLabel("pro"), MonthlyPrice = 599, YearlyPrice = 5750, ListingsPerMonth = -1, FeaturedListings = 10, VitrinListings = 20, HasAnalytics = true, HasPrioritySupport = true, HasCustomLogo = true, HasVerifiedBadge = true },
                new CorporatePlanViewModel { PlanId = "enterprise", PlanName = GetCorporatePlanLabel("enterprise"), MonthlyPrice = 1299, YearlyPrice = 12470, ListingsPerMonth = -1, FeaturedListings = 20, VitrinListings = 40, HasAnalytics = true, HasPrioritySupport = true, HasCustomLogo = true, HasVerifiedBadge = true }
            };
        }

        private static bool IsValidCompanyLogo(IFormFile file)
        {
            if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
            {
                return false;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName);
            return !string.IsNullOrWhiteSpace(extension) && allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<string> SaveCompanyLogoAsync(IFormFile file)
        {
            var logosFolder = _uploadStorage.EnsureDirectory("company-logos");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(logosFolder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"{_uploadStorage.GetPublicDirectory("company-logos")}/{fileName}";
        }

        private (int ListingsPerMonth, int FeaturedListings, int VitrinListings) GetPlanConfig(string planId)
        {
            return planId switch
            {
                "free" => (-1, 0, 0),
                "basic" => (-1, 5, 2),
                "pro" => (-1, 10, 20),
                "enterprise" => (-1, 20, 40),
                _ => (-1, 0, 0)
            };
        }

        #endregion
    }
}

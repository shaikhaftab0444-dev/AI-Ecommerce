using BrandsStore.Data;
using BrandsStore.Helpers;
using BrandsStore.Models;
using BrandsStore.Services;
using BrandsStore.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BrandsStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ============= LOGIN & REGISTER =============

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to appropriate page
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");
                else
                    return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                // Verify password using the helper method
                if (user != null && user.IsActive && PasswordResetHelper.VerifyPassword(model.Password, user.Password))
                {
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName)
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(2)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    TempData["Success"] = $"Welcome back, {user.FullName}!";

                    if (user.Role == "Admin")
                        return RedirectToAction("Index", "Admin");
                    else
                        return RedirectToAction("Index", "Home");
                }
                else if (user != null && !user.IsActive)
                {
                    ModelState.AddModelError("", "Your account has been deactivated. Please contact support.");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                }
            }

            return View(model);
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            // If already logged in, redirect
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                // Create new user with HASHED password
                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = PasswordResetHelper.HashPassword(model.Password), // Hash password
                    Role = "User",
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Registration successful! Please login with your credentials.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Profile
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // ✅ FIX: Map User model → UserProfileViewModel (was passing raw User before)
            var viewModel = new UserProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                MemberSince = user.CreatedDate,
              //  AvatarUrl = user.AvatarUrl   // remove this line if User model has no AvatarUrl
            };

            // Fetch order stats for the stats cards
            ViewBag.TotalOrders = await _context.Orders.CountAsync(o => o.UserId == userId);
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.UserId == userId && o.Status == "Pending");

            return View(viewModel);
        }

        // GET: Account/ChangePassword
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Account/ChangePassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                    var user = await _context.Users.FindAsync(userId);

                    if (user == null)
                    {
                        return NotFound();
                    }

                    Console.WriteLine($"=== CHANGE PASSWORD REQUEST ===");
                    Console.WriteLine($"User: {user.Email}");

                    // Verify current password using the helper method
                    if (!PasswordResetHelper.VerifyPassword(model.CurrentPassword, user.Password))
                    {
                        Console.WriteLine("❌ Current password is incorrect");
                        ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                        return View(model);
                    }

                    // Validate new password strength
                    var (isValid, errorMessage) = PasswordResetHelper.ValidatePasswordStrength(model.NewPassword);
                    if (!isValid)
                    {
                        Console.WriteLine($"❌ Password validation failed: {errorMessage}");
                        ModelState.AddModelError("NewPassword", errorMessage);
                        return View(model);
                    }

                    // Check if new password is same as current password
                    if (PasswordResetHelper.VerifyPassword(model.NewPassword, user.Password))
                    {
                        Console.WriteLine("❌ New password is same as current password");
                        ModelState.AddModelError("NewPassword", "New password cannot be the same as current password.");
                        return View(model);
                    }

                    // Update password with new hashed password
                    user.Password = PasswordResetHelper.HashPassword(model.NewPassword);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("✅ Password changed successfully!");

                    TempData["Success"] = "Password changed successfully!";
                    return RedirectToAction("Profile");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in ChangePassword: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    ModelState.AddModelError("", "An error occurred while changing your password. Please try again.");
                    return View(model);
                }
            }

            return View(model);
        }

        // ============= PASSWORD RESET FUNCTIONALITY =============

        // GET: Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Message = "Please enter your email address.";
                ViewBag.MessageType = "danger";
                return View();
            }

            try
            {
                Console.WriteLine($"=== FORGOT PASSWORD REQUEST ===");
                Console.WriteLine($"Email: {email}");
                Console.WriteLine($"Time: {DateTime.Now}");

                // Search for user in Users table
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    Console.WriteLine($"✅ User found: {user.Email}");

                    // Generate OTP
                    var otp = PasswordResetHelper.GenerateOtp();
                    var otpHash = PasswordResetHelper.HashOtp(otp);

                    Console.WriteLine($"🔑 Generated OTP: {otp}");
                    Console.WriteLine($"🔒 OTP Hash: {otpHash}");

                    // Set OTP data
                    user.PasswordResetOtpHash = otpHash;
                    user.OtpExpiryTime = DateTime.UtcNow.AddMinutes(10);
                    user.OtpAttemptCount = 0;
                    user.OtpLockoutEnd = null; // Clear any previous lockout

                    await _context.SaveChangesAsync();
                    Console.WriteLine("💾 OTP saved to database");

                    // Send email - CRITICAL: MUST AWAIT this
                    try
                    {
                        Console.WriteLine("📧 Attempting to send email...");
                        await _emailService.SendOtpEmailAsync(user.Email, otp);
                        Console.WriteLine("✅ EMAIL SENT SUCCESSFULLY!");

                        // Set success message
                        ViewBag.Message = "Verification code sent successfully! Redirecting...";
                        ViewBag.MessageType = "success";
                        ViewBag.Email = email;
                        ViewBag.RedirectToOtp = true;
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"❌ EMAIL SENDING FAILED!");
                        Console.WriteLine($"Error: {emailEx.Message}");
                        Console.WriteLine($"Inner Exception: {emailEx.InnerException?.Message}");
                        Console.WriteLine($"Stack Trace: {emailEx.StackTrace}");

                        // Show error to user
                        ViewBag.Message = $"Failed to send email: {emailEx.Message}. Please check your email configuration.";
                        ViewBag.MessageType = "danger";
                        return View();
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ User not found - but showing success message for security");

                    // Always show success message (prevent email enumeration)
                    ViewBag.Message = "If an account with that email exists, a verification code has been sent.";
                    ViewBag.MessageType = "success";
                    ViewBag.Email = email;
                    ViewBag.RedirectToOtp = true;
                }

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CRITICAL ERROR IN FORGOT PASSWORD:");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ViewBag.Message = $"An error occurred: {ex.Message}";
                ViewBag.MessageType = "danger";
                return View();
            }
        }

        // GET: Account/VerifyOtp
        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new VerifyOtpViewModel
            {
                Email = email
            };

            return View(model);
        }

        // POST: Account/VerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    ViewBag.ErrorMessage = "Invalid request. Please start over.";
                    return View(model);
                }

                // Check if account is locked out
                if (PasswordResetHelper.IsLockedOut(user.OtpLockoutEnd))
                {
                    var remainingTime = PasswordResetHelper.GetRemainingLockoutMinutes(user.OtpLockoutEnd);
                    ViewBag.ErrorMessage = $"Too many failed attempts. Please try again in {remainingTime} minutes.";
                    return View(model);
                }

                // Check if OTP has expired
                if (PasswordResetHelper.IsExpired(user.OtpExpiryTime))
                {
                    ViewBag.ErrorMessage = "This verification code has expired. Please request a new one.";
                    return View(model);
                }

                // Verify OTP
                if (!PasswordResetHelper.VerifyOtp(model.OtpCode, user.PasswordResetOtpHash))
                {
                    // Increment attempt count
                    user.OtpAttemptCount++;

                    // Lock account after 5 failed attempts
                    if (user.OtpAttemptCount >= 5)
                    {
                        user.OtpLockoutEnd = DateTime.UtcNow.AddMinutes(15);
                        ViewBag.ErrorMessage = "Too many failed attempts. Your account has been locked for 15 minutes.";
                    }
                    else
                    {
                        ViewBag.ErrorMessage = $"Invalid verification code. {5 - user.OtpAttemptCount} attempts remaining.";
                    }

                    await _context.SaveChangesAsync();
                    return View(model);
                }

                // OTP is valid - generate reset token
                var resetToken = PasswordResetHelper.GenerateResetToken();
                user.PasswordResetToken = resetToken;
                user.ResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(15);
                user.PasswordResetOtpHash = null; // Invalidate OTP
                user.OtpAttemptCount = 0;

                await _context.SaveChangesAsync();

                return RedirectToAction("ResetPassword", new { token = resetToken });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in VerifyOtp: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred. Please try again.";
                return View(model);
            }
        }

        // POST: Account/ResendOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string email)
        {
            Console.WriteLine($"=== RESEND OTP REQUEST ===");
            Console.WriteLine($"Email: {email}");
            Console.WriteLine($"Time: {DateTime.Now}");

            if (string.IsNullOrWhiteSpace(email))
            {
                Console.WriteLine("❌ Email is null or empty");
                return Json(new { success = false, message = "Invalid email address" });
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    Console.WriteLine("⚠️ User not found");
                    // Return success even if not found (prevent enumeration)
                    return Json(new { success = true, message = "If your email exists, verification code has been sent" });
                }

                Console.WriteLine($"✅ User found: {user.Email}");

                // Check if locked out
                if (PasswordResetHelper.IsLockedOut(user.OtpLockoutEnd))
                {
                    var remainingTime = PasswordResetHelper.GetRemainingLockoutMinutes(user.OtpLockoutEnd);
                    Console.WriteLine($"🔒 Account is locked out for {remainingTime} more minutes");
                    return Json(new { success = false, message = $"Too many attempts. Please wait {remainingTime} minutes." });
                }

                // Generate new OTP
                var otp = PasswordResetHelper.GenerateOtp();
                var otpHash = PasswordResetHelper.HashOtp(otp);

                Console.WriteLine($"🔑 New OTP generated: {otp}");
                Console.WriteLine($"🔒 OTP Hash: {otpHash}");

                user.PasswordResetOtpHash = otpHash;
                user.OtpExpiryTime = DateTime.UtcNow.AddMinutes(10);
                user.OtpAttemptCount = 0;

                await _context.SaveChangesAsync();
                Console.WriteLine("💾 New OTP saved to database");

                // Send email - MUST AWAIT
                try
                {
                    Console.WriteLine("📧 Sending email...");
                    await _emailService.SendOtpEmailAsync(user.Email, otp);
                    Console.WriteLine("✅ EMAIL SENT SUCCESSFULLY!");
                    return Json(new { success = true, message = "New verification code sent to your email!" });
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"❌ EMAIL SENDING FAILED!");
                    Console.WriteLine($"Error: {emailEx.Message}");
                    Console.WriteLine($"Inner Exception: {emailEx.InnerException?.Message}");
                    return Json(new { success = false, message = $"Failed to send email: {emailEx.Message}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CRITICAL ERROR IN RESEND OTP:");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        // GET: Account/ResetPassword
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("ForgotPassword");
            }

            // Verify token exists and is valid
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PasswordResetToken == token);

            if (user == null || PasswordResetHelper.IsExpired(user.ResetTokenExpiryTime))
            {
                ViewBag.ErrorMessage = "This password reset link has expired or is invalid.";
                return View("ResetPasswordError");
            }

            ViewBag.Token = token;
            return View();
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
        {
            ViewBag.Token = token;

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.ErrorMessage = "Please fill in all fields.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View();
            }

            // Validate password strength
            var (isValid, errorMessage) = PasswordResetHelper.ValidatePasswordStrength(newPassword);
            if (!isValid)
            {
                ViewBag.ErrorMessage = errorMessage;
                return View();
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PasswordResetToken == token);

                if (user == null || PasswordResetHelper.IsExpired(user.ResetTokenExpiryTime))
                {
                    ViewBag.ErrorMessage = "This password reset link has expired or is invalid.";
                    return View();
                }

                // Update password
                user.Password = PasswordResetHelper.HashPassword(newPassword);

                // Clear all reset data
                user.PasswordResetToken = null;
                user.ResetTokenExpiryTime = null;
                user.OtpExpiryTime = null;
                user.OtpLockoutEnd = null;
                user.PasswordResetOtpHash = null;

                await _context.SaveChangesAsync();

                // Send confirmation email (fire and forget)
                _ = _emailService.SendPasswordResetConfirmationAsync(user.Email);

                return RedirectToAction("ResetPasswordSuccess");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ResetPassword: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred. Please try again.";
                return View();
            }
        }

        // GET: Account/ResetPasswordSuccess
        [HttpGet]
        public IActionResult ResetPasswordSuccess()
        {
            return View();
        }

        // GET: Account/ResetPasswordError
        [HttpGet]
        public IActionResult ResetPasswordError()
        {
            return View();
        }
    }
}


namespace BrandsStore.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string otp);
        Task SendPasswordResetConfirmationAsync(string toEmail);
    }
}
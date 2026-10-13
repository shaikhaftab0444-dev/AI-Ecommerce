using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace BrandsStore.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            try
            {
                Console.WriteLine($"");
                Console.WriteLine($"=== SENDING OTP EMAIL VIA GMAIL ===");
                Console.WriteLine($"To: {toEmail}");
                Console.WriteLine($"OTP: {otp}");
                Console.WriteLine($"Time: {DateTime.Now}");

                // Read Gmail configuration
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");

                Console.WriteLine($"SMTP Server: {smtpServer}:{smtpPort}");
                Console.WriteLine($"From Email: {fromEmail}");
                Console.WriteLine($"Username: {username}");
                Console.WriteLine($"Password: {new string('*', password?.Length ?? 0)}"); // Hide password

                // Validate configuration
                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(username) ||
                    string.IsNullOrEmpty(password) || string.IsNullOrEmpty(smtpServer))
                {
                    throw new Exception("❌ Email configuration is incomplete in appsettings.json");
                }

                // Create email message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "BrandsStore Password Reset"),
                    Subject = "🔐 Password Reset OTP - BrandsStore",
                    Body = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset='utf-8'>
                            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        </head>
                        <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
                            <div style='max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                                <!-- Header -->
                                <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center;'>
                                    <h1 style='color: white; margin: 0; font-size: 28px;'>🔐 Password Reset Request</h1>
                                </div>
                                
                                <!-- Content -->
                                <div style='padding: 40px 30px;'>
                                    <p style='font-size: 16px; color: #333333; line-height: 1.6; margin-bottom: 20px;'>
                                        Hello,
                                    </p>
                                    <p style='font-size: 16px; color: #333333; line-height: 1.6; margin-bottom: 30px;'>
                                        You have requested to reset your password for your BrandsStore account. 
                                        Please use the following One-Time Password (OTP) to proceed:
                                    </p>
                                    
                                    <!-- OTP Box -->
                                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                                                padding: 25px; 
                                                text-align: center; 
                                                border-radius: 10px; 
                                                margin: 30px 0;
                                                box-shadow: 0 4px 15px rgba(102, 126, 234, 0.3);'>
                                        <div style='font-size: 42px; 
                                                    font-weight: bold; 
                                                    letter-spacing: 15px; 
                                                    color: white;
                                                    text-shadow: 2px 2px 4px rgba(0,0,0,0.2);'>
                                            {otp}
                                        </div>
                                    </div>
                                    
                                    <!-- Warning -->
                                    <div style='background-color: #fef3cd; 
                                                border-left: 4px solid #ffc107; 
                                                padding: 15px; 
                                                border-radius: 5px;
                                                margin: 20px 0;'>
                                        <p style='margin: 0; font-size: 14px; color: #856404;'>
                                            ⏰ <strong>Important:</strong> This code will expire in <strong style='color: #dc3545;'>10 minutes</strong>.
                                        </p>
                                    </div>
                                    
                                    <p style='font-size: 14px; color: #666666; line-height: 1.6; margin-top: 20px;'>
                                        If you didn't request this password reset, please ignore this email or contact our support team if you have concerns.
                                    </p>
                                    
                                    <!-- Security Tips -->
                                    <div style='background-color: #e7f3ff; 
                                                border-left: 4px solid #2196F3; 
                                                padding: 15px; 
                                                border-radius: 5px;
                                                margin: 20px 0;'>
                                        <p style='margin: 0; font-size: 13px; color: #0c5460;'>
                                            🛡️ <strong>Security Tip:</strong> Never share this OTP with anyone. BrandsStore staff will never ask for your OTP.
                                        </p>
                                    </div>
                                </div>
                                
                                <!-- Footer -->
                                <div style='background-color: #f8f9fa; padding: 20px; text-align: center; border-top: 1px solid #e9ecef;'>
                                    <p style='margin: 0; font-size: 12px; color: #6c757d;'>
                                        © 2025 BrandsStore - Your Trusted Shopping Partner
                                    </p>
                                    <p style='margin: 10px 0 0 0; font-size: 11px; color: #adb5bd;'>
                                        This is an automated message, please do not reply to this email.
                                    </p>
                                </div>
                            </div>
                        </body>
                        </html>
                    ",
                    IsBodyHtml = true,
                    Priority = MailPriority.High
                };

                mailMessage.To.Add(toEmail);

                // Configure SMTP client for Gmail
                using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(username, password);
                    smtpClient.EnableSsl = true; // CRITICAL for Gmail
                    smtpClient.Timeout = 30000; // 30 seconds
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    Console.WriteLine("📧 Connecting to Gmail SMTP server...");
                    await smtpClient.SendMailAsync(mailMessage);
                    Console.WriteLine("✅ EMAIL SENT SUCCESSFULLY!");
                }
            }
            catch (SmtpFailedRecipientsException ex)
            {
                Console.WriteLine($"❌ RECIPIENT ERROR:");
                Console.WriteLine($"Failed Recipients: {ex.FailedRecipient}");
                Console.WriteLine($"Message: {ex.Message}");
                throw new Exception($"Invalid recipient email: {ex.FailedRecipient}", ex);
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"❌ SMTP ERROR:");
                Console.WriteLine($"Status Code: {ex.StatusCode}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");

                // Provide specific error messages
                if (ex.Message.Contains("authentication"))
                {
                    throw new Exception("Gmail authentication failed. Please check your username and App Password.", ex);
                }
                else if (ex.Message.Contains("relay"))
                {
                    throw new Exception("Gmail relay access denied. Ensure 2-Step Verification and App Password are configured.", ex);
                }
                else
                {
                    throw new Exception($"Failed to send email via Gmail: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GENERAL ERROR:");
                Console.WriteLine($"Type: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw new Exception($"Email service error: {ex.Message}", ex);
            }
        }

        public async Task SendPasswordResetConfirmationAsync(string toEmail)
        {
            try
            {
                Console.WriteLine($"📧 Sending password reset confirmation to: {toEmail}");

                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "BrandsStore Security"),
                    Subject = "✅ Password Reset Successful - BrandsStore",
                    Body = @"
                        <!DOCTYPE html>
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                                <div style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                                    <h1 style='color: white; margin: 0;'>✅ Password Reset Successful</h1>
                                </div>
                                <div style='background: white; padding: 30px; border: 1px solid #e5e7eb; border-radius: 0 0 10px 10px;'>
                                    <p style='font-size: 16px;'>Hello,</p>
                                    <p style='font-size: 16px;'>Your password has been successfully reset for your BrandsStore account.</p>
                                    <p style='font-size: 16px;'>If you didn't make this change, please contact our support team immediately.</p>
                                    <hr style='margin: 30px 0; border: none; border-top: 1px solid #e5e7eb;'>
                                    <p style='color: #6b7280; font-size: 12px; text-align: center;'>
                                        © 2025 BrandsStore - Your Trusted Shopping Partner
                                    </p>
                                </div>
                            </div>
                        </body>
                        </html>
                    ",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(username, password);
                    smtpClient.EnableSsl = true;
                    await smtpClient.SendMailAsync(mailMessage);
                }

                Console.WriteLine($"✅ Confirmation email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending confirmation email: {ex.Message}");
                // Don't throw - confirmation email failure shouldn't break the flow
            }
        }
    }
}
using BrandsStore.Data;
using BrandsStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BrandsStore.Controllers
{
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SupportController> _logger;

        public SupportController(ApplicationDbContext context, ILogger<SupportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Support
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Support/SubmitTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTicket(
            string Name,
            string Email,
            string OrderId,
            string Category,
            string Priority,
            string Message)
        {
            try
            {
                // If you have a SupportTicket model/table, save it here.
                // Example (uncomment if you add the model):
                /*
                var ticket = new SupportTicket
                {
                    Name      = Name,
                    Email     = Email,
                    OrderId   = OrderId,
                    Category  = Category,
                    Priority  = Priority,
                    Message   = Message,
                    CreatedAt = DateTime.Now,
                    Status    = "Open"
                };
                _context.SupportTickets.Add(ticket);
                await _context.SaveChangesAsync();
                */

                // Log the ticket for now
                _logger.LogInformation(
                    "Support ticket received — Name: {Name}, Email: {Email}, Category: {Category}, Priority: {Priority}, OrderId: {OrderId}",
                    Name, Email, Category, Priority, OrderId);

                TempData["SupportSuccess"] = "Your message has been sent! We'll reply within 2 hours.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving support ticket");
                TempData["SupportError"] = "Something went wrong. Please try again or contact us directly.";
                return RedirectToAction("Index");
            }
        }
    }
}
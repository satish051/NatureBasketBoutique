using Microsoft.AspNetCore.Identity.UI.Services;

namespace NatureBasketBoutique.Utility
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Logic to send email would go here.
            // For now, we just return a completed task to satisfy the interface.
            return Task.CompletedTask;
        }
    }

}
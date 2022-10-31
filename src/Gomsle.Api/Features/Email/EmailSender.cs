using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Gomsle.Api.Features.Email;

public class EmailSender : IEmailSender
{
    private readonly EmailSenderOptions _emailSenderOptions;

    public EmailSender(EmailSenderOptions emailSenderOptions)
    {
        _emailSenderOptions = emailSenderOptions;
    }

    public async Task Send(string email, string subject, string message, CancellationToken cancellationToken = default)
    {
        var sendGridClient = new SendGridClient(_emailSenderOptions.ApiKey);
        var emailFrom = new EmailAddress(_emailSenderOptions.FromAddress, _emailSenderOptions.FromName);
        var emailTo = new EmailAddress(email);

        var sendGridMessage = MailHelper.CreateSingleEmail(emailFrom, emailTo, subject, message, message);
        sendGridMessage.SetClickTracking(false, false);

        var response = await sendGridClient.SendEmailAsync(sendGridMessage, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            throw new Exception(response.StatusCode.ToString());
        }
    }
}
namespace Gomsle.Api.Features.Email;

public interface IEmailSender
{
    Task Send(string email, string subject, string message);
}
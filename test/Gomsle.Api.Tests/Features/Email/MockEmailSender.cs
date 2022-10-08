using Gomsle.Api.Features.Email;

namespace Gomsle.Api.Tests.Features.Email;

public class MockEmailSender : IEmailSender
{
    public Task Send(string email, string subject, string message)
    {
        return Task.CompletedTask;
    }
}
namespace PhotoVoterMvc.Services
{
    public interface IEmailNotification
    {
        string To { get; }
        string Subject { get; }
        string Body { get; }
    }
}
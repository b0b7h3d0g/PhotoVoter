namespace PhotoVoterMvc.Services
{
    public interface INotificationService
    {
       void Notify(IEmailNotification emailNotification);
    }
}
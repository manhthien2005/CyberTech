namespace CyberTech.Services
{
    public interface IRecaptchaService
    {
        Task<bool> VerifyAsync(string recaptchaResponse);
    }
}
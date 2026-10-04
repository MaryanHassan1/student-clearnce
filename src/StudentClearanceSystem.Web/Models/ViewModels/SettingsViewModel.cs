namespace StudentClearanceSystem.Web.Models.ViewModels;

public class SettingsViewModel
{
    public string FullName { get; set; } = string.Empty;
    public UpdateEmailViewModel UpdateEmail { get; set; } = new();
    public ChangePasswordViewModel ChangePassword { get; set; } = new();
}

namespace IndustrialMonitor.WinForms.UI;

public interface IPage
{
    string Key { get; }
    string Title { get; }
    string Subtitle { get; }
    Task ActivateAsync();
}

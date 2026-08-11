namespace CASAPahampang.Interfaces;
public interface IContentModerationService
{
    Task InitializeAsync();
    bool IsFlagged(string message);
}
namespace SUP.Services;
public interface INavigationService
{
    void NavigateTo<TViewModel>(Action<TViewModel>? init = null) where TViewModel : class;
    bool CanGoBack { get; }
    void GoBack();
}

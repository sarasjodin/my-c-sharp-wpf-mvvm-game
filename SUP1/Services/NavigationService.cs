using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace SUP.Services;

public class NavigationService : INavigationService
{
    private readonly NavigationStore _store;
    private readonly IServiceProvider _sp;

    public sealed class NavigateCommand<TViewModel> : ICommand where TViewModel : class
    {
        private readonly INavigationService _nav;
        private readonly Action<TViewModel>? _init;
        private readonly Func<bool>? _can;

        public NavigateCommand(INavigationService nav,
                               Action<TViewModel>? init = null,
                               Func<bool>? canExecute = null)
        {
            _nav = nav;
            _init = init;
            _can = canExecute;
        }

        public bool CanExecute(object? parameter) => _can?.Invoke() ?? true;
        public void Execute(object? parameter) => _nav.NavigateTo<TViewModel>(_init);

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public NavigationService(NavigationStore store, IServiceProvider sp)
    {
        _store = store;
        _sp = sp;
    }

    public void NavigateTo<TViewModel>(Action<TViewModel>? init = null) where TViewModel : class
    {
        if (_store.CurrentViewModel is not null)
            _store.History.Push(_store.CurrentViewModel);

        var vm = _sp.GetService<TViewModel>() ?? ActivatorUtilities.CreateInstance<TViewModel>(_sp);
        init?.Invoke(vm);                // skickar initdata
        _store.CurrentViewModel = vm;
    }

    public void NavigateTo<TViewModel>() where TViewModel : class
    => NavigateTo<TViewModel>(null);

    public bool CanGoBack => _store.History.Count > 0;

    public void GoBack()
    {
        if (CanGoBack)
            _store.CurrentViewModel = _store.History.Pop();
    }
}

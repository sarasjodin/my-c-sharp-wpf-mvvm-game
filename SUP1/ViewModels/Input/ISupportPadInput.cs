using System.Windows.Input;

namespace SUP.ViewModels.Input

{
    public interface ISupportPadInput
    {
        public ICommand PressPadIndexCommand { get; }
    }
}

using SUP.ViewModels;
using System.Windows.Controls;

namespace SUP.Views
{
    public partial class ScoreboardView : UserControl
    {
        public ScoreboardView()
        {
            InitializeComponent();

            // Trigga laddningen när vyn visas
            Loaded += async (_, __) =>
            {
                if (DataContext is ScoreboardViewModel vm)
                    await vm.RefreshAsync();
            };
        }
    }
}

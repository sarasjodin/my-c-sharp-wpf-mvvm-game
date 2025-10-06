using System.Windows.Controls;
using System.Windows.Input;

namespace SUP.Views;

/// <summary>
/// Interaction logic for BoardView.xaml
/// </summary>
public partial class BoardView : UserControl
{
    public BoardView()
    {
        InitializeComponent();
        Loaded += BoardView_Loaded;
    }

    private void BoardView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Keyboard.Focus(this);
    }

}

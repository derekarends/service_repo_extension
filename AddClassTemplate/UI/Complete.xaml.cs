using Microsoft.VisualStudio.PlatformUI;

namespace ThinkovatorInc.AddClassTemplate
{
    public partial class Complete : DialogWindow
    {
        public Complete()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}

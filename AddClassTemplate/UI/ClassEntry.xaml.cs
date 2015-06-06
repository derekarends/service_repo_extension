using Microsoft.VisualStudio.PlatformUI;

namespace ThinkovatorInc.AddClassTemplate
{
    public partial class ClassEntry : DialogWindow
    {
        public string BaseClassName { get; set; }
        public bool CreateRequestModel { get; set; }
        public bool CreateResponseModel { get; set; }
        public bool CreateService { get; set; }
        public bool CreateRepo { get; set; }

        public ClassEntry()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}

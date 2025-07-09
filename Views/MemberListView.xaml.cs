using ClubManagementApp.ViewModels;
using System.Windows;

namespace ClubManagementApp.Views
{
    /// <summary>
    /// Interaction logic for MemberListView.xaml
    /// </summary>
    public partial class MemberListView : Window
    {
        public MemberListView()
        {
            InitializeComponent();
        }

        public MemberListView(MemberListViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
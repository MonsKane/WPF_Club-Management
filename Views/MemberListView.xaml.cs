using ClubManagementApp.ViewModels;
using ClubManagementApp.Models;
using System.Windows;
using System.Windows.Controls;

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

        private void AddMemberButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MemberListViewModel viewModel)
            {
                viewModel.AddMemberCommand?.Execute(null);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MemberListViewModel viewModel)
            {
                viewModel.RefreshCommand?.Execute(null);
            }
        }

        private void EditMemberButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is User member)
            {
                if (DataContext is MemberListViewModel viewModel)
                {
                    viewModel.EditMemberCommand?.Execute(member);
                }
            }
        }

        private void RemoveMemberButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is User member)
            {
                if (DataContext is MemberListViewModel viewModel)
                {
                    viewModel.DeleteMemberCommand?.Execute(member);
                }
            }
        }
    }
}
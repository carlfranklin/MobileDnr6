using DotNetRocks.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DotNetRocks.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayListManagerPage : ContentPage
    {
        public PlayListManagerPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            await Task.Delay(0);
            var viewModel = (PlayListManagerPageViewModel)BindingContext;
            viewModel.Page = this;
        }

    }
}
using Android.App;
using Android.OS;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace WB.UI.Interviewer.Droid.MvxDroidAdaptation
{
    [Activity(Label = "View for anyViewModel")]
    public class MvxNavigationActivity
        : AndroidActivity
        , IMvxPageNavigationProvider
    {
        private NavigationPage _navPage;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Forms.Init(this, bundle);

            Mvx.Resolve<IMvxPageNavigationHost>().NavigationProvider = this;
            Mvx.Resolve<IMvxAppStart>().Start();
        }

        public async void Push(Page page)
        {
            if (_navPage != null)
            {
                await _navPage.PushAsync(page);
                return;
            }

            _navPage = new NavigationPage(page);
            SetPage(_navPage);
        }

        public async void Pop()
        {
            if (_navPage == null)
                return;

            await _navPage.PopAsync();
        }

        public NavigationPage NavigationPage { get { return _navPage; } }
    }
}
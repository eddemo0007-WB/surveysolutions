using Android.Content;
using Android.Views;
using WB.Core.SharedKernels.Enumerator.Services.MapSynchronization;
using WB.Core.SharedKernels.Enumerator.Services.Synchronization;
using WB.Core.SharedKernels.Enumerator.Views;
using WB.UI.Shared.Enumerator.Activities;
using WB.UI.Shared.Enumerator.Services;
using Toolbar=AndroidX.AppCompat.Widget.Toolbar;

namespace WB.UI.Supervisor.Activities
{
    [Activity(Theme = "@style/GrayAppTheme",
        WindowSoftInputMode = SoftInput.StateHidden,
        HardwareAccelerated = true,
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize,
        Exported = false,
        NoHistory = true)]
    public class MapsActivity : BaseActivity<MapsViewModel>, ISyncBgService<MapSyncProgressStatus>, ISyncServiceHost<MapDownloadBackgroundService>
    {
        public ServiceBinder<MapDownloadBackgroundService> Binder { get; set; }

        protected override int ViewResourceId
        {
            get { return Resource.Layout.maps; }
        }
        
        public override bool OnSupportNavigateUp() {
            OnBackPressedDispatcher.OnBackPressed();
            return true;
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var toolbar = this.FindViewById<Toolbar>(Resource.Id.toolbar);
            toolbar.Title = "";
            this.SetSupportActionBar(toolbar);
        }

        protected override void OnViewModelSet()
        {
            base.OnViewModelSet();
            this.ViewModel.Synchronization.MapSyncBackgroundService = this;
        }

        protected override void OnStart()
        {
            base.OnStart();
            this.BindService(new Intent(this, typeof(MapDownloadBackgroundService)), new SyncServiceConnection<MapDownloadBackgroundService>(this), Bind.AutoCreate);
        }
        
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            this.MenuInflater.Inflate(Resource.Menu.maps, menu);
            return base.OnCreateOptionsMenu(menu);
        }
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_map_synchronization:
                    this.ViewModel.MapSynchronizationCommand.Execute();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void StartSync() => this.Binder.GetService().SyncMaps();

        public MapSyncProgressStatus CurrentProgress => this.Binder.GetService().CurrentProgress;
    }
}

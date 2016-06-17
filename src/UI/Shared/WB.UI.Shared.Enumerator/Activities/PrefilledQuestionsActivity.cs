using Android.App;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Droid.Support.V7.RecyclerView;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.UI.Shared.Enumerator.CustomControls;

namespace WB.UI.Shared.Enumerator.Activities
{
    [Activity(Label = "", Theme = "@style/BlueAppTheme", 
        HardwareAccelerated = true,
        WindowSoftInputMode = SoftInput.StateAlwaysHidden | SoftInput.AdjustPan,
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class PrefilledQuestionsActivity : BaseActivity<PrefilledQuestionsViewModel>
    {
        protected override int ViewResourceId => Resource.Layout.prefilled_questions;

        public override void OnBackPressed() => this.ViewModel.NavigateToPreviousViewModel();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetSupportActionBar(this.FindViewById<Toolbar>(Resource.Id.toolbar));

            var listOfInterviewQuestionsAndGroups = this.FindViewById<MvxRecyclerView>(Resource.Id.questionnaireEntitiesList);
            var layoutManager = new LinearLayoutManager(this);
            
            listOfInterviewQuestionsAndGroups.SetLayoutManager(layoutManager);
            listOfInterviewQuestionsAndGroups.HasFixedSize = true;
            listOfInterviewQuestionsAndGroups.Adapter = new InterviewEntityAdapter((IMvxAndroidBindingContext)this.BindingContext);
        }
    }
}
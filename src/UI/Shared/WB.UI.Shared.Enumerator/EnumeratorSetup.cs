using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Converters;
using Cirrious.CrossCore.Droid.Platform;
using Cirrious.CrossCore.Exceptions;
using Cirrious.MvvmCross.Binding.Bindings.Target.Construction;
using Cirrious.MvvmCross.Binding.Combiners;
using Cirrious.MvvmCross.Binding.Droid.Views;
using Cirrious.MvvmCross.Droid.Platform;
using Cirrious.MvvmCross.Views;
using MvvmCross.Droid.Support.V7.RecyclerView;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.Enumerator;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.UI.Shared.Enumerator.Activities;
using WB.UI.Shared.Enumerator.Converters;
using WB.UI.Shared.Enumerator.CustomBindings;
using WB.UI.Shared.Enumerator.CustomControls;
using WB.UI.Shared.Enumerator.CustomControls.MaskedEditTextControl;
using WB.UI.Shared.Enumerator.ValueCombiners;
using Xamarin;
using BindingFlags = System.Reflection.BindingFlags;

namespace WB.UI.Shared.Enumerator
{
    public abstract class EnumeratorSetup : MvxAndroidSetup
    {
        protected EnumeratorSetup(Context applicationContext) : base(applicationContext)
        {
            //restart the app to avoid incorrect state
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                ProcessException(args.Exception);
            };
            AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
            {
                var exception = args.Exception;

                // this is super dirty hack in order to get exception's stack trace which happend inside async method
                FieldInfo stackTrace = typeof(System.Exception).GetField("stack_trace", BindingFlags.NonPublic | BindingFlags.Instance);
                stackTrace?.SetValue(exception, null);
                this.ProcessException(Java.Lang.Throwable.FromException(exception));

                ProcessException(args.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                object exceptionObject = args.ExceptionObject;

                var typedException = exceptionObject as Exception;
                if (typedException != null)
                {
                    ProcessException(typedException);
                }
                else
                {
                    ProcessException(new Exception("Untyped exception message: '" + exceptionObject + "'"));
                }
            };
        }

        private void ProcessException(Exception exception)
        {
            Mvx.Error("UncaughtExceptionHandler with exception {0}", exception.ToLongString());
            Mvx.Resolve<ILogger>().Fatal("UncaughtExceptionHandler", exception);

            var currentActivity = Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity;
            if (this.StartupActivityType != null && currentActivity != null)
            {
                Intent intent = new Intent(currentActivity, StartupActivityType);
                intent.AddFlags(ActivityFlags.NewTask);
                Application.Context.StartActivity(intent);
            }
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
        }

        protected abstract Type StartupActivityType { get; }

        protected override void InitializeViewLookup()
        {
            var viewModelViewLookup = new Dictionary<Type, Type>()
            {
                {typeof (PrefilledQuestionsViewModel), typeof (PrefilledQuestionsActivity)}
            };

            var container = Mvx.Resolve<IMvxViewsContainer>();
            container.AddAll(viewModelViewLookup);
        }

        protected override void FillValueConverters(IMvxValueConverterRegistry registry)
        {
            base.FillValueConverters(registry);
            
            Mvx.CallbackWhenRegistered<IMvxValueCombinerRegistry>(combinerRegistry => 
                combinerRegistry.AddOrOverwriteFrom(Assembly.GetAssembly(typeof(LayoutBackgroundStyleValueCombiner))));

            registry.AddOrOverwrite("Localization", new LocalizationValueConverter());
            registry.AddOrOverwrite("GroupStateToColor", new GroupStateToColorConverter());
            registry.AddOrOverwrite("InMemoryImageValueWithDefault", new InMemoryImageValueWithDefaultConverter());
            registry.AddOrOverwrite("ToGoogleMapUrl", new ToGoogleMapUrlConverter());
            registry.AddOrOverwrite("QuestionLayoutStyleBackground", new QuestionLayoutStyleBackgroundConverter());
            registry.AddOrOverwrite("QuestionEditorStyleBackground", new QuestionEditorStyleBackgroundConverter());
            registry.AddOrOverwrite("IsStringNotEmpty", new IsStringNotEmptyConverter());
            registry.AddOrOverwrite("MediaButtonStyleBackground", new MediaQuestionButtonBackgroundConverter());
            registry.AddOrOverwrite("ViewOptionStyleBackground", new ViewOptionStyleBackgroundConverter());
            registry.AddOrOverwrite("SectionStyleBackground", new SectionStyleBackgroundConverter());
            registry.AddOrOverwrite("ToSpannableGroupTitle", new ToSpannableGroupTitleConverter());
        }

        protected override void FillTargetFactories(IMvxTargetBindingFactoryRegistry registry)
        {
            registry.RegisterCustomBindingFactory<TextView>("Hint", (view) => new TextViewHintBinding(view));
            registry.RegisterCustomBindingFactory<TextView>("Html", (view) => new TextViewHtmlBinding(view));
            registry.RegisterCustomBindingFactory<TextView>("TextFormatted", (view) => new TextViewTextFormattedBinding(view));
            registry.RegisterCustomBindingFactory<TextView>("SideBarPrefillQuestion", (view) => new TextViewSideBarPrefillQuestionBinding(view));
            registry.RegisterCustomBindingFactory<TextView>("IsSelectedYesNoOptionColor", (view) => new TextViewIsSelectedYesNoOptionColorBinding(view));
            registry.RegisterCustomBindingFactory<MaskedEditText>("IsMaskedQuestionAnswered", (editText) => new MaskedEditTextIsMaskedQuestionAnsweredBinding(editText));
            registry.RegisterCustomBindingFactory<EditText>("FocusValueChanged", (editText) => new EditTextFocusValueChangedBinding(editText));
            registry.RegisterCustomBindingFactory<EditText>("SetFocus", (editText) => new EditTextSetFocusBinding(editText));
            registry.RegisterCustomBindingFactory<ProgressBar>("ShowProgress", (view) => new ProgressBarIndeterminateBinding(view));
            registry.RegisterCustomBindingFactory<View>("BackgroundStyle", (view) => new ViewBackgroundDrawableBinding(view));
            registry.RegisterCustomBindingFactory<TextView>("Bold", textView => new TextViewBoldBinding(textView));
            registry.RegisterCustomBindingFactory<EditText>("DecimalPlaces", editText => new EditTextDecimalPlacesBinding(editText));
            registry.RegisterCustomBindingFactory<EditText>("DateChange", editText => new EditTextDateBinding(editText));
            registry.RegisterCustomBindingFactory<TextView>("GroupInfo", textView => new TextViewGroupInfoBinding(textView));
            registry.RegisterCustomBindingFactory<Button>("ButtonGroupStyle", button => new ButtonGroupStyleBinding(button));
            registry.RegisterCustomBindingFactory<TextView>("GroupStatus", textView => new TextViewGroupStatusBinding(textView));
            registry.RegisterCustomBindingFactory<View>("HideKeyboardOnClick", view => new ViewHideKeyboardOnClickBinding(view));
            registry.RegisterCustomBindingFactory<MvxAutoCompleteTextView>("HidePopupOnDone", view => new MvxAutoCompleteTextViewHidePopupOnDoneBinding(view));
            registry.RegisterCustomBindingFactory<MvxAutoCompleteTextView>("ResetText", view => new MvxAutoCompleteTextViewResetTextBinding(view));
            registry.RegisterCustomBindingFactory<MvxAutoCompleteTextView>("ShowPopupOnFocus", view => new MvxAutoCompleteTextViewShowPopupOnFocusBinding(view));
            registry.RegisterCustomBindingFactory<ViewGroup>("ColorByInterviewStatus", view => new ViewGroupColorByInterviewStatusBinding(view));
            registry.RegisterCustomBindingFactory<ViewGroup>("StatusBarColorByInterviewStatus", view => new ViewGroupStatusBarColorByInterviewStatusBinding(view));
            registry.RegisterCustomBindingFactory<View>("Transparent", view => new ViewTransparentBinding(view));
            registry.RegisterCustomBindingFactory<View>("PaddingLeft", view => new ViewPaddingLeftBinding(view));
            registry.RegisterCustomBindingFactory<View>("Activated", view => new ViewActivatedBinding(view));
            registry.RegisterCustomBindingFactory<TextView>("TextColor", (view) => new TextViewTextColorBinding(view));
            registry.RegisterCustomBindingFactory<TextView>("UnderlinePressed", (view) => new TextViewUnderlinePressedBinding(view));

            base.FillTargetFactories(registry);
        }

        protected override IEnumerable<Assembly> AndroidViewAssemblies => 
            base.AndroidViewAssemblies.Union(new[] {
                typeof (FlowLayout).Assembly,
                typeof (MvxRecyclerView).Assembly,
                typeof (DrawerLayout).Assembly,
                typeof (SwitchCompat).Assembly
            });

        protected override IEnumerable<Assembly> GetViewModelAssemblies()
        {
            return new[]
            {
                typeof(EnumeratorSharedKernelModule).Assembly,
            };
        }
    }
}
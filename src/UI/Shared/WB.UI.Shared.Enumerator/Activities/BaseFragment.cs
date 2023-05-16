﻿using Android.OS;
using Android.Views;
using MvvmCross.Base;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Views.Fragments;
using MvvmCross.ViewModels;

namespace WB.UI.Shared.Enumerator.Activities
{
    public abstract class BaseFragment<TViewModel> : MvxFragment<TViewModel> where TViewModel : MvxViewModel
    {
        protected abstract int ViewResourceId { get; }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            this.EnsureBindingContextIsSet(inflater);
            return this.BindingInflate(ViewResourceId, null);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            this.BindingContext?.ClearAllBindings();
            this.ViewModel?.DisposeIfDisposable();
        }
    }
}

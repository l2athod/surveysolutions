﻿using Android.Views;
using Android.Widget;
using MvvmCross.Binding;
using MvvmCross.Binding.Droid.Views;
using MvvmCross.Droid.Support.V7.AppCompat.Widget;

namespace WB.UI.Shared.Enumerator.CustomBindings
{
    public class MvxAutoCompleteTextViewShowPopupOnFocusBinding : BaseBinding<MvxAppCompatAutoCompleteTextView, object>
    {
        public MvxAutoCompleteTextViewShowPopupOnFocusBinding(MvxAppCompatAutoCompleteTextView androidControl) : base(androidControl)
        {
        }

        protected override void SetValueToView(MvxAppCompatAutoCompleteTextView view, object value) { }

        public override MvxBindingMode DefaultMode { get { return MvxBindingMode.TwoWay;} }

        public override void SubscribeToEvents()
        {
            this.Target.FocusChange += this.Target_FocusChange;
            this.Target.ItemClick += this.OnItemClick;

            base.SubscribeToEvents();
        }

        void OnItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            this.Target.ClearFocus();
        }

        private void Target_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
            {
                this.Target.ShowDropDown();
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                var editText = this.Target;
                if (editText != null)
                {
                    editText.FocusChange -= this.Target_FocusChange;
                    editText.ItemClick -= this.OnItemClick;
                }
            }
            base.Dispose(isDisposing);
        }
    }
}
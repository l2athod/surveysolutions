using System;
using Android.Views;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Droid.Support.V7.RecyclerView;

namespace WB.UI.Interviewer.Activities.Dashboard
{
    public class ExpandableViewHolder : MvxRecyclerViewHolder
    {
        public View DashboardItem { get; }

        public View DetailsView { get; }

        public View ExpandHandle { get; }
        public ExpandableViewHolder(View itemView, IMvxAndroidBindingContext context) : base(itemView, context)
        {
            this.DetailsView = itemView.FindViewById<View>(Resource.Id.dashboardItemDetails);
            this.DashboardItem = itemView.FindViewById<View>(Resource.Id.dashboardItem);
            this.ExpandHandle = itemView.FindViewById<View>(Resource.Id.expandHandle);

            if (this.DashboardItem != null)
            {
                this.DashboardItem.Click += (sender, args) => this.OnCardClick();
            }
        }

        public event EventHandler CardClick;

        protected virtual void OnCardClick()
        {
            this.CardClick?.Invoke(this, EventArgs.Empty);
        }
    }
}
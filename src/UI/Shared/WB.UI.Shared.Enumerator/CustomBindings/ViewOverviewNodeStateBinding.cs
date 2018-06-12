﻿using System;
using Android.Views;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Overview;

namespace WB.UI.Shared.Enumerator.CustomBindings
{
    public class ViewOverviewNodeStateBinding : BaseBinding<View, OverviewNodeState>
    {
        public ViewOverviewNodeStateBinding(View androidControl) : base(androidControl)
        {
        }

        protected override void SetValueToView(View control, OverviewNodeState value)
        {
            switch (value)
            {
                case OverviewNodeState.Answered:
                    control.SetBackgroundResource(Resource.Drawable.overview_background_answered);
                    break;
                case OverviewNodeState.Commented:
                    control.SetBackgroundResource(Resource.Drawable.overview_background_commented);
                    break;
                case OverviewNodeState.Invalid:
                    control.SetBackgroundResource(Resource.Drawable.overview_background_invalid);
                    break;
                case OverviewNodeState.Unanswered:
                    control.SetBackgroundResource(Resource.Drawable.overview_background_unanswered);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
}

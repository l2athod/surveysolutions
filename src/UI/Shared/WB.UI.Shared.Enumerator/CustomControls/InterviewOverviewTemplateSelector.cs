﻿using System;
using System.Collections.Generic;
using MvvmCross.Droid.Support.V7.RecyclerView.ItemTemplates;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Overview;

namespace WB.UI.Shared.Enumerator.CustomControls
{
    public class InterviewOverviewTemplateSelector : IMvxTemplateSelector
    {
        private readonly Dictionary<Type, int> typeMapping;

        public InterviewOverviewTemplateSelector()
        {
            typeMapping = new Dictionary<Type, int>
            {
                {typeof(OverviewQuestion), Resource.Layout.interview_overview_question},
                {typeof(OverviewGroup), Resource.Layout.interview_overview_group},
                {typeof(OverviewStaticText), Resource.Layout.interview_overview_statictext},
                {typeof(OverviewSection), Resource.Layout.interview_overview_section},
            };
        }

        public int GetItemViewType(object forItemObject)
        {
            if (typeMapping.TryGetValue(forItemObject.GetType(), out int result))
            { 
                return result;
            }

            throw new NotSupportedException($"Display of entity of type {forItemObject.GetType()} not supported");
        }

        public int GetItemLayoutId(int fromViewType)
        {
            return fromViewType;
        }

        public int ItemTemplateId { get; set; }
    }
}

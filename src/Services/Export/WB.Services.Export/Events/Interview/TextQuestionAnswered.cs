﻿using System;
using WB.Services.Export.Events.Interview.Base;

namespace WB.Services.Export.Events.Interview
{
    public class TextQuestionAnswered : QuestionAnswered
    {
        public string Answer { get; private set; }

    }
}

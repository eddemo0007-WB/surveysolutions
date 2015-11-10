﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Main.Core.Documents;
using WB.Core.Infrastructure.EventBus.Lite;

namespace Main.Core.Events.Questionnaire
{
    public class TemplateImported : ILiteEvent
    {
        public QuestionnaireDocument Source { get; set; }
    }
}

﻿using Main.Core.Documents;

namespace Main.Core.Events.Questionnaire
{
    public class TemplateImported
    {
        public QuestionnaireDocument Source { get; set; }
        public bool AllowCensusMode { get; set; }
    }
}

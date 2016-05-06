﻿using System;
using System.Collections.Generic;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.QuestionInfo;
using WB.Core.SharedKernels.QuestionnaireEntities;

namespace WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.ChapterInfo
{
    public class VariableView : DescendantItemView, IQuestionnaireItem
    {
        public VariableView()
        {
            this.Breadcrumbs = new Breadcrumb[] {};
            this.VariableData = new VariableData(VariableType.Boolean, null, null);
        }

        public string ItemId { get; set; }

        public VariableData VariableData { get; set; }

        public ChapterItemType ItemType => ChapterItemType.Variable;

        public List<IQuestionnaireItem> Items
        {
            get { return new List<IQuestionnaireItem>(); }
            set { }
        }

        public Breadcrumb[] Breadcrumbs { get; set; }
        public QuestionnaireInfoFactory.SelectOption[] TypeOptions { get; set; }
    }
}
﻿using System;
using Main.Core.Entities.SubEntities;
using Ncqrs.Commanding.CommandExecution.Mapping.Attributes;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base;

namespace WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Group
{
    [Serializable]
    [MapsToAggregateRootMethod(typeof(Aggregates.Questionnaire), "CloneGroupWithoutChildren")]
    public class CloneGroupWithoutChildrenCommand : FullGroupDataCommand
    {
        public CloneGroupWithoutChildrenCommand(Guid questionnaireId, Guid groupId, Guid responsibleId,
            string title, Propagate propagationKind, Guid? rosterSizeQuestionId, string description, string condition,
            Guid? parentGroupId, Guid sourceGroupId, int targetIndex)
            : base(questionnaireId, groupId, responsibleId, title, propagationKind, rosterSizeQuestionId, description, condition)
        {
            this.ParentGroupId = parentGroupId;
            this.SourceGroupId = sourceGroupId;
            this.TargetIndex = targetIndex;
        }

        public Guid? ParentGroupId { get; private set; }
        public Guid SourceGroupId { get; private set; }
        public int TargetIndex { get; private set; }
    }
}
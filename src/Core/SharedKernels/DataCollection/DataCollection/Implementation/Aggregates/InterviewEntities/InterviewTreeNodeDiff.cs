namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities
{
    public class InterviewTreeNodeDiff
    {
        public InterviewTreeNodeDiff(IInterviewTreeNode sourceNode, IInterviewTreeNode changedNode)
        {
            this.SourceNode = sourceNode;
            this.ChangedNode = changedNode;
        }
        public IInterviewTreeNode SourceNode { get; }
        public IInterviewTreeNode ChangedNode { get; }

        public bool IsNodeAdded => this.SourceNode == null && this.ChangedNode != null;
        public bool IsNodeRemoved => this.SourceNode != null && this.ChangedNode == null;

        public bool IsNodeDisabled => this.SourceNode == null
            ? this.ChangedNode.IsDisabled()
            : !this.SourceNode.IsDisabled() && this.ChangedNode.IsDisabled();

        public bool IsNodeEnabled
            => this.SourceNode != null && this.SourceNode.IsDisabled() && !this.ChangedNode.IsDisabled();

        public static InterviewTreeNodeDiff Create(IInterviewTreeNode source, IInterviewTreeNode changed)
        {
            if (source is InterviewTreeRoster || changed is InterviewTreeRoster)
                return new InterviewTreeRosterDiff(source, changed);
            else if (source is InterviewTreeSection || changed is InterviewTreeSection)
                return new InterviewTreeGroupDiff(source, changed);
            else if (source is InterviewTreeGroup || changed is InterviewTreeGroup)
                return new InterviewTreeGroupDiff(source, changed);
            else if (source is InterviewTreeQuestion || changed is InterviewTreeQuestion)
                return new InterviewTreeQuestionDiff(source, changed);
            else if (source is InterviewTreeStaticText || changed is InterviewTreeStaticText)
                return new InterviewTreeStaticTextDiff(source, changed);
            else if (source is InterviewTreeVariable || changed is InterviewTreeVariable)
                return new InterviewTreeVariableDiff(source, changed);

            return new InterviewTreeNodeDiff(source, changed);
        }
    }

    public class InterviewTreeRosterDiff : InterviewTreeGroupDiff
    {
        public new InterviewTreeRoster SourceNode => base.SourceNode as InterviewTreeRoster;
        public new InterviewTreeRoster ChangedNode => base.ChangedNode as InterviewTreeRoster;

        public InterviewTreeRosterDiff(IInterviewTreeNode sourceNode, IInterviewTreeNode changedNode) : base(sourceNode, changedNode)
        {
        }

        public bool IsRosterTitleChanged
            => this.ChangedNode != null && this.SourceNode?.RosterTitle != this.ChangedNode.RosterTitle;
    }

    public class InterviewTreeGroupDiff : InterviewTreeNodeDiff
    {
        public new InterviewTreeGroup SourceNode => base.SourceNode as InterviewTreeGroup;
        public new InterviewTreeGroup ChangedNode => base.ChangedNode as InterviewTreeGroup;

        public InterviewTreeGroupDiff(IInterviewTreeNode sourceNode, IInterviewTreeNode changedNode) : base(sourceNode, changedNode)
        {
        }
    }

    public class InterviewTreeQuestionDiff : InterviewTreeNodeDiff
    {
        public new InterviewTreeQuestion SourceNode => base.SourceNode as InterviewTreeQuestion;
        public new InterviewTreeQuestion ChangedNode => base.ChangedNode as InterviewTreeQuestion;

        public InterviewTreeQuestionDiff(IInterviewTreeNode sourceNode, IInterviewTreeNode changedNode) : base(sourceNode, changedNode)
        {
        }

        public bool IsValid => this.SourceNode == null || !this.SourceNode.IsValid && this.ChangedNode.IsValid;

        public bool IsInvalid => this.SourceNode == null
            ? !this.ChangedNode.IsValid
            : this.SourceNode.IsValid && !this.ChangedNode.IsValid;

        public bool IsAnswerRemoved => this.SourceNode != null && this.SourceNode.IsAnswered() &&
               (this.ChangedNode == null || !this.ChangedNode.IsAnswered());

        public bool IsAnswerChanged
        {
            get
            {
                if ((SourceNode.IsAnswered() && !ChangedNode.IsAnswered()) ||
                    (!SourceNode.IsAnswered() && ChangedNode.IsAnswered())) return true;

                if (SourceNode.IsText) return !SourceNode.AsText.EqualByAnswer(ChangedNode.AsText);
                if (SourceNode.IsInteger) return !SourceNode.AsInteger.EqualByAnswer(ChangedNode.AsInteger);
                if (SourceNode.IsDouble) return !SourceNode.AsDouble.EqualByAnswer(ChangedNode.AsDouble);
                if (SourceNode.IsDateTime) return !SourceNode.AsDateTime.EqualByAnswer(ChangedNode.AsDateTime);
                if (SourceNode.IsMultimedia) return !SourceNode.AsMultimedia.EqualByAnswer(ChangedNode.AsMultimedia);
                if (SourceNode.IsQRBarcode) return !SourceNode.AsQRBarcode.EqualByAnswer(ChangedNode.AsQRBarcode);
                if (SourceNode.IsGps) return !SourceNode.AsGps.EqualByAnswer(ChangedNode.AsGps);
                if (SourceNode.IsSingleOption) return !SourceNode.AsSingleOption.EqualByAnswer(ChangedNode.AsSingleOption);
                if (SourceNode.IsSingleLinkedOption) return !SourceNode.AsSingleLinkedOption.EqualByAnswer(ChangedNode.AsSingleLinkedOption);
                if (SourceNode.IsMultiOption) return !SourceNode.AsMultiOption.EqualByAnswer(ChangedNode.AsMultiOption);
                if (SourceNode.IsMultiLinkedOption) return !SourceNode.AsMultiLinkedOption.EqualByAnswer(ChangedNode.AsMultiLinkedOption);
                if (SourceNode.IsYesNo) return !SourceNode.AsYesNo.EqualByAnswer(ChangedNode.AsYesNo);
                if (SourceNode.IsTextList) return !SourceNode.AsTextList.EqualByAnswer(ChangedNode.AsTextList);

                return false;
            }
        }
    }

    public class InterviewTreeStaticTextDiff : InterviewTreeNodeDiff
    {
        public new InterviewTreeStaticText SourceNode => base.SourceNode as InterviewTreeStaticText;
        public new InterviewTreeStaticText ChangedNode => base.ChangedNode as InterviewTreeStaticText;

        public InterviewTreeStaticTextDiff(IInterviewTreeNode sourceNode, IInterviewTreeNode changedNode) : base(sourceNode, changedNode)
        {
        }

        public bool IsValid => this.SourceNode == null || !this.SourceNode.IsValid && this.ChangedNode.IsValid;

        public bool IsInvalid => this.SourceNode == null
            ? !this.ChangedNode.IsValid
            : this.SourceNode.IsValid && !this.ChangedNode.IsValid;
    }

    public class InterviewTreeVariableDiff : InterviewTreeNodeDiff
    {
        public new InterviewTreeVariable SourceNode => base.SourceNode as InterviewTreeVariable;
        public new InterviewTreeVariable ChangedNode => base.SourceNode as InterviewTreeVariable;

        public InterviewTreeVariableDiff(IInterviewTreeNode sourceNode, IInterviewTreeNode changedNode) : base(sourceNode, changedNode)
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.GenericSubdomains.Portable;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities
{
    public abstract class InterviewTreeGroup : IInterviewTreeNode, IInternalInterviewTreeNode
    {
        private bool isDisabled;
        private List<IInterviewTreeNode> children = new List<IInterviewTreeNode>();

        protected InterviewTreeGroup(Identity identity, SubstitionText title, IEnumerable<QuestionnaireItemReference> childrenReferences)
        {
            this.childEntitiesReferences = childrenReferences.ToReadOnlyCollection();

            this.Identity = identity;
            this.isDisabled = false;
            this.Title = title;

            foreach (var child in this.Children)
            {
                ((IInternalInterviewTreeNode)child).SetParent(this);
            }
        }

        public Identity Identity { get; private set; }
        public SubstitionText Title { get; private set; }
        public InterviewTree Tree { get; private set; }
        public IInterviewTreeNode Parent { get; private set; }
        public IEnumerable<IInterviewTreeNode> Parents { get; private set; }

        public IReadOnlyCollection<IInterviewTreeNode> Children => this.children;

        void IInternalInterviewTreeNode.SetTree(InterviewTree tree)
        {
            this.Tree = tree;
            this.Title.SetTree(tree);
            foreach (var child in this.Children)
            {
                ((IInternalInterviewTreeNode)child).SetTree(tree);
            }
        }

        private readonly IReadOnlyCollection<QuestionnaireItemReference> childEntitiesReferences;

        public void ActualizeChildren()
        {
            foreach (var childEntityReference in childEntitiesReferences)
            {
                var childEntityId = childEntityReference.Id;
                switch (childEntityReference.Type)
                {
                    case QuestionnaireReferenceType.Roster:
                        var rosterId = childEntityId;
                        var rosterManager = Tree.GetRosterManager(rosterId);
                        List<Identity> expectedRosterIdentities = rosterManager.CalcuateExpectedIdentities(this.Identity);
                        var actualRosterIdentities = GetActualIdentitiesById(rosterId);

                        foreach (var actualRosterIdentity in actualRosterIdentities)
                        {
                            if (expectedRosterIdentities.Contains(actualRosterIdentity))
                                continue;
                            this.RemoveChild(actualRosterIdentity);
                        }

                        for (int index = 0; index < expectedRosterIdentities.Count; index++)
                        {
                            var expectedRosterIdentity = expectedRosterIdentities[index];
                            if (actualRosterIdentities.Contains(expectedRosterIdentity))
                                continue;
                            var expectedRoster = rosterManager.CreateRoster(this.Identity, expectedRosterIdentity, index);
                            this.AddChild(expectedRoster);
                            expectedRoster.ActualizeChildren();
                        }

                        break;
                    case QuestionnaireReferenceType.SubSection:
                        var subSectionIdentity = new Identity(childEntityId, this.RosterVector);
                        if (!HasChild(subSectionIdentity))
                        {
                            var subSection = Tree.CreateSubSection(subSectionIdentity);
                            this.AddChild(subSection);
                            subSection.ActualizeChildren();
                        }
                        break;
                    case QuestionnaireReferenceType.StaticText:
                    case QuestionnaireReferenceType.Variable:
                    case QuestionnaireReferenceType.Question:
                        var entityIdentity = new Identity(childEntityId, this.RosterVector);
                        if (!HasChild(entityIdentity))
                        {
                            this.AddChild(Tree.CreateNode(childEntityReference.Type, entityIdentity));
                        }
                        break;
                }
            }
        }

        private List<Identity> GetActualIdentitiesById(Guid rosterId)
        {
            return this.Children.Where(x => x.Identity.Id == rosterId).Select(x => x.Identity).ToList();
        }

        public RosterVector RosterVector => this.Identity.RosterVector;

        void IInternalInterviewTreeNode.SetParent(IInterviewTreeNode parent)
        {
            this.Parent = parent;
            this.Parents = this.GetParents(parent).Reverse();
        }

        private IEnumerable<IInterviewTreeNode> GetParents(IInterviewTreeNode nearestParent)
        {
            while (nearestParent != null)
            {
                yield return nearestParent;
                nearestParent = nearestParent.Parent;
            }
        }

        public void AddChild(IInterviewTreeNode child)
        {
            var internalTreeNode = child as IInternalInterviewTreeNode;
            if (internalTreeNode == null) throw new ArgumentException(nameof(child));

            internalTreeNode.SetTree(this.Tree);
            internalTreeNode.SetParent(this);
            this.children.Add(child);

            Tree?.ProcessAddedNode(child);
        }

        public void AddChildren(IEnumerable<IInterviewTreeNode> nodes)
        {
            nodes.ForEach(this.AddChild);
        }

        public void RemoveChild(Identity identity)
        {
            var nodeToRemove = this.children.Find(child=>child.Identity == identity);
            if (nodeToRemove != null) this.children.Remove(nodeToRemove);

            Tree?.ProcessRemovedNodeByIdentity(identity);
        }

        public bool IsDisabled() => this.isDisabled || (this.Parent?.IsDisabled() ?? false);
        public void Disable() => this.isDisabled = true;
        public void Enable() => this.isDisabled = false;

        public InterviewTreeQuestion GetQuestionFromThisOrUpperLevel(Guid questionId)
        {
            for (int i = this.Identity.RosterVector.Length; i >= 0; i--)
            {
                var questionIdentity = new Identity(questionId, this.Identity.RosterVector.Take(i).ToArray());
                var question = this.Tree.GetQuestion(questionIdentity);
                if (question != null)
                    return question;
            }

            return null;
        }

        public bool HasChild(Identity identity)
        {
            return this.Children.Any(x => x.Identity.Equals(identity));
        }

        public virtual IInterviewTreeNode Clone()
        {
            var clonedInterviewTreeGroup = (InterviewTreeGroup) this.MemberwiseClone();
            clonedInterviewTreeGroup.Tree = null;
            clonedInterviewTreeGroup.children = new List<IInterviewTreeNode>();
            var clonedChildren = this.Children.Select(n => n.Clone()).ToList();
            clonedInterviewTreeGroup.AddChildren(clonedChildren);
            clonedInterviewTreeGroup.Title = this.Title?.Clone();
            return clonedInterviewTreeGroup;
        }

        public void ReplaceSubstitutions()
        {
            this.Title.ReplaceSubstitutions();
        }
    }

    public class InterviewTreeSubSection : InterviewTreeGroup
    {
        public InterviewTreeSubSection(Identity identity, SubstitionText title, IEnumerable<QuestionnaireItemReference> childrenReferences) 
            : base(identity, title, childrenReferences)
        {
        }

        public override string ToString() => $"SubSection ({this.Identity})" + Environment.NewLine + string.Join(Environment.NewLine, this.Children.Select(child => child.ToString().PrefixEachLine("  ")));
    }

    public class InterviewTreeSection : InterviewTreeGroup
    {
        public InterviewTreeSection(Identity identity, SubstitionText title, IEnumerable<QuestionnaireItemReference> childrenReferences) : base(identity, title, childrenReferences)
        {
        }

        public override string ToString() => $"Section ({this.Identity})" + Environment.NewLine + string.Join(Environment.NewLine, this.Children.Select(child => child.ToString().PrefixEachLine("  ")));
    }
}
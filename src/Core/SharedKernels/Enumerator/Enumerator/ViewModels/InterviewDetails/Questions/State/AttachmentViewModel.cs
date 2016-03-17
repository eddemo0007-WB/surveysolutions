﻿using System;
using System.Linq;
using MvvmCross.Core.ViewModels;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Views.BinaryData;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Views;
using WB.Core.SharedKernels.SurveySolutions.Documents;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State
{
    public class AttachmentViewModel : MvxNotifyPropertyChanged
    {
        private readonly IPlainQuestionnaireRepository questionnaireRepository;
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IAttachmentContentStorage attachmentContentStorage;

        private AttachmentContentMetadata attachmentContentMetadata;

        public AttachmentViewModel(
            IPlainQuestionnaireRepository questionnaireRepository,
            IStatefulInterviewRepository interviewRepository,
            IAttachmentContentStorage attachmentContentStorage)
        {
            this.questionnaireRepository = questionnaireRepository;
            this.interviewRepository = interviewRepository;
            this.attachmentContentStorage = attachmentContentStorage;
        }


        public async void Init(string interviewId, Identity entityIdentity)
        {
            if (interviewId == null) throw new ArgumentNullException(nameof(interviewId));
            if (entityIdentity == null) throw new ArgumentNullException(nameof(entityIdentity));

            var interview = this.interviewRepository.Get(interviewId);
            IQuestionnaire questionnaire = this.questionnaireRepository.GetQuestionnaire(interview.QuestionnaireIdentity);

            var attachment = questionnaire.GetAttachmentForEntity(entityIdentity.Id);

            if (attachment != null)
            {
                this.attachmentContentMetadata = await this.attachmentContentStorage.GetMetadataAsync(attachment.ContentId);

                if (IsImage)
                {
                    this.Content = await this.attachmentContentStorage.GetContentAsync(attachment.ContentId);
                }
            }
        }

        private readonly string[] imageContentTypes = {"image/png", "image/jpg", "image/gif", "image/jpeg", "image/pjpeg"};

        public bool IsImage => this.attachmentContentMetadata != null && this.imageContentTypes.Contains(this.attachmentContentMetadata.ContentType);

        public byte[] Content { get; private set; }
    }
}
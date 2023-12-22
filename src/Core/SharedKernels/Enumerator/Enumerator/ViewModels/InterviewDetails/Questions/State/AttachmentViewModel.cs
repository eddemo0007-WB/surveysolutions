﻿using System;
using System.Threading.Tasks;
using MvvmCross.Commands;
using WB.Core.GenericSubdomains.Portable.Tasks;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Views;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State
{
    public class AttachmentViewModel : BaseViewModel,
        IAsyncViewModelEventHandler<VariablesChanged>
    {
        private readonly IQuestionnaireStorage questionnaireRepository;
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IViewModelEventRegistry eventRegistry;
        private readonly IAttachmentContentStorage attachmentContentStorage;
        private readonly IInterviewPdfService pdfService;
        private readonly IViewModelNavigationService viewModelNavigationService;

        private AttachmentContentMetadata attachmentContentMetadata;
        private string interviewId;
        private Guid? attachmentId;
        public Identity Identity { get; private set; }

        public string Tag => "attachment_" + Identity;

        private const string ImageMimeType = "image/";
        private const string VideoMimeType = "video/";
        private const string AudioMimeType = "audio/";
        private const string PdfMimeType = "application/pdf";

        private bool supportPreview = false;

        public AttachmentViewModel(
            IQuestionnaireStorage questionnaireRepository,
            IStatefulInterviewRepository interviewRepository,
            IViewModelEventRegistry eventRegistry,
            IAttachmentContentStorage attachmentContentStorage,
            IInterviewPdfService pdfService,
            IViewModelNavigationService viewModelNavigationService)
        {
            this.questionnaireRepository = questionnaireRepository;
            this.interviewRepository = interviewRepository;
            this.eventRegistry = eventRegistry;
            this.attachmentContentStorage = attachmentContentStorage;
            this.pdfService = pdfService;
            this.viewModelNavigationService = viewModelNavigationService;
        }

        public void Init(string interviewId, Identity entityIdentity, NavigationState navigationState)
        {
            this.interviewId = interviewId ?? throw new ArgumentNullException(nameof(interviewId));
            this.Identity = entityIdentity ?? throw new ArgumentNullException(nameof(entityIdentity));

            this.eventRegistry.Subscribe(this, interviewId);
            BindAttachment().WaitAndUnwrapException();
            this.supportPreview = true;
        }

        public void InitAsStatic(string interviewId, string attachmentName, bool supportPreview = true)
        {
            if (attachmentName == null)
            {
                this.BindNoAttachment().WaitAndUnwrapException();
                return;
            }

            this.interviewId = interviewId ?? throw new ArgumentNullException(nameof(interviewId));
            
            BindAttachment(attachmentName).WaitAndUnwrapException();
            this.supportPreview = supportPreview;
        }

        public IMvxAsyncCommand ShowPhotoView => new MvxAsyncCommand(async () =>
        {
            await this.viewModelNavigationService.NavigateToAsync<PhotoViewViewModel, PhotoViewViewModelArgs>(
                new PhotoViewViewModelArgs
                {
                    InterviewId = Guid.Parse(this.interviewId),
                    AttachmentId = this.attachmentId
                });
        }, () => this.supportPreview);


        private Task BindAttachment(string attachmentName)
        {
            var interview = this.interviewRepository.GetOrThrow(interviewId);
            var questionnaire = this.questionnaireRepository.GetQuestionnaireOrThrow(interview.QuestionnaireIdentity, interview.Language);
            var newAttachment = questionnaire.GetAttachmentIdByName(attachmentName);
            return BindAttachment(newAttachment);
        }

        private Task BindAttachment()
        {
            var interview = this.interviewRepository.GetOrThrow(interviewId);
            var newAttachment = interview.GetAttachmentForEntity(Identity);
            return BindAttachment(newAttachment);
        }

        private async Task BindAttachment(Guid? newAttachment)
        {
            if (newAttachment == null)
            {
                await BindNoAttachment();
                return;
            }
            
            if (this.attachmentId != newAttachment)
            {
                this.attachmentId = newAttachment;
                var interview = this.interviewRepository.GetOrThrow(interviewId);
                IQuestionnaire questionnaire = this.questionnaireRepository.GetQuestionnaireOrThrow(interview.QuestionnaireIdentity, interview.Language);
                var attachment = questionnaire.GetAttachmentById(this.attachmentId.Value);
                
                this.attachmentContentMetadata = this.attachmentContentStorage.GetMetadata(attachment.ContentId);

                if (IsImage)
                {
                    this.Image = await this.attachmentContentStorage.GetPreviewContentAsync(attachment.ContentId);
                }
                
                await RaiseAllPropertiesChanged();
            }
        }

        private async Task BindNoAttachment()
        {
            this.attachmentId = null;
            this.attachmentContentMetadata = null;
            await RaiseAllPropertiesChanged();
        }

        public byte[] Image { get; private set; }
        
        public bool IsImage => this.attachmentContentMetadata != null
                               && this.attachmentContentMetadata.ContentType.StartsWith(ImageMimeType,
                                   StringComparison.OrdinalIgnoreCase);

        public bool IsVideo => this.attachmentContentMetadata != null
                               && this.attachmentContentMetadata.ContentType.StartsWith(VideoMimeType,
                                   StringComparison.OrdinalIgnoreCase);

        public bool IsAudio => this.attachmentContentMetadata != null
                               && this.attachmentContentMetadata.ContentType.StartsWith(AudioMimeType,
                                   StringComparison.OrdinalIgnoreCase);

        public bool IsPdf => this.attachmentContentMetadata != null
                             && this.attachmentContentMetadata.ContentType.StartsWith(PdfMimeType,
                                 StringComparison.OrdinalIgnoreCase);

        public string ShowTitle
        {
            get
            {
                if (IsPdf)
                    return UIResources.Interview_ShowPdf;
                if (IsVideo)
                    return UIResources.Interview_PlayVideo;
                if (IsAudio)
                    return UIResources.Interview_PlayAudio;
                return string.Empty;
            }
        }
        
        public IMvxAsyncCommand ShowAttachment => new MvxAsyncCommand(OpenAttachmentAsync);
        
        private async Task OpenAttachmentAsync()
        {
            if (IsPdf)
            {
                if (this.attachmentId.HasValue)
                    await pdfService.OpenAttachmentAsync(interviewId, this.attachmentId.Value);
                else
                    await pdfService.OpenAsync(interviewId, this.Identity);
            }

            if (IsVideo)
            {
                await viewModelNavigationService.NavigateToAsync<PlayVideoViewModel, PlayMediaViewModelArgs>(
                    new PlayMediaViewModelArgs(interviewId, this.attachmentId.Value));
            }

            if (IsAudio)
            {
                await viewModelNavigationService.NavigateToAsync<PlayAudioViewModel, PlayMediaViewModelArgs>(
                    new PlayMediaViewModelArgs(interviewId, this.attachmentId.Value));
            }
        }

        public IMvxAsyncCommand ShowPdf => new MvxAsyncCommand(OpenPdfAsync);

        private async Task OpenPdfAsync()
        {
            if (this.attachmentId.HasValue)
                await pdfService.OpenAttachmentAsync(interviewId, this.attachmentId.Value);
            else
                await pdfService.OpenAsync(interviewId, this.Identity);
        }
        
        public override void ViewDestroy(bool viewFinishing = true)
        {
            this.Image = null;
            this.eventRegistry.Unsubscribe(this);
            
            base.ViewDestroy(viewFinishing);
        }

        public async Task HandleAsync(VariablesChanged @event)
        {
            await BindAttachment();
        }
    }
}

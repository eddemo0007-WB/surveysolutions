using System.Configuration.Provider;
using System.Web;
using Main.Core.Documents;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.UI.Designer.Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using ICSharpCode.SharpZipLib.Zip;
using Lucene.Net.Search;
using Newtonsoft.Json;
using WB.Core.BoundedContexts.Designer.Implementation.Services.AttachmentService;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Account;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.SharedKernels.SurveySolutions.Documents;
using WB.UI.Designer.BootstrapSupport.HtmlHelpers;
using WB.UI.Designer.Extensions;
using WB.UI.Designer.Models;
using WB.UI.Shared.Web.Extensions;
using WB.UI.Shared.Web.Membership;
using WB.UI.Shared.Web.MembershipProvider.Roles;
using WebMatrix.WebData;

namespace WB.UI.Designer.Controllers
{
    [CustomAuthorize(Roles = "Administrator")]
    public class AdminController : BaseController
    {
        private class RestoreState
        {
            public class Attachment
            {
                public string FileName { get; set; }
                public string ContentType { get; set; }
                public byte[] BinaryContent { get; set; }

                public bool HasAllDataForRestore()
                    => this.FileName != null
                    && this.ContentType != null
                    && this.BinaryContent != null;
            }

            private readonly Dictionary<Guid, Attachment> attachments = new Dictionary<Guid, Attachment>(); 

            public int RestoredEntitiesCount { get; set; }

            public void StoreAttachmentContentType(Guid attachmentId, string contentType)
            {
                this.attachments.GetOrAdd(attachmentId).ContentType = contentType;
            }

            public void StoreAttachmentFile(Guid attachmentId, string fileName, byte[] binaryContent)
            {
                this.attachments.GetOrAdd(attachmentId).FileName = fileName;
                this.attachments.GetOrAdd(attachmentId).BinaryContent = binaryContent;
            }

            public Attachment GetAttachment(Guid attachmentId)
                => this.attachments[attachmentId];

            public void RemoveAttachment(Guid attachmentId)
                => this.attachments.Remove(attachmentId);

            public IEnumerable<Guid> GetPendingAttachments()
                => this.attachments.Keys;
        }

        private readonly IQuestionnaireHelper questionnaireHelper;
        private readonly ILogger logger;
        private readonly IStringCompressor zipUtils;
        private readonly ISerializer serializer;
        private readonly ICommandService commandService;
        private readonly IViewFactory<QuestionnaireViewInputModel, QuestionnaireView> questionnaireViewFactory;
        private readonly IViewFactory<AccountListViewInputModel, AccountListView> accountListViewFactory;
        private readonly ILookupTableService lookupTableService;
        private readonly IAttachmentService attachmentService;

        public AdminController(
            IMembershipUserService userHelper,
            IQuestionnaireHelper questionnaireHelper,
            ILogger logger,
            IStringCompressor zipUtils,
            ICommandService commandService,
            IViewFactory<QuestionnaireViewInputModel, QuestionnaireView> questionnaireViewFactory,
            ISerializer serializer, 
            IViewFactory<AccountListViewInputModel, AccountListView> accountListViewFactory, 
            ILookupTableService lookupTableService,
            IAttachmentService attachmentService)
            : base(userHelper)
        {
            this.questionnaireHelper = questionnaireHelper;
            this.logger = logger;
            this.zipUtils = zipUtils;
            this.commandService = commandService;
            this.questionnaireViewFactory = questionnaireViewFactory;
            this.serializer = serializer;
            this.accountListViewFactory = accountListViewFactory;
            this.lookupTableService = lookupTableService;
            this.attachmentService = attachmentService;
        }

        [HttpGet]
        public ActionResult Import()
        {
            return this.View();
        }

        [HttpPost]
        public ActionResult Import(HttpPostedFileBase uploadFile)
        {
            uploadFile = uploadFile ?? this.Request.Files[0];

            if (uploadFile != null && uploadFile.ContentLength > 0)
            {
                try
                {
                    var document =
                        this.zipUtils.DecompressGZip<QuestionnaireDocumentWithLookUpTables>(CreateStreamCopy(uploadFile.InputStream));
                    if (document != null)
                    {
                        this.commandService.Execute(new ImportQuestionnaire(this.UserHelper.WebUser.UserId,
                            document.QuestionnaireDocument));
                        foreach (var lookupTable in document.LookupTables)
                        {
                            this.lookupTableService.SaveLookupTableContent(document.QuestionnaireDocument.PublicKey,
                                lookupTable.Key,
                                lookupTable.Value);
                        }
                        return this.RedirectToAction("Index", "Questionnaire");
                    }
                }
                catch (JsonSerializationException)
                {
                    var document = this.zipUtils.DecompressGZip<QuestionnaireDocument>(CreateStreamCopy(uploadFile.InputStream));
                    if (document != null)
                    {
                        this.commandService.Execute(new ImportQuestionnaire(this.UserHelper.WebUser.UserId, document));
                        return this.RedirectToAction("Index", "Questionnaire");
                    }
                }
            }
            else
            {
                this.Error("Uploaded file is empty");
            }

            return this.Import();
        }

        [HttpGet]
        public ActionResult RestoreQuestionnaire()
        {
            return this.View();
        }

        [HttpPost]
        public ActionResult RestoreQuestionnaire(HttpPostedFileBase uploadFile)
        {
            try
            {
                uploadFile = uploadFile ?? this.Request.Files[0];

                var areFileNameAndContentAvailable = uploadFile?.FileName != null && uploadFile?.ContentLength > 0;
                if (!areFileNameAndContentAvailable)
                {
                    this.Error("Uploaded file is not specified or empty.");
                    return this.View();
                }

                if (uploadFile.FileName.ToLower().EndsWith(".tmpl"))
                {
                    this.Error("You are trying to restore old format questionnaire. Please use 'Old Format Restore' option.");
                    return this.View();
                }

                if (!uploadFile.FileName.ToLower().EndsWith(".zip"))
                {
                    this.Error("Only zip archives are supported. Please upload correct zip backup.");
                    return this.View();
                }

                var state = new RestoreState();

                using (var zipStream = new ZipInputStream(uploadFile.InputStream))
                {
                    ZipEntry zipEntry = zipStream.GetNextEntry();

                    while (zipEntry != null)
                    {
                        this.RestoreDataFromZipFileEntry(zipEntry, zipStream, state);

                        zipEntry = zipStream.GetNextEntry();
                    }
                }

                foreach (Guid attachmentId in state.GetPendingAttachments())
                {
                    this.Error($"Attachment '{attachmentId.FormatGuid()}' was not restored because there are not enough data for it in it's folder.", append: true);
                }

                this.Success($"Restore finished. Restored {state.RestoredEntitiesCount} entitites. See messages above for details.", append: true);

                return this.View();
            }
            catch (Exception exception)
            {
                this.logger.Error("Unexpected error occurred during restore of questionnaire from backup.", exception);
                this.Error($"Unexpected error occurred.{Environment.NewLine}{exception}", append: true);
                return this.View();
            }
        }

        private void RestoreDataFromZipFileEntry(ZipEntry zipEntry, ZipInputStream zipStream, RestoreState state)
        {
            try
            {
                if (!zipEntry.IsFile)
                    return;

                string[] zipEntryPathChunks = zipEntry.Name.Split(
                    new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries);

                Guid questionnaireId;
                bool isInsideQuestionnaireFolder = Guid.TryParse(
                    zipEntryPathChunks[0].Split(new[] {'(', ')'}, StringSplitOptions.RemoveEmptyEntries).Last(),
                    out questionnaireId);

                if (!isInsideQuestionnaireFolder)
                {
                    this.Info($"Ignored zip file entry '{zipEntry.Name}' because it is not under questionnaire folder. Top-level folder should contain questionnaire ID which will be used for restore.", append: true);
                    return;
                }

                bool isQuestionnaireDocumentEntry =
                    zipEntryPathChunks.Length == 2 &&
                    zipEntryPathChunks[1].ToLower().EndsWith(".json");

                bool isAttachmentEntry =
                    zipEntryPathChunks.Length == 4 &&
                    zipEntryPathChunks[1].ToLower() == "attachments";

                bool isLookupTableEntry =
                    zipEntryPathChunks.Length == 3 &&
                    zipEntryPathChunks[1].ToLower() == "lookup tables" &&
                    zipEntryPathChunks[2].ToLower().EndsWith(".txt");

                if (isQuestionnaireDocumentEntry)
                {
                    string textContent = new StreamReader(zipStream, Encoding.UTF8).ReadToEnd();

                    var questionnaireDocument = this.serializer.Deserialize<QuestionnaireDocument>(textContent);
                    questionnaireDocument.PublicKey = questionnaireId;

                    this.commandService.Execute(new ImportQuestionnaire(this.UserHelper.WebUser.UserId, questionnaireDocument));

                    this.Success($"[{zipEntry.Name}]", append: true);
                    this.Success($"    Restored questionnaire document '{questionnaireDocument.Title}' with id '{questionnaireDocument.PublicKey.FormatGuid()}'.", append: true);
                    state.RestoredEntitiesCount++;
                }
                else if (isAttachmentEntry)
                {
                    var attachmentId = Guid.Parse(zipEntryPathChunks[2]);
                    var fileName = zipEntryPathChunks[3];

                    if (fileName.ToLower() == "content-type.txt")
                    {
                        string textContent = new StreamReader(zipStream, Encoding.UTF8).ReadToEnd();

                        state.StoreAttachmentContentType(attachmentId, textContent);
                        this.Success($"[{zipEntry.Name}]", append: true);
                        this.Success($"    Found content-type '{textContent}' for attachment '{attachmentId.FormatGuid()}'.", append: true);
                    }
                    else
                    {
                        byte[] binaryContent = zipStream.ReadToEnd();

                        state.StoreAttachmentFile(attachmentId, fileName, binaryContent);
                        this.Success($"[{zipEntry.Name}]", append: true);
                        this.Success($"    Found file data '{fileName}' for attachment '{attachmentId.FormatGuid()}'.", append: true);
                    }

                    var attachment = state.GetAttachment(attachmentId);

                    if (attachment.HasAllDataForRestore())
                    {
                        string attachmentContentId = this.attachmentService.CreateAttachmentContentId(attachment.BinaryContent);

                        this.attachmentService.SaveContent(attachmentContentId, attachment.ContentType, attachment.BinaryContent);
                        this.attachmentService.SaveMeta(attachmentId, questionnaireId, attachmentContentId, attachment.FileName);

                        state.RemoveAttachment(attachmentId);

                        this.Success($"    Restored attachment '{attachmentId.FormatGuid()}' for questionnaire '{questionnaireId.FormatGuid()}' using file '{attachment.FileName}' and content-type '{attachment.ContentType}'.", append: true);
                        state.RestoredEntitiesCount++;
                    }
                }
                else if (isLookupTableEntry)
                {
                    var lookupTableId = Guid.Parse(Path.GetFileNameWithoutExtension(zipEntryPathChunks[2]));

                    string textContent = new StreamReader(zipStream, Encoding.UTF8).ReadToEnd();

                    this.lookupTableService.SaveLookupTableContent(questionnaireId, lookupTableId, textContent);

                    this.Success($"[{zipEntry.Name}].", append: true);
                    this.Success($"    Restored lookup table '{lookupTableId.FormatGuid()}' for questionnaire '{questionnaireId.FormatGuid()}'.", append: true);
                    state.RestoredEntitiesCount++;
                }
                else
                {
                    this.Info($"Ignored unknown zip file entry '{zipEntry.Name}'.", append: true);
                }
            }
            catch (Exception exception)
            {
                this.logger.Warn($"Error processing zip file entry '{zipEntry.Name}' during questionnaire restore from backup.", exception);
                this.Error($"Error processing zip file entry '{zipEntry.Name}'.{Environment.NewLine}{exception}", append: true);
            }
        }

        private MemoryStream CreateStreamCopy(Stream originalStream)
        {
            MemoryStream copyStream = new MemoryStream();
            originalStream.Position = 0;
            originalStream.CopyTo(copyStream);
            copyStream.Position = 0;
            return copyStream;
        }

        [HttpGet]
        [Obsolete("Remove after 5.9+ version released")]
        public FileStreamResult Export(Guid id)
        {
            var questionnaireView = questionnaireViewFactory.Load(new QuestionnaireViewInputModel(id));
            if (questionnaireView == null)
                return null;

            var questionnaireDocumentWithLookUpTables = new QuestionnaireDocumentWithLookUpTables()
            {
                QuestionnaireDocument = questionnaireView.Source,
                LookupTables = this.lookupTableService.GetQuestionnairesLookupTables(id)
            };
            return new FileStreamResult(this.zipUtils.Compress(this.serializer.Serialize(questionnaireDocumentWithLookUpTables)), "application/octet-stream")
            {
                FileDownloadName = string.Format("{0}.tmpl", questionnaireView.Title.ToValidFileName())
            };
        }

        [HttpGet]
        public FileStreamResult BackupQuestionnaire(Guid id)
        {
            var questionnaireView = questionnaireViewFactory.Load(new QuestionnaireViewInputModel(id));
            if (questionnaireView == null)
                return null;

            var questionnaireDocument = questionnaireView.Source;

            string questionnaireJson = this.serializer.Serialize(questionnaireDocument);

            var output = new MemoryStream();

            ZipOutputStream zipStream = new ZipOutputStream(output);

            string questionnaireFolderName = $"{questionnaireView.Title.ToValidFileName()} ({id.FormatGuid()})";

            zipStream.PutTextFileEntry($"{questionnaireFolderName}/{questionnaireView.Title.ToValidFileName()}.json", questionnaireJson);

            for (int attachmentIndex = 0; attachmentIndex < questionnaireDocument.Attachments.Count; attachmentIndex++)
            {
                try
                {
                    Attachment attachmentReference = questionnaireDocument.Attachments[attachmentIndex];
                    
                    var attachmentContent = this.attachmentService.GetContent(attachmentReference.ContentId);

                    if (attachmentContent?.Content != null)
                    {
                        var attachmentMeta = this.attachmentService.GetAttachmentMeta(attachmentReference.AttachmentId);

                        zipStream.PutFileEntry($"{questionnaireFolderName}/Attachments/{attachmentReference.AttachmentId.FormatGuid()}/{attachmentMeta?.FileName ?? "unknown-file-name"}", attachmentContent.Content);
                        zipStream.PutTextFileEntry($"{questionnaireFolderName}/Attachments/{attachmentReference.AttachmentId.FormatGuid()}/Content-Type.txt", attachmentContent.ContentType);
                    }
                    else
                    {
                        zipStream.PutTextFileEntry(
                            $"{questionnaireFolderName}/Attachments/Invalid/missing attachment #{attachmentIndex + 1} ({attachmentReference.AttachmentId.FormatGuid()}).txt",
                            $"Attachment '{attachmentReference.Name}' is missing.");
                    }
                }
                catch (Exception exception)
                {
                    this.logger.Warn($"Failed to backup attachment #{attachmentIndex + 1} from questionnaire '{questionnaireView.Title}' ({questionnaireFolderName}).", exception);
                    zipStream.PutTextFileEntry(
                        $"{questionnaireFolderName}/Attachments/Invalid/broken attachment #{attachmentIndex + 1}.txt",
                        $"Failed to backup attachment. See error below.{Environment.NewLine}{exception}");
                }
            }

            Dictionary<Guid, string> lookupTables = this.lookupTableService.GetQuestionnairesLookupTables(id);

            foreach (KeyValuePair<Guid, string> lookupTable in lookupTables)
            {
                zipStream.PutTextFileEntry($"{questionnaireFolderName}/Lookup Tables/{lookupTable.Key.FormatGuid()}.txt", lookupTable.Value);
            }

            zipStream.Finish();

            output.Seek(0, SeekOrigin.Begin);

            return new FileStreamResult(output, "application/zip")
            {
                FileDownloadName = $"{questionnaireView.Title.ToValidFileName()}.zip"
            };
        }

        public ActionResult Create()
        {
            return this.View(new RegisterModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RegisterModel model)
        {
            if (this.ModelState.IsValid)
            {
                try
                {
                    WebSecurity.CreateUserAndAccount(model.UserName, model.Password, new { model.Email }, false);
                    Roles.Provider.AddUsersToRoles(new[] { model.UserName }, new[] { this.UserHelper.USERROLENAME });

                    return this.RedirectToAction("Index");
                }
                catch (MembershipCreateUserException e)
                {
                    this.Error(e.StatusCode.ToErrorCode());
                }
            }

            return View(model);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(Guid id)
        {
            MembershipUser user = this.GetUser(id);
            if (user == null)
            {
                this.Error(string.Format("User \"{0}\" doesn't exist", id));
            }
            else
            {
                Membership.DeleteUser(user.UserName);
                this.Success(string.Format("User \"{0}\" successfully deleted", user.UserName));
            }

            return this.RedirectToAction("Index");
        }

        public ViewResult Details(Guid id)
        {
            MembershipUser account = this.GetUser(id);

            var questionnaires = this.questionnaireHelper.GetQuestionnairesByViewerId(viewerId: id,
                    isAdmin: this.UserHelper.WebUser.IsAdmin);
            questionnaires.ToList().ForEach(
                x =>
                    {
                        x.CanEdit = false;
                        x.CanDelete = false;
                    });
            
            return
                this.View(
                    new AccountViewModel
                        {
                            Id = account.ProviderUserKey.AsGuid(),
                            CreationDate = account.CreationDate.ToUIString(),
                            Email = account.Email,
                            IsApproved = account.IsApproved,
                            IsLockedOut = account.IsLockedOut,
                            LastLoginDate = account.LastLoginDate.ToUIString(),
                            UserName = account.UserName,
                            LastLockoutDate = account.LastLockoutDate.ToUIString(),
                            LastPasswordChangedDate = account.LastPasswordChangedDate.ToUIString(),
                            Comment = account.Comment ?? GlobalHelper.EmptyString,
                            Questionnaires = questionnaires
                        });
        }

        public ActionResult Edit(Guid id)
        {
            MembershipUser intUser = this.GetUser(id);
            return
                this.View(
                    new UpdateAccountModel
                        {
                            Email = intUser.Email, 
                            IsApproved = intUser.IsApproved, 
                            IsLockedOut = intUser.IsLockedOut, 
                            UserName = intUser.UserName, 
                            Id = id
                        });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UpdateAccountModel user)
        {
            if (this.ModelState.IsValid)
            {
                MembershipUser intUser = this.GetUser(user.Id);
                if (intUser != null)
                {
                    try
                    {
                        Membership.UpdateUser(
                            new MembershipUser(
                                providerName: intUser.ProviderName,
                                name: intUser.UserName,
                                providerUserKey: intUser.ProviderUserKey,
                                email: user.Email,
                                passwordQuestion: intUser.PasswordQuestion,
                                comment: null,
                                isApproved: user.IsApproved,
                                isLockedOut: user.IsLockedOut,
                                creationDate: intUser.CreationDate,
                                lastLoginDate: intUser.LastLoginDate,
                                lastActivityDate: intUser.LastActivityDate,
                                lastPasswordChangedDate: intUser.LastPasswordChangedDate,
                                lastLockoutDate: intUser.LastLockoutDate));

                        return this.RedirectToAction("Index");
                    }
                    catch (ProviderException e)
                    {
                        this.Error(e.Message);
                    }
                    catch (Exception e)
                    {
                        logger.Error("User update exception", e);
                        this.Error("Could not update user information. Please, try again later.");
                    }
                }
            }

            return this.View(user);
        }

        public ViewResult Index(int? p, string sb, int? so, string f)
        {
            int page = p ?? 1;

            this.ViewBag.PageIndex = p;
            this.ViewBag.SortBy = sb;
            this.ViewBag.Filter = f;
            this.ViewBag.SortOrder = so;

            if (so.ToBool())
            {
                sb = string.Format("{0} Desc", sb);
            }
            var users = accountListViewFactory.Load(new AccountListViewInputModel()
            {
                Filter = f,
                Page = page,
                PageSize = GlobalHelper.GridPageItemsCount,
                Order = sb ?? string.Empty,
            });

            Func<AccountListItem, bool> editAction =
                (user) => !user.SimpleRoles.Contains(SimpleRoleEnum.Administrator);

            IEnumerable<AccountListViewItemModel> retVal =
                users.Items
                     .Select(
                         x =>
                         new AccountListViewItemModel
                             {
                                 Id = x.ProviderUserKey.AsGuid(), 
                                 UserName = x.UserName, 
                                 Email = x.Email, 
                                 CreationDate = x.CreatedAt,
                                 IsApproved = x.IsConfirmed, 
                                 IsLockedOut = x.IsLockedOut, 
                                 CanEdit = editAction(x), 
                                 CanOpen = false,
                                 CanDelete = false, 
                                 CanPreview = editAction(x)
                             });
            return View(retVal.ToPagedList(page, GlobalHelper.GridPageItemsCount, users.TotalCount));
        }

        private MembershipUser GetUser(Guid id)
        {
            return Membership.GetUser(id, false);
        }
    }
}
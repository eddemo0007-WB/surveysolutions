﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Main.Core;
using Main.Core.Commands.Questionnaire.Completed;
using Main.Core.Events;
using Ncqrs;
using Ncqrs.Commanding.ServiceModel;
using Newtonsoft.Json;
using WB.Core.SharedKernel.Structures.Synchronization;
using WB.Core.Synchronization.SyncProvider;

namespace WB.Core.Synchronization.SyncStorage
{
    internal class IncomePackagesRepository : IIncomePackagesRepository
    {
        private readonly string path;
        private const string FolderName = "IncomigData";
        private const string FileExtension = "sync";
    //    private bool inProcess = false;

        public IncomePackagesRepository(string folderPath)
        {
            this.path = Path.Combine(folderPath, FolderName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            /*else
            {
                ProccessStoredItems();
            }*/
        }

        public void StoreIncomingItem(SyncItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Content))
                throw new ArgumentException("Sync Item is not set.");


            File.WriteAllText(GetItemFileName(item.Id), item.Content);

            var meta = GetContentAsItem<InterviewMetaInfo>(PackageHelper.DecompressString(item.MetaInfo));

            NcqrsEnvironment.Get<ICommandService>().Execute(new UpdateInterviewMetaInfoCommand()
                {
                    PublicKey = meta.PublicKey,
                    ResponsibleId = meta.ResponsibleId,
                    StatusId = meta.Status.Id,
                    TemplateId = meta.TemplateId
                });
            //   Task.Factory.StartNew(() => ProcessItemAsync(item.Id));
        }

        public int GetIncomingItemsCount()
        {
            var incomeDir = new DirectoryInfo(path);
            return
                incomeDir.GetFiles(string.Format("*.{0}", FileExtension)).Count();
        }

        private string GetItemFileName(Guid id)
        {
            return Path.Combine(path, string.Format("{0}.{1}", id, FileExtension));
        }

        protected void ProccessStoredItems()
        {
            var incomeDir = new DirectoryInfo(path);
            var incomingPackages =
                incomeDir.GetFiles(string.Format("*.{0}", FileExtension));
            if(!incomingPackages.Any())
                return;
            
            FileInfo incomingPackage = incomingPackages.First();
            /* foreach (FileInfo incomingPackage in incomingPackages)
             {*/
            var packageId = Guid.Parse(Path.GetFileNameWithoutExtension(incomingPackage.Name));
            Task.Factory.StartNew(() => ProcessItem(packageId));
            //   }
        }

        public void ProcessItem(Guid id)
        {
            var fileName = GetItemFileName(id);
            if (!File.Exists(fileName))
                return;

            var fileContent = File.ReadAllText(fileName);

            var items = GetContentAsItem<AggregateRootEvent[]>(fileContent);
            var processor = new SyncEventHandler();

            //could be slow
            //think about deffered handling

            processor.Process(items);

            File.Delete(fileName);
            
            ProccessStoredItems();
        }

        private T GetContentAsItem<T>(string syncItemContent)
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
            var item = JsonConvert.DeserializeObject<T>(PackageHelper.DecompressString(syncItemContent)
              /*  syncItem.IsCompressed ?
                PackageHelper.DecompressString(syncItem.Content) :
                syncItem.Content*/,
                settings);

            return item;
        }
    }
}

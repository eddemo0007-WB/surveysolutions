﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Main.Core;
using Newtonsoft.Json;

namespace WB.Core.Synchronization.SyncStorage
{
    public class FileChunkStorage : IChunkStorage
    {
        private readonly string path;
        private const string FileExtension = "sync";
        private long currentSequence = 1;
        private readonly object myLock = new object();

        public FileChunkStorage(string folderPath, Guid supervisor)
        {
            this.path = Path.Combine(folderPath, supervisor.ToString());
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var alavibleFiles = GetAllFiles();
            if (alavibleFiles.Any())
                currentSequence = GetAllFiles().Max(x => x.Key) + 1;
        }

       
        public void StoreChunk(Guid id, string syncItem)
        {
            lock (myLock)
            {
                File.WriteAllText(GetFilePath(id, currentSequence), syncItem);
                currentSequence++;
            }
            var syncDir = new DirectoryInfo(path);

            var sequences =
                syncDir.GetFiles(string.Format("*-{0}.{1}", id, FileExtension)).OrderByDescending(f => f.LastWriteTime);

            foreach (var sequenceFile in sequences.Skip(1))
            {
                sequenceFile.Delete();
            }
        }

        public string ReadChunk(Guid id)
        {
            var syncDir = new DirectoryInfo(path);
            var sequences =
                syncDir.GetFiles(string.Format("*-{0}.{1}", id, FileExtension))
                       .Select(ExctractSequence)
                       .OrderByDescending(s => s);
            if (!sequences.Any())
                throw new ArgumentException("chunk is absent");

            return File.ReadAllText(GetFilePath(id, sequences.FirstOrDefault()));
        }

        public IEnumerable<Guid> GetChunksCreatedAfter(long sequence)
        {
            var sequences = GetAllFiles().Where(f => f.Key > sequence);
            return sequences.Select(f => f.Value).Distinct().ToList();
        }

        private string GetFilePath(Guid id, long sequence)
        {
            return Path.Combine(this.path, string.Format("{0}-{1}.{2}", sequence, id, FileExtension));
        }

        private IEnumerable<KeyValuePair<long, Guid>> GetAllFiles()
        {
            var syncDir = new DirectoryInfo(path);

            return
                syncDir.GetFiles(string.Format("*.{0}", FileExtension))
                       .ToDictionary(ExctractSequence, ExctractChuncId);
        }

        private Guid ExctractChuncId(FileInfo f)
        {
            var guidStartsIndex = f.Name.IndexOf('-');
            var guidAsString = f.Name.Substring(guidStartsIndex + 1, f.Name.LastIndexOf('.') - guidStartsIndex - 1);
            return Guid.Parse(guidAsString);
        }

        private long ExctractSequence(FileInfo f)
        {
            var sequenceAsString = f.Name.Substring(0, f.Name.IndexOf('-'));
            return long.Parse(sequenceAsString);
        }
    }
}
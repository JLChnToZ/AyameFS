using System;
using System.IO;
using System.Collections.Generic;

namespace AyameFS {
    public abstract class AbstractHandler<THash> where THash : IEquatable<THash> {
        protected readonly string basePath;

        protected AbstractHandler(string basePath) {
            this.basePath = basePath;
            Directory.CreateDirectory(basePath);
        }

        protected abstract THash GetHash(Stream content);

        public abstract IEnumerable<THash> GetDependencies(THash parent);

        public abstract void SetDependency(THash parent, THash child);

        public void Store(THash parent, Stream child) =>
            SetDependency(parent, Store(child));

        public THash Store(Stream content) {
            if(content.CanSeek) {
                long offset = content.Position;
                var hash = GetHash(content);
                content.Seek(content.Position - offset, SeekOrigin.Current);
                StoreTo(content, hash);
                return hash;
            }
            using(var cloneContent = new MemoryStream()) {
                content.CopyTo(cloneContent);
                cloneContent.Seek(0, SeekOrigin.Begin);
                var hash = GetHash(cloneContent);
                cloneContent.Seek(0, SeekOrigin.Begin);
                StoreTo(cloneContent, hash);
                return hash;
            }
        }

        private bool StoreTo(Stream content, THash hash) {
            var path = GetPathForStorage(hash);
            if(File.Exists(path))
                return false;
            using(var fs = File.OpenWrite(path))
                content.CopyTo(fs);
            return true;
        }

        public bool Remove(THash hash) {
            foreach(var dependency in GetDependencies(hash))
                RemoveSingle(dependency);
            return RemoveSingle(hash);
        }

        protected virtual bool RemoveSingle(THash hash) {
            try {
                var path = GetPath(hash);
                if(File.Exists(path)) {
                    File.Delete(path);
                    return true;
                }
            } catch {}
            return false;
        }

        public virtual string GetPath(THash hash) {
            var hashStr = hash.ToString();
            return Path.Combine(basePath, hashStr.Substring(0, 2), hashStr.Substring(2));
        }

        public Stream GetStream(THash hash) => File.OpenRead(GetPath(hash));

        protected virtual string GetPathForStorage(THash hash) {
            var fileName = GetPath(hash);
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            return fileName;
        }

        public virtual bool Exists(THash hash) => File.Exists(GetPath(hash));
    }
}

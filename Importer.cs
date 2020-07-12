using System;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;
using System.Collections.Generic;

namespace AyameFS {
    public interface IFileIterateSource {
        string CurrentPath { get; }

        Stream Stream { get; }

        Stream SeekFile(string path);
    }

    public static class Importer {
        public static void Iterate(string fromPath, Func<IFileIterateSource, bool> callback, bool deleteFile = false) {
            var dirQueue = new Stack<string>();
            var tempDirs = new Stack<DirectoryInfo>();
            dirQueue.Push(fromPath);
            while(dirQueue.Count > 0) {
                var path = dirQueue.Pop();
                var iterate = Directory.GetFileSystemEntries(path);
                foreach(var fullPath in iterate) {
                    if(Directory.Exists(fullPath)) {
                        dirQueue.Push(fullPath);
                        continue;
                    }
                    if(!File.Exists(fullPath)) continue;
                    bool processed = false;
                    var fileName = Path.GetFileName(fullPath);
                    if(fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) {
                        using(var stream = File.OpenRead(fullPath))
                            IterateZipFile(fullPath, stream, callback);
                        processed = true;
                    } else if(fileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)) {
                        using(var stream = File.OpenRead(fullPath))
                        using(var zStream = new GZipInputStream(stream))
                        using(var tar = TarArchive.CreateInputTarArchive(stream)) {
                            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                            tempDirs.Push(Directory.CreateDirectory(tempDir));
                            tar.ExtractContents(tempDir);
                            dirQueue.Push(tempDir);
                        }
                        processed = true;
                    } else if(fileName.EndsWith(".tar.bz2", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".tbz2", StringComparison.OrdinalIgnoreCase)) {
                        using(var stream = File.OpenRead(fullPath))
                        using(var zStream = new BZip2InputStream(stream))
                        using(var tar = TarArchive.CreateInputTarArchive(stream)) {
                            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                            tempDirs.Push(Directory.CreateDirectory(tempDir));
                            tar.ExtractContents(tempDir);
                            dirQueue.Push(tempDir);
                        }
                        processed = true;
                    } else if(callback(new FileIterateSource(fullPath)))
                        processed = true;
                    if(processed && deleteFile && File.Exists(fullPath))
                        File.Delete(fullPath);
                }
            }
            foreach(var dir in tempDirs)
                dir.Delete(true);
        }

        public static void IterateZipFile(string path, Stream fromStream, Func<IFileIterateSource, bool> callback) {
            using(var zip = new ZipFile(fromStream)) {
                foreach(ZipEntry entry in zip) {
                    var fullPath = Path.Combine(path, entry.Name);
                    var ext = Path.GetExtension(zip.Name);
                    if(ext.Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
                        using(var stream = zip.GetInputStream(entry))
                            IterateZipFile(fullPath, stream, callback);
                        continue;
                    }
                    callback(new ZipFileIterateSource(fullPath, zip, entry));
                }
            }
        }

        struct FileIterateSource: IFileIterateSource {
            public FileIterateSource(string path) {
                CurrentPath = path;
            }

            public string CurrentPath { get; }

            public Stream Stream =>
                File.OpenRead(CurrentPath);

            public Stream SeekFile(string path) =>
                File.OpenRead(Path.Combine(Path.GetDirectoryName(CurrentPath), path));
        }

        struct ZipFileIterateSource: IFileIterateSource {
            private readonly string basePath;
            private readonly ZipFile zipFile;
            private readonly ZipEntry zipEntry;

            public ZipFileIterateSource(string basePath, ZipFile zipFile, ZipEntry zipEntry) {
                this.basePath = basePath;
                this.zipFile = zipFile;
                this.zipEntry = zipEntry;
            }

            public string CurrentPath => ZipEntry.CleanName(Path.Combine(basePath, zipFile.Name));

            public Stream Stream => zipFile.GetInputStream(zipEntry);

            public Stream SeekFile(string path) {
                var entry = zipFile.GetEntry(ZipEntry.CleanName(Path.Combine(Path.GetDirectoryName(zipEntry.Name), path)));
                return entry != null ? zipFile.GetInputStream(entry) : null;
            }
        }
    }
}

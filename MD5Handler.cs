using System.IO;

namespace AyameFS {
    public abstract class MD5Handler: AbstractHandler<MD5Hash> {
        protected MD5Handler(string basePath): base(basePath) { }

        protected override MD5Hash GetHash(Stream content) =>
            MD5Hash.Compute(content);
    }
}

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using TinyFatFS;

namespace GHIElectronics.TinyCLR.SDCard {
    public static class FileSystem {
        private static readonly IDictionary mounted = new Hashtable();

        const int hdc = 1; //dummy hdc
        
        public static IDriveProvider Mount(string SpiBus,int CsPin, int DummyCsPin)
        {
            if (FileSystem.mounted.Contains(hdc))
                throw new InvalidOperationException("Already mounted");

            FatFileSystem.SetSPIConfig(SpiBus, CsPin, DummyCsPin);

            FatFileSystem.FatFS fs=null;
            FatFileSystem.FileResult res;

            if (fs == null) fs = new FatFileSystem.FatFS();

            res = FatFileSystem.Current.MountDrive(ref fs, "", 1);     /* Give a work area to the default drive */
            res.ThrowIfError();

            Debug.WriteLine("Drive successfully mounted");


            var drive = new NativeDriveProvider(ref fs);

            var provider = DriveInfo.RegisterDriveProvider(drive);

            mounted[hdc] = drive;

            return drive;
        }

        public static bool Unmount()
        {
            try
            {
                if (!FileSystem.mounted.Contains(hdc))
                    throw new InvalidOperationException("Not mounted");

                var drive = (IDriveProvider)FileSystem.mounted[hdc];

                FileSystem.mounted.Remove(hdc);

                DriveInfo.DeregisterDriveProvider(drive);


                Debug.WriteLine("Drive successfully unmounted");
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        private class NativeDriveProvider : IDriveProvider
        {
            static FatFileSystem.FatFS fs;
            static FatFileSystem.FileResult res;
            public NativeDriveProvider(ref FatFileSystem.FatFS fileSystem)
            {
                fs = fileSystem;
            }
            public NativeDriveProvider()
            {
                if (fs == null) fs = new FatFileSystem.FatFS();
            }
            private bool initialized;

            public string Name { get; private set; }

            public DriveType DriveType => DriveType.Removable;

            public string DriveFormat
            {
                get
                {
                    switch (fs.fs_type)
                    {
                        case FatFileSystem.FS_EXFAT:
                            return "EXFAT";
                        case FatFileSystem.FS_FAT12:
                            return "FAT12";
                        case FatFileSystem.FS_FAT16:
                            return "FAT16";
                        case FatFileSystem.FS_FAT32:
                            return "FAT32";
                    }
                    return "";
                }
            }

            public bool IsReady => fs != null;

            public long AvailableFreeSpace {
                get
                {
                    uint fre_clust = 0;
                    uint fre_sect;

                    /* Get volume information and free clusters of drive 1 */
                    res = FatFileSystem.Current.GetFreeSpace("0:", ref fre_clust, ref fs);
                    if (res != TinyFatFS.FatFileSystem.FileResult.Ok)
                    {
                        Debug.WriteLine($"An error occured. {res.ToString()}");
                        return 0;
                    };

                    /* Get total sectors and free sectors */
                    //tot_sect = (fs.n_fatent - 2) * fs.csize;
                    fre_sect = fre_clust * fs.csize;
                    return fre_sect; //KB = fre_sect/2 
                }
            }

            public long TotalFreeSpace
            {
                get
                {
                    uint fre_clust = 0;
                    uint fre_sect;

                    /* Get volume information and free clusters of drive 1 */
                    res = FatFileSystem.Current.GetFreeSpace("0:", ref fre_clust, ref fs);
                    if (res != TinyFatFS.FatFileSystem.FileResult.Ok)
                    {
                        Debug.WriteLine($"An error occured. {res.ToString()}");
                        return 0;
                    };

                    /* Get total sectors and free sectors */
                    //tot_sect = (fs.n_fatent - 2) * fs.csize;
                    fre_sect = fre_clust * fs.csize;
                    return fre_sect; //KB = fre_sect/2 
                }
            }

            public long TotalSize
            {

                get
                {
                    uint fre_clust = 0;
                    uint  tot_sect;

                    /* Get volume information and free clusters of drive 1 */
                    res = FatFileSystem.Current.GetFreeSpace("0:", ref fre_clust, ref fs);
                    if (res != TinyFatFS.FatFileSystem.FileResult.Ok)
                    {
                        Debug.WriteLine($"An error occured. {res.ToString()}");
                        return 0;
                    };

                    /* Get total sectors and free sectors */
                    tot_sect = (fs.n_fatent - 2) * fs.csize;
                    //fre_sect = fre_clust * fs.csize;
                    return tot_sect; //KB = tot_sect/2 
                }

            }

            public string VolumeLabel => string.Empty;

            

            public void CreateDirectory(string path)
            {
                res = FatFileSystem.Current.CreateDirectory("sub1");
                if (res != TinyFatFS.FatFileSystem.FileResult.Exists) res.ThrowIfError();

            }

            public void Delete(string path)
            {
                res = FatFileSystem.Current.DeleteFileOrDirectory("/sub1/File2.txt");     /* Give a work area to the default drive */
                res.ThrowIfError();

            }

            public IFileSystemEntryFinder Find(string path, string searchPattern)
            {
                throw new NotImplementedException();
            }

            public FileAttributes GetAttributes(string path)
            {
                throw new NotImplementedException();
            }

            public FileSystemEntry GetFileSystemEntry(string path)
            {
                throw new NotImplementedException();
            }

            public void Initialize(string name)
            {
                if (this.initialized) throw new InvalidOperationException();

                this.initialized = true;
                this.Name = name;
            }

            public bool Move(string source, string destination)
            {
                throw new NotImplementedException();
            }

            public IFileStream OpenFile(string path, int bufferSize)
            {
                throw new NotImplementedException();
            }

            public void SetAttributes(string path, FileAttributes attributes)
            {
                throw new NotImplementedException();
            }
        }
        /*
        public static IDriveProvider Mount(IntPtr hdc) {
            if (FileSystem.mounted.Contains(hdc))
                throw new InvalidOperationException("Already mounted");

            var drive = new NativeDriveProvider();

            var provider = DriveInfo.RegisterDriveProvider(drive);

            FileSystem.Initialize(hdc, provider.Name);

            mounted[hdc] = drive;

            return drive;
        }

        public static bool Unmount(IntPtr hdc) {
            if (!FileSystem.mounted.Contains(hdc))
                throw new InvalidOperationException("Not mounted");

            var drive = (IDriveProvider)FileSystem.mounted[hdc];

            FileSystem.mounted.Remove(hdc);

            DriveInfo.DeregisterDriveProvider(drive);

            return FileSystem.Uninitialize(hdc);
        }

        public static void Flush(IntPtr hdc) => FileSystem.FlushAll(hdc);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern static void FlushAll(IntPtr nativeProvider);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern static void Initialize(IntPtr nativeProvider, string name);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern static bool Uninitialize(IntPtr nativeProvider);

        private class NativeDriveProvider : IDriveProvider {
            private bool initialized;

            public extern DriveType DriveType { [MethodImpl(MethodImplOptions.InternalCall)] get; }

            public extern string DriveFormat { [MethodImpl(MethodImplOptions.InternalCall)] get; }

            public extern bool IsReady { [MethodImpl(MethodImplOptions.InternalCall)] get; }

            public extern long AvailableFreeSpace { [MethodImpl(MethodImplOptions.InternalCall)] get; }

            public extern long TotalFreeSpace { [MethodImpl(MethodImplOptions.InternalCall)] get; }

            public extern long TotalSize { [MethodImpl(MethodImplOptions.InternalCall)] get; }

            public extern string VolumeLabel { [MethodImpl(MethodImplOptions.InternalCall)] get; }

            public string Name { get; private set; }

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern void CreateDirectory(string path);

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern void Delete(string path);

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern FileAttributes GetAttributes(string path);

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern FileSystemEntry GetFileSystemEntry(string path);

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern bool Move(string source, string destination);

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern void SetAttributes(string path, FileAttributes attributes);

            public IFileSystemEntryFinder Find(string path, string searchPattern) => new NativeFileSystemEntryFinder(path, searchPattern);

            public IFileStream OpenFile(string path, int bufferSize) => new NativeFileStream(path, bufferSize);

            public void Initialize(string name) {
                if (this.initialized) throw new InvalidOperationException();

                this.initialized = true;
                this.Name = name;
            }
        }

        private class NativeFileStream : IFileStream, IDisposable {
#pragma warning disable CS0169
            private IntPtr impl;

#pragma warning restore CS0169
            ~NativeFileStream() => this.Dispose();

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern NativeFileStream(string path, int bufferSize);

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern void Dispose();

            public extern bool CanWrite { [MethodImpl(MethodImplOptions.InternalCall)] get; }

            public extern bool CanRead { [MethodImpl(MethodImplOptions.InternalCall)] get; }

            public extern bool CanSeek { [MethodImpl(MethodImplOptions.InternalCall)] get; }

            public extern long Length { [MethodImpl(MethodImplOptions.InternalCall)] get; [MethodImpl(MethodImplOptions.InternalCall)] set; }

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern void Close();

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern void Flush();

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern int Read(byte[] buffer, int offset, int count, TimeSpan timeout);

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern long Seek(long offset, SeekOrigin origin);

            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern int Write(byte[] buffer, int offset, int count, TimeSpan timeout);
        }*/
    }
}

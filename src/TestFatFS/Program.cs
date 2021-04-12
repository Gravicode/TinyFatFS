using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Storage;
using GHIElectronics.TinyCLR.IO;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using TinyFatFS;
using static TinyFatFS.FatFileSystem;

namespace TestFatFS
{
    class Program
    {
        // c# port of FatFs: http://elm-chan.org/fsw/ff/00index_e.html
        
        static uint bw = 0;
        static FileResult res;
        static FatFS fs;
        static FileObject Fil;

        static void AccessSD()
        {
            var sd = StorageController.FromName(GHIElectronics.TinyCLR.Pins.SC20260.StorageController.SdCard);
            var drive = FileSystem.Mount(sd.Hdc);

            //Show a list of files in the root directory
            var directory = new DirectoryInfo(drive.Name);
            var files = directory.GetFiles();

            foreach (var f in files)
            {
                System.Diagnostics.Debug.WriteLine(f.Name);
            }

            //Create a text file and save it to the SD card.
            var file = new FileStream($@"{drive.Name}Test.txt", FileMode.OpenOrCreate);
            var bytes = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString() +
                Environment.NewLine);

            file.Write(bytes, 0, bytes.Length);

            file.Flush();

            FileSystem.Flush(sd.Hdc);
        }
        static void Main()
        {
            //ini spi configuration first for accessing SDCard controller
            //change this spi config for SC13XXX
            //AccessSD();
            //return;
            SetSPIConfig(GHIElectronics.TinyCLR.Pins.SC20260.SpiBus.Spi2, GHIElectronics.TinyCLR.Pins.SC20260.GpioPin.PA13, GHIElectronics.TinyCLR.Pins.SC20100.GpioPin.PC13);

            fs = new FatFS();        /* FatFs work area needed for each volume, constructor : chipselect pin */
            Fil = new FileObject();           /* File object needed for each open file */

            Debug.WriteLine("Start");
            GpioPin led = GpioController.GetDefault().OpenPin(
            GHIElectronics.TinyCLR.Pins.SC20260.GpioPin.PH6); //led
            led.SetDriveMode(GpioPinDriveMode.Output);
            led.Write(GpioPinValue.Low);


            try
            {
                MountDrive();
                DeleteFileExample();
                CreateDirectoriesExample();
                CreateFileExample();
                ReadFileExample();
                RenameFileExample();
                FileExistsExample();
                ListDirectoryExample();

                GetFreeSpaceExample();

                Debug.WriteLine("Done");
                while (true)
                {
                    led.Write(GpioPinValue.High);
                    Thread.Sleep(100);
                    led.Write(GpioPinValue.Low);
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        static void MountDrive()
        {
            res = FatFileSystem.Current.MountDrive(ref fs, "", 1);     /* Give a work area to the default drive */
            res.ThrowIfError();

            Debug.WriteLine("Drive successfully mounted");
        }

        static void CreateFileExample()
        {


            if ((res = FatFileSystem.Current.OpenFile(ref Fil, "/sub1/File1.txt", FA_WRITE | FA_CREATE_ALWAYS)) == FatFileSystem.FileResult.Ok)
            {   /* Create a file */
                Random rnd = new Random();
                var payload = $"File contents is: It works ({rnd.Next()})!".ToByteArray();
                res = FatFileSystem.Current.WriteFile(ref Fil, payload, (uint)payload.Length, ref bw);    /* Write data to the file */
                res.ThrowIfError();

                res = FatFileSystem.Current.CloseFile(ref Fil);   /* Close the file */
                res.ThrowIfError();
            }
            else
            {
                res.ThrowIfError();
            }

            Debug.WriteLine("File successfully created");
        }

        static void ReadFileExample()
        {

            if (FatFileSystem.Current.OpenFile(ref Fil, "/sub1/File1.txt", FA_READ) == FatFileSystem.FileResult.Ok)
            {   /* Create a file */

                var newPayload = new byte[5000];
                res = FatFileSystem.Current.ReadFile(ref Fil, ref newPayload, 5000, ref bw);    /* Read data from file */
                res.ThrowIfError();

                var msg = Encoding.UTF8.GetString(newPayload, 0, (int)bw);
                Debug.WriteLine($"{msg}");

                res = FatFileSystem.Current.CloseFile(ref Fil);                              /* Close the file */
                res.ThrowIfError();
            }

            Debug.WriteLine("File successfully read");
        }

        static void DeleteFileExample()
        {
            res = FatFileSystem.Current.DeleteFileOrDirectory("/sub1/File2.txt");     /* Give a work area to the default drive */
            res.ThrowIfError();

            Debug.WriteLine("File successfully deleted");
        }

        static void ListDirectoryExample()
        {


            res = FatFileSystem.Current.MountDrive(ref fs, "", 1);
            res.ThrowIfError();

            res = Scan_Files("/");
            res.ThrowIfError();

            Debug.WriteLine("Directories successfully listed");
        }

        private static FileResult Scan_Files(string path)
        {
            FileResult res;
            FatFileSystem.FileInfo fno = new FatFileSystem.FileInfo();
            FatFileSystem.DirectoryObject dir = new FatFileSystem.DirectoryObject();
            byte[] buff = new byte[256];
            buff = path.ToNullTerminatedByteArray();

            res = FatFileSystem.Current.OpenDirectory(ref dir, buff);                      /* Open the directory */
            if (res == FileResult.Ok)
            {
                for (; ; )
                {
                    res = FatFileSystem.Current.ReadDirectoryEntry(ref dir, ref fno);           /* Read a directory item */
                    if (res != FileResult.Ok || fno.fileName[0] == 0) break;   /* Break on error or end of dir */
                    if ((fno.fileAttribute & AM_DIR) > 0 && !((fno.fileAttribute & AM_SYS) > 0 || (fno.fileAttribute & AM_HID) > 0))
                    {
                        /* It is a directory */
                        var newpath = path + "/" + fno.fileName.ToStringNullTerminationRemoved();
                        Debug.WriteLine($"Directory: {path}/{fno.fileName.ToStringNullTerminationRemoved()}");
                        res = Scan_Files(newpath);                    /* Enter the directory */
                        if (res != FileResult.Ok) break;
                    }
                    else
                    {
                        /* It is a file. */
                        Debug.WriteLine($"File: {path}/{fno.fileName.ToStringNullTerminationRemoved()}");
                    }
                }
                FatFileSystem.Current.CloseDirectory(ref dir);
            }

            return res;
        }

        static void CreateDirectoriesExample()
        {

            res = FatFileSystem.Current.CreateDirectory("sub1");
            if (res != FileResult.Exists) res.ThrowIfError();


            res = FatFileSystem.Current.CreateDirectory("sub1/sub2");
            if (res != FileResult.Exists) res.ThrowIfError();

            res = FatFileSystem.Current.CreateDirectory("sub1/sub2/sub3");
            if (res != FileResult.Exists) res.ThrowIfError();

            Debug.WriteLine("Directories successfully created");
        }

        static void FileExistsExample()
        {

            FatFileSystem.FileInfo fno = new FatFileSystem.FileInfo();

            res = FatFileSystem.Current.GetFileStatus("/sub1/File2.txt", ref fno);
            switch (res)
            {

                case FatFileSystem.FileResult.Ok:
                    Debug.WriteLine($"Size: {fno.fileSize}");
                    Debug.WriteLine(String.Format("Timestamp: {0}/{1}/{2}, {3}:{4}",
                           (fno.fileDate >> 9) + 1980, fno.fileDate >> 5 & 15, fno.fileDate & 31,
                           fno.fileTime >> 11, fno.fileTime >> 5 & 63));
                    Debug.WriteLine(String.Format("Attributes: {0}{1}{2}{3}{4}",
                           (fno.fileAttribute & AM_DIR) > 0 ? 'D' : '-',
                           (fno.fileAttribute & AM_RDO) > 0 ? 'R' : '-',
                           (fno.fileAttribute & AM_HID) > 0 ? 'H' : '-',
                           (fno.fileAttribute & AM_SYS) > 0 ? 'S' : '-',
                           (fno.fileAttribute & AM_ARC) > 0 ? 'A' : '-'));
                    break;

                case FatFileSystem.FileResult.NoFileExist:
                    Debug.WriteLine("File does not exist");
                    break;

                default:
                    Debug.WriteLine($"An error occured. {res.ToString()}");
                    break;
            }
        }

        static void GetFreeSpaceExample()
        {

            uint fre_clust = 0;
            uint fre_sect, tot_sect;

            /* Get volume information and free clusters of drive 1 */
            res = FatFileSystem.Current.GetFreeSpace("0:", ref fre_clust, ref fs);
            if (res != FileResult.Ok)
            {
                Debug.WriteLine($"An error occured. {res.ToString()}");
                return;
            };

            /* Get total sectors and free sectors */
            tot_sect = (fs.n_fatent - 2) * fs.csize;
            fre_sect = fre_clust * fs.csize;

            /* Print the free space (assuming 512 bytes/sector) */
            Debug.WriteLine(String.Format("{0} KB total drive space\n{1} KB available", tot_sect / 2, fre_sect / 2));
        }

        static void RenameFileExample()
        {
            /* Rename an object in the default drive */
            res = FatFileSystem.Current.RenameFileOrDirectory("/sub1/File1.txt", "/sub1/File2.txt");
            res.ThrowIfError();

            Debug.WriteLine("File successfully renamed");
        }
    }
}

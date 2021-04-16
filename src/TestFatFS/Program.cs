using GHIElectronics.TinyCLR.Devices.Gpio;
//using GHIElectronics.TinyCLR.Devices.Storage;
//using GHIElectronics.TinyCLR.IO;
using GHIElectronics.TinyCLR.SDCard;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
//using TinyFatFS;
//using static TinyFatFS.FatFileSystem;

namespace TestFatFS
{
    class Program
    {
        static void Main()
        {
            TestNewApi();
            Thread.Sleep(-1);
            //TestOldApi();
        }

        static void TestNewApi()
        {
            var drive = FileSystem.Mount(GHIElectronics.TinyCLR.Pins.SC20260.SpiBus.Spi2, GHIElectronics.TinyCLR.Pins.SC20260.GpioPin.PA13, GHIElectronics.TinyCLR.Pins.SC20100.GpioPin.PC13);

            //Show a list of files in the root directory
            var directory = new DirectoryInfo(drive.Name);

            var files = directory.GetFiles();

            foreach (var f in files)
            {
                System.Diagnostics.Debug.WriteLine(f.Name);
            }

            //Create a text file and save it to the SD card.
            var file = new FileStream($@"{drive.Name}Test1.txt", FileMode.OpenOrCreate);
            var bytes = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString() +
                Environment.NewLine);

            file.Write(bytes, 0, bytes.Length);

            file.Flush();

            //FileSystem.Flush();
        }
        
        
    }
}

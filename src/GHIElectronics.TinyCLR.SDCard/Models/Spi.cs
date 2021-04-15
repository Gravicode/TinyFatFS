using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;
using System.Diagnostics;

namespace TinyFatFS
{
    static class Spi
    {
        static SpiDevice device = null;

        /* usi.S: Initialize MMC control ports */
        public static void InitSpi()
        {
            if (device == null)
            {
                
                var cs = GpioController.GetDefault().OpenPin(FatFileSystem.DummyChipSelectPin);//DUMMY_CS_PIN_NUM

                var settings = new SpiConnectionSettings()
                {
                    ChipSelectType = SpiChipSelectType.Gpio,
                    ChipSelectLine = cs,
                    Mode = SpiMode.Mode0,
                    ClockFrequency = 15_000_000,
                };
              
                var controller = SpiController.FromName(FatFileSystem.SpiBusName);
                device = controller.GetDevice(settings);
                /*
                var settings = new SpiConnectionSettings(DUMMY_CS_PIN_NUM)   // The slave's select pin. Not used. CS is controlled by by GPIO pin
                {
                    Mode = SpiMode.Mode0,
                    ClockFrequency = 15 * 1000 * 1000,       //15 Mhz
                    DataBitLength = 8,
                };
                device = SpiDevice.FromId(FEZ.SpiBus.Spi1, settings);
                */
                Debug.WriteLine("Spi device successfully created");
            }

        }

        /* usi.S: Send a byte to the MMC */
        public static void TransmitSpi(byte d)
        {
            byte[] writeBuffer = { d };
            device.Write(writeBuffer);
        }

        /* usi.S: Send a 0xFF to the MMC and get the received byte */
        public static byte ReceiveSpi()
        {
            byte[] writeBuffer = { 0xff };
            byte[] readBuffer = { 0x00 };

            device.TransferFullDuplex(writeBuffer, readBuffer);
            return readBuffer[0];
        }

    }
}

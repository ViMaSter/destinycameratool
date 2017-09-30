using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

using System.Runtime.InteropServices;

namespace ps4remotehack
{
    // namespace Import
    // {
    //     [StructLayout(LayoutKind.Sequential, Pack = 0)]
    //     public struct LIST_ENTRY
    //     {
    //         public IntPtr Flink;
    //         public IntPtr Blink;
    // 
    //         public ListEntryWrapper Fwd
    //         {
    //             get
    //             {
    //                 var fwdAddr = Flink.ToInt32();
    //                 return new ListEntryWrapper()
    //                 {
    //                     Header = Flink.ReadMemory<LIST_ENTRY>(),
    //                     Body = new IntPtr(fwdAddr + Marshal.SizeOf(typeof(LIST_ENTRY))).ReadMemory<LDR_MODULE>()
    //                 };
    //             }
    //         }
    //         public ListEntryWrapper Back
    //         {
    //             get
    //             {
    //                 var fwdAddr = Blink.ToInt32();
    //                 return new ListEntryWrapper()
    //                 {
    //                     Header = Flink.ReadMemory<LIST_ENTRY>(),
    //                     Body = new IntPtr(fwdAddr + Marshal.SizeOf(typeof(LIST_ENTRY))).ReadMemory<LDR_MODULE>()
    //                 };
    //             }
    //         }
    //     }
    // 
    //     [StructLayout(LayoutKind.Sequential, Pack = 0)]
    //     public struct ListEntryWrapper
    //     {
    //         public LIST_ENTRY Header;
    //         public LDR_MODULE Body;
    //     }
    // 
    //     [StructLayout(LayoutKind.Sequential)]
    //     public struct UNICODE_STRING : IDisposable
    //     {
    //         public ushort Length;
    //         public ushort MaximumLength;
    //         private IntPtr buffer;
    // 
    //         public UNICODE_STRING(string s)
    //         {
    //             Length = (ushort)(s.Length * 2);
    //             MaximumLength = (ushort)(Length + 2);
    //             buffer = Marshal.StringToHGlobalUni(s);
    //         }
    // 
    //         public void Dispose()
    //         {
    //             Marshal.FreeHGlobal(buffer);
    //             buffer = IntPtr.Zero;
    //         }
    // 
    //         public override string ToString()
    //         {
    //             return Marshal.PtrToStringUni(buffer);
    //         }
    //     }
    // 
    //     [StructLayout(LayoutKind.Sequential, Pack = 0)]
    //     public struct PEB_LDR_DATA
    //     {
    //         public int Length;
    //         public int Initialized;
    //         public int SsHandle;
    //         public IntPtr InLoadOrderModuleListPtr;
    //         public IntPtr InMemoryOrderModuleListPtr;
    //         public IntPtr InInitOrderModuleListPtr;
    //         public int EntryInProgress;
    //         public ListEntryWrapper InLoadOrderModuleList { get { return InLoadOrderModuleListPtr.ReadMemory<ListEntryWrapper>(); } }
    //         public ListEntryWrapper InMemoryOrderModuleList { get { return InLoadOrderModuleListPtr.ReadMemory<ListEntryWrapper>(); } }
    //         public ListEntryWrapper InInitOrderModuleList { get { return InLoadOrderModuleListPtr.ReadMemory<ListEntryWrapper>(); } }
    //     }
    // 
    //     [StructLayout(LayoutKind.Sequential, Pack = 0)]
    //     public struct LDR_MODULE
    //     {
    //         LIST_ENTRY InLoadOrderModuleList;
    //         LIST_ENTRY InMemoryOrderModuleList;
    //         LIST_ENTRY InInitializationOrderModuleList;
    //         int BaseAddress;
    //         int EntryPoint;
    //         ulong SizeOfImage;
    //         UNICODE_STRING FullDllName;
    //         UNICODE_STRING BaseDllName;
    //         ulong Flags;
    //         short LoadCount;
    //         short TlsIndex;
    //         LIST_ENTRY HashTableEntry;
    //         ulong TimeDateStamp;
    //     }
    // 
    //     static class Extend {
    //         public static T ReadMemory<T>(this IntPtr atAddress)
    //         {
    //             var ret = (T)Marshal.PtrToStructure(atAddress, typeof(T));
    //             return ret;
    //         }
    //     }
    // 
    //     public class Import
    //     {
    //         [DllImport("kernel32.dll")]
    //         public static extern IntPtr ReadProcessMemory(
    //             IntPtr hProcess,
    //             IntPtr lpBaseAddress,
    //             out IntPtr lpBuffer,
    //             int nSize,
    //             out int[] lpNumberOfBytesRead
    //         );
    //     }
    // }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    // PS4 Input Memory allocated for:
    // b: button
    // l: leftstick
    // r: rightstick
    // o: UNKNWON
    // bb bb bO OO LL LL RR RR
    // analogue stick in order:
    // left/right followed by up/down
    public class PS4Input
    {
        public const long DPadUp          = 0x00000010;
        public const long DPadRight       = 0x00000020;
        public const long DPadDown        = 0x00000040;
        public const long DPadLeft        = 0x00000080;
        public const long Share           = 0x00000001;
        public const long LS              = 0x00000002;
        public const long RS              = 0x00000004;
        public const long Options         = 0x00000008;
        public const long Triangle        = 0x00001000;
        public const long Circle          = 0x00002000;
        public const long X               = 0x00004000;
        public const long Square          = 0x00008000;
        public const long L2              = 0x00000100;
        public const long R2              = 0x00000200;
        public const long L1              = 0x00000400;
        public const long R1              = 0x00000800;
        public const long Touchpad        = 0x00100000;

        public long Buttons;
        public int LSX = 0x7D;
        public int LSY = 0x7D;
        public int RSX = 0x7D;
        public int RSY = 0x7D;

        public void ToByte(ref byte[] memory)
        {
            byte[] bButtons = BitConverter.GetBytes((long)Buttons);
            byte[] bLSX = BitConverter.GetBytes(LSX);
            byte[] bLSY = BitConverter.GetBytes(LSY);
            byte[] bRSX = BitConverter.GetBytes(RSX);
            byte[] bRSY = BitConverter.GetBytes(RSY);

            memory = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            memory[0]   = bButtons[0];
            memory[1]   = bButtons[1];
            memory[2]   = bButtons[2];
            memory[3]   = bButtons[3];
            memory[4]   = bLSX[0];
            memory[5]   = bLSY[0];
            memory[6]  = bRSX[0];
            memory[7]  = bRSY[0];
        }

    }

    namespace Import
    {
        public class Helper
        {
            public const int PROCESS_VM_WRITE = 0x0020;
            public const int PROCESS_VM_OPERATION = 0x0008;

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);
        }
    }

    public partial class MainWindow : Window
    {
        int relese = 0;
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        PS4Input state;
        public MainWindow()
        {
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Send);
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            state = new PS4Input();

            InitializeComponent();
        }

        public void DoMagic(PS4Input state)
        {
            // get procees + module base adress
            Process process = Process.GetProcessesByName("RemotePlay")[0];
            IntPtr baseAdress = process.Modules.Cast<ProcessModule>().FirstOrDefault(m => m.ModuleName == "RpCtrlWrapper.dll").BaseAddress;

            // contains the final adresses
            List<IntPtr> convertedOffsets = new List<IntPtr>();

            // grab XML from clipboard
            XElement root = null;
            try
            {
                root = XElement.Parse(Clipboard.GetText());
            }
            catch (System.Xml.XmlException e)
            {
                Debug.WriteLine("No valid XML in Clipboard!");
            }

            // convert clipboard to adresses
            IEnumerable<string> adressStrings = from seg in root.Descendants("Address") select (string)seg;
            foreach (string adressString in adressStrings)
            {
                if (adressString.Contains("RpCtrlWrapper.dll+"))
                {
                    string rawAdressStr = adressString.Replace("RpCtrlWrapper.dll" + "+", "");
                    long rawAdressNmbr = long.Parse(rawAdressStr, System.Globalization.NumberStyles.HexNumber);
                    convertedOffsets.Add(new IntPtr(baseAdress.ToInt64() + rawAdressNmbr));
                }
            }

            IntPtr processHandle = Import.Helper.OpenProcess(Import.Helper.PROCESS_VM_WRITE | Import.Helper.PROCESS_VM_OPERATION, false, process.Id);

            byte[] content = { };
            state.ToByte(ref content);

            int bytesWritten = 0;
            foreach (IntPtr address in convertedOffsets)
            {
                Import.Helper.WriteProcessMemory((int)processHandle, address.ToInt32(), content, content.Length, ref bytesWritten);
            }
            Console.ReadLine();
        }

        private void ButtonX_Up(object sender, RoutedEventArgs e)
        {
            state.Buttons &= ~PS4Input.X;
            DoMagic(state);
        }

        private void ButtonX_Down(object sender, RoutedEventArgs e)
        {
            state.Buttons |= PS4Input.X;
            DoMagic(state);
        }

        private void Square_Up(object sender, RoutedEventArgs e)
        {
            state.Buttons &= ~PS4Input.Square;
            DoMagic(state);
        }

        private void Square_Down(object sender, RoutedEventArgs e)
        {
            state.Buttons |= PS4Input.Square;
            DoMagic(state);
        }

        private void Triangle_Up(object sender, RoutedEventArgs e)
        {
            state.Buttons &= ~PS4Input.Triangle;
            DoMagic(state);
        }

        private void Triangle_Down(object sender, RoutedEventArgs e)
        {
            state.Buttons |= PS4Input.Triangle;
            DoMagic(state);
        }





        private void LX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            state.LSX = (int)this.LX.Value;
            DoMagic(state);
        }

        private void LY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            state.LSY = 255 - (int)this.LY.Value;
            DoMagic(state);
        }

        private void RX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            state.RSX = (int)this.RX.Value;
            DoMagic(state);
        }

        private void RY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            state.RSY = 255 - (int)this.RY.Value;
            DoMagic(state);
        }

        private void LX_Reset(object sender, RoutedEventArgs e)
        {
            state.LSX = (int)128;
            this.LX.Value = (int)128;
            DoMagic(state);
        }

        private void LY_Reset(object sender, RoutedEventArgs e)
        {
            state.LSY = 255 - (int)128;
            this.LY.Value = (int)128;
            DoMagic(state);
        }

        private void RX_Reset(object sender, RoutedEventArgs e)
        {
            state.RSX = (int)128;
            this.RX.Value = (int)128;
            DoMagic(state);
        }

        private void RY_Reset(object sender, RoutedEventArgs e)
        {
            state.RSY = 255 - (int)128;
            this.RY.Value = (int)128;
            DoMagic(state);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)this.NoWeapons.IsChecked)
            {
                dispatcherTimer.Start();
            }
            else
            {
                dispatcherTimer.Stop();
            }
        }

        private async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            this.ButtonX.Content = "down";
            state.Buttons |= PS4Input.Triangle;
            DoMagic(state);
            await Task.Delay(relese);

            this.ButtonX.Content = "up";
            state.Buttons &= ~PS4Input.Triangle;
            DoMagic(state);

        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)timesli.Value);
        }

        private void Slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            relese = (int)timesli_Copy.Value;
        }
    }
}

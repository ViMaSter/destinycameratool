using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
#region PS4 Remoteplay memory
    // PS4 Input Memory allocated for:
    // b: button
    // l: leftstick
    // r: rightstick
    // o: UNKNWON
    // bb bb bO OO LL LL RR RR L2 R2
    //
    // analogue sticks are ordered:
    // 1 byte left/right followed by 1 byte up/down
    // L2 and R2 are not implemented but technically follow right after in memory
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

            memory = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            memory[0]   = bButtons[0];
            memory[1]   = bButtons[1];
            memory[2]   = bButtons[2];
            memory[3]   = bButtons[3];
            memory[4]   = BitConverter.GetBytes(LSX)[0];
            memory[5]   = BitConverter.GetBytes(LSY)[0];
            memory[6]   = BitConverter.GetBytes(RSX)[0];
            memory[7]   = BitConverter.GetBytes(RSY)[0];
        }
    }
#endregion

#region Window data
    public class WindowData : INotifyPropertyChanged
    {
        private int _LX = 128;
        public int LX
        {
            get
            {
                return _LX;
            }
            set
            {
                if (_LX != value)
                {
                    _LX = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _LY = 128;
        public int LY
        {
            get
            {
                return _LY;
            }
            set
            {
                if (_LY != value)
                {
                    _LY = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _RX = 128;
        public int RX
        {
            get
            {
                return _RX;
            }
            set
            {
                if (_RX != value)
                {
                    _RX = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _RY = 128;
        public int RY
        {
            get
            {
                return _RY;
            }
            set
            {
                if (_RY != value)
                {
                    _RY = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _PressOffset = 133;
        public int PressOffset
        {
            get
            {
                return _PressOffset;
            }
            set
            {
                if (_PressOffset != value)
                {
                    _PressOffset = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _ReleaseOffset = 3;
        public int ReleaseOffset
        {
            get
            {
                return _ReleaseOffset;
            }
            set
            {
                if (_ReleaseOffset != value)
                {
                    _ReleaseOffset = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _ToolActive = false;
        public bool ToolActive
        {
            get
            {
                return _ToolActive;
            }
            set
            {
                if (_ToolActive != value)
                {
                    _ToolActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion

#region Native methods
    namespace Import
    {
        public class Helper
        {
            public const int NOP = 0x90;

            public const int PROCESS_VM_READ = 0x0010;
            public const int PROCESS_VM_WRITE = 0x0020;
            public const int PROCESS_VM_OPERATION = 0x0008;

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        }
    }
#endregion

    public partial class MainWindow : Window
    {
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        PS4Input regularState;
        PS4Input autoMoveState;

        WindowData windowMapping = new WindowData();

        Process remotePlayProcess = null;
        IntPtr remotePlayProcessHandle = new IntPtr();
        IntPtr rpCtrlWrapperBaseAdress = new IntPtr();

        public struct ReplaceableAdress
        {
            /// <summary>
            /// Absolute adress or relative offset to a value we want to copy and later override
            /// </summary>
            public int adress;
            /// <summary>
            /// Denotes whether or not ::adress is a relative or absolute adress
            /// </summary>
            public bool relativeToDLL;
            /// <summary>
            /// How many bytes we read or write
            /// </summary>
            public int bytesRequired;
            /// <summary>
            /// Bytes we read from the original codebase
            /// </summary>
            public byte[] bytes;

            public ReplaceableAdress(int _adress, bool _relativeToDLL, int _bytesRequired)
            {
                adress = _adress;
                relativeToDLL = _relativeToDLL;
                bytesRequired = _bytesRequired;
                bytes = new byte[bytesRequired];
            }
        }

        /// <summary>
        /// List of adresses we need to replace
        /// 
        /// These adresses contain instructions that copy the gamepad input
        /// into a buffer that is sent to the PS4. We need to replace these with
        /// NOP-instructions so we can freely modify the buffer on our own.
        /// 
        /// We copy the instructions at first so we can replace it if needed, but also
        /// restore it if we want to return control to the user.
        /// </summary>
        List<ReplaceableAdress> writeToAdresses = new List<ReplaceableAdress>{
            new ReplaceableAdress(0x0020DDBB, true, 5),
            new ReplaceableAdress(0x0020DDD5, true, 3),
            new ReplaceableAdress(0x0020DDD5, true, 3),
            new ReplaceableAdress(0x0020DDDC, true, 3),
            new ReplaceableAdress(0x0020DDE3, true, 3),
            new ReplaceableAdress(0x0020DDEA, true, 3),
            new ReplaceableAdress(0x0020DDF1, true, 3)
        };

        /// <summary>
        /// Key: Base adresses for all buffers sent to the PS4 over the network.
        /// This is an incredibly broad approach, as we simply replce all the buffers and don't
        /// care about why they are all there in the first place.
        /// (Someone feel free to enlighten me here.)
        /// </summary>
        List<int> offsets = new List<int> {
            0x002EBB94,
            0x002EBC1C,
            0x002EBCA4,
            0x002EBD2C,
            0x002EBDB4,
            0x002EBE3C,
            0x002EBEC4,
            0x002EBF4C,
            0x002EBFD4,
            0x002EC05C,
            0x002EC0E4,
            0x002EC16C,
            0x002EC1F4,
            0x002EC27C,
            0x002EC304,
            0x002EC38C,
            0x002EC414,
            0x002EC49C,
            0x002EC524,
            0x002EC5AC,
            0x002EC634,
            0x002EC6BC,
            0x002EC744,
            0x002EC7CC,
            0x002EC854,
            0x002EC8DC,
            0x002EC964,
            0x002EC9EC,
            0x002ECA74,
            0x002ECAFC,
            0x002ECB84,
            0x002ECC0C,
            0x002ECC94,
            0x002ECD1C,
            0x002ECDA4,
            0x002ECE2C,
            0x002ECEB4,
            0x002ECF3C,
            0x002ECFC4,
            0x002ED04C,
            0x002ED0D4,
            0x002ED15C,
            0x002ED1E4,
            0x002ED26C,
            0x002ED2F4,
            0x002ED37C,
            0x002ED404,
            0x002ED48C,
            0x002ED514,
            0x002ED59C,
            0x002ED624,
            0x002ED6AC,
            0x002ED734,
            0x002ED7BC,
            0x002ED844,
            0x002ED8CC,
            0x002ED954,
            0x002ED9DC,
            0x002EDA64,
            0x002EDAEC,
            0x002EDB74,
            0x002EDBFC,
            0x002EDC84,
            0x002EDD0C
        };
        /// <summary>
        /// We convert the relative adresses to absolute adresses at runtime, but only need to do this once.
        /// </summary>
        List<int> convertedOffsets = new List<int>();

        /// <summary>
        /// This tool offers two different setups; one used when the triangle-button is auto-pressed
        /// and one if it's not.
        /// </summary>
        /// <returns></returns>
        ref PS4Input GetState()
        {
            if (this.NoWeapons == null || this.NoWeapons.IsChecked == null)
            {
                return ref regularState;
            }

            if ((bool)this.NoWeapons.IsChecked)
            {
                return ref autoMoveState;
            }
            else
            {
                return ref regularState;
            }
        }

        public MainWindow()
        {
            this.DataContext = windowMapping;

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Send);
            dispatcherTimer.Tick += DispatcherTimer_Tick;

            regularState = new PS4Input();
            autoMoveState = new PS4Input();

            this.windowMapping.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                GetState().LSX = this.windowMapping.LX;
                GetState().LSY = this.windowMapping.LY;
                GetState().RSX = this.windowMapping.RX;
                GetState().RSY = this.windowMapping.RY;

                GetState().RSX = this.windowMapping.RX;
                GetState().RSY = this.windowMapping.RY;

                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)this.windowMapping.PressOffset);

                WriteStateToMemory(GetState());
            };

            InitialSetup();
            InitializeComponent();
        }

        void InitialSetup()
        {
            remotePlayProcess = Process.GetProcessesByName("RemotePlay")[0];
            remotePlayProcessHandle = Import.Helper.OpenProcess(Import.Helper.PROCESS_VM_WRITE | Import.Helper.PROCESS_VM_READ | Import.Helper.PROCESS_VM_OPERATION, false, remotePlayProcess.Id);

            // get procees + module base adress
            rpCtrlWrapperBaseAdress = remotePlayProcess.Modules.Cast<ProcessModule>().FirstOrDefault(m => m.ModuleName == "RpCtrlWrapper.dll").BaseAddress;

            // setup absolute memory adresses
            foreach (int adress in offsets)
            {
                convertedOffsets.Add(adress + rpCtrlWrapperBaseAdress.ToInt32());
            }

            ReadMemory();
        }

#region Take/grant control from gamepad
        void ReadMemory()
        {
            foreach (ReplaceableAdress replaceableAdress in writeToAdresses)
            {
                int bytesRead = -1;
                int adressToUse = replaceableAdress.adress;
                if (replaceableAdress.relativeToDLL)
                {
                    adressToUse += rpCtrlWrapperBaseAdress.ToInt32();
                }

                Import.Helper.ReadProcessMemory((int)remotePlayProcessHandle, adressToUse, replaceableAdress.bytes, replaceableAdress.bytesRequired, ref bytesRead);
            }
        }

        /// <summary>
        /// We can either restore the gamepad functionality once we
        /// overwrote it or let the tool take control of the input.
        /// </summary>
        enum WriteDirective
        {
            RestoreGamepad,
            TakeControl
        }

        void OverrideMemory(WriteDirective directive)
        {
            foreach (ReplaceableAdress replaceableAdress in writeToAdresses)
            {
                int bytesWritten = -1;
                byte[] byteArray;
                if (directive == WriteDirective.RestoreGamepad)
                {
                    if (replaceableAdress.bytes[0] == 0x00)
                    {
                        // if don't have bytes we could write, skip this memory location
                        continue;
                    }
                    byteArray = replaceableAdress.bytes;
                }
                else
                {
                    byteArray = new byte[replaceableAdress.bytesRequired];
                    for (int i = 0; i < replaceableAdress.bytesRequired; i++)
                    {
                        byteArray[i] = Import.Helper.NOP;
                    }
                }

                int adressToUse = replaceableAdress.adress;
                if (replaceableAdress.relativeToDLL)
                {
                    adressToUse += rpCtrlWrapperBaseAdress.ToInt32();
                }

                Import.Helper.WriteProcessMemory((int)remotePlayProcessHandle, adressToUse, byteArray, replaceableAdress.bytesRequired, ref bytesWritten);
            }
        }
#endregion

#region Forward tool-input to PS4
        public void WriteStateToMemory(PS4Input state)
        {
            if (remotePlayProcess == null || remotePlayProcessHandle == new IntPtr())
            {
                return;
            }

            byte[] content = { };
            state.ToByte(ref content);

            int bytesWritten = 0;
            foreach (int address in convertedOffsets)
            {
                Import.Helper.WriteProcessMemory((int)remotePlayProcessHandle, address, content, content.Length, ref bytesWritten);
            }
            Console.ReadLine();
        }

        private async void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            GetState().Buttons |= PS4Input.Triangle;
            WriteStateToMemory(GetState());
            await Task.Delay(windowMapping.ReleaseOffset);

            GetState().Buttons &= ~PS4Input.Triangle;
            WriteStateToMemory(GetState());
        }
#endregion

#region WPF related
        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            int result = -1;
            if (!Int32.TryParse(((TextBox)sender).Text + e.Text, out result))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = !Enumerable.Range(0, 256).Contains(result);
            }
        }
        
        private void LX_Reset(object sender, RoutedEventArgs e)
        {
            this.windowMapping.LX = (int)128;
        }

        private void LY_Reset(object sender, RoutedEventArgs e)
        {
            this.windowMapping.LY = (int)128;
        }

        private void RX_Reset(object sender, RoutedEventArgs e)
        {
            this.windowMapping.RX = (int)128;
        }

        private void RY_Reset(object sender, RoutedEventArgs e)
        {
            this.windowMapping.RY = (int)128;
        }

        private void EnableTool_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)this.EnableTool.IsChecked)
            {
                OverrideMemory(WriteDirective.TakeControl);
            }
            else
            {
                OverrideMemory(WriteDirective.RestoreGamepad);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            OverrideMemory(WriteDirective.RestoreGamepad);
        }

        private void HideWeapon_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)this.NoWeapons.IsChecked)
            {
                this.windowMapping.LX = GetState().LSX;
                this.windowMapping.RX = GetState().RSX;
                this.windowMapping.LY = GetState().LSY;
                this.windowMapping.RY = GetState().RSY;
                WriteStateToMemory(GetState());

                dispatcherTimer.Start();
            }
            else
            {
                this.windowMapping.LX = GetState().LSX;
                this.windowMapping.RX = GetState().RSX;
                this.windowMapping.LY = GetState().LSY;
                this.windowMapping.RY = GetState().RSY;
                WriteStateToMemory(GetState());

                dispatcherTimer.Stop();
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)windowMapping.PressOffset);
        }
#endregion
    }
}

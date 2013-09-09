using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace SleepPreventer
{
    public class Win32API
    {
		[DllImport("kernel32.dll")]
		public static extern
            uint SetThreadExecutionState(uint esFlags);

		public const uint ES_AWAYMODE_REQUIRED = 0x00000040;
		public const uint ES_CONTINUOUS = 0x80000000;
		public const uint ES_DISPLAY_REQUIRED = 0x00000002;
		public const uint ES_SYSTEM_REQUIRED = 0x00000001;
		public const uint ES_USER_PRESENT = 0x00000004;

        //http://msdn.microsoft.com/en-us/library/windows/desktop/aa366730(v=vs.85).aspx
		[DllImport("kernel32.dll")]
		public static extern
            IntPtr LocalFree(IntPtr hMem );

        //http://msdn.microsoft.com/en-us/library/windows/desktop/aa372734(v=vs.85).aspx
		[DllImport("PowrProf.dll")]
        public static extern
            uint PowerReadACValue(
							IntPtr RootPowerKey,
							ref Guid SchemeGuid,
							ref Guid SubGroupOfPowerSettingsGuid,
							ref Guid PowerSettingGuid,
							ref uint Type,
							Byte[] Buffer,
							ref uint BufferSize
						);

        //http://msdn.microsoft.com/en-us/library/windows/desktop/aa372734(v=vs.85).aspx
		[DllImport("PowrProf.dll")]
        public static extern
            uint PowerReadDCValue(
							IntPtr RootPowerKey,
							ref Guid SchemeGuid,
							ref Guid SubGroupOfPowerSettingsGuid,
							ref Guid PowerSettingGuid,
							ref uint Type,
							Byte[] Buffer,
							ref uint BufferSize
						);

        //http://msdn.microsoft.com/en-us/library/windows/desktop/aa372735(v=vs.85).aspx
		[DllImport("PowrProf.dll")]
        public static extern
			uint PowerReadACValueIndex(
							IntPtr RootPowerKey,
							ref Guid SchemeGuid,
							ref Guid SubGroupOfPowerSettingsGuid,
							ref Guid PowerSettingGuid,
							ref uint AcValueIndex
						);

        //http://msdn.microsoft.com/en-us/library/windows/desktop/aa372738(v=vs.85).aspx
		[DllImport("PowrProf.dll")]
        public static extern
            uint PowerReadDCValueIndex(
							IntPtr RootPowerKey,
							ref Guid SchemeGuid,
							ref Guid SubGroupOfPowerSettingsGuid,
							ref Guid PowerSettingGuid,
                            ref uint DcValueIndex
						);

        //http://msdn.microsoft.com/en-us/library/windows/desktop/aa372765(v=vs.85).aspx
		[DllImport("PowrProf.dll")]
		public static extern
            uint PowerWriteACValueIndex(
							IntPtr RootPowerKey,
							ref Guid SchemeGuid,
							ref Guid SubGroupOfPowerSettingsGuid,
							ref Guid PowerSettingGuid,
							uint AcValueIndex
						);

        //http://msdn.microsoft.com/en-us/library/windows/desktop/aa372769(v=vs.85).aspx
		[DllImport("PowrProf.dll")]
		public static extern 
            uint PowerWriteDCValueIndex(
							IntPtr RootPowerKey,
							ref Guid SchemeGuid,
							ref Guid SubGroupOfPowerSettingsGuid,
							ref Guid PowerSettingGuid,
							uint DcValueIndex
						);

        //http://msdn.microsoft.com/en-us/library/windows/desktop/aa372688(v=vs.85).aspx
		[DllImport("PowrProf.dll")]
		public static extern 
            int GetActivePwrScheme(ref uint puiID);

        //http://msdn.microsoft.com/en-us/library/windows/desktop/aa372758(v=vs.85).aspx
		[DllImport("PowrProf.dll")]
		public static extern 
            uint PowerSetActiveScheme(IntPtr UserRootPowerKey, ref Guid SchemeGuid);

		//http://msdn.microsoft.com/en-us/library/windows/desktop/aa372731(v=vs.85).aspx
		//DWORD WINAPI PowerGetActiveScheme( _In_opt_ HKEY UserRootPowerKey, _Out_ GUID **ActivePolicyGuid);
		[DllImport("PowrProf.dll")]
		public static extern 
            uint PowerGetActiveScheme(IntPtr UserRootPowerKey, ref IntPtr p_ActivePolicyGuid);

		//http://msdn.microsoft.com/en-us/library/windows/desktop/aa373196(v=vs.85).aspx
        [DllImport(@"User32", SetLastError=true, EntryPoint = "RegisterPowerSettingNotification",
            CallingConvention = CallingConvention.StdCall)]
		public static extern
			IntPtr RegisterPowerSettingNotification(
							IntPtr hRecipient,
							ref Guid PowerSettingGuid,
							uint Flags
						);
		public const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0;
		public const uint DEVICE_NOTIFY_SERVICE_HANDLE = 1;
        //http://msdn.microsoft.com/en-us/library/windows/desktop/ms683241(v=vs.85).aspx
		public delegate uint HandlerEx(
            uint dwControl, uint dwEventType, IntPtr lpEventData, IntPtr lpContext);
        public const uint SERVICE_CONTROL_POWEREVENT = 0x0000000D;

        public const int WM_POWERBROADCAST = 0x218;
        public const int WM_QUERYENDSESSION = 0x011;
		public const int WM_ENDSESSION = 0X0016;
        //http://msdn.microsoft.com/en-us/library/windows/desktop/aa373237(v=vs.85).aspx
		[DllImport("User32.dll")]
		public static extern
			int	UnregisterPowerSettingNotification(IntPtr Handle);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public uint DataLength;
            public byte Data;
        }

        [DllImport("kernel32.dll")]
		public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
		public static extern IntPtr GetCurrentThread();

        //http://msdn.microsoft.com/en-us/library/windows/desktop/ms724251(v=vs.85).aspx
        [DllImport("kernel32.dll", SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
		  IntPtr hSourceHandle, IntPtr hTargetProcessHandle, ref IntPtr lpTargetHandle,
		  uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

    }
}

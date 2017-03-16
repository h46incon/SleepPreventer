using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;
using System.Resources;

namespace SleepPreventer
{
    public class Public
    {
		public static bool PowerGetActiveSchemeWarp(out Guid guid)
        {
            IntPtr null_ptr = IntPtr.Zero;
			IntPtr p_guid = IntPtr.Zero;
			if (Win32API.PowerGetActiveScheme(null_ptr, ref p_guid) == 0)
			{
                guid = (Guid)Marshal.PtrToStructure(p_guid, typeof(Guid));
                Win32API.LocalFree(p_guid);
                return true;
			}
            else
            {
                guid = new Guid();
                return false;
            }
        }

        static public bool ResetSysIdle(uint option)
        {
            return Win32API.SetThreadExecutionState(option) != 0;
        }

        private IntPtr GetWindowHandle(NotifyIcon notifyIcon)
        {
            if (notifyIcon == null)
            {
                return IntPtr.Zero;
            }

            Type type = notifyIcon.GetType();
            BindingFlags bf = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo fiWindow = type.GetField("window", bf);
            object objWindow = fiWindow.GetValue(notifyIcon);

            type = objWindow.GetType().BaseType;
            FieldInfo fiHandle = type.GetField("handle", bf);
            IntPtr handle = (IntPtr)fiHandle.GetValue(objWindow);
            return handle;
        }

		/// <summary>
		///  Global const variable
		/// </summary>
        public enum LocalStrID
        {
            TITLE,
            EXIT,
            OPTION_NOT_SUPPORT
        }

        public static readonly Dictionary<uint, string> OptNameDic;
        public static readonly Dictionary<LocalStrID, string> LocalStrDic;

        public static Guid GUID_SYSTEM_BUTTON_SUBGROUP = new Guid("4f971e89-eebd-4455-a8de-9e59040e7347");
        public static Guid GUID_LIDCLOSE_ACTION = new Guid("5ca83367-6e45-459f-a27b-476b1d01c936");
        public const uint NO_ACTION_IN_LIDCLOSE_INDEX = 0;

		static Public()
        {
            ResourceManager rm = new ResourceManager(typeof(LocalString));
            // Load local string
            LocalStrDic = new Dictionary<LocalStrID, string>();
            LocalStrDic[LocalStrID.TITLE] = rm.GetString("T_Title");
            LocalStrDic[LocalStrID.EXIT] = rm.GetString("T_Exit");

            // Load OptionName
            OptNameDic = new Dictionary<uint, string>();
			OptNameDic[TExcState.AWAY_MODE] = rm.GetString("T_Btn_AwayMode");
            OptNameDic[TExcState.SYS_REQ] = rm.GetString("T_Btn_RequireSystem");
            OptNameDic[TExcState.DISPLAY_REQ] = rm.GetString("T_Btn_RequireDisplay");
			OptNameDic[TExcState.AWAKE_IN_LID_CLODE] = rm.GetString("T_Btn_LidClosedAwake");
        }
    }

    public class ValChangeEvent
    {
		public delegate void ValChangeCB();
        protected List<ValChangeCB> call_back_list_ = new List<ValChangeCB>();
	
        public void AddValChangeCB(ValChangeCB cb) 
        {
            call_back_list_.Add(cb);
        }

        public bool DelValChangeCB(ValChangeCB cb)
        {
			return call_back_list_.Remove(cb);
        }

        protected void RunValChangeCB()
        {
			foreach (ValChangeCB cb in call_back_list_)
			{
                if (cb == null)
                {
					// TODO: List item can be deleted in loop?
                    DelValChangeCB(cb);
                    continue;
                }
                cb();
			}
        }

    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;

namespace SleepPreventer
{
    public class LidCloseAwakeKeeper : ValChangeEvent, IMessageFilter
    {
        public struct Setting
        {
            public bool Equals(Setting v)
            {
                // Return true if the fields match:
                return (v.ACIndex == this.ACIndex) && (v.DCIndex == this.DCIndex);
            }
            public uint DCIndex;
            public uint ACIndex;
        }

        public static bool GetActiveSchemeSetting(
			out Guid scheme_guid, ref Setting lid_close_setting)
        {
			if ( ! Public.PowerGetActiveSchemeWarp(out scheme_guid) )
			{
                return false;
			}
            return GetSchemeSetting(scheme_guid, ref lid_close_setting);
        }

		public static bool GetSchemeSetting(
            Guid scheme_guid, ref Setting lid_close_setting)
        {
            uint index = 0;
			if( Win32API.PowerReadACValueIndex(
                        IntPtr.Zero,
                        ref scheme_guid,
                        ref Public.GUID_SYSTEM_BUTTON_SUBGROUP,
                        ref Public.GUID_LIDCLOSE_ACTION,
                        ref index
					) != 0)
            {
                return false;
            }
            lid_close_setting.ACIndex = index;

            if (Win32API.PowerReadDCValueIndex(
                        IntPtr.Zero,
                        ref scheme_guid,
                        ref Public.GUID_SYSTEM_BUTTON_SUBGROUP,
                        ref Public.GUID_LIDCLOSE_ACTION,
                        ref index
                    ) != 0)
            {
                return false;
            }
            lid_close_setting.DCIndex = index;

            return true;

        }

        public static bool WriteSchemeSetting(
			Guid scheme_guid, Setting lid_close_setting)
        {
			// Write setting			
			if( Win32API.PowerWriteACValueIndex(
                        IntPtr.Zero,
                        ref scheme_guid,
                        ref Public.GUID_SYSTEM_BUTTON_SUBGROUP,
                        ref Public.GUID_LIDCLOSE_ACTION,
                        lid_close_setting.ACIndex
					) != 0)
            {
                return false;
            }

            if (Win32API.PowerWriteDCValueIndex(
                        IntPtr.Zero,
                        ref scheme_guid,
                        ref Public.GUID_SYSTEM_BUTTON_SUBGROUP,
                        ref Public.GUID_LIDCLOSE_ACTION,
                        lid_close_setting.DCIndex
                    ) != 0)
            {
                return false;
            }

			// Check is it current power scheme
            Guid cur_scheme;
			if (Public.PowerGetActiveSchemeWarp(out cur_scheme) )
			{
				if (cur_scheme == scheme_guid)
				{
                    if (Win32API.PowerSetActiveScheme(
                        IntPtr.Zero, ref cur_scheme) != 0)
                    {
                        return false;
                    }
				}
			}

            return true;
        }

        public static bool WriteActiveSchemeSetting(Setting lid_close_setting)
        {
			Guid scheme_guid;
            if ( ! Public.PowerGetActiveSchemeWarp(out scheme_guid) )
			{
                return false;
			}
            return WriteSchemeSetting(scheme_guid, lid_close_setting);
        }

        public static void CheckLogError()
        {
			string fn = SleepPreventer.Properties.Resources.ConfigFileName;
			if (File.Exists(fn))
			{
                using(var fs = File.OpenText(fn))
                {
                    string line = fs.ReadLine();
					if (line == null)
					{
						// The file is empty
                        return;
					}
                    string[] s_ary = line.Split(' ');
                    if (s_ary.Length == 2)
                    {
                        Setting back_st;
                        back_st.ACIndex = uint.Parse(s_ary[0]);
                        back_st.DCIndex = uint.Parse(s_ary[1]);

						// Get setting name
                        Dictionary<uint, string> index_name = new Dictionary<uint, string>();
                        index_name[0] = "不采取任何操作";
                        index_name[1] = "睡眠";
                        index_name[2] = "休眠";
                        index_name[3] = "关机";

						// Get current setting
						Guid guid;
                        Setting cur_setting = new Setting();
                        if (!GetActiveSchemeSetting(out guid, ref cur_setting))
                        {
                            return;
                        }
						if (cur_setting.Equals(back_st))
						{
                            return;
						}

                        string ac_setting, dc_setting;
						if (index_name.TryGetValue(back_st.ACIndex, out ac_setting)
								&& index_name.TryGetValue(back_st.DCIndex, out dc_setting))
						{
							if (MessageBox.Show("是否将合上盖子的操作恢复为： " 
											+ "\n\t"
											+ "使用电池时："+ ac_setting
											+ "，"
											+ "使用外置电源时：" + dc_setting
											+ "\n"
											+ "当前的设置为: "
											+ "\n\t"
											+ "使用电池时："+ index_name[cur_setting.ACIndex]
											+ "，"
											+ "使用外置电源时：" + index_name[cur_setting.DCIndex],
										"本程序上次意外退出",
                                        MessageBoxButtons.OKCancel) == DialogResult.OK)
							{
                                WriteActiveSchemeSetting(back_st);
							}
						}
                    }
                }
				// Clean file
				using (StreamWriter sw = File.CreateText(
						SleepPreventer.Properties.Resources.ConfigFileName))
				{
					;
				}
			}
        }

        public LidCloseAwakeKeeper(IntPtr hWnd = default(IntPtr))
        {
            hWnd_ = hWnd;
			if (hWnd_ == default(IntPtr))
			{
                hWnd_ = IntPtr.Zero;
			}
            target_setting_ = new Setting();
            target_setting_.ACIndex = target_power_setting_index_;
            target_setting_.DCIndex = target_power_setting_index_;

            lid_close_setting_back_ = new Setting();
            GetActiveSchemeSetting(out scheme_guid_,ref lid_close_setting_back_);
            IsPrevent = false;
            is_watching_ = false;
        }

        ~LidCloseAwakeKeeper()
        {
            UnWatchPowerSetting();
        }

        public bool IsPrevent { get; private set; }

        public bool TrySetNeedPrevent(bool value)
        {
			if (value != IsPrevent)
			{
                if (value)
                {
                    if (WatchPowerSetting())
                    {
                        IsPrevent = true;
                    }
                }
                else
                {
					if (UnWatchPowerSetting())
					{
                        IsPrevent = false;
					}
                }
				RunValChangeCB();
                return IsPrevent == value;
			}
            return true;
        }

        public bool PreFilterMessage(ref Message m)
        {
			if (is_watching_ )
            {
                switch (m.Msg)
                {
					case Win32API.WM_POWERBROADCAST:
						DoWatchPowerSetting();
						return true;
					case Win32API.WM_QUERYENDSESSION:
                        this.TrySetNeedPrevent(false);
						return false;
					//case Win32API.WM_ENDSESSION:
                    //    return false;
					default:
						break;
                }
            }
            return false;
        }

        private bool DoWatchPowerSetting()
        {
			Guid cur_scheme_id;
			Setting setting = new Setting();
			if (!GetActiveSchemeSetting(out cur_scheme_id, ref setting))
			{
				return false;
			}

			// Check power setting value is changed
			if (setting.Equals(target_setting_))
			{
				// Why the system send power setting changed message to me?
				return true;
			}

			if (scheme_guid_ != cur_scheme_id)
			{
				// The backup scheme is not active scheme now, write the backup setting back!
				WriteSchemeSetting(scheme_guid_, lid_close_setting_back_);
			}
			// Update the backup setting to current setting
			GetActiveSchemeSetting(out scheme_guid_, ref lid_close_setting_back_);
			// Write target Setting
			WriteSchemeSetting(cur_scheme_id, target_setting_);

			return true;	
        }

        private bool WatchPowerSetting()
        {
			if (is_watching_)
			{
                return true;
			}
			// Backup current setting
            GetActiveSchemeSetting(out scheme_guid_, ref lid_close_setting_back_);

			// listen power setting changed event
            // Win32API.HandlerEx handler = this.SystemPowerSettingChangedHandle;
            // IntPtr p = handler.Method.MethodHandle.Value;
            //ps_changed_gc_h_ = GCHandle.Alloc(handler);

            //IntPtr p_phandle = Win32API.GetCurrentProcess();
            //IntPtr t_phandle = Win32API.GetCurrentThread();
            //IntPtr t_handle = IntPtr.Zero;
            //var error = Win32API.DuplicateHandle(p_phandle, t_phandle, p_phandle, ref t_handle, 0, false, 2);
			if (hWnd_ != IntPtr.Zero)
			{
				ps_changed_h_ =
					Win32API.RegisterPowerSettingNotification(
						//GCHandle.ToIntPtr(ps_changed_gc_h_),
						hWnd_,
						ref Public.GUID_LIDCLOSE_ACTION,
						Win32API.DEVICE_NOTIFY_WINDOW_HANDLE
					);
                if (ps_changed_h_ != IntPtr.Zero)
                {
					// The form must call the message filter 
					//Application.AddMessageFilter(this);
                }
				else
				{
					//ps_changed_gc_h_.Free();
					return false;
				}
			}

			// Set target setting to system
			// The sheme_guid_ must be current scheme
			WriteSchemeSetting(scheme_guid_, target_setting_);
			// Write backup setting to file 
            using (StreamWriter sw = File.CreateText(SleepPreventer.Properties.Resources.ConfigFileName))
            {
                sw.WriteLine("" + 
						lid_close_setting_back_.ACIndex + " " 
                        + lid_close_setting_back_.DCIndex );
            }
			is_watching_ = true;
			return true;
        }

        private bool UnWatchPowerSetting()
        {
			if (! is_watching_)
			{
                return true;
			}
            bool result = true;	// If need not to unregister, return true
			if (ps_changed_h_ != IntPtr.Zero)
			{
                result = (Win32API.UnregisterPowerSettingNotification(ps_changed_h_) != 0);
                //Application.RemoveMessageFilter(this);
                //ps_changed_gc_h_.Free();
			}
            ps_changed_h_ = IntPtr.Zero;
			// Restore the backup setting
			WriteSchemeSetting(scheme_guid_, lid_close_setting_back_);
			// Clean back file
            using (StreamWriter sw = File.CreateText(SleepPreventer.Properties.Resources.ConfigFileName))
            {
                ;
            }
            is_watching_ = false;
            return result;
        }


        private uint SystemPowerSettingChangedHandle(
            uint dwControl, uint dwEventType, IntPtr lpEventData, IntPtr lpContext)
        {
            MessageBox.Show("Power setting changed!");
            if (dwControl == Win32API.SERVICE_CONTROL_POWEREVENT)
            {
                // TODO
            }
            return 0;
        }

		
        private IntPtr hWnd_;
        private IntPtr ps_changed_h_;
        //private GCHandle ps_changed_gc_h_;
        private Guid scheme_guid_;
        private Setting lid_close_setting_back_;
		private Setting target_setting_;
        private bool is_watching_;
        private const uint target_power_setting_index_ = Public.NO_ACTION_IN_LIDCLOSE_INDEX;
    }

    public class TExcState : ValChangeEvent
    {
        public const uint AWAY_MODE = Win32API.ES_AWAYMODE_REQUIRED;
        public const uint DISPLAY_REQ = Win32API.ES_DISPLAY_REQUIRED;
        public const uint SYS_REQ = Win32API.ES_SYSTEM_REQUIRED;
		// The ES_USER_PRESENT is invalid, reuse this magic number
        public const uint AWAKE_IN_LID_CLODE = Win32API.ES_USER_PRESENT;

        public TExcState(IntPtr hWnd = default(IntPtr))
        {
			// Always Need this option
            cur_state_ = Win32API.ES_CONTINUOUS;
            lc_keeper_ = new LidCloseAwakeKeeper(hWnd);
            //lc_keeper_.AddValChangeCB(new ValChangeCB(this.RunValChangeCB));
        }

        ~TExcState()
        {
			// The option will take effect before next set.
            Public.ResetSysIdle(Win32API.ES_CONTINUOUS);
        }

        public uint State
        {
            get { return cur_state_; }
        }

        public bool PreMessageFilter(ref Message m)
        {
            return lc_keeper_.PreFilterMessage(ref m);
        }

        public static uint SetBit(uint target, uint source, uint bits)
        {
			// reset target bits
            uint empty = target & ~bits;
			// set bits
            return empty | (source & bits);
        }


		public bool TrySetState(uint opt, bool setting)
        {
            uint opt_group = cur_state_;
            if (setting == true)
            {
                opt_group |= opt;
            }
            else
            {
                opt_group &= ~opt;
            }
            return TrySetStateGroup(opt_group);
        }

        private bool TrySetStateGroup(uint _state)
        {
            bool result = true;

            uint idle_state = _state & ~AWAKE_IN_LID_CLODE;
            // Handle SYS_REQ option
            // The DISPLAY_REQ option always need SYS_REQ option
            if ( (idle_state & DISPLAY_REQ) != 0)
            {
                idle_state |= SYS_REQ;
            }

            // Check sys idle state setter return
			if (Public.ResetSysIdle(idle_state))
			{
				cur_state_ = SetBit(cur_state_, _state, 
                    AWAY_MODE | DISPLAY_REQ | SYS_REQ);
            }
            else
            {
                result = false;
            }

			// Check lid close power setting setter
            bool val = 
                (_state & AWAKE_IN_LID_CLODE) != 0;
            if (!lc_keeper_.TrySetNeedPrevent(val))
            {
                result = false;
            }
            uint bitmask = lc_keeper_.IsPrevent ? ~(uint)0 : (uint)0;
            cur_state_ = SetBit(cur_state_, bitmask, AWAKE_IN_LID_CLODE);
			RunValChangeCB();
            return result;
        }
		private uint cur_state_;
        private LidCloseAwakeKeeper lc_keeper_;
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SleepPreventer
{

    public class MyNotifyIcon : IDisposable
    {
        public MyNotifyIcon(Form form, TExcState ts_setter = null, bool show_now = false)
        {
            ts_setter_ = ts_setter ?? new TExcState();
            main_form_ = form;

            InitMenuStrip();
			// Notify_icon
			notify_icon_ = new System.Windows.Forms.NotifyIcon();
            notify_icon_.Text = Public.LocalStrDic[Public.LocalStrID.TITLE];
            notify_icon_.Visible = true;
			notify_icon_.Click += new System.EventHandler(notifyIcon_Click);
            notify_icon_.ContextMenuStrip = notify_menu_;

            InitIcon();
			// Set state
            SetMenuItemState();

            ts_setter_.AddValChangeCB(new TExcState.ValChangeCB(this.SetMenuItemState));

			if (show_now)
			{
                main_form_.Show();
			}
				
        }

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            MouseEventArgs mouse_event = (MouseEventArgs)e;
			// Right button is for menu strip
            if (mouse_event.Button == MouseButtons.Right)
            {
                return;
            }
			else
            {
                //if (main_form_.IsDisposed)
                //{
                //    main_form_ = new Form1(ts_setter_, this);
                //    main_form_.Show();
                //}
                //else
                //{
                //	  main_form_.Close();
                //}
				if (main_form_.Visible)
				{
					main_form_.Hide();
				}
				else
				{
					main_form_.Show();
					main_form_.Activate();
				}
            }
        }

        private void exit_toolstripItemClick(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void opt_menuitemClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            item.Checked ^= true;
            uint value = menuitem_opt_dic_[item];
            if ( ! ts_setter_.TrySetState(value, item.Checked)) {
                MessageBox.Show(Public.LocalStrDic[Public.LocalStrID.OPTION_NOT_SUPPORT]);
            }
        }

        private void InitMenuStrip()
        {
			// New strip menu item
            opt_menuitem_dic_ = new Dictionary<uint, ToolStripMenuItem>();
			foreach (var opt in Public.OptNameDic.Keys)
			{
				opt_menuitem_dic_[opt] = new ToolStripMenuItem();
			}
            exit_item_ = new ToolStripMenuItem();
			
			// Set property
            menuitem_opt_dic_ = new Dictionary<ToolStripMenuItem, uint>();
			foreach (KeyValuePair<uint, ToolStripMenuItem> item in opt_menuitem_dic_)
			{
                uint opt = item.Key;
                ToolStripMenuItem menu_item = item.Value;
                menuitem_opt_dic_[menu_item] = opt;
                menu_item.Text = Public.OptNameDic[opt];
                menu_item.Click += new EventHandler(this.opt_menuitemClick);
			}
            exit_item_.Text = Public.LocalStrDic[Public.LocalStrID.EXIT];
            exit_item_.Click += new EventHandler(this.exit_toolstripItemClick);
			
			// Notify Menu
            notify_menu_ = new ContextMenuStrip();

            notify_menu_.Items.Add(opt_menuitem_dic_[TExcState.AWAY_MODE]);
            notify_menu_.Items.Add(opt_menuitem_dic_[TExcState.DISPLAY_REQ]);
            notify_menu_.Items.Add(opt_menuitem_dic_[TExcState.SYS_REQ]);
            notify_menu_.Items.Add(new ToolStripSeparator());
            notify_menu_.Items.Add(opt_menuitem_dic_[TExcState.AWAKE_IN_LID_CLODE]);
            notify_menu_.Items.Add(new ToolStripSeparator());
            notify_menu_.Items.Add(exit_item_);
        }

        private void SetMenuItemState()
        {
			foreach (KeyValuePair<uint, ToolStripMenuItem> item in opt_menuitem_dic_)
			{
                item.Value.Checked = ((item.Key & ts_setter_.State) != 0);
			}
			// icon
            uint cur_opt = ts_setter_.State;
			if ((cur_opt &	Win32API.ES_DISPLAY_REQUIRED) != 0)
			{
				notify_icon_.Icon = none_sleep_ico_;
			}
			else if ((cur_opt & Win32API.ES_SYSTEM_REQUIRED) != 0)
			{
                notify_icon_.Icon = part_sleep_ico_;
			}
            else
            {
                notify_icon_.Icon = all_sleep_ico_;
            }
        }

        private void InitIcon()
        {
			this.all_sleep_ico_ = Properties.Resources.Allsleep;
			this.part_sleep_ico_ = Properties.Resources.PartSleep;
            this.none_sleep_ico_ = Properties.Resources.NoneSleep;
        }

        public void Dispose()
        {
            notify_icon_.Dispose();
			Application.Exit();
        }
        private Dictionary<uint, ToolStripMenuItem> opt_menuitem_dic_;	
        private Dictionary<ToolStripMenuItem, uint> menuitem_opt_dic_;
        private ToolStripMenuItem exit_item_;

        private NotifyIcon notify_icon_;
        private ContextMenuStrip notify_menu_;
        private TExcState ts_setter_;
        private Form main_form_;

        private System.Drawing.Icon all_sleep_ico_;
        private System.Drawing.Icon part_sleep_ico_;
        private System.Drawing.Icon none_sleep_ico_;
    }
}

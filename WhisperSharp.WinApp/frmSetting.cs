using DatabaseManager.Core;
using SharpUtility.Helper;

namespace WhisperSharp.WinApp
{
    public partial class frmSetting : Form
    {
        public frmSetting()
        {
            InitializeComponent();
        }

        private void frmSetting_Load(object sender, EventArgs e)
        {
            var setting = SettingManager.Setting;

            this.txtDataFolder.Text = setting.DataFolder;
            this.chkEnableLog.Checked = setting.EnableLog;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var setting = SettingManager.Setting;

            setting.DataFolder = this.txtDataFolder.Text;
            setting.EnableLog = this.chkEnableLog.Checked;           

            SettingManager.SaveConfig(setting);

            LogHelper.EnableLog = setting.EnableLog;

            this.Close();
        }

        private void btnChooseFolder_Click(object sender, EventArgs e)
        {
            var result = this.folderBrowserDialog1.ShowDialog();

            if(result == DialogResult.OK)
            {
                this.txtDataFolder.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }
    }
}

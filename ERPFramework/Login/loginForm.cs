﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ERPFramework.Login
{
    public partial class loginForm : MetroFramework.Forms.MetroForm
    {
        public loginForm()
        {
            userInfo = GlobalInfo.UserInfo;

            pManager = new PasswordManager();

            InitializeComponent();

            if (GlobalInfo.LoginInfo.UserList.Count > 0)
            {
                this.txtUser.AutoCompleteCustomSource.AddRange(GlobalInfo.LoginInfo.UserList.ToArray());
                this.txtUser.Text = GlobalInfo.LoginInfo.UserList[0];

            }
        }

        protected override void OnShown(EventArgs e)
        {
           // metroStyleManager.Update();
        }

        private void btnLogin_Click(object sender, System.EventArgs e)
        {
            if (CheckUser(txtUser.Text, txtPassword.Text))
            {
                userInfo.User = txtUser.Text;
                userInfo.userType = pManager.UserType;
                this.DialogResult = DialogResult.OK;
                SaveData();
            }
            else
                this.DialogResult = DialogResult.Cancel;

            pManager.Dispose();
            this.Close();
        }

        public bool CheckUser(string user, string password)
        {
            if (pManager.SelectUser(user) == UserStatus.Found)
            {
                if (!pManager.CheckPassword(password)) return false;
                if (pManager.HasToChangePassword && !ChgPwdForm())
                    return false;
            }
            else
                return false;
            return true;
        }

        private void SaveData()
        {
            GlobalInfo.LoginInfo.RememberLastLogin = ckbRememberMe.Checked;

            if (!GlobalInfo.LoginInfo.UserList.Contains(txtUser.Text))
                GlobalInfo.LoginInfo.UserList.Add(txtUser.Text);

            GlobalInfo.LoginInfo.LastUser = ckbRememberMe.Checked
                            ? txtUser.Text
                            : string.Empty;
            GlobalInfo.LoginInfo.LastPassword = ckbRememberMe.Checked
                            ? txtPassword.Text
                            : string.Empty;

            GlobalInfo.DBaseInfo.dbManager.WriteConfigFile();
        }

        private bool ChgPwdForm()
        {
            using (var chgpwdForm = new ChgPwdForm(pManager.Password))
            {
                if (chgpwdForm.ShowDialog() == DialogResult.Cancel)
                    return false;

                pManager.ChangePassword(chgpwdForm.Password);
                txtPassword.Text = chgpwdForm.Password;
                return true;
            }
        }
    }
}
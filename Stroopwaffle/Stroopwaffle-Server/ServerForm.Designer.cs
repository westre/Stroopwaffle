﻿using System;

namespace Stroopwaffle_Server {
    partial class ServerForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.messageLog = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // messageLog
            // 
            this.messageLog.FormattingEnabled = true;
            this.messageLog.Location = new System.Drawing.Point(13, 13);
            this.messageLog.Name = "messageLog";
            this.messageLog.Size = new System.Drawing.Size(813, 238);
            this.messageLog.TabIndex = 0;
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(838, 261);
            this.Controls.Add(this.messageLog);
            this.Name = "ServerForm";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox messageLog;
    }
}


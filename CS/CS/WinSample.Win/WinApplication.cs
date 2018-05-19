using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.ExpressApp.Win;

namespace WinSample.Win {
    public partial class WinSampleWindowsFormsApplication : WinApplication {
        public WinSampleWindowsFormsApplication() {
            InitializeComponent();
        }

        private void WinSampleWindowsFormsApplication_DatabaseVersionMismatch(object sender, DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs e) {
            if (System.Diagnostics.Debugger.IsAttached) {
                e.Updater.Update();
                e.Handled = true;
            }
        }
    }
}

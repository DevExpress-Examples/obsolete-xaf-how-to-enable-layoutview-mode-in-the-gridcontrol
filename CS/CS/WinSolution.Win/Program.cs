using System;
using System.Windows.Forms;
using System.Configuration;
using DevExpress.ExpressApp.Security;

namespace WinSolution.Win {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            EditModelPermission.AlwaysGranted = System.Diagnostics.Debugger.IsAttached;
            WinSolutionWindowsFormsApplication winApplication = new WinSolutionWindowsFormsApplication();
            winApplication.DatabaseVersionMismatch += new EventHandler<DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs>(winApplication_DatabaseVersionMismatch);
            if (ConfigurationManager.ConnectionStrings["ConnectionString"] != null) {
                winApplication.ConnectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            }
            winApplication.ConnectionString = Demo.CodeCentralExampleInMemoryDataStoreProvider.ConnectionString;
            try {
                winApplication.Setup();
                winApplication.Start();
            } catch (Exception e) {
                winApplication.HandleException(e);
            }
        }
        static void winApplication_DatabaseVersionMismatch(object sender, DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs e) {
            e.Updater.Update();
            e.Handled = true;
        }
    }
}

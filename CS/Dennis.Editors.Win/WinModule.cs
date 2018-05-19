using DevExpress.ExpressApp;
using System.ComponentModel;
using DevExpress.ExpressApp.Model;

namespace Dennis.Editors.Win {
    [ToolboxItemFilter("Xaf.Platform.Win")]
    public sealed partial class EditorsWindowsFormsModule : ModuleBase {
        public EditorsWindowsFormsModule() {
            InitializeComponent();
        }
        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelListView, IModelLayoutViewListView>();
        }
    }
}

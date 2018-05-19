using DevExpress.ExpressApp;
using System.ComponentModel;
using DevExpress.ExpressApp.Model;
using System.ComponentModel.Design;

namespace Dennis.Editors.Win {
    public interface IModelLayoutViewListView {
        IModelLayoutViewSettings LayoutViewSettings { get; }
    }
    public interface IModelLayoutViewSettings : IModelNode, ISettingsProvider {
        [Category("Appearance")]
        [Editor(typeof(MultilineStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
        new string Settings { get; set; }
    }
}

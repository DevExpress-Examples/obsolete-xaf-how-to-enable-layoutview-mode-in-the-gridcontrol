using System;
using System.Windows.Forms;
using DevExpress.XtraLayout;
using DevExpress.XtraEditors;
using DevExpress.ExpressApp.DC;
using System.Collections.Generic;
using DevExpress.XtraGrid.Columns;
using DevExpress.ExpressApp.Model;
using DevExpress.XtraGrid.Views.Layout;
using DevExpress.ExpressApp.Localization;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.XtraGrid.Views.Grid.Drawing;
using DevExpress.XtraGrid.Views.Layout.Customization;

namespace Dennis.Editors.Win {
    //TODO: Ask XtraGrid team to expose public properties for customization form and rewrite this controller.
    public class LayoutViewColumnChooserController : ColumnChooserControllerBase {
        private LayoutViewField selectedColumn;
        private LayoutView layoutView;
        private LayoutControl layoutControl;
        private LayoutViewCustomizationForm customizationFormCore;
        private LayoutViewListEditor ListEditor {
            get { return ((DevExpress.ExpressApp.ListView)View).Editor as LayoutViewListEditor; }
        }
        private void columnChooser_SelectedColumnChanged(object sender, EventArgs e) {
            if (selectedColumn != null) {
                selectedColumn.ImageIndex = -1;
            }
            selectedColumn = ((ListBoxControl)ActiveListBox).SelectedItem as LayoutViewField;
            if (selectedColumn != null) {
                selectedColumn.ImageIndex = GridPainter.IndicatorFocused;
            }
            RemoveButton.Enabled = selectedColumn != null;
        }
        private void layoutView_ShowCustomization(object sender, EventArgs e) {
            CustomizationForm.VisibleChanged += new EventHandler(CustomizationForm_VisibleChanged);
        }
        private void CustomizationForm_VisibleChanged(object sender, EventArgs e) {
            ((Control)sender).VisibleChanged -= new EventHandler(CustomizationForm_VisibleChanged);
            if (((Control)sender).Visible) {
                layoutControl = new List<LayoutControl>(FindNestedControls<LayoutControl>(CustomizationForm))[3];
                InsertButtons();
                AddButton.Text += " (TODO)";
                selectedColumn = null;
                ((ListBoxControl)ActiveListBox).SelectedItem = null;
                ((ListBoxControl)ActiveListBox).KeyDown += new KeyEventHandler(ActiveListBox_KeyDown);
                ((ListBoxControl)ActiveListBox).SelectedValueChanged += new EventHandler(columnChooser_SelectedColumnChanged);
                layoutView.Images = GridPainter.Indicator;
            }
        }
        private void layoutView_HideCustomization(object sender, EventArgs e) {
            DeleteButtons();
            if (selectedColumn != null) {
                selectedColumn.ImageIndex = -1;
            }
            layoutView.Images = null;
            ((ListBoxControl)ActiveListBox).SelectedValueChanged += new EventHandler(columnChooser_SelectedColumnChanged);
            ((ListBoxControl)ActiveListBox).KeyDown += new KeyEventHandler(ActiveListBox_KeyDown);
            layoutControl = null;
            customizationFormCore = null;
            selectedColumn = null;
        }
        private void ActiveListBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Delete) {
                RemoveSelectedColumn();
            }
        }
        protected LayoutViewCustomizationForm CustomizationForm {
            get {
                if (customizationFormCore == null) {
                    customizationFormCore = typeof(LayoutView).GetProperty("CustomizationForm", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(layoutView, null) as LayoutViewCustomizationForm;
                }
                return customizationFormCore;
            }
        }
        protected override Control ActiveListBox {
            get {
                return layoutControl.Controls[4];
            }
        }
        private static IEnumerable<T> FindNestedControls<T>(Control container) where T : Control {
            if (container.Controls != null)
                foreach (Control item in container.Controls) {
                    if (item is T)
                        yield return (T)item;
                    foreach (T child in FindNestedControls<T>(item))
                        yield return child;
                }
        }
        protected override List<string> GetUsedProperties() {
            List<string> result = new List<string>();
            foreach (IModelColumn columnInfoNodeWrapper in ListEditor.Model.Columns) {
                result.Add(columnInfoNodeWrapper.PropertyName);
            }
            return result;
        }
        protected override ITypeInfo DisplayedTypeInfo {
            get { return ((DevExpress.ExpressApp.ListView)View).ObjectTypeInfo; }
        }
        //TODO: Implement adding new properties into the customization form.
        protected override void AddColumn(string propertyName) {
            IModelColumn columnInfo = FindColumnModelByPropertyName(propertyName);
            if (columnInfo == null) {
                columnInfo = ListEditor.Model.Columns.AddNode<IModelColumn>();
                columnInfo.Id = propertyName;
                columnInfo.PropertyName = propertyName;
                columnInfo.Index = -1;
                LayoutViewColumnWrapper wrapper = ListEditor.AddColumn(columnInfo) as LayoutViewColumnWrapper;
                if (wrapper != null && wrapper.Column != null && wrapper.Column.LayoutViewField != null) {
                    ((ListBoxControl)ActiveListBox).Items.Add(wrapper.Column.LayoutViewField);
                }
            }
            else {
                throw new Exception(SystemExceptionLocalizer.GetExceptionMessage(ExceptionId.CannotAddDuplicateProperty, propertyName));
            }
        }
        protected override void RemoveSelectedColumn() {
            LayoutViewField field = ((ListBoxControl)ActiveListBox).SelectedItem as LayoutViewField;
            if (field != null) {
                LayoutViewColumnWrapper columnInfo = null;
                foreach (LayoutViewColumn item in layoutView.Columns) {
                    if (item.FieldName == field.FieldName) {
                        columnInfo = ListEditor.FindColumn(((XafLayoutViewColumn)item).PropertyName) as LayoutViewColumnWrapper;
                        break;
                    }
                }
                if (columnInfo != null)
                    ListEditor.RemoveColumn(columnInfo);
                ((ListBoxControl)ActiveListBox).Items.Remove(field);
            }
        }
        protected override void AddButtonsToCustomizationForm() {
            layoutControl.Controls.Add(RemoveButton);
            layoutControl.Controls.Add(AddButton);

            LayoutControlGroup hiddenItemsGroup = layoutControl.Items[0] as LayoutControlGroup;
            LayoutControlItem addButtonLayoutItem = hiddenItemsGroup.AddItem();
            addButtonLayoutItem.Control = this.AddButton;
            addButtonLayoutItem.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 0, 0);
            addButtonLayoutItem.TextVisible = false;

            LayoutControlItem removeButtonLayoutItem = hiddenItemsGroup.AddItem();
            removeButtonLayoutItem.Control = this.RemoveButton;
            removeButtonLayoutItem.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 0, 5);
            removeButtonLayoutItem.TextVisible = false;
        }
        protected override void OnViewControlsCreated() {
            base.OnViewControlsCreated();
            SubscribeLayoutViewEvents();
        }
        private void SubscribeLayoutViewEvents() {
            if (ListEditor != null) {
                layoutView = ListEditor.LayoutView;
                layoutView.ShowCustomization += new EventHandler(layoutView_ShowCustomization);
                layoutView.HideCustomization += new EventHandler(layoutView_HideCustomization);
            }
        }
        protected override void OnDeactivated() {
            UnsubscribeLayoutViewEvents();
            selectedColumn = null;
            base.OnDeactivated();
        }
        private void UnsubscribeLayoutViewEvents() {
            if (layoutView != null) {
                layoutView.ShowCustomization -= new EventHandler(layoutView_ShowCustomization);
                layoutView.HideCustomization -= new EventHandler(layoutView_HideCustomization);
                layoutView = null;
            }
        }
        public LayoutViewColumnChooserController() {
            TypeOfView = typeof(DevExpress.ExpressApp.ListView);
        }
    }
}

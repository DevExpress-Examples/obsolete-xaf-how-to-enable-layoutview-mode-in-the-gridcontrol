using System;
using System.IO;
using System.Text;
using System.Drawing;
using DevExpress.Data;
using DevExpress.Utils;
using DevExpress.XtraGrid;
using System.Windows.Forms;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Filtering;
using DevExpress.XtraGrid.Views.Layout;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.XtraGrid.Views.Layout.ViewInfo;

namespace Dennis.Editors.Win {
    public class LayoutViewColumnWrapper : ColumnWrapper {
        private const int defaultColumnWidth = 75;
        static DefaultBoolean Convert(bool val) {
            if (!val) {
                return DefaultBoolean.False;
            }
            return DefaultBoolean.Default;
        }
        static bool Convert(DefaultBoolean val) {
            if (val == DefaultBoolean.False) {
                return false;
            }
            return true;
        }
        private XafLayoutViewColumn column;
        public LayoutViewColumnWrapper(XafLayoutViewColumn column) {
            this.column = column;
        }
        public XafLayoutViewColumn Column {
            get { return column; }
        }
        public override string Id {
            get {
                return column.Model.Id;
            }
        }
        public override string PropertyName {
            get { return column.PropertyName; }
        }
        public override int SortIndex {
            get { return column.SortIndex; }
            set { column.SortIndex = value; }
        }
        public override ColumnSortOrder SortOrder {
            get { return column.SortOrder; }
            set { column.SortOrder = value; }
        }
        public override bool AllowSortingChange {
            get { return Convert(column.OptionsColumn.AllowSort); }
            set { column.OptionsColumn.AllowSort = Convert(value); }
        }
        public override int VisibleIndex {
            get { return column.VisibleIndex; }
            set { column.VisibleIndex = value;}
        }
        public override string Caption {
            get {
                return column.Caption;
            }
            set {
                column.Caption = value;
                if (string.IsNullOrEmpty(column.Caption)) {
                    column.Caption = column.FieldName;
                }
            }
        }
        public override string DisplayFormat {
            get {
                return column.DisplayFormat.FormatString;
            }
            set {
                column.DisplayFormat.FormatString = value;
                column.DisplayFormat.FormatType = FormatType.Custom;
                column.GroupFormat.FormatString = value;
                column.GroupFormat.FormatType = FormatType.Custom;
            }
        }
        public override int Width {
            get {
                if (column.Width == defaultColumnWidth) {
                    return 0;
                }
                return column.Width;
            }
            set {
                if (value == 0) { return; }
                column.Width = value;
            }
        }
        public override void DisableFeaturesForProtectedContentColumn() {
            base.DisableFeaturesForProtectedContentColumn();
            column.OptionsFilter.AllowFilter = false;
            column.OptionsFilter.AllowAutoFilter = false;
            column.OptionsColumn.AllowIncrementalSearch = false;
        }
        public override void ApplyModel(IModelColumn columnInfo) {
            base.ApplyModel(columnInfo);
            column.ApplyModel(columnInfo);
        }
        public override void SynchronizeModel() {
            base.SynchronizeModel();
            column.SynchronizeModel();
        }
    }
    public class LayoutViewModelSynchronizer : ModelSynchronizer<LayoutView, IModelListView> {
        private LayoutViewListEditor listEditor;
        public LayoutViewModelSynchronizer(LayoutViewListEditor listEditor, IModelListView model)
            : base(listEditor.LayoutView, model) {
            this.listEditor = listEditor;
            listEditor.ControlsCreated += new EventHandler(listEditor_ControlsCreated);
        }
        private void listEditor_ControlsCreated(object sender, EventArgs e) {
            if (listEditor.CollectionSource != null) {
                CriteriaOperator criteriaOperator = CriteriaOperator.Parse(((IModelListViewWin)Model).ActiveFilterString);
                FilterWithObjectsProcessor criteriaProcessor = new FilterWithObjectsProcessor(listEditor.CollectionSource.ObjectSpace, Model.ModelClass.TypeInfo, false);
                criteriaProcessor.Process(criteriaOperator, FilterWithObjectsProcessorMode.StringToObject);
                EnumPropertyValueCriteriaProcessor enumParametersProcessor = new EnumPropertyValueCriteriaProcessor(listEditor.CollectionSource.ObjectTypeInfo);
                enumParametersProcessor.Process(criteriaOperator);
                Control.ActiveFilterCriteria = criteriaOperator;
            }
            Control.ActiveFilterEnabled = ((IModelListViewWin)Model).IsActiveFilterEnabled;
        }
        protected override void ApplyModelCore() {
            Control.ActiveFilterEnabled = ((IModelListViewWin)Model).IsActiveFilterEnabled;
            Control.ActiveFilterString = ((IModelListViewWin)Model).ActiveFilterString;
            if (Model is IModelListViewShowFindPanel) {
                if (((IModelListViewShowFindPanel)Model).ShowFindPanel) {
                    Control.ShowFindPanel();
                }
                else {
                    Control.HideFindPanel();
                }
            }
            if (Model is IModelLayoutViewListView) {
                string settings = ((IModelLayoutViewListView)Model).LayoutViewSettings.Settings;
                if (!string.IsNullOrEmpty(settings)) {
                    using (MemoryStream restoreStream = new MemoryStream(Encoding.UTF8.GetBytes((string)settings))) {
                        Control.RestoreLayoutFromStream(restoreStream, Control.OptionsLayout);
                    }
                }
            }
        }
        public override void SynchronizeModel() {
            ((IModelListViewWin)Model).IsActiveFilterEnabled = Control.ActiveFilterEnabled;
            if (!Object.ReferenceEquals(Control.ActiveFilterCriteria, null) && listEditor.CollectionSource != null) {
                CriteriaOperator criteriaOperator = CriteriaOperator.Clone(Control.ActiveFilterCriteria);
                FilterWithObjectsProcessor criteriaProcessor = new FilterWithObjectsProcessor(listEditor.CollectionSource.ObjectSpace);
                criteriaProcessor.Process(criteriaOperator, FilterWithObjectsProcessorMode.ObjectToString);
                ((IModelListViewWin)Model).ActiveFilterString = criteriaOperator.ToString();
            }
            else {
                ((IModelListViewWin)Model).ActiveFilterString = null;
            }
            if (Model is IModelListViewShowFindPanel) {
                ((IModelListViewShowFindPanel)Model).ShowFindPanel = Control.IsFindPanelVisible;
            }
            if (Model is IModelLayoutViewListView) {
                using (MemoryStream saveStream = new MemoryStream()) {
                    Control.SaveLayoutToStream(saveStream, Control.OptionsLayout);
                    ((IModelLayoutViewListView)Model).LayoutViewSettings.Settings = Encoding.UTF8.GetString(saveStream.ToArray());
                }
            }
        }
        public override void Dispose() {
            base.Dispose();
            if (listEditor != null) {
                listEditor.ControlsCreated -= new EventHandler(listEditor_ControlsCreated);
            }
        }
    }
    public class LayoutViewListEditorSynchronizer : ModelSynchronizer {
        private ModelSynchronizerList modelSynchronizerList;
        public LayoutViewListEditorSynchronizer(LayoutViewListEditor gridListEditor, IModelListView model)
            : base(gridListEditor, model) {
            modelSynchronizerList = new ModelSynchronizerList();
            modelSynchronizerList.Add(new ColumnsListEditorModelSynchronizer(gridListEditor, model));
            modelSynchronizerList.Add(new LayoutViewModelSynchronizer(gridListEditor, model));
            ((LayoutViewListEditor)Control).LayoutView.ColumnPositionChanged += Control_Changed;
        }
        protected override void ApplyModelCore() {
            modelSynchronizerList.ApplyModel();
        }
        public override void SynchronizeModel() {
            modelSynchronizerList.SynchronizeModel();
        }
        public override void Dispose() {
            base.Dispose();
            modelSynchronizerList.Dispose();
            LayoutViewListEditor gridListEditor = Control as LayoutViewListEditor;
            if (gridListEditor != null && gridListEditor.LayoutView != null) {
                gridListEditor.LayoutView.ColumnPositionChanged -= Control_Changed;
            }
        }
    }
    public class LayoutViewUtils {
        public static bool HasValidRowHandle(LayoutView view) {
            return ((view.GridControl.DataSource != null) && (view.FocusedRowHandle >= 0) && (view.RowCount > 0));
        }
        public static void SelectFocusedRow(LayoutView view) {
            SelectRowByHandle(view, view.FocusedRowHandle);
        }
        public static void SelectRowByHandle(LayoutView view, int rowHandle) {
            if (rowHandle != GridControl.InvalidRowHandle && view.GridControl != null) {
                view.BeginSelection();
                try {
                    view.ClearSelection();
                    view.SelectRow(rowHandle);
                    view.FocusedRowHandle = rowHandle;
                } finally {
                    view.EndSelection();
                }
            }
        }
        public static object GetFocusedRowObject(LayoutView view) {
            return GetRow(view, view.FocusedRowHandle);
        }
        public static object GetNearestRowObject(LayoutView view) {
            object result = GetRow(view, view.FocusedRowHandle + 1);
            if (result == null) {
                result = GetRow(view, view.FocusedRowHandle - 1);
            }
            return result;
        }
        public static object GetRow(LayoutView view, int rowHandle) {
            return GetRow(null, view, rowHandle);
        }
        public static bool IsRowSelected(LayoutView view, int rowHandle) {
            int[] selected = view.GetSelectedRows();
            for (int i = 0; (selected != null) && (i < selected.Length - 1); i++) {
                if (selected[i] == rowHandle) {
                    return true;
                }
            }
            return false;
        }
        public static Object GetRow(CollectionSourceBase collectionSource, LayoutView view, int rowHandle) {
            if (
                (!view.IsDataRow(rowHandle) && !view.IsNewItemRow(rowHandle))
                ||
                (view.GridControl.DataSource == null)
                ||
                ((view.DataSource != view.GridControl.DataSource) && !view.IsServerMode)) {
                return null;
            }
            if ((collectionSource is CollectionSource) && ((CollectionSource)collectionSource).IsServerMode && ((CollectionSource)collectionSource).IsAsyncServerMode) {
                if (!view.IsRowLoaded(rowHandle)) {
                    return null;
                }
                String keyPropertyName = "";
                if (collectionSource.ObjectTypeInfo.KeyMember != null) {
                    keyPropertyName = collectionSource.ObjectTypeInfo.KeyMember.Name;
                }
                if (!String.IsNullOrEmpty(keyPropertyName)) {
                    Object objectKey = view.GetRowCellValue(rowHandle, keyPropertyName);
                    return collectionSource.ObjectSpace.GetObjectByKey(collectionSource.ObjectTypeInfo.Type, objectKey);
                }
            }
            object result = view.GetRow(rowHandle);
            return result;
        }
        public static Object GetFocusedRowObject(CollectionSourceBase collectionSource, LayoutView view) {
            return GetRow(collectionSource, view, view.FocusedRowHandle);
        }
    }
    internal class CancelEventArgsAppearanceAdapter : IAppearanceEnabled, IAppearanceItem {
        private CancelEventArgs cancelEdit;
        public CancelEventArgsAppearanceAdapter(CancelEventArgs cancelEdit) {
            this.cancelEdit = cancelEdit;
        }
        #region IAppearanceEnabled Members
        public bool Enabled {
            get { return !cancelEdit.Cancel; }
            set { cancelEdit.Cancel = !value; }
        }
        #endregion
        #region IAppearanceItem Members
        public object Data {
            get { return cancelEdit; }
        }
        #endregion
    }
    internal class AppearanceObjectAdapterWithReset : AppearanceObjectAdapter, IAppearanceReset {
        private AppearanceObject appearanceObject;
        public AppearanceObjectAdapterWithReset(AppearanceObject appearanceObject, object data)
            : base(appearanceObject, data) {
            this.appearanceObject = appearanceObject;
        }
        public void ResetAppearance() {
            appearanceObject.Reset();
        }
    }
    public class LayoutViewAutoScrollHelper {
        public LayoutViewAutoScrollHelper(LayoutView view) {
            fGrid = view.GridControl;
            fView = view;
            fScrollInfo = new ScrollInfo(this, view);
        }

        GridControl fGrid;
        LayoutView fView;
        ScrollInfo fScrollInfo;
        public int ThresholdInner = 20;
        public int ThresholdOutter = 100;
        public int HorizontalScrollStep = 10;
        public int ScrollTimerInterval {
            get {
                return fScrollInfo.scrollTimer.Interval;
            }
            set {
                fScrollInfo.scrollTimer.Interval = value;
            }
        }

        public void ScrollIfNeeded() {
            Point pt = fGrid.PointToClient(Control.MousePosition);
            LayoutViewInfo viewInfo = fView.GetViewInfo() as LayoutViewInfo;
            Rectangle rect = viewInfo.ViewRects.CardsRect;
            fScrollInfo.GoLeft = (pt.X > rect.Left - ThresholdOutter) && (pt.X < rect.Left + ThresholdInner);
            fScrollInfo.GoRight = (pt.X > rect.Right - ThresholdInner) && (pt.X < rect.Right + ThresholdOutter);
            fScrollInfo.GoUp = (pt.Y < rect.Top + ThresholdInner) && (pt.Y > rect.Top - ThresholdOutter);
            fScrollInfo.GoDown = (pt.Y > rect.Bottom - ThresholdInner) && (pt.Y < rect.Bottom + ThresholdOutter);
        }

        internal class ScrollInfo {
            internal Timer scrollTimer;
            LayoutView view = null;
            bool left, right, up, down;

            LayoutViewAutoScrollHelper owner;
            public ScrollInfo(LayoutViewAutoScrollHelper owner, LayoutView view) {
                this.owner = owner;
                this.view = view;
                this.scrollTimer = new Timer();
                this.scrollTimer.Interval = 500;
                this.scrollTimer.Tick += new EventHandler(scrollTimer_Tick);
            }
            public bool GoLeft {
                get { return left; }
                set {
                    if (left != value) {
                        left = value;
                        CalcInfo();
                    }
                }
            }
            public bool GoRight {
                get { return right; }
                set {
                    if (right != value) {
                        right = value;
                        CalcInfo();
                    }
                }
            }
            public bool GoUp {
                get { return up; }
                set {
                    if (up != value) {
                        up = value;
                        CalcInfo();
                    }
                }
            }
            public bool GoDown {
                get { return down; }
                set {
                    if (down != value) {
                        down = value;
                        CalcInfo();
                    }
                }
            }
            private void scrollTimer_Tick(object sender, EventArgs e) {
                owner.ScrollIfNeeded();

                if (GoDown)
                    view.VisibleRecordIndex++;
                if (GoUp)
                    view.VisibleRecordIndex--;
                if (GoLeft)
                    view.VisibleRecordIndex--;
                if (GoRight)
                    view.VisibleRecordIndex++;

                if (view.VisibleRecordIndex == 0 || view.VisibleRecordIndex == view.RowCount - 1)
                    scrollTimer.Stop();
            }
            void CalcInfo() {
                if (!(GoDown && GoLeft && GoRight && GoUp))
                    scrollTimer.Stop();

                if (GoDown || GoLeft || GoRight || GoUp)
                    scrollTimer.Start();
            }
        }
    }

}

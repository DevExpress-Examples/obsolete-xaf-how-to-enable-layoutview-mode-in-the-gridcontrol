using System;
using DevExpress.Xpo;
using System.Drawing;
using DevExpress.Data;
using DevExpress.XtraGrid;
using System.Windows.Forms;
using DevExpress.ExpressApp.DC;
using DevExpress.XtraGrid.Filter;
using DevExpress.ExpressApp.Core;
using DevExpress.XtraGrid.Columns;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Editors;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.ExpressApp.Win.Core;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Views.Layout;
using DevExpress.XtraEditors.Filtering;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraEditors.DXErrorProvider;

namespace Dennis.Editors.Win {
    public class XafLayoutView : LayoutView {
        private ErrorMessages errorMessages;
        private BaseGridController gridController;
        private Boolean skipMakeRowVisible;
        public XafLayoutView() { }
        public XafLayoutView(GridControl ownerGrid)
            : base(ownerGrid) { }
        internal void SuppressInvalidCastException() {
            foreach (GridColumn column in Columns) {
                if (column.ColumnEdit != null && column.ColumnEdit is RepositoryItemLookupEdit) {
                    //TODO: Check whether it is important here.
                    //((RepositoryItemLookupEdit)column.ColumnEdit).ThrowInvalidCastException = false;
                }
            }
        }
        internal void CancelSuppressInvalidCastException() {
            foreach (GridColumn column in Columns) {
                if (column.ColumnEdit != null && column.ColumnEdit is RepositoryItemLookupEdit) {
                    //TODO: Check whether it is important here.
                    //((RepositoryItemLookupEdit)column.ColumnEdit).ThrowInvalidCastException = true;
                }
            }
        }
        private object GetFocusedObject() {
            return LayoutViewUtils.GetFocusedRowObject(this);
        }
        protected override BaseView CreateInstance() {
            XafLayoutView view = new XafLayoutView();
            view.SetGridControl(GridControl);
            return view;
        }
        protected override void AssignColumns(ColumnView cv, bool synchronize) {
            if (synchronize) {
                base.AssignColumns(cv, synchronize);
            }
            else {
                Columns.Clear();
                for (int n = 0; n < cv.Columns.Count; n++) {
                    if (cv.Columns[n] is XafLayoutViewColumn) {
                        XafLayoutViewColumn cvColumn = (XafLayoutViewColumn)cv.Columns[n];
                        Columns.Add(new XafLayoutViewColumn(cvColumn.TypeInfo, cvColumn.ListEditor));
                    }
                    else {
                        Columns.Add(new GridColumn());
                    }
                }
                for (int n = 0; n < Columns.Count; n++) {
                    if (Columns[n] is XafLayoutViewColumn) {
                        ((XafLayoutViewColumn)Columns[n]).Assign(cv.Columns[n]);
                    }
                }
            }
        }
        protected override void RaiseShownEditor() {
            if (ActiveEditor is IGridInplaceEdit) {
                if (GetFocusedObject() is IXPSimpleObject) {
                    ((IGridInplaceEdit)ActiveEditor).GridEditingObject = (IXPSimpleObject)GetFocusedObject();
                }
             }
            base.RaiseShownEditor();
        }
        protected override string GetColumnError(int rowHandle, GridColumn column) {
            string result = null;
            if (errorMessages != null) {
                object listItem = GetRow(rowHandle);
                if (column == null) {
                    result = errorMessages.GetMessages(listItem);
                }
                else {
                    result = errorMessages.GetMessage(column.FieldName, listItem);
                }
            }
            else {
                result = base.GetColumnError(rowHandle, column);
            }
            return result;
        }
        protected override ErrorType GetColumnErrorType(int rowHandle, GridColumn column) {
            return ErrorType.Critical;
        }
        protected virtual void OnCustomCreateFilterColumnCollection(CustomCreateFilterColumnCollectionEventArgs args) {
            if (CustomCreateFilterColumnCollection != null) {
                CustomCreateFilterColumnCollection(this, args);
            }
        }
        protected override FilterColumnCollection CreateFilterColumnCollection() {
            CustomCreateFilterColumnCollectionEventArgs args = new CustomCreateFilterColumnCollectionEventArgs();
            OnCustomCreateFilterColumnCollection(args);
            if (args.FilterColumnCollection == null) {
                args.FilterColumnCollection = base.CreateFilterColumnCollection();
            }
            return args.FilterColumnCollection;
        }
        protected void RaiseFilterEditorPopup() {
            if (FilterEditorPopup != null) {
                FilterEditorPopup(this, EventArgs.Empty);
            }
        }
        protected void RaiseFilterEditorClosed() {
            if (FilterEditorClosed != null) {
                FilterEditorClosed(this, EventArgs.Empty);
            }
        }
        protected override void ShowFilterPopup(GridColumn column, Rectangle bounds, Control ownerControl, object creator) {
            RaiseFilterEditorPopup();
            base.ShowFilterPopup(column, bounds, ownerControl, creator);
        }
        protected override void OnFilterPopupCloseUp(GridColumn column) {
            base.OnFilterPopupCloseUp(column);
            RaiseFilterEditorClosed();
        }
        protected override ColumnFilterInfo DoCustomFilter(GridColumn column, ColumnFilterInfo filterInfo) {
            RaiseFilterEditorPopup();
            ColumnFilterInfo result = base.DoCustomFilter(column, filterInfo);
            RaiseFilterEditorClosed();
            return result;
        }
        protected override void RaiseInvalidRowException(InvalidRowExceptionEventArgs ex) {
            if (String.IsNullOrEmpty(ex.ErrorText)) {
                ex.ExceptionMode = ExceptionMode.NoAction;
            }
            else {
                ex.ExceptionMode = ExceptionMode.ThrowException;
            }
            base.RaiseInvalidRowException(ex);
        }
        protected override void OnActiveEditor_MouseDown(object sender, MouseEventArgs e) {
            if (ActiveEditor != null) {
                base.OnActiveEditor_MouseDown(sender, e);
            }
        }
        protected override BaseGridController CreateDataController() {
            gridController = base.CreateDataController();
            return gridController;
        }
        protected override FilterCustomDialog CreateCustomFilterDialog(GridColumn column) {
            if (!OptionsFilter.UseNewCustomFilterDialog) {
                return new XafFilterCustomDialog(column);
            }
            return new XafFilterCustomDialog2(column, Columns);
        }
        protected internal void CancelCurrentRowEdit() {
            if ((gridController != null) && !gridController.IsDisposed
                && (ActiveEditor != null) && (gridController.IsCurrentRowEditing || gridController.IsCurrentRowModified)) {
                gridController.CancelCurrentRowEdit();
            }
        }
        protected override void MakeRowVisibleCore(int rowHandle, bool invalidate) {
            if (!skipMakeRowVisible) {
                base.MakeRowVisibleCore(rowHandle, invalidate);
            }
        }
        protected internal Boolean SkipMakeRowVisible {
            get { return skipMakeRowVisible; }
            set { skipMakeRowVisible = value; }
        }
        public override void ShowFilterEditor(GridColumn defaultColumn) {
            RaiseFilterEditorPopup();
            SuppressInvalidCastException();
            base.ShowFilterEditor(defaultColumn);
            CancelSuppressInvalidCastException();
            RaiseFilterEditorClosed();
        }
        public bool IsFirstColumnInFirstRowFocused {
            get {
                return (FocusedRowHandle == 0) && (FocusedColumn == GetVisibleColumn(0));
            }
        }
        public bool IsLastColumnInLastRowFocused {
            get {
                return (FocusedRowHandle == RowCount - 1) && IsLastColumnFocused;
            }
        }
        public bool IsLastColumnFocused {
            get {
                return (FocusedColumn == GetVisibleColumn(VisibleColumns.Count - 1));
            }
        }
        public ErrorMessages ErrorMessages {
            get { return errorMessages; }
            set { errorMessages = value; }
        }
        public event EventHandler FilterEditorPopup;
        public event EventHandler FilterEditorClosed;
        public event EventHandler<CustomCreateFilterColumnCollectionEventArgs> CustomCreateFilterColumnCollection;
    }
    public class XafLayoutViewColumn : LayoutViewColumn {
        private ITypeInfo typeInfo;
        private IModelColumn model;
        private LayoutViewListEditor listEditor;
        private ModelSynchronizer CreateModelSynchronizer() {
            return new ColumnWrapperModelSynchronizer(new LayoutViewColumnWrapper(this), model, listEditor);
        }
        public XafLayoutViewColumn(ITypeInfo typeInfo, LayoutViewListEditor listEditor) {
            this.typeInfo = typeInfo;
            this.listEditor = listEditor;
        }
        internal new void Assign(GridColumn column) {
            base.Assign(column);
        }
        public void ApplyModel(IModelColumn columnInfo) {
            model = columnInfo;
            CreateModelSynchronizer().ApplyModel();
        }
        public void SynchronizeModel() {
            CreateModelSynchronizer().SynchronizeModel();
        }
        public LayoutViewListEditor ListEditor { get { return listEditor; } }
        public ITypeInfo TypeInfo { get { return typeInfo; } }
        public string PropertyName {
            get {
                if (model != null)
                    return model.PropertyName;
                return string.Empty;
            }
        }
        public override Type ColumnType {
            get {
                if (string.IsNullOrEmpty(FieldName) || TypeInfo == null) return base.ColumnType;
                IMemberInfo memberInfo = typeInfo.FindMember(FieldName);
                return memberInfo != null ? memberInfo.MemberType : base.ColumnType;
            }
        }
        public IModelColumn Model { get { return model; } }
    }
}

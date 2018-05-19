/* DISCLAIMER
 * 
 * The LayoutViewListEditor class implemented in this example is not a complete solution, but rather a starting point for creating a custom List Editor based on the XtraGrid's LayoutView and providing similar features as a standard GridListEditor. 
 * Since this custom List Editor may have issues, you will use it at your own risk. 
 * Refer to the sources of a standard GridListEditor class ("%ProgramFiles\DevExpress 2011.1\eXpressApp Framework\Sources\DevExpress.ExpressApp\DevExpress.ExpressApp.Win\Editors\GridListEditor.cs") and the product documentation for more information about implementing custom List Editors.
 * You can also track http://www.devexpress.com/Support/Center/p/S19992.aspx to be automatically notified when a complete solution is available.
 * 
 * Thanks, 
 * Dennis
*/

using System;
using System.Drawing;
using DevExpress.Xpo;
using DevExpress.Data;
using DevExpress.Utils;
using System.Collections;
using DevExpress.XtraGrid;
using System.Windows.Forms;
using DevExpress.Utils.Menu;
using DevExpress.ExpressApp;
using System.ComponentModel;
using DevExpress.XtraEditors;
using DevExpress.XtraPrinting;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Win;
using System.Collections.Generic;
using DevExpress.ExpressApp.Core;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Utils;
using DevExpress.XtraGrid.Columns;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Win.Core;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraEditors.Controls;
using DevExpress.ExpressApp.Templates;
using DevExpress.XtraGrid.Views.Layout;
using DevExpress.XtraEditors.Repository;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.ExpressApp.Localization;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Win.Controls;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.XtraGrid.Views.Layout.ViewInfo;

namespace Dennis.Editors.Win {
    [ListEditor(typeof(object), false)]
    public class LayoutViewListEditor : ColumnsListEditor, /*Removed: ISupportNewItemRowPosition, IGridListEditorTestable, ISupportFooter, ISupportConditionalFormatting,*/IControlOrderProvider, IDXPopupMenuHolder, IComplexListEditor, IPrintableSource, ILookupListEditor, IHtmlFormattingSupport, IFocusedElementCaptionProvider, ILookupEditProvider, IExportableEditor, ISupportAppearanceCustomization {
        private RepositoryEditorsFactory repositoryFactory;
        private bool readOnlyEditors;
        private GridControl grid;
        private XafLayoutView layoutView;
        private int mouseDownTime;
        private int mouseUpTime;
        private bool activatedByMouse;
        private bool focusedChangedRaised;
        private bool selectedChangedRaised;
        private bool isForceSelectRow;
        private int prevFocusedRowHandle;
        private CollectionSourceBase collectionSource;
        private RepositoryItem activeEditor;
        private ActionsDXPopupMenu popupMenu;
        private Boolean processSelectedItemBySingleClick;
        private Boolean scrollOnMouseMove;
        private Boolean trackMousePosition;
        private bool selectedItemExecuting;
        private XafApplication application;
        private bool isRowFocusingForced;
        private IPrintable printable;
        private LayoutViewAutoScrollHelper autoScrollHelper;
        public LayoutViewListEditor(IModelListView model)
            : base(model) {
            popupMenu = new ActionsDXPopupMenu();
        }
        private BaseEdit GetEditor(Object sender) {
            if (sender is BaseEdit) {
                return (BaseEdit)sender;
            }
            if (sender is RepositoryItem) {
                return ((RepositoryItem)sender).OwnerEdit;
            }
            return null;
        }
        private void SubscribeLayoutViewEvents() {
            layoutView.BeforeLeaveRow += new RowAllowEventHandler(layoutView_BeforeLeaveRow);
            layoutView.FocusedRowChanged += new FocusedRowChangedEventHandler(layoutView_FocusedRowChanged);
            layoutView.ColumnFilterChanged += new EventHandler(layoutView_ColumnFilterChanged);
            layoutView.SelectionChanged += new SelectionChangedEventHandler(layoutView_SelectionChanged);
            layoutView.ShowingEditor += new CancelEventHandler(layoutView_EditorShowing);
            layoutView.ShownEditor += new EventHandler(layoutView_ShownEditor);
            layoutView.HiddenEditor += new EventHandler(layoutView_HiddenEditor);
            layoutView.MouseDown += new MouseEventHandler(layoutView_MouseDown);
            layoutView.MouseUp += new MouseEventHandler(layoutView_MouseUp);
            layoutView.Click += new EventHandler(layoutView_Click);
            layoutView.MouseMove += new MouseEventHandler(layoutView_MouseMove);
            layoutView.ShowCustomization += new EventHandler(layoutView_ShowCustomization);
            layoutView.HideCustomization += new EventHandler(layoutView_HideCustomization);
            layoutView.CustomFieldValueStyle += new DevExpress.XtraGrid.Views.Layout.Events.LayoutViewFieldValueStyleEventHandler(layoutView_CustomFieldValueStyle);
            layoutView.CustomCreateFilterColumnCollection += new EventHandler<CustomCreateFilterColumnCollectionEventArgs>(layoutView_CustomCreateFilterColumnCollection);
            layoutView.CustomRowCellEdit += new DevExpress.XtraGrid.Views.Layout.Events.LayoutViewCustomRowCellEditEventHandler(layoutView_CustomRowCellEdit);
            layoutView.FilterEditorPopup += new EventHandler(layoutView_FilterEditorPopup);
            layoutView.FilterEditorClosed += new EventHandler(layoutView_FilterEditorClosed);
            layoutView.CustomCardCaptionImage += new DevExpress.XtraGrid.Views.Layout.Events.LayoutViewCardCaptionImageEventHandler(layoutView_CustomCardCaptionImage);
            if (AllowEdit) {
                layoutView.ValidateRow += new ValidateRowEventHandler(layoutView_ValidateRow);
            }
            if (layoutView.DataController != null) {
                layoutView.DataController.ListChanged += new ListChangedEventHandler(DataController_ListChanged);
            }
        }
        private void layoutView_CustomCardCaptionImage(object sender, DevExpress.XtraGrid.Views.Layout.Events.LayoutViewCardCaptionImageEventArgs e) {
            e.CaptionImageLocation = GroupElementLocation.BeforeText;
            e.Image = ImageLoader.Instance.GetImageInfo(ViewImageNameHelper.GetImageName(Model)).Image;
        }
        private void layoutView_MouseMove(object sender, MouseEventArgs e) {
            if (scrollOnMouseMove || trackMousePosition)
                autoScrollHelper.ScrollIfNeeded();
        }
        private void layoutView_CustomRowCellEdit(object sender, CustomRowCellEditEventArgs e) {
            if (e.RowHandle == GridControl.AutoFilterRowHandle && e.Column.OptionsFilter.FilterBySortField != DefaultBoolean.False && !String.IsNullOrEmpty(e.Column.FieldNameSortGroup) && e.Column.FieldName != e.Column.FieldNameSortGroup) {
                e.RepositoryItem = new RepositoryItemStringEdit();
            }
        }
        private void layoutView_CustomCreateFilterColumnCollection(object sender, CustomCreateFilterColumnCollectionEventArgs e) {
            if (collectionSource != null) {
                IFilterColumnCollectionHelper helper = new FilterColumnCollectionHelper(application, collectionSource.ObjectSpace, collectionSource.ObjectTypeInfo);
                e.FilterColumnCollection = new MemberInfoFilterColumnCollection(helper);
            }
        }
        private void UnsubscribeLayoutViewEvents() {
            layoutView.FocusedRowChanged -= new FocusedRowChangedEventHandler(layoutView_FocusedRowChanged);
            layoutView.ColumnFilterChanged -= new EventHandler(layoutView_ColumnFilterChanged);
            layoutView.SelectionChanged -= new SelectionChangedEventHandler(layoutView_SelectionChanged);
            layoutView.ShowingEditor -= new CancelEventHandler(layoutView_EditorShowing);
            layoutView.ShownEditor -= new EventHandler(layoutView_ShownEditor);
            layoutView.HiddenEditor -= new EventHandler(layoutView_HiddenEditor);
            layoutView.MouseDown -= new MouseEventHandler(layoutView_MouseDown);
            layoutView.MouseUp -= new MouseEventHandler(layoutView_MouseUp);
            layoutView.Click -= new EventHandler(layoutView_Click);
            layoutView.MouseMove -= new MouseEventHandler(layoutView_MouseMove);
            layoutView.ShowCustomization -= new EventHandler(layoutView_ShowCustomization);
            layoutView.HideCustomization -= new EventHandler(layoutView_HideCustomization);
            layoutView.CustomFieldValueStyle -= new DevExpress.XtraGrid.Views.Layout.Events.LayoutViewFieldValueStyleEventHandler(layoutView_CustomFieldValueStyle);
            layoutView.ValidateRow -= new ValidateRowEventHandler(layoutView_ValidateRow);
            layoutView.BeforeLeaveRow -= new RowAllowEventHandler(layoutView_BeforeLeaveRow);
            layoutView.CustomCreateFilterColumnCollection -= new EventHandler<CustomCreateFilterColumnCollectionEventArgs>(layoutView_CustomCreateFilterColumnCollection);
            layoutView.CustomRowCellEdit -= new DevExpress.XtraGrid.Views.Layout.Events.LayoutViewCustomRowCellEditEventHandler(layoutView_CustomRowCellEdit);
            layoutView.FilterEditorPopup -= new EventHandler(layoutView_FilterEditorPopup);
            layoutView.FilterEditorClosed -= new EventHandler(layoutView_FilterEditorClosed);
            layoutView.CustomCardCaptionImage += new DevExpress.XtraGrid.Views.Layout.Events.LayoutViewCardCaptionImageEventHandler(layoutView_CustomCardCaptionImage);
            if (layoutView.DataController != null) {
                layoutView.DataController.ListChanged -= new ListChangedEventHandler(DataController_ListChanged);
            }
        }
        private void SetupLayoutView() {
            DevExpress.ExpressApp.Utils.Guard.ArgumentNotNull(layoutView, "layoutView");
            autoScrollHelper = new LayoutViewAutoScrollHelper(layoutView);
            layoutView.TemplateCard = new LayoutViewCard();
            layoutView.TemplateCard.AllowDrawBackground = false;
            layoutView.CardMinSize = new Size(400, 200);
            layoutView.ErrorMessages = ErrorMessages;
            layoutView.ShowButtonMode = ShowButtonModeEnum.ShowOnlyInEditor;

            layoutView.OptionsSingleRecordMode.StretchCardToViewHeight = true;
            layoutView.OptionsSingleRecordMode.StretchCardToViewWidth = true;

            layoutView.OptionsView.AnimationType = GridAnimationType.AnimateFocusedItem;
            layoutView.OptionsView.AllowHotTrackFields = false;
            layoutView.OptionsView.ShowFieldHints = true;

            layoutView.OptionsBehavior.AutoFocusCardOnScrolling = true;
            layoutView.OptionsBehavior.AutoPopulateColumns = false;
            layoutView.OptionsBehavior.FocusLeaveOnTab = true;
            layoutView.OptionsBehavior.EditorShowMode = EditorShowMode.MouseDownFocused;
            layoutView.OptionsBehavior.Editable = true;

            layoutView.OptionsFilter.AllowFilterEditor = true;
            layoutView.OptionsFilter.DefaultFilterEditorView = FilterEditorViewMode.VisualAndText;
            layoutView.OptionsFilter.AllowFilterIncrementalSearch = !AllowEdit || ReadOnlyEditors;
            layoutView.OptionsFilter.FilterEditorAggregateEditing = FilterControlAllowAggregateEditing.AggregateWithCondition;
            layoutView.OptionsFilter.FilterEditorUseMenuForOperandsAndOperators = false;

            layoutView.OptionsCarouselMode.CardCount = 8;
            layoutView.OptionsCarouselMode.BottomCardScale = 0.6f;

            layoutView.OptionsLayout.Columns.RemoveOldColumns = true;
            layoutView.OptionsLayout.Columns.AddNewColumns = true;
            layoutView.OptionsSelection.MultiSelect = true;

            ApplyHtmlFormatting();
            SubscribeLayoutViewEvents();
        }
        private LayoutView CreateLayoutView() {
            layoutView = CreateLayoutViewCore();
            return layoutView;
        }
        private void SelectRowByHandle(int rowHandle) {
            if (layoutView.IsValidRowHandle(rowHandle)) {
                try {
                    isRowFocusingForced = true;
                    LayoutViewUtils.SelectRowByHandle(layoutView, rowHandle);
                } finally {
                    isRowFocusingForced = false;
                }
            }
        }
        private void layoutView_CustomFieldValueStyle(object sender, DevExpress.XtraGrid.Views.Layout.Events.LayoutViewFieldValueStyleEventArgs e) {
            string propertyName = e.Column is XafLayoutViewColumn ? ((XafLayoutViewColumn)e.Column).PropertyName : e.Column.FieldName;
            OnCustomizeAppearance(new CustomizeAppearanceEventArgs(propertyName, new AppearanceObjectAdapter(e.Appearance, e), e.RowHandle));
        }
        private void layoutView_HideCustomization(object sender, EventArgs e) {
            OnEndCustomization();
        }
        private void layoutView_ShowCustomization(object sender, EventArgs e) {
            OnBeginCustomization();
        }
        private void layoutView_FilterEditorPopup(object sender, EventArgs e) {
            OnBeginCustomization();
        }
        private void layoutView_FilterEditorClosed(object sender, EventArgs e) {
            OnEndCustomization();
        }
        private void OnBeginCustomization() {
            if (BeginCustomization != null) {
                BeginCustomization(this, EventArgs.Empty);
            }
        }
        private void OnEndCustomization() {
            if (EndCustomization != null) {
                EndCustomization(this, EventArgs.Empty);
            }
        }
        private bool IsGroupRowHandle(int handle) {
            return handle < 0;
        }
        private void grid_HandleCreated(object sender, EventArgs e) {
            AssignDataSourceToControl(DataSource);
        }
        private void grid_KeyDown(object sender, KeyEventArgs e) {
            ProcessGridKeyDown(e);
        }
        private void SubmitActiveEditorChanges() {
            if ((LayoutView.ActiveEditor != null) && LayoutView.ActiveEditor.IsModified) {
                LayoutView.PostEditor();
                LayoutView.UpdateCurrentRow();
            }
        }
        private void grid_DoubleClick(object sender, EventArgs e) {
            ProcessMouseClick(e);
        }
        private void layoutView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            isForceSelectRow = e.Action == CollectionChangeAction.Add;
            OnSelectionChanged();
        }
        private void layoutView_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e) {
            if (!isRowFocusingForced && DataSource != null && !(DataSource is XPBaseCollection)) {
                prevFocusedRowHandle = e.PrevFocusedRowHandle;
            }
            OnFocusedObjectChanged();
        }
        private void layoutView_ColumnFilterChanged(object sender, EventArgs e) {
            if (!LayoutView.IsLoading) {
                OnFocusedObjectChanged();
            }
        }
        private void layoutView_Click(object sender, EventArgs e) {
            if (processSelectedItemBySingleClick) {
                ProcessMouseClick(e);
            }
        }
        private void layoutView_ValidateRow(object sender, ValidateRowEventArgs e) {
            if (e.Valid) {
                ValidateObjectEventArgs ea = new ValidateObjectEventArgs(FocusedObject, true);
                OnValidateObject(ea);
                e.Valid = ea.Valid;
                e.ErrorText = ea.ErrorText;
            }
        }
        private void layoutView_BeforeLeaveRow(object sender, RowAllowEventArgs e) {
            if (e.Allow)
                e.Allow = OnFocusedObjectChanging();
        }
        private void layoutView_EditorShowing(object sender, CancelEventArgs e) {
            activeEditor = null;
            string propertyName = layoutView.FocusedColumn.FieldName;
            RepositoryItem edit = layoutView.FocusedColumn.ColumnEdit;
            OnCustomizeAppearance(new CustomizeAppearanceEventArgs(propertyName, new CancelEventArgsAppearanceAdapter(e), layoutView.FocusedRowHandle));
            if (!e.Cancel) {
                if (edit != null) {
                    OnCustomizeAppearance(new CustomizeAppearanceEventArgs(propertyName, new AppearanceObjectAdapterWithReset(edit.Appearance, edit), layoutView.FocusedRowHandle));
                    edit.MouseDown += new MouseEventHandler(Editor_MouseDown);
                    edit.MouseUp += new MouseEventHandler(Editor_MouseUp);
                    RepositoryItemButtonEdit buttonEdit = edit as RepositoryItemButtonEdit;
                    if (buttonEdit != null) {
                        buttonEdit.ButtonPressed += new ButtonPressedEventHandler(ButtonEdit_ButtonPressed);
                    }
                    RepositoryItemBaseSpinEdit spinEdit = edit as RepositoryItemBaseSpinEdit;
                    if (spinEdit != null) {
                        spinEdit.Spin += new SpinEventHandler(SpinEdit_Spin);
                    }
                    edit.KeyDown += new KeyEventHandler(Editor_KeyDown);
                    activeEditor = edit;
                }
            }
        }
        private void layoutView_ShownEditor(object sender, EventArgs e) {
            if (popupMenu != null) {
                popupMenu.ResetPopupContextMenuSite();
            }
            PopupBaseEdit popupEdit = layoutView.ActiveEditor as PopupBaseEdit;
            if (popupEdit != null && activatedByMouse && popupEdit.Properties.ShowDropDown != ShowDropDown.Never) {
                popupEdit.ShowPopup();
            }
            activatedByMouse = false;
            LookupEdit editor = layoutView.ActiveEditor as LookupEdit;
            if (editor != null) {
                OnLookupEditCreated(editor);
            }
        }
        private void layoutView_HiddenEditor(object sender, EventArgs e) {
            if (popupMenu != null) {
                popupMenu.SetupPopupContextMenuSite();
            }
            LookupEdit editor = layoutView.ActiveEditor as LookupEdit;
            if (editor != null) {
                OnLookupEditCreated(editor);
            }
            if (activeEditor != null) {
                activeEditor.KeyDown -= new KeyEventHandler(Editor_KeyDown);
                activeEditor.MouseDown -= new MouseEventHandler(Editor_MouseDown);
                activeEditor.MouseUp -= new MouseEventHandler(Editor_MouseUp);
                RepositoryItemButtonEdit buttonEdit = activeEditor as RepositoryItemButtonEdit;
                if (buttonEdit != null) {
                    buttonEdit.ButtonPressed -= new ButtonPressedEventHandler(ButtonEdit_ButtonPressed);
                }
                RepositoryItemBaseSpinEdit spinEdit = activeEditor as RepositoryItemBaseSpinEdit;
                if (spinEdit != null) {
                    spinEdit.Spin -= new SpinEventHandler(SpinEdit_Spin);
                }
                activeEditor = null;
            }
        }
        private void layoutView_MouseDown(object sender, MouseEventArgs e) {
            LayoutView view = (LayoutView)sender;
            LayoutViewHitInfo hi = view.CalcHitInfo(new Point(e.X, e.Y));
            if (hi.RowHandle >= 0) {
                mouseDownTime = Environment.TickCount;
            }
            else {
                mouseDownTime = 0;
            }
            activatedByMouse = true;
        }
        private void layoutView_MouseUp(object sender, MouseEventArgs e) {
            mouseUpTime = Environment.TickCount;
        }
        private void Editor_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                Int32 currentTime = Environment.TickCount;
                if ((mouseDownTime <= mouseUpTime) && (mouseUpTime <= currentTime) && (currentTime - mouseDownTime < SystemInformation.DoubleClickTime)) {
                    OnProcessSelectedItem();
                    mouseDownTime = 0;
                }
            }
        }
        private void Editor_MouseUp(object sender, MouseEventArgs e) {
            mouseUpTime = Environment.TickCount;
        }
        private void Editor_KeyDown(object sender, KeyEventArgs e) {
            ProcessEditorKeyDown(e);
        }
        private void SpinEdit_Spin(object sender, SpinEventArgs e) {
            mouseDownTime = 0;
        }
        private void ButtonEdit_ButtonPressed(object sender, ButtonPressedEventArgs e) {
            mouseDownTime = 0;
        }
        private void DataController_ListChanged(object sender, ListChangedEventArgs e) {
            if ((grid != null) && (grid.FindForm() != null) && !grid.ContainsFocus) {
                IList dataSource = ListHelper.GetList(((BaseGridController)sender).DataSource);
                if (dataSource != null && dataSource.Count == 1 && e.ListChangedType == ListChangedType.ItemAdded) {
                    IEditableObject obj = dataSource[e.NewIndex] as IEditableObject;
                    if (obj != null) {
                        obj.EndEdit();
                    }
                }
            }
            if (e.ListChangedType == ListChangedType.ItemChanged) {
                prevFocusedRowHandle = layoutView.FocusedRowHandle;
            }
            if (e.ListChangedType == ListChangedType.ItemDeleted && layoutView.FocusedRowHandle != BaseListSourceDataController.NewItemRow) {
                if (!layoutView.IsValidRowHandle(prevFocusedRowHandle)) {
                    prevFocusedRowHandle--;
                }
                SelectRowByHandle(prevFocusedRowHandle);
                OnFocusedObjectChanged();
            }
            if (layoutView != null) {
                if (e.ListChangedType == ListChangedType.Reset) {
                    if (layoutView.IsServerMode) {
                        SelectRowByHandle(prevFocusedRowHandle);
                    }
                    if (layoutView.SelectedRowsCount == 0) {
                        LayoutViewUtils.SelectFocusedRow(layoutView);
                    }
                    OnFocusedObjectChanged();
                }
            }
        }
        private void repositoryItem_EditValueChanging(object sender, ChangingEventArgs e) {
            BaseEdit editor = GetEditor(sender);
            if ((editor != null) && (editor.InplaceType == InplaceType.Grid)) {
                OnObjectChanged();
            }
        }
        private void OnPrintableChanged() {
            if (PrintableChanged != null) {
                PrintableChanged(this, new PrintableChangedEventArgs(printable));
            }
        }
        private void grid_VisibleChanged(object sender, EventArgs e) {
            AssignDataSourceToControl(DataSource);
            if (grid.Visible) {
                GridColumn defaultColumn = GetDefaultColumn();
                if (defaultColumn != null)
                    layoutView.FocusedColumn = defaultColumn;
            }
        }
        private void grid_ParentChanged(object sender, EventArgs e) {
            if (grid.Parent != null) {
                grid.ForceInitialize();
            }
        }
        private GridColumn GetDefaultColumn() {
            GridColumn result = null;
            if (Model != null) {
                ITypeInfo classType = Model.ModelClass.TypeInfo;
                if (classType != null) {
                    IMemberInfo defaultMember = classType.DefaultMember;
                    if (defaultMember != null) {
                        result = LayoutView.Columns[defaultMember.Name];
                    }
                }
            }
            return result == null || !result.Visible ? null : result;
        }
        private void RemoveColumnInfo(GridColumn column) {
            if (column is XafLayoutViewColumn) {
                IModelColumn columnInfo = Model.Columns[((XafLayoutViewColumn)column).Model.Id];
                if (columnInfo != null) {
                    Model.Columns.Remove(columnInfo);
                }
            }
        }
        private void UpdateAllowEditGridViewAndColumnsOptions() {
            if (layoutView != null) {
                layoutView.BeginUpdate();
                foreach (GridColumn column in layoutView.Columns) {
                    column.OptionsColumn.AllowEdit = column.ColumnEdit != null && IsDataShownOnDropDownWindow(column.ColumnEdit) ? true : AllowEdit;
                    if (column.ColumnEdit != null) {
                        column.ColumnEdit.ReadOnly = !AllowEdit || ReadOnlyEditors;
                    }
                    if (AllowEdit) {
                        layoutView.ValidateRow += new ValidateRowEventHandler(layoutView_ValidateRow);
                    }
                    else {
                        layoutView.ValidateRow -= new ValidateRowEventHandler(layoutView_ValidateRow);
                    }
                }
                layoutView.EndUpdate();
            }
        }
        private IMemberInfo FindMemberInfoForColumn(IModelColumn columnInfo) {
            return ObjectTypeInfo.FindMember(columnInfo.PropertyName);
        }
        protected virtual void OnCustomizeAppearance(CustomizeAppearanceEventArgs args) {
            if (CustomizeAppearance != null) {
                CustomizeAppearanceEventArgs workArgs;
                bool canCustomizeAppearance = true;
                object rowObj = LayoutViewUtils.GetRow(layoutView, (int)args.ContextObject);
                workArgs = new CustomizeAppearanceEventArgs(args.Name, args.Item, rowObj);
                if (canCustomizeAppearance) {
                    CustomizeAppearance(this, workArgs);
                }
            }
        }
        protected virtual XafLayoutView CreateLayoutViewCore() {
            return new XafLayoutView();
        }
        protected virtual void ProcessMouseClick(EventArgs e) {
            if (!selectedItemExecuting) {
                if (LayoutView.FocusedRowHandle >= 0) {
                    DXMouseEventArgs args = DXMouseEventArgs.GetMouseArgs(grid, e);
                    LayoutViewHitInfo hitInfo = LayoutView.CalcHitInfo(args.Location);
                    if (hitInfo.InCard
                        && (hitInfo.HitTest == LayoutViewHitTest.FieldCaption
                        || hitInfo.HitTest == LayoutViewHitTest.CardCaption
                        || hitInfo.HitTest == LayoutViewHitTest.Field
                        || hitInfo.HitTest == LayoutViewHitTest.FieldValue
                        || hitInfo.HitTest == LayoutViewHitTest.LayoutItem)
                    ) {
                        args.Handled = true;
                        OnProcessSelectedItem();
                    }
                }
            }
        }
        protected virtual void ProcessGridKeyDown(KeyEventArgs e) {
            if (FocusedObject != null && e.KeyCode == Keys.Enter) {
                if (LayoutView.ActiveEditor == null && !ReadOnlyEditors) {
                    OnProcessSelectedItem();
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                }
                else {
                    if (ReadOnlyEditors && LayoutView.ActiveEditor == null) {
                        if (layoutView.IsLastColumnFocused) {
                            layoutView.UpdateCurrentRow();
                            e.Handled = true;
                        }
                        else {
                            LayoutView.FocusedColumn =
                                LayoutView.GetVisibleColumn(1 + layoutView.VisibleColumns.IndexOf(LayoutView.FocusedColumn));
                            e.Handled = true;
                        }
                    }
                    else {
                        PopupBaseEdit popupEdit = LayoutView.ActiveEditor as PopupBaseEdit;
                        if ((popupEdit == null) || (!popupEdit.IsPopupOpen)) {
                            SubmitActiveEditorChanges();
                            e.Handled = true;
                        }
                    }
                }
            }
        }
        protected virtual void ProcessEditorKeyDown(KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                SubmitActiveEditorChanges();
            }
        }
        protected virtual void OnColumnCreated(GridColumn column, IModelColumn columnInfo) {
            if (ColumnCreated != null) {
                ColumnCreatedEventArgs args = new ColumnCreatedEventArgs(column, columnInfo);
                ColumnCreated(this, args);
            }
        }
        protected override void OnFocusedObjectChanged() {
            base.OnFocusedObjectChanged();
            focusedChangedRaised = true;
        }
        protected override void OnSelectionChanged() {
            base.OnSelectionChanged();
            selectedChangedRaised = true;
            if (LayoutView.SelectedRowsCount == 0 && isForceSelectRow) {
                LayoutViewUtils.SelectFocusedRow(LayoutView);
            }
        }
        protected virtual void OnGridDataSourceChanging() {
            if (GridDataSourceChanging != null) {
                GridDataSourceChanging(this, EventArgs.Empty);
            }
        }
        private void SubscribeGridControlEvents() {
            grid.HandleCreated += new EventHandler(grid_HandleCreated);
            grid.KeyDown += new KeyEventHandler(grid_KeyDown);
            grid.DoubleClick += new EventHandler(grid_DoubleClick);
            grid.ParentChanged += new EventHandler(grid_ParentChanged);
            grid.VisibleChanged += new EventHandler(grid_VisibleChanged);
        }
        protected override object CreateControlsCore() {
            if (grid == null) {
                grid = new GridControl();
                ((ISupportInitialize)(grid)).BeginInit();
                try {
                    SubscribeGridControlEvents();
                    grid.MinimumSize = new Size(100, 75);
                    grid.Dock = DockStyle.Fill;
                    grid.AllowDrop = true;
                    grid.Height = 100;
                    grid.TabStop = true;
                    grid.MainView = CreateLayoutView();
                    grid.Tag = EasyTestTagHelper.FormatTestTable(Name);
                    SetupLayoutView();
                    ApplyModel();
                } finally {
                    ((ISupportInitialize)(grid)).EndInit();
                    grid.ForceInitialize();
                }
                Printable = Grid;
            }
            return grid;
        }
        protected override IModelSynchronizable CreateModelSynchronizer() {
            return new LayoutViewListEditorSynchronizer(this, Model);
        }
        public override void ApplyModel() {
            Grid.BeginUpdate();
            LayoutView.BeginInit();
            try {
                base.ApplyModel();
            } finally {
                LayoutView.EndInit();
                Grid.EndUpdate();
            }
        }
        protected override void OnProcessSelectedItem() {
            selectedItemExecuting = true;
            try {
                if ((layoutView != null) && (layoutView.ActiveEditor != null)) {
                    BindingHelper.EndCurrentEdit(Grid);
                }
                base.OnProcessSelectedItem();
            } finally {
                selectedItemExecuting = false;
            }
        }
        protected internal bool IsDataShownOnDropDownWindow(RepositoryItem repositoryItem) {
            return DXPropertyEditor.RepositoryItemsTypesWithMandatoryButtons.Contains(repositoryItem.GetType());
        }
        protected override void AssignDataSourceToControl(Object dataSource) {
            if (grid != null && grid.DataSource != dataSource) {
                if (grid.IsHandleCreated && grid.Visible) {
                    focusedChangedRaised = false;
                    selectedChangedRaised = false;
                    OnGridDataSourceChanging();
                    grid.BeginUpdate();
                    try {
                        if (layoutView.DataController != null) {
                            layoutView.DataController.ListChanged -= new ListChangedEventHandler(DataController_ListChanged);
                        }
                        layoutView.CancelCurrentRowEdit();
                        grid.DataSource = dataSource;
                        if (layoutView.DataController != null) {
                            layoutView.DataController.ListChanged += new ListChangedEventHandler(DataController_ListChanged);
                        }
                    } finally {
                        grid.EndUpdate();
                    }
                    if (!selectedChangedRaised) {
                        OnSelectionChanged();
                    }
                    if (!focusedChangedRaised) {
                        OnFocusedObjectChanged();
                    }
                }
                else {
                    grid.DataSource = null;
                }
            }
        }
        protected override void OnProtectedContentTextChanged() {
            base.OnProtectedContentTextChanged();
            repositoryFactory.ProtectedContentText = ProtectedContentText;
        }
        protected override void OnAllowEditChanged() {
            UpdateAllowEditGridViewAndColumnsOptions();
            base.OnAllowEditChanged();
        }
        protected override void OnErrorMessagesChanged() {
            base.OnErrorMessagesChanged();
            if (grid != null && layoutView != null) {
                grid.Refresh();
                layoutView.LayoutChanged();
            }
        }
        private void UnsubscribeGridControlEvents() {
            grid.VisibleChanged -= new EventHandler(grid_VisibleChanged);
            grid.KeyDown -= new KeyEventHandler(grid_KeyDown);
            grid.HandleCreated -= new EventHandler(grid_HandleCreated);
            grid.DoubleClick -= new EventHandler(grid_DoubleClick);
            grid.ParentChanged -= new EventHandler(grid_ParentChanged);
        }
        public override void Dispose() {
            ColumnCreated = null;
            GridDataSourceChanging = null;
            if (popupMenu != null) {
                popupMenu.Dispose();
                popupMenu = null;
            }
            if (layoutView != null) {
                UnsubscribeLayoutViewEvents();
                layoutView.Dispose();
                layoutView = null;
            }
            if (grid != null) {
                grid.DataSource = null;
                UnsubscribeGridControlEvents();
                grid.RepositoryItems.Clear();
                grid.Dispose();
                grid = null;
            }
            base.Dispose();
        }
        public override void BreakLinksToControls() {
            base.BreakLinksToControls();
            if (grid != null) {
                grid.Dispose();
                grid = null;
            }
        }
        protected override ColumnWrapper AddColumnCore(IModelColumn columnInfo) {
            XafLayoutViewColumn column = new XafLayoutViewColumn(ObjectTypeInfo, this);
            LayoutView.Columns.Add(column);
            IMemberInfo memberInfo = FindMemberInfoForColumn(columnInfo);
            if (memberInfo != null) {
                column.FieldName = memberInfo.BindingName;
                if (memberInfo.MemberType.IsEnum) {
                    column.SortMode = ColumnSortMode.Value;
                }
                else if (!SimpleTypes.IsSimpleType(memberInfo.MemberType)) {
                    column.SortMode = ColumnSortMode.DisplayText;
                }
                if (SimpleTypes.IsClass(memberInfo.MemberType)) {
                    column.FilterMode = ColumnFilterMode.DisplayText;
                }
                else {
                    column.FilterMode = ColumnFilterMode.Value;
                }
            }
            else {
                column.FieldName = columnInfo.PropertyName;
            }
            column.ApplyModel(columnInfo);
            if (memberInfo != null) {
                if (repositoryFactory != null) {
                    bool isGranted = DataManipulationRight.CanRead(ObjectType, columnInfo.PropertyName, null, collectionSource);
                    RepositoryItem repositoryItem = null;
                    repositoryItem = repositoryFactory.CreateRepositoryItem(!isGranted, columnInfo, ObjectType);
                    if (repositoryItem != null) {
                        grid.RepositoryItems.Add(repositoryItem);
                        repositoryItem.EditValueChanging += new ChangingEventHandler(repositoryItem_EditValueChanging);
                        column.ColumnEdit = repositoryItem;
                        column.OptionsColumn.AllowEdit = IsDataShownOnDropDownWindow(repositoryItem) ? true : AllowEdit;
                        column.AppearanceCell.Options.UseTextOptions = true;
                        column.AppearanceCell.TextOptions.HAlignment = WinAlignmentProvider.GetAlignment(memberInfo.MemberType);
                        repositoryItem.ReadOnly |= !AllowEdit || ReadOnlyEditors;
                        if (repositoryItem is ILookupEditRepositoryItem) {
                            column.FilterMode = ColumnFilterMode.Value;
                            column.OptionsFilter.AutoFilterCondition = AutoFilterCondition.Equals;
                            column.OptionsFilter.FilterBySortField = DefaultBoolean.False;
                        }
                        if (repositoryItem is RepositoryItemMemoExEdit) {
                            column.OptionsColumn.AllowSort = DefaultBoolean.True;
                        }
                        if (repositoryItem is RepositoryItemRtfEditEx) {
                            column.FilterMode = ColumnFilterMode.DisplayText;
                        }
                        if (Model.UseServerMode) {
                            column.FieldNameSortGroup = new ObjectEditorHelperBase(XafTypesInfo.Instance.FindTypeInfo(columnInfo.ModelMember.Type), columnInfo).GetFullDisplayMemberName(columnInfo.PropertyName);
                        }
                        if (!repositoryItem.DisplayFormat.IsEmpty) {
                            column.DisplayFormat.FormatType = repositoryItem.DisplayFormat.FormatType;
                            column.DisplayFormat.FormatString = repositoryItem.DisplayFormat.FormatString;
                        }
                    }
                }
                if ((column.ColumnEdit == null) && !typeof(IList).IsAssignableFrom(memberInfo.MemberType)) {
                    column.OptionsColumn.AllowEdit = false;
                    column.FieldName = GetDisplayablePropertyName(columnInfo.PropertyName);
                }
            }
            if (column.LayoutViewField == null) {
                column.LayoutViewField = new LayoutViewField();
            }
            column.LayoutViewField.Name = column.FieldName;
            column.LayoutViewField.ColumnName = column.FieldName;
            if (!LayoutView.TemplateCard.Items.Contains(column.LayoutViewField))
                LayoutView.TemplateCard.Add(column.LayoutViewField);
            OnColumnCreated(column, columnInfo);
            if (!grid.IsLoading && layoutView.DataController.Columns.GetColumnIndex(column.FieldName) == -1) {
                layoutView.DataController.RePopulateColumns();
            }
            return new LayoutViewColumnWrapper(column);
        }
        public override void RemoveColumn(ColumnWrapper column) {
            GridColumn gridColumn = ((LayoutViewColumnWrapper)column).Column;
            if (LayoutView != null && LayoutView.Columns.Contains(gridColumn)) {
                RemoveColumnInfo(gridColumn);
                LayoutView.Columns.Remove(gridColumn);
            }
            else {
                throw new ArgumentException(string.Format(SystemExceptionLocalizer.GetExceptionMessage(ExceptionId.GridColumnDoesNotExist), column.PropertyName), "PropertyName");
            }
        }
        public override void Refresh() {
            if (grid != null) {
                prevFocusedRowHandle = layoutView.FocusedRowHandle;
                grid.RefreshDataSource();
                SelectRowByHandle(prevFocusedRowHandle);
            }
        }
        public override IList GetSelectedObjects() {
            ArrayList selectedObjects = new ArrayList();
            if (LayoutView != null) {
                int[] selectedRows = LayoutView.GetSelectedRows();
                if ((selectedRows != null) && (selectedRows.Length > 0)) {
                    foreach (int rowHandle in selectedRows) {
                        if (!IsGroupRowHandle(rowHandle)) {
                            object obj = LayoutViewUtils.GetRow(CollectionSource, LayoutView, rowHandle);
                            if (obj != null) {
                                selectedObjects.Add(obj);
                            }
                        }
                    }
                }
            }
            return (object[])selectedObjects.ToArray(typeof(object));
        }
        protected override bool HasProtectedContent(string propertyName) {
            return !(ObjectTypeInfo.FindMember(propertyName) == null || DataManipulationRight.CanRead(ObjectType, propertyName, null, collectionSource));
        }
        public IPrintable GetPrintable() {
            return grid;
        }
        public override string[] RequiredProperties {
            get {
                List<string> result = new List<string>();
                if (Model != null) {
                    foreach (IModelColumn columnInfo in Model.Columns) {
                        if ((columnInfo.Index > -1) || (columnInfo.GroupIndex > -1)) {
                            IMemberInfo memberInfo = FindMemberInfoForColumn(columnInfo);
                            if (memberInfo != null) {
                                result.Add(memberInfo.BindingName);
                            }
                        }
                    }
                }
                return result.ToArray();
            }
        }
        public override IContextMenuTemplate ContextMenuTemplate {
            get { return popupMenu; }
        }
        public override string Name {
            get { return base.Name; }
            set {
                base.Name = value;
                if (grid != null)
                    grid.Tag = EasyTestTagHelper.FormatTestTable(Name);
            }
        }
        public override object FocusedObject {
            get {
                object result = null;
                if (LayoutView != null)
                    result = LayoutViewUtils.GetFocusedRowObject(CollectionSource, LayoutView);
                return result;
            }
            set {
                if (value != null && value != DBNull.Value && layoutView != null && DataSource != null) {
                    LayoutViewUtils.SelectRowByHandle(layoutView, layoutView.GetRowHandle(List.IndexOf(value)));
                    if (LayoutViewUtils.HasValidRowHandle(layoutView))
                        layoutView.ExpandCard(layoutView.FocusedRowHandle);
                }
            }
        }
        public override SelectionType SelectionType {
            get { return SelectionType.Full; }
        }
        public RepositoryEditorsFactory RepositoryFactory {
            get { return repositoryFactory; }
            set { repositoryFactory = value; }
        }
        public GridControl Grid {
            get { return grid; }
        }
        public XafLayoutView LayoutView {
            get { return layoutView; }
        }
        public Boolean ScrollOnMouseMove {
            get { return scrollOnMouseMove; }
            set { scrollOnMouseMove = value; }
        }
        public bool ReadOnlyEditors {
            get { return readOnlyEditors; }
            set {
                if (readOnlyEditors != value) {
                    readOnlyEditors = value;
                    AllowEdit = !readOnlyEditors;
                }
            }
        }
        public override IList<ColumnWrapper> Columns {
            get {
                List<ColumnWrapper> result = new List<ColumnWrapper>();
                if (LayoutView != null) {
                    foreach (GridColumn column in LayoutView.Columns) {
                        if (column is XafLayoutViewColumn) {
                            result.Add(new LayoutViewColumnWrapper((XafLayoutViewColumn)column));
                        }
                    }
                }
                return result;
            }
        }
        public override Boolean IsServerModeSupported {
            get { return true; }
        }
        public event EventHandler GridDataSourceChanging;
        public event EventHandler<ColumnCreatedEventArgs> ColumnCreated;
        #region IDXPopupMenuHolder Members
        public Control PopupSite {
            get { return Grid; }
        }
        public bool CanShowPopupMenu(Point position) {
            LayoutViewHitTest hitTest = layoutView.CalcHitInfo(grid.PointToClient(position)).HitTest;
            return
                 ((hitTest == LayoutViewHitTest.Card)
                 || (hitTest == LayoutViewHitTest.Field)
                 || (hitTest == LayoutViewHitTest.FieldValue)
                 || (hitTest == LayoutViewHitTest.FieldCaption)
                 || (hitTest == LayoutViewHitTest.CardCaption)
                 || (hitTest == LayoutViewHitTest.LayoutItem)
                 || (hitTest == LayoutViewHitTest.None));
        }
        public void SetMenuManager(IDXMenuManager manager) {
            if (grid != null) {
                grid.MenuManager = manager;
            }
        }
        #endregion
        #region IControlOrderProvider Members
        public int GetIndexByObject(Object obj) {
            int index = -1;
            if ((DataSource != null) && (layoutView != null)) {
                int dataSourceIndex = List.IndexOf(obj);
                index = layoutView.GetRowHandle(dataSourceIndex);
                if (index == GridControl.InvalidRowHandle) {
                    index = -1;
                }
            }
            return index;
        }
        public Object GetObjectByIndex(int index) {
            if ((layoutView != null) && (layoutView.DataController != null)) {
                return layoutView.GetRow(index);
            }
            return null;
        }
        public IList GetOrderedObjects() {
            List<Object> list = new List<Object>();
            if (layoutView != null && !layoutView.IsServerMode) {
                for (int i = 0; i < layoutView.DataRowCount; i++) {
                    list.Add(layoutView.GetRow(i));
                }
            }
            return list;
        }
        #endregion
        #region IComplexListEditor Members
        public virtual void Setup(CollectionSourceBase collectionSource, XafApplication application) {
            this.collectionSource = collectionSource;
            this.application = application;
            repositoryFactory = new RepositoryEditorsFactory(application, collectionSource.ObjectSpace);
        }
        #endregion
        internal CollectionSourceBase CollectionSource {
            get { return collectionSource; }
        }
        #region ILookupListEditor Members
        public Boolean ProcessSelectedItemBySingleClick {
            get { return processSelectedItemBySingleClick; }
            set { processSelectedItemBySingleClick = value; }
        }
        public Boolean TrackMousePosition {
            get { return trackMousePosition; }
            set { trackMousePosition = value; }
        }
        public event EventHandler BeginCustomization;
        public event EventHandler EndCustomization;
        #endregion
        #region ISupportAppearanceCustomization Members
        public event EventHandler<CustomizeAppearanceEventArgs> CustomizeAppearance;
        #endregion
        #region IHtmlFormattingSupport Members
        private bool htmlFormattingEnabled;
        public void SetHtmlFormattingEnabled(bool htmlFormattingEnabled) {
            this.htmlFormattingEnabled = htmlFormattingEnabled;
            if (LayoutView != null) {
                ApplyHtmlFormatting();
            }
        }
        private void ApplyHtmlFormatting() {
            layoutView.Appearance.HeaderPanel.TextOptions.WordWrap = htmlFormattingEnabled ? WordWrap.Wrap : WordWrap.Default;
        }
        #endregion
        #region IFocusedElementCaptionProvider Members
        object IFocusedElementCaptionProvider.FocusedElementCaption {
            get {
                if (LayoutView != null)
                    return LayoutView.GetFocusedDisplayText();
                return null;
            }
        }
        #endregion
        #region IExportableEditor Members
        public IList<PrintingSystemCommand> ExportTypes {
            get {
                IList<PrintingSystemCommand> exportTypes = new List<PrintingSystemCommand>();
                exportTypes.Add(PrintingSystemCommand.ExportXls);
                exportTypes.Add(PrintingSystemCommand.ExportHtm);
                exportTypes.Add(PrintingSystemCommand.ExportTxt);
                exportTypes.Add(PrintingSystemCommand.ExportMht);
                exportTypes.Add(PrintingSystemCommand.ExportPdf);
                exportTypes.Add(PrintingSystemCommand.ExportRtf);
                exportTypes.Add(PrintingSystemCommand.ExportGraphic);
                return exportTypes;
            }
        }
        public IPrintable Printable {
            get { return printable; }
            set {
                if (printable != value) {
                    printable = value;
                    OnPrintableChanged();
                }
            }
        }
        public event EventHandler<PrintableChangedEventArgs> PrintableChanged;
        #endregion
        #region ILookupEditProvider Members
        private event EventHandler<LookupEditProviderEventArgs> lookupEditCreated;
        private event EventHandler<LookupEditProviderEventArgs> lookupEditHide;
        protected void OnLookupEditCreated(LookupEdit control) {
            if (lookupEditCreated != null) {
                lookupEditCreated(this, new LookupEditProviderEventArgs(control));
            }
        }
        protected void OnLookupEditHide(LookupEdit control) {
            if (lookupEditHide != null) {
                lookupEditHide(this, new LookupEditProviderEventArgs(control));
            }
        }
        event EventHandler<LookupEditProviderEventArgs> ILookupEditProvider.ControlCreated {
            add { lookupEditCreated += value; }
            remove { lookupEditCreated -= value; }
        }
        event EventHandler<LookupEditProviderEventArgs> ILookupEditProvider.HideControl {
            add { lookupEditHide += value; }
            remove { lookupEditHide -= value; }
        }
        #endregion
    }

}

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms
Imports DevExpress.Data
Imports DevExpress.Utils
Imports DevExpress.Utils.Menu
Imports DevExpress.Xpo
Imports DevExpress.XtraEditors
Imports DevExpress.XtraEditors.Controls
Imports DevExpress.XtraEditors.Repository
Imports DevExpress.XtraGrid
Imports DevExpress.XtraGrid.Columns
Imports DevExpress.XtraGrid.Views.Base
Imports DevExpress.XtraGrid.Views.Grid
Imports DevExpress.XtraPrinting
Imports DevExpress.ExpressApp
Imports DevExpress.ExpressApp.Editors
Imports DevExpress.ExpressApp.NodeWrappers
Imports DevExpress.ExpressApp.SystemModule
Imports DevExpress.ExpressApp.Templates
Imports DevExpress.ExpressApp.Utils
Imports DevExpress.ExpressApp.Win.Controls
Imports DevExpress.ExpressApp.Win.Core
Imports DevExpress.Persistent.Base
Imports DevExpress.ExpressApp.Localization
Imports DevExpress.XtraGrid.Views.Layout
Imports DevExpress.ExpressApp.Win
Imports DevExpress.ExpressApp.Win.Editors
Imports DevExpress.XtraGrid.Views.Layout.ViewInfo
Imports DevExpress.ExpressApp.DC

Namespace WinSample.Module.Win
	Public Class XafLayotView
		Inherits LayoutView
		Private errorMessages_Renamed As ErrorMessages
		Private Function GetFocusedObject() As Object
			Return Me.GetRow(Me.FocusedRowHandle)
		End Function
		Protected Sub RaiseFilterEditorPopup()
			RaiseEvent FilterEditorPopup(Me, EventArgs.Empty)
		End Sub
		Protected Sub RaiseFilterEditorClosed()
			RaiseEvent FilterEditorClosed(Me, EventArgs.Empty)
		End Sub
		Protected Overrides Sub RaiseInvalidRowException(ByVal ex As InvalidRowExceptionEventArgs)
			ex.ExceptionMode = ExceptionMode.ThrowException
			MyBase.RaiseInvalidRowException(ex)
		End Sub
		Protected Overrides Sub OnActiveEditor_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
			If ActiveEditor IsNot Nothing Then
				MyBase.OnActiveEditor_MouseDown(sender, e)
			End If
		End Sub
		Public Sub ForceLoaded()
			OnLoaded()
		End Sub
		Public ReadOnly Property IsFirstColumnInFirstRowFocused() As Boolean
			Get
				Return (FocusedRowHandle = 0) AndAlso (FocusedColumn Is GetVisibleColumn(0))
			End Get
		End Property
		Public ReadOnly Property IsLastColumnInLastRowFocused() As Boolean
			Get
				Return (FocusedRowHandle = RowCount - 1) AndAlso IsLastColumnFocused
			End Get
		End Property
		Public ReadOnly Property IsLastColumnFocused() As Boolean
			Get
				Return (FocusedColumn Is GetVisibleColumn(VisibleColumns.Count - 1))
			End Get
		End Property
		Public Property ErrorMessages() As ErrorMessages
			Get
				Return errorMessages_Renamed
			End Get
			Set(ByVal value As ErrorMessages)
				errorMessages_Renamed = value
			End Set
		End Property
		Public Event FilterEditorPopup As EventHandler
		Public Event FilterEditorClosed As EventHandler
	End Class
	Public Class ColumnCreatedEventArgs
		Inherits EventArgs
		Private column_Renamed As LayoutViewColumn
		Private columnInfo_Renamed As ColumnInfoNodeWrapper
		Public Sub New(ByVal column As LayoutViewColumn, ByVal columnInfo As ColumnInfoNodeWrapper)
			Me.column_Renamed = column
			Me.columnInfo_Renamed = columnInfo
		End Sub
		Public Property Column() As LayoutViewColumn
			Get
				Return column_Renamed
			End Get
			Set(ByVal value As LayoutViewColumn)
				column_Renamed = value
			End Set
		End Property
		Public Property ColumnInfo() As ColumnInfoNodeWrapper
			Get
				Return columnInfo_Renamed
			End Get
			Set(ByVal value As ColumnInfoNodeWrapper)
				columnInfo_Renamed = value
			End Set
		End Property
	End Class
	Public Class CustomCreateColumnEventArgs
		Inherits HandledEventArgs
		Private column_Renamed As LayoutViewColumn
		Private columnInfo_Renamed As ColumnInfoNodeWrapper
		Private repositoryFactory_Renamed As RepositoryEditorsFactory
		Public Sub New(ByVal column As LayoutViewColumn, ByVal columnInfo As ColumnInfoNodeWrapper, ByVal repositoryFactory As RepositoryEditorsFactory)
			Me.column_Renamed = column
			Me.columnInfo_Renamed = columnInfo
			Me.repositoryFactory_Renamed = repositoryFactory
		End Sub
		Public Property Column() As LayoutViewColumn
			Get
				Return column_Renamed
			End Get
			Set(ByVal value As LayoutViewColumn)
				column_Renamed = value
			End Set
		End Property
		Public Property ColumnInfo() As ColumnInfoNodeWrapper
			Get
				Return columnInfo_Renamed
			End Get
			Set(ByVal value As ColumnInfoNodeWrapper)
				columnInfo_Renamed = value
			End Set
		End Property
		Public Property RepositoryFactory() As RepositoryEditorsFactory
			Get
				Return repositoryFactory_Renamed
			End Get
			Set(ByVal value As RepositoryEditorsFactory)
				repositoryFactory_Renamed = value
			End Set
		End Property
	End Class
	Public Class LayoutViewGridListEditor
		Inherits ListEditor
		Implements IControlOrderProvider, IDXPopupMenuHolder, IComplexListEditor, IPrintableSource, ILookupListEditor
		Public Const IsGroupPanelVisible As String = "IsGroupPanelVisible"
		Public Const ActiveFilterString As String = "ActiveFilterString"
		Public Const IsFooterVisible As String = "IsFooterVisible"
		Public Const IsActiveFilterEnabled As String = "IsActiveFilterEnabled"
		Public Const DragEnterCustomCodeId As String = "DragEnter"
		Public Const DragDropCustomCodeId As String = "DragDrop"
		Public Const ColumnDefaultWidth As Integer = 50
		Private repositoryFactory_Renamed As RepositoryEditorsFactory
		Private editMode_Renamed As EditMode = EditMode.Editable
		Private grid_Renamed As GridControl
		Private layoutView_Renamed As XafLayotView
		Private mouseDownTime As Integer
		Private mouseUpTime As Integer
		Private activatedByMouse As Boolean = False
		Private focusedChangedRaised As Boolean
		Private selectedChangedRaised As Boolean
		Private isForceSelectRow As Boolean
		Private visibleIndexOnChanged As Integer
		Private activeEditor As RepositoryItem
		Private columnsProperties As Dictionary(Of LayoutViewColumn, String) = New Dictionary(Of LayoutViewColumn, String)()
		Private popupMenu As ActionsDXPopupMenu
		Private processSelectedItemBySingleClick_Renamed As Boolean
		Private moveRowFocusSpeedLimiter As New TimeAutoLatch()
		Private selectedItemActionExecuting As Boolean = False
		Private Function CreateLayotView() As XafLayotView
			layoutView_Renamed = New XafLayotView()
			layoutView_Renamed.TemplateCard = New LayoutViewCard()
			layoutView_Renamed.ErrorMessages = ErrorMessages
			AddHandler layoutView_Renamed.ShowingEditor, AddressOf LayoutView_EditorShowing
			AddHandler layoutView_Renamed.ShownEditor, AddressOf LayoutView_ShownEditor
			AddHandler layoutView_Renamed.HiddenEditor, AddressOf LayoutView_HiddenEditor
			AddHandler layoutView_Renamed.MouseDown, AddressOf LayoutView_MouseDown
			AddHandler layoutView_Renamed.MouseUp, AddressOf LayoutView_MouseUp
			AddHandler layoutView_Renamed.FocusedRowChanged, AddressOf LayoutView_FocusedRowChanged
			AddHandler layoutView_Renamed.SelectionChanged, AddressOf LayoutView_SelectionChanged
			AddHandler layoutView_Renamed.Click, AddressOf LayoutView_Click
			AddHandler layoutView_Renamed.MouseWheel, AddressOf LayoutView_MouseWheel
			If editMode_Renamed = EditMode.Editable Then
				AddHandler layoutView_Renamed.ValidateRow, AddressOf LayoutView_ValidateRow
				AddHandler layoutView_Renamed.InitNewRow, AddressOf LayoutView_InitNewRow
			End If
			layoutView_Renamed.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.Click
			layoutView_Renamed.OptionsBehavior.Editable = True
			layoutView_Renamed.OptionsBehavior.AutoSelectAllInEditor = False
			layoutView_Renamed.OptionsBehavior.AutoPopulateColumns = False
			layoutView_Renamed.OptionsBehavior.FocusLeaveOnTab = True
			layoutView_Renamed.OptionsSelection.MultiSelect = True
			layoutView_Renamed.ShowButtonMode = ShowButtonModeEnum.ShowOnlyInEditor
			layoutView_Renamed.ActiveFilterEnabled = Model.Node.GetAttributeBoolValue(IsActiveFilterEnabled, True)
			Return layoutView_Renamed
		End Function
		Private Sub LayoutView_InitNewRow(ByVal sender As Object, ByVal e As InitNewRowEventArgs)
			OnNewObjectCreated()
		End Sub
		Private Sub LayoutView_MouseWheel(ByVal sender As Object, ByVal e As MouseEventArgs)
			moveRowFocusSpeedLimiter.Reset()
		End Sub
		Private Sub LayoutView_HideCustomizationForm(ByVal sender As Object, ByVal e As EventArgs)
			RaiseEvent EndCustomization(Me, EventArgs.Empty)
		End Sub
		Private Sub LayoutView_ShowCustomizationForm(ByVal sender As Object, ByVal e As EventArgs)
			RaiseEvent BeginCustomization(Me, EventArgs.Empty)
		End Sub
		Private Function IsGroupRowHandle(ByVal handle As Integer) As Boolean
			Return handle < 0
		End Function
		Private Sub grid_HandleCreated(ByVal sender As Object, ByVal e As EventArgs)
			AssignDataSourceToControl(Me.DataSource)
		End Sub
		Private Sub grid_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs)
			If FocusedObject IsNot Nothing AndAlso e.KeyCode = Keys.Enter Then
				If (editMode_Renamed <> EditMode.ReadOnlyEditors OrElse (LayoutView.ActiveEditor Is Nothing)) Then
					Me.OnProcessSelectedItem()
					e.SuppressKeyPress = True
					e.Handled = True
				Else
					If (editMode_Renamed <> EditMode.ReadOnly) AndAlso (LayoutView.ActiveEditor Is Nothing) Then
						If layoutView_Renamed.IsLastColumnFocused Then
							layoutView_Renamed.UpdateCurrentRow()
							e.Handled = True
						Else
							LayoutView.FocusedColumn = LayoutView.GetVisibleColumn(1 + layoutView_Renamed.VisibleColumns.IndexOf(LayoutView.FocusedColumn))
							e.Handled = True
						End If
					Else
						Dim popupEdit As PopupBaseEdit = TryCast(LayoutView.ActiveEditor, PopupBaseEdit)
						If (popupEdit Is Nothing) OrElse ((Not popupEdit.IsPopupOpen)) Then
							SubmitActiveEditorChanges()
							e.Handled = True
						End If
					End If
				End If
			End If
		End Sub
		Private Sub SubmitActiveEditorChanges()
			If (LayoutView.ActiveEditor IsNot Nothing) AndAlso LayoutView.ActiveEditor.IsModified Then
				LayoutView.PostEditor()
				LayoutView.UpdateCurrentRow()
			End If
		End Sub
		Private Sub grid_DoubleClick(ByVal sender As Object, ByVal e As EventArgs)
			ProcessMouseClick(e)
		End Sub
		Private Sub LayoutView_SelectionChanged(ByVal sender As Object, ByVal e As SelectionChangedEventArgs)
			isForceSelectRow = e.Action = CollectionChangeAction.Add
			OnSelectionChanged()
		End Sub
		Private Sub LayoutView_FocusedRowChanged(ByVal sender As Object, ByVal e As FocusedRowChangedEventArgs)
			If DataSource IsNot Nothing AndAlso Not(TypeOf DataSource Is XPBaseCollection) Then
				visibleIndexOnChanged = e.PrevFocusedRowHandle
			End If
			OnFocusedObjectChanged()
		End Sub
		Private Sub LayoutView_Click(ByVal sender As Object, ByVal e As EventArgs)
			If processSelectedItemBySingleClick_Renamed Then
				ProcessMouseClick(e)
			End If
		End Sub
		Private Sub LayoutView_ValidateRow(ByVal sender As Object, ByVal e As ValidateRowEventArgs)
		End Sub
		Private Sub LayoutView_EditorShowing(ByVal sender As Object, ByVal e As CancelEventArgs)
			activeEditor = Nothing
			Dim edit As RepositoryItem = layoutView_Renamed.FocusedColumn.ColumnEdit
			If edit IsNot Nothing Then
				AddHandler edit.MouseDown, AddressOf Editor_MouseDown
				AddHandler edit.MouseUp, AddressOf Editor_MouseUp
				Dim buttonEdit As RepositoryItemButtonEdit = TryCast(edit, RepositoryItemButtonEdit)
				If buttonEdit IsNot Nothing Then
					AddHandler buttonEdit.ButtonPressed, AddressOf ButtonEdit_ButtonPressed
				End If
				Dim spinEdit As RepositoryItemBaseSpinEdit = TryCast(edit, RepositoryItemBaseSpinEdit)
				If spinEdit IsNot Nothing Then
					AddHandler spinEdit.Spin, AddressOf SpinEdit_Spin
				End If
				AddHandler edit.KeyDown, AddressOf Editor_KeyDown
				activeEditor = edit
			End If
		End Sub
		Private Sub LayoutView_ShownEditor(ByVal sender As Object, ByVal e As EventArgs)
			Dim popupEdit As PopupBaseEdit = TryCast(layoutView_Renamed.ActiveEditor, PopupBaseEdit)
			If popupEdit IsNot Nothing AndAlso activatedByMouse Then
				popupEdit.ShowPopup()
			End If
			activatedByMouse = False
		End Sub
		Private Sub LayoutView_HiddenEditor(ByVal sender As Object, ByVal e As EventArgs)
			If activeEditor IsNot Nothing Then
				RemoveHandler activeEditor.KeyDown, AddressOf Editor_KeyDown
				RemoveHandler activeEditor.MouseDown, AddressOf Editor_MouseDown
				RemoveHandler activeEditor.MouseUp, AddressOf Editor_MouseUp
				Dim buttonEdit As RepositoryItemButtonEdit = TryCast(activeEditor, RepositoryItemButtonEdit)
				If buttonEdit IsNot Nothing Then
					RemoveHandler buttonEdit.ButtonPressed, AddressOf ButtonEdit_ButtonPressed
				End If
				Dim spinEdit As RepositoryItemBaseSpinEdit = TryCast(activeEditor, RepositoryItemBaseSpinEdit)
				If spinEdit IsNot Nothing Then
					RemoveHandler spinEdit.Spin, AddressOf SpinEdit_Spin
				End If
				activeEditor = Nothing
			End If
		End Sub
		Private Sub LayoutView_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
			Dim view As LayoutView = CType(sender, LayoutView)
			Dim hi As LayoutViewHitInfo = view.CalcHitInfo(New Point(e.X, e.Y))
			If hi.RowHandle >= 0 Then
				mouseDownTime = System.Environment.TickCount
			Else
				mouseDownTime = 0
			End If
			activatedByMouse = True
		End Sub
		Private Sub LayoutView_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs)
			mouseUpTime = System.Environment.TickCount
			Dim hi As LayoutViewHitInfo = (CType(sender, LayoutView)).CalcHitInfo(New Point(e.X, e.Y))
			If hi.RowHandle = BaseListSourceDataController.NewItemRow Then
				layoutView_Renamed.ShowEditorByMouse()
			End If
		End Sub
		Private Sub LayoutView_CustomDrawFooterCell(ByVal sender As Object, ByVal e As FooterCellCustomDrawEventArgs)
			If e.Info.Visible Then
				Select Case e.Column.SummaryItem.SummaryType
					Case SummaryItemType.Sum, SummaryItemType.Average, SummaryItemType.Max, SummaryItemType.Min
						If (e.Column.ColumnEdit IsNot Nothing) Then
							e.Info.DisplayText = String.Format("{0}={1}", e.Column.SummaryItem.SummaryType.ToString(),e.Column.ColumnEdit.DisplayFormat.GetDisplayText(e.Info.Value))
						Else
							e.Info.DisplayText = String.Format("{0}={1}", e.Column.SummaryItem.SummaryType.ToString(),e.Info.Value.ToString())
						End If
				End Select
			End If
		End Sub
		Private Sub Editor_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
			If e.Button = MouseButtons.Left Then
				Dim currentTime As Int32 = System.Environment.TickCount
				If (mouseDownTime <= mouseUpTime) AndAlso (mouseUpTime <= currentTime) AndAlso (currentTime - mouseDownTime < SystemInformation.DoubleClickTime) Then
					Me.OnProcessSelectedItem()
					mouseDownTime = 0
				End If
			End If
		End Sub
		Private Sub Editor_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs)
			mouseUpTime = System.Environment.TickCount
		End Sub
		Private Sub Editor_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs)
			If e.KeyCode = Keys.Enter Then
				SubmitActiveEditorChanges()
			End If
		End Sub
		Private Sub SpinEdit_Spin(ByVal sender As Object, ByVal e As SpinEventArgs)
			mouseDownTime = 0
		End Sub
		Private Sub ButtonEdit_ButtonPressed(ByVal sender As Object, ByVal e As ButtonPressedEventArgs)
			mouseDownTime = 0
		End Sub
		Private Sub DataSource_ListChanged(ByVal sender As Object, ByVal e As ListChangedEventArgs)
			If (grid_Renamed IsNot Nothing) AndAlso (grid_Renamed.FindForm() IsNot Nothing) AndAlso (Not grid_Renamed.ContainsFocus) Then
				If (e.ListChangedType = ListChangedType.ItemAdded) AndAlso ((CType(sender, IList)).Count = 1) Then
					Dim obj As IEditableObject = TryCast((CType(sender, IList))(e.NewIndex), IEditableObject)
					If obj IsNot Nothing Then
						obj.EndEdit()
					End If
				End If
			End If
			If e.ListChangedType = ListChangedType.ItemChanged Then
				visibleIndexOnChanged = layoutView_Renamed.GetVisibleIndex(layoutView_Renamed.FocusedRowHandle)
			End If
			If e.ListChangedType = ListChangedType.ItemDeleted AndAlso layoutView_Renamed.FocusedRowHandle <> BaseListSourceDataController.NewItemRow Then
				layoutView_Renamed.FocusedRowHandle = layoutView_Renamed.GetVisibleRowHandle(visibleIndexOnChanged)
				OnFocusedObjectChanged()
			End If
			If layoutView_Renamed IsNot Nothing Then
				If e.ListChangedType = ListChangedType.Reset AndAlso layoutView_Renamed.SelectedRowsCount = 0 Then
					layoutView_Renamed.SelectRow(layoutView_Renamed.FocusedRowHandle)
				End If
			End If
		End Sub
		Private Sub SetTag()
			If grid_Renamed IsNot Nothing Then
				grid_Renamed.Tag = EasyTestTagHelper.FormatTestTable(Name)
			End If
		End Sub
		Private Sub RefreshColumn(ByVal frameColumn As ColumnInfoNodeWrapper, ByVal column As LayoutViewColumn)
			column.Caption = frameColumn.Caption
			If String.IsNullOrEmpty(column.Caption) Then
				column.Caption = column.FieldName
			End If
			column.LayoutViewField.ColumnName = column.Caption
			If (Not String.IsNullOrEmpty(frameColumn.DisplayFormat)) Then
				column.DisplayFormat.FormatString = frameColumn.DisplayFormat
				column.DisplayFormat.FormatType = FormatType.Custom
				column.GroupFormat.FormatString = frameColumn.DisplayFormat
				column.GroupFormat.FormatType = FormatType.Custom
			End If
			column.GroupIndex = frameColumn.GroupIndex
			column.SortIndex = frameColumn.SortIndex
			column.SortOrder = frameColumn.SortOrder
			column.Width = frameColumn.Width
			If column.VisibleIndex <> frameColumn.VisibleIndex Then
				column.VisibleIndex = frameColumn.VisibleIndex
			End If
			column.SummaryItem.SummaryType = frameColumn.SummaryType
		End Sub
		Protected Overridable Sub ProcessMouseClick(ByVal e As EventArgs)
			If (Not selectedItemActionExecuting) Then
				If LayoutView.FocusedRowHandle >= 0 Then
					Dim args As DXMouseEventArgs = DXMouseEventArgs.GetMouseArgs(grid_Renamed, e)
					Dim hitInfo As LayoutViewHitInfo = LayoutView.CalcHitInfo(args.Location)
					If hitInfo.InCard AndAlso (hitInfo.HitTest = LayoutViewHitTest.Field) Then
						args.Handled = True
						Me.OnProcessSelectedItem()
					End If
				End If
			End If
		End Sub
		Protected Overridable Sub OnCustomCreateColumn(ByVal args As CustomCreateColumnEventArgs)
			RaiseEvent CustomCreateColumn(Me, args)
		End Sub
		Protected Overridable Sub OnColumnCreated(ByVal column As LayoutViewColumn, ByVal columnInfo As ColumnInfoNodeWrapper)
			If ColumnCreatedEvent IsNot Nothing Then
				Dim args As New ColumnCreatedEventArgs(column, columnInfo)
				RaiseEvent ColumnCreated(Me, args)
			End If
		End Sub
		Protected Overrides Sub OnFocusedObjectChanged()
			MyBase.OnFocusedObjectChanged()
			focusedChangedRaised = True
		End Sub
		Protected Overrides Sub OnSelectionChanged()
			MyBase.OnSelectionChanged()
			selectedChangedRaised = True
			If LayoutView.SelectedRowsCount = 0 AndAlso isForceSelectRow Then
				LayoutView.SelectRow(LayoutView.FocusedRowHandle)
			End If
		End Sub
		Protected Overridable Sub OnGridDataSourceChanging()
			RaiseEvent GridDataSourceChanging(Me, EventArgs.Empty)
		End Sub
		Protected Overrides Function CreateControlsCore() As Object
			If grid_Renamed Is Nothing Then
				grid_Renamed = New GridControl()
				CType(grid_Renamed, System.ComponentModel.ISupportInitialize).BeginInit()
				Try
					grid_Renamed.MinimumSize = New Size(100, 75)
					grid_Renamed.Dock = DockStyle.Fill
					grid_Renamed.AllowDrop = True
					AddHandler grid_Renamed.HandleCreated, AddressOf grid_HandleCreated
					AddHandler grid_Renamed.KeyDown, AddressOf grid_KeyDown
					AddHandler grid_Renamed.DoubleClick, AddressOf grid_DoubleClick
					AddHandler grid_Renamed.ParentChanged, AddressOf grid_ParentChanged
					AddHandler grid_Renamed.VisibleChanged, AddressOf grid_VisibleChanged
					grid_Renamed.Height = 100
					grid_Renamed.TabStop = True
					grid_Renamed.MainView = CreateLayotView()
					SetTag()
					LayoutView.Columns.Clear()
					RefreshColumns()
				Finally
					CType(grid_Renamed, System.ComponentModel.ISupportInitialize).EndInit()
					layoutView_Renamed.ForceLoaded()
				End Try
			End If
			Return grid_Renamed
		End Function
		Private Sub grid_VisibleChanged(ByVal sender As Object, ByVal e As EventArgs)
			If grid_Renamed.Visible Then
				RemoveHandler grid_Renamed.VisibleChanged, AddressOf grid_VisibleChanged
				Dim defaultColumn As LayoutViewColumn = GetDefaultColumn()
				If defaultColumn IsNot Nothing Then
					layoutView_Renamed.FocusedColumn = defaultColumn
				End If
			End If
		End Sub
		Private Sub grid_ParentChanged(ByVal sender As Object, ByVal e As EventArgs)
			If grid_Renamed.Parent IsNot Nothing Then
				layoutView_Renamed.ForceLoaded()
			End If
		End Sub
		Private Function GetDefaultColumn() As LayoutViewColumn
			Dim result As LayoutViewColumn = Nothing
			Dim classType As Type = Model.BusinessObjectType
			If classType IsNot Nothing Then
				Dim defaultMember As IMemberInfo = XafTypesInfo.Instance.FindTypeInfo(classType).DefaultMember
				If defaultMember IsNot Nothing Then
					result = LayoutView.Columns(defaultMember.Name)
				End If
			End If
			If result Is Nothing OrElse (Not result.Visible) Then
				Return Nothing
			Else
				Return result
			End If
		End Function
		Private Sub RemoveColumnInfo(ByVal column As LayoutViewColumn)
			Dim originalPropertyName As String = columnsProperties(column)
			Dim columnInfo As ColumnInfoNodeWrapper = Model.Columns.FindColumnInfo(originalPropertyName)
			If columnInfo IsNot Nothing Then
				Model.Node.ChildNodes(0).RemoveChildNode(columnInfo.Node)
			End If
		End Sub
		Protected Overrides Sub OnProcessSelectedItem()
			If (layoutView_Renamed IsNot Nothing) AndAlso (layoutView_Renamed.ActiveEditor IsNot Nothing) Then
				BindingHelper.EndCurrentEdit(Grid)
			End If

			MyBase.OnProcessSelectedItem()
		End Sub

		Protected Friend Function IsDataShownOnDropDownWindow(ByVal repositoryItem As RepositoryItem) As Boolean
			Return DXPropertyEditor.RepositoryItemsTypesWithMandatoryButtons.Contains(repositoryItem.GetType())
		End Function
		Protected Overrides Sub AssignDataSourceToControl(ByVal dataSource As IList)
			If grid_Renamed IsNot Nothing AndAlso grid_Renamed.DataSource IsNot dataSource Then
				If TypeOf grid_Renamed.DataSource Is IBindingList Then
					RemoveHandler (CType(grid_Renamed.DataSource, IBindingList)).ListChanged, AddressOf DataSource_ListChanged
				End If
				If grid_Renamed.IsHandleCreated Then
					focusedChangedRaised = False
					selectedChangedRaised = False
					OnGridDataSourceChanging()
					grid_Renamed.BeginUpdate()
					Try
						grid_Renamed.DataSource = dataSource
					Finally
						grid_Renamed.EndUpdate()
					End Try
					If (Not selectedChangedRaised) Then
						OnSelectionChanged()
					End If
					If (Not focusedChangedRaised) Then
						OnFocusedObjectChanged()
					End If
					If TypeOf grid_Renamed.DataSource Is IBindingList Then
						AddHandler (CType(grid_Renamed.DataSource, IBindingList)).ListChanged, AddressOf DataSource_ListChanged
					End If
				End If
			End If
		End Sub
		Public Sub New(ByVal info As DictionaryNode)
			MyBase.New(info)
			popupMenu = New ActionsDXPopupMenu()
		End Sub
		Public Sub New()
			MyBase.New()
		End Sub
		Public Function AddColumn(ByVal columnInfo As ColumnInfoNodeWrapper) As LayoutViewColumn
			If columnsProperties.ContainsValue(columnInfo.PropertyName) Then
				Throw New ArgumentException(String.Format(SystemExceptionLocalizer.GetExceptionMessage(ExceptionId.GridColumnExists), columnInfo.PropertyName), "ColumnInfo")
			End If
			Dim frameColumn As ColumnInfoNodeWrapper = Model.Columns.FindColumnInfo(columnInfo.PropertyName)
			If frameColumn Is Nothing Then
				Model.Columns.Node.AddChildNode(columnInfo.Node)
			End If
			Dim column As New LayoutViewColumn()
			columnsProperties.Add(column, columnInfo.PropertyName)
			LayoutView.Columns.Add(column)
			Dim customArgs As New CustomCreateColumnEventArgs(column, columnInfo, repositoryFactory_Renamed)
			OnCustomCreateColumn(customArgs)
			If (Not customArgs.Handled) Then
				Dim memberDescriptor As IMemberInfo = XafTypesInfo.Instance.FindTypeInfo(ObjectType).FindMember(columnInfo.PropertyName)
				If memberDescriptor IsNot Nothing Then
					column.FieldName = memberDescriptor.BindingName
					If memberDescriptor.MemberType.IsEnum Then
						column.SortMode = ColumnSortMode.Value
					ElseIf (Not SimpleTypes.IsSimpleType(memberDescriptor.MemberType)) Then
						column.SortMode = ColumnSortMode.DisplayText
					End If
					If SimpleTypes.IsClass(memberDescriptor.MemberType) Then
						column.FilterMode = ColumnFilterMode.DisplayText
					Else
						column.FilterMode = ColumnFilterMode.Value
					End If
				Else
					column.FieldName = columnInfo.PropertyName
				End If
				RefreshColumn(columnInfo, column)
				If memberDescriptor IsNot Nothing Then
					If repositoryFactory_Renamed IsNot Nothing Then
						Dim repositoryItem As RepositoryItem = repositoryFactory_Renamed.CreateRepositoryItem(False, New DetailViewItemInfoNodeWrapper(columnInfo.Node), ObjectType)
						If repositoryItem IsNot Nothing Then
							grid_Renamed.RepositoryItems.Add(repositoryItem)
							column.ColumnEdit = repositoryItem
							If IsDataShownOnDropDownWindow(repositoryItem) Then
								column.OptionsColumn.AllowEdit = True
							Else
								column.OptionsColumn.AllowEdit = editMode_Renamed <> EditMode.ReadOnly
							End If
							repositoryItem.ReadOnly = repositoryItem.ReadOnly Or editMode_Renamed <> EditMode.Editable
							If (TypeOf repositoryItem Is ILookupEditRepositoryItem) AndAlso (CType(repositoryItem, ILookupEditRepositoryItem)).IsFilterByValueSupported Then
								column.FilterMode = ColumnFilterMode.Value
							End If
						End If
					End If
					If (column.ColumnEdit Is Nothing) AndAlso (Not GetType(IList).IsAssignableFrom(memberDescriptor.MemberType)) Then
						column.OptionsColumn.AllowEdit = False
						column.FieldName = GetDisplayablePropertyName(columnInfo.PropertyName)
					End If
				End If
			End If
			OnColumnCreated(column, columnInfo)
			Return column
		End Function
		Public Sub RemoveColumn(ByVal propertyName As String)
			Dim found As Boolean = False
			If LayoutView IsNot Nothing Then
				For Each column As LayoutViewColumn In LayoutView.Columns
					If (column.FieldName = propertyName) OrElse (column.FieldName = propertyName & "!") Then
						RemoveColumnInfo(column)
						columnsProperties.Remove(column)
						LayoutView.Columns.Remove(column)
						found = True
						Exit For
					End If
				Next column
			End If
			If (Not found) Then
				Throw New ArgumentException(String.Format(SystemExceptionLocalizer.GetExceptionMessage(ExceptionId.GridColumnDoesNotExist), propertyName), "PropertyName")
			End If
		End Sub
		Public Sub RefreshColumns()
			Grid.BeginUpdate()
			Try
				Dim presentedColumns As Dictionary(Of String, LayoutViewColumn) = New Dictionary(Of String, LayoutViewColumn)()
				Dim toDelete As List(Of LayoutViewColumn) = New List(Of LayoutViewColumn)()
				For Each column As LayoutViewColumn In LayoutView.Columns
					presentedColumns.Add(columnsProperties(column), column)
					toDelete.Add(column)
				Next column
				For Each column As ColumnInfoNodeWrapper In Model.Columns.Items
					Dim LayoutViewColumn As LayoutViewColumn = Nothing
					If presentedColumns.TryGetValue(column.PropertyName, LayoutViewColumn) Then
						RefreshColumn(column, LayoutViewColumn)
					Else
						LayoutViewColumn = AddColumn(column)
						presentedColumns.Add(column.PropertyName, LayoutViewColumn)
					End If
					toDelete.Remove(LayoutViewColumn)
				Next column
				For Each LayoutViewColumn As LayoutViewColumn In toDelete
					LayoutView.Columns.Remove(LayoutViewColumn)
					columnsProperties.Remove(LayoutViewColumn)
				Next LayoutViewColumn
			Finally
				Grid.EndUpdate()
			End Try
		End Sub
		Public Overrides Sub Refresh()
			If grid_Renamed IsNot Nothing Then
				grid_Renamed.RefreshDataSource()
			End If
		End Sub
		Public Overrides Sub Dispose()
			ColumnCreatedEvent = Nothing
			CustomCreateColumnEvent = Nothing
			GridDataSourceChangingEvent = Nothing
			If popupMenu IsNot Nothing Then
				popupMenu.Dispose()
				popupMenu = Nothing
			End If
			columnsProperties.Clear()
			If layoutView_Renamed IsNot Nothing Then
				RemoveHandler layoutView_Renamed.FocusedRowChanged, AddressOf LayoutView_FocusedRowChanged
				RemoveHandler layoutView_Renamed.SelectionChanged, AddressOf LayoutView_SelectionChanged
				RemoveHandler layoutView_Renamed.ShowingEditor, AddressOf LayoutView_EditorShowing
				RemoveHandler layoutView_Renamed.ShownEditor, AddressOf LayoutView_ShownEditor
				RemoveHandler layoutView_Renamed.HiddenEditor, AddressOf LayoutView_HiddenEditor
				RemoveHandler layoutView_Renamed.MouseDown, AddressOf LayoutView_MouseDown
				RemoveHandler layoutView_Renamed.MouseUp, AddressOf LayoutView_MouseUp
				RemoveHandler layoutView_Renamed.Click, AddressOf LayoutView_Click
				RemoveHandler layoutView_Renamed.ValidateRow, AddressOf LayoutView_ValidateRow
				RemoveHandler layoutView_Renamed.InitNewRow, AddressOf LayoutView_InitNewRow
				layoutView_Renamed.Dispose()
				layoutView_Renamed = Nothing
			End If
			If grid_Renamed IsNot Nothing Then
				If TypeOf grid_Renamed.DataSource Is IBindingList Then
					RemoveHandler (CType(grid_Renamed.DataSource, IBindingList)).ListChanged, AddressOf DataSource_ListChanged
				End If
				grid_Renamed.DataSource = Nothing
				RemoveHandler grid_Renamed.VisibleChanged, AddressOf grid_VisibleChanged
				RemoveHandler grid_Renamed.KeyDown, AddressOf grid_KeyDown
				RemoveHandler grid_Renamed.HandleCreated, AddressOf grid_HandleCreated
				RemoveHandler grid_Renamed.DoubleClick, AddressOf grid_DoubleClick
				RemoveHandler grid_Renamed.ParentChanged, AddressOf grid_ParentChanged
				grid_Renamed.RepositoryItems.Clear()
				grid_Renamed.Dispose()
				grid_Renamed = Nothing
			End If
			MyBase.Dispose()
		End Sub
		Public Overrides Function GetSelectedObjects() As IList
			Dim selectedObjects As New ArrayList()
			If LayoutView IsNot Nothing Then
				Dim selectedRows() As Integer = LayoutView.GetSelectedRows()
				If (selectedRows IsNot Nothing) AndAlso (selectedRows.Length > 0) Then
					For Each rowHandle As Integer In selectedRows
						If (Not IsGroupRowHandle(rowHandle)) Then
							Dim obj As Object = LayoutView.GetRow(rowHandle)
							If obj IsNot Nothing Then
								selectedObjects.Add(obj)
							End If
						End If
					Next rowHandle
				End If
			End If
			Return CType(selectedObjects.ToArray(GetType(Object)), Object())
		End Function
		Public Overrides Sub SynchronizeInfo()
			If LayoutView IsNot Nothing Then
				Model.Node.SetAttribute(IsGroupPanelVisible, layoutView_Renamed.OptionsView.ShowHeaderPanel)
				Model.Node.SetAttribute(IsFooterVisible, layoutView_Renamed.OptionsView.ShowCardLines)
				For Each column As LayoutViewColumn In LayoutView.Columns
					Dim propertyName As String
					If columnsProperties.TryGetValue(column, propertyName) Then
						Dim frameColumn As ColumnInfoNodeWrapper = Model.Columns.FindColumnInfo(propertyName)
						If column.Caption <> frameColumn.Caption Then
							frameColumn.Caption = column.Caption
						End If
						If column.Width <> frameColumn.Width Then
							frameColumn.Width = column.Width
						End If
						If column.GroupIndex <> frameColumn.GroupIndex Then
							frameColumn.GroupIndex = column.GroupIndex
						End If
						If frameColumn.SortIndex <> column.SortIndex Then
							frameColumn.SortIndex = column.SortIndex
						End If
						If frameColumn.SortOrder <> column.SortOrder Then
							frameColumn.SortOrder = column.SortOrder
						End If
						If frameColumn.VisibleIndex <> column.VisibleIndex Then
							frameColumn.VisibleIndex = column.VisibleIndex
						End If
						If frameColumn.SummaryType <> column.SummaryItem.SummaryType Then
							frameColumn.SummaryType = column.SummaryItem.SummaryType
						End If
					End If
				Next column
			End If
		End Sub
		Public Overrides Sub StartIncrementalSearch(ByVal searchString As String)
			Dim defaultColumn As LayoutViewColumn = GetDefaultColumn()
			If defaultColumn IsNot Nothing Then
				LayoutView.FocusedColumn = defaultColumn
			End If
		End Sub
		Public Function GetPrintable() As IPrintable Implements IPrintableSource.GetPrintable
			Return grid_Renamed
		End Function
		Private Function ReplaceExclamationMarks(ByVal memberName As String) As String
			Return memberName.TrimEnd("!"c).Replace("!"c, "."c)
		End Function
		Public Overrides ReadOnly Property ShownProperties() As String()
			Get
				Dim result As List(Of String) = New List(Of String)()
				If (ObjectType IsNot Nothing) AndAlso (LayoutView IsNot Nothing) Then
					For Each column As LayoutViewColumn In LayoutView.VisibleColumns
						If TypeOf column.ColumnEdit Is IShownPropertiesProvider Then
							Dim editorShownProperties() As String = (CType(column.ColumnEdit, IShownPropertiesProvider)).ShownProperties
							For Each propertyName As String In editorShownProperties
								If (Not String.IsNullOrEmpty(propertyName)) Then
									result.Add(ReplaceExclamationMarks(column.FieldName) & "."c & propertyName)
								Else
									result.Add(ReplaceExclamationMarks(column.FieldName))
								End If
							Next propertyName
						Else
							result.Add(ReplaceExclamationMarks(column.FieldName))
						End If
					Next column
				End If
				Return result.ToArray()
			End Get
		End Property
		Public Overrides ReadOnly Property RequiredProperties() As String()
			Get
				Dim result As List(Of String) = New List(Of String)()
				If (ObjectType IsNot Nothing) AndAlso (LayoutView IsNot Nothing) Then
					For Each column As LayoutViewColumn In LayoutView.VisibleColumns
						result.Add(column.FieldName)
					Next column
				End If
				Return result.ToArray()
			End Get
		End Property
		Public Overrides ReadOnly Property ContextMenuTemplate() As IContextMenuTemplate
			Get
				Return popupMenu
			End Get
		End Property
		Public Overrides Property Name() As String
			Get
				Return MyBase.Name
			End Get
			Set(ByVal value As String)
				MyBase.Name = value
				SetTag()
			End Set
		End Property
		Public Overrides Property EditMode() As EditMode
			Get
				Return editMode_Renamed
			End Get
			Set(ByVal value As EditMode)
				If grid_Renamed IsNot Nothing Then
					Throw New InvalidOperationException("Cannot set EditMode property. GridControl have been created already.")
				End If
				editMode_Renamed = value
			End Set
		End Property
		Public Overrides Property FocusedObject() As Object
			Get
				Dim result As Object = Nothing
				If LayoutView IsNot Nothing Then
					result = LayoutView.GetRow(LayoutView.FocusedRowHandle)
				End If
				Return result
			End Get
			Set(ByVal value As Object)
				If (value IsNot Nothing) AndAlso (layoutView_Renamed IsNot Nothing) AndAlso (DataSource IsNot Nothing) Then
					layoutView_Renamed.SelectRow(layoutView_Renamed.GetRowHandle(DataSource.IndexOf(value)))
					If layoutView_Renamed.IsValidRowHandle(layoutView_Renamed.FocusedRowHandle) Then
						layoutView_Renamed.ExpandCard(layoutView_Renamed.FocusedRowHandle)
					End If
				End If
			End Set
		End Property
		Public Overrides ReadOnly Property SelectionType() As SelectionType
			Get
				Return SelectionType.Full
			End Get
		End Property
		Public Property RepositoryFactory() As RepositoryEditorsFactory
			Get
				Return repositoryFactory_Renamed
			End Get
			Set(ByVal value As RepositoryEditorsFactory)
				repositoryFactory_Renamed = value
			End Set
		End Property
		Public ReadOnly Property Grid() As GridControl
			Get
				Return grid_Renamed
			End Get
		End Property
		Public ReadOnly Property LayoutView() As XafLayotView
			Get
				Return layoutView_Renamed
			End Get
		End Property
		#Region "IDXPopupMenuHolder Members"
		Public ReadOnly Property PopupSite() As Control Implements IDXPopupMenuHolder.PopupSite
			Get
				Return Grid
			End Get
		End Property
		Public Function CanShowPopupMenu(ByVal position As Point) As Boolean Implements IDXPopupMenuHolder.CanShowPopupMenu
			Dim hitTest As LayoutViewHitTest = layoutView_Renamed.CalcHitInfo(grid_Renamed.PointToClient(position)).HitTest
			Return ((hitTest = LayoutViewHitTest.Card) OrElse (hitTest = LayoutViewHitTest.Field) OrElse (hitTest = LayoutViewHitTest.LayoutItem) OrElse (hitTest = LayoutViewHitTest.None))
		End Function
		Public Sub SetMenuManager(ByVal manager As IDXMenuManager) Implements IDXPopupMenuHolder.SetMenuManager
			grid_Renamed.MenuManager = manager
		End Sub
		#End Region
		#Region "IControlOrderProvider Members"
		Public Function GetIndexByObject(ByVal obj As Object) As Integer Implements IControlOrderProvider.GetIndexByObject
			Dim index As Integer = -1
			If (DataSource IsNot Nothing) AndAlso (layoutView_Renamed IsNot Nothing) Then
				Dim dataSourceIndex As Integer = DataSource.IndexOf(obj)
				index = layoutView_Renamed.GetRowHandle(dataSourceIndex)
				If index = GridControl.InvalidRowHandle Then
					index = -1
				End If
			End If
			Return index
		End Function
		Public Function GetObjectByIndex(ByVal index As Integer) As Object Implements IControlOrderProvider.GetObjectByIndex
			If (layoutView_Renamed IsNot Nothing) AndAlso (layoutView_Renamed.DataController IsNot Nothing) Then
				Return layoutView_Renamed.GetRow(index)
			End If
			Return Nothing
		End Function
		Public Function GetOrderedObjects() As IList Implements IControlOrderProvider.GetOrderedObjects
			Dim list As List(Of Object) = New List(Of Object)()
			If layoutView_Renamed IsNot Nothing AndAlso layoutView_Renamed.GridControl IsNot Nothing AndAlso (Not grid_Renamed.ServerMode) Then
				For i As Integer = 0 To layoutView_Renamed.DataRowCount - 1
					list.Add(layoutView_Renamed.GetRow(i))
				Next i
			End If
			Return list
		End Function
		#End Region
		#Region "IComplexListEditor Members"
		Public Overridable Sub Setup(ByVal collectionSource As CollectionSourceBase, ByVal application As XafApplication) Implements IComplexListEditor.Setup
			repositoryFactory_Renamed = New RepositoryEditorsFactory(application, collectionSource.ObjectSpace)
		End Sub
		#End Region
		#Region "ILookupListEditor Members"
		Public Property ProcessSelectedItemBySingleClick() As Boolean Implements ILookupListEditor.ProcessSelectedItemBySingleClick
			Get
				Return processSelectedItemBySingleClick_Renamed
			End Get
			Set(ByVal value As Boolean)
				processSelectedItemBySingleClick_Renamed = value
			End Set
		End Property
		Public Event BeginCustomization As EventHandler Implements ILookupListEditor.BeginCustomization
		Public Event EndCustomization As EventHandler Implements ILookupListEditor.EndCustomization
		#End Region
		Public Event ColumnCreated As EventHandler(Of ColumnCreatedEventArgs)
		Public Event CustomCreateColumn As EventHandler(Of CustomCreateColumnEventArgs)
		Public Event GridDataSourceChanging As EventHandler

		#Region "ILookupListEditor Member"


		Public Property TrackMousePosition() As Boolean Implements ILookupListEditor.TrackMousePosition
			Get
				Return False
			End Get
			Set(ByVal value As Boolean)
				Return
			End Set
		End Property

		#End Region
	End Class
	Friend Class TimeAutoLatch
		Private lastEventTicks As Long
		Private timeIntervalInMs As Integer
		Public Sub New(ByVal timeIntervalInMs As Integer)
			Me.timeIntervalInMs = timeIntervalInMs
			Me.lastEventTicks = 0
		End Sub
		Public Sub New()
			Me.New(100)
		End Sub
		Public ReadOnly Property IsTimeIntervalExpired() As Boolean
			Get
				Dim result As Boolean = ((DateTime.Now.Ticks - lastEventTicks) / 10000) > timeIntervalInMs
				If result Then
					lastEventTicks = DateTime.Now.Ticks
				End If
				Return result
			End Get
		End Property
		Public Sub Reset()
			lastEventTicks = 0
		End Sub
	End Class
End Namespace
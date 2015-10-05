﻿#region

using System;
using System.Linq;
using JosephM.Application.ViewModel.Grid;
using JosephM.Record.IService;
using JosephM.Record.Metadata;

#endregion

namespace JosephM.Application.ViewModel.RecordEntry.Field
{
    public class LookupGridViewModel : ViewModelBase
    {
        public LookupGridViewModel(IReferenceFieldViewModel referenceField,
            Action<IRecord> onRecordSelected)
            : base(referenceField.RecordEntryViewModel.ApplicationController)
        {
            OnRecordSelected = onRecordSelected;
            DynamicGridViewModel = new DynamicGridViewModel(ApplicationController)
            {
                OnDoubleClick = OnDoubleClick,
                ViewType = ViewType.LookupView,
                RecordService = referenceField.LookupService,
                FormController = new FormController(referenceField.LookupService, null, referenceField.RecordEntryViewModel.ApplicationController),
                RecordType = referenceField.RecordTypeToLookup,
                IsReadOnly = true
            };
        }

        private Action<IRecord> OnRecordSelected { get; set; }

        public void OnKeyDown()
        {
        }

        public void OnDoubleClick()
        {
            SetLookupToSelectedRow();
        }

        public void SetLookupToSelectedRow()
        {
            if (DynamicGridViewModel.SelectedRow != null)
                OnRecordSelected(DynamicGridViewModel.SelectedRow.Record);
        }

        public void MoveDown()
        {
            try
            {
                if (DynamicGridViewModel.GridRecords != null && DynamicGridViewModel.GridRecords.Any())
                {
                    var index = -1;
                    if (DynamicGridViewModel.SelectedRow != null)
                        index = DynamicGridViewModel.GridRecords.IndexOf(DynamicGridViewModel.SelectedRow);
                    index++;
                    if (index > DynamicGridViewModel.GridRecords.Count - 1)
                        index = 0;
                    DynamicGridViewModel.SelectedRow = DynamicGridViewModel.GridRecords[index];
                }
            }
            catch
            {
            }
        }

        public void MoveUp()
        {
            try
            {
                if (DynamicGridViewModel.GridRecords != null && DynamicGridViewModel.GridRecords.Any())
                {
                    var index = DynamicGridViewModel.GridRecords.Count;
                    if (DynamicGridViewModel.SelectedRow != null)
                        index = DynamicGridViewModel.GridRecords.IndexOf(DynamicGridViewModel.SelectedRow);
                    index--;
                    if (index < 0)
                        index = DynamicGridViewModel.GridRecords.Count - 1;
                    DynamicGridViewModel.SelectedRow = DynamicGridViewModel.GridRecords[index];
                }
            }
            catch
            {
            }
        }

        public DynamicGridViewModel DynamicGridViewModel { get; set; }
    }
}
﻿#region

using JosephM.Prism.Infrastructure.Prism;
using JosephM.Record.Application.Dialog;
using JosephM.Record.Xrm.XrmRecord;

#endregion

namespace JosephM.Xrm.RecordExtract.TextSearch
{
    public class XrmTextSearchSettingsDialog : TextSearchSettingsDialogBase
    {
        public XrmTextSearchSettingsDialog(IDialogController dialogController, PrismContainer container,
            XrmRecordService recordService)
            : base(dialogController, container, recordService)
        {
        }
    }
}
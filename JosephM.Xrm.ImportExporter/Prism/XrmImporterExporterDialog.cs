﻿#region

using JosephM.Core.FieldType;
using JosephM.Prism.Infrastructure.Dialog;
using JosephM.Record.Application.Dialog;
using JosephM.Record.IService;
using JosephM.Record.Xrm.XrmRecord;
using JosephM.Xrm.ImportExporter.Service;

#endregion

namespace JosephM.Xrm.ImportExporter.Prism
{
    public class XrmImporterExporterDialog :
        ServiceRequestDialog
            <XrmImporterExporterService<XrmRecord, XrmRecordService>, XrmImporterExporterRequest,
                XrmImporterExporterResponse, XrmImporterExporterResponseItem>
    {
        public XrmImporterExporterDialog(XrmImporterExporterService<XrmRecord, XrmRecordService> service,
            IDialogController dialogController, XrmRecordService lookupService)
            : base(service, dialogController, lookupService)
        {
        }
    }
}
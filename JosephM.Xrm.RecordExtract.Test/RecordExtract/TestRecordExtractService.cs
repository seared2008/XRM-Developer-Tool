﻿using JosephM.Application.ViewModel.Fakes;
using JosephM.Record.Application.Fakes;
using JosephM.Xrm.RecordExtract.RecordExtract;

namespace JosephM.Xrm.RecordExtract.Test.RecordExtract
{
    public class TestRecordExtractService : RecordExtractService
    {
        public TestRecordExtractService(FakeRecordService service, IRecordExtractSettings settings,
            DocumentWriter.DocumentWriter documentWriter)
            : base(service, settings, documentWriter)
        {
        }
    }
}
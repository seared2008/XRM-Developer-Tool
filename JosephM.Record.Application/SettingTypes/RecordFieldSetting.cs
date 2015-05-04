﻿using JosephM.Core.Attributes;
using JosephM.Core.FieldType;

namespace JosephM.Record.Application.SettingTypes
{
    public class RecordFieldSetting
    {
        [DisplayOrder(10)]
        [RequiredProperty]
        [RecordTypeFor("RecordField")]
        public RecordType RecordType { get; set; }

        [DisplayOrder(20)]
        [RequiredProperty]
        public RecordField RecordField { get; set; }
    }
}